"""Run an existing QA Windows build without graphics, audible output or OS input.
Requires ScenarioSmoke's -scenario-headless guard. No screenshots/visual acceptance.
"""
from pathlib import Path
import argparse, subprocess, json, hashlib, datetime, re
parser=argparse.ArgumentParser()
parser.add_argument('--label',required=True)
parser.add_argument('--build-label',required=True)
parser.add_argument('--mode',choices=['full','door','chase'],default='full')
args=parser.parse_args()
for label in (args.label,args.build_label):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',label):raise SystemExit('Use simple evidence labels')
project=Path(__file__).resolve().parents[2]/'unity-game'
build=project/'QA'/args.build_label
if json.loads((build/'exit.json').read_text())['exit_code']!=0:raise SystemExit('Build did not complete successfully')
if json.loads((build/'process.json').read_text())['operation']!='core-build':raise SystemExit('Requires explicit core-build evidence')
exe=project/'Builds/ScenarioCore/Ataka-Loop.exe'
if not exe.is_file():raise SystemExit('Player is missing')
out=project/'QA'/args.label
if out.exists():raise SystemExit('Use a fresh evidence directory')
out.mkdir(parents=True)
cmd=[str(exe),'-batchmode','-nographics','-silent-crashes','-job-worker-count','2','-scenario-headless','-scenario-smoke',str(out),'-logFile',str(out/'player.log')]
if args.mode=='door':cmd+=['-scenario-door-probe']
if args.mode=='chase':cmd+=['-scenario-chase-only']
if args.mode in ('full','chase'):cmd+=['-scenario-death-skip']
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
with (out/'console.log').open('w',encoding='utf-8') as log:
 child=subprocess.Popen(cmd,cwd=exe.parent,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,startupinfo=startup,creationflags=subprocess.CREATE_NO_WINDOW|subprocess.BELOW_NORMAL_PRIORITY_CLASS)
 info={'pid':child.pid,'command':cmd,'build_label':args.build_label,'exe_sha256':hashlib.sha256(exe.read_bytes()).hexdigest(),'runtime_sha256':hashlib.sha256((exe.parent/'Ataka-Loop_Data/Managed/Ataka.Runtime.dll').read_bytes()).hexdigest(),'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'Runtime mechanics, synthetic in-process input, muted listener, Null graphics. No visual or audible acceptance.'}
 (out/'process.json').write_bytes((json.dumps(info,indent=2)+'\n').encode())
 print('HEADLESS_PLAYER_PID',child.pid,flush=True)
 try:code=child.wait(timeout=600)
 except subprocess.TimeoutExpired:
  # Only our own process, never an existing game or editor.
  child.terminate();child.wait(timeout=30);code=124
 (out/'exit.json').write_bytes((json.dumps({'pid':child.pid,'exit_code':code})+'\n').encode())
 print('HEADLESS_PLAYER_EXIT',code,flush=True)
 if code==0:
  result=json.loads((out/'result.json').read_text())
  if args.mode=='full' and not result.get('coreFinished'):raise SystemExit('Full route did not finish the core')
  if not (result.get('headless') and result.get('graphicsDevice')=='Null' and result.get('listenerVolume')==0 and result.get('passed')):raise SystemExit('Runtime report did not confirm the required silent mode and checks')
 raise SystemExit(code)
