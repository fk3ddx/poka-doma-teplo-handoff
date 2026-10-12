"""Rendered SCENARIO 11 face QA with silent audio capture; never alters delivered builds."""
from pathlib import Path
import argparse,subprocess,json,datetime,hashlib,re
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--build-label',required=True);a=p.parse_args()
for v in (a.label,a.build_label):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',v):raise SystemExit('Simple labels only')
project=Path(__file__).resolve().parents[2]/'unity-game';build=project/'QA'/a.build_label
assert json.loads((build/'exit.json').read_text())['exit_code']==0
exe=project/'Builds/ScenarioCore/Ataka-Loop.exe';out=project/'QA'/a.label;out.mkdir(parents=True,exist_ok=False)
cmd=[str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-audio-capture',str(out/'silent-mix.wav'),'-logFile',str(out/'player.log'),'-face-qa',str(out)]
info={'command':cmd,'build_label':a.build_label,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'runtime_sha256':hashlib.sha256((exe.parent/'Ataka-Loop_Data/Managed/Ataka.Runtime.dll').read_bytes()).hexdigest(),'scope':'Real Windows finale QA; camera staged, acts invoked, waits accelerated. ABSENT entries remain unimplemented. Inspect checks and rendered frames; process success alone is not visual or audio acceptance.'}
with (out/'console.log').open('w',encoding='utf-8') as log:
 child=subprocess.Popen(cmd,cwd=exe.parent,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW|subprocess.BELOW_NORMAL_PRIORITY_CLASS)
 info['pid']=child.pid;(out/'process.json').write_text(json.dumps(info,indent=2),encoding='utf-8');print('FACE_PLAYER_PID',child.pid,flush=True)
 try:code=child.wait(timeout=480)
 except subprocess.TimeoutExpired:child.terminate();child.wait(timeout=30);code=124
 (out/'exit.json').write_text(json.dumps({'pid':child.pid,'exit_code':code}),encoding='utf-8')
 if code==0:
  checks=(out/'checks.txt').read_text(encoding='utf-8-sig');assert 'FAIL ' not in checks and 'ERROR ' not in checks,checks
  result=json.loads((out/'result.json').read_text(encoding='utf-8-sig'));assert result['passed'] and result['failures']==0 and result['errors']==0,result
  assert result['standInInstalled'] and not result['leftovers'],result
  assert len(list(out.glob('face-*.png')))>=10 and len(list(out.glob('tear-*.png')))>=4,'Incomplete rendered evidence'
  mix=json.loads((out/'silent-mix.json').read_text(encoding='utf-8-sig'))
  assert mix['captureModeMutesOutput'] and mix['clearedOutputCallbacks']>0,'Silent capture was not confirmed'
  assert not mix['error'] and mix['droppedSamples']==0 and mix['formatChanges']==0,mix
  assert mix['samples']>0,'No captured audio samples'
 print('FACE_PLAYER_EXIT',code,flush=True);raise SystemExit(code)
