import * as THREE from 'three';
import {boxGeo, rboxGeo, cyl, lathe, tube, rnd, rr, srand} from '../geo.js';
import {std} from '../materials.js';
import * as T from '../textures.js';

// ---------- Cast-iron radiator (МС-140) with riser pipes ----------
export function radiator(B, M, x, z, sections = 8, ry = 0, {riser = true, ceilH = 2.5} = {}) {
  B.push(x, 0, z, ry);
  const pitch = 0.093, w = sections * pitch;
  const mat = M.radiator;
  for (let i = 0; i < sections; i++) {
    const sx = -w / 2 + pitch / 2 + i * pitch;
    // two columns per section, rounded
    for (const dz of [-0.035, 0.035]) B.add(rboxGeo(0.07, 0.5, 0.05, 0.02), mat, sx, 0.42, dz);
    // header knuckles top/bottom
    for (const y of [0.15, 0.69]) B.add(rboxGeo(0.085, 0.07, 0.13, 0.03), mat, sx, y, 0);
    // fin between columns
    B.add(boxGeo(0.012, 0.42, 0.06), mat, sx, 0.42, 0);
  }
  // end plugs + feed pipes
  const pipe = M.radiator;
  if (riser) {
    const px = w / 2 + 0.09;
    B.add(cyl(0.016, 0.016, ceilH, 10), pipe, px, ceilH / 2, -0.02);
    B.add(cyl(0.013, 0.013, 0.12, 8), pipe, w / 2 + 0.04, 0.69, 0, 0, 0, Math.PI / 2);
    B.add(cyl(0.013, 0.013, 0.12, 8), pipe, w / 2 + 0.04, 0.15, 0, 0, 0, Math.PI / 2);
    // valve
    B.add(cyl(0.022, 0.022, 0.05, 10), M.darkMetal, w / 2 + 0.03, 0.69, 0, 0, 0, Math.PI / 2);
    B.add(cyl(0.03, 0.03, 0.012, 12), M.bakelite, w / 2 + 0.03, 0.75, 0);
  }
  // wall brackets
  for (const bx of [-w / 3, w / 3]) B.add(boxGeo(0.02, 0.04, 0.08), M.darkMetal, bx, 0.6, -0.08);
  B.pop();
}

// ---------- Curtain with folds (displaced plane) ----------
export function foldedPlane(w, h, folds = 8, depth = 0.04, segX = 48, segY = 6, gather = 0) {
  const g = new THREE.PlaneGeometry(w, h, segX, segY);
  const p = g.attributes.position;
  for (let i = 0; i < p.count; i++) {
    const x = p.getX(i), y = p.getY(i);
    const u = (x / w + 0.5);
    const v = (y / h + 0.5);
    let z = Math.sin(u * Math.PI * 2 * folds) * depth * (0.7 + 0.3 * Math.sin(u * 13.1 + 1.7));
    // gathered at bottom (tie-back) or swaying hem
    if (gather) { const k = (1 - v) * gather; p.setX(i, x * (1 - k * 0.6)); z *= 1 + k; }
    p.setZ(i, z);
  }
  g.computeVertexNormals();
  return g;
}

