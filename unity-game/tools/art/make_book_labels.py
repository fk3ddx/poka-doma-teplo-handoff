from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
r=Path(__file__).resolve().parents[2];dest=r/'Assets/_Project/Art/Textures';dest.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/times.ttf',24)
names=['ЧЕХОВ','БУНИН','ГОГОЛЬ','ПУШКИН','ТУРГЕНЕВ','ЛЕРМОНТОВ','КУПРИН','ГОНЧАРОВ','ЛЕСКОВ','ЧЕХОВ','ПАУСТОВСКИЙ','ПРИШВИН','ТЮТЧЕВ','БЛОК']
for i,name in enumerate(names):
 im=Image.new('RGBA',(512,128));d=ImageDraw.Draw(im);title=name+'   •   ИЗБРАННОЕ';d.text((256,64),title,font=font,fill=(191,169,116,255),anchor='mm');im.transpose(Image.Transpose.ROTATE_90).save(dest/('BookTitle'+str(i)+'.png'))
print('14 original spine labels')
