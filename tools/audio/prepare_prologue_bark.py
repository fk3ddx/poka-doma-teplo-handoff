"""Prepare one sourced CC0 dog bark without playing audio. Audition remains pending."""
from pathlib import Path
import argparse,sys,json,hashlib
p=argparse.ArgumentParser();p.add_argument('--library-dir',required=True);a=p.parse_args();sys.path.insert(0,a.library_dir)
import soundfile as sf,numpy as np
repo=Path(__file__).resolve().parents[2]
source=repo/'art-source/prologue/SmallDogBehindDoor-3537.mp3'
x,sr=sf.read(source,dtype='float32',always_2d=True);x=x.mean(axis=1)
start,end=1.8,3.15
x=x[int(sr*start):int(sr*end)].copy();x-=x.mean()
for count,reverse in [(int(.015*sr),False),(int(.18*sr),True)]:
 ramp=np.linspace(0,1,count,dtype='float32')
 if reverse:x[-count:]*=ramp[::-1]
 else:x[:count]*=ramp
# Preserve recorded dynamics, with headroom for the in-game bank gain.
peak=float(abs(x).max());gain=min(1.0,10**(-6/20)/max(peak,1e-6));x*=gain
out=repo/'unity-game/Assets/_Project/Resources/Audio/dog_bark.wav';out.parent.mkdir(parents=True,exist_ok=True)
sf.write(out,x,sr,subtype='PCM_16')
entry=json.loads(source.with_suffix('.source.json').read_text(encoding='utf-8'))
entry.update(file=out.relative_to(repo).as_posix(),source_file=source.relative_to(repo).as_posix(),start_seconds=start,end_seconds=end,mono=True,samplerate=sr,attack_seconds=.015,release_seconds=.18,gain=float(gain),peak_dbfs=float(20*np.log10(max(float(abs(x).max()),1e-12))),duration_seconds=len(x)/sr,sha256=hashlib.sha256(out.read_bytes()).hexdigest(),audition='pending; never played on user speakers',validation='finite samples, no clipping, zero fade endpoints')
assert np.isfinite(x).all() and abs(x).max()<1 and x[0]==0 and x[-1]==0
(repo/'art-source/prologue/dog-bark-edit.json').write_text(json.dumps(entry,indent=2)+'\n',encoding='utf-8');print(json.dumps(entry,indent=2))
