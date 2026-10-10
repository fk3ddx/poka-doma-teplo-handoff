import * as THREE from 'three';

// Look-at interactions. Each entry: {objs: Object3D[], prompt, enabled(), action(), dist}
export class Interactions {
  constructor(camera, input, ui, col) {
    this.cam = camera; this.input = input; this.ui = ui; this.col = col;
    this.list = [];
    this.ray = new THREE.Raycaster();
    this.ray.far = 2.2;
    this.current = null;
    this.enabled = true;
  }
  add(def) {
    const objs = Array.isArray(def.obj) ? def.obj : [def.obj];
    const e = {dist: 1.9, enabled: () => true, ...def, objs};
    for (const o of objs) o.traverse(c => { c.userData.inter = e; });
    this.list.push(e);
    return e;
  }
  // proxy box for static (batched) things
  proxy(scene, x, y, z, w, h, d, def) {
    const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), new THREE.MeshBasicMaterial({visible: false}));
    m.position.set(x, y, z); scene.add(m);
    return this.add({...def, obj: m});
  }
  occluded(origin, dir, dist) {
    for (const b of this.col.boxes) {
      if (!b.on || b.noOcclude) continue;
      const y0 = b.y0 ?? 0, y1 = b.y1 ?? 2.5;
      let tmin = 0, tmax = dist;
      for (const [o, d, lo, hi] of [[origin.x, dir.x, b.x0, b.x1], [origin.y, dir.y, y0, y1], [origin.z, dir.z, b.z0, b.z1]]) {
        if (Math.abs(d) < 1e-8) { if (o < lo || o > hi) { tmin = 1; tmax = 0; break; } continue; }
        let t1 = (lo - o) / d, t2 = (hi - o) / d; if (t1 > t2) [t1, t2] = [t2, t1];
        tmin = Math.max(tmin, t1); tmax = Math.min(tmax, t2);
        if (tmin > tmax) break;
      }
      if (tmin <= tmax && tmin > 0.02 && tmin < dist - 0.06) return true;
    }
    return false;
  }
  update() {
    let best = null;
    if (this.enabled) {
      this.ray.setFromCamera({x: 0, y: 0}, this.cam);
      const objs = [];
      for (const e of this.list) if (e.enabled()) for (const o of e.objs) if (o.parent) objs.push(o);
      const hits = this.ray.intersectObjects(objs, true);
      for (const h of hits) {
        let o = h.object; let e = null;
        while (o && !e) { e = o.userData.inter; o = o.parent; }
        if (!e || h.distance > e.dist) continue;
        if (this.occluded(this.ray.ray.origin, this.ray.ray.direction, h.distance)) break;
        best = e; break;
      }
    }
    this.current = best;
    const p = best ? (typeof best.prompt === 'function' ? best.prompt() : best.prompt) : null;
    this.ui.setPrompt(p);
    if (best && p && (this.input.hit('KeyE') || this.input.clicked)) best.action();
  }
}
