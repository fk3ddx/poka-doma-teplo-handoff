"""Bedroom and hall furnishing. Runs in the build_furniture Blender namespace."""
def textile(name, point, rows=22, cols=32, material='Linen'):
 verts=[xyz(point(i/cols,j/rows)) for j in range(rows+1) for i in range(cols+1)];faces=[]
 for j in range(rows):
  for i in range(cols):
   a=j*(cols+1)+i;faces.append((a,a+1,a+cols+2,a+cols+1))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);register(o,name,material)
 for f in mesh.polygons:f.use_smooth=True
 o.modifiers.new('Fabric thickness','SOLIDIFY').thickness=.003
 return o

def ring(name,center,rx,ry,material='Brass',radius=.002,plane='xy'):
 x,y,z=center;points=[]
 for i in range(25):
  a=i*math.tau/24;points.append((x+rx*math.cos(a),y+ry*math.sin(a),z) if plane=='xy' else (x+rx*math.cos(a),y,z+ry*math.sin(a)))
 return curve(name,points,radius,material)

def clock():
 rod('Alarm clock body',(0,.088,-.028),(0,.088,.026),.077,'Brass',vertices=32)
 rod('Alarm ivory dial',(0,.088,.028),(0,.088,.031),.069,'Porcelain',vertices=32)
 for i in range(12):
  a=i*math.tau/12;rod('Clock hour mark',(.057*math.sin(a),.088+.057*math.cos(a),.034),(.065*math.sin(a),.088+.065*math.cos(a),.034),.0015,'Black',vertices=6)
 rod('Minute hand',(0,.088,.036),(.047,.114,.036),.0018,'Black',vertices=6);rod('Hour hand',(0,.088,.037),(-.023,.066,.037),.0022,'Black',vertices=6)
 for sign in [-1,1]:
  ellipsoid('Twin alarm bell',(sign*.051,.164,0),(.032,.018,.027),'Brass');rod('Clock foot',(sign*.046,0,.012),(sign*.040,.030,0),.005,'Brass')
 curve('Alarm carrying handle',[(-.031,.159,-.004),(-.020,.197,-.004),(.020,.197,-.004),(.031,.159,-.004)],.003,'Brass');rod('Alarm hammer',(0,.158,0),(0,.18,0),.003,'Black',vertices=8)

def chair():
 for x in [-.19,.19]:
  for z in [-.18,.18]:rod('Chair tapered leg',(x*1.13,0,z*1.1),(x,.45,z),.016,'Wood',.024)
 box('Wood chair seat',(0,.46,0),(.43,.04,.42),'WoodLight',.026)
 for x in [-.19,.19]:rod('Chair backpost',(x,.43,-.18),(x,1.02,-.2),.017,'Wood')
 for y in [.72,.91]:box('Chair curved backrest',(0,y,-.195),(.42,.08,.025),'WoodLight',.018)
 textile('Shawl over chair',lambda u,v:((u-.5)*.42,.99-v*.22 if v<.4 else .902-(v-.4)*.67,-.207+v*.09+.014*math.sin(u*31+v*2)),material='Plaid')
 for i in range(17):
  x=(i/16-.5)*.42;curve('Shawl fringe',[(x,.50,-.12),(x+.004,.46,-.1),(x-.002,.43,-.095)],.0015,'Plaid')

def suitcase():
 box('Leather suitcase',(0,.14,0),(.69,.27,.44),'Leather',.03)
 for y in [.045,.25]:ring('Suitcase stitched edge',(0,y,0),.34,.213,'BookTan',.003,'xz')
 for x in [-.22,.22]:
  box('Suitcase strap',(x,.282,0),(.033,.008,.41),'BookTan',.004);box('Case brass clasp',(x,.22,.225),(.034,.05,.015),'Brass',.005)
 curve('Case handle',[(-.09,.13,.234),(-.07,.13,.30),(.07,.13,.30),(.09,.13,.234)],.012,'Leather')

current='Bedroom'
def cover_point(u,v):
 x=.23+u*1.62;z=1.29+v*1.43;drop=max(0,.37-x,x-1.75)
 return (x,.574-drop*1.65+.013*math.sin(u*14+v*2)*(1-v*.4)+.008*math.sin(v*11+u*3)*math.sin(u*math.pi),z)
