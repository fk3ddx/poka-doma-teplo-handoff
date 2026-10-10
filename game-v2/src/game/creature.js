import * as THREE from 'three';
import {lathe, tube} from '../world/geo.js';
import * as T from '../world/textures.js';

// "Гостья". It wears grandma's face — the skin of a memory stretched over a skull that is
// too long. Emaciated, 2.3 m when it stands up, grey mottled skin, torn nightgown, grandma's
// headscarf. Jointed arms with long fingers, a jaw that drops open far too wide.

const rand = (s => () => (s = (s * 16807) % 2147483647) / 2147483647)(12345);

function skinTexture() {
  const [c, g] = T.canvas(512);
  g.fillStyle = '#7d7a70'; g.fillRect(0, 0, 512, 512);
  for (let i = 0; i < 300; i++) { const r = 4 + rand() * 30; g.fillStyle = `rgba(${60 + rand() * 40 | 0},${55 + rand() * 30 | 0},${50 + rand() * 40 | 0},${0.15 + rand() * 0.2})`; g.beginPath(); g.ellipse(rand() * 512, rand() * 512, r, r * (0.5 + rand()), rand() * 3, 0, Math.PI * 2); g.fill(); }
  // veins
  g.lineWidth = 1.4;
  for (let i = 0; i < 70; i++) {
    g.strokeStyle = `rgba(${50 + rand() * 30 | 0},${40 + rand() * 30 | 0},${80 + rand() * 50 | 0},${0.35 + rand() * 0.3})`;
    let x = rand() * 512, y = rand() * 512; g.beginPath(); g.moveTo(x, y);
    for (let k = 0; k < 8; k++) { x += (rand() - 0.5) * 40; y += rand() * 30; g.lineTo(x, y); }
    g.stroke();
  }
  // bruised patches
  for (let i = 0; i < 12; i++) { const gr = g.createRadialGradient(0, 0, 0, 0, 0, 40); gr.addColorStop(0, 'rgba(70,30,40,0.4)'); gr.addColorStop(1, 'rgba(70,30,40,0)'); g.save(); g.translate(rand() * 512, rand() * 512); g.fillStyle = gr; g.fillRect(-40, -40, 80, 80); g.restore(); }
  T.grain(g, 512, 512, 0.12, 7);
  const t = T.tex(c); return t;
}
function gownTexture() {
  const [c, g] = T.canvas(512);
  g.fillStyle = '#b8b2a2'; g.fillRect(0, 0, 512, 512);
  for (let i = 0; i < 512; i += 3) { g.fillStyle = 'rgba(0,0,0,0.04)'; g.fillRect(i, 0, 1, 512); }
  // grime toward the hem, dark stains
  const gr = g.createLinearGradient(0, 0, 0, 512); gr.addColorStop(0, 'rgba(60,50,40,0)'); gr.addColorStop(1, 'rgba(50,40,30,0.7)');
  g.fillStyle = gr; g.fillRect(0, 0, 512, 512);
  for (let i = 0; i < 25; i++) { const x = rand() * 512, y = rand() * 512, r = 10 + rand() * 50; const s = g.createRadialGradient(x, y, 0, x, y, r); s.addColorStop(0, `rgba(${70 + rand() * 40 | 0},20,15,0.65)`); s.addColorStop(1, 'rgba(60,20,15,0)'); g.fillStyle = s; g.fillRect(x - r, y - r, r * 2, r * 2); }
  T.grain(g, 512, 512, 0.1, 5);
  return T.tex(c);
}
function scarfTexture() {
  const [c, g] = T.canvas(256);
  g.fillStyle = '#2a1e22'; g.fillRect(0, 0, 256, 256);
  for (let y = 0; y < 256; y += 42) for (let x = 0; x < 256; x += 42) {
    const ox = (y / 42) % 2 ? 21 : 0;
    g.fillStyle = 'rgba(150,40,40,0.75)';
    for (let i = 0; i < 5; i++) { const a = i / 5 * Math.PI * 2; g.beginPath(); g.ellipse(x + ox + Math.cos(a) * 6, y + Math.sin(a) * 6, 6, 4, a, 0, Math.PI * 2); g.fill(); }
    g.fillStyle = 'rgba(200,160,80,0.7)'; g.beginPath(); g.arc(x + ox, y, 2.5, 0, Math.PI * 2); g.fill();
    g.fillStyle = 'rgba(70,90,50,0.7)'; g.beginPath(); g.ellipse(x + ox + 12, y + 8, 3, 7, 0.8, 0, Math.PI * 2); g.fill();
  }
  T.grain(g, 256, 256, 0.08, 4);
  return T.tex(c);
}
// face: 'monster' = grandma's skin stretched wrong; 'grandma' = the kind face it pretends with
function faceTexture(kind) {
  const [c, g] = T.canvas(256, 320);
  const grandma = kind === 'grandma';
  const sk = g.createRadialGradient(128, 150, 20, 128, 160, 175);
  if (grandma) { sk.addColorStop(0, '#d8b49a'); sk.addColorStop(0.6, '#c09a80'); sk.addColorStop(1, '#6a5040'); }
  else { sk.addColorStop(0, '#a49a8a'); sk.addColorStop(0.55, '#7a7266'); sk.addColorStop(1, '#2a2620'); }
  g.fillStyle = sk; g.fillRect(0, 0, 256, 320);
  // wrinkles on the forehead and cheeks
  g.strokeStyle = grandma ? 'rgba(110,70,50,0.35)' : 'rgba(40,30,22,0.45)'; g.lineWidth = 1.5;
  for (let i = 0; i < 5; i++) { g.beginPath(); g.moveTo(64, 56 + i * 8); g.quadraticCurveTo(128, 48 + i * 8, 192, 56 + i * 8); g.stroke(); }
  for (const s of [-1, 1]) for (let i = 0; i < 3; i++) { g.beginPath(); g.moveTo(128 + s * (62 + i * 6), 150); g.quadraticCurveTo(128 + s * (70 + i * 7), 190, 128 + s * (56 + i * 5), 230); g.stroke(); }
  if (grandma) {
    // kind eyes, brows, smile
    for (const s of [-1, 1]) {
      const x = 128 + s * 38;
      g.fillStyle = 'rgba(90,60,45,0.5)'; g.beginPath(); g.ellipse(x, 124, 22, 9, 0, 0, Math.PI * 2); g.fill();
      g.fillStyle = '#e8e0d4'; g.beginPath(); g.ellipse(x, 122, 14, 6, 0, 0, Math.PI * 2); g.fill();
      g.fillStyle = '#4a5a6a'; g.beginPath(); g.arc(x, 122, 5, 0, Math.PI * 2); g.fill();
      g.fillStyle = '#111'; g.beginPath(); g.arc(x, 122, 2.2, 0, Math.PI * 2); g.fill();
      g.strokeStyle = 'rgba(200,190,180,0.8)'; g.lineWidth = 4; g.beginPath(); g.moveTo(x - 18, 104); g.quadraticCurveTo(x, 98, x + 18, 106); g.stroke();
    }
    g.fillStyle = 'rgba(120,70,60,0.35)'; g.beginPath(); g.ellipse(128, 175, 10, 22, 0, 0, Math.PI * 2); g.fill();
    g.strokeStyle = '#8a4a40'; g.lineWidth = 4; g.beginPath(); g.moveTo(100, 236); g.quadraticCurveTo(128, 250, 156, 236); g.stroke();
  } else {
    g.filter = 'blur(5px)';
    // misaligned, too-deep sockets: the skin doesn't fit the skull
    for (const [x, y, rx, ry] of [[90, 136, 22, 28], [168, 124, 26, 33]]) {
      const e = g.createRadialGradient(x, y, 1, x, y, ry);
      e.addColorStop(0, '#000'); e.addColorStop(0.55, 'rgba(0,0,0,0.9)'); e.addColorStop(1, 'rgba(20,14,10,0)');
      g.fillStyle = e; g.beginPath(); g.ellipse(x, y, rx, ry, 0, 0, Math.PI * 2); g.fill();
    }
    g.fillStyle = 'rgba(30,22,16,0.5)'; g.beginPath(); g.ellipse(128, 186, 9, 26, 0, 0, Math.PI * 2); g.fill();
    for (const x of [66, 190]) { g.fillStyle = 'rgba(30,22,16,0.4)'; g.beginPath(); g.ellipse(x, 214, 24, 44, 0, 0, Math.PI * 2); g.fill(); }
    // blood weeping from the sockets
    g.fillStyle = 'rgba(70,5,5,0.85)';
    g.fillRect(88, 150, 4, 70); g.fillRect(170, 146, 5, 90);
    g.filter = 'none';
    // stitched seams where the skin was sewn on
    g.strokeStyle = 'rgba(30,10,10,0.9)'; g.lineWidth = 2;
    g.beginPath(); g.moveTo(30, 40); g.quadraticCurveTo(128, 18, 226, 40); g.stroke();
    for (let x = 36; x < 222; x += 12) { const y = 40 - Math.sin((x - 30) / 196 * Math.PI) * 18; g.beginPath(); g.moveTo(x, y - 5); g.lineTo(x + 4, y + 5); g.stroke(); }
    for (const s of [-1, 1]) { g.beginPath(); g.moveTo(128 + s * 100, 60); g.lineTo(128 + s * 108, 280); g.stroke(); for (let y = 66; y < 280; y += 14) { g.beginPath(); g.moveTo(128 + s * 96, y); g.lineTo(128 + s * 112, y + 4); g.stroke(); } }
  }
  T.grain(g, 256, 320, grandma ? 0.06 : 0.12, 9);
  return T.tex(c, {repeat: false});
}

