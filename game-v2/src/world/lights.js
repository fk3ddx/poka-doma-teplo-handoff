import * as THREE from 'three';

// All interior light sources. Each "fixture" has a light and optional emissive materials,
// so blackouts/flicker turn both off together.
export class Lights {
  constructor(scene) {
    this.scene = scene;
    this.fixtures = {};
    this.mains = true;     // building electricity
  }
  add(name, {pos, color = 0xffb46b, intensity = 20, distance = 9, decay = 2, shadow = false, mapSize = 512, bias = -0.002, mats = [], on = true, mains = true, type = 'point', target = null, angle = 0.9, penumbra = 0.6, emissive = 1, link = null}) {
    let l;
    if (type === 'spot') {
      l = new THREE.SpotLight(color, intensity, distance, angle, penumbra, decay);
      l.target.position.copy(target); this.scene.add(l.target);
    } else l = new THREE.PointLight(color, intensity, distance, decay);
    l.position.copy(pos);
    if (shadow) {
      l.castShadow = true;
      l.shadow.mapSize.set(mapSize, mapSize);
      l.shadow.bias = bias; l.shadow.normalBias = 0.02;
      l.shadow.camera.near = 0.05; l.shadow.camera.far = distance;
      l.shadow.radius = 3;
      l.shadow.autoUpdate = false; l.shadow.needsUpdate = true;
    }
    this.scene.add(l);
    const f = {light: l, base: intensity, mats: mats.map(m => ({m, e: m.emissiveIntensity ?? 1, col: m.emissive?.clone()})), on, mains, flicker: 0, mul: 1, emissive, link};
    this.fixtures[name] = f;
    this.apply(f);
    return f;
  }
  apply(f) {
    const on = f.link ? this.fixtures[f.link]?.on : f.on;
    const powered = on && (!f.mains || this.mains);
    const k = powered ? f.mul : 0;
    f.light.intensity = f.base * k;
    f.light.visible = f.light.castShadow ? k > 0.001 : true;
    for (const {m, e} of f.mats) m.emissiveIntensity = e * k * f.emissive;
  }
  set(name, on) { const f = this.fixtures[name]; if (!f) return; f.on = on; this.apply(f); for (const g of Object.values(this.fixtures)) if (g.link === name) this.apply(g); }
  toggle(name) { const f = this.fixtures[name]; this.set(name, !f.on); return f.on; }
  setMulAll(k) { for (const f of Object.values(this.fixtures)) { f.mul = k; this.apply(f); } }
  isOn(name) { const f = this.fixtures[name]; return f.on && (!f.mains || this.mains); }
  setMains(on) { this.mains = on; for (const f of Object.values(this.fixtures)) this.apply(f); this.scene.environmentIntensity = on ? 0.18 : 0.02; }
  setMul(name, k) { const f = this.fixtures[name]; f.mul = k; this.apply(f); }
  update(dt, t) {
    for (const f of Object.values(this.fixtures)) {
      if (f.flicker > 0) {
        f.flicker -= dt;
        f.mul = f.flicker > 0 ? (Math.random() > 0.35 ? 0.2 + Math.random() * 0.9 : 0) : 1;
        this.apply(f);
      }
    }
  }
  flicker(name, dur = 1) { const f = this.fixtures[name]; if (f) f.flicker = dur; }
  flickerAll(dur = 1) { for (const k of Object.keys(this.fixtures)) if (this.fixtures[k].mains) this.flicker(k, dur * (0.7 + Math.random() * 0.6)); }
}