// Curtain set on a window opening (a0..a1 along x, at wall z), returns tulle mesh for animation
export function curtains(B, M, scene, {a0, a1, z, ceil = 2.5, heavy = true, tulleMat, drapeMat, axis = 'x', tie = false, tulle0 = null, tulle1 = null}) {
  const w = a1 - a0 + 0.5, cx = (a0 + a1) / 2;
  // rail (карниз): wooden board with brass ends
  B.add(rboxGeo(w + 0.1, 0.05, 0.06, 0.01), M.walnut, cx, ceil - 0.06, z + 0.09);
  // tulle
  const t0 = tulle0 ?? a0 - 0.25, t1 = tulle1 ?? a1 + 0.25, tw = t1 - t0;
  const tg = foldedPlane(tw, ceil - 0.12, Math.round(tw * 18), 0.025, 120, 4);
  const tulle = new THREE.Mesh(tg, tulleMat ?? M.tulle);
  tulle.position.set((t0 + t1) / 2, (ceil - 0.12) / 2, z + 0.13);
  tulle.renderOrder = 2;
  scene.add(tulle);
  // heavy drapes on the sides
  if (heavy) {
    const dw = 0.55;
    for (const side of [-1, 1]) {
      const dg = foldedPlane(dw, ceil - 0.1, 5, 0.05, 40, 8, tie ? 0.6 : 0);
      B.add(dg, drapeMat ?? M.jacquardCurtain, cx + side * (w / 2 - dw / 2 + 0.05), (ceil - 0.1) / 2, z + 0.2);
    }
  }
  return tulle;
}

// ---------- Light switch / socket ----------
export function lightSwitch(scene, M, x, y, z, ry) {
  const g = new THREE.Group();
  const base = new THREE.Mesh(cyl(0.035, 0.038, 0.022, 20), M.whitePlastic);
  base.rotation.x = Math.PI / 2; g.add(base);
  const key = new THREE.Mesh(rboxGeo(0.022, 0.034, 0.012, 0.004), std(0xd8d3c4, 0.35));
  key.position.z = 0.014; g.add(key);
  g.position.set(x, y, z); g.rotation.y = ry;
  scene.add(g);
  g.userData.key = key;
  g.userData.set = on => { key.rotation.x = on ? -0.25 : 0.25; };
  g.userData.set(true);
  return g;
}
export function socket(B, M, x, y, z, ry) {
  B.push(x, y, z, ry);
  B.add(cyl(0.033, 0.036, 0.018, 20), M.whitePlastic, 0, 0, 0.009, 0, Math.PI / 2);
  for (const dx of [-0.01, 0.01]) B.add(cyl(0.0035, 0.0035, 0.02, 6), M.black, dx, 0, 0.012, 0, Math.PI / 2);
  B.pop();
}

// ---------- Picture frame with photo ----------
export function frame(B, M, x, y, z, ry, w, h, photoKind, frameMat) {
  B.push(x, y, z, ry);
  const fm = frameMat ?? M.walnut;
  const t = 0.025, d = 0.02;
  B.add(rboxGeo(w, t, d, 0.004), fm, 0, h / 2 - t / 2, d / 2);
  B.add(rboxGeo(w, t, d, 0.004), fm, 0, -h / 2 + t / 2, d / 2);
  B.add(rboxGeo(t, h, d, 0.004), fm, -w / 2 + t / 2, 0, d / 2);
  B.add(rboxGeo(t, h, d, 0.004), fm, w / 2 - t / 2, 0, d / 2);
  const pm = new THREE.MeshStandardMaterial({map: T.photo(photoKind), roughness: 0.4});
  B.add(new THREE.PlaneGeometry(w - t * 1.6, h - t * 1.6), pm, 0, 0, 0.006);
  B.pop();
}

// ---------- Books row in a shelf, using the spine atlas ----------
let atlas = null;
export function books(B, x0, x1, y, z, depth = 0.2, ry = 0, {lean = true, maxH = 0.3} = {}) {
  atlas ??= T.bookAtlas();
  const covers = atlas.map;
  const mats = [new THREE.MeshStandardMaterial({map: covers, roughness: 0.8})];
  const page = std(0xe8dfc8, 0.9);
  let x = x0;
  while (x < x1 - 0.02) {
    const s = atlas.spines[Math.floor(rnd() * atlas.spines.length)];
    const th = Math.min(s[1] * 1024 / 1000, 0.05) * (0.8 + rnd() * 0.5);
    if (x + th > x1) break;
    const h = maxH * (0.7 + rnd() * 0.3), d = depth * (0.8 + rnd() * 0.2);
    const g = new THREE.BoxGeometry(th, h, d);
    // map spine (+z face) to atlas strip
    const uv = g.attributes.uv;
    for (let i = 0; i < uv.count; i++) {
      const face = Math.floor(i / 4);
      if (face === 4) uv.setXY(i, s[0] + uv.getX(i) * s[1], uv.getY(i));
      else uv.setXY(i, s[0] + 0.3 * s[1], 0.5);
    }
    B.push(0, 0, 0, ry);
    B.add(g, mats[0], x + th / 2, y + h / 2, z + (depth - d) / 2);
    B.pop();
    x += th + 0.002;
  }
}

