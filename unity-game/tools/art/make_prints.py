"""Typeset original in-world printed matter; font is used for rasterization, not redistributed."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parents[2]/'Assets/_Project/Art/Textures'
fontpath='C:/Windows/Fonts/arial.ttf';boldpath='C:/Windows/Fonts/arialbd.ttf'
def font(n,b=False):return ImageFont.truetype(boldpath if b else fontpath,n)
def center(d,text,y,f,fill,w):d.text((w/2,y),text,font=f,fill=fill,anchor='mt')
im=Image.new('RGB',(384,560),(220,209,180));d=ImageDraw.Draw(im)
d.rectangle((0,0,383,95),fill=(115,43,31));center(d,'НОЯБРЬ',17,font(42,True),(235,223,191),384);center(d,'1996',66,font(23),(235,223,191),384)
center(d,'15',115,font(188,True),(47,43,34),384);center(d,'ПЯТНИЦА',332,font(34,True),(108,43,30),384)
d.line((28,402,356,402),fill=(144,130,104),width=2);center(d,'ВОСХОД  08:07',426,font(20),(90,79,61),384);center(d,'ЗАКАТ   16:18',457,font(20),(90,79,61),384);center(d,'Берегите тепло вашего дома',506,font(17),(90,79,61),384);im.save(OUT/'CalendarPrint.png')
im=Image.new('RGB',(768,1024),(191,184,160));d=ImageDraw.Draw(im)
center(d,'ВЕЧЕРНИЙ ГОРОД',35,font(65,True),(40,40,34),768);d.line((40,125,728,125),fill=(54,51,40),width=4)
center(d,'Пятница, 15 ноября 1996 года  •  № 214',143,font(21),(50,47,37),768)
center(d,'ПЕРВЫЙ МОКРЫЙ СНЕГ',201,font(44,True),(40,40,34),768)
paragraph='Синоптики обещают похолодание. Вечером в городе ожидаются туман и мокрый снег. Коммунальные службы проверяют отопление жилых домов. Во дворах зажигаются фонари. Жители возвращаются домой с работы.'
import textwrap
for col in range(3):
 x=42+col*237;y=282
 for line in textwrap.wrap(paragraph+' '+paragraph,20):d.text((x,y),line,font=font(17),fill=(65,61,47));y+=25
 d.line((x,917,x+204,917),fill=(78,70,54),width=2)
 d.text((x,947),['НАШ РАЙОН','ПОГОДА','ОБЪЯВЛЕНИЯ'][col],font=font(18,True),fill=(44,43,36))
im.save(OUT/'NewspaperPrint.png')
for label,name,w,h in [('59','Number59',256,160),('61','Number61',256,160),('48','Number48',256,160),('ЦОЙ ЖИВ','Graffiti',768,176)]:
 im=Image.new('RGBA',(w,h),(0,0,0,0));d=ImageDraw.Draw(im);f=font(105,True if label.isdigit() else False);d.text((w/2,h/2),label,font=f,fill=(167,154,121,255) if label.isdigit() else (35,42,34,255),anchor='mm');im.save(OUT/(name+'.png'))
print('Original printed textures generated')