// torn edges: alpha map with ragged holes and a jagged lower hem
function tatterAlpha() {
  const [c, g] = T.canvas(256);
  g.fillStyle = '#fff'; g.fillRect(0, 0, 256, 256);
  g.fillStyle = '#000';
  for (let x = 0; x < 256; x += 4) { const h = 20 + Math.abs(Math.sin(x * 0.13) * Math.sin(x * 0.041)) * 120 + rand() * 30; g.fillRect(x, 0, 5, h * 0.5 + (x % 24 < 8 ? 40 : 0)); }
  for (let i = 0; i < 40; i++) { g.beginPath(); g.ellipse(rand() * 256, 100 + rand() * 156, 3 + rand() * 10, 4 + rand() * 18, rand() * 3, 0, Math.PI * 2); g.fill(); }
  const t = T.tex(c, {srgb: false}); t.wrapS = THREE.RepeatWrapping; t.flipY = true; return t;
}

function cutOpening(geo, fn) {
  const g = geo.toNonIndexed(); const p = g.attributes.position, uv = g.attributes.uv;
  const keepP = [], keepU = [];
  for (let i = 0; i < p.count; i += 3) {
    const cx = (p.getX(i) + p.getX(i + 1) + p.getX(i + 2)) / 3, cy = (p.getY(i) + p.getY(i + 1) + p.getY(i + 2)) / 3, cz = (p.getZ(i) + p.getZ(i + 1) + p.getZ(i + 2)) / 3;
    if (fn(cx, cy, cz)) continue;
    for (let k = 0; k < 3; k++) { keepP.push(p.getX(i + k), p.getY(i + k), p.getZ(i + k)); keepU.push(uv.getX(i + k), uv.getY(i + k)); }
  }
  const out = new THREE.BufferGeometry();
  out.setAttribute('position', new THREE.Float32BufferAttribute(keepP, 3));
  out.setAttribute('uv', new THREE.Float32BufferAttribute(keepU, 2));
  out.computeVertexNormals();
  return out;
}

