"""Exercise real story/loop door states with rendered Windows player and silent capture."""
from pathlib import Path
import argparse,subprocess,json,datetime,hashlib,re
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--build-label',required=True);a=p.parse_args()
for v in (a.label,a.build_label):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',v):raise SystemExit('Simple labels only')
r=Path(__file__).resolve().parents[2]/'unity-game';build=r/'QA'/a.build_label
assert json.loads((build/'exit.json').read_text())['exit_code']==0
exe=r/'Builds/ScenarioCore/Ataka-Loop.exe';out=r/'QA'/a.label;out.mkdir(parents=True,exist_ok=False)
cmd=[str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-audio-capture',str(out/'silent-mix.wav'),'-logFile',str(out/'player.log'),'-door-qa',str(out)]
info={'command':cmd,'build_label':a.build_label,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'runtime_sha256':hashlib.sha256((exe.parent/'Ataka-Loop_Data/Managed/Ataka.Runtime.dll').read_bytes()).hexdigest(),'scope':'Actual scene door transitions and collision routes; not a human walkthrough.'}
with (out/'console.log').open('w',encoding='utf-8') as log:
 child=subprocess.Popen(cmd,cwd=exe.parent,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW|subprocess.BELOW_NORMAL_PRIORITY_CLASS)
 info['pid']=child.pid;(out/'process.json').write_text(json.dumps(info,indent=2),encoding='utf-8');print('DOOR_PLAYER_PID',child.pid,flush=True)
 try:code=child.wait(timeout=120)
 except subprocess.TimeoutExpired:child.terminate();child.wait(timeout=30);code=124
 (out/'exit.json').write_text(json.dumps({'pid':child.pid,'exit_code':code}),encoding='utf-8')
 if code==0:
  checks=(out/'checks.txt').read_text(encoding='utf-8-sig');assert 'FAIL ' not in checks and 'ERROR ' not in checks,checks
 print('DOOR_PLAYER_EXIT',code,flush=True);raise SystemExit(code)
