"""Actual Windows story flow QA; camera is staged and holds skipped by Claude's autopilot.
No claim of walking routes, complete art, manual UI, or audio acceptance.
"""
from pathlib import Path
import argparse,subprocess,json,datetime,hashlib,re
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--build-label',required=True);p.add_argument('--chapter',default='Prologue',choices=['Prologue','Evening','Night1','Night2','Finale']);a=p.parse_args()
for value in (a.label,a.build_label):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',value):raise SystemExit('Simple labels only')
project=Path(__file__).resolve().parents[2]/'unity-game';build=project/'QA'/a.build_label
assert json.loads((build/'exit.json').read_text())['exit_code']==0
exe=project/'Builds/ScenarioCore/Ataka-Loop.exe';out=project/'QA'/a.label;out.mkdir(parents=True,exist_ok=False)
cmd=[str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-audio-capture',str(out/'silent-mix.wav'),'-logFile',str(out/'player.log'),'-story-smoke',str(out),'-story-from',a.chapter]
info={'command':cmd,'build_label':a.build_label,'chapter':a.chapter,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'runtime_sha256':hashlib.sha256((exe.parent/'Ataka-Loop_Data/Managed/Ataka.Runtime.dll').read_bytes()).hexdigest(),'scope':'Authored StoryAutopilot stages camera at actions, skips holds, accelerates nights. Checks story flow; not walking route/art/manual menu acceptance.'}
with (out/'console.log').open('w',encoding='utf-8') as log:
 child=subprocess.Popen(cmd,cwd=exe.parent,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW|subprocess.BELOW_NORMAL_PRIORITY_CLASS)
 info['pid']=child.pid;(out/'process.json').write_bytes(json.dumps(info,indent=2).encode());print('STORY_PLAYER_PID',child.pid,flush=True)
 try:code=child.wait(timeout=2700)
 except subprocess.TimeoutExpired:child.terminate();child.wait(timeout=30);code=124
 (out/'exit.json').write_text(json.dumps({'pid':child.pid,'exit_code':code}),encoding='utf-8');print('STORY_PLAYER_EXIT',code,flush=True)
 if code==0:assert json.loads((out/'result.json').read_text())['passed']
 raise SystemExit(code)
