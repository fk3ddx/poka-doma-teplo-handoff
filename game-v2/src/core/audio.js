import * as THREE from 'three';
import voiceManifest from '../assets/voice/manifest.json';
const voiceUrls = import.meta.glob('../assets/voice/*.wav', {eager: true, query: '?url', import: 'default'});

// Procedural audio. Everything is synthesised; nothing is sampled from other works.
const rand = (a, b) => a + Math.random() * (b - a);

export class Audio {
  constructor() {
    this.ctx = null; this.started = false; this.volume = 0.8;
    this.loops = {};
    this.roomOf = () => 'none';
  }
  start() {
    if (this.started) { this.ctx.resume(); return; }
    const ctx = this.ctx = new (window.AudioContext || window.webkitAudioContext)();
    this.master = ctx.createGain(); this.master.gain.value = this.volume;
    const comp = ctx.createDynamicsCompressor(); comp.threshold.value = -10; comp.ratio.value = 4;
    this.master.connect(comp).connect(ctx.destination);
    this.sfx = ctx.createGain(); this.sfx.connect(this.master);
    this.amb = ctx.createGain(); this.amb.connect(this.master);
    // reverb for the flat (short room) and stairwell
    this.verb = ctx.createConvolver(); this.verb.buffer = this.impulse(1.1, 2.8);
    this.verbGain = ctx.createGain(); this.verbGain.gain.value = 0.22;
    this.verb.connect(this.verbGain).connect(this.master);
    this.noiseBuf = this.makeNoise(4, 'white');
    this.pinkBuf = this.makeNoise(6, 'pink');
    this.brownBuf = this.makeNoise(6, 'brown');
    this.started = true;
    this.loadVoices();
  }

  async loadVoices() {
    this.voices = {};
    await Promise.all(Object.entries(voiceManifest).map(async ([text, id]) => {
      const url = voiceUrls[`../assets/voice/${id}.wav`]; if (!url) return;
      try { const ab = await (await fetch(url)).arrayBuffer(); this.voices[text] = await this.ctx.decodeAudioData(ab); } catch (e) { console.warn('voice', id, e); }
    }));
  }
  hasVoice(text) { return !!this.voices?.[text]; }
  // play a voice line with situational processing; returns duration (s) or 0
  voice(text, mode = 'phone', pos = null) {
    const buf = this.voices?.[text]; if (!buf || !this.started) return 0;
    const ctx = this.ctx, t = this.now;
    const rate = {phone: 0.95, strange: 0.86, door: 0.88, tv: 1.0, ear: 0.84, balcony: 0.86}[mode] ?? 1;
    const spatialPos = mode === 'phone' || mode === 'strange' ? null : pos;
    const out = this.spatial(spatialPos, {verb: {door: 0.6, balcony: 0.5, tv: 0.25, ear: 0.15}[mode] ?? 0, occlFreq: 1200, ref: mode === 'ear' ? 0.3 : 1});
    const g = this.gain(mode === 'ear' ? 1.4 : 1.1);
    let chain = g;
    const shaper = ctx.createWaveShaper(); const curve = new Float32Array(256); for (let i = 0; i < 256; i++) { const x = i / 128 - 1; curve[i] = Math.tanh(x * 2.2); } shaper.curve = curve;
    if (mode === 'phone' || mode === 'strange') { const hp = this.filter('highpass', 380), lp = this.filter('lowpass', 3200), pk = this.filter('peaking', 1500, 1); pk.gain.value = 6; g.connect(hp).connect(pk).connect(lp).connect(shaper).connect(out.input); const n = this.noiseSrc(this.noiseBuf, false), nb = this.filter('bandpass', 2000, 0.5), ng = this.gain(0.012); n.connect(nb).connect(ng).connect(out.input); n.start(t); n.stop(t + buf.duration / rate + 0.3); }
    else if (mode === 'door' || mode === 'balcony') { const lp = this.filter('lowpass', mode === 'door' ? 1100 : 1600); g.connect(lp).connect(out.input); }
    else if (mode === 'tv') { const hp = this.filter('highpass', 250), lp = this.filter('lowpass', 4500); g.connect(hp).connect(lp).connect(shaper).connect(out.input); }
    else { const hp = this.filter('highpass', 300); g.connect(hp).connect(out.input); }
    const play = (r, vol, detune = 0) => {
      const s = ctx.createBufferSource(); s.buffer = buf; s.playbackRate.value = r;
      if (detune) { const lfo = this.osc('sine', 0.7), lg = this.gain(detune); lfo.connect(lg).connect(s.detune); lfo.start(t); lfo.stop(t + buf.duration / r + 0.2); }
      const sg = this.gain(vol); s.connect(sg).connect(g); s.start(t); return s;
    };
    play(rate, 1, mode === 'strange' || mode === 'ear' ? 35 : 0);
    // uncanny octave-down double for the thing that imitates her
    if (['strange', 'door', 'ear', 'balcony'].includes(mode)) play(rate, mode === 'ear' ? 0.5 : 0.32, 0).detune.value = -1200;
    return buf.duration / rate;
  }
  setVolume(v) { this.volume = v; if (this.master) this.master.gain.value = v; }
  get now() { return this.ctx.currentTime; }

