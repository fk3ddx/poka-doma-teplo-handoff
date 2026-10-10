import * as THREE from 'three';
import {boxGeo, rboxGeo, cyl, tube} from './geo.js';
import {std} from './materials.js';
import * as T from './textures.js';

// Hinged door: leaf group pivots at hinge; local +x runs hinge -> latch, local z = thickness.
export class Door {
  constructor({scene, col, name, hinge, closedRot, openDelta, width, height = 1.98, thick = 0.04, build, colBox, maxOpen = 1, startOpen = 0, sounds}) {
    this.name = name;
    this.pivot = new THREE.Group();
    this.pivot.position.copy(hinge);
    this.closedRot = closedRot; this.openDelta = openDelta;
    this.leaf = new THREE.Group();
    this.pivot.add(this.leaf);
    build(this.leaf, width, height, thick);
    this.leaf.traverse(o => { if (o.isMesh) { o.castShadow = true; o.receiveShadow = true; } });
    scene.add(this.pivot);
    this.open = startOpen; this.target = startOpen; this.maxOpen = maxOpen;
    this.locked = false;
    this.speed = 1.6;
    this.col = colBox ? col.addBox(...colBox, {noOcclude: true}) : null;
    this.apply();
  }
  apply() {
    this.pivot.rotation.y = this.closedRot + this.openDelta * this.open;
    if (this.col) this.col.on = this.open < 0.35;
  }
  get isOpen() { return this.target > 0.5; }
  get isClosed() { return this.open < 0.02 && this.target < 0.02; }
  setTarget(v, speed = 1.6) { this.target = v; this.speed = speed; }
  toggle() { this.setTarget(this.target > 0.5 ? 0 : this.maxOpen); return this.target > 0.5; }
  snap(v) { this.open = this.target = v; this.apply(); }
  update(dt) {
    if (Math.abs(this.open - this.target) > 1e-4) {
      const d = this.target - this.open;
      const step = Math.sign(d) * Math.min(Math.abs(d), dt * this.speed * (0.4 + Math.min(1, Math.abs(d) * 3)));
      this.open += step;
      this.apply();
      return true;
    }
    return false;
  }
}

function lever(g, M, x, y, z, side) {
  const alu = std(0xbfbfb8, 0.35, 0.9);
  const rose = new THREE.Mesh(rboxGeo(0.04, 0.16, 0.012, 0.004), alu); rose.position.set(x, y - 0.03, z); g.add(rose);
  const h = new THREE.Mesh(tube([[0, 0, 0], [0, 0, side * 0.04], [-0.11, 0, side * 0.05]], 0.009, 10, 6), alu);
  h.position.set(x, y, z); g.add(h);
  const key = new THREE.Mesh(boxGeo(0.008, 0.02, 0.003), std(0x111111, 0.5)); key.position.set(x, y - 0.075, z + side * 0.007); g.add(key);
}

export function interiorLeaf(M, {glass = false, pattern = false} = {}) {
  return (g, w, h, t) => {
    const paint = M.doorPaint;
    const add = (geo, mat, x, y, z) => { const m = new THREE.Mesh(geo, mat); m.position.set(x, y, z); g.add(m); return m; };
    // stiles & rails
    const sw = 0.1;
    add(rboxGeo(sw, h, t, 0.005), paint, sw / 2, h / 2, 0);
    add(rboxGeo(sw, h, t, 0.005), paint, w - sw / 2, h / 2, 0);
    add(rboxGeo(w - sw * 2, 0.18, t, 0.004), paint, w / 2, 0.09, 0);
    add(rboxGeo(w - sw * 2, 0.1, t, 0.004), paint, w / 2, h - 0.05, 0);
    add(rboxGeo(w - sw * 2, 0.1, t, 0.004), paint, w / 2, h * 0.42, 0);
    // lower panel
    add(rboxGeo(w - sw * 2, h * 0.42 - 0.23, t * 0.5, 0.004), paint, w / 2, (0.18 + h * 0.42 - 0.05) / 2, 0);
    // upper part: glass or panel
    const uh = h - 0.1 - (h * 0.42 + 0.05);
    const uy = h * 0.42 + 0.05 + uh / 2;
    if (glass) {
      const gm = new THREE.MeshStandardMaterial({color: 0xf0ead8, roughness: 0.35, metalness: 0, transparent: true, opacity: 0.62, map: pattern ? T.noiseTex(128, 9) : null, side: THREE.DoubleSide, depthWrite: false});
      add(new THREE.PlaneGeometry(w - sw * 2, uh), gm, w / 2, uy, 0);
      // glazing beads
      for (const s of [-1, 1]) {
        add(boxGeo(w - sw * 2, 0.012, 0.012), paint, w / 2, uy + s * (uh / 2 - 0.006), t * 0.3);
        add(boxGeo(w - sw * 2, 0.012, 0.012), paint, w / 2, uy + s * (uh / 2 - 0.006), -t * 0.3);
      }
    } else {
      add(rboxGeo(w - sw * 2, uh, t * 0.5, 0.004), paint, w / 2, uy, 0);
    }
    lever(g, M, w - 0.065, 0.98, t / 2 + 0.006, 1);
    lever(g, M, w - 0.065, 0.98, -t / 2 - 0.006, -1);
    // hinges
    for (const y of [0.25, h - 0.25]) add(cyl(0.008, 0.008, 0.1, 8), M.brass, -0.002, y, 0);
  };
}

