"""Remaining domestic clusters and readable courtyard silhouettes for M1."""
current='Kitchen'
# Counter layout reserves the right-hand end for the breadbin.
for prefix in ('Preserve jar','Jar metal lid'):
 for obj,z in zip(sorted((o for o in groups['Kitchen'] if o.name.startswith(prefix)),key=lambda o:o.location.y),[1.20,1.33,1.46]):obj.location.y=z
# Enamel soup pot on the front burner; separate rim, lid and handles.
rod('Soup pot',(8.31,.891,1.82),(8.31,1.02,1.82),.104,'Enamel',vertices=32)
rod('Pot rolled lip',(8.31,1.018,1.82),(8.31,1.031,1.82),.109,'Chrome',vertices=32)
ellipsoid('Domed pot lid',(8.31,1.032,1.82),(.10,.019,.10),'Enamel')
rod('Pot lid knob',(8.31,1.047,1.82),(8.31,1.072,1.82),.025,'Black')
for dx in [-.12,.12]:curve('Pot grip',[(8.31+dx*.8,.98,1.77),(8.31+dx*1.2,.98,1.78),(8.31+dx*1.2,.98,1.86),(8.31+dx*.8,.98,1.87)],.007,'Black')
# Pot-bellied porcelain teapot, sugar bowl and a plate of homemade pastries.
ellipsoid('Teapot body',(7.52,.842,.34),(.067,.075,.062),'Porcelain')
rod('Tea spout',(7.56,.824,.34),(7.647,.897,.34),.026,'Porcelain',.012)
curve('Teapot handle',[(7.47,.891,.34),(7.427,.89,.34),(7.425,.81,.34),(7.48,.804,.34)],.007,'Porcelain')
ellipsoid('Teapot lid',(7.52,.912,.34),(.046,.014,.046),'Porcelain');ellipsoid('Teapot lid bud',(7.52,.927,.34),(.013,.012,.013),'Brass')
ellipsoid('Sugar bowl',(7.64,.802,.21),(.046,.041,.046),'Porcelain');ellipsoid('Sugar bowl lid',(7.64,.843,.21),(.048,.007,.048),'Porcelain')
rod('Pastry plate',(7.86,.767,.76),(7.86,.777,.76),.112,'Porcelain',vertices=32)
for x,z in [(7.82,.72),(7.9,.75),(7.83,.81)]:
 ellipsoid('Baked pastry',(x,.800,z),(.050,.026,.031),'Bread')
 curve('Crimped pastry seam',[(x-.03,.82,z),(x,.829,z+.002),(x+.03,.82,z)],.002,'BookTan')
textile('Pastry linen cover',lambda u,v:(7.79+u*.16,.811+.036*math.sin(u*math.pi)*math.sin(v*math.pi),.805+v*.15),rows=10,cols=12)
# Breadbin: a curved roll-top rather than another rectangular brown block.
box('Breadbin bottom',(6.84,.904,1.72),(.36,.03,.32),'WoodLight',.016)
for i in range(18):
 a=(i/17)*math.pi*.58
 y=.91+.16*math.sin(a);z=1.86-.15*math.cos(a)
 box('Breadbin rolled slat',(6.84,y,z),(.35,.013,.022),'WoodLight',.006)
rod('Breadbin handle',(6.78,1.045,1.852),(6.9,1.045,1.852),.007,'Brass')
# Dish rack hangs above the sink, leaving the little worktop usable.
for y in [1.21,1.37]:
 for z in [.62,1.05]:rod('Dish rack rail',(6.63,y,z),(6.98,y,z),.004,'Chrome')
for z in [.64+i*.065 for i in range(7)]:
 curve('Dish rack divider',[(6.64,1.20,z),(6.66,1.35,z),(6.91,1.35,z),(6.96,1.20,z)],.003,'Chrome')
for z in [.70,.83,.96]:rod('Drying plate',(6.80,1.29,z),(6.80,1.29,z+.011),.075,'Porcelain',vertices=32)
box('Spice ledge',(8.69,1.42,2.55),(.17,.025,.42),'WoodLight',.006)
for z in [2.42,2.55,2.68]:
 rod('Spice jar',(8.67,1.435,z),(8.67,1.523,z),.035,'Crystal',vertices=16)
 rod('Spice jar cap',(8.67,1.524,z),(8.67,1.536,z),.036,'BookRed',vertices=16)
