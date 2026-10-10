import * as THREE from 'three';
import * as S from './script.js';
import {spots, rooms} from '../world/layout.js';
import {PHASE_LIGHT} from '../world/phases.js';
import {tube} from '../world/geo.js';
import {LAYER_FEED, setLayerDeep} from './scares.js';

const V = (x, y, z) => new THREE.Vector3(x, y, z);
const ARMCHAIR_SEAT = {pos: V(3.33, 0.98, 0.8), yaw: -1.914, pitch: -0.22, yawRange: 1.5};
const TV_POS = V(6.1, 0.85, 1.8);
const PHONE_POS = V(6.82, 0.85, 3.7);
const DOOR_OUT = V(8.05, 1.5, 6.5);

// path graph for the creature
const NODES = {
  balcony: V(5.17, 0, -0.85), balconyDoor: V(5.17, 0, 0.35), living: V(4.7, 0, 2.0), livingS: V(5.0, 0, 3.6), livingDoor: V(5.65, 0, 4.45),
  hallB: V(5.65, 0, 5.25), hallA: V(7.6, 0, 4.9), front: V(8.05, 0, 5.6), kitchenDoor: V(7.25, 0, 2.95), kitchen: V(7.6, 0, 1.6),
  bedroomDoor: V(2.85, 0, 3.75), bedroom: V(1.6, 0, 2.6),
};
const HIDE = {
  wardrobe: {name: 'шифоньер', cam: V(2.56, 1.42, 2.3), yaw: Math.PI / 2, pitch: -0.05, front: V(1.85, 0, 2.3), exit: V(2.0, 0, 2.3), overlay: 'wardrobe'},
  bed: {name: 'кровать', cam: V(0.62, 0.24, 3.15), yaw: -Math.PI / 2, pitch: 0.05, front: V(1.45, 0, 3.0), exit: V(1.4, 0, 3.1), overlay: 'bed'},
  table: {name: 'стол', cam: V(6.95, 0.42, 0.85), yaw: -Math.PI / 2, pitch: 0.0, front: V(7.6, 0, 0.9), exit: V(7.5, 0, 1.7), overlay: 'table'},
};
const EDGES = [['balcony', 'balconyDoor'], ['balconyDoor', 'living'], ['living', 'livingS'], ['livingS', 'livingDoor'], ['livingDoor', 'hallB'], ['hallB', 'hallA'], ['hallA', 'front'], ['hallA', 'kitchenDoor'], ['kitchenDoor', 'kitchen'], ['livingS', 'bedroomDoor'], ['bedroomDoor', 'bedroom'], ['living', 'bedroomDoor']];

export class Game {
  constructor(env) {
    Object.assign(this, env);
    this.timers = []; this.waiters = [];
    this.token = 0;
    this.paused = false;
    this.phase = 'menu';
    this.loop = 0; this.deaths = 0;
    this.flags = {};
    this.clock = 21 * 60 + 34;  // minutes
    this.clockRate = 1 / 15;    // game minutes per real second
    this.eyesClosed = false; this.eyesHeld = 0;
    this.ringing = null;        // 'call0' | 'call1' | 'fake' | 'call2' | 'end' | 'code'
    this.inCall = false;
    this.peeping = false;
    this.kettleT = -1;
    this.creatureActive = false;
    this.film = this.renderer.film.uniforms;
    this.faceLight = new THREE.PointLight(0xffd0b0, 0, 2.5, 2); this.scene.add(this.faceLight);
    this.memory = [];   // hiding places used in earlier loops — the thing remembers them too
    this.hidden = null; this.breath = 1; this.holding = false; this.gaspT = 0;
    // grandpa's belt tying the balcony handle to the radiator riser
    this.beltMesh = new THREE.Mesh(tube([[4.86, 1.05, -0.02], [4.7, 0.95, 0.06], [4.47, 0.72, 0.07]], 0.012, 10, 6), new THREE.MeshStandardMaterial({color: 0x2a1a10, roughness: 0.5}));
    this.beltMesh.visible = false; this.scene.add(this.beltMesh);
    this.setupInteractions();
    this.audio.roomOf = (x, z) => roomOf(x, z);
  }

  // ---------- scheduling ----------
  wait(sec) { const tok = this.token; return new Promise(res => this.timers.push({t: sec, res, tok})); }
  until(fn) { const tok = this.token; return new Promise(res => this.waiters.push({fn, res, tok})); }
  alive(tok) { return tok === this.token; }
  tick(dt) {
    for (let i = this.timers.length - 1; i >= 0; i--) {
      const tm = this.timers[i];
      if (tm.tok !== this.token) { this.timers.splice(i, 1); continue; }
      tm.t -= dt; if (tm.t <= 0) { this.timers.splice(i, 1); tm.res(); }
    }
    for (let i = this.waiters.length - 1; i >= 0; i--) {
      const w = this.waiters[i];
      if (w.tok !== this.token) { this.waiters.splice(i, 1); continue; }
      if (w.fn()) { this.waiters.splice(i, 1); w.res(); }
    }
  }
  abort() { this.token++; this.timers.length = 0; this.waiters.length = 0; }

  // ---------- helpers ----------
  get W() { return this.world; }
  get pos() { return this.player.pos; }
  near(p, r) { return Math.hypot(this.pos.x - p.x, this.pos.z - p.z) < r; }
  lookingAt(p, cos = 0.8) { const f = this.player.forward(); const d = p.clone().sub(this.camera.position).normalize(); return f.dot(d) > cos; }
  room() { return roomOf(this.pos.x, this.pos.z); }
  timeStr(m = this.clock) { const h = Math.floor(m / 60) % 24, mm = Math.floor(m % 60); return `${h}:${String(mm).padStart(2, '0')}`; }
  async say(text, ms) { return this.ui.thought(text, ms); }
  async line(l) {
    const tok = this.token;
    if (l.who) {
      let dur = 1.3 + l.text.length * 0.055;
      const vd = this.audio.voice(l.text, l.strange ? 'strange' : 'phone');
      if (vd) dur = vd + 0.45;
      else if (this.inCall) this.audio.murmur(null, dur * 0.8, {phone: true, pitch: l.strange ? 175 : 215, v: 0.1, strange: l.strange});
      await this.ui.subtitle(l.who, l.text, dur * 1000, l.strange ? 'strange' : '');
    }
    return this.alive(tok);
  }
  async dialogue(lines) {
    const tok = this.token;
    let lastChoice = -1;
    for (const l of lines) {
      if (!this.alive(tok)) return -1;
      if (l.choice) { lastChoice = await this.ui.choose(l.choice); continue; }
      if (l.branch !== undefined && lastChoice !== l.branch) continue;
      if (l.ifLoop && this.loop === 0) continue;
      await this.line(l);
      if (l.cut) { this.audio.busyTone(2.5); break; }
    }
    return lastChoice;
  }
  objective(id, text) { this.ui.addObjective(id, text); }
  done(id) { return this.ui.complete(id); }
  setClock(m) { this.clock = m; }

