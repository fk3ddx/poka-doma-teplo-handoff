import * as THREE from 'three';
import {mergeGeometries, mergeVertices} from 'three/addons/utils/BufferGeometryUtils.js';
import {RoundedBoxGeometry} from 'three/addons/geometries/RoundedBoxGeometry.js';

// Box with UVs in world metres (divided by `scale`), so tiled textures never stretch.
export function boxGeo(w, h, d, scale = 1) {
  const g = new THREE.BoxGeometry(w, h, d);
  const uv = g.attributes.uv;
  // face order: +x, -x, +y, -y, +z, -z; 4 verts each
  const dims = [[d, h], [d, h], [w, d], [w, d], [w, h], [w, h]];
  for (let f = 0; f < 6; f++) {
    for (let i = 0; i < 4; i++) {
      const k = f * 4 + i;
      uv.setXY(k, uv.getX(k) * dims[f][0] / scale, uv.getY(k) * dims[f][1] / scale);
    }
  }
  return g;
}

export function rboxGeo(w, h, d, r = 0.01, seg = 2) {
  r = Math.min(r, w / 2 - 1e-4, h / 2 - 1e-4, d / 2 - 1e-4);
  return new RoundedBoxGeometry(w, h, d, seg, Math.max(r, 1e-4));
}

export function planeGeo(w, h, scale = 1) {
  const g = new THREE.PlaneGeometry(w, h);
  const uv = g.attributes.uv;
  for (let i = 0; i < uv.count; i++) uv.setXY(i, uv.getX(i) * w / scale, uv.getY(i) * h / scale);
  return g;
}

export function cyl(rt, rb, h, seg = 16, open = false) {
  return new THREE.CylinderGeometry(rt, rb, h, seg, 1, open);
}

export function lathe(points, seg = 24) {
  return new THREE.LatheGeometry(points.map(p => new THREE.Vector2(p[0], p[1])), seg);
}

// Tube through points
export function tube(points, r = 0.01, seg = 32, radial = 8, closed = false) {
  const curve = new THREE.CatmullRomCurve3(points.map(p => new THREE.Vector3(...p)), closed, 'catmullrom', 0.2);
  return new THREE.TubeGeometry(curve, seg, r, radial, closed);
}

export function extrude(shapePts, depth, bevel = 0.004) {
  const s = new THREE.Shape(shapePts.map(p => new THREE.Vector2(p[0], p[1])));
  return new THREE.ExtrudeGeometry(s, {depth, bevelEnabled: bevel > 0, bevelThickness: bevel, bevelSize: bevel, bevelSegments: 2, curveSegments: 12});
}

// Collects static meshes per material and merges them into few draw calls.
export class Batcher {
  constructor() { this.groups = new Map(); }
  add(geo, mat, matrix, {cast = true, receive = true} = {}) {
    let g = geo.index ? geo.toNonIndexed() : geo.clone();
    if (!g.attributes.uv) {
      g.setAttribute('uv', new THREE.BufferAttribute(new Float32Array(g.attributes.position.count * 2), 2));
    }
    for (const k of Object.keys(g.attributes)) if (!['position', 'normal', 'uv'].includes(k)) g.deleteAttribute(k);
    g.applyMatrix4(matrix);
    const key = mat.uuid + (cast ? 'c' : '') + (receive ? 'r' : '');
    if (!this.groups.has(key)) this.groups.set(key, {mat, cast, receive, list: []});
    this.groups.get(key).list.push(g);
  }
  build(parent) {
    for (const {mat, cast, receive, list} of this.groups.values()) {
      const merged = mergeGeometries(list, false);
      const m = new THREE.Mesh(merged, mat);
      m.castShadow = cast; m.receiveShadow = receive;
      m.matrixAutoUpdate = false;
      parent.add(m);
    }
    this.groups.clear();
  }
}

// Small builder helper: place things relative to a local frame and send them to a batcher.
export class Builder {
  constructor(batcher, base = new THREE.Matrix4()) {
    this.b = batcher; this.stack = [base.clone()];
  }
  get m() { return this.stack[this.stack.length - 1]; }
  push(x = 0, y = 0, z = 0, ry = 0, rx = 0, rz = 0, s = 1) {
    const local = new THREE.Matrix4().compose(
      new THREE.Vector3(x, y, z),
      new THREE.Quaternion().setFromEuler(new THREE.Euler(rx, ry, rz, 'YXZ')),
      new THREE.Vector3(s, s, s));
    this.stack.push(this.m.clone().multiply(local));
    return this;
  }
  pop() { this.stack.pop(); return this; }
  add(geo, mat, x = 0, y = 0, z = 0, ry = 0, rx = 0, rz = 0, opts) {
    const local = new THREE.Matrix4().compose(
      new THREE.Vector3(x, y, z),
      new THREE.Quaternion().setFromEuler(new THREE.Euler(rx, ry, rz, 'YXZ')),
      new THREE.Vector3(1, 1, 1));
    this.b.add(geo, mat, this.m.clone().multiply(local), opts);
    return this;
  }
  box(w, h, d, mat, x, y, z, opts) { return this.add(boxGeo(w, h, d, opts?.uvScale ?? 1), mat, x, y, z, opts?.ry ?? 0, opts?.rx ?? 0, opts?.rz ?? 0, opts); }
  rbox(w, h, d, r, mat, x, y, z, opts) { return this.add(rboxGeo(w, h, d, r), mat, x, y, z, opts?.ry ?? 0, opts?.rx ?? 0, opts?.rz ?? 0, opts); }
  worldPos(x, y, z) { return new THREE.Vector3(x, y, z).applyMatrix4(this.m); }
}

export function mat4(x = 0, y = 0, z = 0, ry = 0, rx = 0, rz = 0, s = 1) {
  return new THREE.Matrix4().compose(new THREE.Vector3(x, y, z),
    new THREE.Quaternion().setFromEuler(new THREE.Euler(rx, ry, rz, 'YXZ')), new THREE.Vector3(s, s, s));
}

export {mergeGeometries, mergeVertices};

let _seed = 1337;
export function srand(s) { _seed = s >>> 0; }
export function rnd() { _seed = (_seed * 1664525 + 1013904223) >>> 0; return _seed / 4294967296; }
export const rr = (a, b) => a + (b - a) * rnd();
