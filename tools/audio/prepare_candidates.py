"""Create reproducible candidate edits. Audition required before art acceptance."""
import argparse,sys,json,hashlib,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--library-dir',required=True);a=p.parse_args();sys.path.insert(0,a.library_dir)
import soundfile as sf,numpy as np
repo=Path(__file__).resolve().parents[2];src=repo/'art-sources/Audio';dest=repo/'unity-game/Assets/_Project/Audio/Recorded';dest.mkdir(parents=True,exist_ok=True)
manifest=json.loads((src/'Freesound/manifest.json').read_text(encoding='utf-8'));byrole={e['role']:src/'Freesound'/e['file'] for e in manifest if 'file' in e};edits=[]
def read(path):
 x,sr=sf.read(path,dtype='float32',always_2d=True);return x.mean(axis=1),sr

def put(name,path,start=0,end=None,loop=False,attack=.01,release=.04):
 x,sr=read(path);end=end if end is not None else len(x)/sr;x=x[max(0,int(start*sr)):min(len(x),int(end*sr))].copy()
 if len(x)<sr*.05:raise ValueError(name+' empty/short')
 if loop:
  n=min(int(.08*sr),len(x)//8);fade=np.linspace(0,1,n);joined=x[-n:]*(1-fade)+x[:n]*fade;x=np.concatenate((joined,x[n:-n]))
 else:
  n=min(int(attack*sr),len(x)//4);m=min(int(release*sr),len(x)//4);x[:n]*=np.linspace(0,1,n);x[-m:]*=np.linspace(1,0,m)
 peak=float(np.abs(x).max());rms=float(np.sqrt(np.mean(x.astype('float64')**2)));long_sound=loop or name.startswith('guest_breath_');peak_target=-3 if long_sound else -1;rms_target=-20 if long_sound else None;gain=10**(peak_target/20)/max(peak,1e-6);gain=min(gain,10**(rms_target/20)/max(rms,1e-6)) if long_sound else gain;x*=gain;target=dest/(name+'.wav');sf.write(target,x,sr,subtype='PCM_16')
 edits.append(dict(file=target.relative_to(repo).as_posix(),source=path.relative_to(repo).as_posix(),start=start,end=end,loop_crossfade=loop,attack_fade_seconds=attack,release_fade_seconds=release,mono=True,gain_db=20*math.log10(gain),peak_target_dbfs=peak_target,rms_target_dbfs=rms_target,achieved_rms_dbfs=20*math.log10(max(float(np.sqrt(np.mean(x.astype("float64")**2))),1e-12)),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),audition='pending'))

def transients(role,prefix,count,spacing=.5,before=.03,tail=.35):
 path=byrole[role];x,sr=read(path);hop=max(1,int(sr*.01));env=np.array([np.sqrt(np.mean(x[i:i+hop]**2)) for i in range(0,len(x),hop)]);chosen=[]
 for i in np.argsort(env)[::-1]:
  t=i*.01
  if t<.1 or t>len(x)/sr-tail or any(abs(t-v)<spacing for v in chosen):continue
  chosen.append(t)
  if len(chosen)==count:break
 for n,t in enumerate(sorted(chosen)):put(f'{prefix}_{n:02}',path,max(0,t-before),t+tail)
for n,(st,en) in enumerate([(7.15,9.55),(13.15,15.8),(19.15,21.75)]):put(f'phone_{n:02}',byrole['phone'],st,en)
put('clock_loop',byrole['clock'],20,40,True);put('chime',byrole['chime'],24.98,39)
for n,(st,en) in enumerate([(.4,4.3),(5.7,8),(8.8,12.5),(14.4,17)]):put(f'guest_breath_{n:02}',byrole['guest-breath'],st,en)
put('player_breath_loop',byrole['player-breath'],.15,6.55,True)
put('door_creak_00',byrole['door']);put('shriek',byrole['shriek']);transients('knock','knock',3,.7,.07,.38)
transients('slippers','step',8,.8,.12,.45);transients('palms','palms',6,.7,.07,.4)
put('heartbeat_loop',byrole['heartbeat'],2,12,True);put('wind_loop',byrole['wind'],8,28,True);put('rope_loop',byrole['rope'],2,8,True);put('scratch',byrole['scratch']);put('fridge_loop',byrole['fridge'],3,15,True)

put('finger_crack',byrole['crack'],attack=.002)
# Deterministic 1.2-second candidate from the densest sustained foley region.
# This is an edit-selection aid, not a listening judgement.
x,sr=read(byrole['tear']);width=int(1.2*sr);hop=int(.05*sr)
starts=list(range(0,len(x)-width,hop));energies=[float(np.mean(x[i:i+width].astype('float64')**2)) for i in starts]
start=starts[int(np.argmax(energies))]/sr
put('wet_tear',byrole['tear'],start,start+.9,attack=.002,release=.08)
put('choked_cry',byrole['pain-cry'],attack=.002)
# Deliberate synthesized tinnitus asset, not a field recording. Fits 4.5-5.0s.
put('tinnitus',byrole['tinnitus'],1,1.45,attack=.005,release=.1)

for prefix,pattern in [('door_close','doorClose_*.ogg'),('door_open','doorOpen_*.ogg'),('latch','metalLatch.ogg')]:
 for n,path in enumerate(sorted((src/'Kenney/RPG/Audio').glob(pattern))):put(f'{prefix}_{n:02}',path)
for prefix,pattern in [('wood_impact','impactWood_heavy_*.ogg'),('glass_impact','impactGlass_light_*.ogg'),('carpet_step','footstep_carpet_*.ogg')]:
 for n,path in enumerate(sorted((src/'Kenney/Impact/Audio').glob(pattern))):put(f'{prefix}_{n:02}',path)
(src/'processed-manifest.json').write_bytes((json.dumps(edits,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
print(len(edits),'mono candidate WAVs; peaks -1 transient / -3 long, long RMS target -20 bounded by peak; listening pending')