// ---------- Wall clock with pendulum (dynamic hands) ----------
export function pendulumClock(scene, M, x, y, z, ry) {
  const g = new THREE.Group(); g.position.set(x, y, z); g.rotation.y = ry; scene.add(g);
  const wood = M.cherry;
  const add = (geo, mat, px, py, pz) => { const m = new THREE.Mesh(geo, mat); m.position.set(px, py, pz); m.castShadow = true; g.add(m); return m; };
  add(rboxGeo(0.34, 0.74, 0.11, 0.012), wood, 0, 0, 0.055);
  add(rboxGeo(0.38, 0.06, 0.13, 0.01), wood, 0, 0.39, 0.065);
  add(rboxGeo(0.36, 0.04, 0.13, 0.01), wood, 0, -0.38, 0.065);
  const face = add(new THREE.CircleGeometry(0.12, 40), new THREE.MeshStandardMaterial({map: T.clockFace('wall'), roughness: 0.5}), 0, 0.2, 0.112);
  add(new THREE.TorusGeometry(0.125, 0.008, 8, 40), M.brass, 0, 0.2, 0.112);
  // pendulum window
  add(new THREE.PlaneGeometry(0.24, 0.36), std(0x120c08, 0.8), 0, -0.14, 0.111);
  const pend = new THREE.Group(); pend.position.set(0, 0.05, 0.1); g.add(pend);
  const rod = new THREE.Mesh(boxGeo(0.006, 0.3, 0.003), M.brass); rod.position.y = -0.15; pend.add(rod);
  const bob = new THREE.Mesh(cyl(0.035, 0.035, 0.006, 24), M.brass); bob.rotation.x = Math.PI / 2; bob.position.y = -0.3; pend.add(bob);
  add(new THREE.PlaneGeometry(0.25, 0.37), M.glass, 0, -0.14, 0.118);
  const hands = clockHands(g, 0, 0.2, 0.118, 0.065, 0.095, M.black);
  return {group: g, pend, ...hands};
}
export function clockHands(parent, x, y, z, hl, ml, mat) {
  const h = new THREE.Group(), m = new THREE.Group();
  h.position.set(x, y, z); m.position.set(x, y, z + 0.002);
  const hg = new THREE.Mesh(boxGeo(0.008, hl, 0.002), mat); hg.position.y = hl / 2 - 0.01; h.add(hg);
  const mg = new THREE.Mesh(boxGeo(0.005, ml, 0.002), mat); mg.position.y = ml / 2 - 0.01; m.add(mg);
  parent.add(h, m);
  return {hour: h, minute: m, setTime(hh, mm) { h.rotation.z = -((hh % 12) + mm / 60) / 12 * Math.PI * 2; m.rotation.z = -mm / 60 * Math.PI * 2; }};
}

export function kitchenClock(scene, M, x, y, z, ry) {
  const g = new THREE.Group(); g.position.set(x, y, z); g.rotation.y = ry; scene.add(g);
  const body = new THREE.Mesh(cyl(0.14, 0.14, 0.04, 40), std(0xb8432f, 0.4)); body.rotation.x = Math.PI / 2; body.position.z = 0.02; g.add(body);
  const face = new THREE.Mesh(new THREE.CircleGeometry(0.125, 40), new THREE.MeshStandardMaterial({map: T.clockFace('kitchen'), roughness: 0.5})); face.position.z = 0.041; g.add(face);
  const glass = new THREE.Mesh(new THREE.CircleGeometry(0.13, 40), M.glass); glass.position.z = 0.05; g.add(glass);
  return {group: g, ...clockHands(g, 0, 0, 0.045, 0.07, 0.1, M.black)};
}

