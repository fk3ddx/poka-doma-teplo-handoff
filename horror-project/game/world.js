import * as T from './vendor/three.module.js';
import {addDomesticDetails} from './domestic.js';
import {GLTFLoader} from './vendor/addons/loaders/GLTFLoader.js';
import {RoundedBoxGeometry} from './vendor/addons/geometries/RoundedBoxGeometry.js';
import {mergeGeometries} from './vendor/addons/utils/BufferGeometryUtils.js';

let seed=92041;
function rand(){seed=(seed*1664525+1013904223)>>>0;return seed/4294967296;}
function texture(kind,size=256){
 const c=document.createElement('canvas');c.width=c.height=size;const g=c.getContext('2d');
 const base={wall:'#c3b68e',wood:'#775335',floor:'#886442',rug:'#793d32',cloth:'#8b7050',tile:'#abb4a3',facade:'#6a7371',picture:'#71817c',lace:'#bcae87'}[kind]||'#aaa';
 g.fillStyle=base;g.fillRect(0,0,size,size);
 for(let i=0;i<4000;i++){const v=rand()>.5?255:0;g.fillStyle=`rgba(${v},${v},${v},${rand()*.07})`;g.fillRect(rand()*size,rand()*size,1+rand()*3,1+rand()*2);}
 if(kind==='wall'){
   g.strokeStyle='#837a5738';g.lineWidth=1;
   for(let x=16;x<size;x+=32)for(let y=16;y<size;y+=40){g.beginPath();g.moveTo(x,y-12);g.bezierCurveTo(x+14,y-4,x+12,y+6,x,y+13);g.bezierCurveTo(x-12,y+6,x-14,y-4,x,y-12);g.stroke();g.beginPath();g.moveTo(x,y-8);g.lineTo(x,y+10);g.stroke();}
 }
 if(kind==='wood'||kind==='floor'){
   for(let i=0;i<160;i++){g.strokeStyle=`rgba(38,24,12,${rand()*.13})`;g.beginPath();let y=rand()*size;g.moveTo(0,y);g.bezierCurveTo(70,y+rand()*12-6,160,y+rand()*10-5,size,y);g.stroke();}
   if(kind==='floor'){for(let y=0;y<size;y+=32){g.fillStyle='#39241677';g.fillRect(0,y,size,2);for(let x=(y%64?0:64);x<size;x+=128)g.fillRect(x,y,2,32);}}
 }
 if(kind==='rug'){
   for(let inset of [5,10,20,26]){g.strokeStyle=inset%2?'#baa16a':'#402e25';g.lineWidth=3;g.strokeRect(inset,inset,size-inset*2,size-inset*2);}
   for(let x=40;x<size-30;x+=29)for(let y=40;y<size-30;y+=29){g.fillStyle='#b5975c';g.beginPath();g.moveTo(x,y-10);g.lineTo(x+9,y);g.lineTo(x,y+10);g.lineTo(x-9,y);g.closePath();g.fill();g.fillStyle='#534c36';g.fillRect(x-3,y-3,6,6);}
 }
 if(kind==='cloth'||kind==='lace'){for(let i=0;i<size;i+=4){g.fillStyle='#d2c6a51a';g.fillRect(i,0,1,size);g.fillRect(0,i,size,1);}if(kind==='lace'){g.strokeStyle='#e1d4ae';for(let x=8;x<size;x+=16)for(let y=8;y<size;y+=16){g.beginPath();g.arc(x,y,6,0,Math.PI*2);g.stroke();}}}
 if(kind==='tile'){g.strokeStyle='#596758';g.lineWidth=3;for(let i=0;i<=size;i+=64){g.beginPath();g.moveTo(i,0);g.lineTo(i,size);g.moveTo(0,i);g.lineTo(size,i);g.stroke();}g.strokeStyle='#c9c9b244';for(let i=3;i<size;i+=64){g.beginPath();g.moveTo(i,0);g.lineTo(i,size);g.stroke();}}
 if(kind==='picture'){
   g.fillStyle='#a1a197';g.fillRect(0,0,size,size*.6);g.fillStyle='#7e8882';for(let i=0;i<14;i++){g.beginPath();g.moveTo(i*24-10,180);g.lineTo(i*24+8,65+rand()*50);g.lineTo(i*24+30,180);g.fill();}g.fillStyle='#96917b';g.fillRect(0,180,size,76);g.strokeStyle='#454d40';for(let i=0;i<8;i++){let x=rand()*size;g.beginPath();g.moveTo(x,220);g.lineTo(x-5,70);g.stroke();}
 }
 const t=new T.CanvasTexture(c);t.colorSpace=T.SRGBColorSpace;t.wrapS=t.wrapT=T.RepeatWrapping;t.magFilter=T.LinearFilter;t.anisotropy=2;return t;
}
const materialCache=new Map();
function mat(color,map,extra={}){const key=String(color)+'|'+(map?.uuid||'')+'|'+JSON.stringify(Object.entries(extra).map(([k,v])=>[k,v?.uuid||v?.toArray?.()||v]));if(!materialCache.has(key))materialCache.set(key,new T.MeshStandardMaterial({color,roughness:.86,map:map||null,...extra}));return materialCache.get(key);}
export async function buildWorld(scene){
 const root=new T.Group();root.scale.set(.78,1,.78);scene.add(root);const solids=[],interactables=[],lamps=[];const materials={};
 for(const k of ['wall','wood','floor','rug','cloth','tile','facade','picture','lace'])materials[k]=texture(k);
 const textureLoader=new T.TextureLoader();
 async function loadTex(path,color=true){const t=await textureLoader.loadAsync('./assets/'+path);if(color)t.colorSpace=T.SRGBColorSpace;t.wrapS=t.wrapT=T.RepeatWrapping;t.anisotropy=4;return t;}
 const [wallMap,rugMap,woodMap,floorMap,floorNormal,woodNormal]=await Promise.all([
 loadTex('wallpaper-v2.png'),loadTex('rug-v2.png'),loadTex('wood_table_001/Diffuse.jpg'),loadTex('wood_floor_worn/Diffuse.jpg'),loadTex('wood_floor_worn/nor_gl.jpg',false),loadTex('wood_table_001/nor_gl.jpg',false)]);
 materials.wall=wallMap;materials.rug=rugMap;materials.wood=woodMap;materials.floor=floorMap;
 const wallpaper=mat(0xd1d2c8,materials.wall),wood=mat(0xc6b8a3,materials.wood,{normalMap:woodNormal,normalScale:new T.Vector2(.2,.2),roughness:.52}),darkWood=mat(0x65554a,materials.wood,{roughness:.5}),cream=mat(0xc4c0b3),white=mat(0xc3c6b9),brass=mat(0x93815b,null,{metalness:.35,roughness:.5});
 const floorTex=materials.floor.clone();floorTex.needsUpdate=true;floorTex.repeat.set(5,4);floorNormal.repeat.copy(floorTex.repeat);const floorMat=mat(0xb9afa1,floorTex,{normalMap:floorNormal,normalScale:new T.Vector2(.22,.22),roughness:.65});
 const basic=(color)=>new T.MeshBasicMaterial({color});
 function box(w,h,d,x,y,z,m=cream,parent=root){const geo=new T.BoxGeometry(w,h,d);
   if(m===wallpaper||m.map===materials.wood){const uv=geo.attributes.uv,pos=geo.attributes.position,n=geo.attributes.normal;for(let i=0;i<uv.count;i++){const horizontal=Math.abs(n.getY(i))>.5;uv.setXY(i,(Math.abs(n.getX(i))>.5?pos.getZ(i):pos.getX(i))/1.8,(horizontal?pos.getZ(i):pos.getY(i))/1.8);}}
   const o=new T.Mesh(geo,m);o.position.set(x,y,z);o.castShadow=true;o.receiveShadow=true;parent.add(o);return o;}
 function softbox(w,h,d,x,y,z,m=cream,parent=root,r=.035){const o=new T.Mesh(new RoundedBoxGeometry(w,h,d,2,Math.min(r,w/3,h/3,d/3)),m);o.position.set(x,y,z);o.castShadow=true;o.receiveShadow=true;parent.add(o);return o;}
 function cyl(r1,r2,h,x,y,z,m=cream,parent=root,n=16){const o=new T.Mesh(new T.CylinderGeometry(r1,r2,h,n),m);o.position.set(x,y,z);o.castShadow=true;o.receiveShadow=true;parent.add(o);return o;}
 function sphere(r,x,y,z,m=cream,parent=root,sx=1,sy=1,sz=1){const o=new T.Mesh(new T.SphereGeometry(r,16,12),m);o.position.set(x,y,z);o.scale.set(sx,sy,sz);parent.add(o);return o;}
 function collider(x,z,w,d){const c={x,z,w,d};solids.push(c);return c;}
 function wall(w,h,d,x,y,z,m=wallpaper){const o=box(w,h,d,x,y,z,m);collider(x,z,w,d);return o;}
 function target(o,id,label,range=2.2){o.userData={id,label,range};interactables.push(o);return o;}
 function hotspot(w,h,d,x,y,z,id,label,range=2.2){const o=box(w,h,d,x,y,z,new T.MeshBasicMaterial({transparent:true,opacity:0,depthWrite:false}));o.castShadow=false;return target(o,id,label,range);}
 function picture(x,y,z,w=.65,h=.55,rot=0){const p=new T.Group();p.position.set(x,y,z);p.rotation.y=rot;root.add(p);box(w+.07,h+.07,.045,0,0,0,darkWood,p);box(w,h,.048,0,0,.006,mat(0xc4b899,materials.picture),p);return p;}
 function lamp(x,y,z,type='shade'){
   const p=new T.Group();root.add(p);p.position.set(x,0,z);
   cyl(.19,.22,.07,0,.045,0,darkWood,p);cyl(.022,.022,y-.38,0,(y-.38)/2,0,brass,p);
   const shade=new T.Mesh(new T.CylinderGeometry(.22,.36,.42,20,1,true),new T.MeshStandardMaterial({color:0xd8b47a,emissive:0xb57a30,emissiveIntensity:.32,side:T.DoubleSide}));shade.position.y=y-.21;p.add(shade);
   const light=new T.PointLight(0xffd4a5,8,5,2);light.position.set(x,y-.28,z);root.add(light);lamps.push(light);
   light.userData.base=8;
   sphere(.065,0,y-.25,0,basic(0xffd18d),p);
   return p;
 }
 function plant(x,y,z,scale=1){const p=new T.Group();p.position.set(x,y,z);p.scale.setScalar(scale);root.add(p);cyl(.12,.085,.23,0,.115,0,mat(0x855b3a),p);cyl(.108,.108,.015,0,.232,0,mat(0x333522),p);for(let i=0;i<9;i++){const a=i*2.4;const leaf=sphere(.15,Math.cos(a)*.12,.31+rand()*.18,Math.sin(a)*.12,mat(i%2?0x54603a:0x697144),p,.45,1.25,.2);leaf.rotation.set(Math.cos(a)*.8,0,Math.sin(a)*.8);}return p;}
 function mug(x,y,z){const p=new T.Group();p.position.set(x,y,z);root.add(p);cyl(.09,.076,.15,0,.08,0,mat(0xc8c0a0),p);cyl(.078,.078,.005,0,.153,0,mat(0x30251a),p);const h=new T.Mesh(new T.TorusGeometry(.053,.015,6,14),cream);h.position.set(.102,.09,0);p.add(h);return p;}
 function book(x,y,z,w=.07,h=.27,d=.19,color=0x716045,parent=root){box(w,h,d,x,y,z,mat(color),parent);box(w*.7,h*.93,d*.99,x,y,z+.005,mat(0xbdb393),parent);box(w,h,.012,x,y,z+d*.5,mat(color),parent);}
 // Apartment shell. Normal room scale, fifth floor; windows are real openings.
 box(12,.16,10,0,-.08,0,floorMat);box(12,.12,10,0,2.7,0,mat(0xaaa99f));
 wall(.18,2.7,10,-6,1.35,0);wall(.18,2.7,10,6,1.35,0);
 const rearWall=wall(12,2.7,.18,0,1.35,5);rearWall.userData.dynamic=true;
 // Front facade has two large windows.
 wall(1,2.7,.18,-5.5,1.35,-5);wall(2,2.7,.18,-.5,1.35,-5);wall(1,2.7,.18,5.5,1.35,-5);
 wall(1.8,2.7,.18,1.4,1.35,-5);
 wall(.5,2.7,.18,2.55,1.35,-5);
 for(const [cx,w] of [[-3.25,3.5],[3.9,2.2]]){
   wall(w,.84,.18,cx,.42,-5);wall(w,.32,.18,cx,2.54,-5);
   box(w+.15,.07,.4,cx,.88,-4.9,white);
   for(const xx of [cx-w/2,cx-w/6,cx+w/6,cx+w/2])box(.055,1.55,.12,xx,1.65,-4.98,white);
   for(const yy of [.94,2.37])box(w,.065,.12,cx,yy,-4.98,white);
   box(w,.04,.12,cx,1.97,-4.98,white);
   // Weak glass tint: view retains the distance and fog, no reflective mirror.
   box(w,1.38,.012,cx,1.65,-5.005,new T.MeshBasicMaterial({color:0x9ba6a2,transparent:true,opacity:.07,depthWrite:false}));
   for(let i=0;i<11;i++){softbox(.1,.53,.17,cx-.8+i*.16,.41,-4.73,mat(0xb3b5aa),root,.035);cyl(.025,.025,.12,cx-.8+i*.16,.58,-4.61,white).rotation.x=Math.PI/2;}
   box(1.84,.045,.09,cx,.66,-4.67,white);box(1.84,.045,.09,cx,.17,-4.67,white);
   hotspot(w,1.3,.1,cx,1.65,-4.9,'window','Посмотреть во двор',2.8);
 }
 // Curtains gathered on the sides, pleated cloth rather than blocks.
 for(const x of [-4.93,-1.56,2.87,4.92]){
   cyl(.025,.025,.8,x,2.4,-4.73,brass).rotation.z=Math.PI/2;
   const geometry=new T.PlaneGeometry(.42,1.52,28,18),p=geometry.attributes.position;
   for(let i=0;i<p.count;i++){const xx=p.getX(i),yy=p.getY(i);p.setZ(i,Math.sin(xx*85)*.029+(Math.sin(yy*3)*.025));p.setX(i,xx*(.8+(1-yy)*.14));}geometry.computeVertexNormals();
   const mesh=new T.Mesh(geometry,mat(0xd8d5c8,materials.cloth,{side:T.DoubleSide,roughness:1}));mesh.position.set(x,1.65,-4.71);mesh.castShadow=true;mesh.receiveShadow=true;root.add(mesh);
 }
 // Living room wall opening into hall, plus kitchen and bedroom openings.
 wall(.14,2.7,3.7,.5,1.35,-3.15);wall(.14,2.7,1,.5,1.35,1.0);box(.14,.46,1.3,.5,2.47,-.65);
 wall(.14,2.7,2.95,2.3,1.35,-3.525);wall(.14,2.7,.5,2.3,1.35,-.55);box(.14,.48,1.5,2.3,2.46,-1.35);
 wall(.14,2.7,2.3,2.3,1.35,.85);wall(.14,2.7,1.9,2.3,1.35,4.05);box(.14,.46,1.1,2.3,2.47,2.55);
 wall(3.7,2.7,.14,4.15,1.35,.35);
 wall(6.5,2.7,.15,-2.75,1.35,1.55);
 // Door frames and skirting make the rooms lived-in and correctly scaled.
 for(const [x,z,w] of [[.5,-.65,1.3],[2.3,-1.35,1.5],[2.3,2.55,1.1]]){
   for(const zz of [z-w/2,z+w/2])box(.23,2.25,.065,x,1.125,zz,wood);
   box(.23,.07,w+.13,x,2.25,z,wood);
 }
 for(const x of [-5.88,.39,2.42,5.88])box(.055,.11,6.35,x,.055,-1.65,darkWood);
 box(11.7,.11,.06,0,.055,-4.86,darkWood);
 // Sofa, cushions and throw.
 const sofa=new T.Group();sofa.position.set(-5.25,0,-.35);root.add(sofa);const fabric=mat(0x8c8264,materials.cloth);
 box(1.1,.29,2.25,0,.32,0,fabric,sofa);box(.23,.72,2.28,-.46,.59,0,fabric,sofa);
 for(const z of [-.63,0,.63]){const cushion=box(.9,.16,.62,.03,.53,z,fabric,sofa);cushion.rotation.z=.025;box(.17,.44,.6,-.27,.82,z,fabric,sofa);}
 for(const z of [-1.07,1.07])box(1.12,.43,.18,0,.62,z,fabric,sofa);
 for(const z of [-.88,.88])for(const x of [-.36,.36])cyl(.04,.035,.2,x,.1,z,darkWood,sofa);
 const pillow=box(.26,.47,.46,-.1,.77,-.63,mat(0xb49c68,materials.cloth),sofa);pillow.rotation.x=.2;pillow.rotation.z=-.25;
 box(.87,.025,.55,.16,.63,.57,mat(0x795a46,materials.rug),sofa);collider(-5.25,-.35,1.1,2.25);
 // Persian rug, coffee table with curved legs.
 box(3.55,.012,2.45,-3.25,.012,-1.35,mat(0xc3a884,materials.rug));
 const table=box(1.45,.08,.85,-3.55,.5,-1.35,wood);collider(-3.55,-1.35,1.45,.85);
 for(const x of [-4.12,-2.98])for(const z of [-1.65,-1.06])cyl(.025,.043,.46,x,.24,z,darkWood);
 box(.65,.008,.51,-3.5,.546,-1.32,mat(0xf0deb2,materials.lace));
 mug(-3.83,.55,-1.4);hotspot(.3,.3,.3,-3.83,.71,-1.4,'coffee','Выпить кофе');
 const note=box(.24,.005,.29,-3.39,.558,-1.2,mat(0xe0d1a7));note.rotation.y=-.2;target(note,'note','Прочитать записку');
 for(let i=0;i<5;i++)box(.17,.001,.004,-3.39,.562,-1.3+i*.028,mat(0x706d56));
 const handheld=softbox(.32,.033,.145,-3.12,.574,-1.47,mat(0x212a2b,null,{roughness:.36}),root,.019);target(handheld,'handheld','Взять приставку');
 softbox(.193,.004,.107,-3.12,.593,-1.47,mat(0x0d151a),root,.008);box(.17,.003,.087,-3.12,.596,-1.47,basic(0x52686b));
 box(.026,.006,.008,-3.246,.596,-1.47,mat(0x969f9b));box(.008,.006,.027,-3.246,.596,-1.47,mat(0x969f9b));
 for(const [dx,dz] of [[0,.014],[0,-.014],[.014,0],[-.014,0]])cyl(.005,.005,.006,-2.994+dx,.596,-1.47+dz,mat(0x9aa19d));
 for(let i=0;i<4;i++)box(.002,.001,.045,-3.16+i*.022,.599,-1.47,basic(0x829882));
 // Steam is animated, very faint and only near the coffee.
 const steam=new T.Group();steam.userData.dynamic=true;steam.position.set(-3.83,.78,-1.4);root.add(steam);const steamCanvas=document.createElement('canvas');steamCanvas.width=steamCanvas.height=32;const sg=steamCanvas.getContext('2d'),gradient=sg.createRadialGradient(16,16,1,16,16,16);gradient.addColorStop(0,'rgba(225,220,202,.3)');gradient.addColorStop(1,'rgba(225,220,202,0)');sg.fillStyle=gradient;sg.fillRect(0,0,32,32);const steamMap=new T.CanvasTexture(steamCanvas);for(let i=0;i<6;i++){const puff=new T.Sprite(new T.SpriteMaterial({map:steamMap,color:0xdfdacf,transparent:true,opacity:.22,depthWrite:false}));puff.scale.set(.055,.11,1);puff.position.y=i*.05;steam.add(puff);} 
 // CRT and cabinet; screen drawn each frame with a quiet original weather channel.
 // Television now belongs to the wall unit, with a proper viewing direction.
 
 const tv=new T.Group();tv.userData.dynamic=true;tv.position.set(-.28,1.08,-3.1);tv.rotation.y=-Math.PI/2;root.add(tv);
 box(.95,.68,.53,0,0,0,mat(0x574f3a),tv);box(.74,.55,.04,-.06,.02,.283,mat(0x272d25),tv);
 const tvCanvas=document.createElement('canvas');tvCanvas.width=256;tvCanvas.height=192;const tvTex=new T.CanvasTexture(tvCanvas);tvTex.colorSpace=T.SRGBColorSpace;
 const screen=new T.Mesh(new T.PlaneGeometry(.65,.46),new T.MeshBasicMaterial({map:tvTex}));screen.position.set(-.06,.025,.308);tv.add(screen);target(screen,'tv','Телевизор');
 for(const y of [-.17,.04,.17])cyl(.025,.025,.02,.36,y,.29,brass,tv).rotation.x=Math.PI/2;
 const tvLight=new T.PointLight(0xc8ddcb,.6,3);tvLight.position.set(-.6,1.1,-3.3);root.add(tvLight);
 // Side table, rotary-inspired telephone and a small lamp.
 box(.64,.065,.55,-5.2,.66,-2.13,wood);for(const x of [-5.43,-4.97])for(const z of [-2.32,-1.94])cyl(.025,.04,.64,x,.32,z,darkWood);collider(-5.2,-2.13,.64,.55);
 const phone=softbox(.27,.09,.22,-5.12,.75,-2.1,mat(0x636d58),root,.025);target(phone,'phone','Позвонить / ответить');
 const receiver=softbox(.31,.045,.065,-5.12,.833,-2.14,mat(0x323e33),root,.022);target(receiver,'phone','Позвонить / ответить');
 for(const x of [-5.24,-5.0])softbox(.069,.065,.079,x,.811,-2.14,mat(0x323e33),root,.025);
 cyl(.063,.063,.007,-5.12,.805,-2.045,mat(0xc5bca2));
 for(let i=0;i<8;i++){const a=i/8*Math.PI*2;cyl(.011,.011,.009,-5.12+Math.sin(a)*.04,.795,-2.05+Math.cos(a)*.04,mat(0x343f32));}
 lamp(-5.48,1.91,-3.05);lamp(-1.0,1.86,.99);
 // Wall carpet and low bookcase.
 const wallRug=box(.018,1.28,2.5,-5.87,1.6,-.3,mat(0xc4a982,materials.rug));
 const shelf=new T.Group();shelf.position.set(-1.5,0,1.21);root.add(shelf);
 for(let y of [.1,.53,.98,1.45])box(1.7,.055,.34,0,y,0,darkWood,shelf);
 for(let x of [-.84,.84])box(.065,1.49,.34,x,.75,0,wood,shelf);
 for(let row=0;row<3;row++)for(let i=0;i<15;i++)book(-.7+i*.09,.29+row*.44,.0,.06+rand()*.03,.22+rand()*.12,.23,[0x76644b,0x67433a,0x64705c,0x9b8a64][i%4],shelf);
 collider(-1.5,1.21,1.7,.34);picture(-3.1,1.72,1.45,.64,.5,Math.PI);picture(-4.1,1.66,1.45,.4,.57,Math.PI);
 // Replace the layout meshes with authored CC0 furniture and proper UV materials.
 const loader=new GLTFLoader();
 async function furniture(id,x,y,z,scale,rotation=0){const data=await loader.loadAsync('./assets/'+id+'/model.gltf');const obj=data.scene;obj.position.set(x,y,z);obj.scale.set(...scale);obj.rotation.y=rotation;
 obj.traverse(o=>{if(o.isMesh){o.castShadow=true;o.receiveShadow=true;if(o.material.map)o.material.map.anisotropy=4;o.material.envMapIntensity=.1;}});root.add(obj);return obj;}
 const replacedTable=root.children.filter(o=>o===table||(o.isMesh&&o.position.y===.24&&[-4.12,-2.98].includes(o.position.x)));
 replacedTable.forEach(o=>o.visible=false);sofa.visible=false;shelf.visible=false;
 // The imported television contains its own curved glass and controls.
 tv.children.forEach(o=>o.visible=false);
 const models=await Promise.all([
 furniture('Sofa_01',-5.25,0,-.35,[1.5,1.16,1.35],Math.PI/2),
 furniture('CoffeeTable_01',-3.55,0,-1.35,[.94,1.04,.87]),
 furniture('Television_01',-.28,.68,-3.1,[1.4,1.4,1.4],-Math.PI/2),
 furniture('vintage_cabinet_01',-1.45,0,1.14,[.77,.79,.68],Math.PI)
 ]);
 models[2].traverse(o=>{if(o.isMesh)target(o,'tv','Телевизор');});
 // Weak CRT phosphor glow laid over the curved screen; the original body remains visible.
 screen.visible=true;screen.geometry.dispose();screen.geometry=new T.PlaneGeometry(.56,.39);screen.position.set(-.061,-.08,.293);screen.material.transparent=true;screen.material.opacity=.64;
 tvLight.intensity=.65;tvLight.position.set(-.85,1.13,-3.1);
 picture(-.75,1.7,-4.81,.52,.4);
 
 // A small wall calendar, written on instead of a perfectly empty wall.
 const calCanvas=document.createElement('canvas');calCanvas.width=256;calCanvas.height=320;const cg=calCanvas.getContext('2d');cg.fillStyle='#cfcbb8';cg.fillRect(0,0,256,320);cg.fillStyle='#765749';cg.fillRect(0,0,256,65);cg.fillStyle='#dfd2b4';cg.font='24px Georgia';cg.fillText('НОЯБРЬ',50,43);cg.font='16px Georgia';cg.fillStyle='#535748';for(let i=0;i<30;i++)cg.fillText(String(i+1),22+(i%7)*32,112+Math.floor(i/7)*37);const ct=new T.CanvasTexture(calCanvas);ct.colorSpace=T.SRGBColorSpace;box(.27,.34,.007,.393,1.7,-1.43,mat(0xe2dbcb,ct)).rotation.y=-Math.PI/2;
 await furniture('potted_plant_02',-4.15,.92,-4.73,[.47,.47,.47]);
 // Kitchen: tiled splashback, cupboards, enamel stove and table.
 const tileTex=materials.tile.clone();tileTex.needsUpdate=true;tileTex.repeat.set(4,1.3);
 box(.025,.71,2.5,5.88,1.24,-3.19,mat(0xc4c6b8,tileTex));
 for(let x of [3.02,3.9,4.78,5.61]){
   softbox(.78,.82,.58,x,.43,-4.45,mat(0xb5b5a7));if(x!==3.9)softbox(.8,.055,.63,x,.875,-4.42,mat(0xb8b9ac),root,.018);
   softbox(.7,.65,.025,x,.44,-4.143,mat(0xbebfb1),root,.009);box(.19,.025,.035,x,.66,-4.12,brass);
 }
 collider(4.2,-4.45,3.5,.65);
 // Recessed sink: opening in the counter, curved bowl and bent metal tap.
 const metal=mat(0x87918e,null,{metalness:.35,roughness:.36,side:T.DoubleSide});
 for(const xx of [3.55,4.25])softbox(.1,.055,.63,xx,.875,-4.42,mat(0xb8b9ac),root,.009);
 for(const zz of [-4.68,-4.17])softbox(.62,.055,.12,3.9,.875,zz,mat(0xb8b9ac),root,.009);
 const bowlVertices=[],bowlUV=[],bowlIndices=[];
 for(let ring=0;ring<4;ring++)for(let i=0;i<48;i++){const a=i/48*Math.PI*2,cs=Math.cos(a),sn=Math.sin(a),ww=[.31,.28,.25,.18][ring],dd=[.20,.17,.145,.085][ring];bowlVertices.push(3.9+Math.sign(cs)*Math.pow(Math.abs(cs),.45)*ww,[.915,.924,.894,.774][ring],-4.43+Math.sign(sn)*Math.pow(Math.abs(sn),.45)*dd);bowlUV.push(i/48,ring/3);}
 for(let r=0;r<3;r++)for(let i=0;i<48;i++){const a=r*48+i,b=r*48+(i+1)%48;bowlIndices.push(a,b,a+48,b,b+48,a+48);}
 const sinkGeo=new T.BufferGeometry();sinkGeo.setAttribute('position',new T.Float32BufferAttribute(bowlVertices,3));sinkGeo.setAttribute('uv',new T.Float32BufferAttribute(bowlUV,2));sinkGeo.setIndex(bowlIndices);sinkGeo.computeVertexNormals();const sinkMesh=new T.Mesh(sinkGeo,metal);sinkMesh.receiveShadow=true;root.add(sinkMesh);
 softbox(.365,.008,.175,3.9,.773,-4.43,metal,root,.03);cyl(.027,.027,.002,3.9,.779,-4.43,mat(0x3e4945));
 const tapCurve=new T.CatmullRomCurve3([new T.Vector3(3.92,.916,-4.65),new T.Vector3(3.92,1.11,-4.65),new T.Vector3(3.92,1.19,-4.6),new T.Vector3(3.92,1.19,-4.49),new T.Vector3(3.92,1.14,-4.45)]);
 const faucet=new T.Mesh(new T.TubeGeometry(tapCurve,24,.016,10,false),metal);faucet.castShadow=true;root.add(faucet);
 for(const xx of [3.81,4.03]){cyl(.035,.045,.048,xx,.944,-4.65,metal);box(.085,.013,.023,xx,.974,-4.65,cream);}
 hotspot(.65,.5,.5,3.92,1.1,-4.43,'tap','Проверить кран');
 // Stove near the side wall, with kettle.
 await furniture('electric_stove',5.55,0,-3.49,[1.25,1.04,1]);
 await furniture('vintage_electric_kettle',5.5,.91,-4.42,[.82,.82,.82],-.4);collider(5.55,-3.49,.66,.63);
 softbox(.72,1.78,.69,5.5,.9,-.23,mat(0xb4b9a5),root,.055);softbox(.67,1.3,.07,5.5,.71,.134,mat(0xc3c6b6),root,.03);softbox(.67,.32,.07,5.5,1.56,.134,mat(0xc3c6b6),root,.025);softbox(.023,.42,.05,5.23,1.07,.187,brass,root,.01);collider(5.5,-.23,.72,.69);
 // Fridge magnets and a child's drawing.
 box(.23,.31,.004,5.52,1.3,.18,mat(0xd4c7a0));box(.06,.05,.015,5.49,1.43,.19,mat(0x8e6946));
 for(let z of [-2.52,-1.7]){softbox(.29,.65,.72,5.72,1.94,z,wood);softbox(.03,.58,.64,5.55,1.94,z,mat(0xbdbbaa),root,.01);box(.025,.12,.03,5.52,1.9,z-.23,brass);}
 const kitchenTable=box(1.05,.06,.8,4.18,.75,-1.56,wood);box(1.12,.015,.85,4.18,.79,-1.56,mat(0xe1c9a0,materials.lace));for(let x of [3.78,4.58])for(let z of [-1.84,-1.27])cyl(.026,.036,.72,x,.36,z,darkWood);collider(4.18,-1.56,1.05,.8);
 const bowl=cyl(.16,.095,.07,4.28,.84,-1.62,mat(0xb69e6b));for(let i=0;i<5;i++)sphere(.055,4.18+(i%3)*.065,.91,-1.62+Math.floor(i/3)*.07,mat(0x9b7033));
 for(let z of [-2.25,-.8]){await furniture('SchoolChair_01',4.2,0,z,[.8,.85,.8],z<-1.5?0:Math.PI);collider(4.2,z,.4,.43);}
 await furniture('wall_clock',5.86,2.08,-.77,[1,1,1],-Math.PI/2);
 // Kitchen pendant is the only light that survives the attack.
 cyl(.008,.008,.39,4.05,2.47,-2.05,darkWood);const pendant=cyl(.1,.28,.23,4.05,2.19,-2.05,mat(0xd5b875,null,{emissive:0x9b6024,emissiveIntensity:.18}));
 const kitchenLight=new T.SpotLight(0xffd6a6,16,7,1.43,.68,1.8);kitchenLight.target.position.set(4.05,0,-2.05);root.add(kitchenLight.target);kitchenLight.position.set(4.05,2.04,-2.05);root.add(kitchenLight);kitchenLight.castShadow=true;kitchenLight.shadow.mapSize.set(512,512);kitchenLight.shadow.bias=-.002;kitchenLight.shadow.normalBias=.025;
 const radio=box(.31,.21,.13,3.01,1.01,-4.37,mat(0x715d3e));target(radio,'radio','Кухонное радио');for(let i=0;i<9;i++)box(.01,.11,.008,2.89+i*.018,1.01,-4.299,mat(0x434837));sphere(.019,3.12,1.01,-4.3,brass);
 // Hinged kitchen door. Collider switches with its fully open/closed state.
 const doorPivot=new T.Group();doorPivot.userData.dynamic=true;doorPivot.position.set(2.3,0,-2.07);root.add(doorPivot);
 const kitchenDoor=box(.07,2.16,1.42,0,1.08,.71,mat(0xa79061),doorPivot);target(kitchenDoor,'kitchen-door','Закрыть дверь кухни',2.8);
 for(const yy of [.57,1.62])box(.077,.72,1.11,0,yy,.71,mat(0xb8a37c),doorPivot);
 const handle=box(.12,.035,.13,-.065,1.02,1.26,brass,doorPivot);target(handle,'kitchen-door','Дверь кухни',2.8);
 // Initial closed kitchen is deliberately established before the danger.
 let doorClosed=true,doorAngle=0;const doorCollider=collider(2.3,-1.36,.09,1.42);
 // Hall: coat hooks, mirror, shoes. Entrance stays locked, chain conversation.
 box(.05,.19,1.1,2.18,1.85,3.9,wood);for(let z of [3.5,3.82,4.15]){box(.07,.08,.03,2.12,1.8,z,brass);box(.13,.63,.28,2.05,1.36,z,mat(z<3.7?0x4e5c50:0x77705b));}
 box(.52,.045,1.0,.84,.35,3.7,wood);for(let z of [3.35,3.62,3.9])sphere(.1,.91,.09,z,mat(0x494437),root,1.5,.5,.8);
 const entryPivot=new T.Group();entryPivot.userData.dynamic=true;entryPivot.position.set(0,0,0);root.add(entryPivot);
 const frontDoor=box(1.15,2.27,.07,1.39,1.135,4.86,darkWood,entryPivot);target(frontDoor,'front-door','Послушать у двери / ответить',2.2);
 box(.9,1.86,.017,1.39,1.12,4.81,mat(0x6a6045),entryPivot);sphere(.015,1.38,1.61,4.786,brass,entryPivot);
 hotspot(.16,.16,.04,1.38,1.61,4.76,'peephole','Посмотреть в глазок',1.5);
 box(.14,.045,.06,1.78,1.0,4.77,brass,entryPivot);
 // Interior entry wall covers rest of back wall, door target in front of it.
 const hallLight=new T.PointLight(0xe8bb78,7,5);hallLight.position.set(1.4,2.35,1.5);root.add(hallLight);lamps.push(hallLight);
 sphere(.14,1.4,2.52,1.5,mat(0xd1be89,null,{emissive:0x9f6724,emissiveIntensity:.4}),root,1,.5,1);
 picture(.59,1.63,2.75,.45,.67,Math.PI/2);
 // Bedroom, kept as an optional quiet place.
 box(1.5,.34,2.2,4.7,.27,3.39,darkWood);box(1.44,.16,2.13,4.7,.52,3.36,mat(0xb7b29b,materials.cloth));
 box(1.47,.03,1.4,4.7,.62,3.75,mat(0x8b8970,materials.cloth));box(1.58,.8,.09,4.7,.7,2.27,wood);
 for(const x of [4.35,5.05])sphere(.3,x,.68,2.57,mat(0xd3c6a4,materials.cloth),root,1.05,.3,.7);collider(4.7,3.39,1.55,2.25);
 box(.62,.64,.48,3.48,.32,2.35,wood);lamp(3.47,1.4,2.37);collider(3.48,2.35,.62,.48);
 box(.6,2.2,1.5,5.59,1.1,1.28,darkWood);box(.025,1.95,.65,5.28,1.12,1.27,wood);collider(5.59,1.28,.6,1.5);
 picture(4.54,1.83,.44,.65,.47);
 // Back corridor revealed by impossible displacement after survival.
 const extension=new T.Group();root.add(extension);extension.visible=false;
 for(const x of [.49,2.31])box(.14,2.7,7.2,x,1.35,8.55,wallpaper,extension);
 box(1.82,.1,7.2,1.4,-.01,8.55,floorMat,extension);box(1.82,.1,7.2,1.4,2.7,8.55,cream,extension);
 box(1.82,2.7,.1,1.4,1.35,12.1,darkWood,extension);
 const endLight=new T.PointLight(0x8da78a,4,9);endLight.position.set(1.4,2.2,10);extension.add(endLight);
 // Exterior geometry lies four floors below. Fog obscures distant details.
 const outdoors=new T.Group();scene.add(outdoors);
 box(250,.3,250,0,-12.5,0,mat(0x4d5b59),outdoors);
 box(110,.02,9,0,-12.33,-21,mat(0x344341),outdoors);
 // Dirty thawed snow islands, flat and muted.
 for(let i=0;i<32;i++){const s=new T.Mesh(new T.CircleGeometry(1+rand()*3,8),mat(0x7a8882));s.rotation.x=-Math.PI/2;s.position.set(rand()*90-45,-12.32,-8-rand()*65);s.scale.x=1.5+rand();outdoors.add(s);}
 const facadeMat=new T.MeshBasicMaterial({color:0x7e8582,map:materials.facade}),windowMat=basic(0x29393e),litMat=basic(0x9b947b),frameMat=basic(0x78817b),panelSeam=basic(0x596662),balconyMat=basic(0x627273);
 function building(x,z,w,h,d){
   box(w,h,d,x,h/2-12.4,z,facadeMat,outdoors);
   box(w+.2,.28,d+.2,x,h-12.3,z,panelSeam,outdoors);
   let row=0;
   for(let yy=-10.9;yy<h-13;yy+=2.7){let col=0;for(let xx=x-w/2+1;xx<x+w/2-.5;xx+=2.4){
    const lit=rand()>.84;box(1.22,1.53,.07,xx,yy,z+d/2+.04,frameMat,outdoors);box(1.04,1.35,.075,xx,yy,z+d/2+.05,lit?litMat:windowMat,outdoors);box(.035,1.37,.08,xx-.1,yy,z+d/2+.08,frameMat,outdoors);box(1.3,.1,.3,xx,yy-.78,z+d/2+.16,frameMat,outdoors);
    if(col%4===2){box(1.75,.16,.9,xx,yy-.86,z+d/2+.45,panelSeam,outdoors);box(1.74,.64,.055,xx,yy-.48,z+d/2+.89,balconyMat,outdoors);for(let bx of [-.84,.84])box(.045,.7,.9,xx+bx,yy-.48,z+d/2+.45,frameMat,outdoors);box(1.8,.04,.08,xx,yy-.13,z+d/2+.9,frameMat,outdoors);}
    col++;
   }box(w,.035,.04,x,yy-1.05,z+d/2+.06,panelSeam,outdoors);row++;}
   for(let xx=x-w/2;xx<x+w/2;xx+=3)box(.028,h,.03,xx,h/2-12.4,z+d/2+.075,panelSeam,outdoors);
 }
 building(-5,-43,32,14.3,8);building(31,-52,23,25,12);building(-42,-69,34,21,9);building(8,-89,37,26,10);
 // Flanking our own building gives the window depth and reinforces the height.
 box(12,12,.2,0,-6,-4.05,mat(0x737b6d),outdoors);
 function branch(a,b,r){const v=new T.Vector3().subVectors(b,a),o=new T.Mesh(new T.CylinderGeometry(r*.55,r,v.length(),5),mat(0x35453e));o.position.copy(a).add(b).multiplyScalar(.5);o.quaternion.setFromUnitVectors(new T.Vector3(0,1,0),v.normalize());outdoors.add(o);}
 for(const [x,z] of [[-12,-20],[10,-25],[19,-16],[-24,-27]]){
   branch(new T.Vector3(x,-12,z),new T.Vector3(x+.3,-3,z),.16);
   for(let i=0;i<14;i++){const a=rand()*Math.PI*2,y=-10+rand()*6;const end=new T.Vector3(x+Math.sin(a)*(2+rand()*2),y+2.1,z+Math.cos(a)*2);branch(new T.Vector3(x,y,z),end,.055);branch(end,new T.Vector3(end.x+.6,end.y+1,end.z-.3),.022);}
 }
 for(let x of [-17,16]){cyl(.06,.09,6,x,-9.4,-20,mat(0x46544d),outdoors);box(.55,.1,.23,x,-6.4,-20,basic(0xafae81),outdoors);const light=new T.PointLight(0xbfc38d,30,10);light.position.set(x,-6.6,-20);outdoors.add(light);}
 for(let i=0;i<6;i++){const car=new T.Group();car.position.set(-13+i*4.4,-11.8,-29);outdoors.add(car);box(1.6,.65,3,0,0,0,mat([0x5e6661,0x655c51,0x455550][i%3]),car);box(1.4,.55,1.5,0,.55,0,mat(0x485a55),car);for(let x of [-.75,.75])for(let z of [-.9,.9]){const wh=cyl(.27,.27,.14,x,-.3,z,mat(0x252e28),car,10);wh.rotation.z=Math.PI/2;}}
 const snowGeom=new T.BufferGeometry(),snowPos=new Float32Array(600*3);for(let i=0;i<600;i++){snowPos[i*3]=(rand()-.5)*40;snowPos[i*3+1]=rand()*25-13;snowPos[i*3+2]=-6-rand()*35;}snowGeom.setAttribute('position',new T.BufferAttribute(snowPos,3));const snow=new T.Points(snowGeom,new T.PointsMaterial({color:0x9eaaa4,size:.045,transparent:true,opacity:.45,depthWrite:false}));scene.add(snow);
 // Threat is a human-scale, indistinct silhouette; never present during the warm opening.
 const shadow=new T.Group();shadow.userData.dynamic=true;root.add(shadow);shadow.visible=false;const shadowMat=mat(0x111c17,null,{roughness:1});
 sphere(.18,0,1.73,0,shadowMat,shadow,.85,1.14,.9);cyl(.17,.27,1.15,0,1.02,0,shadowMat,shadow,10);for(const x of [-.13,.13])cyl(.07,.07,.6,x,.3,0,shadowMat,shadow,8);for(const x of [-.24,.24])cyl(.05,.07,.75,x,1.05,0,shadowMat,shadow,8);
 // Two local shadow-casting spotlights define the occupied spaces.
 const ambient=new T.HemisphereLight(0x9baeb9,0x665447,.48);scene.add(ambient);
 const outsideLight=new T.DirectionalLight(0x9fb5bd,.35);outsideLight.position.set(-8,15,-18);scene.add(outsideLight);
 const key=new T.SpotLight(0xffd3a5,23,10,1.1,.7,1.6);key.position.set(-5.45,1.72,-3.05);key.target.position.set(-2.9,.15,-1.5);key.castShadow=true;key.shadow.mapSize.set(1024,1024);key.shadow.bias=-.0005;key.shadow.normalBias=.015;root.add(key,key.target);
 const livingFill=new T.PointLight(0xf0c9a5,3.6,7,2);livingFill.position.set(-2.7,2.05,-1.7);root.add(livingFill);lamps.push(livingFill);livingFill.userData.base=3.6;hallLight.userData.base=3.5;
 const windowFill=new T.SpotLight(0x92afbf,15,10,.95,.8,2);windowFill.position.set(-3.3,2.15,-4.72);windowFill.target.position.set(-3.0,.25,-1.5);root.add(windowFill,windowFill.target);
 let dark=false,changed=false,tvOn=true,forecast=false;
 function setDoor(closed){doorClosed=closed;kitchenDoor.userData.label=closed?'Открыть дверь кухни':'Закрыть дверь кухни';}
 function setDark(v){dark=v;shadow.visible=v;}
 function setChanged(v){
   if(changed===v)return;changed=v;extension.visible=v;entryPivot.position.z=v?7:0;
   // Replace full rear wall with two side sections around the new hall opening.
   rearWall.visible=!v;
   const rearCollider=solids.find(c=>c.z===5&&c.w===12);if(rearCollider)rearCollider.disabled=v;
   if(v){collider(.49,8.5,.14,7);collider(2.31,8.5,.14,7);collider(1.4,12.1,1.82,.1);wall(6.5,2.7,.18,-2.75,1.35,5);wall(3.7,2.7,.18,4.15,1.35,5);}
 }
 function canMove(x,z,r=.2){
   if(x<-5.7||x>5.72||z<-4.68||z>(changed?11.7:4.55))return false;
   for(const s of solids){if(s.disabled)continue;if(s===doorCollider){if(!doorClosed)continue;}if(x>s.x-s.w/2-r&&x<s.x+s.w/2+r&&z>s.z-s.d/2-r&&z<s.z+s.d/2+r)return false;}
   return true;
 }
 const tvg=tvCanvas.getContext('2d');let frame=0;
 function update(t,dt,stage){
   doorAngle=T.MathUtils.damp(doorAngle,doorClosed?0:Math.PI/2,9,dt);doorPivot.rotation.y=doorAngle;
   for(const light of lamps)light.intensity=T.MathUtils.damp(light.intensity,dark?.015:light.userData.base||8,3,dt);
   key.intensity=T.MathUtils.damp(key.intensity,dark?0:23,4,dt);ambient.intensity=T.MathUtils.damp(ambient.intensity,dark?.08:.48,3,dt);
   if(shadow.visible){shadow.position.set(1.36,0,4.2-Math.min(6,t%20)*.34);shadow.rotation.y=Math.sin(t)*.04;}
   steam.visible=!dark;steam.children.forEach((o,i)=>{o.position.y=((t*.09+i*.05)%.32);o.position.x=Math.sin(t+i)*.012;});
   for(let i=0;i<600;i++){snowPos[i*3]+=.11*dt;snowPos[i*3+1]-=1.25*dt;if(snowPos[i*3+1]<-13){snowPos[i*3+1]=12;snowPos[i*3]-=2.2;}}snowGeom.attributes.position.needsUpdate=true;
   if(frame++%8===0){
     tvg.fillStyle=tvOn?(dark?'#26312a':'#546354'):'#151e17';tvg.fillRect(0,0,256,192);
     if(tvOn){tvg.fillStyle='#8e9b7f';tvg.fillRect(0,126,256,66);tvg.fillStyle='#bec1a3';tvg.font='12px Georgia';tvg.fillText('ВЕЧЕРНИЙ ЭФИР',16,23);tvg.fillStyle='#a6ad93';tvg.beginPath();tvg.arc(190,64,25,0,Math.PI*2);tvg.fill();tvg.fillStyle='#5f705b';for(let i=0;i<7;i++)tvg.fillRect(i*43,85+Math.sin(i)*12,33,40);tvg.font='13px Georgia';tvg.fillStyle='#283d32';tvg.fillText(forecast?'Завтра всё повторится.':'В области мокрый снег.',15,153);tvg.font='11px Georgia';tvg.fillText(forecast?'Оставайтесь дома.':'Ночью от −1 до +2.',15,174);}
     for(let y=0;y<192;y+=3){tvg.fillStyle='#11180f1a';tvg.fillRect(0,y,256,1);}tvTex.needsUpdate=true;
   }
 }
 addDomesticDetails({root,box,softbox,cyl,sphere,mat,woodMap,cloth:materials.cloth,rug:materials.rug,cream,brass,collider});
 // Merge static meshes by material: keeps the scene responsive with actual furniture.
 function mergeStatic(parent){for(const c of [...parent.children]){if(!c.isMesh&&c.visible&&!c.userData.dynamic)mergeStatic(c);}const groups=new Map();parent.updateMatrixWorld(true);for(const o of [...parent.children]){if(!o.isMesh||!o.visible||o.userData.id||o.userData.dynamic||o.material.transparent)continue;const id=o.material.uuid+'|'+!!o.geometry.index;const list=groups.get(id)||[];list.push(o);groups.set(id,list);}for(const list of groups.values()){if(list.length<3)continue;const gs=list.map(o=>o.geometry.clone().applyMatrix4(o.matrix));const geo=mergeGeometries(gs);if(!geo)continue;const mesh=new T.Mesh(geo,list[0].material);mesh.castShadow=true;mesh.receiveShadow=true;list.forEach(o=>parent.remove(o));parent.add(mesh);gs.forEach(g=>g.dispose());}}
 mergeStatic(root);mergeStatic(outdoors);
 return {root,get shadowsDirty(){return dark||Math.abs(doorAngle-(doorClosed?0:Math.PI/2))>.001;},solids,interactables,canMove,update,setDoor,setDark,setChanged,get doorClosed(){return doorClosed;},kitchenDoor,shadow,
   toggleTV(){tvOn=!tvOn;},setForecast(v){forecast=v;},targets:Object.fromEntries([...interactables].reverse().map(o=>[o.userData.id,o])),T};
}