textile('Waffle cotton coverlet',cover_point)
textile('Turned linen top',lambda u,v:(.38+u*1.37,.586+.028*math.sin(v*math.pi)+.005*math.sin(u*27),1.28+v*.20),rows=8)
for side in [.234,1.846]:
 for i in range(27):
  z=1.31+i*.051;hem=.35 if side<1 else .413;curve('Crochet hem',[(side,hem,z),(side,hem-.026,z+.01),(side,hem-.027,z+.038),(side,hem,z+.05)],.0018,'Lace')
# Pillow edge seams are part of the authored soft pillow, not separate elliptical rings.
unit('Mechanical alarm',clock,(2.04,.636,.79),15)
rod('Water glass bottom',(2.23,.639,.80),(2.23,.646,.80),.031,'Crystal',vertices=12)
for i in range(12):
 a=i*math.tau/12;b=(i+1)*math.tau/12
 quad('Tumbler facet',[(2.23+.029*math.cos(a),.646,.80+.029*math.sin(a)),(2.23+.029*math.cos(b),.646,.80+.029*math.sin(b)),(2.23+.034*math.cos(b),.747,.80+.034*math.sin(b)),(2.23+.034*math.cos(a),.747,.80+.034*math.sin(a))],'Glass')
ring('Glass rim',(2.23,.748,.8),.034,.034,'Crystal',.0015,'xz');rod('Water surface',(2.23,.708,.8),(2.23,.709,.8),.032,'Glass',vertices=24)
box('Handkerchief',(2.17,.638,1.015),(.18,.009,.13),'Linen',.006)
for x in [2.127,2.205]:ring('Spectacle lens rim',(x,.652,1.018),.034,.023,'Brass',.0014,'xz')
curve('Spectacle bridge',[(2.16,.652,1.018),(2.166,.659,1.021),(2.172,.652,1.018)],.0014,'Brass')
for x in [2.09,2.24]:curve('Spectacle folded arm',[(x,.652,1.018),(x,.661,1.058),(x-.055,.66,1.062)],.0014,'Brass')
box('Medicine blister foil',(1.96,.637,1.036),(.085,.003,.045),'Chrome',.002)
for x in [1.935,1.96,1.985]:
 for z in [1.025,1.047]:ellipsoid('White tablet',(x,.642,z),(.008,.003,.005),'Porcelain')
unit('Shawl chair',chair,(.31,0,3.41),90);unit('Stored suitcase',suitcase,(.86,2.025,4.13),180)
box('Sewing table top',(2.53,.73,2.12),(.42,.045,.96),'WoodLight',.018)
for x in [2.38,2.68]:
 for z in [1.77,2.48]:rod('Sewing stand leg',(x,.03,z),(x,.70,z),.018,'Metal')
box('Sewing machine base',(2.53,.767,2.04),(.31,.029,.62),'Black',.012);box('Sewing machine covered case',(2.53,.91,2.04),(.275,.255,.49),'Coat',.04)
textile('Machine dust cover',lambda u,v:(2.32+u*.42,1.052-4*max(0,abs((u-.5)*.42)-.145,abs((v-.5)*.62)-.25)+.002*math.sin(v*32),1.73+v*.62),material='Linen')
rod('Thread spool',(2.50,.756,2.50),(2.50,.805,2.50),.013,'BookRed');rod('Wood spool top',(2.50,.804,2.50),(2.50,.809,2.50),.019,'WoodLight')
box('Bedroom family frame',(.046,1.66,1.9),(.035,.45,.35),'WoodLight',.007)
quad('Family photo bedroom',[(.067,1.465,1.75),(.067,1.465,2.05),(.067,1.855,2.05),(.067,1.855,1.75)],'FamilyPhoto',[(.46,.29),(.93,.29),(.93,.89),(.46,.89)])
curve('Mourning ribbon',[(.072,1.49,1.84),(.072,1.55,1.79),(.072,1.61,1.745)],.013,'Black')
quad('Floor carpet bedroom',[(1.87,.012,1.4),(2.26,.012,1.4),(2.26,.012,2.84),(1.87,.012,2.84)],'Carpet')