# Wall clock uses the existing geometric dial, scaled for readability on the wall.
unit('Kitchen wall clock',clock,(8.67,1.97,2.52),-90)
# A folded bag of shopping bags on the side wall, soft profile and handles.
ellipsoid('Bag of bags',(6.71,1.03,2.54),(.09,.18,.12),'Linen')
curve('Bag handle',[(6.71,1.16,2.47),(6.71,1.31,2.54),(6.71,1.16,2.62)],.009,'Linen')

current='Living'
# Throw over the armchair seat/back; two different folded edges and tassels.
textile('Armchair blanket',lambda u,v:(4.665+u*.225,.698-max(0,u*.225-.105)*2.4+.003*math.sin(v*27),.68+v*.36),material='Plaid')
# Narrow folding table along the rear wall, outside the living/hall route.
box('Folding table centre',(3.50,.73,4.18),(.35,.04,.49),'WoodLight',.012)
for x in [3.30,3.70]:box('Table hanging leaf',(x,.38,4.18),(.027,.69,.49),'WoodLight',.008)
for x in [3.39,3.62]:
 for z in [4.0,4.36]:rod('Folding table foot',(x,.01,z),(x,.72,z),.018,'Wood')
# Wicker sewing basket and wool balls cluster on the table.
for j in range(8):ring('Wicker basket weave',(3.49,.78+j*.014,4.18),.112,.085,'BookTan',.004,'xz')
for i in range(14):
 a=i*math.tau/14;rod('Basket upright',(3.49+.11*math.cos(a),.775,4.18+.083*math.sin(a)),(3.49+.117*math.cos(a),.888,4.18+.088*math.sin(a)),.003,'WoodLight')
for x,z,m in [(3.46,4.16,'BookRed'),(3.55,4.19,'DarkFabric'),(3.49,4.22,'Linen')]:ellipsoid('Wool ball',(x,.88,z),(.036,.036,.035),m)
curve('Basket handle',[(3.38,.875,4.18),(3.41,1.025,4.18),(3.58,1.025,4.18),(3.61,.875,4.18)],.009,'WoodLight')
# Five tulips replace the dry twig arrangement in the living-room jug.
for i in range(5):
 a=i*2.4;dx=.07*math.cos(a);dz=.07*math.sin(a);top=1.09+i*.018
 curve('Tulip stem',[(4.12,.88,1.87),(4.12+dx*.4,1.00,1.87+dz*.5),(4.12+dx,top,1.87+dz)],.003,'Leaf')
 for k in range(3):
  b=k*math.tau/3
  ellipsoid('Tulip petal',(4.12+dx+.01*math.cos(b),top+.022,1.87+dz+.01*math.sin(b)),(.016,.034,.014),'BookRed')
 leaf=ellipsoid('Tulip leaf',(4.12+dx*.9,1.01,1.87+dz*1.2),(.012,.065,.006),'Leaf');leaf.rotation_euler.y=a*.4

current='Bathroom'
# Compact activator washer in the free back-left corner, with thick plastic lid.
rod('Malyutka tub',(3.20,.06,5.75),(3.20,.62,5.75),.205,'WasherBlue',.218,32)
rod('Washer base',(3.20,.02,5.75),(3.20,.07,5.75),.212,'Enamel',vertices=32)
ellipsoid('Malyutka lid',(3.20,.635,5.75),(.223,.047,.223),'Enamel')
box('Washer lid handle',(3.20,.681,5.75),(.13,.027,.042),'WasherBlue',.013)
box('Washer motor housing',(3.20,.26,5.945),(.23,.42,.12),'WasherBlue',.032)
curve('Washer drain hose',[(3.38,.12,5.8),(3.5,.07,5.7),(3.57,.12,5.3),(3.5,.57,5.24)],.012,'Black')
# Nested basins have open interiors with separate rim and bottom.
for y,r in [(.04,.20),(.105,.18)]:
 rod('Basin bottom',(3.72,y,5.83),(3.72,y+.015,5.83),r*.78,'WasherBlue',vertices=24)
 for i in range(24):
  a=i*math.tau/24;b=(i+1)*math.tau/24
  quad('Basin side',[(3.72+r*.78*math.cos(a),y,5.83+r*.78*math.sin(a)),(3.72+r*.78*math.cos(b),y,5.83+r*.78*math.sin(b)),(3.72+r*math.cos(b),y+.12,5.83+r*math.sin(b)),(3.72+r*math.cos(a),y+.12,5.83+r*math.sin(a))],'WasherBlue')
 ring('Basin rim',(3.72,y+.12,5.83),r,r,'Enamel',.007,'xz')
