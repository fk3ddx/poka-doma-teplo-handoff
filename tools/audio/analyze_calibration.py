"""Measure captured fixed-distance Unity audio, without normalising the output."""
import argparse,sys,json,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('capture');p.add_argument('--library-dir',required=True);a=p.parse_args();sys.path.insert(0,a.library_dir)
import soundfile as sf,numpy as np
path=Path(a.capture);info=json.loads(path.with_suffix('.json').read_text());x,sr=sf.read(path,dtype='float32',always_2d=True)
assert info['droppedSamples']==0 and info['formatChanges']==0 and not info['error']
def db(v):return round(20*math.log10(max(float(v),1e-12)),2)
rows=[]
for marker in info['markers']:
 if not marker['state'].startswith('calibration-'):continue
 name=marker['state'].removeprefix('calibration-');end=next(m for m in info['markers'] if m['state']=='end-'+name)
 z=x[int(marker['seconds']*sr):int(end['seconds']*sr)];hop=int(.1*sr);rms=[np.sqrt(np.mean(z[i:i+hop].astype('float64')**2)) for i in range(0,len(z)-hop,hop)]
 rows.append(dict(cue=name,seconds=len(z)/sr,sample_peak_dbfs=db(np.abs(z).max()),max_100ms_rms_dbfs=db(max(rms)),rms_dbfs=db(np.sqrt(np.mean(z.astype('float64')**2))),samples_over_full_scale=int(np.sum(np.abs(z)>=1))))
result=dict(scope='1.5 m, no world ambience or occlusion; receiver effects 2D; stereo energy 100ms RMS, not LUFS/true peak',rows=rows,diagnostics=info.get('cues',[]))
path.with_name(path.stem+'-levels.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
for r in rows:print(r['cue'],r['sample_peak_dbfs'],r['max_100ms_rms_dbfs'])