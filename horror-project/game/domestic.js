import * as T from './vendor/three.module.js';

// Small authored objects are grouped by use: television, reading, tea, plants.
export function addDomesticDetails({root,box,softbox,cyl,sphere,mat,woodMap,cloth,rug,cream,brass,collider}){
 const walnut=mat(0x958470,woodMap,{roughness:.48}),edge=mat(0x665744,woodMap,{roughness:.5});
 const paper=mat(0xd3c9ad),porcelain=mat(0xc6c7b6,null,{roughness:.28});
 const glass=new T.MeshPhysicalMaterial({color:0x9ea99f,transparent:true,opacity:.12,roughness:.16,metalness:.05,depthWrite:false});
 const unit=new T.Group();unit.position.set(.08,0,-3.1);unit.rotation.y=-Math.PI/2;root.add(unit);
 // Three linked modules, recessed doors, moulded lips, open shelves and glazing.
 const W=3.02,D=.52;
 box(W,1.98,.025,0,1.12,-D/2,edge,unit);
 for(const x of [-1.49,-.57,.72,1.49])softbox(.046,2.18,D,x,1.11,0,walnut,unit,.009);
 for(const y of [.15,.68,2.18])softbox(W,.055,D+.025,0,y,0,walnut,unit,.009);
 box(W-.12,.09,D-.12,0,.08,-.02,edge,unit);
 for(const x of [-1.02,-.12,.98]){
  const width=x===-.12?1.23:.8;
  softbox(width,.43,.037,x,.408,.281,walnut,unit,.008);
  box(width-.1,.33,.009,x,.408,.304,edge,unit);
  box(width-.14,.29,.012,x,.408,.311,walnut,unit);
  cyl(.012,.012,.09,x+.25,.53,.337,brass,unit).rotation.x=Math.PI/2;
 }
 // Central CRT opening, objects on a shelf above it.
 softbox(1.25,.04,D,-.0,1.52,0,walnut,unit,.007);
 softbox(1.25,.04,D,-.0,1.97,0,walnut,unit,.007);
 for(const y of [1.11,1.59,1.98])box(.87,.028,D,-1.035,y,0,walnut,unit);
 for(const y of [1.02,1.39,1.79])box(.71,.028,D,1.08,y,0,walnut,unit);
 // Books of different sizes, compressed page blocks, cloth spines and faded bands.
 const tones=[0x565e54,0x897257,0x65443b,0x9f9274,0x4d5663,0x71534b];
 for(let row=0;row<3;row++)for(let i=0;i<8;i++){
  const x=.78+i*.074,h=.21+((i*7+row)%5)*.02,y=1.035+row*.385+h/2;
  const cover=mat(tones[(i+row*2)%tones.length],null,{roughness:.9});
  box(.056,h,.25,x,y,.04,cover,unit);box(.046,h-.017,.225,x,y,.033,paper,unit);
  box(.058,h,.016,x,y,.17,cover,unit);box(.04,.005,.002,x,y+h*.28,.179,brass,unit);
 }
 // Cups and a teapot behind framed glass. Normal familiar silhouettes.
 for(const [x,y] of [[-1.28,1.15],[-1.04,1.15],[-.8,1.15],[-1.22,1.63],[-.85,1.63]]){
  cyl(.076,.042,.019,x,y,.09,porcelain,unit);
  cyl(.053,.038,.085,x,y+.052,.09,porcelain,unit);
  const h=new T.Mesh(new T.TorusGeometry(.026,.009,6,16),porcelain);h.position.set(x+.057,y+.055,.09);unit.add(h);
 }
 for(const x of [-1.46,-.6])box(.027,1.34,.025,x,1.4,.296,walnut,unit);
 box(.83,1.32,.008,-1.03,1.4,.3,glass,unit);
 // A little reflection edge reads as glass without mirroring an empty room.
 box(.004,1.26,.002,-1.41,1.4,.307,mat(0x88998f,null,{transparent:true,opacity:.22}),unit);
 // Stacked albums, ordinary storage tins, family keepsakes.
 for(let i=0;i<3;i++){box(.43,.032,.29,-.25,1.556+i*.034,.015,mat(tones[i]),unit);box(.39,.019,.292,-.25,1.557+i*.034,.017,paper,unit);}
 const vase=mat(0x8e9d99,null,{roughness:.32});sphere(.095,.4,1.65,.025,vase,unit,.7,1.35,.7);cyl(.031,.04,.075,.4,1.78,.025,vase,unit);
 for(let i=0;i<5;i++)box(.1,.18,.19,-.43+i*.22,2.083,-.04,mat(tones[i]),unit);
 collider(-.1,-3.1,.66,3.02);

 // A real cable route and socket by the television, rather than floating appliances.
 function cable(points,r=.008,color=0x34372e){const curve=new T.CatmullRomCurve3(points.map(p=>new T.Vector3(...p)));const mesh=new T.Mesh(new T.TubeGeometry(curve,28,r,5,false),mat(color));root.add(mesh);}
 softbox(.019,.12,.1,.402,.26,-1.49,cream,root,.009);
 for(const z of [-1.51,-1.47])sphere(.008,.388,.26,z,mat(0x44483e));
 cable([[-.2,.79,-3.16],[.2,.57,-3.35],[.25,.08,-2.4],[.31,.075,-1.53],[.38,.25,-1.49]]);
 // TV remote on an accessible edge of the coffee table.
 const remote=softbox(.051,.021,.18,-4.04,.563,-1.08,mat(0x30362e),root,.009);remote.rotation.y=.2;
 for(let i=0;i<12;i++)sphere(.0037,-4.06+(i%3)*.014,.578,-1.135+Math.floor(i/3)*.022,mat(i===0?0x985b40:0x777b6f));
 // Saucer, biscuits, spoon: a small evening, not an exhibition display.
 cyl(.106,.1,.013,-3.83,.56,-1.4,porcelain);
 cyl(.12,.10,.017,-3.87,.564,-1.03,porcelain);
 for(const [dx,dz,angle] of [[-.045,0,.2],[.02,.025,-.35]]){const cookie=softbox(.074,.016,.057,-3.87+dx,.582,-1.03+dz,mat(0xac8951),root,.008);cookie.rotation.y=angle;for(let i=0;i<5;i++)sphere(.002,-3.88+dx+(i%3)*.016,.592,-1.045+dz+Math.floor(i/3)*.019,mat(0x735c39));}
 const spoon=softbox(.012,.005,.15,-3.7,.563,-1.35,brass,root,.004);spoon.rotation.y=.25;sphere(.02,-3.68,.563,-1.28,brass,root,.65,.12,1);
 // Reading left unfinished on the side table: a cloth bookmark protrudes.
 const journal=new T.Group();journal.position.set(-5.11,.7,-2.47);journal.rotation.y=.2;root.add(journal);
 box(.27,.013,.2,0,0,0,paper,journal);box(.005,.018,.2,0,.006,0,mat(0x756951),journal);
 for(let page=0;page<2;page++)for(let line=0;line<11;line++)box(.105-(line%3)*.012,.001,.0015,-.07+page*.14,.009,-.077+line*.014,mat(0x8c8774),journal);
 box(.017,.001,.23,.083,.01,.008,mat(0x835c4e),journal);
 // Looped telephone cord, spare spectacles and a crocheted runner.
 cable([[-5.24,.83,-2.14],[-5.38,.68,-2.2],[-5.4,.48,-2.27],[-5.34,.6,-2.09],[-5.22,.74,-2.01]],.009);
 for(const x of [-5.21,-5.14]){const ring=new T.Mesh(new T.TorusGeometry(.028,.0025,5,18),brass);ring.rotation.x=Math.PI/2;ring.position.set(x,.72,-2.45);root.add(ring);}

 // Window latch, seals and water marks live on the window, not across the screen.
 const seal=mat(0x646c60);
 for(const x of [-3.84,-2.68]){box(.012,1.35,.017,x,1.64,-4.90,seal);softbox(.021,.12,.033,x-.038,1.52,-4.87,cream,root,.009);}
 const drops=new T.Group();root.add(drops);
 for(let i=0;i<68;i++){const x=-4.8+(i*0.619%3.05),y=1.03+(i*.177%1.28);const drop=new T.Mesh(new T.SphereGeometry(.006,5,5),new T.MeshBasicMaterial({color:0xabbcc1,transparent:true,opacity:.12,depthWrite:false}));drop.position.set(x,y,-4.994);drop.scale.set(.45,2+(i%4),.3);drops.add(drop);}
 // Folded knit throw draped over the sofa, with an actual hanging end.
 const knit=mat(0x927562,cloth,{roughness:1,side:T.DoubleSide});const drape=new T.PlaneGeometry(.72,.91,14,22),p=drape.attributes.position;
 for(let i=0;i<p.count;i++){const y=p.getY(i);p.setZ(i,Math.sin(p.getX(i)*45)*.009);p.setY(i,y>.0?.66+y*.045:.66+y*.8);p.setX(i,p.getX(i)-5.16);p.setZ(i,p.getZ(i)+.53+(y>0?y*.8:0));}drape.computeVertexNormals();const throwMesh=new T.Mesh(drape,knit);throwMesh.castShadow=true;throwMesh.receiveShadow=true;root.add(throwMesh);

 // Kitchen shelf: tea tins and cups sit beside the kettle, utensils beside the stove.
 for(let i=0;i<3;i++){cyl(.06,.06,.16,4.77+i*.15,.994,-4.64,mat([0x736f55,0x9a8770,0x737f7c][i]));cyl(.064,.064,.014,4.77+i*.15,1.08,-4.64,brass);}
 for(let z of [-3.6,-3.4,-3.2]){cyl(.006,.006,.23,5.8,1.43,z,brass);sphere(.05,5.8,1.29,z,brass,root,.11,1, .68);}
 const rail=cyl(.012,.012,.9,5.8,1.6,-3.38,brass);rail.rotation.x=Math.PI/2;
 // Enamel saucepan with a lip, lid and handles rather than a painted silhouette.
 const enamel=mat(0xb4b8a8,null,{roughness:.32});cyl(.105,.09,.115,5.48,.985,-3.49,enamel);cyl(.11,.11,.012,5.48,1.046,-3.49,brass);sphere(.027,5.48,1.073,-3.49,mat(0x3c443d),root,1,.5,1);
 for(const z of [-3.35,-3.63])softbox(.068,.018,.054,5.48,1.012,z,mat(0x42493e),root,.01);
 return unit;
}