  // ---------- interactions ----------
  setupInteractions() {
    const I = this.inter, W = this.world, g = this;
    const d = W.doors;
    // generic door toggles
    for (const [name, door] of Object.entries(d)) {
      if (name === 'front' || name === 'balcony') continue;
      I.add({obj: door.leaf, prompt: () => door.locked ? 'Дверь в ванную' : (door.isOpen ? 'Закрыть' : 'Открыть'), action: () => {
        if (door.locked) { g.say(S.THOUGHTS.bath); g.audio.click(door.pivot.position.clone().setY(1), 900, 0.3); return; }
        const open = door.toggle(); const p = door.pivot.position.clone().setY(1);
        open ? g.audio.doorOpen(p, 0.2) : setTimeout(() => g.audio.doorClose(p, 0.35), 450);
      }});
    }
    // front door
    I.add({obj: d.front.leaf, dist: 2.0, prompt: () => g.frontPrompt(), action: () => g.frontAction()});
    // peephole (needs looking at the upper part of the door from inside)
    // balcony door
    I.add({obj: d.balcony.leaf, prompt: () => d.balcony.isOpen || d.balcony.target > 0.05 ? 'Закрыть балкон' : 'Открыть балкон', action: () => g.balconyToggle()});
    // phone
    I.add({obj: W.phone.group, prompt: () => g.ringing ? 'Снять трубку' : 'Телефон', action: () => g.answerPhone()});
    // note
    I.add({obj: W.note, prompt: 'Прочитать записку', enabled: () => !g.flags.noteRead || true, action: () => g.readNote()});
    // drawer with flashlight
    I.proxy(this.scene, 6.97, 0.7, 3.95, 0.06, 0.12, 0.36, {prompt: () => g.flags.flashlight ? 'Ящик' : 'Открыть ящик', action: () => g.takeFlashlight()});
    // shoe shelf / coat hooks
    I.proxy(this.scene, 8.62, 0.25, 5.15, 0.3, 0.45, 0.9, {prompt: 'Переобуться', enabled: () => g.flags.noteRead && !g.flags.slippers, action: () => g.slippers()});
    I.proxy(this.scene, 8.7, 1.6, 5.15, 0.2, 0.5, 1.0, {prompt: 'Повесить куртку', enabled: () => g.flags.noteRead && !g.flags.coat, action: () => g.hangCoat()});
    // matches
    I.add({obj: W.matches, prompt: 'Взять спички', enabled: () => !g.flags.matches, action: () => { g.flags.matches = true; W.matches.visible = false; g.audio.click(W.matches.position, 3000, 0.2); g.say(S.THOUGHTS.matches); }});
    // stove
    I.proxy(this.scene, 8.5, 0.92, 1.7, 0.55, 0.2, 0.55, {prompt: () => g.stovePrompt(), action: () => g.stoveAction()});
    I.add({obj: W.kettle, prompt: () => g.stovePrompt(), action: () => g.stoveAction()});
    // pirozhki
    I.add({obj: W.pirozhki.towel, prompt: 'Взять пирожок', enabled: () => !g.flags.pirozhok && g.phase === 'warm', action: () => { g.flags.pirozhok = true; g.audio.creak(W.pirozhki.pos, 0.05, 0.2, 2000); g.say(S.THOUGHTS.pirozhok); g.done('pirozhok'); g.checkWarm(); }});
    // TV
    I.add({obj: W.tv.group, prompt: () => W.tv.on ? 'Выключить телевизор' : 'Включить телевизор', enabled: () => g.phase === 'warm' || g.phase === 'end', action: () => g.tvToggle()});
    // switches
    const sw = (obj, light, pos) => I.add({obj, prompt: 'Выключатель', action: () => { const on = g.lights.toggle(light); obj.userData.set(on); g.audio.switchClick(pos); g.onSwitch(light, on); }});
    sw(W.switchKitchen, 'kitchen', W.switchKitchen.position);
    sw(W.switchLiving, 'living', W.switchLiving.position);
    sw(W.switchHall, 'hall', W.switchHall.position);
    sw(W.switchBedroom, 'night', W.switchBedroom.position);
    W.switchLiving.userData.set(false); W.switchKitchen.userData.set(false);
    // armchair
    I.proxy(this.scene, 3.45, 0.55, 0.85, 0.7, 0.5, 0.7, {prompt: () => g.player.seated ? null : 'Сесть в кресло', action: () => g.sitArmchair()});
    // flavour
    I.proxy(this.scene, 0.16, 1.9, 0.16, 0.3, 0.35, 0.3, {prompt: 'Лампадка', action: () => g.say(S.THOUGHTS.lampada)});
    I.proxy(this.scene, 0.03, 1.6, 3.4, 0.06, 0.45, 0.35, {prompt: 'Портрет', action: () => g.say(S.THOUGHTS.portrait)});
    I.proxy(this.scene, 6.62, 1.58, 1.6, 0.05, 0.35, 0.22, {prompt: 'Календарь', action: () => g.say(S.THOUGHTS.calendar)});
    I.proxy(this.scene, 8.48, 0.65, 2.55, 0.62, 1.2, 0.62, {prompt: 'Холодильник', action: () => g.say(g.lights.mains ? S.THOUGHTS.fridge : S.THOUGHTS.fridgeOff)});
    I.proxy(this.scene, 6.65, 1.62, 1.0, 0.1, 0.18, 0.28, {prompt: 'Радиоточка', action: () => g.say(S.THOUGHTS.radio)});
    I.proxy(this.scene, 2.52, 1.0, 2.3, 0.5, 1.8, 1.0, {prompt: () => g.canHide ? 'Спрятаться в шифоньер' : (g.loop >= 1 && g.phase === 'event' && !g.flags.belt ? 'Взять дедов ремень' : 'Шифоньер'), action: () => {
      if (g.canHide) return g.hide('wardrobe');
      if (g.loop >= 1 && g.phase === 'event' && !g.flags.belt) { g.flags.belt = true; g.audio.creak(V(2.5, 1, 2.3), 0.15, 0.6, 300); g.say(S.THOUGHTS.belt); return; }
      g.say(S.THOUGHTS.wardrobe);
    }});
    I.proxy(this.scene, 0.52, 0.35, 3.3, 0.95, 0.5, 2.0, {prompt: 'Залезть под кровать', enabled: () => g.canHide && !g.hidden, action: () => g.hide('bed')});
    I.proxy(this.scene, 6.92, 0.55, 0.85, 0.66, 0.4, 0.96, {prompt: 'Спрятаться под стол', enabled: () => g.canHide && !g.hidden, action: () => g.hide('table')});
    I.proxy(this.scene, 6.25, 1.27, 0.75, 0.4, 0.9, 0.9, {prompt: 'Сервант', action: () => g.say(S.THOUGHTS.sideboard)});
    I.add({obj: W.livingClock.group, prompt: 'Часы', action: () => g.say(`${g.timeStr()}. ${g.clockComment()}`)});
    I.add({obj: W.kitchenClock.group, prompt: 'Часы', action: () => g.say(`${g.timeStr()}. ${g.clockComment()}`)});
    for (const [x, z] of [[1.4, 0.15], [3.95, 0.15], [7.7, 0.15]]) I.proxy(this.scene, x, 1.6, z, 1.3, 1.2, 0.1, {prompt: 'Окно', dist: 1.6, action: () => g.windowLook()});
    I.proxy(this.scene, 6.0, 1.0, 7.07, 0.1, 2.0, 0.85, {prompt: 'Квартира 59', action: () => g.say(S.THOUGHTS.neighbour59)});
    I.proxy(this.scene, 9.82, 1.0, 6.33, 0.85, 2.0, 0.1, {prompt: 'Квартира 61', action: () => g.say(S.THOUGHTS.neighbour61)});
    I.proxy(this.scene, 6.4, 2.45, 7.2, 0.7, 0.15, 0.7, {prompt: 'Люк', dist: 2.4, action: () => g.say(S.THOUGHTS.hatch)});
  }

  clockComment() {
    const m = this.clock;
    if (this.phase === 'warm') return 'Бабушка обещала к десяти.';
    if (m < 22 * 60 + 7) return `До семи минут одиннадцатого — ${Math.max(0, Math.ceil(22 * 60 + 7 - m))} мин.`;
    return 'Время идёт.';
  }

  // ---------- warm phase ----------
  async newGame() {
    this.abort();
    const tok = this.token;
    this.phase = 'intro'; this.loop = 0; this.deaths = 0; this.flags = {}; this.memory = []; this.endingDeath = false;
    this.resetWorld();
    this.player.canMove = false; this.ui.crosshair(false);
    this.ui.fade(1, 0);
    await this.ui.card(S.INTRO, {hold: 1600, gap: 1400});
    if (!this.alive(tok)) return;
    this.player.stand(spots.landingStart.x, spots.landingStart.z, Math.PI);
    this.player.yaw = 0; this.player.pitch = -0.05;
    this.ui.crosshair(true);
    this.phase = 'arrive';
    this.ambience();
    this.player.canMove = true;
    await this.ui.fade(0, 2500);
    this.say(S.THOUGHTS.landing);
    await this.wait(2);
    this.objective('door', 'Открыть дверь');
    this.ui.hint('<span class="key">E</span> / ЛКМ — действие   <span class="key">Shift</span> — быстрее');
  }

  resetWorld() {
    const W = this.world, d = W.doors;
    d.front.snap(0); d.front.locked = true;
    d.balcony.snap(0.16);
    d.kitchen.snap(0.95); d.living.snap(1); d.bedroom.snap(0.85); d.bath.snap(0);
    this.lights.setMains(true);
    this.lights.set('kitchen', false); this.lights.set('living', false); this.lights.set('lamp', true); this.lights.set('hall', true); this.lights.set('night', true); this.lights.set('landing', true); this.lights.set('lampada', true);
    W.switchKitchen.userData.set(false); W.switchLiving.userData.set(false); W.switchHall.userData.set(true);
    W.tv.setMode('off'); W.tv.power = true;
    W.matches.visible = true;
    W.stove.flame.visible = false; this.kettleT = -1;
    this.creature.hide(); this.creatureActive = false;
    this.hands.hold(null); this.hands.charge = 0;
    this.setOppositeLights(true);
    this.clock = 21 * 60 + 34; this.clockRate = 1 / 15;
    this.film.uRewind.value = 0; this.film.uGlitch.value = 0; this.film.uFear.value = 0;
    this.ringing = null; this.audio.stop('ring');
    this.renderer.markShadows(3);
    this.applyLoopChanges(0);
  }

  ambience() {
    const A = this.audio;
    A.wind(V(4.5, 1.5, -1.5), 'wind', 0.1);
    A.sleetOnGlass(V(3.95, 1.5, -0.2), 'sleetL', 0.05);
    A.sleetOnGlass(V(7.7, 1.5, -0.2), 'sleetK', 0.05);
    A.sleetOnGlass(V(1.4, 1.5, -0.2), 'sleetB', 0.04);
    A.fridge(V(8.48, 0.8, 2.55));
    A.clock(V(4.75, 1.65, 4.35), 'clock');
    this.updateBalconyAudio();
  }
  updateBalconyAudio() {
    const open = this.world.doors.balcony.target > 0.05;
    this.audio.fade('wind', open ? 0.32 : 0.1, 1.5);
  }

