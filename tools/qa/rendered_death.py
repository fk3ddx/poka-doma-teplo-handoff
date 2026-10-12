"""Rendered death composition QA; silent capture, no OS input or system volume changes."""
from pathlib import Path
import argparse,subprocess,json,datetime,hashlib,re
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--build-label',required=True);p.add_argument('--torch',choices=['on','off'],default='on');a=p.parse_args()
for value in (a.label,a.build_label):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',value):raise SystemExit('Simple labels only')
project=Path(__file__).resolve().parents[2]/'unity-game';build=project/'QA'/a.build_label
assert json.loads((build/'exit.json').read_text())['exit_code']==0
exe=project/'Builds/ScenarioCore/Ataka-Loop.exe';out=project/'QA'/a.label;out.mkdir(parents=True,exist_ok=False)
cmd=[str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-audio-capture',str(out/'silent-mix.wav'),'-logFile',str(out/'player.log'),'-death-qa',str(out),'-death-qa-torch',a.torch]
info={'command':cmd,'build_label':a.build_label,'torch':a.torch,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'runtime_sha256':hashlib.sha256((exe.parent/'Ataka-Loop_Data/Managed/Ataka.Runtime.dll').read_bytes()).hexdigest(),'scope':'Actual rendered Windows frames: full/short deaths at 0 and +/-60 pitch in hall/living, then the real first-night sequence. Not human art, scare or audio acceptance.'}
with (out/'console.log').open('w',encoding='utf-8') as log:
 child=subprocess.Popen(cmd,cwd=exe.parent,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW|subprocess.BELOW_NORMAL_PRIORITY_CLASS)
 info['pid']=child.pid;(out/'process.json').write_text(json.dumps(info,indent=2),encoding='utf-8');print('DEATH_PLAYER_PID',child.pid,flush=True)
 try:code=child.wait(timeout=480)
 except subprocess.TimeoutExpired:child.terminate();child.wait(timeout=30);code=124
 (out/'exit.json').write_text(json.dumps({'pid':child.pid,'exit_code':code}),encoding='utf-8')
 if code==0:
  checks=(out/'checks.txt').read_text(encoding='utf-8-sig');shots=list(out.glob('Death*.png'))
  assert 'FAIL ' not in checks and 'ERROR ' not in checks,checks
  assert len(shots)==145,len(shots)
  print('DEATH_FRAMES',len(shots),flush=True)
 print('DEATH_PLAYER_EXIT',code,flush=True);raise SystemExit(code)