export function frontLeaf(M) {
  return (g, w, h, t) => {
    const add = (geo, mat, x, y, z, rx = 0) => { const m = new THREE.Mesh(geo, mat); m.position.set(x, y, z); m.rotation.x = rx; g.add(m); return m; };
    add(rboxGeo(w, h, t, 0.01), M.leatherette, w / 2, h / 2, 0);
    // brass upholstery nails on both faces: border lines
    const nail = M.brass;
    const ng = new THREE.SphereGeometry(0.006, 6, 4);
    for (const zf of [t / 2 + 0.002, -t / 2 - 0.002]) {
      for (let y = 0.05; y < h - 0.03; y += 0.06) { add(ng, nail, 0.04, y, zf); add(ng, nail, w - 0.04, y, zf); }
      for (let x = 0.07; x < w - 0.05; x += 0.06) { add(ng, nail, x, 0.04, zf); add(ng, nail, x, h - 0.04, zf); }
    }
    // inside: chain, latch knob, lock cylinder
    const zi = -t / 2;
    add(rboxGeo(0.05, 0.12, 0.03, 0.008), M.darkMetal, w - 0.09, 1.0, zi - 0.015);
    add(cyl(0.017, 0.017, 0.03, 12), M.brass, w - 0.09, 1.02, zi - 0.04, Math.PI / 2);
    add(rboxGeo(0.06, 0.03, 0.025, 0.006), M.chrome, w - 0.09, 0.86, zi - 0.012);
    add(boxGeo(0.07, 0.02, 0.02), M.chrome, w - 0.09, 1.25, zi - 0.01);
    // chain hanging
    for (let i = 0; i < 8; i++) add(new THREE.TorusGeometry(0.008, 0.002, 4, 8), M.chrome, w - 0.14 - i * 0.002, 1.24 - i * 0.016, zi - 0.02);
    // peephole
    add(cyl(0.012, 0.012, t + 0.02, 12), M.brass, w / 2, 1.52, 0, Math.PI / 2);
    // outside: number plate + lock
    const plate = new THREE.MeshStandardMaterial({map: T.label('60', {w: 128, h: 96, bg: '#c8b070', fg: '#2a1a0a', font: 'bold 64px PT Serif, serif'}), roughness: 0.35, metalness: 0.7});
    const pl = add(new THREE.PlaneGeometry(0.08, 0.06), plate, w / 2, 1.68, t / 2 + 0.004); pl.rotation.y = 0;
    add(cyl(0.017, 0.017, 0.012, 14), M.brass, w - 0.09, 1.02, t / 2 + 0.006, Math.PI / 2);
    add(rboxGeo(0.03, 0.12, 0.012, 0.006), M.brass, w - 0.09, 0.9, t / 2 + 0.006);
    // handle outside (fixed pull)
    add(tube([[0, 0, 0], [0, 0, 0.05], [0, 0.16, 0.05], [0, 0.16, 0]], 0.009, 12, 6), M.brass, w - 0.09, 1.1, t / 2);
  };
}