  frontPrompt() {
    const d = this.world.doors.front;
    if (this.phase === 'arrive' && !this.flags.entered) return 'Открыть ключом';
    if (this.phase === 'arrive' && this.flags.entered && !this.flags.frontLocked) return 'Закрыть дверь';
    if (this.inside() && this.eyeOnPeephole()) return 'Посмотреть в глазок';
    if (this.phase === 'event' || this.phase === 'end') return d.locked ? 'Отпереть' : (d.isOpen ? 'Закрыть' : 'Открыть');
    return d.locked ? 'Заперто' : 'Дверь';
  }
  inside() { return this.pos.z < 6.05; }
  eyeOnPeephole() { const f = this.player.forward(); return f.y > -0.05 && this.near(V(8.05, 0, 5.75), 0.75) && f.z > 0.6; }
  async frontAction() {
    const d = this.world.doors.front, p = V(8.05, 1, 6.15);
    if (this.phase === 'arrive' && !this.flags.entered) {
      this.audio.lockTurn(p); await this.wait(0.9);
      d.locked = false; d.setTarget(0.8, 1.2); this.audio.doorOpen(p, 0.25);
      this.flags.entered = true; this.done('door');
      this.ui.hideHint();
      await this.until(() => this.pos.z < 5.6);
      this.objective('close', 'Закрыть за собой дверь');
      return;
    }
    if (this.phase === 'arrive' && this.flags.entered && !this.flags.frontLocked) {
      if (this.pos.z > 5.95) return;
      d.setTarget(0, 1.4); await this.wait(0.55); this.audio.doorClose(p, 0.6);
      await this.wait(0.3); this.audio.lockTurn(p); d.locked = true; this.flags.frontLocked = true;
      this.done('close');
      await this.wait(0.8);
      this.objective('note', 'Прочитать записку на зеркале');
      return;
    }
    if (this.inside() && this.eyeOnPeephole()) { this.peep(); return; }
    if (this.phase === 'event') {
      if (d.locked) { this.audio.lockTurn(p); d.locked = false; this.flags.unlocked = true; return; }
      if (!d.isOpen) { d.setTarget(0.7, 1.4); this.audio.doorOpen(p, 0.3); this.onFrontOpened(); return; }
      d.setTarget(0, 1.4); setTimeout(() => this.audio.doorClose(p, 0.5), 500); return;
    }
    if (d.locked) { this.say(S.THOUGHTS.frontLocked); this.audio.click(p, 900, 0.3); }
  }

  readNote() {
    this.ui.note(S.NOTE);
    this.audio.creak(this.world.note.position, 0.04, 0.3, 3000);
    this.noteOpen = true; this.player.canMove = false;
    if (!this.flags.noteRead) {
      this.flags.noteRead = true; this.done('note');
      this.onNoteClosed = () => { this.objective('slippers', 'Переобуться'); this.objective('coat', 'Повесить куртку'); };
    }
  }
  closeNote() { this.ui.hideNote(); this.noteOpen = false; this.player.canMove = true; const f = this.onNoteClosed; this.onNoteClosed = null; f?.(); }

  slippers() { this.flags.slippers = true; this.done('slippers'); this.audio.step(this.pos, 'carpet', 0.3); this.say(S.THOUGHTS.slippers); this.afterArrival(); }
  hangCoat() { this.flags.coat = true; this.done('coat'); this.audio.creak(this.world.coatHooks, 0.08, 0.3, 900); this.say(S.THOUGHTS.coat); this.afterArrival(); }
  async afterArrival() {
    if (!(this.flags.slippers && this.flags.coat) || this.flags.call0) return;
    this.flags.call0 = 'pending';
    this.phase = 'warm';
    await this.wait(2.5);
    this.startRing('call0');
    this.objective('phone', 'Ответить на звонок');
  }

  startRing(kind) { this.ringing = kind; this.audio.phoneRing(PHONE_POS); }
  stopRing() { this.ringing = null; this.audio.stop('ring', 0.05); }

  async answerPhone() {
    if (this.inCall) return;
    const W = this.world;
    if (!this.ringing) { this.say(S.THOUGHTS.phoneIdle); this.audio.click(PHONE_POS, 1200, 0.2); return; }
    const kind = this.ringing;
    this.stopRing();
    this.audio.click(PHONE_POS, 900, 0.4);
    this.inCall = true; this.player.canMove = false;
    W.phone.handset.visible = false;
    this.ui.hideHint();
    let res = -1;
    if (kind === 'call0') { this.done('phone'); res = await this.dialogue(S.CALL0); this.afterCall0(); }
    else if (kind === 'call1') { res = await this.dialogue(S.call1(this.loop, this.world.doors.balcony.target < 0.02, this.flags.flashlight)); this.flags.call1Done = true; }
    else if (kind === 'fake') { this.flags.fakeAnswered = true; await this.dialogue(this.flags.tied ? [{who: 'Бабушка', text: S.FAKE_MEMORY, strange: true}, ...S.FAKE_CALL.slice(1)] : S.FAKE_CALL); }
    else if (kind === 'code') { await this.wait(0.6); this.audio.busyTone(1.5); }
    else if (kind === 'call2') { await this.dialogue(S.call2(this.loop)); this.flags.call2Done = true; }
    else if (kind === 'end') { await this.dialogue(S.CALL_END); this.flags.endCall = true; }
    this.audio.click(PHONE_POS, 900, 0.4);
    W.phone.handset.visible = true;
    this.inCall = false; this.player.canMove = true;
    return res;
  }

  afterCall0() {
    this.flags.call0 = true;
    this.objective('kettle', 'Поставить чайник');
    this.objective('blink', 'Помигать бабушке светом с кухни');
    this.objective('tv', 'Включить телевизор');
    this.objective('pirozhok', 'Взять пирожок');
    this.kitchenToggles = 0;
  }

  stovePrompt() {
    if (this.phase !== 'warm' && this.phase !== 'arrive') return this.kettleT >= 0 ? 'Выключить газ' : 'Плита';
    if (this.kettleT < 0 && !this.flags.tea) return 'Зажечь газ';
    if (this.kettleT >= 0 && this.kettleT < 38) return 'Чайник греется';
    if (this.kettleT >= 38) return 'Выключить газ и налить чай';
    return 'Плита';
  }
  async stoveAction() {
    const W = this.world, p = W.stove.flamePos;
    if (this.kettleT < 0 && !this.flags.tea && (this.phase === 'warm' || this.phase === 'arrive')) {
      if (!this.flags.matches) { this.say(S.THOUGHTS.noMatches); return; }
      this.audio.matchStrike(p); await this.wait(0.5);
      this.audio.whoosh(p, 0.3); W.stove.flame.visible = true; this.audio.burner(p); this.audio.kettle(W.stove.kettlePos);
      this.kettleT = 0; this.say(S.THOUGHTS.gasOn);
      return;
    }
    if (this.kettleT >= 38 || (this.kettleT >= 0 && this.phase !== 'warm')) {
      W.stove.flame.visible = false; this.audio.stop('burner'); this.audio.stop('kettle', 1.2); this.kettleT = -1;
      this.audio.click(p, 1600, 0.2);
      if (this.phase === 'warm') { this.flags.tea = true; this.done('kettle'); this.hands.hold('tea'); this.audio.whoosh(p, 0.1); this.say(S.THOUGHTS.tea); this.checkWarm(); }
    }
  }
  onSwitch(light, on) {
    if (light === 'kitchen' && this.flags.call0 === true && !this.flags.blink) {
      this.kitchenToggles = (this.kitchenToggles ?? 0) + 1;
      if (this.kitchenToggles >= 2 && on) {
        this.flags.blink = true;
        this.blinkBack();
      }
    }
  }
  async blinkBack() {
    await this.wait(1.6);
    const opp = this.world.opposite, i = this.world.valyaWindow;
    for (let k = 0; k < 2; k++) { opp.set(i, false); await this.wait(0.45); opp.set(i, true); await this.wait(0.45); }
    if (this.room() === 'kitchen') this.say(S.THOUGHTS.blink);
    this.done('blink'); this.checkWarm();
  }
  tvToggle() {
    const tv = this.world.tv;
    if (tv.on) { tv.setMode('off'); this.audio.stop('music'); this.audio.click(TV_POS, 700, 0.3); return; }
    tv.setMode('concert'); this.audio.click(TV_POS, 700, 0.3); this.audio.music(TV_POS);
    if (this.phase === 'warm' && !this.flags.tv) { this.flags.tv = true; this.done('tv'); this.say(S.THOUGHTS.tvOn); this.checkWarm(); }
  }
  balconyToggle() {
    const d = this.world.doors.balcony, p = V(5.17, 1, -0.1);
    if (this.flags.tied) { this.say(S.THOUGHTS.tied); return; }
    if (d.target > 0.05) {
      d.setTarget(0, 1.4); setTimeout(() => { this.audio.doorClose(p, 0.4); this.audio.click(p, 2200, 0.3); }, 500);
      if (this.flags.belt) { this.flags.tied = true; setTimeout(() => { this.beltMesh.visible = true; this.audio.creak(p, 0.12, 0.5, 500); this.say(S.THOUGHTS.tied); }, 900); }
      else if (this.phase === 'event' || this.loop === 0) {
        // the latch is broken: the wind pushes it open again
        const tok = this.token;
        setTimeout(() => { if (this.alive(tok) && !this.flags.tied) { d.setTarget(0.35, 0.6); this.audio.creak(p, 0.25, 1.2, 220); this.audio.fade('wind', 0.32, 1); this.say(S.THOUGHTS.latchBroken); } }, 2600);
      } else this.say(S.THOUGHTS.balconyClosed);
    }
    else { d.setTarget(0.85, 1.2); this.audio.doorOpen(p, 0.25); this.say(S.THOUGHTS.balconyOpen); }
    setTimeout(() => this.updateBalconyAudio(), 600);
  }
  takeFlashlight() {
    if (this.flags.flashlight) { this.audio.creak(V(6.97, 0.7, 3.95), 0.06, 0.3, 700); return; }
    this.flags.flashlight = true; this.audio.creak(V(6.97, 0.7, 3.95), 0.1, 0.35, 600);
    this.say('Жучок. Надо жать, чтобы светил.');
    this.ui.hint('<span class="key">F</span> — фонарь (жми несколько раз, чтобы подкачать)', 8000);
  }
  checkWarm() {
    const f = this.flags;
    if (this.phase === 'warm' && f.tea && f.tv && f.blink && !f.sitObjective) {
      f.sitObjective = true;
      this.objective('sit', 'Сесть в кресло с чаем');
    }
  }
  sitArmchair() {
    if (this.phase === 'event' && !this.flags.canSitEvent) { this.player.sit(ARMCHAIR_SEAT); return; }
    this.player.sit(ARMCHAIR_SEAT);
    this.audio.creak(V(3.45, 0.5, 0.85), 0.12, 0.5, 300);
    if (this.phase === 'warm') {
      if (!this.flags.sitObjective) { this.say(S.THOUGHTS.armchairNotYet); return; }
      this.done('sit');
      this.say(S.THOUGHTS.sit);
      this.ui.hint('Удерживайте <span class="key">C</span> — закрыть глаза', 0);
      this.flags.restReady = true;
    }
  }
  windowLook() {
    if (this.phase === 'warm' || this.phase === 'arrive') return this.say(S.THOUGHTS.window);
    if (this.phase === 'event' && this.flags.fakeAnswered) return;
    this.say('Ничего не видно. Только туман.');
  }
  setOppositeLights(on) {
    const opp = this.world.opposite;
    opp.info.forEach((w, i) => { if (w.origLit === undefined) w.origLit = w.lit; opp.set(i, on && w.origLit); });
    opp.set(this.world.valyaWindow, on);
    for (const l of this.world.streetLamps) { l.light.intensity = on ? l.base : 0; l.halo.visible = on; l.mat.color.set(on ? 0xffa040 : 0x111111); }
  }

