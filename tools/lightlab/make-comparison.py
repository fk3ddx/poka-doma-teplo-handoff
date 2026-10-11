from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
r=Path(__file__).resolve().parents[2];run=r/'Screenshots/step1/final';refs=r/'tools/look-check/refs'
pairs=[('view1-light2.png','carson-1.jpg','Торшер: тёплый островок, холодные силуэты'),('view4-light1.png','home-5.jpg','Проём и коридор: свет из соседней комнаты'),('view1-light3.png','home-1.jpg','Блэкаут: читаемость форм в темноте'),('profile-tension.png','norwood-7.jpg','Тревога: растекание цвета и двоение'),('view1-light1.png','ironbark-6.jpg','Тёплый тон и мягкость: разные помещения / локации')]
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22);small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
w=1920;row=608;out=Image.new('RGB',(w,100+row*len(pairs)),(17,20,26));d=ImageDraw.Draw(out)
d.text((24,14),'LightLab 0.1.0 — проверка света и VHS; слева наша сцена, справа официальный FtF',font=font,fill='white')
d.text((24,52),'Только одинаковое уменьшение до 960×540. Яркость, цвет и резкость скриншотов не изменены. Геометрия — тестовая.',font=small,fill=(180,190,200))
for i,(ours,ref,title) in enumerate(pairs):
 y=100+row*i;d.text((24,y+5),title,font=font,fill='white')
 for x,p in [(0,run/ours),(960,refs/ref)]:
  im=Image.open(p).convert('RGB');im=im.resize((960,540),Image.Resampling.LANCZOS);out.paste(im,(x,y+36))
 d.text((20,y+581),'LightLab · '+ours,font=small,fill=(180,190,200));d.text((980,y+581),'Fears to Fathom · '+ref,font=small,fill=(180,190,200))
out.save(r/'Screenshots/step1/LightLab-vs-FtF.jpg',quality=95,subsampling=0)
(r/'Screenshots/step1/comparison-sources.json').write_text(json.dumps({'pairs':pairs,'onlyTransform':'same 50% resize and labels, original PNGs used for all metrics','lookCheckUnmodified':True},ensure_ascii=False,indent=2),encoding='utf-8')
print('COMPARISON_READY')
