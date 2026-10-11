from pathlib import Path
import subprocess,sys,json,time
repo=Path(__file__).resolve().parents[2];p=repo/'unity-game';run=sys.argv[1] if len(sys.argv)>1 else 'pass1'
out=repo/'Screenshots/step1'/run;out.mkdir(parents=True,exist_ok=True)
args=[str(p/'Builds/LightLab011/LightLab.exe'),'-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-lightlab-capture',str(out),'-logFile',str(p/'Logs'/('lightlab-'+run+'.log'))]
if len(sys.argv)>2 and sys.argv[2]=='baseline':args+=['-lab-baseline']
start=time.time();r=subprocess.run(args,timeout=180)
(out/'process.json').write_text(json.dumps({'exitCode':r.returncode,'seconds':time.time()-start},indent=2))
print('LIGHTLAB_CAPTURE_EXIT',r.returncode,flush=True)
if r.returncode:raise SystemExit(r.returncode)
files=sorted(str(x) for x in out.glob('view*-light*.png'))
if len(files)!=18:raise RuntimeError('Expected 18 unmodified native captures, got '+str(len(files)))
with (out/'look-check.txt').open('w',encoding='utf-8') as log:
 subprocess.run(['node',str(repo/'tools/look-check/look-check.js'),*files],stdout=log,check=True)
print((out/'look-check.txt').read_text(encoding='utf-8'),flush=True)