  async continueFromCheckpoint() {
    const cp = JSON.parse(localStorage.getItem('atk-zap-checkpoint-v1') || 'null');
    if (!cp) return this.newGame();
    this.abort();
    this.resetWorld();
    this.loop = cp.loop; this.deaths = cp.deaths; this.flags = {...cp.flags};
    this.snap = cp.snap;
    this.ambience();
    this.ui.fade(1, 0);
    this.applyLoopChanges(this.loop);
    this.startEvent(false);
    this.ui.fade(0, 2000);
  }

  // ---------- the rest that leads to the event ----------
  async rest() {
    // called when C held long enough in the armchair (warm phase)
    this.flags.restReady = false; this.ui.hideHint();
    this.phase = 'transition';
    await this.ui.fade(1, 2500);
    this.ui.eyes(false);
    this.say(S.THOUGHTS.restHint, 2000);
    await this.wait(2.5);
    this.startEvent(true);
  }

  // ---------- EVENT LOOP ----------
  snapshot() {
    const d = this.world.doors;
    this.snap = {balcony: d.balcony.target, frontLocked: d.front.locked, flashlight: !!this.flags.flashlight, kitchen: this.lights.isOn('kitchen'), living: this.lights.fixtures.living.on, hall: this.lights.fixtures.hall.on, kitchenDoor: d.kitchen.target, livingDoor: d.living.target, bedroomDoor: d.bedroom.target};
  }
  restore() {
    if (!this.snap) this.snapshot();
    this.flags.belt = false; this.flags.tied = false; this.beltMesh.visible = false;
    this.unhide(true); this.canHide = false;
    this.world.doors.bath.snap(0); this.world.doors.bath.locked = true;
    const d = this.world.doors, s = this.snap;
    d.balcony.snap(s.balcony); d.front.snap(0); d.front.locked = s.frontLocked;
    d.kitchen.snap(s.kitchenDoor); d.living.snap(s.livingDoor); d.bedroom.snap(s.bedroomDoor);
    this.flags.flashlight = s.flashlight;
    this.lights.setMains(true);
    this.lights.set('kitchen', s.kitchen); this.lights.set('living', s.living); this.lights.set('hall', s.hall); this.lights.set('lamp', true); this.lights.set('lampada', true);
    this.world.switchKitchen.userData.set(s.kitchen);
    this.setOppositeLights(true);
    this.updateBalconyAudio();
  }

  async startEvent(first) {
    this.abort();
    const tok = this.token;
    this.phase = 'event';
    this.ui.clearObjectives();
    if (first) { this.snapshot(); try { localStorage.setItem('atk-zap-checkpoint-v1', JSON.stringify({snap: this.snap, loop: this.loop, deaths: this.deaths, flags: {flashlight: !!this.flags.flashlight, slippers: true, tea: true}})); } catch (e) {} }
    else this.restore();
    const W = this.world, A = this.audio;
    // reset event state
    for (const k of ['call1Done', 'fakeAnswered', 'call2Done', 'unlocked', 'canSitEvent', 'insideNow', 'endCall']) this.flags[k] = false;
    this.creature.hide(); this.creatureActive = false; this.creature.lookAt = null;
    this.stopRing(); A.stop('tvstatic'); A.stop('tvtone'); A.stop('drone', 1); A.stop('music');
    if (!A.loops.fridge) A.fridge(V(8.48, 0.8, 2.55));
    if (!A.loops.clock) A.clock(V(4.75, 1.65, 4.35), 'clock');
    W.stove.flame.visible = false; A.stop('burner'); A.stop('kettle'); this.kettleT = -1;
    this.player.sit(ARMCHAIR_SEAT);
    this.hands.hold('tea'); this.hands.charge = 0;
    this.eyesClosed = false; this.ui.eyes(false);
    this.clock = 21 * 60 + 59 + 40 / 60; this.clockRate = 1 / 60;
    if (this.loop === 0) { W.tv.setMode('concert'); A.music(TV_POS); }
    else { W.tv.setMode(this.loop >= 2 ? 'face' : 'static'); A.tvStatic(TV_POS); }
    this.film.uFear.value = 0;
    this.renderer.markShadows(3);
    this.player.canMove = true;
    if (first) await this.ui.fade(0, 2500);

    // --- 22:00 time signal
    await this.wait(first ? 4 : 2); if (!this.alive(tok)) return;
    A.stop('music', 0.2); A.stop('tvstatic', 0.1); W.tv.setMode('clock'); A.pips(TV_POS); A.pips(W.radioPos);
    await this.wait(6.2); if (!this.alive(tok)) return;
    this.clock = 22 * 60; this.clockRate = 1 / 24;
    W.tv.setMode('testcard'); W.tv.crawl = S.CRAWL; W.tv.crawlT = 0; A.tvTone(TV_POS);
    this.film.uFear.value = 0.15;
    this.parallel(this.announce(tok));
    // --- call 1
    await this.wait(24); if (!this.alive(tok)) return;
    this.startRing('call1');
    this.parallel(this.ringTimeout(tok, 'call1', 32));
    await this.until(() => this.flags.call1Done || this.ringing !== 'call1'); if (!this.alive(tok)) return;
    await this.until(() => !this.inCall);
    // --- 22:03 blackout wave across the yard
    await this.wait(5); if (!this.alive(tok)) return;
    A.stop('tvtone'); W.tv.setMode('static'); A.tvStatic(TV_POS);
    await this.blackoutWave(tok); if (!this.alive(tok)) return;
    // our flat
    this.lights.flickerAll(1.6); A.tvStatic(TV_POS);
    await this.wait(1.7); if (!this.alive(tok)) return;
    this.lights.setMains(false); W.tv.power = false; A.stop('tvstatic', 0.05); A.stop('fridge', 0.3); A.stop('clock', 0.05); A.powerDown();
    this.renderer.markShadows(2);
    this.film.uFear.value = 0.35;
    A.drone('drone', 0.05, 38);
    await this.wait(2.5); if (!this.alive(tok)) return;
    if (!this.flags.flashlight) this.say(S.THOUGHTS.dark); else if (this.hands.current !== 'flashlight') this.ui.hint('<span class="key">F</span> — фонарь', 5000);
    if (this.loop >= 1) this.parallel(this.tvFeedScare(tok));
    if (this.loop >= 2) this.parallel(this.hangedScare(tok));
    // --- 22:05 knocking at the door
    await this.wait(9); if (!this.alive(tok)) return;
    this.creature.show(8.05, 0, 6.95, Math.PI); // on the landing, facing the door
    this.creatureAt = 'door';
    this.parallel(this.doorVisitor(tok));
    // --- 22:06 fake call
    await this.wait(26); if (!this.alive(tok)) return;
    if (!this.inCall) { this.startRing('fake'); this.parallel(this.ringTimeout(tok, 'fake', 22)); }
    await this.until(() => this.ringing !== 'fake' && !this.inCall); if (!this.alive(tok)) return;
    if (this.flags.fakeAnswered) this.parallel(this.windowTrap(tok));
    // --- 22:07 balcony
    await this.wait(8); if (!this.alive(tok)) return;
    this.creatureAt = 'balcony';
    if (this.loop === 0 && W.doors.balcony.target < 0.05) W.doors.balcony.setTarget(0.4, 2);
    if (!this.flags.tied) { await this.balconyEntry(tok); return; }
    await this.balconyVisit(tok); if (!this.alive(tok)) return;
    // --- 22:08 real call (code)
    await this.wait(6); if (!this.alive(tok)) return;
    this.ringing = 'code'; A.phoneRingOnce(PHONE_POS, 2);
    await this.wait(7.5); if (!this.alive(tok)) return;
    if (this.ringing === 'code') this.ringing = null;
    await this.wait(1.5); if (!this.alive(tok)) return;
    if (!this.inCall) { this.startRing('call2'); this.parallel(this.ringTimeout(tok, 'call2', 30)); }
    await this.until(() => (this.ringing !== 'call2' && !this.inCall)); if (!this.alive(tok)) return;
    // --- 22:10 it is inside
    await this.wait(3); if (!this.alive(tok)) return;
    await this.hidePhase(tok); if (!this.alive(tok)) return;
    // --- survived
    await this.ending(tok);
  }
  parallel(p) { p.catch(e => console.error(e)); }
  async ringTimeout(tok, kind, sec) { await this.wait(sec); if (this.alive(tok) && this.ringing === kind) this.stopRing(); }