// ---------- Ceiling lamps ----------
export function chandelier(scene, M, x, y, z) {
  const g = new THREE.Group(); g.position.set(x, y, z); scene.add(g);
  const shadeMat = new THREE.MeshStandardMaterial({color: 0xf6ead2, roughness: 0.5, emissive: 0xffc27a, emissiveIntensity: 1.2, side: THREE.DoubleSide, transparent: true, opacity: 0.92});
  const m = (geo, mat, px, py, pz, rx = 0, rz = 0) => { const o = new THREE.Mesh(geo, mat); o.position.set(px, py, pz); o.rotation.set(rx, 0, rz); g.add(o); return o; };
  m(cyl(0.06, 0.07, 0.03, 20), M.brass, 0, -0.015, 0);
  m(cyl(0.006, 0.006, 0.4, 8), M.brass, 0, -0.23, 0);
  m(new THREE.SphereGeometry(0.04, 16, 12), M.brass, 0, -0.44, 0);
  for (let i = 0; i < 3; i++) {
    const a = i / 3 * Math.PI * 2;
    const ax = Math.cos(a) * 0.22, az = Math.sin(a) * 0.22;
    const arm = new THREE.Mesh(tube([[0, -0.44, 0], [ax * 0.5, -0.47, az * 0.5], [ax, -0.4, az]], 0.007, 16, 6), M.brass); g.add(arm);
    const shade = m(lathe([[0.02, 0], [0.05, -0.02], [0.075, -0.08], [0.085, -0.13], [0.08, -0.15]], 24), shadeMat, ax, -0.35, az);
    shade.rotation.x = Math.PI; // open upwards (tulip)
    shade.position.y = -0.41;
  }
  return {group: g, mat: shadeMat};
}
export function domeLamp(scene, M, x, y, z, r = 0.16) {
  const mat = new THREE.MeshStandardMaterial({color: 0xf8f1e2, roughness: 0.4, emissive: 0xffd9a0, emissiveIntensity: 1.5});
  const g = new THREE.Group(); g.position.set(x, y, z); scene.add(g);
  const d = new THREE.Mesh(new THREE.SphereGeometry(r, 28, 12, 0, Math.PI * 2, Math.PI / 2, Math.PI / 2), mat);
  d.scale.y = 0.45; g.add(d);
  const rim = new THREE.Mesh(cyl(0.04, 0.04, 0.03, 16), M.whitePlastic); rim.position.y = -0.0; g.add(rim);
  return {group: g, mat};
}

// ---------- Floor lamp (торшер) ----------
export function floorLamp(B, scene, M, x, z) {
  B.add(cyl(0.16, 0.18, 0.025, 28), M.darkMetal, x, 0.0125, z);
  B.add(cyl(0.012, 0.012, 1.45, 10), M.brass, x, 0.75, z);
  const shadeMat = new THREE.MeshStandardMaterial({color: 0xd9a868, roughness: 1, side: THREE.DoubleSide, emissive: 0xff9a40, emissiveIntensity: 0.9, transparent: true, opacity: 0.97});
  const shade = new THREE.Mesh(lathe([[0.25, 0], [0.235, 0.05], [0.17, 0.32], [0.165, 0.34]], 32), shadeMat);
  shade.position.set(x, 1.28, z); scene.add(shade);
  // fringe ring
  const fr = new THREE.Mesh(cyl(0.252, 0.252, 0.035, 32, true), std(0xb07e44, 1, 0, {side: THREE.DoubleSide}));
  fr.position.set(x, 1.27, z); scene.add(fr);
  return {shade, mat: shadeMat, lightPos: new THREE.Vector3(x, 1.42, z)};
}