box('Bathroom mirror shelf',(4.1,1.11,5.90),(.46,.024,.14),'Enamel',.008)
for x in [3.94,4.07]:
 rod('Wash bottle',(x,1.125,5.87),(x,1.235,5.87),.023,'Enamel',vertices=16);rod('Wash bottle cap',(x,1.235,5.87),(x,1.254,5.87),.012,'BookRed')
curve('Bath laundry line',[(3.10,2.25,4.75),(3.54,2.22,4.75),(4.04,2.25,4.75)],.002,'Linen')
textile('Hanging washed towel',lambda u,v:(3.28+u*.40,2.23-v*.52,4.75+.018*math.sin(u*34)+.02*v),material='Linen')
for x in [3.3,3.64]:box('Wood clothes peg',(x,2.245,4.75),(.012,.055,.023),'WoodLight',.003)

current='Balcony'
# Stored winter equipment along the closed wall, never in the exit opening.
for x in [3.02,3.13]:
 curve('Wood ski',[(x,.02,-.81),(x,.95,-.82),(x,1.72,-.83),(x,1.85,-.74)],.027,'WoodLight')
 box('Ski binding',(x,.8,-.80),(.07,.08,.035),'Metal',.003)
for x in [3.05,3.15]:rod('Ski pole',(x,.02,-.65),(x,1.57,-.63),.006,'Metal')
for z in [-.53,-.98]:curve('Sled runner',[(3.25,.06,z),(3.79,.06,z),(3.90,.13,z),(3.91,.22,z)],.012,'Metal')
for x in [3.3,3.5,3.7]:box('Sled seat plank',(x,.24,-.755),(.17,.026,.47),'WoodLight',.008)
for x in [3.33,3.71]:
 for z in [-.53,-.98]:rod('Sled brace',(x,.06,z),(x,.22,z),.008,'Metal')
# Potato crate + jars at the opposite end, under window-height.
for y in [.08,.19,.3]:
 for z in [-.53,-1.01]:box('Balcony crate plank',(6.10,y,z),(.5,.085,.016),'WoodLight',.003)
