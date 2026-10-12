"""Archive a verified Windows build without overwriting previous deliveries."""
from pathlib import Path
import argparse,json,subprocess,hashlib,zipfile,re,datetime
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--build-label',required=True);p.add_argument('--notes',required=True);a=p.parse_args()
for v in (a.label,a.build_label):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',v):raise SystemExit('Simple labels only')
r=Path(__file__).resolve().parents[2];project=r/'unity-game';qa=project/'QA'/a.build_label
assert json.loads((qa/'exit.json').read_text())['exit_code']==0
build=project/'Builds/ScenarioCore';assert (build/'Ataka-Loop.exe').is_file()
notes=Path(a.notes).resolve();assert notes.is_file()
archive=r/'builds'/('Ataka-'+a.label+'-win64.zip');archive.parent.mkdir(exist_ok=True)
assert not archive.exists(),'Never overwrite a previous delivery'
files=[f for f in build.rglob('*') if f.is_file() and not any('DoNotShip' in part for part in f.parts)]
extra={n:r/n for n in ('CREDITS.md','CREDITS-STORY-PROPS.md','CREDITS-GUEST.md','CREDITS-AUDIO.md')}
extra['README-RU.md']=notes;extra['docs/CREDITS-M1-HISTORY.md']=project/'CREDITS.md'
for f in extra.values():assert f.is_file(),f
manifest={'label':a.label,'build_label':a.build_label,'source_head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=r,text=True).strip(),'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'files':{}}
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for f in files:
  name=f.relative_to(build).as_posix();data=f.read_bytes();z.writestr(name,data);manifest['files'][name]=hashlib.sha256(data).hexdigest()
 for name,f in extra.items():z.write(f,name)
 z.writestr('BUILD-MANIFEST.json',json.dumps(manifest,indent=2))
with zipfile.ZipFile(archive) as z:assert z.testzip() is None
sha=hashlib.sha256(archive.read_bytes()).hexdigest();archive.with_suffix('.zip.sha256').write_text(sha+'  '+archive.name+'\n',encoding='utf-8')
print(json.dumps({'archive':str(archive),'bytes':archive.stat().st_size,'sha256':sha,'runtime_files':len(files),'crc':'passed'}))