  async announce(tok) {
    for (const t of S.ANNOUNCE) {
      if (!this.alive(tok)) return;
      if (this.inCall) break;
      const vd = this.audio.voice(t, 'tv', TV_POS);
      await this.ui.subtitle('Телевизор', t, vd ? vd * 1000 + 500 : 1500 + t.length * 60, 'tv');
    }
  }

  async blackoutWave(tok) {
    const opp = this.world.opposite, A = this.audio;
    for (let f = 0; f < 5; f++) {
      const idx = opp.info.map((w, i) => i).filter(i => opp.info[i].floor === f && i !== this.world.valyaWindow);
      for (const i of idx) opp.set(i, false);
      A.thump(V(8, -6 + f * 2.7, -40), 0.12, 60);
      await this.wait(1.6); if (!this.alive(tok)) return;
      if (f === 3) { opp.set(this.world.valyaWindow, false); }
    }
    for (const l of this.world.streetLamps) { l.light.intensity = 0; l.halo.visible = false; l.mat.color.set(0x111111); A.click(l.lens.position, 300, 0.4); await this.wait(0.9); if (!this.alive(tok)) return; }
    for (const s of this.world.chimneyLights) s.visible = false;
    this.world.farLights.visible = false; this.world.ourFacadeWindows.visible = false;
  }

  async doorVisitor(tok) {
    const A = this.audio, p = V(8.05, 1.2, 6.5);
    A.knock(p, 3, 0.55, 0.8);
    await this.wait(2.5); if (!this.alive(tok)) return;
    let i = 0;
    while (this.alive(tok) && this.creatureAt === 'door') {
      const t = S.DOOR_VOICE[i % S.DOOR_VOICE.length];
      const vd = this.audio.voice(t, 'door', p);
      if (!vd) A.murmur(p, 1.2 + t.length * 0.04, {pitch: 200, v: 0.16});
      if (!this.inCall) this.ui.subtitle('За дверью', t, vd ? vd * 1000 + 600 : 2200 + t.length * 45, 'strange');
      i++;
      await this.wait(7 + Math.random() * 3); if (!this.alive(tok)) return;
      if (this.creatureAt !== 'door') return;
      if (i === 2) { A.doorbell(V(8.05, 2.2, 5.97)); await this.wait(1.6); if (this.inside() && !this.inCall) this.say(S.THOUGHTS.doorbellNoPower); await this.wait(2); }
      else if (Math.random() < 0.6) { A.knock(p, 2 + Math.floor(Math.random() * 3), 0.4, 0.6); await this.wait(1.5); }
      if (!this.alive(tok)) return;
    }
  }

  onFrontOpened() {
    if (this.phase !== 'event') return;
    if (this.creature.visible && this.creatureAt === 'door') this.die('door');
    else if (this.creatureActive) { /* nothing special */ }
  }

  async windowTrap(tok) {
    const W = this.world, opp = W.opposite;
    // the player must walk to the kitchen window and look out within 25 s
    const t0 = this.clock;
    await this.until(() => (this.room() === 'kitchen' && this.pos.z < 1.2 && this.lookingAt(V(7.7, 1.2, -2), 0.7)) || this.creatureAt === 'balcony');
    if (!this.alive(tok) || this.creatureAt === 'balcony') return;
    // a single window lights up opposite, then lights rush toward us
    const order = opp.info.map((w, i) => i).filter(i => opp.info[i].floor >= 3).sort((a, b) => Math.abs(opp.info[a].x - 7.7) - Math.abs(opp.info[b].x - 7.7)).slice(0, 4);
    for (const i of order) { opp.set(i, true, 1.3); await this.wait(0.35); opp.set(i, false); }
    if (!this.alive(tok)) return;
    this.creature.show(7.7, -0.15, -0.75, 0);
    this.creature.lookAt = this.camera.position;
    this.audio.whisper(V(7.7, 1.6, -0.6), 1.2, 0.3);
    await this.wait(0.7); if (!this.alive(tok)) return;
    this.die('window');
  }

  async balconyVisit(tok) {
    // balcony closed: it stands on the balcony and taps on the glass, then climbs to the roof
    const A = this.audio;
    this.creature.show(4.3, 0, -0.85, 0);
    this.creature.lookAt = this.camera.position;
    const glass = V(4.3, 1.5, -0.3);
    A.knock(glass, 3, 0.7, 0.35);
    await this.wait(2.2); if (!this.alive(tok)) return;
    const t = S.BALCONY_VOICE;
    if (!this.audio.voice(t, 'balcony', glass)) A.murmur(glass, 1.6, {pitch: 190, v: 0.14});
    if (!this.inCall) this.ui.subtitle('За окном', t, 3000, 'strange');
    await this.wait(4); if (!this.alive(tok)) return;
    A.knock(glass, 2, 0.9, 0.3);
    await this.wait(3); if (!this.alive(tok)) return;
    this.creature.hide();
    // footsteps over the ceiling, toward the hall
    for (let i = 0; i < 9; i++) { const x = 4.3 + i * 0.45; A.step(V(x, 2.8, 1.5 + i * 0.3), 'concrete', 0.25, true); await this.wait(0.8); if (!this.alive(tok)) return; }
    if (this.room() !== 'none' && !this.inCall) this.say(S.THOUGHTS.roof);
    this.creatureAt = 'roof';
  }

  async balconyEntry(tok) {
    const W = this.world, A = this.audio, d = W.doors.balcony;
    this.creatureAt = 'inside';
    A.fade('wind', 0.5, 0.3);
    this.creature.show(5.17, 0, -0.9, 0);
    await this.wait(0.6); if (!this.alive(tok)) return;
    d.setTarget(1, 4); A.thump(V(5.17, 1, 0), 0.6, 80); A.creak(V(5.17, 1, 0), 0.3, 0.8, 200);
    this.renderer.markShadows(2);
    if (this.room() === 'living' && !this.inCall) this.say('…Балкон.');
    await this.hunt(tok, 1.05, null, 'balcony');
  }

  // creature walks to the player through the flat; death when close
  async hunt(tok, speed = 1.0, until = null, kind = null) {
    this.creatureActive = true;
    this.creature.lookAt = this.camera.position;
    let repath = 0;
    while (this.alive(tok)) {
      if (repath <= 0 || !this.creature.walking) { this.creature.walkPath(this.pathTo(this.creature.pos, this.pos), speed); repath = 2; }
      await this.wait(0.3); repath -= 0.3;
      if (!this.alive(tok)) return;
      if (until && until()) return;
      const dist = Math.hypot(this.creature.pos.x - this.pos.x, this.creature.pos.z - this.pos.z);
      if (dist < 1.1) { this.die(kind ?? (this.eyesClosed ? 'eyes' : 'look')); return; }
    }
  }
  pathTo(from, to) {
    const nearest = p => Object.entries(NODES).reduce((b, [k, v]) => { const dd = Math.hypot(v.x - p.x, v.z - p.z); return dd < b[1] && !this.wallBetween(p, v) ? [k, dd] : b; }, [null, 1e9])[0] ?? 'living';
    const a = nearest(from), b = nearest(to);
    const prev = {[a]: null}, q = [a];
    while (q.length) { const n = q.shift(); if (n === b) break; for (const [x, y] of EDGES) { const m = x === n ? y : y === n ? x : null; if (m && !(m in prev)) { prev[m] = n; q.push(m); } } }
    const out = []; let n = b; while (n) { out.unshift(NODES[n]); n = prev[n]; }
    out.push(V(to.x, 0, to.z));
    // don't walk back to a node we are already past
    while (out.length >= 2 && Math.hypot(from.x - out[1].x, from.z - out[1].z) < Math.hypot(out[0].x - out[1].x, out[0].z - out[1].z) + 0.05) out.shift();
    return out;
  }
  wallBetween(p, q) {
    // coarse: different rooms and no direct opening
    const ra = roomOf(p.x, p.z), rb = roomOf(q.x, q.z);
    return ra !== rb && !(ra.startsWith('hall') && rb.startsWith('hall'));
  }

