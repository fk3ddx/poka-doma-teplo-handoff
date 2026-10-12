"""Household candidate edits (SCENARIO 4 / 6 / 11 / 14) from the CC0 previews fetched by fetch_cc0_previews.py.
Separate from prepare_candidates.py so accepted earlier WAVs are not regenerated. Audition required before acceptance.
  python tools/audio/prepare_household.py [--library-dir <site-packages with numpy+soundfile>]"""
import argparse,sys,json,hashlib,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--library-dir');a=p.parse_args()
if a.library_dir:sys.path.insert(0,a.library_dir)
import soundfile as sf,numpy as np
repo=Path(__file__).resolve().parents[2];src=repo/'art-sources/Audio';dest=repo/'unity-game/Assets/_Project/Audio/Recorded';dest.mkdir(parents=True,exist_ok=True)
manifest=json.loads((src/'Freesound/manifest.json').read_text(encoding='utf-8'));byrole={e['role']:src/'Freesound'/e['file'] for e in manifest if 'file' in e};edits=[]
def read(path):
 x,sr=sf.read(path,dtype='float32',always_2d=True);return x.mean(axis=1),sr
def put(name,role,start=0,end=None,loop=False,attack=.01,release=.04,long_sound=None):
 path=byrole[role];x,sr=read(path);end=end if end is not None else len(x)/sr;x=x[max(0,int(start*sr)):min(len(x),int(end*sr))].copy()
 if len(x)<sr*.05:raise ValueError(name+' empty/short')
 if loop:
  n=min(int(.08*sr),len(x)//8);fade=np.linspace(0,1,n);joined=x[-n:]*(1-fade)+x[:n]*fade;x=np.concatenate((joined,x[n:-n]))
 else:
  n=max(1,min(int(attack*sr),len(x)//4));m=max(1,min(int(release*sr),len(x)//4));x[:n]*=np.linspace(0,1,n);x[-m:]*=np.linspace(1,0,m)
 long_sound=loop if long_sound is None else long_sound
 peak=float(np.abs(x).max());rms=float(np.sqrt(np.mean(x.astype('float64')**2)));peak_target=-3 if long_sound else -1;rms_target=-20 if long_sound else None
 gain=10**(peak_target/20)/max(peak,1e-6);gain=min(gain,10**(rms_target/20)/max(rms,1e-6)) if long_sound else gain;x*=gain;target=dest/(name+'.wav');sf.write(target,x,sr,subtype='PCM_16')
 edits.append(dict(file=target.relative_to(repo).as_posix(),source=path.relative_to(repo).as_posix(),start=start,end=end,loop_crossfade=loop,attack_fade_seconds=attack,release_fade_seconds=release,mono=True,gain_db=20*math.log10(gain),peak_target_dbfs=peak_target,rms_target_dbfs=rms_target,achieved_rms_dbfs=20*math.log10(max(float(np.sqrt(np.mean(x.astype("float64")**2))),1e-12)),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),audition='pending'))
# Kitchen (4.2 / 4.8 / 11.7-11.8)
put('match_strike','match',0,1.4,attack=.002,release=.15)
put('gas_ignite','gas-ignite',4.6,7.4,attack=.003,release=.3)
put('gas_hiss_loop','gas-ignite',7.5,10.4,True)
put('tap_fill','tap-fill',.3,6.6,attack=.15,release=.4,long_sound=True)
put('kettle_rise','kettle-whistle',17,24.5,attack=.5,release=.05,long_sound=True)
put('kettle_whistle_loop','kettle-whistle',23.5,29.5,True)
put('spoon_clink','spoon',4.6,6.2,attack=.002,release=.1)
put('drip_00','drip',0,1.2,attack=.001,release=.2);put('drip_01','drip',1.4,2.6,attack=.001,release=.2)
# Clock key (4.6 / 10.4), bulb (4.5), electricity (22:01), TV (4.7 / 22:00), street (night 21:42-22:00)
put('clock_wind','clock-wind',1.5,9.5,attack=.005,release=.1)
put('bulb_screw','bulb',.1,1.6,attack=.005,release=.2)
put('switch_click','switch',.55,.95,attack=.001,release=.05)
put('breaker','breaker',9.4,10.3,attack=.001,release=.15)
put('tv_on','tv',0,2.2,attack=.002,release=.2)
put('crt_whine_loop','crt-whine',2,12,True)
put('doorbell','doorbell',2.3,6.8,attack=.002,release=.5)
put('car_pass','car-pass',0,10.8,attack=.5,release=1,long_sound=True)
(src/'processed-household-manifest.json').write_bytes((json.dumps(edits,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
print(len(edits),'household candidate WAVs; listening pending')