for x in [5.86,6.34]:box('Crate end',(x,.18,-.77),(.018,.30,.5),'WoodLight',.005)
for i in range(12):ellipsoid('Stored potato',(5.93+(i%3)*.13,.25+(i//9)*.07,-.60+(i//3%3)*-.13),(.056,.037,.045),'BookTan')
for z in [-.59,-.84]:
 rod('Winter preserve jar',(6.12,.37,z),(6.12,.56,z),.062,'Crystal',vertices=16);rod('Winter jar lid',(6.12,.56,z),(6.12,.576,z),.065,'Brass',vertices=16)
curve('Balcony clothesline',[(3.05,2.3,-1.16),(4.6,2.19,-1.16),(6.3,2.3,-1.16)],.002,'Black')

current='Yard'
# Metal swing frame and empty sandbox: familiar silhouettes below the fifth floor.
y=-11.7
for x in [-1.8,1.8]:
 rod('Swing A frame',(x,y,-13.4),(x,y+2.7,-14.3),.043,'YardPaint')
 rod('Swing A frame',(x,y,-15.2),(x,y+2.7,-14.3),.043,'YardPaint')
rod('Swing crossbar',(-1.8,y+2.7,-14.3),(1.8,y+2.7,-14.3),.049,'YardPaint')
for x in [-.85,.85]:
 for dx in [-.22,.22]:rod('Swing chain',(x+dx,y+2.65,-14.3),(x+dx,y+.52,-14.36),.006,'Metal')
 box('Empty swing seat',(x,y+.50,-14.36),(.56,.055,.30),'WoodLight',.01)
for x in [-.85,.85]:box('Sandbox long plank',(x+7,y+.12,-13),(.09,.24,2.8),'YardPaint',.01)
for z in [-14.4,-11.6]:box('Sandbox end plank',(7,y+.12,z),(1.8,.24,.09),'YardPaint',.01)
box('Sandbox frozen sand',(7,y+.035,-13),(1.64,.07,2.6),'Concrete',0)
# Carpet-beating frame, garages, battered entrance canopy and distant heat stack.
for x in [11,13.5]:rod('Carpet beater upright',(x,y,-11),(x,y+2.15,-11),.033,'YardPaint')
rod('Carpet beater top',(11,y+2.15,-11),(13.5,y+2.15,-11),.033,'YardPaint')
for i in range(6):
 x=18+i*3.1;box('Garage shell',(x,y+1.18,-33),(3.0,2.36,5.0),'Concrete',.03)
 box('Garage metal door',(x,y+1.0,-30.48),(2.73,2.0,.08),'GaragePaint',.025)
 box('Garage roof',(x,y+2.4,-33),(3.15,.1,5.15),'Road',.01)
 box('Garage door seam',(x,y+1.0,-30.43),(.025,2.0,.01),'Black',0)
for x in [-16,-4,8]:
 box('Entrance canopy',(x,y+2.35,-27.7),(2.05,.15,1.1),'Concrete',.02)
 for dx in [-.85,.85]:rod('Canopy bracket',(x+dx,y+1.8,-28.2),(x+dx,y+2.30,-27.2),.025,'Metal')
# Far silhouette seen through fog, not a bright decorative skyline.
rod('Distant heat stack',(-27,y,-69),(-27,y+38,-69),1.12,'Concrete',.68,24)
for h in [17,22,27,32]:rod('Chimney faded band',(-27,y+h,-69),(-27,y+h+1.4,-69),1.14-(h/38)*.44,'StackBand',1.14-((h+1.4)/38)*.44,24)

# Keep the diagonal living-room/bedroom route open; group stays under the family photo.
for obj in groups['Living']:
 if obj.name.startswith(('Folding table','Table hanging leaf','Wicker basket','Basket upright','Wool ball','Basket handle')):obj.location.x+=.45

current='Living'
box('Television lace mat',(6.17,.656,1.92),(.38,.005,.73),'Tulle',.002)
rod('Dry flower vase',(4.02,.753,4.0),(4.02,.9,4.0),.038,'Porcelain',.023,24)
for i in range(5):
 a=i*2.1;dx=.055*math.cos(a);dz=.05*math.sin(a)
 curve('Dried flowers at folding table',[(4.02,.88,4.0),(4.02+dx,1.02+i*.014,4.0+dz),(4.02+dx*1.4,1.06+i*.014,4.0+dz*1.3)],.002,'DriedStem')
current='Kitchen'
box('Fridge linen mat',(8.39,1.304,.70),(.55,.004,.54),'Tulle',.001)
current='Balcony'
rod('Stored basin bottom',(3.61,.27,-.755),(3.61,.28,-.755),.115,'WasherBlue',vertices=24)
for i in range(24):
 a=i*math.tau/24;b=(i+1)*math.tau/24
 quad('Stored basin wall',[(3.61+.115*math.cos(a),.28,-.755+.115*math.sin(a)),(3.61+.115*math.cos(b),.28,-.755+.115*math.sin(b)),(3.61+.16*math.cos(b),.4,-.755+.16*math.sin(b)),(3.61+.16*math.cos(a),.4,-.755+.16*math.sin(a))],'WasherBlue')
ring('Stored basin rim',(3.61,.4,-.755),.16,.16,'Enamel',.006,'xz')

# Put the sitting bath against the west wall; preserve a full capsule-width entry gap.
for obj in groups['Bathroom']:
 if obj.name.startswith(('Bath enamel body','Bath inner well','Bath foot')):obj.location.x-=.06