  // ---------- hiding ----------
  hide(spot) {
    if (this.hidden) return;
    const h = HIDE[spot];
    this.hidden = spot;
    this.player.sit({pos: h.cam, yaw: h.yaw, pitch: h.pitch, yawRange: 0.55});
    this.player.sitT = 0.6;
    this.audio.creak(h.cam, 0.12, 0.5, spot === 'wardrobe' ? 260 : 600);
    this.ui.hideOverlay(h.overlay);
    this.ui.hint('<span class="key">Пробел</span> — не дышать · <span class="key">E</span> — вылезти', 0);
    this.hands.hold(null);
    this.say(S.THOUGHTS.hide, 1800);
  }
  unhide(silent = false) {
    if (!this.hidden) return;
    const h = HIDE[this.hidden];
    this.hidden = null; this.holding = false;
    this.ui.hideOverlay(null); this.ui.breath(null);
    if (!silent) { this.player.stand(h.exit.x, h.exit.z, h.yaw); this.audio.creak(h.cam, 0.1, 0.4, 400); this.ui.hideHint(); }
  }

  async hidePhase(tok) {
    const A = this.audio, C = this.creature, W = this.world;
    this.creatureAt = 'inside';
    this.canHide = true;
    this.ui.hint('Спрятаться: <span class="key">E</span> у шифоньера, кровати или кухонного стола', 9000);
    // the jammed bathroom door opens by itself
    await this.wait(3); if (!this.alive(tok)) return;
    W.doors.bath.locked = false; W.doors.bath.setTarget(0.9, 0.35);
    A.creak(V(4.65, 1, 5.25), 0.35, 2.5, 170);
    A.drone('drone', 0.1, 33);
    this.film.uFear.value = 0.6;
    await this.wait(2.5); if (!this.alive(tok)) return;
    C.show(3.6, 0, 5.25, Math.PI / 2); C.lookAt = null; C.twist = 0;
    C.walkPath([V(4.3, 0, 5.25), V(5.3, 0, 5.25)], 0.55);
    this.creatureActive = true;
    // a few seconds of grace, then it hunts anyone who is not hidden
    for (let t = 0; t < 12 && !this.hidden; t += 0.25) { await this.wait(0.25); if (!this.alive(tok)) return; }
    if (!this.hidden) { await this.hunt(tok, 1.1, null, 'found'); return; }
    const mine = this.hidden;
    let voiceI = 0;
    const say = () => { const t = S.SEARCH_VOICE[voiceI++ % S.SEARCH_VOICE.length]; if (!A.voice(t, 'door', C.pos.clone().setY(1.8))) A.whisper(C.pos.clone().setY(1.8), 1.6, 0.3); this.ui.subtitle('', t, 2600, 'strange'); };
    // it checks every place you hid in before, most recent first
    const order = [...this.memory].reverse().filter((v, i, a) => a.indexOf(v) === i);
    for (const spot of order) {
      if (!this.alive(tok)) return;
      if (!this.hidden) { await this.hunt(tok, 1.2, null, 'look'); return; }
      const h = HIDE[spot];
      say();
      C.walkPath(this.pathTo(C.pos, h.front).concat([h.front]), 0.9);
      await this.until(() => !C.walking); if (!this.alive(tok)) return;
      C.faceTo(h.cam); C.reachTarget = 1; C.jawTarget = 0.4;
      A.creak(h.cam, 0.3, 0.8, 200);
      await this.wait(1.6); if (!this.alive(tok)) return;
      if (this.hidden === spot) { this.die('remembered'); return; }
      C.reachTarget = 0; C.jawTarget = 0;
      await this.wait(0.8);
    }
    if (!this.alive(tok)) return;
    // then it comes to where you are, and listens
    const h = HIDE[mine];
    if (this.memory.length === 0) say();
    C.walkPath(this.pathTo(C.pos, h.front).concat([h.front]), 0.7);
    await this.until(() => !C.walking || !this.hidden); if (!this.alive(tok)) return;
    if (!this.hidden) { this.die('look'); return; }
    C.faceTo(h.cam); C.lookAt = h.cam.clone(); C.twist = 0;
    A.heartbeat(0.6); setTimeout(() => A.heartbeat(0.6), 700);
    if (!this.holding) this.say('Оно рядом. Не дыши.', 1800);
    // breath check: must hold breath the whole time it stands there
    for (let t = 0; t < 6; t += 0.2) {
      if (!this.alive(tok)) return;
      if (!this.hidden) { this.die('look'); return; }
      if (t > 2.0 && !this.holding) { this.die('breath'); return; }
      if (Math.random() < 0.15) A.breathOut(C.headWorld(), 0.12, 1.2);
      if (t > 2.6 && t < 2.9) { C.twist = Math.PI * 0.9; A.crack(A.now, 0.5); }   // the neck turns all the way round
      if (t > 4.4 && t < 4.7) C.twist = 0;
      await this.wait(0.2);
    }
    if (!this.alive(tok)) return;
    // it leaves
    C.lookAt = null;
    C.walkPath(this.pathTo(C.pos, V(4.0, 0, 5.25)).concat([V(3.5, 0, 5.25)]), 0.8);
    await this.wait(5); if (!this.alive(tok)) return;
    C.hide(); this.creatureActive = false;
    W.doors.bath.setTarget(0, 2); setTimeout(() => A.doorClose(V(4.65, 1, 5.25), 0.6), 400); W.doors.bath.locked = true;
    this.memory.push(mine);
    A.stop('drone', 2);
    await this.wait(3); if (!this.alive(tok)) return;
    A.powerUp(); A.fridge(V(8.48, 0.8, 2.55));
    this.lights.setMains(true);
    A.clock(V(4.75, 1.65, 4.35), 'clock');
    this.film.uFear.value = 0.2;
    this.canHide = false;
    this.ui.hideHint();
    await this.wait(2);
  }

  // ---------- scares ----------
  async tvFeedScare(tok) {
    const W = this.world, C = this.creature, F = this.feed;
    await this.wait(1.5); if (!this.alive(tok) || this.inCall) return;
    // the TV switches on by itself: a picture of the armchair — you sitting in it, and her behind you
    W.tv.power = true; W.tv.setFeed(F.rt.texture);
    F.aim(V(5.95, 1.15, 1.8), V(3.3, 0.85, 0.8));
    this.selfDummy.visible = true;
    C.show(2.98, 0, 0.62, Math.atan2(0.94, 0.34)); setLayerDeep(C.group, LAYER_FEED); C.lookAt = null; C.twist = 0;
    F.active = true;
    this.audio.tvStatic(TV_POS); this.audio.fade('tvstatic', 0.03, 0.2);
    let said = false;
    for (let t = 0; t < 6.5; t += 0.25) {
      await this.wait(0.25); if (!this.alive(tok)) break;
      if (!said && this.lookingAt(TV_POS, 0.85)) { said = true; this.say(S.THOUGHTS.tvSelf, 2600); }
      if (t > 4.2) C.twist = -0.8;   // it turns to look at the camera
    }
    F.active = false; this.selfDummy.visible = false;
    C.hide(); setLayerDeep(C.group, 0);
    W.tv.setMode('static'); this.audio.fade('tvstatic', 0.08, 0.1);
    setTimeout(() => { W.tv.power = false; this.audio.stop('tvstatic', 0.05); }, 400);
  }

  async hangedScare(tok) {
    const H = this.hanged, A = this.audio;
    H.place(1.45, 2.5);
    await this.until(() => this.near(V(2.6, 0, 3.7), 1.6) && this.lookingAt(V(1.45, 1.5, 2.5), 0.85));
    if (!this.alive(tok)) return;
    const k = this.hands.charge; this.hands.charge = 0.05;
    await this.wait(0.25);
    H.group.visible = true; H.props.visible = true;
    this.hands.charge = Math.max(k, 0.9);
    A.sting(0.7); this.say(S.THOUGHTS.hanged, 2500);
    this.film.uGlitch.value = 0.25;
    for (let t = 0; t < 1.8; t += 0.05) { H.group.rotation.y = Math.sin(t * 2) * 0.15; if (Math.random() < 0.2) A.creak(V(1.45, 2.3, 2.5), 0.2, 0.3, 120); await this.wait(0.05); }
    this.film.uGlitch.value = 0;
    this.hands.charge = 0.02; await this.wait(0.3); this.hands.charge = Math.max(k, 0.6);
    H.group.visible = false;   // the stool and the slipper stay
  }