// ---------- Potted plant (procedural fallback geranium) ----------
export function geranium(B, M, x, y, z, s = 1) {
  srand(Math.floor(x * 1000 + z * 77));
  B.add(lathe([[0, 0], [0.06 * s, 0], [0.075 * s, 0.11 * s], [0.08 * s, 0.12 * s], [0.0, 0.12 * s]], 18), std(0xa0583a, 0.8), x, y, z);
  B.add(cyl(0.072 * s, 0.072 * s, 0.01, 16), M.soil, x, y + 0.11 * s, z);
  const leaf = std(0x3d6b2c, 0.7, 0, {side: THREE.DoubleSide});
  const flower = std(0xc8303a, 0.8);
  for (let i = 0; i < 14; i++) {
    const a = rnd() * Math.PI * 2, r = rnd() * 0.09 * s, h = 0.14 * s + rnd() * 0.14 * s;
    const g = new THREE.CircleGeometry(0.035 * s, 7);
    B.add(g, leaf, x + Math.cos(a) * r, y + h, z + Math.sin(a) * r, a, -1.1 + rnd() * 0.6, 0);
  }
  for (let i = 0; i < 4; i++) {
    const a = rnd() * Math.PI * 2, r = rnd() * 0.05 * s;
    B.add(new THREE.SphereGeometry(0.03 * s, 8, 6), flower, x + Math.cos(a) * r, y + 0.3 * s + rnd() * 0.05, z + Math.sin(a) * r);
  }
}

// Glass jar (банка)
export function jar(B, M, x, y, z, h = 0.17, content = null) {
  B.add(lathe([[0, 0], [0.05, 0], [0.052, 0.01], [0.052, h * 0.8], [0.04, h * 0.92], [0.04, h]], 20), M.glassThick, x, y, z, 0, 0, 0, {cast: false});
  if (content) B.add(cyl(0.048, 0.048, h * 0.7, 18), content, x, y + h * 0.36, z);
  B.add(cyl(0.043, 0.043, 0.015, 18), std(0x9ca2a6, 0.35, 0.8), x, y + h + 0.005, z);
}

// Crystal vase / glasses
export function crystalVase(B, M, x, y, z, s = 1) {
  B.add(lathe([[0, 0], [0.05 * s, 0], [0.055 * s, 0.02 * s], [0.03 * s, 0.06 * s], [0.045 * s, 0.16 * s], [0.07 * s, 0.25 * s], [0.068 * s, 0.26 * s]], 12), M.crystal, x, y, z, 0, 0, 0, {cast: false});
}
export function wineGlass(B, M, x, y, z) {
  B.add(lathe([[0, 0], [0.028, 0], [0.004, 0.006], [0.003, 0.06], [0.025, 0.08], [0.032, 0.12], [0.031, 0.121]], 12), M.crystal, x, y, z, 0, 0, 0, {cast: false});
}

// Cup with saucer
export function cup(B, M, x, y, z, mat, ry = 0) {
  const m = mat ?? std(0xf3efe6, 0.25);
  B.push(x, y, z, ry);
  B.add(lathe([[0, 0], [0.065, 0], [0.068, 0.006], [0.05, 0.012], [0, 0.012]], 20), m, 0, 0, 0);
  B.add(lathe([[0, 0.012], [0.03, 0.012], [0.04, 0.03], [0.045, 0.075], [0.043, 0.078], [0.04, 0.03], [0.0, 0.02]], 20), m, 0, 0, 0);
  B.add(new THREE.TorusGeometry(0.018, 0.005, 6, 12, Math.PI * 1.2), m, 0.05, 0.05, 0, 0, 0, -Math.PI * 0.6);
  B.pop();
}

export {T};
