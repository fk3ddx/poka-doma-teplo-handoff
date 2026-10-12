"""Save/Continue across a FULL restart of the Windows player (SaveResumeQA), one case per call.
Process 1 stages the case and writes the checkpoints with the game's own methods, then quits; process 2 is a NEW
process that opens on the menu and presses the menu's Continue (LoopDirector.ContinueFromSave). Both processes get the
same -qa-save-dir inside the evidence folder, so the player's own saves (persistentDataPath) are never read or touched.
Only run when the user allows game windows. Never changes system volume.
"""
from pathlib import Path
import argparse,subprocess,json,datetime,re,hashlib
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--build-label',required=True);p.add_argument('--case',choices=['lap2','chase','finale','reject'],required=True);a=p.parse_args()
for label in (a.label,a.build_label):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',label):raise SystemExit('Simple evidence labels only')
project=Path(__file__).resolve().parents[2]/'unity-game';build=project/'QA'/a.build_label
if json.loads((build/'exit.json').read_text())['exit_code']!=0:raise SystemExit('Build failed')
exe=project/'Builds/ScenarioCore/Ataka-Loop.exe';out=project/'QA'/a.label;out.mkdir(parents=True,exist_ok=False)
saves=out/'qa-saves';saves.mkdir()
runtime_sha256=hashlib.sha256((exe.parent/'Ataka-Loop_Data/Managed/Ataka.Runtime.dll').read_bytes()).hexdigest()
def run(phase,timeout,save_dir=None):
 log=out/f'{phase}-player.log'
 cmd=[str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-audio-capture',str(out/f'{phase}-silent-mix.wav'),'-logFile',str(log),
      '-qa-save-dir',str(saves) if save_dir is None else save_dir,'-save-resume-qa',str(out),'-save-resume-case','lap2' if a.case=='reject' else a.case,'-save-resume-phase',phase,'-scenario-death-skip']
 with (out/f'{phase}-console.log').open('w',encoding='utf-8') as c:
  child=subprocess.Popen(cmd,cwd=exe.parent,stdin=subprocess.DEVNULL,stdout=c,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW|subprocess.BELOW_NORMAL_PRIORITY_CLASS)
  info={'pid':child.pid,'command':cmd,'runtime_sha256':runtime_sha256,'build_label':a.build_label,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
  (out/f'{phase}-process.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
  print('SAVE_RESUME',phase,'PID',child.pid,flush=True)
  try:code=child.wait(timeout=timeout)
  except subprocess.TimeoutExpired:child.terminate();child.wait(timeout=30);code=124
 (out/f'{phase}-exit.json').write_text(json.dumps({'pid':child.pid,'exit_code':code,'command':cmd,'utc':datetime.datetime.utcnow().isoformat()+'Z'}),encoding='utf-8')
 return code
def sha(name):
 f=saves/name;return hashlib.sha256(f.read_bytes()).hexdigest() if f.exists() else '(none)'
STORY,LOOP='story-checkpoint.json','scenario-core-checkpoint.json'
if a.case=='reject':
 # an empty -qa-save-dir must fail at once, with nothing written anywhere (no fallback to the player's folder)
 code=run('stage',60,save_dir='');r=json.loads((out/'stage/result.json').read_text(encoding='utf-8-sig'))
 results={'exit_nonzero':code!=0,'reported':not r.get('passed') and r.get('saveDirectory') in (None,''),'qa_folder_empty':not any(saves.iterdir())}
 (out/'save-resume.json').write_text(json.dumps({'case':'reject','exit':code,'results':results},indent=2),encoding='utf-8');print('SAVE_RESUME_RESULT',json.dumps(results),flush=True)
 raise SystemExit(0 if all(results.values()) else 1)
staged=run('stage',120)
# what the writer left, hashed BEFORE the reader process exists (the reader records its own boot / pre-Continue hashes)
before_reader={'files':sorted(f.name for f in saves.iterdir()),STORY:sha(STORY),LOOP:sha(LOOP)}
(out/'staged-files.json').write_text(json.dumps(before_reader,indent=2),encoding='utf-8')
if staged!=0:raise SystemExit(f'stage exit {staged}')
resumed=run('resume',1000 if a.case!='lap2' else 400)
def mix_ok(phase):
 f=out/f'{phase}-silent-mix.json'
 if not f.exists():return False
 m=json.loads(f.read_text(encoding='utf-8-sig'))
 return bool(m.get('captureModeMutesOutput') and m.get('clearedOutputCallbacks',0)>0 and m.get('samples',0)>0 and not m.get('error') and m.get('droppedSamples')==0 and m.get('formatChanges')==0)

def ok(path):
 if not path.exists():return False
 r=json.loads(path.read_text(encoding='utf-8-sig'));return r.get('passed') and r.get('errors',0)==0
results={'stage':ok(out/'stage/result.json'),'resume':ok(out/'resume/result.json')}
results['silent_capture']=mix_ok('stage') and mix_ok('resume')
results['rendered_evidence']=len(list((out/'stage').glob('*.png')))>=1 and len(list((out/'resume').rglob('*.png')))>=3
reader=json.loads((out/'resume/result.json').read_text(encoding='utf-8-sig')) if (out/'resume/result.json').exists() else {}
# the reader saw exactly the writer's files at boot and still right before Continue (no overwrite on boot)
results['unchanged_until_continue']=all(reader.get(k)==before_reader[f] for k,f in (('storyHashBoot',STORY),('loopHashBoot',LOOP),('storyHashBeforeContinue',STORY),('loopHashBeforeContinue',LOOP)))
if a.case=='lap2':results['walk']=ok(out/'resume/walk/result.json')
if a.case=='chase':results['chase']=ok(out/'resume/chase/core-result.json');results['finale']=ok(out/'resume/chase/finale/result.json')
if a.case=='finale':results['finale']=ok(out/'resume/finale/result.json')
f={'chase':out/'resume/chase/finale/result.json','finale':out/'resume/finale/result.json'}.get(a.case)
if f:
 results['epilogue']=f.exists() and bool(json.loads(f.read_text(encoding='utf-8-sig')).get('epilogue'))
 # the story's own end deleted its checkpoint in the QA folder (checked on disk after the reader exited)
 results['story_save_deleted_after_epilogue']=not (saves/STORY).exists() and bool(reader.get('storySaveDeletedAfterEpilogue'))
(out/'save-resume.json').write_text(json.dumps({'case':a.case,'stage_exit':staged,'resume_exit':resumed,'results':results},indent=2),encoding='utf-8')
print('SAVE_RESUME_RESULT',json.dumps(results),flush=True)
raise SystemExit(0 if resumed==0 and all(results.values()) else 1)
