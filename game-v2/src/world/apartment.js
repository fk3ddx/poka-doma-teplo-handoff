import * as THREE from 'three';
import {Batcher, Builder, boxGeo, rboxGeo, cyl, lathe, mat4, rnd, srand} from './geo.js';
import {materials, std} from './materials.js';
import {H, rooms, openings, wallSlabs, EXT_T, balcony} from './layout.js';

const EPS = 0.012;

// Generic quad with planar UV (tangent t, scale s)
function quadGeo(p0, p1, p2, p3, t, s = 1) {
  // p0 bottom-left, p1 bottom-right, p2 top-right, p3 top-left (CCW seen from front)
  const pos = [p0, p1, p2, p0, p2, p3];
  const n = new THREE.Vector3().subVectors(p1, p0).cross(new THREE.Vector3().subVectors(p3, p0)).normalize();
  const g = new THREE.BufferGeometry();
  const P = [], N = [], U = [];
  for (const p of pos) { P.push(p.x, p.y, p.z); N.push(n.x, n.y, n.z); U.push(p.dot(t) / s, p.y / s); }
  g.setAttribute('position', new THREE.Float32BufferAttribute(P, 3));
  g.setAttribute('normal', new THREE.Float32BufferAttribute(N, 3));
  g.setAttribute('uv', new THREE.Float32BufferAttribute(U, 2));
  return g;
}
// horizontal quad (floor/ceiling) with xz uv
function hquad(x0, z0, x1, z1, y, up = true, s = 1) {
  const a = new THREE.Vector3(x0, y, z1), b = new THREE.Vector3(x1, y, z1), c = new THREE.Vector3(x1, y, z0), d = new THREE.Vector3(x0, y, z0);
  const g = new THREE.BufferGeometry();
  const pts = up ? [a, b, c, a, c, d] : [a, c, b, a, d, c];
  const P = [], N = [], U = [];
  for (const p of pts) { P.push(p.x, p.y, p.z); N.push(0, up ? 1 : -1, 0); U.push(p.x / s, -p.z / s); }
  g.setAttribute('position', new THREE.Float32BufferAttribute(P, 3));
  g.setAttribute('normal', new THREE.Float32BufferAttribute(N, 3));
  g.setAttribute('uv', new THREE.Float32BufferAttribute(U, 2));
  return g;
}

function sideFrame(room, side) {
  // returns {a0,a1, point(a,y), tangent, axis, c}
  const {x0, z0, x1, z1} = room;
  if (side === 'n') return {a0: x0, a1: x1, P: (a, y) => new THREE.Vector3(a, y, z0), t: new THREE.Vector3(1, 0, 0), axis: 'z', c: z0, match: o => Math.abs(o.c1 - z0) < 0.02, flip: false};
  if (side === 's') return {a0: x0, a1: x1, P: (a, y) => new THREE.Vector3(a, y, z1), t: new THREE.Vector3(1, 0, 0), axis: 'z', c: z1, match: o => Math.abs(o.c0 - z1) < 0.02, flip: true};
  if (side === 'w') return {a0: z0, a1: z1, P: (a, y) => new THREE.Vector3(x0, y, a), t: new THREE.Vector3(0, 0, 1), axis: 'x', c: x0, match: o => Math.abs(o.c1 - x0) < 0.02, flip: true};
  return {a0: z0, a1: z1, P: (a, y) => new THREE.Vector3(x1, y, a), t: new THREE.Vector3(0, 0, 1), axis: 'x', c: x1, match: o => Math.abs(o.c0 - x1) < 0.02, flip: false};
}

function wallRect(b, F, mat, a0, a1, y0, y1, s = 1) {
  if (a1 - a0 < 1e-3 || y1 - y0 < 1e-3) return;
  let p0 = F.P(a0, y0), p1 = F.P(a1, y0), p2 = F.P(a1, y1), p3 = F.P(a0, y1);
  if (F.flip) [p0, p1, p2, p3] = [p1, p0, p3, p2];
  b.add(quadGeo(p0, p1, p2, p3, F.t, s), mat, new THREE.Matrix4());
}