  // ---------- death: it grabs your face and tears it off ----------
  async die(kind) {
    if (this.phase !== 'event' && this.phase !== 'end') return;
    this.abort();
    this.phase = 'death';
    this.deaths++;
    const A = this.audio, C = this.creature, F = this.film;
    this.stopRing(); this.inCall = false; this.ui.hideSubtitle(); this.ui.hideHint();
    this.eyesClosed = false; this.ui.eyes(false);
    this.unhide(true); this.canHide = false;
    this.player.canMove = false;
    const cam = this.camera.position.clone();
    const f = this.player.forward(); f.y = 0; f.normalize();
    setLayerDeep(C.group, 0);
    if (!C.visible || C.pos.distanceTo(V(cam.x, C.pos.y, cam.z)) > 2.4) C.show(cam.x + f.x * 1.4, kind === 'window' ? -0.15 : 0, cam.z + f.z * 1.4, 0);
    if (!this.endingDeath) C.setDisguise(false);
    C.walking = false; C.faceTo(V(cam.x, 0, cam.z)); C.lookAt = cam.clone(); C.twist = 0;
    this.player.seated = null;
    this.player.forced = {target: C.headWorld(), speed: 14};
    this.faceLight.intensity = 0.7; this.faceLight.color.set(0xff8a70);
    this.hands.hold(null);
    F.uFear.value = 1;
    const anim = (ms, fn) => new Promise(r => { const t0 = performance.now(); const step = () => { const k = Math.min(1, (performance.now() - t0) / ms); fn(k); if (k < 1) requestAnimationFrame(step); else r(); }; step(); });
    // 1. lunge, jaw dropping
    A.shriek(1.1, 0.7); A.sting(0.8);
    C.jawTarget = 1; C.reachTarget = 1;
    const start = C.pos.clone();
    await anim(420, k => {
      const target = V(cam.x - f.x * 0.0 + (start.x - cam.x) * 0.28, C.pos.y, cam.z + (start.z - cam.z) * 0.28);
      C.pos.lerpVectors(start, target, k * k);
      this.player.forced.target.copy(C.headWorld());
      this.faceLight.position.lerpVectors(cam, C.headWorld(), 0.5);
      this.player.shake = 0.6 + k;
      F.uGlitch.value = k * 0.4;
    });
    // 2. hands on the face
    A.scream(2.2, 0.35); A.heartbeat(0.7);
    await anim(450, k => { this.hands.claw = k; this.player.shake = 1.4; F.uGlitch.value = 0.3; });
    // 3. the skin goes
    A.rip(1.7, 0.85);
    await anim(1700, k => { F.uTear.value = Math.pow(k, 1.3); F.uGore.value = Math.min(1, k * 1.2); this.player.shake = 1.6; this.player.pitch += (Math.random() - 0.5) * 0.02; F.uGlitch.value = 0.15 + Math.random() * 0.2; });
    // 4. blood runs down, vision fails
    this.hands.claw = 0.85;
    await anim(1300, k => { F.uRed.value = k * 0.8; F.uGore.value = 1; F.uBright.value = 1 - k * 0.7; });
    this.ui.fade(1, 0);
    F.uTear.value = 0; F.uGore.value = 0; F.uRed.value = 0; F.uGlitch.value = 0; F.uBright.value = this.brightness ?? 1;
    this.hands.claw = 0; this.faceLight.intensity = 0;
    this.player.forced = null; C.hide();
    for (const k of Object.keys(A.loops)) if (k !== 'wind') A.stop(k, 0.05);
    A.heartbeat(0.4); await sleepReal(900); A.heartbeat(0.25); await sleepReal(1300);
    if (this.endingDeath) { this.finalCard(); return; }
    const mem = S.DEATH_MEMORY[kind] ?? S.DEATH_MEMORY.look;
    await this.ui.card([this.timeStr()], {hold: 600, gap: 400, cls: ['big']});
    // rewind to 21:59:40
    this.loop++;
    this.restore();
    this.applyLoopChanges(this.loop);
    this.player.sit(ARMCHAIR_SEAT); this.player.sitT = 1;
    this.hands.hold(null);
    this.world.tv.power = true; this.world.tv.setMode('static');
    F.uRewind.value = 1; F.uFear.value = 0.2;
    this.renderer.markShadows(3);
    A.rewind(3.2);
    this.rewindingClock = true;
    this.ui.fade(0, 250);
    const r0 = performance.now();
    await new Promise(r => { const step = () => { const k = (performance.now() - r0) / 3200; F.uRewind.value = 1 - Math.pow(k, 3) * 0.6; this.clock -= 0.35; if (k < 1) requestAnimationFrame(step); else r(); }; step(); });
    this.rewindingClock = false;
    F.uRewind.value = 0;
    this.ui.flash(150);
    this.startEvent(false);
    await sleepReal(1500);
    this.say(mem[0], 3600);
    await sleepReal(4000);
    this.say(mem[1], 4200);
    const ph = Math.min(3, this.loop);
    if (ph >= 1) { await sleepReal(4600); this.say([null, S.THOUGHTS.cold, S.THOUGHTS.rot, S.THOUGHTS.nightmare][ph], 3200); }
  }

  async finalCard() {
    this.phase = 'credits';
    this.player.canMove = false;
    await this.ui.card(['Конец пролога', `Смертей: ${this.deaths}`, 'Это был не последний раз.'], {hold: 4500, gap: 1500, cls: ['big', 'small', '']});
    this.onEnd?.();
  }

  // P.T.-style: the flat decays after every death. 0 warm, 1 cold, 2 rot, 3 nightmare
  applyLoopChanges(loop) {
    const W = this.world, ph = Math.min(3, loop), L = PHASE_LIGHT[ph];
    this.flags.clockBackwards = loop >= 1;
    if (W.extraDoor) W.extraDoor.visible = ph >= 2;
    if (W.livingClock) W.livingClock.pend.visible = ph < 2;
    this.lights.set('lampada', ph < 3);
    this.phases?.set(ph);
    for (const [name, f] of Object.entries(this.lights.fixtures)) {
      if (name === 'lampada') continue;
      f.base0 ??= f.base; f.color0 ??= f.light.color.clone();
      f.base = f.base0 * L.k;
      if (L.color) f.light.color.copy(f.color0).lerp(new THREE.Color(L.color), L.mix ?? 0.6); else f.light.color.copy(f.color0);
      this.lights.apply(f);
    }
    for (const f of Object.values(this.lights.fixtures)) for (const {m} of f.mats) { m.userData.em0 ??= m.emissive.clone(); m.emissive.copy(m.userData.em0); if (L.color) m.emissive.lerp(new THREE.Color(L.color), (L.mix ?? 0.6) * 0.8); }
    const M = this.world.M;
    for (const k of ['wallLiving', 'wallBedroom', 'wallHall', 'kitchenWall', 'ceiling']) { const m = M[k]; m.userData.c0 ??= m.color.clone(); m.color.copy(m.userData.c0); if (L.wall) m.color.multiply(new THREE.Color(L.wall)); }
    this.lights.set('red', ph >= 3);
    this.lights.envOn = L.env; this.scene.environmentIntensity = this.lights.mains ? L.env : 0.02;
    this.scene.fog.color.set(L.fog);
    this.film.uWarm.value = ph === 0 ? 1 : ph === 3 ? 0.3 : 0;
    this.hanged?.props && (this.hanged.props.visible = ph >= 3);
    if (ph >= 2) this.audio.drone('flies', 0.015, 180);
  }