  makeNoise(sec, type) {
    const ctx = this.ctx, n = sec * ctx.sampleRate, b = ctx.createBuffer(2, n, ctx.sampleRate);
    for (let ch = 0; ch < 2; ch++) {
      const d = b.getChannelData(ch); let b0 = 0, b1 = 0, b2 = 0, last = 0;
      for (let i = 0; i < n; i++) {
        const w = Math.random() * 2 - 1;
        if (type === 'pink') { b0 = 0.997 * b0 + w * 0.029591; b1 = 0.985 * b1 + w * 0.032534; b2 = 0.95 * b2 + w * 0.048056; d[i] = (b0 + b1 + b2 + w * 0.1) * 0.9; }
        else if (type === 'brown') { last = (last + 0.02 * w) / 1.02; d[i] = last * 3.5; }
        else d[i] = w;
      }
    }
    return b;
  }
  impulse(sec, decay) {
    const ctx = this.ctx, n = sec * ctx.sampleRate, b = ctx.createBuffer(2, n, ctx.sampleRate);
    for (let ch = 0; ch < 2; ch++) { const d = b.getChannelData(ch); for (let i = 0; i < n; i++) d[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / n, decay); }
    return b;
  }

  // listener follows camera
  updateListener(cam) {
    if (!this.started) return;
    const l = this.ctx.listener, p = cam.position, f = new THREE.Vector3(0, 0, -1).applyQuaternion(cam.quaternion), u = new THREE.Vector3(0, 1, 0).applyQuaternion(cam.quaternion);
    const t = this.now;
    if (l.positionX) {
      l.positionX.setTargetAtTime(p.x, t, 0.02); l.positionY.setTargetAtTime(p.y, t, 0.02); l.positionZ.setTargetAtTime(p.z, t, 0.02);
      l.forwardX.setTargetAtTime(f.x, t, 0.02); l.forwardY.setTargetAtTime(f.y, t, 0.02); l.forwardZ.setTargetAtTime(f.z, t, 0.02);
      l.upX.setTargetAtTime(u.x, t, 0.02); l.upY.setTargetAtTime(u.y, t, 0.02); l.upZ.setTargetAtTime(u.z, t, 0.02);
    } else { l.setPosition(p.x, p.y, p.z); l.setOrientation(f.x, f.y, f.z, u.x, u.y, u.z); }
    this.listenerRoom = this.roomOf(p.x, p.z);
    // occlusion for positional loops
    for (const lp of Object.values(this.loops)) if (lp.pos && lp.occl) {
      const same = this.roomOf(lp.pos.x, lp.pos.z) === this.listenerRoom || lp.room === this.listenerRoom;
      lp.occl.frequency.setTargetAtTime(same ? 18000 : lp.occlFreq ?? 900, t, 0.15);
    }
  }

  // output chain: [filter] -> panner -> sfx (+ reverb send)
  spatial(pos, {ref = 1, rolloff = 1.2, verb = 0.3, occlFreq = 900, bus = null} = {}) {
    const ctx = this.ctx;
    const occl = ctx.createBiquadFilter(); occl.type = 'lowpass'; occl.frequency.value = 18000;
    if (pos) {
      const room = this.roomOf(pos.x, pos.z);
      if (room !== this.listenerRoom) occl.frequency.value = occlFreq;
      const p = ctx.createPanner(); p.panningModel = 'HRTF'; p.distanceModel = 'inverse'; p.refDistance = ref; p.rolloffFactor = rolloff; p.maxDistance = 200;
      if (p.positionX) { p.positionX.value = pos.x; p.positionY.value = pos.y; p.positionZ.value = pos.z; } else p.setPosition(pos.x, pos.y, pos.z);
      occl.connect(p); p.connect(bus ?? this.sfx);
      if (verb) { const s = ctx.createGain(); s.gain.value = verb; p.connect(s).connect(this.verb); }
      return {input: occl, panner: p, occl};
    }
    occl.connect(bus ?? this.sfx);
    if (verb) { const s = ctx.createGain(); s.gain.value = verb; occl.connect(s).connect(this.verb); }
    return {input: occl, occl};
  }
  movePanner(src, pos) { if (src?.panner?.positionX) { const t = this.now; src.panner.positionX.setTargetAtTime(pos.x, t, 0.05); src.panner.positionY.setTargetAtTime(pos.y, t, 0.05); src.panner.positionZ.setTargetAtTime(pos.z, t, 0.05); } }

  env(g, t, a, peak, d, sustain = 0, r = 0.1) {
    g.gain.cancelScheduledValues(t); g.gain.setValueAtTime(0.0001, t);
    g.gain.linearRampToValueAtTime(peak, t + a);
    g.gain.exponentialRampToValueAtTime(Math.max(0.0001, sustain || peak * 0.001), t + a + d);
    if (sustain) g.gain.exponentialRampToValueAtTime(0.0001, t + a + d + r);
  }
  noiseSrc(buf = this.noiseBuf, loop = true) { const s = this.ctx.createBufferSource(); s.buffer = buf; s.loop = loop; s.loopStart = Math.random(); return s; }
  osc(type, f) { const o = this.ctx.createOscillator(); o.type = type; o.frequency.value = f; return o; }
  filter(type, f, q = 1) { const b = this.ctx.createBiquadFilter(); b.type = type; b.frequency.value = f; b.Q.value = q; return b; }
  gain(v = 1) { const g = this.ctx.createGain(); g.gain.value = v; return g; }

