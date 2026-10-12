"""Prepare licensed TV audience laughter; never plays audio. Listening approval stays pending."""
from pathlib import Path
import argparse,sys,json,hashlib,math
p=argparse.ArgumentParser();p.add_argument('--library-dir',required=True);a=p.parse_args();sys.path.insert(0,a.library_dir)
import soundfile as sf,numpy as np
repo=Path(__file__).resolve().parents[2]
source=repo/'art-source/prologue/SmallAudience-17121-preview.mp3'
x,sr=sf.read(source,dtype='float32',always_2d=True);x=x.mean(axis=1);x-=x.mean()
# A small television behind a closed door: remove rumble and soften the recorded room's treble.
def lowpass(signal,hz):
 alpha=1-math.exp(-2*math.pi*hz/sr);state=0.;out=np.empty_like(signal)
 for i,value in enumerate(signal):state+=alpha*(float(value)-state);out[i]=state
 return out
x-=lowpass(x,180);x=lowpass(lowpass(x,2800),2800)
for seconds,reverse in [(.03,False),(.14,True)]:
 n=int(seconds*sr);ramp=np.linspace(0,1,n,dtype='float32')
 if reverse:x[-n:]*=ramp[::-1]
 else:x[:n]*=ramp
peak=float(abs(x).max());gain=min(1.,10**(-6/20)/max(peak,1e-6));x*=gain
out=repo/'unity-game/Assets/_Project/Resources/Audio/tv_laughter.wav';sf.write(out,x,sr,subtype='PCM_16')
assert np.isfinite(x).all() and abs(x).max()<1 and x[0]==0 and x[-1]==0
info=json.loads(source.with_suffix('.source.json').read_text(encoding='utf-8'))
info.update(bytes=out.stat().st_size,file=out.relative_to(repo).as_posix(),source_file=source.relative_to(repo).as_posix(),mono=True,samplerate=sr,duration_seconds=len(x)/sr,filter='one pole high pass 180 Hz, two one pole low passes 2800 Hz',attack_seconds=.03,release_seconds=.14,gain=float(gain),peak_dbfs=float(20*np.log10(max(float(abs(x).max()),1e-12))),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),validation='finite samples, no clipping, zero fade endpoints',audition='pending, no speaker playback')
(repo/'art-source/prologue/tv-laughter-edit.json').write_text(json.dumps(info,indent=2)+'\n',encoding='utf-8',newline='\n');print(json.dumps(info,indent=2))
