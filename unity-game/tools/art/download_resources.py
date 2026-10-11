import hashlib, json, pathlib, urllib.request
ROOT = pathlib.Path(__file__).resolve().parents[2]
DEST = ROOT / 'Assets/_Project/Art/CC0'
SELECTION = {
 'asphalt_02': ('Diffuse','nor_gl','Rough'),
 'asphalt_snow': ('Diffuse','nor_gl','Rough'),
 'clay_plaster': ('Diffuse','nor_gl','Rough'),
 'covered_car': ('Diffuse','nor_gl','Rough','fbx'),
 'painted_wooden_bench': ('Diffuse','nor_gl','Rough','fbx'),
 'dark_wood': ('Diffuse', 'nor_gl', 'Rough', 'AO'),
 'herringbone_parquet': ('Diffuse', 'nor_gl', 'Rough', 'AO'),
 'quatrefoil_jacquard_fabric': ('Diffuse', 'nor_gl', 'Rough'),
 'plastered_wall_02': ('Diffuse', 'nor_gl', 'Rough'),
 'mid_century_lounge_chair': ('Diffuse', 'nor_gl', 'Rough', 'fbx'),
 'book_encyclopedia_set_01': ('Diffuse', 'nor_gl', 'Rough', 'fbx'),
 'ceramic_vase_01': ('Diffuse', 'nor_gl', 'Rough', 'fbx'),
}
def request(url):
 with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent':'Ataka-M1-AssetResearch/1.0'}), timeout=120) as r: return r.read()
manifest=[]
for asset, maps in SELECTION.items():
 data=json.loads(request('https://api.polyhaven.com/files/'+asset))
 dest=DEST/asset; dest.mkdir(parents=True,exist_ok=True)
 for kind in maps:
  if kind not in data:
   print('Missing optional',asset,kind,flush=True); continue
  resolutions=data[kind]; res='2k' if '2k' in resolutions else next(iter(resolutions))
  formats=resolutions[res]; fmt='fbx' if kind=='fbx' else 'jpg'
  if fmt not in formats: fmt=next(iter(formats))
  f=formats[fmt]; target=dest/(kind+'.'+fmt)
  payload=target.read_bytes() if target.exists() else request(f['url'])
  if hashlib.md5(payload).hexdigest()!=f['md5']: raise RuntimeError('Checksum mismatch '+asset+'/'+kind)
  target.write_bytes(payload)
  manifest.append({'asset':asset,'map':kind,'source':f['url'],'md5':f['md5'],'license':'CC0-1.0','path':target.relative_to(ROOT).as_posix()})
  print(asset,kind,len(payload),flush=True)
(DEST/'provenance.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