  // ---------------- one-shots ----------------
  click(pos, f = 2600, v = 0.25) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: 0.15});
    const n = this.noiseSrc(this.noiseBuf, false), bp = this.filter('bandpass', f, 3), g = this.gain(0);
    n.connect(bp).connect(g).connect(o.input); this.env(g, t, 0.001, v, 0.03); n.start(t); n.stop(t + 0.1);
    const o2 = this.osc('square', f / 3), g2 = this.gain(0); o2.connect(g2).connect(o.input); this.env(g2, t, 0.001, v * 0.15, 0.02); o2.start(t); o2.stop(t + 0.05);
  }
  switchClick(pos) { this.click(pos, 1800, 0.35); setTimeout(() => this.click(pos, 900, 0.12), 25); }
  thump(pos, v = 0.6, f = 90) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: 0.35});
    const os = this.osc('sine', f), g = this.gain(0); os.frequency.exponentialRampToValueAtTime(f * 0.5, t + 0.2);
    os.connect(g).connect(o.input); this.env(g, t, 0.003, v, 0.35); os.start(t); os.stop(t + 0.5);
    const n = this.noiseSrc(this.brownBuf, false), lp = this.filter('lowpass', 600), g2 = this.gain(0);
    n.connect(lp).connect(g2).connect(o.input); this.env(g2, t, 0.002, v * 0.8, 0.15); n.start(t); n.stop(t + 0.3);
  }
  knock(pos, n = 3, gap = 0.32, v = 0.7, heavy = false) {
    if (!this.started) return;
    for (let i = 0; i < n; i++) {
      const t = this.now + i * gap * rand(0.92, 1.08);
      const o = this.spatial(pos, {verb: 0.4, occlFreq: 1500});
      const s = this.noiseSrc(this.noiseBuf, false), bp = this.filter('bandpass', heavy ? 160 : 260, 1.4), g = this.gain(0);
      s.connect(bp).connect(g).connect(o.input); this.env(g, t, 0.001, v * 1.5, heavy ? 0.16 : 0.09); s.start(t); s.stop(t + 0.3);
      const os = this.osc('sine', heavy ? 70 : 120), g2 = this.gain(0); os.connect(g2).connect(o.input); this.env(g2, t, 0.001, v * 0.8, 0.12); os.start(t); os.stop(t + 0.3);
    }
  }
  step(pos, surface = 'wood', v = 0.18, heavy = false) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: surface === 'concrete' ? 0.6 : 0.2, ref: 0.8});
    const s = this.noiseSrc(this.noiseBuf, false);
    const f = {wood: 700, lino: 1300, tile: 2400, concrete: 1800, carpet: 400}[surface] ?? 900;
    const bp = this.filter('bandpass', f * rand(0.85, 1.15), surface === 'carpet' ? 0.6 : 1.2), g = this.gain(0);
    s.connect(bp).connect(g).connect(o.input); this.env(g, t, 0.004, v * (heavy ? 2.2 : 1), heavy ? 0.16 : 0.07); s.start(t); s.stop(t + 0.3);
    const lo = this.osc('sine', heavy ? 55 : 85), g2 = this.gain(0); lo.connect(g2).connect(o.input); this.env(g2, t, 0.003, v * (heavy ? 1.6 : 0.6), 0.09); lo.start(t); lo.stop(t + 0.2);
    // parquet creak sometimes
    if (surface === 'wood' && Math.random() < (heavy ? 0.6 : 0.12)) this.creak(pos, v * (heavy ? 1.2 : 0.5), rand(0.15, 0.3));
  }
  creak(pos, v = 0.2, dur = 0.5, f = rand(380, 620)) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: 0.3});
    const os = this.osc('sawtooth', f), g = this.gain(0), bp = this.filter('bandpass', f * 2, 6);
    const lfo = this.osc('square', rand(18, 40)), lg = this.gain(f * 0.08); lfo.connect(lg).connect(os.frequency);
    os.frequency.linearRampToValueAtTime(f * rand(0.8, 1.25), t + dur);
    os.connect(bp).connect(g).connect(o.input);
    g.gain.setValueAtTime(0.0001, t); g.gain.linearRampToValueAtTime(v * 0.5, t + dur * 0.2); g.gain.linearRampToValueAtTime(v * 0.3, t + dur * 0.8); g.gain.linearRampToValueAtTime(0.0001, t + dur);
    os.start(t); lfo.start(t); os.stop(t + dur + 0.05); lfo.stop(t + dur + 0.05);
  }
  doorOpen(pos, v = 0.3) { this.click(pos, 1400, 0.3); this.creak(pos, v, 0.9, rand(250, 400)); }
  doorClose(pos, v = 0.5) { this.thump(pos, v, 110); this.click(pos, 1700, 0.25); }
  lockTurn(pos) { for (let i = 0; i < 2; i++) setTimeout(() => { this.click(pos, 2200, 0.3); setTimeout(() => this.click(pos, 1200, 0.25), 90); }, i * 380); }
  matchStrike(pos) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: 0.1});
    const s = this.noiseSrc(this.noiseBuf, false), bp = this.filter('bandpass', 3000, 0.8), g = this.gain(0);
    s.connect(bp).connect(g).connect(o.input);
    g.gain.setValueAtTime(0.0001, t); g.gain.linearRampToValueAtTime(0.35, t + 0.05); g.gain.exponentialRampToValueAtTime(0.05, t + 0.25); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.8);
    s.start(t); s.stop(t + 0.9);
  }
  whoosh(pos, v = 0.4) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: 0.1});
    const s = this.noiseSrc(this.pinkBuf, false), lp = this.filter('lowpass', 300), g = this.gain(0);
    lp.frequency.exponentialRampToValueAtTime(2500, t + 0.15); lp.frequency.exponentialRampToValueAtTime(900, t + 0.5);
    s.connect(lp).connect(g).connect(o.input); this.env(g, t, 0.02, v, 0.5); s.start(t); s.stop(t + 0.7);
  }
  doorbell(pos, v = 0.35) {
    // 90s electronic "nightingale" trill
    if (!this.started) return; const t0 = this.now, o = this.spatial(pos, {verb: 0.4, occlFreq: 2500});
    const notes = [2093, 2637, 2093, 2637, 2349, 2794, 2349, 2794, 1976, 2637, 1976, 2637];
    notes.forEach((f, i) => {
      const t = t0 + i * 0.085, os = this.osc('square', f), g = this.gain(0), lp = this.filter('lowpass', 4000);
      os.connect(lp).connect(g).connect(o.input); this.env(g, t, 0.003, v * 0.25, 0.08); os.start(t); os.stop(t + 0.1);
    });
  }
  pips(pos, last = true) {
    // time signal: 5 short + 1 long, 1 kHz
    if (!this.started) return;
    for (let i = 0; i < 6; i++) {
      const t = this.now + i, o = this.spatial(pos, {verb: 0.2, occlFreq: 1200}), os = this.osc('sine', 1000), g = this.gain(0);
      const dur = i === 5 && last ? 0.5 : 0.1;
      os.connect(g).connect(o.input); g.gain.setValueAtTime(0.0001, t); g.gain.linearRampToValueAtTime(0.18, t + 0.005); g.gain.setValueAtTime(0.18, t + dur); g.gain.linearRampToValueAtTime(0.0001, t + dur + 0.01);
      os.start(t); os.stop(t + dur + 0.05);
    }
  }
  busyTone(sec = 3) {
    if (!this.started) return; const t0 = this.now;
    for (let i = 0; i < sec / 0.7; i++) {
      const t = t0 + i * 0.7, os = this.osc('sine', 425), g = this.gain(0), bp = this.filter('bandpass', 1000, 0.5);
      os.connect(bp).connect(g).connect(this.sfx); g.gain.setValueAtTime(0.0001, t); g.gain.linearRampToValueAtTime(0.08, t + 0.01); g.gain.setValueAtTime(0.08, t + 0.35); g.gain.linearRampToValueAtTime(0.0001, t + 0.36);
      os.start(t); os.stop(t + 0.4);
    }
  }
  sting(v = 0.9) {
    // death sting: dissonant cluster + noise burst
    if (!this.started) return; const t = this.now;
    const o = this.spatial(null, {verb: 0.6});
    for (const f of [92, 97.5, 138, 146, 196, 207, 415, 440, 830]) {
      const os = this.osc('sawtooth', f), g = this.gain(0), lp = this.filter('lowpass', 3000);
      os.frequency.exponentialRampToValueAtTime(f * 0.7, t + 1.6);
      os.connect(lp).connect(g).connect(o.input); this.env(g, t, 0.005, v * 0.12, 1.6); os.start(t); os.stop(t + 1.8);
    }
    const n = this.noiseSrc(this.noiseBuf, false), g = this.gain(0); n.connect(g).connect(o.input); this.env(g, t, 0.002, v * 0.5, 0.5); n.start(t); n.stop(t + 0.7);
  }
  // wet tearing skin: clustered crackle bursts swept through a resonant band, plus low squelch
  rip(dur = 1.6, v = 0.8) {
    if (!this.started) return; const t0 = this.now, o = this.spatial(null, {verb: 0.2});
    const n = this.noiseSrc(this.noiseBuf, false), bp = this.filter('bandpass', 900, 3), g = this.gain(0);
    bp.frequency.setValueAtTime(500, t0); bp.frequency.exponentialRampToValueAtTime(3200, t0 + dur * 0.7); bp.frequency.exponentialRampToValueAtTime(700, t0 + dur);
    n.connect(bp).connect(g).connect(o.input);
    for (let i = 0; i < dur * 38; i++) { const tt = t0 + i / 38 + Math.random() * 0.02; g.gain.setValueAtTime(v * (0.3 + Math.random() * 0.9), tt); g.gain.setTargetAtTime(0.0001, tt + 0.004, 0.012); }
    n.start(t0); n.stop(t0 + dur + 0.1);
    for (let i = 0; i < 6; i++) {
      const tt = t0 + Math.random() * dur, sq = this.noiseSrc(this.brownBuf, false), lp = this.filter('lowpass', 400, 4), sg = this.gain(0);
      lp.frequency.setValueAtTime(600, tt); lp.frequency.exponentialRampToValueAtTime(120, tt + 0.25);
      sq.connect(lp).connect(sg).connect(o.input); this.env(sg, tt, 0.01, v * 0.9, 0.3); sq.start(tt); sq.stop(tt + 0.4);
    }
    for (let i = 0; i < 4; i++) { const tt = t0 + 0.1 + Math.random() * dur * 0.8; this.crack(tt, v * 0.6); }
  }
  crack(t, v = 0.5) {
    const o = this.spatial(null, {verb: 0.1}), n = this.noiseSrc(this.noiseBuf, false), hp = this.filter('highpass', 1800), g = this.gain(0);
    n.connect(hp).connect(g).connect(o.input); this.env(g, t, 0.001, v, 0.05); n.start(t); n.stop(t + 0.1);
  }
  // choked scream of the hero (formant 'a' through distortion), cut off
  scream(dur = 1.4, v = 0.35) {
    if (!this.started) return; const t = this.now, o = this.spatial(null, {verb: 0.25});
    const src = this.osc('sawtooth', 320), g = this.gain(0);
    const f1 = this.filter('bandpass', 850, 5), f2 = this.filter('bandpass', 1350, 6), mix = this.gain(1);
    const sh = this.ctx.createWaveShaper(); const c = new Float32Array(256); for (let i = 0; i < 256; i++) { const x = i / 128 - 1; c[i] = Math.tanh(x * 4); } sh.curve = c;
    src.connect(f1); src.connect(f2); f1.connect(mix); f2.connect(mix); mix.connect(sh).connect(g).connect(o.input);
    const vib = this.osc('sine', 9), vg = this.gain(25); vib.connect(vg).connect(src.frequency);
    src.frequency.setValueAtTime(260, t); src.frequency.linearRampToValueAtTime(420, t + 0.25); src.frequency.linearRampToValueAtTime(300, t + dur);
    g.gain.setValueAtTime(0.0001, t); g.gain.linearRampToValueAtTime(v, t + 0.08); g.gain.setValueAtTime(v, t + dur - 0.1); g.gain.linearRampToValueAtTime(0.0001, t + dur);
    src.start(t); vib.start(t); src.stop(t + dur + 0.05); vib.stop(t + dur + 0.05);
  }
  // the thing's shriek: detuned saws + noise, pitch dive
  shriek(dur = 1.2, v = 0.6) {
    if (!this.started) return; const t = this.now, o = this.spatial(null, {verb: 0.5});
    for (const f of [520, 553, 781, 1180]) { const s = this.osc('sawtooth', f), g = this.gain(0), bp = this.filter('bandpass', f * 1.5, 2); s.frequency.exponentialRampToValueAtTime(f * 0.45, t + dur); s.connect(bp).connect(g).connect(o.input); this.env(g, t, 0.01, v * 0.18, dur); s.start(t); s.stop(t + dur + 0.1); }
    const n = this.noiseSrc(this.noiseBuf, false), bp = this.filter('bandpass', 2500, 1), g = this.gain(0); n.connect(bp).connect(g).connect(o.input); this.env(g, t, 0.005, v * 0.5, dur * 0.8); n.start(t); n.stop(t + dur);
  }

  rewind(dur = 3.2) {
    if (!this.started) return; const t = this.now;
    const o = this.spatial(null, {verb: 0});
    const os = this.osc('sawtooth', 60), g = this.gain(0), bp = this.filter('bandpass', 800, 2);
    os.frequency.exponentialRampToValueAtTime(900, t + dur);
    bp.frequency.exponentialRampToValueAtTime(5000, t + dur);
    os.connect(bp).connect(g).connect(o.input);
    g.gain.setValueAtTime(0.0001, t); g.gain.linearRampToValueAtTime(0.18, t + 0.3); g.gain.setValueAtTime(0.18, t + dur - 0.2); g.gain.linearRampToValueAtTime(0.0001, t + dur);
    os.start(t); os.stop(t + dur + 0.1);
    const n = this.noiseSrc(this.noiseBuf, false), hp = this.filter('highpass', 2000), g2 = this.gain(0);
    n.connect(hp).connect(g2).connect(o.input);
    g2.gain.setValueAtTime(0.0001, t); g2.gain.linearRampToValueAtTime(0.12, t + 0.2); g2.gain.setValueAtTime(0.12, t + dur - 0.1); g2.gain.linearRampToValueAtTime(0.0001, t + dur);
    n.start(t); n.stop(t + dur + 0.1);
    // chirps of reversed "voices"
    for (let i = 0; i < 14; i++) {
      const tt = t + 0.2 + i * (dur - 0.4) / 14, c = this.osc('triangle', rand(300, 900)), cg = this.gain(0);
      c.frequency.exponentialRampToValueAtTime(rand(1200, 2400), tt + 0.12);
      c.connect(cg).connect(o.input); this.env(cg, tt, 0.01, 0.05, 0.12); c.start(tt); c.stop(tt + 0.15);
    }
    // stop clunk
    setTimeout(() => this.thump(null, 0.4, 60), dur * 1000);
  }
  powerDown() { if (!this.started) return; this.thump(null, 0.35, 50); const t = this.now, os = this.osc('sawtooth', 100), g = this.gain(0), lp = this.filter('lowpass', 400); os.frequency.exponentialRampToValueAtTime(30, t + 0.8); os.connect(lp).connect(g).connect(this.sfx); this.env(g, t, 0.01, 0.12, 0.8); os.start(t); os.stop(t + 1); }
  powerUp() { if (!this.started) return; const t = this.now, os = this.osc('sawtooth', 50), g = this.gain(0), lp = this.filter('lowpass', 500); os.connect(lp).connect(g).connect(this.sfx); this.env(g, t, 0.05, 0.08, 0.6); os.start(t); os.stop(t + 0.8); }
  breathIn(pos = null, v = 0.15, dur = 1.1) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: 0.1, ref: 0.3, rolloff: 2});
    const n = this.noiseSrc(this.pinkBuf, false), bp = this.filter('bandpass', 1100, 1.2), g = this.gain(0);
    bp.frequency.linearRampToValueAtTime(1600, t + dur);
    n.connect(bp).connect(g).connect(o.input);
    g.gain.setValueAtTime(0.0001, t); g.gain.linearRampToValueAtTime(v, t + dur * 0.6); g.gain.linearRampToValueAtTime(0.0001, t + dur);
    n.start(t); n.stop(t + dur + 0.05);
  }
  breathOut(pos = null, v = 0.12, dur = 1.4) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: 0.1, ref: 0.3, rolloff: 2});
    const n = this.noiseSrc(this.pinkBuf, false), bp = this.filter('bandpass', 700, 1), g = this.gain(0);
    n.connect(bp).connect(g).connect(o.input);
    g.gain.setValueAtTime(0.0001, t); g.gain.linearRampToValueAtTime(v, t + 0.15); g.gain.linearRampToValueAtTime(0.0001, t + dur);
    n.start(t); n.stop(t + dur + 0.05);
  }
  heartbeat(v = 0.4) { if (!this.started) return; this.thump(null, v, 55); setTimeout(() => this.thump(null, v * 0.7, 50), 180); }
  whisper(pos, dur = 1.6, v = 0.2) {
    if (!this.started) return; const t = this.now, o = this.spatial(pos, {verb: 0.4, ref: 0.6});
    const n = this.noiseSrc(this.noiseBuf, false), g = this.gain(0);
    const f1 = this.filter('bandpass', 2400, 4), f2 = this.filter('bandpass', 5200, 3);
    n.connect(f1); n.connect(f2); const m = this.gain(1); f1.connect(m); f2.connect(m); m.connect(g).connect(o.input);
    for (let i = 0; i < dur * 7; i++) { const tt = t + i / 7; f1.frequency.setValueAtTime(rand(1400, 3200), tt); g.gain.setValueAtTime(rand(0.2, 1) * v, tt); g.gain.setValueAtTime(0.0001, tt + rand(0.06, 0.12)); }
    n.start(t); n.stop(t + dur);
  }
  // muffled human voice "presence" (behind a door / over phone): formant babble
  murmur(pos, dur = 2, {pitch = 210, v = 0.18, phone = false, strange = false} = {}) {
    if (!this.started) return; const t = this.now;
    const o = this.spatial(pos, {verb: phone ? 0 : 0.35, occlFreq: 700, ref: 1});
    const src = this.osc('sawtooth', pitch), g = this.gain(0);
    const f1 = this.filter('bandpass', 700, 6), f2 = this.filter('bandpass', 1200, 6), mix = this.gain(1);
    src.connect(f1); src.connect(f2); f1.connect(mix); f2.connect(mix);
    let out = mix;
    if (phone) { const hp = this.filter('highpass', 350), lp = this.filter('lowpass', 3000); out.connect(hp).connect(lp); out = lp; const nn = this.noiseSrc(this.noiseBuf, false), ng = this.gain(0.01); nn.connect(ng).connect(o.input); nn.start(t); nn.stop(t + dur); }
    else { const lp = this.filter('lowpass', 900); out.connect(lp); out = lp; }
    out.connect(g).connect(o.input);
    const syl = 6.5;
    const vowels = [[730, 1090], [270, 2290], [570, 840], [530, 1840], [300, 870], [660, 1700]];
    for (let i = 0; i < dur * syl; i++) {
      const tt = t + i / syl, vw = vowels[Math.floor(Math.random() * vowels.length)];
      f1.frequency.setTargetAtTime(vw[0], tt, 0.02); f2.frequency.setTargetAtTime(vw[1], tt, 0.02);
      src.frequency.setTargetAtTime(pitch * (strange ? rand(0.6, 1.0) : rand(0.9, 1.15)) * (1 - i / (dur * syl) * 0.12), tt, 0.04);
      const pause = Math.random() < 0.12;
      g.gain.setTargetAtTime(pause ? 0.0001 : v * rand(0.6, 1), tt, 0.02);
      g.gain.setTargetAtTime(0.0001, tt + 0.11, 0.02);
    }
    src.start(t); src.stop(t + dur + 0.1);
  }

  // ---------------- loops ----------------
  loop(name, build, pos, opts) {
    if (!this.started || this.loops[name]) return this.loops[name];
    const o = this.spatial(pos, opts);
    const g = this.gain(0); g.connect(o.input);
    const lp = {out: g, nodes: [], pos, occl: o.occl, occlFreq: opts?.occlFreq, panner: o.panner, room: opts?.room};
    build(lp, g);
    this.loops[name] = lp;
    return lp;
  }
  fade(name, v, time = 0.5) { const lp = this.loops[name]; if (lp) lp.out.gain.setTargetAtTime(v, this.now, time / 3); }
  stop(name, time = 0.3) {
    const lp = this.loops[name]; if (!lp) return; delete this.loops[name];
    lp.out.gain.setTargetAtTime(0.0001, this.now, time / 3);
    setTimeout(() => { for (const n of lp.nodes) { try { n.stop?.(); } catch (e) {} try { n.disconnect(); } catch (e) {} } clearInterval(lp.timer); lp.out.disconnect(); }, time * 1000 + 300);
  }

  wind(pos, name = 'wind', v = 0.15) {
    return this.loop(name, (lp, g) => {
      const n = this.noiseSrc(this.pinkBuf), bp = this.filter('bandpass', 500, 0.8), lfo = this.osc('sine', 0.08), lg = this.gain(300);
      lfo.connect(lg).connect(bp.frequency);
      const am = this.gain(0.6), alfo = this.osc('sine', 0.13), alg = this.gain(0.4); alfo.connect(alg).connect(am.gain);
      n.connect(bp).connect(am).connect(g);
      n.start(); lfo.start(); alfo.start(); lp.nodes.push(n, lfo, alfo);
      g.gain.setTargetAtTime(v, this.now, 1);
    }, pos, {verb: 0.1, ref: 1.5, occlFreq: 600});
  }
  sleetOnGlass(pos, name, v = 0.1) {
    return this.loop(name, (lp, g) => {
      const n = this.noiseSrc(this.noiseBuf), hp = this.filter('highpass', 3500), am = this.gain(0);
      n.connect(hp).connect(am).connect(g); n.start(); lp.nodes.push(n);
      lp.timer = setInterval(() => { const t = this.now; for (let i = 0; i < 6; i++) { const tt = t + Math.random() * 0.2; am.gain.setValueAtTime(rand(0.2, 1), tt); am.gain.setTargetAtTime(0, tt + 0.003, 0.01); } }, 200);
      g.gain.setTargetAtTime(v, this.now, 1);
    }, pos, {verb: 0.05, ref: 0.6, rolloff: 1.6, occlFreq: 500});
  }
  fridge(pos, v = 0.06) {
    return this.loop('fridge', (lp, g) => {
      for (const [f, a] of [[50, 1], [100, 0.5], [150, 0.25], [300, 0.05]]) { const o = this.osc('sine', f), og = this.gain(a); o.connect(og).connect(g); o.start(); lp.nodes.push(o); }
      const n = this.noiseSrc(this.brownBuf), lpf = this.filter('lowpass', 250), ng = this.gain(0.4); n.connect(lpf).connect(ng).connect(g); n.start(); lp.nodes.push(n);
      g.gain.setTargetAtTime(v, this.now, 0.6);
    }, pos, {verb: 0.05, ref: 0.7, rolloff: 1.5, occlFreq: 300});
  }
  clock(pos, name = 'clock') {
    return this.loop(name, (lp, g) => {
      let tick = false;
      g.gain.value = 1;
      lp.timer = setInterval(() => {
        tick = !tick; const t = this.now;
        const n = this.noiseSrc(this.noiseBuf, false), bp = this.filter('bandpass', tick ? 3200 : 2500, 8), eg = this.gain(0);
        n.connect(bp).connect(eg).connect(g); this.env(eg, t, 0.001, 0.22, 0.03); n.start(t); n.stop(t + 0.06);
      }, 1000);
    }, pos, {verb: 0.25, ref: 0.6, rolloff: 1.4, occlFreq: 800});
  }
  burner(pos) {
    return this.loop('burner', (lp, g) => {
      const n = this.noiseSrc(this.pinkBuf), bp = this.filter('bandpass', 900, 0.7); n.connect(bp).connect(g); n.start(); lp.nodes.push(n);
      g.gain.setTargetAtTime(0.05, this.now, 0.3);
    }, pos, {verb: 0.05, ref: 0.5, occlFreq: 400});
  }
  kettle(pos) {
    return this.loop('kettle', (lp, g) => {
      const n = this.noiseSrc(this.brownBuf), lpf = this.filter('lowpass', 400), ng = this.gain(0.6); n.connect(lpf).connect(ng).connect(g); n.start(); lp.nodes.push(n);
      const w = this.osc('sine', 1800), wg = this.gain(0); const wv = this.osc('sine', 6), wvg = this.gain(30); wv.connect(wvg).connect(w.frequency);
      w.connect(wg).connect(g); w.start(); wv.start(); lp.nodes.push(w, wv);
      lp.whistle = (k) => { wg.gain.setTargetAtTime(k * 0.25, this.now, 0.4); w.frequency.setTargetAtTime(1700 + k * 500, this.now, 1); };
      g.gain.setTargetAtTime(0.1, this.now, 2);
    }, pos, {verb: 0.15, ref: 0.6, occlFreq: 900});
  }
  phoneRing(pos) {
    // Soviet electromechanical bell: 0.8s ring, 3.2s pause; hammer ~22 Hz on two inharmonic bells
    return this.loop('ring', (lp, g) => {
      g.gain.value = 1;
      const ring = () => {
        const t = this.now;
        for (let k = 0; k < 18; k++) {
          const tt = t + k / 22;
          for (const [f, a] of [[1180, 0.35], [1630, 0.25], [2740, 0.15], [3550, 0.1]]) {
            const o = this.osc('sine', f * (k % 2 ? 1.04 : 1)), eg = this.gain(0);
            o.connect(eg).connect(g); this.env(eg, tt, 0.001, a * 0.35, 0.09); o.start(tt); o.stop(tt + 0.12);
          }
        }
      };
      ring(); lp.timer = setInterval(ring, 4000);
    }, pos, {verb: 0.35, ref: 1.2, occlFreq: 2000});
  }
  phoneRingOnce(pos, times = 2) {
    // the grandma code: two rings, then hang up
    if (!this.started) return;
    for (let r = 0; r < times; r++) setTimeout(() => {
      const t = this.now; const o = this.spatial(pos, {verb: 0.35, ref: 1.2, occlFreq: 2000});
      for (let k = 0; k < 18; k++) { const tt = t + k / 22; for (const [f, a] of [[1180, 0.35], [1630, 0.25], [2740, 0.15]]) { const os = this.osc('sine', f * (k % 2 ? 1.04 : 1)), eg = this.gain(0); os.connect(eg).connect(o.input); this.env(eg, tt, 0.001, a * 0.35, 0.09); os.start(tt); os.stop(tt + 0.12); } }
    }, r * 4000);
  }
  tvStatic(pos) {
    return this.loop('tvstatic', (lp, g) => {
      const n = this.noiseSrc(this.noiseBuf), bp = this.filter('bandpass', 3000, 0.4); n.connect(bp).connect(g); n.start(); lp.nodes.push(n);
      g.gain.setTargetAtTime(0.06, this.now, 0.05);
    }, pos, {verb: 0.2, occlFreq: 1200});
  }
  tvTone(pos) {
    return this.loop('tvtone', (lp, g) => {
      const o = this.osc('sine', 1000); o.connect(g); o.start(); lp.nodes.push(o);
      g.gain.setTargetAtTime(0.025, this.now, 0.05);
    }, pos, {verb: 0.2, occlFreq: 1200});
  }
  drone(name = 'drone', v = 0.1, f = 41) {
    return this.loop(name, (lp, g) => {
      for (const [ff, a] of [[f, 1], [f * 1.5, 0.4], [f * 2.02, 0.3], [f * 3.97, 0.08]]) { const o = this.osc('sawtooth', ff), og = this.gain(a), lpf = this.filter('lowpass', 220); o.connect(lpf).connect(og).connect(g); o.start(); lp.nodes.push(o); }
      const n = this.noiseSrc(this.brownBuf), ng = this.gain(0.5); n.connect(ng).connect(g); n.start(); lp.nodes.push(n);
      g.gain.setTargetAtTime(v, this.now, 3);
    }, null, {verb: 0.4});
  }

  // TV concert: soft waltz with a vibrato lead, from the TV speaker
  music(pos, name = 'music', v = 0.16) {
    return this.loop(name, (lp, g) => {
      const speaker = this.filter('bandpass', 1400, 0.45);
      const shaper = this.ctx.createWaveShaper(); const curve = new Float32Array(256); for (let i = 0; i < 256; i++) { const x = i / 128 - 1; curve[i] = Math.tanh(x * 1.6); } shaper.curve = curve;
      speaker.connect(shaper).connect(g);
      const bpm = 132, beat = 60 / bpm;
      // A minor waltz: Am Dm G C F Dm E7 Am
      const chords = [[57, 60, 64], [62, 65, 69], [55, 59, 62], [60, 64, 67], [53, 57, 60], [62, 65, 69], [52, 56, 59, 62], [57, 60, 64]];
      const melody = [76, 74, 72, 74, 76, 76, 77, 76, 74, 72, 71, 72, 74, 72, 71, 69, 72, 71, 69, 68, 69, 71, 72, 74];
      const mf = m => 440 * Math.pow(2, (m - 69) / 12);
      let bar = 0, start = this.now + 0.1;
      const schedule = () => {
        while (start < this.now + 1.5) {
          const ch = chords[bar % chords.length];
          for (let b = 0; b < 3; b++) {
            const t = start + b * beat;
            const notes = b === 0 ? [ch[0] - 12] : ch;
            for (const n of notes) {
              const o = this.osc(b === 0 ? 'triangle' : 'sine', mf(n)), eg = this.gain(0);
              o.connect(eg).connect(speaker); this.env(eg, t, 0.01, b === 0 ? 0.22 : 0.07, beat * 0.9); o.start(t); o.stop(t + beat);
            }
          }
          const mi = (bar * 3) % melody.length;
          for (let b = 0; b < 3; b++) {
            const t = start + b * beat, f = mf(melody[(mi + b) % melody.length]);
            const o = this.osc('triangle', f), eg = this.gain(0), vib = this.osc('sine', 5.5), vg = this.gain(f * 0.012);
            vib.connect(vg).connect(o.frequency);
            o.connect(eg).connect(speaker); eg.gain.setValueAtTime(0.0001, t); eg.gain.linearRampToValueAtTime(0.11, t + 0.06); eg.gain.linearRampToValueAtTime(0.07, t + beat * 0.8); eg.gain.linearRampToValueAtTime(0.0001, t + beat * 0.98);
            o.start(t); vib.start(t); o.stop(t + beat); vib.stop(t + beat);
          }
          start += beat * 3; bar++;
        }
      };
      schedule(); lp.timer = setInterval(schedule, 400);
      g.gain.setTargetAtTime(v, this.now, 0.3);
    }, pos, {verb: 0.25, ref: 1, occlFreq: 700});
  }
}
