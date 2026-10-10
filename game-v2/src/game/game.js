import * as THREE from 'three';
import * as S from './script.js';
import {spots, rooms} from '../world/layout.js';

const V = (x, y, z) => new THREE.Vector3(x, y, z);
const ARMCHAIR_SEAT = {pos: V(3.5, 1.12, 0.9), yaw: -1.914, pitch: -0.08, yawRange: 1.5};
const TV_POS = V(6.1, 0.85, 1.8);
const PHONE_POS = V(6.82, 0.85, 3.7);
const DOOR_OUT = V(8.05, 1.5, 6.5);

// path graph for the creature
const NODES = {
  balcony: V(5.17, 0, -0.85), balconyDoor: V(5.17, 0, 0.35), living: V(4.7, 0, 2.0), livingS: V(5.0, 0, 3.6), livingDoor: V(5.65, 0, 4.45),
  hallB: V(5.65, 0, 5.25), hallA: V(7.6, 0, 4.9), front: V(8.05, 0, 5.6), kitchenDoor: V(7.25, 0, 2.95), kitchen: V(7.6, 0, 1.6),
  bedroomDoor: V(2.85, 0, 3.75), bedroom: V(1.6, 0, 2.6),
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
    const sw = (obj, light, pos) => I.add({obj, prompt: 'Выключатель', action: () => { const on = g.lights.toggle(light); obj.userData.set(on); g.audio.switchClick(pos); g.renderer.markShadows(2); g.onSwitch(light, on); }});
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
    I.proxy(this.scene, 8.48, 0.75, 2.55, 0.62, 1.4, 0.62, {prompt: 'Холодильник', action: () => g.say(g.lights.mains ? S.THOUGHTS.fridge : S.THOUGHTS.fridgeOff)});
    I.proxy(this.scene, 6.65, 1.62, 1.0, 0.1, 0.18, 0.28, {prompt: 'Радиоточка', action: () => g.say(S.THOUGHTS.radio)});
    I.proxy(this.scene, 2.52, 1.0, 2.3, 0.5, 1.8, 1.0, {prompt: 'Шифоньер', action: () => g.say(S.THOUGHTS.wardrobe)});
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
    this.phase = 'intro'; this.loop = 0; this.deaths = 0; this.flags = {};
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
    else if (kind === 'fake') { this.flags.fakeAnswered = true; await this.dialogue(S.FAKE_CALL); }
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
    if (d.target > 0.05) { d.setTarget(0, 1.4); setTimeout(() => { this.audio.doorClose(p, 0.4); this.audio.click(p, 2200, 0.3); }, 500); if (this.phase === 'warm') this.say(S.THOUGHTS.balconyClosed); }
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
    for (const l of this.world.streetLamps) { l.light.visible = on; l.halo.visible = on; l.mat.color.set(on ? 0xffa040 : 0x111111); }
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
    if (first) { this.snapshot(); this.ui.clearObjectives(); try { localStorage.setItem('atk-zap-checkpoint-v1', JSON.stringify({snap: this.snap, loop: this.loop, deaths: this.deaths, flags: {flashlight: !!this.flags.flashlight, slippers: true, tea: true}})); } catch (e) {} }
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
    W.tv.setMode(this.loop >= 2 ? 'face' : 'concert'); A.music(TV_POS);
    this.film.uFear.value = 0;
    this.renderer.markShadows(3);
    this.player.canMove = true;
    if (first) await this.ui.fade(0, 2500);

    // --- 22:00 time signal
    await this.wait(first ? 4 : 2); if (!this.alive(tok)) return;
    A.stop('music', 0.2); W.tv.setMode('clock'); A.pips(TV_POS); A.pips(W.radioPos);
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
    if (W.doors.balcony.target > 0.02) { await this.balconyEntry(tok); return; }
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
    await this.insidePhase(tok); if (!this.alive(tok)) return;
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
    for (const l of this.world.streetLamps) { l.light.visible = false; l.halo.visible = false; l.mat.color.set(0x111111); A.click(l.lens.position, 300, 0.4); await this.wait(0.9); if (!this.alive(tok)) return; }
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

  async insidePhase(tok) {
    const A = this.audio;
    this.creatureAt = 'inside';
    this.flags.insideNow = true;
    if (!this.flags.eyesTaught) this.ui.hint('Удерживайте <span class="key">C</span> — закрыть глаза', 0);
    // it appears far from the player, as if it was always there
    const spawns = [V(8.05, 0, 5.55), V(1.4, 0, 2.2), V(7.9, 0, 0.8), V(4.6, 0, 0.6)];
    const sp = spawns.reduce((b, p) => Math.hypot(p.x - this.pos.x, p.z - this.pos.z) > Math.hypot(b.x - this.pos.x, b.z - this.pos.z) ? p : b);
    this.creature.show(sp.x, 0, sp.z, Math.PI);
    A.creak(V(sp.x, 1, sp.z), 0.25, 1.2, 180);
    A.drone('drone', 0.1, 36);
    this.film.uFear.value = 0.6;
    const t0 = performance.now();
    let fakeSaid = false, ended = false;
    this.parallel((async () => {
      await this.wait(3);
      while (this.alive(tok) && !ended) { A.breathOut(this.creature.pos.clone().setY(1.8), 0.08, 1.6); await this.wait(2.4 + Math.random()); }
    })());
    // timeline while hunting: fake voice at 24s, real end at 36s
    this.parallel((async () => {
      await this.wait(24); if (!this.alive(tok)) return;
      if (this.eyesClosed) { const ear = this.camera.position.clone().add(new THREE.Vector3(Math.cos(this.player.yaw) * 0.25, 0, -Math.sin(this.player.yaw) * 0.25)); if (!A.voice(S.EAR_VOICE, 'ear', ear)) A.murmur(ear, 2.2, {pitch: 215, v: 0.18}); this.ui.subtitle('Бабушка?', S.EAR_VOICE, 3600, 'strange'); fakeSaid = true; }
    })());
    // hunting: slower, it lingers near the player when eyes are closed
    let reached = false;
    const huntTok = tok;
    const stopAt = 36;
    const t1 = this.elapsedEvent = 0;
    const startClock = performance.now();
    let elapsed = 0;
    while (this.alive(tok) && elapsed < stopAt) {
      const dist = Math.hypot(this.creature.pos.x - this.pos.x, this.creature.pos.z - this.pos.z);
      if (this.eyesClosed && dist < 1.6) {
        // stands right in front of the face, breathing. Do not open.
        this.creature.walking = false;
        const f = this.player.forward(); f.y = 0; f.normalize();
        const target = this.pos.clone().addScaledVector(f, 0.55);
        this.creature.pos.lerp(V(target.x, 0, target.z), 0.2);
        this.creature.faceTo(this.pos, 1);
        reached = true;
      } else if (!this.eyesClosed && dist < 1.15) {
        this.die(reached ? 'eyes' : 'look'); return;
      } else {
        const path = this.pathTo(this.creature.pos, this.pos);
        this.creature.walkPath(path, this.eyesClosed ? 0.55 : 0.9);
      }
      if (!this.eyesClosed && reached) { this.die('eyes'); return; }
      await this.wait(0.25); elapsed += 0.25;
    }
    if (!this.alive(tok)) return;
    ended = true;
    // power returns: fridge hum first, then warmth through the eyelids
    this.creature.hide(); this.creatureActive = false;
    A.stop('drone', 2);
    A.powerUp(); A.fridge(V(8.48, 0.8, 2.55));
    this.lights.setMains(true); this.renderer.markShadows(3);
    this.ui.eyes(this.eyesClosed, true);
    A.clock(V(4.75, 1.65, 4.35), 'clock');
    this.film.uFear.value = 0.2;
    await this.wait(1);
    this.ui.hideHint();
  }

  async ending(tok) {
    const W = this.world, A = this.audio;
    this.phase = 'end';
    await this.until(() => !this.eyesClosed); if (!this.alive(tok)) return;
    this.setOppositeLights(true);
    for (const s of W.chimneyLights) s.visible = true;
    W.farLights.visible = true; W.ourFacadeWindows.visible = true;
    W.tv.power = true; W.tv.setMode('testcard'); W.tv.crawl = 'АТАКА ЗАВЕРШЕНА. ПРОВЕРЬТЕ: ВСЁ ЛИ ВЫ ПОМНИТЕ?'; W.tv.crawlT = 0; A.tvTone(TV_POS);
    await this.wait(3); if (!this.alive(tok)) return;
    await this.say('Свет. Холодильник гудит. Всё?');
    await this.wait(2); if (!this.alive(tok)) return;
    this.ringing = 'code'; A.phoneRingOnce(PHONE_POS, 2);
    await this.wait(8.5); if (!this.alive(tok)) return;
    this.startRing('end');
    await this.until(() => this.flags.endCall); if (!this.alive(tok)) return;
    await this.wait(2);
    await this.say('Пять минут.');
    A.stop('tvtone');
    W.tv.setMode('off');
    await this.wait(9); if (!this.alive(tok)) return;
    A.doorbell(V(8.05, 2.2, 5.97));
    await this.wait(2.5);
    await this.say('Бабушка?');
    await this.wait(1.2);
    A.doorbell(V(8.05, 2.2, 5.97));
    await this.wait(1.6);
    // cut
    this.ui.fade(1, 0); A.stop('fridge', 0.05); A.stop('clock', 0.05); A.stop('wind', 0.05);
    this.player.canMove = false;
    await this.ui.card(['Конец пролога', `Смертей: ${this.deaths}`, 'Ты помнишь всё?'], {hold: 4000, gap: 1500, cls: ['big', 'small', '']});
    this.phase = 'credits';
    this.onEnd?.();
  }

  // ---------- death & rewind ----------
  async die(kind) {
    if (this.phase !== 'event' && this.phase !== 'end') return;
    this.abort();
    const tok = this.token;
    this.phase = 'death';
    this.deaths++;
    const A = this.audio, C = this.creature;
    this.stopRing(); this.inCall = false; this.ui.hideSubtitle(); this.ui.hideHint();
    this.eyesClosed = false; this.ui.eyes(false);
    this.player.canMove = false;
    if (this.player.seated) this.player.stand(this.player.seated.pos.x + 0.4, this.player.seated.pos.z + 0.3);
    // place it in front of the face if not already close
    const f = this.player.forward(); f.y = 0; f.normalize();
    const cam = this.camera.position;
    if (!C.visible || C.pos.distanceTo(V(cam.x, C.pos.y, cam.z)) > 2.2) C.show(cam.x + f.x * 1.3, kind === 'window' ? -0.15 : 0, cam.z + f.z * 1.3, 0);
    C.walking = false; C.faceTo(V(cam.x, 0, cam.z)); C.lookAt = cam.clone();
    const face = V(C.pos.x, C.pos.y + 1.95, C.pos.z);
    this.player.forced = {target: face, speed: 10};
    A.sting(0.9); A.heartbeat(0.5);
    const faceLight = this.faceLight; faceLight.intensity = 1.6;
    this.film.uFear.value = 1;
    const tStart = performance.now();
    await new Promise(r => {
      const step = () => {
        const k = (performance.now() - tStart) / 650;
        this.film.uGlitch.value = Math.min(1, k * 1.2);
        const target = V(cam.x + (C.pos.x - cam.x) * 0.2, C.pos.y, cam.z + (C.pos.z - cam.z) * 0.2);
        C.pos.lerp(V(target.x, C.pos.y, target.z), 0.12);
        this.player.forced.target.set(C.pos.x, C.pos.y + 1.95, C.pos.z);
        faceLight.position.set((C.pos.x + cam.x) / 2, C.pos.y + 1.9, (C.pos.z + cam.z) / 2);
        this.player.shake = 1;
        if (k < 1) requestAnimationFrame(step); else r();
      };
      step();
    });
    this.ui.flash(200);
    this.ui.fade(1, 0);
    faceLight.intensity = 0;
    this.film.uGlitch.value = 0;
    this.player.forced = null;
    C.hide();
    for (const k of Object.keys(A.loops)) if (k !== 'wind') A.stop(k, 0.05);
    await sleepReal(1400);
    // time label
    const mem = S.DEATH_MEMORY[kind] ?? S.DEATH_MEMORY.look;
    await this.ui.card([this.timeStr()], {hold: 600, gap: 400, cls: ['big']});
    // rewind: show the room under heavy tape distortion and spin the clocks back
    this.loop++;
    this.applyLoopChanges(this.loop);
    this.restore();
    this.player.sit(ARMCHAIR_SEAT);
    this.hands.hold(null);
    this.world.tv.power = true; this.world.tv.setMode('static');
    this.film.uRewind.value = 1; this.film.uFear.value = 0.2;
    this.renderer.markShadows(3);
    A.rewind(3.2);
    this.rewindingClock = true;
    this.ui.fade(0, 250);
    const r0 = performance.now();
    await new Promise(r => { const step = () => { const k = (performance.now() - r0) / 3200; this.film.uRewind.value = 1 - Math.pow(k, 3) * 0.6; this.clock -= 0.35; if (k < 1) requestAnimationFrame(step); else r(); }; step(); });
    this.rewindingClock = false;
    this.film.uRewind.value = 0;
    this.ui.flash(150);
    this.startEvent(false);
    await sleepReal(1500);
    this.say(mem[0], 3600);
    await sleepReal(4000);
    this.say(mem[1], 3600);
  }

  // P.T.-style: the flat is subtly different after each death
  applyLoopChanges(loop) {
    const W = this.world;
    this.flags.clockBackwards = loop >= 1;
    if (W.extraDoor) W.extraDoor.visible = loop >= 3;
    if (W.livingClock) W.livingClock.pend.visible = loop < 2;
    this.lights.set('lampada', loop < 4);
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
    // stand up from armchair
    if (this.player.seated && this.player.canMove && !this.inCall && (inp.hit('KeyW') || inp.hit('KeyA') || inp.hit('KeyS') || inp.hit('KeyD') || inp.hit('Space')) && !this.eyesClosed) {
      this.player.stand(3.75, 1.35, this.player.yaw);
      this.ui.hideHint();
      if (this.flags.restReady) this.flags.restReady = false;
    }
    // eyes
    const wantClosed = inp.down('KeyC') && this.player.canMove && !this.inCall && (this.phase === 'event' || this.phase === 'end' || this.flags.restReady);
    if (wantClosed !== this.eyesClosed) {
      this.eyesClosed = wantClosed; this.ui.eyes(wantClosed, this.lights.mains && this.phase !== 'event');
      if (wantClosed) { this.flags.eyesTaught = true; this.audio.breathIn(null, 0.05, 0.8); }
    }
    this.player.frozen = this.eyesClosed;
    if (this.eyesClosed) this.eyesHeld += dt; else this.eyesHeld = 0;
    if (this.flags.restReady && this.eyesHeld > 2.2) this.rest();
    // flashlight
    if (inp.hit('KeyF') && this.flags.flashlight && !this.inCall) {
      if (this.hands.current !== 'flashlight') this.hands.hold('flashlight');
      this.hands.squeeze(); this.audio.click(this.camera.position, 500, 0.12); this.audio.creak(this.camera.position, 0.05, 0.12, 900);
    }
    if (this.hands.lightOn) this.renderer.markShadows();
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
