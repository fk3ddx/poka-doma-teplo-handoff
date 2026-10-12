"""Explicit visible Windows QA, no OS input, audio captured then cleared by QA listener.
Only run when the user allows game windows. Never changes system volume.
"""
from pathlib import Path
import argparse,subprocess,json,datetime,hashlib,re
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--build-label',required=True);p.add_argument('--mode',choices=['survey','bed-probe','full','integrated','door','hall','light-probe'],required=True);a=p.parse_args()
for label in (a.label,a.build_label):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',label):raise SystemExit('Simple evidence labels only')
project=Path(__file__).resolve().parents[2]/'unity-game';build=project/'QA'/a.build_label
if json.loads((build/'exit.json').read_text())['exit_code']!=0:raise SystemExit('Build failed')
exe=project/'Builds/ScenarioCore/Ataka-Loop.exe';out=project/'QA'/a.label
out.mkdir(parents=True,exist_ok=False)
cmd=[str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-audio-capture',str(out/'silent-mix.wav'),'-logFile',str(out/'player.log')]
if a.mode in ('survey','bed-probe'):
 cmd+=['-scenario-art-survey',str(out)]
 if a.mode=='bed-probe':cmd+=['-scenario-bed-probe']
else:
 cmd+=['-scenario-smoke',str(out),'-scenario-death-skip']
 if a.mode=='integrated':cmd+=['-scenario-integrated-finale']
 if a.mode=='door':cmd+=['-scenario-door-probe']
 if a.mode=='hall':cmd+=['-scenario-hall-probe']
 if a.mode=='light-probe':cmd+=['-scenario-hall-light-probe']
info={'command':cmd,'build_label':a.build_label,'mode':a.mode,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'runtime_sha256':hashlib.sha256((exe.parent/'Ataka-Loop_Data/Managed/Ataka.Runtime.dll').read_bytes()).hexdigest(),'scope':'Actual Windows graphics; survey modes stage cameras and are not proof of gameplay. QA audio capture clears output; no listening acceptance.'}
with (out/'console.log').open('w',encoding='utf-8') as log:
 child=subprocess.Popen(cmd,cwd=exe.parent,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW|subprocess.BELOW_NORMAL_PRIORITY_CLASS)
 info['pid']=child.pid;(out/'process.json').write_text(json.dumps(info,indent=2),encoding='utf-8');print('RENDERED_PLAYER_PID',child.pid,flush=True)
 try:code=child.wait(timeout=1000 if a.mode=='integrated' else 600)
 except subprocess.TimeoutExpired:child.terminate();child.wait(timeout=30);code=124
 (out/'exit.json').write_text(json.dumps({'pid':child.pid,'exit_code':code}),encoding='utf-8');print('RENDERED_PLAYER_EXIT',code,flush=True)
 if code==0:
  if a.mode in ('survey','bed-probe'):
   report=json.loads((out/'survey.json').read_text());expected=15 if a.mode=='bed-probe' else 50
   assert len(report['views'])==expected
   assert all((out/v['file']).is_file() for v in report['views'])
  else:
   result=json.loads((out/('core-result.json' if a.mode=='integrated' else 'result.json')).read_text());assert result['passed'] and result['graphicsDevice']!='Null'
   if a.mode=='integrated':
    from verify_integrated import verify_integrated
    (out/'validation.json').write_text(json.dumps(verify_integrated(out),indent=2),encoding='utf-8')
   if a.mode=='full':assert result['coreFinished']
 raise SystemExit(code)
