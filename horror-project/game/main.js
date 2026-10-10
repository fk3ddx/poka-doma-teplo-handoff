import * as THREE from './vendor/three.module.js';
import {buildWorld} from './world.js';
import {Story} from './story.js';
import {Sound} from './audio.js';
import {EffectComposer} from './vendor/addons/postprocessing/EffectComposer.js';
import {RenderPass} from './vendor/addons/postprocessing/RenderPass.js';
import {SSAOPass} from './vendor/addons/postprocessing/SSAOPass.js';
import {UnrealBloomPass} from './vendor/addons/postprocessing/UnrealBloomPass.js';
import {ShaderPass} from './vendor/addons/postprocessing/ShaderPass.js';
import {VHSShader} from './vhs.js';
import {OutputPass} from './vendor/addons/postprocessing/OutputPass.js';
const $=id=>document.getElementById(id);
const SAVE='warm-home-prologue-v1',OPTIONS='warm-home-options-v1';
const readJSON=k=>{try{return JSON.parse(localStorage.getItem(k));}catch{return null;}};
const writeJSON=(k,v)=>{try{localStorage.setItem(k,JSON.stringify(v));}catch{/* Private browsing can disallow storage; the game still runs. */}};
let renderer,scene,camera,world,composer,ssao,vhs,story=new Story(),sound=new Sound();
let mode='menu',settingsFrom='menu',yaw=-.78,pitch=-.045,keys={},target=null,started=false;
let gameTime=0,last=performance.now(),stepTime=0,subtitleTime=0,transition=null,dialogLines=[],dialogDone=null,drag=false;
let peeping=0,miniTime=0,miniX=240,miniScore=0,miniStars=[],lastStage='warm';
let seated=true,captureRequested=false;
const opts={volume:.5,sensitivity:1,brightness:1,quality:'high',grain:false,vhs:true,bob:false,...(readJSON(OPTIONS)||{})};
const position=new THREE.Vector3(-4.02,1.58,.15),raycaster=new THREE.Raycaster();
function line(text,time=7){$('subtitle').textContent=text;subtitleTime=time;}
function unlock(){if(document.pointerLockElement)document.exitPointerLock();drag=false;keys={};}
function lock(){const p=$('game').requestPointerLock?.();p?.catch?.(()=>{});}
function setMode(next){mode=next;for(const id of ['menu','pause','settings','reader','dialog','handheld','ending'])$(id).hidden=id!==next;$('hud').hidden=['menu','settings','ending'].includes(next);if(next==='play'){sound.resume();lock();}else{unlock();if(['pause','menu','settings'].includes(next))sound.pause();}target=null;$('prompt').style.display='none';}
function applyOptions(){
 for(const [k,v] of Object.entries(opts)){const el=$(k);if(el.type==='checkbox')el.checked=v;else el.value=v;}
 sound.setVolume(Number(opts.volume));$('film').style.display=opts.grain?'':'none';
 if(vhs)vhs.enabled=opts.vhs;
 if(renderer){renderer.toneMappingExposure=Number(opts.brightness)*1.12;resize();}writeJSON(OPTIONS,opts);
}
function resize(){if(!renderer)return;const w=innerWidth,h=innerHeight,max=opts.quality==='low'?900:1440,scale=Math.min(1,max/w);renderer.setSize(Math.round(w*scale),Math.round(h*scale),false);composer?.setSize(Math.round(w*scale),Math.round(h*scale));if(vhs)vhs.uniforms.resolution.value.set(Math.round(w*scale),Math.round(h*scale));camera.aspect=w/h;camera.updateProjectionMatrix();}
function save(){writeJSON(SAVE,story.checkpoint());$('save-hint').textContent='Вечер сохранён';setTimeout(()=>{$('save-hint').textContent='';},1800);}
function resetPose(){seated=true;position.set(-4.72,1.17,.06);yaw=-.88;pitch=-.08;}
function begin(useSaved=false){
 story=new Story(useSaved?readJSON(SAVE):null);lastStage=story.stage;started=true;gameTime=0;transition=null;peeping=0;
 $('fade').style.opacity=0;document.body.classList.remove('peep');resetPose();
 world.setDoor(true);world.setDark(false);world.setForecast(false);world.setChanged(['after','free'].includes(story.stage));
 sound.dark=false;sound.start();setMode('play');save();
 if(story.stage!=='warm'){seated=false;position.set(-2.35,1.58,-.38);}
 if(story.stage!=='warm')line(story.loop?'Я уже прожил этот вечер. Теперь нужно вспомнить, как остаться в живых.':'Я остался на пятом этаже. Вечер ещё не закончился.');
}
function reader(title,body,type='ЗАПИСКА НА СТОЛЕ'){$('reader-title').textContent=title;$('reader-body').textContent=body;$('reader-type').textContent=type;setMode('reader');}
function dialog(name,lines,done){dialogLines=[...lines];dialogDone=done||null;$('dialog-name').textContent=name;setMode('dialog');nextDialog();}
function nextDialog(){if(dialogLines.length){$('dialog-text').textContent=dialogLines.shift();}else{const cb=dialogDone;dialogDone=null;if(cb)cb();else setMode('play');}}
function processEvents(){for(const event of story.drain()){
 switch(event.type){
 case 'save':save();break;
 case 'stage':lastStage=event.stage;break;
 case 'line':line(event.text,8);break;
 case 'bell':sound.bell();line(story.stage==='neighbor'?'Звонок в дверь. Поздновато для гостей.':'Опять звонок. Соседка уже ушла.',5);break;
 case 'phone-ring':sound.phone();if(story.stage==='warm'&&!story.flags.phone)line('Телефон рядом с диваном. Наверное, бабушка.',5);break;
 case 'coffee':sound.tone(250,.16,.018);line('Ещё тёплый. И сахара опять положила за двоих.');break;
 case 'note':reader('Оставайся, если снег пойдёт.',
 'Кофе в термосе, печенье на кухне. Цветы я уже полила — забыла тебе сказать.\n\nНе торопись домой. Постель в маленькой комнате свежая.\n\nДверь кухни закрывай, когда радио играет. А то я его через всю квартиру слышу.\n\nИ позвони, как устроишься.\n\nОбнимаю. Бабушка.');break;
 case 'window':line(story.stage==='warm'?'Пятый этаж. Внизу снег превращался в воду ещё до того, как касался асфальта.\nНапротив светились три чужих окна.':'Двор тот же. Даже машина не успела проехать.');break;
 case 'call-home':dialog('Бабушка · телефон',[
  '— Алло? Добрался? Я тебе говорила, ключ под синей кружкой. Нашёл?',
  '— Да, я уже сижу. Тут хорошо. А ты как?',
  '— У Нины. Сериал смотрим. Ты поешь там чего-нибудь, кофе — это не еда.',
  'За трубкой тихо смеялись. Кто-то попросил прибавить телевизор.',
  '— Снег у вас тоже? Оставайся, конечно. В шкафу одеяло. Я завтра после обеда буду.',
  '— Спокойной ночи, ба.',
  '— И тебе. Только дверь на цепочку. Люблю тебя.'
 ]);break;
 case 'neighbor-talk':sound.door();dialog('За дверью · соседка снизу',[
  'Я оставил цепочку. На площадке стояла соседка в домашнем халате.',
  '— Извини, что поздно. У вас вода нигде не течёт? У меня по трубе так шумит, будто ванну набирают.',
  '— Нет вроде. Сейчас посмотрю.',
  '— И ещё… стулья не двигайте, пожалуйста. Надо мной уже полчаса кто-то ходит.',
  '— Я всё это время сидел. Может, выше?',
  'Она посмотрела на потолок. Потом на меня.',
  '— Вы же на пятом. Выше только крыша.',
  'Снизу хлопнула дверь. На площадке снова стало тихо.'
 ]);break;
 case 'tap-wrong':line('Кран не капал. Труба была совершенно холодной.\nНо шум шёл откуда-то выше.',8);break;
 case 'tap':line('Вода холодная. Кран закрыт.');sound.tone(340,.3,.02,'triangle');break;
 case 'dead-line':line('В трубке только моё дыхание. С небольшой задержкой.');sound.tone(430,1,.045);break;
 case 'locked':line(story.stage==='warm'?'За дверью холодный подъезд. Здесь лучше.':'Цепочка на месте. Я никому не открою.');break;
 case 'door-voice':dialog('По другую сторону двери',[
  '— Это я. Открой, пожалуйста. Я весь промок.',
  'Пауза. Потом тот же голос, слово в слово.',
  '— Это я. Открой, пожалуйста. Я весь промок.',
  'Я не ответил. В замке что-то тихо повернулось.'
 ]);break;
 case 'door-attack':line('Ручка холодная. Не нужно подходить ближе.');break;
 case 'forecast':world.setForecast(true);sound.music=false;line('Телевизор: «Завтра всё повторится. Оставайтесь дома».',7);break;
 case 'pipe':sound.pipe();line(story.elapsed<51?'В трубе щёлкнуло.':'Второй щелчок. Совсем рядом.',4);break;
 case 'footsteps':sound.step();sound.tone(67,1.6,.13);break;
 case 'attack-start':world.setDark(true);sound.dark=true;sound.tone(39,4,.14);line('Свет погас. Только на кухне ещё играло радио.',6);break;
 case 'death':
  // A finite fade and rewind, with no redoing the long introductory chores.
  transition={kind:'death',t:0};unlock();$('fade').style.opacity=1;sound.tone(48,2,.2,'triangle');
  line('Я услышал свой голос совсем близко.\nА потом не осталось ничего.',5);break;
 case 'survived':world.setDark(false);sound.dark=false;sound.music=true;world.setChanged(true);world.setForecast(false);line('Оно прошло мимо.\nТёплый свет всё ещё лежал на столе.',7);save();break;
 case 'ending':dialog('Бабушка · телефон',[
  '— Алло? Добрался? Я тебе говорила, ключ под синей кружкой. Нашёл?',
  'Те же слова. Та же интонация. Даже телевизор за её спиной засмеялся в том же месте.',
  '— Ба… ты ведь уже звонила.',
  '— Нет, родной. Я только сейчас собралась.',
  'Я посмотрел в коридор. До входной двери стало на семь шагов больше.',
  'В этот раз я запомнил всё.'
 ],()=>setMode('ending'));break;
 }
}}
function interact(id){
 if(transition)return;
 if(id==='kitchen-door'){world.setDoor(!world.doorClosed);sound.door();return;}
 if(id==='handheld'){setMode('handheld');miniScore=0;miniTime=0;miniStars=[];miniX=240;return;}
 if(id==='tv'){world.toggleTV();line('Старый телевизор тихо потрескивал.');return;}
 if(id==='radio'){sound.music=!sound.music;line(sound.music?'Знакомая мелодия. На кухне всегда было теплее.':'Я сделал радио потише.');return;}
 if(id==='peephole'){
  sound.tone(120,.1,.02);peeping=2.6;document.body.classList.add('peep');
  if(story.stage==='neighbor'){dialog('Дверной глазок',['Соседка с четвёртого этажа. Халат, тапочки, ключи в руке. Кажется, она не собирается уходить.','Можно ответить ей через дверь.']);}
  else if(['warning','attack'].includes(story.stage)){line('На площадке стоял я.\nСмотрел точно в глазок. Не моргал.',7);}
  else line('Пустая площадка. Лампочка над лестницей мигает.');return;
 }
 story.interact(id);processEvents();
}
function findTarget(){
 raycaster.setFromCamera(new THREE.Vector2(0,0),camera);
 const hits=raycaster.intersectObjects(world.root.children,true);
 let blocker=Infinity;
 for(const hit of hits){
  const o=hit.object;if(!o.visible)continue;
  let p=o,visible=true;while(p){if(!p.visible)visible=false;p=p.parent;}if(!visible)continue;
  if(o.userData.id&&hit.distance<=o.userData.range&&hit.distance<=blocker+.13)return o;
  if(!(o.material?.transparent&&o.material.opacity<.2)&&hit.distance<blocker)blocker=hit.distance;
 }
 return null;
}
function move(dt){
 let mx=(keys.KeyD?1:0)-(keys.KeyA?1:0),mz=(keys.KeyW?1:0)-(keys.KeyS?1:0);
 if(seated&&(mx||mz)){seated=false;position.set(-4.31,1.58,.2);}
 if(mx||mz){const len=Math.hypot(mx,mz),speed=1.75;mx/=len;mz/=len;const dx=(Math.cos(yaw)*mx-Math.sin(yaw)*mz)*speed*dt,dz=(-Math.sin(yaw)*mx-Math.cos(yaw)*mz)*speed*dt;
  if(world.canMove(position.x+dx,position.z))position.x+=dx;
  if(world.canMove(position.x,position.z+dz))position.z+=dz;
  stepTime+=dt;if(stepTime>.53){stepTime=0;sound.step();}
 }
 camera.position.set(position.x*.78,position.y,position.z*.78);camera.position.y=(seated?1.17:1.58)+(opts.bob&&(mx||mz)?Math.sin(gameTime*9)*.018:0);
 camera.rotation.set(pitch,yaw,0,'YXZ');
}
function updateMini(dt){
 miniTime+=dt;miniX+=((keys.ArrowRight||keys.KeyD?1:0)-(keys.ArrowLeft||keys.KeyA?1:0))*230*dt;miniX=Math.max(20,Math.min(460,miniX));
 if(miniStars.length<5&&Math.random()<dt*2)miniStars.push({x:20+Math.random()*440,y:30,speed:30+Math.random()*35});
 const g=$('mini').getContext('2d');g.fillStyle='#1d3740';g.fillRect(0,0,480,272);g.fillStyle='#59777a';for(let i=0;i<28;i++)g.fillRect((i*89)%480,(i*41)%190,2,2);
 g.fillStyle='#b3c5aa';g.font='14px monospace';g.fillText('ЗВЁЗДЫ В КАРМАНЕ',20,25);g.fillText(String(miniScore).padStart(2,'0'),430,25);
 for(const s of miniStars){s.y+=s.speed*dt;g.fillStyle='#d9c987';g.fillRect(s.x-3,s.y-7,6,14);g.fillRect(s.x-7,s.y-3,14,6);if(s.y>228&&s.y<249&&Math.abs(s.x-miniX)<28){miniScore++;s.y=300;sound.tone(640+miniScore*12,.3,.04);}}
 miniStars=miniStars.filter(s=>s.y<280);g.fillStyle='#98b399';g.fillRect(miniX-24,241,48,6);g.fillRect(miniX-28,232,5,14);g.fillRect(miniX+23,232,5,14);g.fillStyle='#446368';g.fillRect(0,261,480,11);
}
function frame(now){
 requestAnimationFrame(frame);const dt=Math.min((now-last)/1000,.05);last=now;
 if(mode==='play'){
  gameTime+=dt;
  if(transition){transition.t+=dt;if(transition.t>2.6&&!transition.moved){transition.moved=true;seated=false;position.set(-2.35,1.58,-.38);yaw=-1.42;pitch=0;world.setDark(false);world.setDoor(true);world.setForecast(false);sound.dark=false;sound.music=true;line('22:06. Снова.\nЯ помнил, как умер. На кухне свет не погас.',7);}
   if(transition.t>3.5)$('fade').style.opacity=0;
   if(transition.t>5.5){transition=null;lock();save();}
  }else{
   move(dt);story.tick(dt,{inKitchen:position.x>2.55&&position.z<.1,doorClosed:world.doorClosed});processEvents();
   target=findTarget();$('prompt').style.display=target?'block':'none';if(target)$('prompt').innerHTML='<kbd>E</kbd>'+target.userData.label;
  }
  sound.tick(dt,position.x,position.z);
  subtitleTime-=dt;if(subtitleTime<=0)$('subtitle').textContent='';
  if(peeping>0){peeping-=dt;if(peeping<=0)document.body.classList.remove('peep');}
  $('objective').textContent=story.objective();$('time-label').textContent=story.stage==='warm'?'22:06':story.stage==='free'?'22:06':story.loop?'22:06':'22:09';
 }
 if(mode==='handheld'){updateMini(dt);sound.tick(dt);}
 if(mode==='menu'&&!started){camera.position.set(-4.13*.78,1.46,.7*.78);camera.rotation.set(-.09,-.73+Math.sin(now*.00008)*.04,0,'YXZ');}
 if(transition){camera.position.set(position.x*.78,position.y,position.z*.78);camera.rotation.set(pitch,yaw,0,'YXZ');}
 world.update(gameTime,mode==='pause'?0:dt,story.stage);if(world.shadowsDirty)renderer.shadowMap.needsUpdate=true;renderer.info.reset();if(vhs)vhs.uniforms.time.value=now*.001;composer.render();
 if(captureRequested){captureRequested=false;$('game').toBlob(blob=>{const a=document.createElement('a');a.href=URL.createObjectURL(blob);a.download='warm-home-frame.png';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000);});}
}
function action(){if(mode==='reader'){setMode('play');return;}if(mode==='dialog'){nextDialog();return;}if(mode==='handheld'){setMode('play');return;}if(mode==='play'&&target)interact(target.userData.id);}
function bind(){
 $('start').onclick=()=>begin(false);$('continue').onclick=()=>begin(true);
 $('resume').onclick=()=>setMode('play');$('to-menu').onclick=()=>{setMode('menu');$('continue').hidden=!readJSON(SAVE);};
 $('settings-open').onclick=()=>{settingsFrom='menu';setMode('settings');};$('pause-settings').onclick=()=>{settingsFrom='pause';setMode('settings');};$('settings-close').onclick=()=>setMode(settingsFrom);
 $('reader-close').onclick=()=>setMode('play');$('dialog-next').onclick=nextDialog;$('handheld-close').onclick=()=>setMode('play');
 $('ending-explore').onclick=()=>{setMode('play');line('Входная дверь стала дальше. Но за ней всё ещё тот же подъезд.',7);};$('ending-restart').onclick=()=>location.reload();
 for(const k of Object.keys(opts))$(k).addEventListener('input',e=>{opts[k]=e.target.type==='checkbox'?e.target.checked:e.target.type==='range'?Number(e.target.value):e.target.value;applyOptions();});
 addEventListener('resize',resize);
 addEventListener('keydown',e=>{
  if(['KeyW','KeyA','KeyS','KeyD','ArrowLeft','ArrowRight','Space','KeyE','Escape'].includes(e.code))e.preventDefault();
  keys[e.code]=true;if(e.repeat)return;
  if(e.code==='KeyE')action();
  if(e.code==='Escape'){if(mode==='play')setMode('pause');else if(mode==='pause')setMode('play');else if(mode==='reader'||mode==='handheld')setMode('play');else if(mode==='settings')setMode(settingsFrom);}
 });
 addEventListener('keyup',e=>keys[e.code]=false);
 addEventListener('blur',()=>{keys={};if(mode==='play')setMode('pause');});
 document.addEventListener('visibilitychange',()=>{if(document.hidden&&mode==='play')setMode('pause');});
 $('game').addEventListener('mousedown',()=>{if(mode==='play'){drag=true;lock();}});addEventListener('mouseup',()=>drag=false);
 addEventListener('mousemove',e=>{if(mode==='play'&&!transition&&(document.pointerLockElement=== $('game')||drag)){yaw-=e.movementX*.002*opts.sensitivity;pitch=Math.max(-1.25,Math.min(1.25,pitch-e.movementY*.002*opts.sensitivity));}});
 // Browser Escape releases pointer lock before dispatching keydown in some engines.
 document.addEventListener('pointerlockchange',()=>{if(!document.pointerLockElement&&mode==='play'&&!transition)setMode('pause');});
}
async function boot(){
 try{
  renderer=new THREE.WebGLRenderer({canvas:$('game'),antialias:true,powerPreference:'high-performance'});renderer.info.autoReset=false;renderer.setPixelRatio(1);renderer.outputColorSpace=THREE.SRGBColorSpace;renderer.toneMapping=THREE.ACESFilmicToneMapping;renderer.shadowMap.enabled=true;renderer.shadowMap.autoUpdate=false;renderer.shadowMap.needsUpdate=true;renderer.shadowMap.type=THREE.PCFSoftShadowMap;
  scene=new THREE.Scene();scene.background=new THREE.Color(0x46565f);scene.fog=new THREE.FogExp2(0x586970,.025);camera=new THREE.PerspectiveCamera(60,innerWidth/innerHeight,.045,170);camera.rotation.order='YXZ';
  $('start').disabled=true;$('start').textContent='В квартире зажигается свет…';
  world=await buildWorld(scene);
  const rt=new THREE.WebGLRenderTarget(innerWidth,innerHeight,{type:THREE.HalfFloatType,samples:4});composer=new EffectComposer(renderer,rt);
  composer.addPass(new RenderPass(scene,camera));ssao=new SSAOPass(scene,camera,innerWidth,innerHeight);ssao.kernelRadius=9;ssao.minDistance=.0015;ssao.maxDistance=.08;composer.addPass(ssao);
  composer.addPass(new UnrealBloomPass(new THREE.Vector2(innerWidth,innerHeight),.13,.6,.9));composer.addPass(new OutputPass());
  vhs=new ShaderPass(VHSShader);vhs.uniforms.resolution.value=new THREE.Vector2(innerWidth,innerHeight);composer.addPass(vhs);
  $('start').disabled=false;$('start').innerHTML='Остаться на вечер <span>→</span>';
  bind();applyOptions();$('continue').hidden=!readJSON(SAVE);requestAnimationFrame(frame);
  // Explicit opt-in test API. Production play has no shortcuts or hidden timer changes.
  if(new URLSearchParams(location.search).has('debug'))window.__game={
   get state(){return {mode,vhs:opts.vhs,stage:story.stage,elapsed:story.elapsed,flags:{...story.flags},loop:story.loop,position:position.toArray(),doorClosed:world.doorClosed,transition:!!transition,drawCalls:renderer.info.render.calls,triangles:renderer.info.render.triangles};},
   capture(){captureRequested=true;},interact,lookAt(x,y,z){camera.position.set(position.x*.78,position.y,position.z*.78);camera.lookAt(x,y,z);yaw=camera.rotation.y;pitch=camera.rotation.x;},
   pose(x,z,yawValue=0,pitchValue=0){seated=false;position.set(x,1.58,z);yaw=yawValue;pitch=pitchValue;},
   advance(seconds){for(let i=0;i<seconds*20;i++){story.tick(.05,{inKitchen:position.x>2.55&&position.z<.1,doorClosed:world.doorClosed});processEvents();if(transition||mode==='dialog')break;}},
   nextDialog,close(){setMode('play');},get targets(){return Object.fromEntries(Object.entries(world.targets).map(([id,o])=>[id,o.getWorldPosition(new THREE.Vector3()).toArray()]));},
   collision:(x,z)=>world.canMove(x,z),setDoor:v=>world.setDoor(v)
  };
  if(window.__game){const {installQA}=await import('./qa-panel.js');installQA(window.__game);}
 }catch(err){console.error(err);$('boot-error').hidden=false;$('error-message').textContent='Нужен браузер с WebGL 2. '+err.message;}
}
boot();
