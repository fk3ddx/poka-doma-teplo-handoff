import * as THREE from 'three';
import '@fontsource/pt-mono/400.css';
import '@fontsource/pt-serif/400.css';
import '@fontsource/pt-serif/400-italic.css';
import '@fontsource/marck-script/400.css';
import './style.css';
import {Renderer} from './core/renderer.js';
import {Input} from './core/input.js';
import {Collision} from './core/collision.js';
import {Player} from './core/player.js';
import {UI} from './core/ui.js';
import {Interactions} from './core/interact.js';
import {Audio} from './core/audio.js';
import {buildWorld} from './world/world.js';
import {loadModels} from './world/models.js';
import {Lights} from './world/lights.js';
import {setAniso} from './world/materials.js';
import {Game, roomOf} from './game/game.js';
import {Creature} from './game/creature.js';
import {Hands} from './game/hands.js';

const params = new URLSearchParams(location.search);
const $ = id => document.getElementById(id);
const loadPct = $('load-pct');
const setLoad = p => { loadPct.textContent = ` ${Math.round(p * 100)}%`; };

const canvas = $('view');
const renderer = new Renderer(canvas);
setAniso(Math.min(8, renderer.r.capabilities.getMaxAnisotropy()));
const scene = new THREE.Scene();
scene.background = new THREE.Color(0x0b0d10);
scene.fog = new THREE.FogExp2(0x1b1c1f, 0.03);
const camera = new THREE.PerspectiveCamera(68, innerWidth / innerHeight, 0.03, 400);
renderer.setup(scene, camera);

// ---- options
const OPT_KEY = 'atk-zap-options-v1';
const opts = Object.assign({sens: 1, vol: 0.8, bright: 1, vhs: 0.6, quality: 'medium', bob: true}, JSON.parse(localStorage.getItem(OPT_KEY) || '{}'));
if (params.get('q')) opts.quality = params.get('q');
renderer.setQuality(opts.quality);

const input = new Input(canvas);
const col = new Collision();
const player = new Player(camera, input, col);
const ui = new UI();
const audio = new Audio();
const inter = new Interactions(camera, input, ui, col);

await new Promise(r => setTimeout(r, 30));
await loadModels(p => setLoad(p * 0.7));
const world = buildWorld(scene, col);
setLoad(0.85);

const lights = new Lights(scene);
lights.add('living', {pos: new THREE.Vector3(4.7, 2.1, 2.2), intensity: 10, mats: [world.chandelier.mat], on: false});
lights.add('lamp', {pos: world.livingLamp.lightPos, intensity: 9, shadow: true, mats: [world.livingLamp.mat]});
lights.add('fillLiving', {pos: new THREE.Vector3(4.6, 2.3, 2.0), intensity: 1.4, distance: 7, color: 0xffb070, link: 'lamp'});
lights.add('kitchen', {pos: new THREE.Vector3(7.7, 2.3, 1.45), intensity: 12, shadow: true, mats: [world.kitchenLamp.mat], on: false});
lights.add('fillKitchen', {pos: new THREE.Vector3(7.7, 1.2, 1.4), intensity: 1.2, distance: 5, color: 0xffc890, link: 'kitchen'});
lights.add('hall', {pos: new THREE.Vector3(7.7, 2.3, 4.6), intensity: 6, mats: [world.hallLamp.mat]});
lights.add('night', {pos: world.nightLamp.pos, intensity: 2.2, mats: [world.nightLamp.mat]});
lights.add('lampada', {pos: world.lampada.pos, color: 0xff3a1a, intensity: 0.8, distance: 2.6, mains: false, mats: [world.lampada.mat]});
lights.add('landing', {pos: world.landingBulb.pos, color: 0xffc27a, intensity: 4.5, shadow: true, mats: [world.landingBulb.mat], mains: false});

const creature = new Creature(scene);
const hands = new Hands(camera, scene);
const game = new Game({renderer, scene, camera, player, input, ui, audio, inter, world, lights, creature, hands, col});
window.__game = game;

// mirror reflection: bake the hallway once into a cube map
const cubeRT = new THREE.WebGLCubeRenderTarget(128, {type: THREE.HalfFloatType});
const cubeCam = new THREE.CubeCamera(0.05, 30, cubeRT);
cubeCam.position.set(7.3, 1.5, 3.75); scene.add(cubeCam);
let mirrorBaked = false;
for (const m of world.mirrorMats) { m.envMap = cubeRT.texture; m.needsUpdate = true; }