  async ending(tok) {
    const W = this.world, A = this.audio, C = this.creature;
    this.phase = 'end';
    this.setOppositeLights(true);
    for (const s of W.chimneyLights) s.visible = true;
    W.farLights.visible = true; W.ourFacadeWindows.visible = true;
    W.tv.power = true; W.tv.setMode('testcard'); W.tv.crawl = 'АТАКА ЗАВЕРШЕНА. ПРОВЕРЬТЕ: ВСЁ ЛИ ВЫ ПОМНИТЕ?'; W.tv.crawlT = 0; A.tvTone(TV_POS);
    await this.wait(2); if (!this.alive(tok)) return;
    await this.say('Свет. Холодильник гудит. Всё?');
    this.ringing = 'code'; A.phoneRingOnce(PHONE_POS, 2);
    await this.wait(8.5); if (!this.alive(tok)) return;
    this.startRing('end');
    await this.until(() => this.flags.endCall); if (!this.alive(tok)) return;
    await this.wait(1.5);
    await this.say('Пять минут.');
    A.stop('tvtone'); W.tv.setMode('off');
    await this.wait(7); if (!this.alive(tok)) return;
    // she has her own key
    A.doorbell(V(8.05, 2.2, 5.97));
    await this.wait(2.2); if (!this.alive(tok)) return;
    A.lockTurn(V(8.05, 1, 6.15));
    await this.wait(1.2); if (!this.alive(tok)) return;
    W.doors.front.locked = false; W.doors.front.setTarget(0.8, 0.8); A.doorOpen(V(8.05, 1, 6.15), 0.3);
    C.show(8.05, 0, 6.6, Math.PI); C.setDisguise(true); C.lookAt = this.camera.position;
    await this.say('Бабушка!', 1500);
    C.walkPath(this.pathTo(C.pos, this.pos), 0.6);
    const voice = t => { if (!A.voice(t, 'door', C.headWorld())) A.murmur(C.headWorld(), 1.5, {pitch: 220, v: 0.15}); this.ui.subtitle('Бабушка', t, 2600); };
    voice(S.GRANDMA_END[0]);
    await this.until(() => Math.hypot(C.pos.x - this.pos.x, C.pos.z - this.pos.z) < 1.3 || !this.alive(tok)); if (!this.alive(tok)) return;
    C.walking = false;
    this.player.canMove = false; this.player.forced = {target: C.headWorld(), speed: 4};
    await this.wait(1.2); if (!this.alive(tok)) return;
    voice(S.GRANDMA_END[1]);
    await this.wait(2.4); if (!this.alive(tok)) return;
    // the face slides, the body unfolds to its real height
    this.film.uGlitch.value = 0.6; A.crack(A.now, 0.7); A.crack(A.now + 0.15, 0.6);
    await this.wait(0.25);
    C.setDisguise(false); this.film.uGlitch.value = 0;
    this.endingDeath = true;
    this.die('ending');
  }


  // ---------- peephole ----------
  peep() {
    if (this.peeping) return;
    this.peeping = true;
    this.player.canMove = false; this.player.canLook = false;
    this.savedFov = this.camera.fov;
    this.peepCam = {pos: this.camera.position.clone(), quat: this.camera.quaternion.clone()};
    this.film.uFish.value = 1;
    this.ui.crosshair(false);
    this.ui.hint('<span class="key">E</span> — отойти от двери', 0);
    this.audio.click(V(8.05, 1.5, 6.1), 600, 0.15);
    if (this.creature.visible && this.creatureAt === 'door') {
      this.creature.pos.set(8.05, 0, 7.25); this.creature.group.rotation.y = 0; // back to the door
      this.creature.lookAt = null;
      this.peepT = 0;
    } else if (!this.creature.visible) this.say(S.THOUGHTS.peepEmpty, 2200);
  }
  unpeep() {
    this.peeping = false;
    this.film.uFish.value = 0;
    this.camera.fov = this.savedFov; this.camera.updateProjectionMatrix();
    this.player.canMove = true; this.player.canLook = true;
    this.ui.crosshair(true); this.ui.hideHint();
    if (this.creature.visible && this.creatureAt === 'door') { this.creature.pos.set(8.05, 0, 6.95); this.creature.group.rotation.y = Math.PI; }
  }

  // ---------- per-frame ----------
  update(dt) {
    if (this.paused) return;
    this.tick(dt);
    const W = this.world, inp = this.input;
    // clock
    if (!this.rewindingClock && (this.phase === 'warm' || this.phase === 'arrive' || this.phase === 'event' || this.phase === 'end')) this.clock += dt * this.clockRate;
    const hh = Math.floor(this.clock / 60), mm = this.clock % 60;
    W.livingClock.setTime(hh, mm);
    if (this.flags.clockBackwards && this.phase === 'event') W.kitchenClock.setTime(hh, 120 - mm); else W.kitchenClock.setTime(hh, mm);
    if (W.livingClock.pend.visible && this.lights.mains !== undefined) W.livingClock.pend.rotation.z = Math.sin(performance.now() / 1000 * Math.PI) * 0.12;
    // kettle
    if (this.kettleT >= 0) {
      this.kettleT += dt;
      const k = Math.max(0, Math.min(1, (this.kettleT - 30) / 8));
      this.audio.loops.kettle?.whistle?.(k);
      if (this.kettleT > 38 && !this.flags.kettleSaid) { this.flags.kettleSaid = true; this.say(S.THOUGHTS.kettleDone); }
    }
    // tulle sway when balcony open
    const bo = W.doors.balcony.open;
    if (W.livingTulle) { W.livingTulle.rotation.x = -bo * 0.25 * (0.6 + 0.4 * Math.sin(performance.now() / 700)); W.livingTulle.position.z = 0.13 + bo * 0.15; }
    // stairs
    if (this.pos.z > 8.0 && this.phase !== 'death' && !this.flags.stairsSaid) { this.flags.stairsSaid = true; this.say(S.THOUGHTS.stairsDown); setTimeout(() => this.flags.stairsSaid = false, 6000); }
    // note
    if (this.noteOpen && (inp.hit('KeyE') || inp.hit('Escape') || inp.clicked)) { this.closeNote(); inp.pressed.delete('KeyE'); inp.clicked = false; }
    // peephole
    if (this.peeping) {
      this.camera.position.set(8.05, 1.52, 6.36);
      this.camera.rotation.set(-0.05, Math.PI, 0, 'YXZ');
      this.camera.fov = 125; this.camera.updateProjectionMatrix();
      if (this.creature.visible && this.creatureAt === 'door') {
        this.peepT += dt;
        // after a moment the head turns all the way around
        if (this.peepT > 2.2) { this.creature.head.rotation.y = Math.min(Math.PI, (this.peepT - 2.2) * 0.9); }
        if (this.peepT > 6) { this.unpeep(); this.audio.sting(0.4); this.creature.head.rotation.y = 0; }
      }
      if (inp.hit('KeyE') || inp.hit('Escape')) { this.unpeep(); inp.pressed.delete('KeyE'); }
      return;
    }
    // hiding: E leaves, Space holds the breath
    if (this.hidden) {
      if (inp.hit('KeyE') && !this.inCall) { this.unhide(); inp.pressed.delete('KeyE'); }
      const want = inp.down('Space') && this.gaspT <= 0;
      if (want && !this.holding) this.audio.breathIn(null, 0.06, 0.5);
      if (!want && this.holding) { this.audio.breathOut(null, this.breath < 0.25 ? 0.25 : 0.1, 1.0); }
      this.holding = want;
      if (this.holding) { this.breath -= dt / 13; if (this.breath <= 0) { this.breath = 0; this.holding = false; this.gaspT = 1.6; this.audio.breathIn(null, 0.35, 0.6); } }
      else { this.breath = Math.min(1, this.breath + dt / 5); this.gaspT -= dt; }
      this.ui.breath(this.breath, this.holding);
    }
    // stand up from armchair
    if (this.player.seated && !this.hidden && this.player.canMove && !this.inCall && (inp.hit('KeyW') || inp.hit('KeyA') || inp.hit('KeyS') || inp.hit('KeyD') || inp.hit('Space'))) {
      this.player.stand(3.75, 1.35, this.player.yaw);
      this.ui.hideHint();
      if (this.flags.restReady) this.flags.restReady = false;
    }
    // resting in the armchair in the warm evening (hold C) — the only use left for closing the eyes
    const wantClosed = inp.down('KeyC') && this.flags.restReady && this.player.canMove && !this.inCall;
    if (wantClosed !== this.eyesClosed) { this.eyesClosed = wantClosed; this.ui.eyes(wantClosed, true); if (wantClosed) this.audio.breathIn(null, 0.05, 0.8); }
    this.player.frozen = this.eyesClosed;
    if (this.eyesClosed) this.eyesHeld += dt; else this.eyesHeld = 0;
    if (this.flags.restReady && this.eyesHeld > 2.2) { this.eyesClosed = false; this.ui.eyes(false); this.rest(); }
    // P.T.: in the nightmare phase the front door leads back into the flat
    if (this.phase === 'event' && this.loop >= 3 && this.pos.z > 6.5 && !this.flags.looping) {
      this.flags.looping = true;
      this.film.uGlitch.value = 0.8; this.audio.rewind(0.6);
      setTimeout(() => { this.player.teleport(8.05, 4.6, 0, 0); this.world.doors.front.snap(0); this.film.uGlitch.value = 0; this.say(S.THOUGHTS.loopDoor); this.flags.looping = false; }, 350);
    }
    // flashlight
    if (inp.hit('KeyF') && this.flags.flashlight && !this.inCall) {
      if (this.hands.current !== 'flashlight') this.hands.hold('flashlight');
      this.hands.squeeze(); this.audio.click(this.camera.position, 500, 0.12); this.audio.creak(this.camera.position, 0.05, 0.12, 900);
    }
    // creature footsteps
    // fear vignette eases
    if (this.phase === 'event' && this.creatureActive) this.player.shake = Math.max(this.player.shake, 0.15);
  }
}

function sleepReal(ms) { return new Promise(r => setTimeout(r, ms)); }

export function roomOf(x, z) {
  for (const [k, r] of Object.entries(rooms)) if (x >= r.x0 - 0.05 && x <= r.x1 + 0.05 && z >= r.z0 - 0.05 && z <= r.z1 + 0.05) return k;
  if (z < 0 && z > -1.4 && x > 2.9 && x < 6.5) return 'balcony';
  if (z > 6.3) return 'landing';
  return 'none';
}