export function buildApartment(scene, col) {
  const M = materials();
  const batch = new Batcher();
  const B = new Builder(batch);
  const root = new THREE.Group(); root.name = 'apartment'; scene.add(root);
  const trimMat = M.doorPaint;
  const baseboardMat = std(0x6b4a33, 0.55);
  const cornMat = M.ceiling;

  for (const [name, room] of Object.entries(rooms)) {
    const wallMat = M[room.wall];
    for (const side of ['n', 's', 'w', 'e']) {
      const F = sideFrame(room, side);
      let ops = openings.filter(o => o.axis === F.axis && F.match(o) && o.a1 > F.a0 && o.a0 < F.a1);
      const nw = room.noWall?.[side];
      if (nw) ops = ops.concat([{a0: nw[0], a1: nw[1], y0: 0, y1: H, type: 'gap'}]);
      ops.sort((p, q) => p.a0 - q.a0);
      // split
      let a = F.a0;
      for (const o of ops) {
        wallRect(batch, F, wallMat, a, o.a0, 0, H);
        wallRect(batch, F, wallMat, o.a0, o.a1, 0, o.y0);
        wallRect(batch, F, wallMat, o.a0, o.a1, o.y1, H);
        a = o.a1;
      }
      wallRect(batch, F, wallMat, a, F.a1, 0, H);

      // baseboard + cornice along this side (skip doors/gaps)
      if (name !== 'bath') {
        let s0 = F.a0;
        const segs = [];
        for (const o of ops) { if (o.y0 < 0.1) { segs.push([s0, o.a0]); s0 = o.a1; } }
        segs.push([s0, F.a1]);
        for (const [p, q] of segs) {
          if (q - p < 0.02) continue;
          const mid = (p + q) / 2, len = q - p;
          const c = F.P(mid, 0);
          const inward = side === 'n' ? [0, 0, 1] : side === 's' ? [0, 0, -1] : side === 'w' ? [1, 0, 0] : [-1, 0, 0];
          const along = F.axis === 'z';
          const bw = along ? len : 0.018, bd = along ? 0.018 : len;
          B.add(rboxGeo(along ? len : 0.02, 0.075, along ? 0.02 : len, 0.004), baseboardMat, c.x + inward[0] * 0.01, 0.0375, c.z + inward[2] * 0.01, 0, 0, 0, {cast: false});
        }
        // chamfer cornice (галтель): a 45-degree prism along the wall/ceiling corner
        const mid = (F.a0 + F.a1) / 2, len = F.a1 - F.a0;
        const c = F.P(mid, H);
        const along = F.axis === 'z';
        const cg = boxGeo(along ? len : 0.06, 0.06, along ? 0.06 : len);
        const m = new THREE.Matrix4().makeTranslation(c.x, H, c.z)
          .multiply(new THREE.Matrix4().makeRotationAxis(along ? new THREE.Vector3(1, 0, 0) : new THREE.Vector3(0, 0, 1), Math.PI / 4));
        batch.add(cg, cornMat, m, {cast: false});
      }
    }
    // floor & ceiling
    const fmat = M[room.floor];
    batch.add(hquad(room.x0, room.z0, room.x1, room.z1, 0, true), fmat, new THREE.Matrix4(), {cast: false});
    batch.add(hquad(room.x0, room.z0, room.x1, room.z1, H, false), name === 'kitchen' ? M.whitewash : M.ceiling, new THREE.Matrix4(), {cast: false});
  }

  // Opening reveals, thresholds, trims
  for (const o of openings) {
    const t = o.c1 - o.c0;
    const isZ = o.axis === 'z';
    const ac = (o.a0 + o.a1) / 2, aw = o.a1 - o.a0, cc = (o.c0 + o.c1) / 2;
    const revealMat = o.type === 'door' ? trimMat : M.whitewash;
    // side reveals
    for (const a of [o.a0, o.a1]) {
      const gx = isZ ? a : cc, gz = isZ ? cc : a;
      B.box(isZ ? 0.004 : t, o.y1 - o.y0, isZ ? t : 0.004, revealMat, gx, (o.y0 + o.y1) / 2, gz, {cast: false});
    }
    // top
    B.box(isZ ? aw : t, 0.004, isZ ? t : aw, revealMat, isZ ? ac : cc, o.y1, isZ ? cc : ac, {cast: false});
    if (o.type === 'door') {
      // wooden threshold
      B.rbox(isZ ? aw : t + 0.02, 0.02, isZ ? t + 0.02 : aw, 0.004, std(0x7a5236, 0.6), isZ ? ac : cc, 0.01, isZ ? cc : ac, {cast: false});
      // casings (наличники) on both faces
      for (const face of [o.c0, o.c1]) {
        const dir = face === o.c0 ? -1 : 1;
        const fc = face + dir * 0.008;
        const tw = 0.07;
        for (const a of [o.a0 - tw / 2 + 0.005, o.a1 + tw / 2 - 0.005]) {
          B.rbox(isZ ? tw : 0.016, o.y1 + tw - 0.005, isZ ? 0.016 : tw, 0.004, trimMat, isZ ? a : fc, (o.y1 + tw) / 2, isZ ? fc : a);
        }
        B.rbox(isZ ? aw + tw * 2 - 0.01 : 0.016, tw, isZ ? 0.016 : aw + tw * 2 - 0.01, 0.004, trimMat, isZ ? ac : fc, o.y1 + tw / 2 - 0.005, isZ ? fc : ac);
      }
    } else {
      // window sill (interior side) and bottom reveal
      B.box(isZ ? aw : t, 0.004, isZ ? t : aw, M.whitewash, isZ ? ac : cc, o.y0, isZ ? cc : ac, {cast: false});
      if (o.type === 'window') {
        B.rbox(aw + 0.12, 0.035, 0.26, 0.008, M.windowPaint, ac, o.y0 - 0.0175 + 0.03, o.c1 - 0.06);
        // exterior tin drip ledge
        B.box(aw + 0.04, 0.012, 0.12, std(0x8a8d8e, 0.4, 0.6), ac, o.y0 - 0.02, o.c0 - 0.03, {rx: 0.18});
      }
      // window/balcony frames
      buildWindowFrames(B, M, o);
    }
  }

  // collisions for walls
  for (const s of wallSlabs) {
    // cut doorways out of slabs
    const cuts = openings.filter(o => o.type !== 'window' && o.y0 < 0.1);
    let pieces = [s];
    for (const o of cuts) {
      const ob = o.axis === 'z' ? [o.a0, o.c0, o.a1, o.c1] : [o.c0, o.a0, o.c1, o.a1];
      const next = [];
      for (const p of pieces) {
        const ix0 = Math.max(p[0], ob[0]), iz0 = Math.max(p[1], ob[1]), ix1 = Math.min(p[2], ob[2]), iz1 = Math.min(p[3], ob[3]);
        if (ix0 >= ix1 - 1e-4 || iz0 >= iz1 - 1e-4) { next.push(p); continue; }
        if (o.axis === 'z') { next.push([p[0], p[1], ob[0], p[3]], [ob[2], p[1], p[2], p[3]]); }
        else { next.push([p[0], p[1], p[2], ob[1]], [p[0], ob[3], p[2], p[3]]); }
      }
      pieces = next.filter(p => p[2] - p[0] > 1e-3 && p[3] - p[1] > 1e-3);
    }
    for (const p of pieces) col.addBox(p[0], p[1], p[2], p[3]);
  }
  // walkable floors
  for (const r of Object.values(rooms)) col.addFloor(r.x0, r.z0, r.x1, r.z1, 0);
  for (const o of openings) if (o.type !== 'window') {
    if (o.axis === 'z') col.addFloor(o.a0, o.c0 - 0.05, o.a1, o.c1 + 0.05, 0);
    else col.addFloor(o.c0 - 0.05, o.a0, o.c1 + 0.05, o.a1, 0);
  }
  col.addFloor(balcony.x0, balcony.z0, balcony.x1, balcony.z1 + 0.01, 0);

  batch.build(root);
  return root;
}