// ---- footsteps by surface
function surfaceAt(x, z) {
  const r = roomOf(x, z);
  if (r === 'living') return (x > 3.65 && x < 5.55 && z > 0.8 && z < 3.6) ? 'carpet' : 'wood';
  if (r === 'bedroom') return 'wood';
  if (r === 'kitchen' || r.startsWith('hall')) return 'lino';
  if (r === 'bath') return 'tile';
  return 'concrete';
}
player.onStep = (p, run) => {
  const s = surfaceAt(p.x, p.z);
  const soft = game.flags.slippers && s !== 'concrete';
  audio.step(new THREE.Vector3(p.x, 0.05, p.z), s, (run ? 0.26 : 0.17) * (soft ? 0.55 : 1));
};

// ---- options UI
function applyOpts() {
  input.sens = opts.sens; audio.setVolume(opts.vol);
  renderer.film.uniforms.uBright.value = opts.bright; renderer.film.uniforms.uVhs.value = opts.vhs;
  player.bobEnabled = opts.bob;
  if (renderer.quality !== opts.quality) renderer.setQuality(opts.quality);
  localStorage.setItem(OPT_KEY, JSON.stringify(opts));
}
const bind = (id, key, parse = parseFloat) => { const el = $(id); if (el.type === 'checkbox') el.checked = opts[key]; else el.value = opts[key]; el.oninput = el.onchange = () => { opts[key] = el.type === 'checkbox' ? el.checked : parse(el.value); applyOpts(); }; };
const qSel = $('opt-quality'); qSel.addEventListener('change', () => { opts.userQuality = true; applyOpts(); });
bind('opt-sens', 'sens'); bind('opt-vol', 'vol'); bind('opt-bright', 'bright'); bind('opt-vhs', 'vhs'); bind('opt-quality', 'quality', v => v); bind('opt-bob', 'bob');
applyOpts();

// ---- menus
let started = false, optionsFrom = null;
const show = (id, on) => $(id).classList.toggle('visible', on);
function startGame() {
  show('menu', false); started = true;
  audio.start(); audio.setVolume(opts.vol);
  input.lock();
  game.newGame();
}
$('btn-start').onclick = startGame;
if (localStorage.getItem('atk-zap-checkpoint-v1')) { $('btn-continue').classList.remove('hidden'); $('btn-continue').textContent = 'Продолжить с 21:59'; }
$('btn-continue').onclick = () => { show('menu', false); started = true; audio.start(); audio.setVolume(opts.vol); input.lock(); game.continueFromCheckpoint(); };
$('btn-resume').onclick = () => { input.lock(); };
$('btn-restart').onclick = () => { show('pause', false); input.lock(); game.paused = false; audio.ctx?.resume(); for (const k of Object.keys(audio.loops)) audio.stop(k, 0.05); game.newGame(); };
$('btn-options').onclick = () => { optionsFrom = 'menu'; show('menu', false); show('options', true); };
$('btn-options2').onclick = () => { optionsFrom = 'pause'; show('pause', false); show('options', true); };
$('btn-options-back').onclick = () => { show('options', false); show(optionsFrom, true); };
input.onLockChange = locked => {
  if (!started) return;
  if (locked) { show('pause', false); show('options', false); game.paused = false; audio.ctx?.resume(); }
  else if (game.phase !== 'credits') { show('pause', true); game.paused = true; audio.ctx?.suspend(); }
};
canvas.addEventListener('click', () => { if (started && !input.locked) input.lock(); });
game.onEnd = () => { input.unlock(); started = false; show('menu', true); $('btn-start').textContent = 'Сыграть ещё раз'; };

// ---- debug camera for screenshots (?shot=x,z,yawDeg,pitchDeg[,eyeY])
const shot = params.get('shot');
if (shot) {
  const [x, z, yaw, pitch, y] = shot.split(',').map(Number);
  player.teleport(x, z, yaw * Math.PI / 180, (pitch || 0) * Math.PI / 180);
  if (!isNaN(y)) player.eyeY = y;
  show('menu', false); $('fade').style.opacity = 0;
  world.tv.setMode(params.get('tv') || 'concert');
  lights.set('kitchen', params.get('kl') === '1');
  if (params.get('dark') === '1') { lights.setMains(false); world.tv.power = false; game.setOppositeLights(false); }
  if (params.get('creature')) { const [cx, cz, cr] = params.get('creature').split(',').map(Number); creature.show(cx, 0, cz, cr * Math.PI / 180); }
  if (params.get('hold')) { hands.hold(params.get('hold')); }
  if (params.get('hold') === 'flashlight') { hands.charge = 1; hands.update(0.01, false); }
  hands.blend = 1;
  for (let i = 0; i < 6; i++) world.tv.update(0.2);
  if (params.get('fish') === '1') game.film.uFish.value = 1;
  if (params.get('balc')) world.doors.balcony.snap(+params.get('balc'));
  if (params.get('sit')) { player.sit({pos: new THREE.Vector3(3.33, 0.98, 0.8), yaw: -1.914, pitch: -0.22, yawRange: 1.5}); player.sitT = 1; }
  window.__shotMode = true;
} else {
  // living-room backdrop behind the menu
  world.tv.setMode('concert');
  $('fade').style.opacity = 0;
}
let menuT = 0;
function menuCamera(dt) {
  menuT += dt;
  camera.position.set(6.0 - Math.sin(menuT * 0.05) * 0.25, 1.45, 3.95);
  camera.rotation.set(-0.12, 0.62 + Math.sin(menuT * 0.07) * 0.18, 0, 'YXZ');
}
renderer.markShadows(3);
// warm up shader variants for blackout / flashlight / creature so the first scare doesn't stutter
if (!shot) {
  try {
    creature.show(0, -50, 0); hands.hold('flashlight'); hands.charge = 1; hands.update(0.01, false);
    lights.setMains(false); renderer.r.compile(scene, camera);
    lights.setMains(true); renderer.r.compile(scene, camera);
    creature.hide(); hands.hold(null);
  } catch (e) { console.warn(e); }
}