function teeth(n, w, mat, down = 1, maxLen = 0.03) {
  const g = new THREE.Group();
  for (let i = 0; i < n; i++) {
    const a = (i / (n - 1) - 0.5) * Math.PI * 0.95;
    const len = maxLen * (0.5 + Math.abs(Math.sin(i * 2.3)) * 0.5);
    const t = new THREE.Mesh(new THREE.ConeGeometry(0.004, len, 5), mat);
    t.position.set(Math.sin(a) * w, -down * len / 2, Math.cos(a) * w * 0.8);
    t.rotation.x = down > 0 ? Math.PI : 0; t.rotation.z = (Math.random() - 0.5) * 0.4;
    g.add(t);
  }
  return g;
}

// jointed limb segment helper: a tapered capsule from 0 to -len along y
function seg(len, r0, r1, mat) {
  const g = new THREE.Mesh(lathe([[0, 0.02], [r0, 0], [r0 * 0.9, -len * 0.3], [r1 * 1.1, -len * 0.75], [r1, -len], [0, -len - 0.02]], 10), mat);
  return g;
}

export function buildHand(skin, nail, s, L = 1) {
  const hand = new THREE.Group();
  const palm = new THREE.Mesh(new THREE.SphereGeometry(0.045, 10, 8), skin); palm.scale.set(1, 1.5, 0.45); palm.position.y = -0.05; hand.add(palm);
  hand.fingers = [];
  for (let f = 0; f < 4; f++) {
    const finger = new THREE.Group(); finger.position.set((f - 1.5) * 0.02, -0.11, 0); finger.rotation.z = (f - 1.5) * 0.14 + (Math.sin(f * 7.1) * 0.06); hand.add(finger);
    let parent = finger; const lens = [0.07, 0.06, 0.05].map(l => l * L * (f === 1 || f === 2 ? 1.15 : 1));
    finger.joints = [];
    for (let k = 0; k < 3; k++) {
      const j = new THREE.Group(); if (k > 0) j.position.y = -lens[k - 1]; parent.add(j);
      j.add(seg(lens[k], 0.0085 - k * 0.0015, 0.007 - k * 0.0015, skin));
      finger.joints.push(j); parent = j;
    }
    const n = new THREE.Mesh(new THREE.ConeGeometry(0.005, 0.03 * L, 5), nail); n.position.y = -lens[2] - 0.012; n.rotation.x = Math.PI; parent.add(n);
    hand.fingers.push(finger);
  }
  const thumb = new THREE.Group(); thumb.position.set(s * 0.04, -0.06, 0.015); thumb.rotation.z = s * 0.7; hand.add(thumb);
  thumb.add(seg(0.07, 0.009, 0.007, skin));
  hand.thumb = thumb;
  hand.curl = k => { for (const f of hand.fingers) f.joints.forEach((j, i) => { j.rotation.x = k * (0.5 + i * 0.35); }); thumb.rotation.x = k * 0.6; };
  return hand;
}