function balconyLeaf(M) {
  return (g, w, h, t) => {
    const paint = M.windowPaint;
    const add = (geo, mat, x, y, z) => { const m = new THREE.Mesh(geo, mat); m.position.set(x, y, z); g.add(m); return m; };
    for (const dz of [0, 0.2]) {
      add(rboxGeo(0.07, h, t, 0.004), paint, 0.035, h / 2, dz);
      add(rboxGeo(0.07, h, t, 0.004), paint, w - 0.035, h / 2, dz);
      add(rboxGeo(w, 0.07, t, 0.004), paint, w / 2, h - 0.035, dz);
      add(rboxGeo(w, 0.12, t, 0.004), paint, w / 2, 0.06, dz);
      add(rboxGeo(w, 0.07, t, 0.004), paint, w / 2, 0.75, dz);
      add(rboxGeo(w - 0.14, 0.62, t * 0.4, 0.004), paint, w / 2, 0.42, dz);
      const glass = new THREE.Mesh(new THREE.PlaneGeometry(w - 0.14, h - 0.86), M.glass);
      glass.position.set(w / 2, (0.785 + h - 0.07) / 2, dz); g.add(glass);
      add(boxGeo(0.035, h - 0.86, t * 0.8), paint, w / 2, (0.785 + h - 0.07) / 2, dz);
    }
    // latch handle (шпингалет) inside
    add(rboxGeo(0.02, 0.12, 0.025, 0.006), M.chrome, w - 0.06, 1.05, -t / 2 - 0.012);
    add(cyl(0.006, 0.006, 0.5, 6), M.chrome, w - 0.025, h - 0.3, -t / 2 - 0.006);
  };
}

export function buildDoors(scene, col, M) {
  const d = {};
  d.bedroom = new Door({scene, col, name: 'bedroom', hinge: new THREE.Vector3(2.85, 0, 4.15), closedRot: Math.PI / 2, openDelta: Math.PI / 2 * 0.95, width: 0.78, build: interiorLeaf(M), colBox: [2.8, 3.35, 2.9, 4.15], startOpen: 0.85});
  d.living = new Door({scene, col, name: 'living', hinge: new THREE.Vector3(6.05, 0, 4.45), closedRot: Math.PI, openDelta: -Math.PI / 2 * 0.92, width: 0.78, build: interiorLeaf(M, {glass: true, pattern: true}), colBox: [5.25, 4.4, 6.05, 4.5], startOpen: 1});
  d.kitchen = new Door({scene, col, name: 'kitchen', hinge: new THREE.Vector3(6.9, 0, 2.95), closedRot: 0, openDelta: Math.PI / 2 * 0.9, width: 0.68, build: interiorLeaf(M, {glass: true, pattern: true}), colBox: [6.9, 2.9, 7.6, 3.0], startOpen: 0.95});
  d.bath = new Door({scene, col, name: 'bath', hinge: new THREE.Vector3(4.65, 0, 5.6), closedRot: Math.PI / 2, openDelta: Math.PI / 2 * 0.9, width: 0.68, build: interiorLeaf(M), colBox: [4.6, 4.9, 4.7, 5.6]});
  d.bath.locked = true;
  d.front = new Door({scene, col, name: 'front', hinge: new THREE.Vector3(7.6, 0, 6.08), closedRot: 0, openDelta: Math.PI / 2 * 0.85, width: 0.9, height: 2.06, thick: 0.065, build: frontLeaf(M), colBox: [7.6, 6.0, 8.5, 6.3]});
  d.front.locked = true;
  d.balcony = new Door({scene, col, name: 'balcony', hinge: new THREE.Vector3(5.55, 0, -0.06), closedRot: Math.PI, openDelta: Math.PI / 2 * 0.95, width: 0.75, height: 2.28, thick: 0.05, build: balconyLeaf(M), colBox: [4.8, -0.35, 5.55, 0], startOpen: 0.16});
  // door stops (притвор) on the inside of the entrance: no light leaks around the leaf
  const stop = M.doorPaint;
  for (const [w, h, x, y] of [[0.04, 2.08, 7.59, 1.04], [0.04, 2.08, 8.51, 1.04], [0.96, 0.04, 8.05, 2.06]]) {
    const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, 0.05), stop); m.position.set(x, y, 6.03); m.castShadow = true; scene.add(m);
  }
  return d;
}