// Double wooden frames, Soviet style. Interior frame at c1-0.07, exterior at c0+0.07.
function buildWindowFrames(B, M, o) {
  const ac = (o.a0 + o.a1) / 2, aw = o.a1 - o.a0;
  const fw = 0.055, fd = 0.045;
  const paint = M.windowPaint;
  const isBalc = o.type === 'balcony';
  if (isBalc) return; // balcony door is a dynamic object (see doors.js)
  for (const zc of [o.c1 - 0.07, o.c0 + 0.08]) {
    const y0 = o.y0, y1 = o.y1, h = y1 - y0;
    // outer box frame
    B.box(aw, fw, fd, paint, ac, y0 + fw / 2, zc);
    B.box(aw, fw, fd, paint, ac, y1 - fw / 2, zc);
    B.box(fw, h, fd, paint, o.a0 + fw / 2, (y0 + y1) / 2, zc);
    B.box(fw, h, fd, paint, o.a1 - fw / 2, (y0 + y1) / 2, zc);
    // central mullion
    B.box(fw * 1.3, h, fd, paint, ac, (y0 + y1) / 2, zc);
    // casement sashes (slightly proud) with glass
    const sash = (sa0, sa1, sy0, sy1) => {
      const sw = sa1 - sa0, sh = sy1 - sy0, sc = (sa0 + sa1) / 2, sy = (sy0 + sy1) / 2;
      const bw = 0.045;
      B.box(sw, bw, fd * 0.8, paint, sc, sy0 + bw / 2, zc);
      B.box(sw, bw, fd * 0.8, paint, sc, sy1 - bw / 2, zc);
      B.box(bw, sh, fd * 0.8, paint, sa0 + bw / 2, sy, zc);
      B.box(bw, sh, fd * 0.8, paint, sa1 - bw / 2, sy, zc);
      B.add(new THREE.PlaneGeometry(sw - bw * 2, sh - bw * 2), M.glass, sc, sy, zc, 0, 0, 0, {cast: false, receive: false});
    };
    const m = fw * 0.65;
    sash(o.a0 + fw, ac - m, y0 + fw, y1 - fw);
    // right side: big casement + fortochka on top
    const fy = y1 - fw - 0.42;
    sash(ac + m, o.a1 - fw, y0 + fw, fy);
    sash(ac + m, o.a1 - fw, fy, y1 - fw);
    // inner frame gets latch handles
    if (zc > o.c0 + 0.1) {
      for (const [hx, hy] of [[ac - m - 0.03, (y0 + y1) / 2], [ac + m + 0.03, (y0 + fy) / 2]]) {
        B.add(rboxGeo(0.018, 0.09, 0.02, 0.006), M.chrome, hx, hy, zc + 0.035);
      }
    }
  }
  // cotton wool with tinsel between frames (winter insulation habit)
  const wool = std(0xf2f0ea, 1);
  srand(Math.floor(ac * 100));
  for (let a = o.a0 + 0.08; a < o.a1 - 0.06; a += 0.06) {
    const g = new THREE.SphereGeometry(0.03 + rnd() * 0.02, 7, 5);
    g.scale(1.4, 0.55, 1);
    B.add(g, wool, a, o.y0 + 0.07, (o.c0 + o.c1) / 2, rnd() * 3, 0, 0, {cast: false});
  }
  const tinsel = std(0xd8c070, 0.25, 1, {emissive: 0x2a2208});
  for (let i = 0; i < 9; i++) {
    const a = o.a0 + 0.1 + rnd() * (aw - 0.2);
    B.add(new THREE.BoxGeometry(0.004, 0.003, 0.09 + rnd() * 0.12), tinsel, a, o.y0 + 0.095, (o.c0 + o.c1) / 2, rnd() * 3, 0, 0, {cast: false});
  }
}
