"""Verify runtime cue timing and actual silent audio; not a listening judgement."""
import argparse,json,math,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('capture',type=Path);p.add_argument('--library-dir',required=True);p.add_argument('--allow-skipped-death',action='store_true');a=p.parse_args();sys.path.insert(0,a.library_dir)
import numpy as np,soundfile as sf
info=json.loads(a.capture.with_suffix('.json').read_text(encoding='utf-8'));x,sr=sf.read(a.capture,dtype='float32',always_2d=True);checks=[]
def check(ok,name,evidence):checks.append(dict(passed=bool(ok),name=name,evidence=evidence))
def peak_db(v):return 20*math.log10(max(float(np.max(np.abs(v))),1e-12))
check(not info['error'] and info['droppedSamples']==0 and info['formatChanges']==0,'capture complete',{k:info[k] for k in ['error','droppedSamples','formatChanges']})
check(np.isfinite(x).all() and np.max(np.abs(x))<1,'finite and below digital full scale',peak_db(x))
markers=info['markers']
for scene,events,silence in [
 ('DeathShort',{'breath':0,'finger_crack':.3,'palms':1,'choked_cry':1,'wet_tear':1.5},(2.65,3.25)),
 ('HangingVision',{'breath':3.5,'tinnitus':4.5,'latch':6},(5.15,5.75))]:
 starts=[m for m in markers if m['state']==scene];check(len(starts)>0,scene+' exercised',len(starts))
 for n,start in enumerate(starts):
  end=next((m['seconds'] for m in markers if not m['state'].startswith('cue:') and m['seconds']>start['seconds']),len(x)/sr)
  cues=[m for m in markers if m.get('scene')==scene and start['seconds']-.1<=m['seconds']<end]
  skipped=scene=='DeathShort' and end-start['seconds']<3
  check(not skipped or a.allow_skipped_death,f'{scene} #{n+1} expected completion/skip',dict(duration=end-start['seconds'],skipped=skipped))
  for cue,expected in events.items():
   times=[m['timelineSeconds'] for m in cues if m['state']=='cue:'+cue]
   if skipped and expected>end-start['seconds']+.06:
    check(not times,f'{scene} #{n+1} {cue} not started after skip',times);continue
   check(len(times)==1 and expected-.0001<=times[0]<=expected+.06,f'{scene} #{n+1} {cue} once near {expected}s',times)
  wrong=[m['state'] for m in cues if m['state'] in ('cue:shriek','cue:creak')];check(not wrong,f'{scene} #{n+1} no mismatched shriek/door creak',wrong)
  reference=next((m for m in cues if m['state']==('cue:wet_tear' if scene=='DeathShort' else 'cue:tinnitus')),None)
  if reference and not skipped:
   origin=reference['seconds']-reference['timelineSeconds'];lo,hi=[int((origin+t)*sr) for t in silence];section=x[lo:hi];level=peak_db(section)
   check(level < -80,f'{scene} #{n+1} silence {silence}',dict(sample_peak_dbfs=level,audio_seconds=[lo/sr,hi/sr]))
# Recovery is part of the silent death transition, even after the timeline ends.
recoveries=[m for m in markers if m['state']=='death-recovery']
check(bool(recoveries),'death recovery exercised',len(recoveries))
for n,start in enumerate(recoveries):
 end=next((m['seconds'] for m in markers if not m['state'].startswith('cue:') and m['seconds']>start['seconds']),None)
 check(end is not None and .85<end-start['seconds']<1.2,f'recovery #{n+1} lasts one second',None if end is None else end-start['seconds'])
 if end is not None:
  lo,hi=int((start['seconds']+.15)*sr),int((end-.1)*sr);section=x[lo:hi]
  check(len(section)>0 and peak_db(section)<-80,f'recovery #{n+1} stays silent after timeline exit',dict(peak_dbfs=peak_db(section),audio_seconds=[lo/sr,hi/sr]))
  starts=[m['state'] for m in markers if m['state'].startswith('cue:') and start['seconds']<m['seconds']<end]
  check(not starts,f'recovery #{n+1} no restarted ambient/cues',starts)
outside=[m for m in markers if m['state'].startswith('cue:') and not m.get('scene')]
check(bool(outside) and all(m['timelineSeconds']==-1 for m in outside),'outside-scene cues use -1 time',len(outside))
report=dict(passed=all(c['passed'] for c in checks),scope='Cue scheduling within 60ms and listener-mix sample peak; not listening acceptance or precise DSP onset',checks=checks)
(a.capture.parent/'scene-event-checks.json').write_bytes((json.dumps(report,indent=2)+'\n').encode('utf-8'));print(json.dumps(report,indent=2));sys.exit(0 if report['passed'] else 1)