# Keep at least 0.55 m clear between the bed/cover and the sewing table.
for obj in groups['Bedroom']:
 if obj.name.startswith(('Sewing table','Sewing stand','Sewing machine','Machine dust','Thread spool','Wood spool')):obj.location.x+=.08

current='Hall'
box('Dresser polished top',(6.93,.875,5.52),(.61,.036,.82),'WoodLight',.022)
for z in [5.32,5.71]:
 for y,h in [(.66,.20),(.30,.43)]:
  box('Hall dresser inset drawer',(7.187,y,z),(.025,h,.34),'WoodLight',.007);box('Hall drawer recessed field',(7.203,y,z),(.008,h-.055,.28),'Wood',.004)
  curve('Hall drawer drop handle',[(7.219,y+.025,z-.04),(7.245,y-.025,z-.038),(7.245,y-.025,z+.038),(7.219,y+.025,z+.04)],.005,'Brass')
box('Dresser lower plinth',(6.94,.05,5.52),(.54,.08,.75),'WoodLight',.008)
box('Address book',(6.94,.901,5.78),(.16,.025,.17),'BookGreen',.004);box('Address book pages',(6.94,.903,5.784),(.144,.014,.15),'Paper',.001)
rod('Pocket flashlight',(6.97,.927,5.77),(7.10,.927,5.77),.022,'Black',vertices=16);rod('Torch lens',(7.101,.927,5.77),(7.107,.927,5.77),.024,'Chrome',vertices=16)
for y in [.09,.47]:box('Shoe bench shelf',(8.58,y,5.44),(.36,.032,.83),'WoodLight',.015)
for z in [5.05,5.83]:box('Shoe bench upright',(8.72,.28,z),(.045,.44,.04),'Wood')
for z in [5.24,5.51]:
 ellipsoid('Boot sole',(8.54,.13,z),(.145,.023,.067),'Black');ellipsoid('Leather boot foot',(8.52,.185,z),(.13,.062,.063),'Leather')
 rod('Boot shaft',(8.63,.18,z),(8.63,.39,z),.056,'Leather',.063,16);rod('Boot dark opening',(8.63,.391,z),(8.63,.392,z),.052,'Black',vertices=16)
box('Hat shelf',(8.55,1.98,4.6),(.42,.035,1.12),'WoodLight',.012)
ellipsoid('Ushanka crown',(8.55,2.07,4.39),(.125,.095,.115),'DarkFabric')
for z in [4.28,4.50]:ellipsoid('Ushanka earflap',(8.56,2.013,z),(.10,.025,.051),'Coat')
box('Folded scarf',(8.53,2.02,4.8),(.30,.045,.17),'Plaid',.017)
box('Electric meter backing',(8.59,2.06,6.075),(.28,.36,.028),'Wood',.008);box('Meter bakelite body',(8.59,2.06,6.05),(.22,.29,.065),'Black',.035)
quad('Meter window',[(8.505,1.993,6.014),(8.675,1.993,6.014),(8.675,2.139,6.014),(8.505,2.139,6.014)],'Glass')
box('Meter counter',(8.59,2.09,6.005),(.13,.029,.004),'Paper',.002)
for i in range(6):box('Meter number',(8.543+i*.019,2.091,6.001),(.009,.017,.002),'Black',0)
rod('Meter spinning disc',(8.59,2.025,6.025),(8.59,2.03,6.025),.068,'Chrome',vertices=24)
curve('Electric meter conduit',[(8.59,2.23,6.05),(8.59,2.37,6.05),(8.7,2.37,6.05)],.009,'Enamel')
box('Doorbell chime',(7.63,2.28,6.07),(.21,.13,.065),'Enamel',.014)
for y in [2.25,2.275,2.30]:box('Doorbell vent',(7.63,y,6.032),(.15,.006,.003),'Black',.001)
box('Hall overhead cabinet',(7.69,2.285,3.27),(2.10,.40,.49),'Wood',.009)
for x in [7.17,8.21]:
 box('Antresol door',(x,2.29,3.524),(1.015,.365,.026),'WoodLight',.006);ellipsoid('Antresol knob',(x+(.40 if x<7.6 else -.4),2.18,3.55),(.016,.016,.016),'Brass')
quad('Floor carpet hall',[(7.36,.014,3.64),(7.97,.014,3.64),(7.97,.014,5.53),(7.36,.014,5.53)],'Carpet')