// ---- loop
let last = performance.now(), t = 0, frames = 0, fpsAcc = 0, fpsN = 0;
function frame(now) {
  const fixed = window.__fixedDt;
  const dt = fixed ?? Math.max(0, Math.min(0.05, (now - last) / 1000)); last = now; t += dt;
  if (!started && !window.__shotMode && game.phase === 'menu') { menuCamera(dt); world.tv.update(dt); lights.update(dt, t); renderer.render(dt, t); if (!mirrorBaked && frames >= 2) { cubeCam.update(renderer.r, scene); mirrorBaked = true; } input.endFrame(); if (++frames === 4) window.__ready = true; requestAnimationFrame(frame); return; }
  if (!game.paused) {
    player.update(dt);
    if (!window.__shotMode) game.update(dt);
    if (!game.peeping && !game.noteOpen && !game.inCall && game.phase !== 'death' && !window.__shotMode) inter.update(); else ui.setPrompt(null);
    lights.update(dt, t);
    for (const d of Object.values(world.doors)) if (d.update(dt)) renderer.markShadows(1, d.pivot.position);
    creature.update(dt, p => audio.step(new THREE.Vector3(p.x, 0.05, p.z), surfaceAt(p.x, p.z), 0.3, true));
    if (creature.visible && (creature.walking || frames % 4 === 0)) renderer.markShadows(1, creature.pos);
    if (window.__shotMode && hands.current === 'flashlight') { hands.charge = 1; hands.blend = 1; }
    hands.update(dt, player.bobAmp > 0.2);
    hands.lap.visible = !!player.seated && (player.sitT ?? 1) > 0.6;
    world.tv.update(dt);
    world.sleet.uniforms.uTime.value = t;
    for (const s of world.chimneyLights) s.material.opacity = (Math.sin(t * 2) > 0 ? 0.85 : 0.15);
    world.landingHalo.visible = lights.isOn('landing');
    if (!lights.mains && lights.fixtures.landing.flicker <= 0 && Math.random() < 0.004) lights.flicker('landing', 0.4 + Math.random() * 0.6);
    world.lampada.flame.scale.set(1 + Math.random() * 0.15, 2 + Math.random() * 0.4, 1);
    if (lights.fixtures.lampada.light.visible) lights.fixtures.lampada.light.intensity = 0.6 * (0.85 + Math.random() * 0.25);
    audio.updateListener(camera);
  }
  // FPS meter (F3) and automatic quality step-down when the machine can't keep up
  fpsAcc += dt; fpsN++;
  if (input.hit('F3')) $('fps').classList.toggle('on');
  if (fpsAcc >= 3) {
    const fps = fpsN / fpsAcc; $('fps').textContent = `${fps.toFixed(0)} fps · ${renderer.quality}`;
    if (started && !game.paused && !opts.userQuality && fps < 32 && renderer.quality !== 'low') {
      opts.quality = renderer.quality === 'high' ? 'medium' : 'low'; applyOpts(); qSel.value = opts.quality;
      ui.hint('Качество снижено для плавности (Настройки → Качество)', 4000);
    }
    fpsAcc = 0; fpsN = 0;
  }
  renderer.render(dt, t);
  if (!mirrorBaked && frames === 2) { cubeCam.update(renderer.r, scene); mirrorBaked = true; }
  input.endFrame();
  if (++frames === 4) window.__ready = true;
  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);

setLoad(1);
$('loading').classList.add('hidden');
$('menu-buttons').classList.remove('hidden');
// debug helpers
if (params.get('debug') === '1') {
  addEventListener('keydown', e => {
    if (e.code === 'F6') game.flags = {...game.flags, noteRead: true, slippers: true, coat: true, call0: true, tea: true, tv: true, blink: true, matches: true, flashlight: true, sitObjective: true};
    if (e.code === 'F7') { game.phase = 'warm'; game.startEvent(true); }
    if (e.code === 'F8') game.die('look');
  });
}