export class Creature {
  constructor(scene) {
    const g = this.group = new THREE.Group();
    const skinTex = skinTexture(); skinTex.repeat.set(2, 3);
    // wet, grey-yellow, dead skin: low roughness so the flashlight glints on it
    const skin = this.skinMat = new THREE.MeshStandardMaterial({map: skinTex, roughness: 0.32, color: 0x9a9480});
    const nail = new THREE.MeshStandardMaterial({color: 0x1a120c, roughness: 0.25});
    const gownTex = gownTexture();
    const gown = new THREE.MeshStandardMaterial({map: gownTex, alphaMap: tatterAlpha(), alphaTest: 0.5, roughness: 1, side: THREE.DoubleSide, color: 0x8a8478});
    const scarfMat = new THREE.MeshStandardMaterial({map: scarfTexture(), alphaMap: tatterAlpha(), alphaTest: 0.5, roughness: 0.6, side: THREE.DoubleSide, color: 0x6a5a5a});
    scarfMat.map.repeat.set(1.5, 1.5);
    const toothMat = new THREE.MeshStandardMaterial({color: 0xb8ab88, roughness: 0.3});
    const gum = new THREE.MeshStandardMaterial({color: 0x240303, roughness: 0.25});
    this.faceMats = {monster: new THREE.MeshStandardMaterial({map: faceTexture('monster'), roughness: 0.35, color: 0x6a6254}), grandma: new THREE.MeshStandardMaterial({map: faceTexture('grandma'), roughness: 0.7})};

    // pelvis high (long legs), spine bent forward like a broken old woman that grew too tall
    const hips = this.hips = new THREE.Group(); hips.position.y = 1.12; g.add(hips);
    const chest = this.chest = new THREE.Group(); chest.rotation.x = 0.5; hips.add(chest);
    // emaciated ribcage: narrow, deep rib grooves, sunken belly
    const tg = lathe([[0, -0.05], [0.09, -0.04], [0.085, 0.12], [0.13, 0.28], [0.15, 0.42], [0.13, 0.55], [0.07, 0.62], [0.02, 0.64], [0, 0.64]], 28);
    const tp = tg.attributes.position;
    for (let i = 0; i < tp.count; i++) {
      const x = tp.getX(i), y = tp.getY(i), z = tp.getZ(i);
      const a = Math.atan2(z, x), r = Math.hypot(x, z);
      const front = Math.max(0, Math.sin(a)), back = Math.max(0, -Math.sin(a));
      let k = 1;
      if (y > 0.22 && y < 0.55) k -= Math.max(0, Math.sin(y * 70)) * 0.12 * (0.4 + front);   // ribs
      if (y < 0.2) k -= front * 0.35;                                                       // starved belly
      k -= back * Math.max(0, Math.sin(y * 90)) * 0.05;                                     // vertebrae
      tp.setXYZ(i, x * k, y, z * k * 0.62);
    }
    tg.computeVertexNormals();
    chest.add(new THREE.Mesh(tg, skin));
    // protruding spine and shoulder blades
    for (let i = 0; i < 12; i++) { const v = new THREE.Mesh(new THREE.SphereGeometry(0.018, 6, 5), skin); v.position.set(0, 0.02 + i * 0.05, -0.085 - Math.sin(i / 11 * Math.PI) * 0.015); v.scale.set(1, 0.7, 1); chest.add(v); }
    for (const sx of [-1, 1]) { const sb = new THREE.Mesh(new THREE.SphereGeometry(0.06, 8, 6), skin); sb.scale.set(1, 1.3, 0.35); sb.position.set(sx * 0.07, 0.48, -0.07); sb.rotation.z = sx * 0.4; chest.add(sb); }
    // collarbones
    for (const sx of [-1, 1]) { const cb = seg(0.16, 0.012, 0.01, skin); cb.rotation.z = sx * (Math.PI / 2 + 0.15); cb.position.set(0, 0.6, 0.05); chest.add(cb); }
    // torn nightgown: only from the waist down, in hanging tatters
    const gg = new THREE.CylinderGeometry(0.17, 0.3, 0.95, 32, 6, true);
    const gp = gg.attributes.position;
    for (let i = 0; i < gp.count; i++) { const x = gp.getX(i), y = gp.getY(i), z = gp.getZ(i); const a = Math.atan2(z, x); const f = 1 + Math.sin(a * 11) * 0.05 * (0.5 - y); gp.setXYZ(i, x * f, y, z * f * 0.85); }
    gg.computeVertexNormals();
    const gownMesh = new THREE.Mesh(gg, gown); gownMesh.position.y = -0.38; hips.add(gownMesh);
    // long neck jutting forward, small skull hanging low
    const neck = this.neck = new THREE.Group(); neck.position.set(0, 0.62, 0.03); neck.rotation.x = 0.9; chest.add(neck);
    const nk = seg(0.3, 0.035, 0.028, skin); nk.rotation.x = Math.PI; neck.add(nk);
    for (let i = 0; i < 4; i++) { const ten = seg(0.28, 0.006, 0.004, skin); ten.rotation.x = Math.PI; ten.position.set((i - 1.5) * 0.018, 0, 0.02); neck.add(ten); }
    const head = this.head = new THREE.Group(); head.position.set(0, 0.3, 0); neck.add(head);
    head.rotation.x = -0.75;
    const skull = new THREE.Mesh(new THREE.SphereGeometry(0.07, 22, 18), skin); skull.scale.set(0.85, 1.45, 1.05); skull.position.y = 0.05; head.add(skull);
    // grandma's face: a loose mask of skin, too big for the skull, sagging
    const fg = new THREE.SphereGeometry(0.092, 40, 32, Math.PI / 2 - 0.95, 1.9, 0.3, 1.5);
    { // carve the face: sunken, misaligned sockets, hollow cheeks, a nose ridge, a slack mouth line
      const p = fg.attributes.position, uv = fg.attributes.uv;
      const dent = (u, v, cu, cv, ru, rv, d) => { const x = (u - cu) / ru, y = (v - cv) / rv; return d * Math.exp(-(x * x + y * y) * 2.2); };
      for (let i = 0; i < p.count; i++) {
        const u = uv.getX(i), v = uv.getY(i);
        let k = 1;
        k -= dent(u, v, 0.33, 0.6, 0.11, 0.1, 0.32) + dent(u, v, 0.68, 0.64, 0.12, 0.11, 0.36);   // sockets
        k -= dent(u, v, 0.22, 0.38, 0.1, 0.16, 0.14) + dent(u, v, 0.78, 0.38, 0.1, 0.16, 0.14);   // cheeks
        k += dent(u, v, 0.5, 0.5, 0.05, 0.13, 0.12);                                              // nose
        k -= dent(u, v, 0.5, 0.2, 0.2, 0.04, 0.08);                                               // mouth seam
        p.setXYZ(i, p.getX(i) * k, p.getY(i) * k, p.getZ(i) * k);
      }
      fg.computeVertexNormals();
    }
    this.face = new THREE.Mesh(fg, this.faceMats.monster);
    this.face.scale.set(0.82, 1.62, 1.05); this.face.position.set(0, 0.035, 0.004); head.add(this.face);
    const voidMat = new THREE.MeshBasicMaterial({color: 0x000000});
    for (const [x, y] of [[-0.026, 0.083], [0.028, 0.093]]) { const v = new THREE.Mesh(new THREE.SphereGeometry(0.02, 10, 8), voidMat); v.scale.set(1, 1.2, 0.6); v.position.set(x, y, 0.058); head.add(v); }
    this.glints = [];
    for (const [x, y] of [[-0.026, 0.083], [0.028, 0.093]]) { const e = new THREE.Mesh(new THREE.SphereGeometry(0.0025, 6, 4), new THREE.MeshBasicMaterial({color: 0xd8d4c8})); e.position.set(x, y, 0.07); head.add(e); this.glints.push(e); }
    // mouth: teeth and gullet, only visible when the jaw drops
    const mouth = this.mouth = new THREE.Group(); mouth.position.set(0, -0.055, 0.03); head.add(mouth);
    const cavity = new THREE.Mesh(new THREE.SphereGeometry(0.06, 14, 10), gum); cavity.scale.set(0.8, 1.2, 0.7); cavity.position.set(0, -0.04, 0); mouth.add(cavity);
    const upper = teeth(18, 0.045, toothMat, 1, 0.045); upper.position.set(0, 0, 0.035); mouth.add(upper);
    // jaw hinged at the ears, unhinges far down
    const jaw = this.jaw = new THREE.Group(); jaw.position.set(0, -0.04, -0.02); head.add(jaw);
    const jawMesh = new THREE.Mesh(new THREE.SphereGeometry(0.07, 18, 12, 0, Math.PI * 2, Math.PI / 2, Math.PI / 2), this.faceMats.monster);
    jawMesh.scale.set(0.9, 1.3, 1.15); jawMesh.position.set(0, -0.02, 0.04); jaw.add(jawMesh);
    const lower = teeth(17, 0.043, toothMat, -1, 0.05); lower.position.set(0, -0.02, 0.07); jaw.add(lower);
    const tongue = new THREE.Mesh(new THREE.SphereGeometry(0.035, 10, 8), new THREE.MeshStandardMaterial({color: 0x3a0a0a, roughness: 0.2})); tongue.scale.set(0.8, 0.35, 2.2); tongue.position.set(0, -0.035, 0.07); jaw.add(tongue);
    // remnants of grandma's headscarf: wet, torn, stuck to the back of the skull
    const sg = cutOpening(new THREE.SphereGeometry(0.1, 24, 18, 0, Math.PI * 2, 0, Math.PI * 0.55), (x, y, z) => z > -0.02);
    const scarf = new THREE.Mesh(sg, scarfMat); scarf.scale.set(1, 1.45, 1.1); scarf.position.set(0, 0.05, -0.008); head.add(scarf);
    const tail = new THREE.Mesh(new THREE.PlaneGeometry(0.08, 0.35, 2, 6), scarfMat); tail.position.set(0.03, -0.12, -0.08); tail.rotation.set(0.3, 0.2, 0.1); head.add(tail);
    // arms: far too long, knobby joints, fingertips below the knees
    this.arms = [];
    for (const s of [-1, 1]) {
      const sh = new THREE.Group(); sh.position.set(s * 0.14, 0.56, 0.0); chest.add(sh);
      sh.add(seg(0.56, 0.03, 0.02, skin));
      const elbowKnob = new THREE.Mesh(new THREE.SphereGeometry(0.026, 8, 6), skin); elbowKnob.position.y = -0.56; sh.add(elbowKnob);
      const el = new THREE.Group(); el.position.y = -0.56; sh.add(el);
      el.add(seg(0.6, 0.022, 0.015, skin));
      const wr = new THREE.Group(); wr.position.y = -0.6; el.add(wr);
      const hand = buildHand(skin, nail, s, 1.9); wr.add(hand);
      this.arms.push({sh, el, wr, hand, s});
    }
    // legs: thin, backward-bent knees, long-toed feet
    this.legs = [];
    for (const s of [-1, 1]) {
      const hip = new THREE.Group(); hip.position.set(s * 0.08, 0, 0); hips.add(hip);
      hip.add(seg(0.58, 0.045, 0.025, skin));
      const kn = new THREE.Group(); kn.position.y = -0.58; hip.add(kn);
      kn.add(new THREE.Mesh(new THREE.SphereGeometry(0.03, 8, 6), skin));
      kn.add(seg(0.56, 0.024, 0.016, skin));
      const foot = new THREE.Mesh(new THREE.SphereGeometry(0.032, 10, 8), skin); foot.scale.set(0.9, 0.45, 2.6); foot.position.set(0, -0.56, 0.06); kn.add(foot);
      for (let t = 0; t < 4; t++) { const toe = seg(0.07, 0.008, 0.005, skin); toe.rotation.x = -Math.PI / 2 - 0.4; toe.position.set((t - 1.5) * 0.015, -0.565, 0.13); kn.add(toe); }
      this.legs.push({hip, kn, s});
    }
    // disguise: grandma's housecoat and full headscarf over the body
    const cover = this.cover = new THREE.Group(); g.add(cover);
    const coatTex = scarfTexture(); coatTex.repeat.set(3, 4);
    const coatMat = new THREE.MeshStandardMaterial({map: coatTex, color: 0xb89a9a, roughness: 1});
    const cg = lathe([[0, 0.02], [0.3, 0.03], [0.28, 0.5], [0.25, 1.0], [0.27, 1.3], [0.24, 1.55], [0.12, 1.66], [0.05, 1.7], [0, 1.71]], 24); cg.scale(1, 1, 0.8);
    cover.add(new THREE.Mesh(cg, coatMat));
    for (const sx of [-1, 1]) { const sl = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.07, 0.6, 10), coatMat); sl.position.set(sx * 0.27, 1.25, 0.02); sl.rotation.z = sx * 0.08; cover.add(sl); const hd = new THREE.Mesh(new THREE.SphereGeometry(0.045, 8, 6), skin); hd.position.set(sx * 0.29, 0.92, 0.04); cover.add(hd); }
    const hood = new THREE.Mesh(cutOpening(new THREE.SphereGeometry(0.15, 22, 16, 0, Math.PI * 2, 0, Math.PI * 0.75), (x, y, z) => z > 0.05 && y < 0.09 && y > -0.12), new THREE.MeshStandardMaterial({map: scarfTexture(), color: 0x8a7070, roughness: 1, side: THREE.DoubleSide}));
    hood.scale.set(1.05, 1.25, 1.1); cover.add(hood); this.hood = hood;
    cover.visible = false;
    g.traverse(o => { if (o.isMesh) { o.castShadow = true; o.receiveShadow = true; } });
    g.visible = false;
    scene.add(g);
    this.t = 0; this.walking = false; this.speed = 1.0; this.path = []; this.lookAt = null;
    this.pose = 'idle'; this.jawOpen = 0; this.jawTarget = 0; this.reach = 0; this.reachTarget = 0;
    this.disguised = false; this.twist = 0; this.freeze = 0;
    this.baseScale = 1.0;
    g.scale.setScalar(this.baseScale);
  }
  show(x, y, z, ry = 0) { this.baseY = y; this.group.position.set(x, y, z); this.group.rotation.set(0, ry, 0); this.head.rotation.y = 0; this.group.visible = true; }
  hide() { this.group.visible = false; this.walking = false; this.path = []; this.jawOpen = this.jawTarget = 0; this.reach = this.reachTarget = 0; this.setDisguise(false); }
  get visible() { return this.group.visible; }
  get pos() { return this.group.position; }
  setDisguise(on) {
    this.disguised = on;
    this.face.material = on ? this.faceMats.grandma : this.faceMats.monster;
    this.jaw.children[0].material = this.face.material;
    this.group.scale.setScalar(on ? 0.82 : this.baseScale);
    this.cover.visible = on;
    this.hips.children.forEach(c => { if (c !== this.chest) c.visible = !on; });
    this.chest.children.forEach(c => { if (c !== this.neck) c.visible = !on; });
    for (const a of this.arms) a.sh.visible = !on;
  }
  faceTo(p, k = 1) {
    const d = new THREE.Vector3().subVectors(p, this.group.position);
    const target = Math.atan2(d.x, d.z);
    let a = target - this.group.rotation.y; a = Math.atan2(Math.sin(a), Math.cos(a));
    this.group.rotation.y += a * k;
  }
  headWorld() { const v = new THREE.Vector3(); this.head.getWorldPosition(v); return v; }
  walkPath(pts, speed = 1.0) { this.path = pts.map(p => p.clone()); this.speed = speed; this.walking = true; }
  update(dt, onStep) {
    this.t += dt;
    const g = this.group;
    if (!g.visible) return;
    if (this.walking && this.path.length) {
      const target = this.path[0];
      const d = new THREE.Vector3(target.x - g.position.x, 0, target.z - g.position.z);
      const dist = d.length();
      if (dist < 0.08) { this.path.shift(); if (!this.path.length) this.walking = false; }
      else {
        d.normalize();
        // lurching gait: speed pulses with each step
        const surge = 0.55 + 0.9 * Math.max(0, Math.sin(this.t * 5.2));
        g.position.addScaledVector(d, Math.min(dist, this.speed * surge * dt));
        this.faceTo(target, Math.min(1, dt * 4));
        this.stepAcc = (this.stepAcc ?? 0) + this.speed * dt;
        if (this.stepAcc > 0.65) { this.stepAcc = 0; onStep?.(g.position); }
      }
    }
    const w = this.walking ? 1 : 0;
    const t = this.t;
    // jerky: pose updates in steps, with rare freezes
    const tick = Math.floor(t * 9) / 9;
    if (!this.disguised) {
      this.chest.rotation.x = 0.5 + Math.sin(tick * 1.3) * 0.05 + w * 0.15;
      this.chest.rotation.z = Math.sin(tick * 2.1) * 0.06 * (1 + w);
      this.head.rotation.z = Math.sin(Math.floor(t * 3) * 1.7) * 0.25;
      this.head.rotation.x = -0.75 + Math.sin(tick * 0.8) * 0.1;
    } else {
      this.chest.rotation.x = 0.12; this.chest.rotation.z = 0; this.head.rotation.z = 0.05; this.head.rotation.x = -0.75;
    }
    g.position.y = (this.baseY ?? 0) + Math.abs(Math.sin(t * 5.2)) * 0.03 * w;
    // legs
    for (const L of this.legs) { const ph = t * 5.2 + (L.s > 0 ? Math.PI : 0); L.hip.rotation.x = Math.sin(ph) * 0.35 * w - 0.05; L.kn.rotation.x = Math.max(0, -Math.sin(ph)) * 0.6 * w + 0.12; }
    // arms: hanging and twitching, or reaching forward
    this.reach += (this.reachTarget - this.reach) * Math.min(1, dt * 6);
    for (const A of this.arms) {
      const sway = Math.sin(t * 2.6 + A.s) * 0.12 * (0.3 + w);
      A.sh.rotation.x = sway * (1 - this.reach) - this.reach * 1.9 - 0.45;
      A.sh.rotation.z = A.s * (0.18 + this.reach * 0.1) + (A.s > 0 ? 0.08 : 0);
      A.el.rotation.x = -(A.s > 0 ? 0.9 : 0.5) * (1 - this.reach * 0.6) + Math.sin(Math.floor(t * 4 + A.s * 3)) * 0.06;
      A.wr.rotation.x = 0.5 * (1 - this.reach); A.wr.rotation.z = -A.s * 0.3;
      A.hand.curl(0.25 + this.reach * 0.6 + Math.sin(t * 7 + A.s) * 0.08);
    }
    // jaw drops open and unhinges
    this.jawOpen += (this.jawTarget - this.jawOpen) * Math.min(1, dt * 10);
    this.jaw.rotation.x = this.jawOpen * 1.25;
    this.jaw.position.y = -0.04 - this.jawOpen * 0.09;
    this.mouth.visible = this.jawOpen > 0.05;
    // the neck twists in sudden ticks when it decides to look behind
    this.head.rotation.y += (this.twist - this.head.rotation.y) * (Math.floor(t * 8) !== this._tw ? 0.5 : 0); this._tw = Math.floor(t * 8);
    if (this.lookAt) {
      const hp = this.headWorld();
      const d = new THREE.Vector3().subVectors(this.lookAt, hp);
      const local = Math.atan2(d.x, d.z) - g.rotation.y;
      const want = Math.max(-2.8, Math.min(2.8, Math.atan2(Math.sin(local), Math.cos(local))));
      // snaps toward the target instead of turning smoothly
      if (Math.floor(t * 6) !== this._snap) { this._snap = Math.floor(t * 6); this.head.rotation.y += (want - this.head.rotation.y) * 0.6; }
    }
    if (this.glints) for (const e of this.glints) e.visible = !this.disguised && Math.random() > 0.03;
    if (this.disguised) { this.head.getWorldPosition(this._hp ??= new THREE.Vector3()); this.cover.worldToLocal(this._hp); this.hood.position.copy(this._hp).add(new THREE.Vector3(0, 0.02, -0.01)); const k = Math.max(0.6, (this._hp.y - 0.06) / 1.71); this.cover.children[0].scale.set(1, k, 0.8); this.cover.children[0].position.z = this._hp.z * 0.6; }
  }
}
