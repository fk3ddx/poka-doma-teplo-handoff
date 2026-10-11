from pathlib import Path
import json, urllib.request, hashlib
root=Path(__file__).resolve().parents[2]
out=root/'Assets/_Project/Art/OpenAccessMet';out.mkdir(parents=True,exist_ok=True)
for object_id in [831188,205079]:
 request=urllib.request.Request(f'https://collectionapi.metmuseum.org/public/collection/v1/objects/{object_id}',headers={'User-Agent':'Ataka-M1-AssetResearch/1.0'})
 with urllib.request.urlopen(request,timeout=60) as response:data=json.load(response)
 assert data['isPublicDomain'], 'Only Public Domain images are permitted'
 with urllib.request.urlopen(data['primaryImageSmall'],timeout=60) as response:payload=response.read()
 (out/f'Icon{object_id}.jpg').write_bytes(payload)
 metadata={key:data.get(key) for key in ['objectID','title','artistDisplayName','objectDate','objectURL','isPublicDomain','primaryImageSmall','creditLine']}
 metadata['sha256']=hashlib.sha256(payload).hexdigest()
 (out/f'Icon{object_id}.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2),encoding='utf8')
 print(object_id,len(payload))
