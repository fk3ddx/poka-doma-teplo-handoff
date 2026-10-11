"""Compare opt-in Unity listener captures; no LUFS/true-peak claims."""
import argparse,json,math,sys,hashlib
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--library-dir',required=True);p.add_argument('--versions',nargs='+',default=['before','after-v1']);p.add_argument('--report',default='comparison');p.add_argument('--clips',nargs='*');a=p.parse_args();sys.path.insert(0,a.library_dir)
import soundfile as sf
import numpy as np
repo=Path(__file__).resolve().parents[2];root=repo/'QA/Audio';out=root/'comparison';out.mkdir(exist_ok=True)
def db(v):return round(20*math.log10(max(float(v),1e-12)),2)
def metrics(x):
 return dict(sample_peak_dbfs=db(np.max(np.abs(x))),rms_dbfs=db(np.sqrt(np.mean(x.astype('float64')**2))),samples_at_or_above_full_scale=int(np.count_nonzero(np.abs(x)>=1)),nonzero_samples=int(np.count_nonzero(x)))
report={'scope':'Unity listener mix before OS/device, sample peak and RMS only. Not LUFS, not true peak. No listening acceptance.', 'versions':{}}
for version in a.versions:
 folder=root/version;info=json.loads((folder/'full-mix.json').read_text());x,sr=sf.read(folder/'full-mix.wav',dtype='float32',always_2d=True)
 if info['droppedSamples'] or info['formatChanges'] or info['error']:raise ValueError('Invalid capture '+version)
 if not np.isfinite(x).all() or not np.count_nonzero(x):raise ValueError('Silent/nonfinite capture '+version)
 markers=info['markers'];first=lambda name:next(m['seconds'] for m in markers if m['state']==name)
 selections=[('quiet',first('lap-1'),8),('under-bed',first('UnderBed')-2,6),('phone',first('lap-5'),18),('hanging',first('HangingVision')-1,9),('death',first('DeathShort')-.5,5),('chase-end',first('finished')-20,20)]
 result=dict(duration_seconds=len(x)/sr,sample_rate=sr,capture=info,metrics=metrics(x),sha256=hashlib.sha256((folder/'full-mix.wav').read_bytes()).hexdigest(),clips={})
 for name,start,duration in selections:
  if a.clips and name not in a.clips:continue
  clip=x[max(0,int(start*sr)):min(len(x),int((start+duration)*sr))];path=out/(version+'-'+name+'.wav');sf.write(path,clip,sr,subtype='PCM_16')
  result['clips'][name]=dict(start_seconds=start,duration_seconds=len(clip)/sr,metrics=metrics(clip),file=path.relative_to(repo).as_posix())
 report['versions'][version]=result
(root/(a.report+'.json')).write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps({k:v['metrics'] for k,v in report['versions'].items()},indent=2))