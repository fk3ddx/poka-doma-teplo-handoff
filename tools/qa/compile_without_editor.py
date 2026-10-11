"""Compile against the last real Unity response file, without starting Unity or a player.
Not a Unity import/build/test run. Never overwrite Bee/Library outputs.
"""
from pathlib import Path
import argparse, subprocess, hashlib, json, re, datetime
parser=argparse.ArgumentParser();parser.add_argument('--assembly',default='Ataka.Runtime');parser.add_argument('--use-local-runtime',action='store_true');args=parser.parse_args()
repo=Path(__file__).resolve().parents[2];project=repo/'unity-game'
editor=Path('C:/Users/User/UnityEditors/6000.3.26f1/Editor/Data')
source=project/'Library/Bee/artifacts/1900b0aPDevDbg.dag'/f'{args.assembly}.rsp'
if not source.exists(): source=project/'Library/Bee/artifacts/1900b0aE.dag'/f'{args.assembly}.rsp'
assert source.is_file(), 'A real prior Unity response file is required'
out=project/'QA/NoWindowCompile'/args.assembly;out.mkdir(parents=True,exist_ok=True)
lines=source.read_text(encoding='utf-8-sig').splitlines();sources=[];changed=[]
local_runtime=None
if args.use_local_runtime:
 assert args.assembly!='Ataka.Runtime'
 runtime=project/'QA/NoWindowCompile/Ataka.Runtime';report=json.loads((runtime/'result.json').read_text(encoding='utf-8'))
 assert report['exit_code']==0, 'Compile Runtime successfully first'
 for item in report['sources']:
  assert hashlib.sha256((project/item['path']).read_bytes()).hexdigest()==item['sha256'], 'Runtime source changed; recompile first'
 local_runtime=runtime/'Ataka.Runtime.ref.dll';assert local_runtime.exists()

for line in lines:
 if local_runtime and line.startswith('-r:') and line.endswith('/Ataka.Runtime.ref.dll\"'): line='-r:\"'+local_runtime.as_posix()+'\"'
 if line.startswith('-out:'): line='-out:"'+(out/(args.assembly+'.dll')).as_posix()+'"'
 elif line.startswith('-refout:'): line='-refout:"'+(out/(args.assembly+'.ref.dll')).as_posix()+'"'
 if line.startswith('"Assets/') and line.endswith('.cs"'):
  file=project/line.strip('"');sources.append({'path':file.relative_to(project).as_posix(),'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
 changed.append(line)
rsp=out/'compile.rsp';rsp.write_text('\n'.join(changed)+'\n',encoding='utf-8')
command=[str(editor/'NetCoreRuntime/dotnet.exe'),str(editor/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)]
result=subprocess.run(command,cwd=project,capture_output=True,text=True,encoding='utf-8',errors='replace',creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
(out/'compiler.log').write_text(result.stdout+result.stderr,encoding='utf-8')
report={'scope':'C# compile only; no Unity import, editor, game window, visual or gameplay test','source_response_file':str(source),'source_response_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'exit_code':result.returncode,'local_runtime_reference':str(local_runtime) if local_runtime else None,'sources':sources,'utc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
(out/'result.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(f'{args.assembly}: exit={result.returncode}, sources={len(sources)}; {out}/compiler.log')
print(result.stdout+result.stderr)
raise SystemExit(result.returncode)
