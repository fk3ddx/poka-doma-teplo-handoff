"""Download public CC0 listening previews, keeping provenance; originals remain separate."""
import urllib.request,re,json,html,time,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'art-sources/Audio/Freesound';root.mkdir(parents=True,exist_ok=True)
items=[('phone','mycompasstv',456433),('clock','Vrymaa',797574),('chime','claudius9uk',96717),('guest-breath','mucky_pete7',574208),('player-breath','ShaneF91',386573),('knock','775noise',494569),('door','BlondPanda',778462),('slippers','Yuval',208878)]
items += [('shriek','DigestContent',449702),('palms','fordps3',262588),('heartbeat','loudernoises',332821),('wind','poots',627793),('rope','harveyism',130364),('scratch','nickyg11p',390869),('fridge','RCSound99',651210)]
existing=json.loads((root/'manifest.json').read_text(encoding='utf-8')) if (root/'manifest.json').exists() else []
results=[]
for role,user,ident in items:
    cached=next((e for e in existing if e.get('id')==ident and e.get('file') and (root/e['file']).exists()),None)
    if cached: results.append(cached); continue
    url=f'https://freesound.org/people/{user}/sounds/{ident}/'
    try:
        req=urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'})
        with urllib.request.urlopen(req,timeout=30) as r: page=r.read().decode('utf-8')
        if not re.search(r'creativecommons.org/publicdomain/zero/1.0',page): raise ValueError('CC0 license not confirmed on page')
        candidates=sorted(set(html.unescape(x) for x in re.findall(r'https?://[^\s\"\'<>]+\.(?:mp3|ogg)',page)))
        candidates=[x for x in candidates if 'cdn.freesound.org/previews/' in x]
        candidates.sort(key=lambda x:('-hq.' not in x,not x.endswith('.mp3')))
        if not candidates: raise ValueError('No public preview URL found')
        media=candidates[0];dest=root/f'{role}-{ident}-preview{Path(media).suffix}'
        with urllib.request.urlopen(urllib.request.Request(media,headers={'User-Agent':'Mozilla/5.0'}),timeout=45) as r: payload=r.read()
        dest.write_bytes(payload)
        title=re.search(r'<title>(.*?)</title>',page,re.S)
        info=dict(role=role,id=ident,author=user,page=url,title=html.unescape(title.group(1)).strip() if title else role,license='CC0-1.0',license_url='https://creativecommons.org/publicdomain/zero/1.0/',checked='2026-10-11',source_kind='public listening preview, NOT original download',media_url=media,file=dest.name,bytes=len(payload),sha256=hashlib.sha256(payload).hexdigest(),listening_review='pending')
        results.append(info);print(role,ident,len(payload),'CC0 verified',flush=True)
    except Exception as e:
        results.append(dict(role=role,id=ident,page=url,error=str(e)));print(role,'ERROR',str(e),flush=True)
    time.sleep(2)
(root/'manifest.json').write_bytes((json.dumps(results,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))