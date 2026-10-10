import * as THREE from 'three';
import {lathe, tube} from '../world/geo.js';
import * as T from '../world/textures.js';

// "Гостья": looks like grandma at first glance — headscarf, dark coat — but too tall,
// stooped, with a long pale face and black hollows instead of eyes.
function faceTexture() {
  const [c, g] = T.canvas(256, 320);
  const sk = g.createRadialGradient(128, 150, 20, 128, 160, 170);
  sk.addColorStop(0, '#857c6e'); sk.addColorStop(0.55, '#5f574c'); sk.addColorStop(1, '#221e19');
  g.fillStyle = sk; g.fillRect(0, 0, 256, 320);
  g.filter = 'blur(6px)';
  // deep eye sockets
  for (const [x, y, rx, ry] of [[92, 132, 22, 27], [166, 128, 25, 30]]) {
    const e = g.createRadialGradient(x, y, 1, x, y, ry);
    e.addColorStop(0, '#000'); e.addColorStop(0.5, 'rgba(0,0,0,0.85)'); e.addColorStop(1, 'rgba(20,14,10,0)');
    g.fillStyle = e; g.beginPath(); g.ellipse(x, y, rx, ry, 0, 0, Math.PI * 2); g.fill();
  }
  // nose shadow and cheek hollows
  g.fillStyle = 'rgba(30,22,16,0.45)'; g.beginPath(); g.ellipse(128, 185, 10, 26, 0, 0, Math.PI * 2); g.fill();
  for (const x of [70, 186]) { g.fillStyle = 'rgba(30,22,16,0.35)'; g.beginPath(); g.ellipse(x, 210, 24, 40, 0, 0, Math.PI * 2); g.fill(); }
  // long open mouth
  g.fillStyle = 'rgba(5,3,2,0.9)'; g.beginPath(); g.ellipse(131, 268, 9, 40, 0.06, 0, Math.PI * 2); g.fill();
  g.filter = 'none';
  // wrinkles
  g.strokeStyle = 'rgba(40,30,22,0.35)'; g.lineWidth = 1.5;
  for (let i = 0; i < 6; i++) { g.beginPath(); g.moveTo(60, 70 + i * 7); g.quadraticCurveTo(128, 60 + i * 7, 196, 70 + i * 7); g.stroke(); }
  T.grain(g, 256, 320, 0.12, 9);
  return T.tex(c, {repeat: false});
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

// keep only triangles not in the face opening
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

export class Creature {
  constructor(scene) {
    const g = this.group = new THREE.Group();
    const wool = T.fabric('upholstery');
    const coat = new THREE.MeshStandardMaterial({color: 0x2a2420, map: wool.map, normalMap: wool.normalMap, roughness: 1});
    coat.map.repeat.set(3, 3);
    const scarfMat = new THREE.MeshStandardMaterial({map: scarfTexture(), roughness: 1, side: THREE.DoubleSide});
    scarfMat.map.repeat.set(2, 2);
    const skin = new THREE.MeshStandardMaterial({color: 0x9a907f, roughness: 0.75});
    const faceMat = new THREE.MeshStandardMaterial({map: faceTexture(), roughness: 0.7});
    // coat: lathe with folds, stooped (rotated forward at the top via a bent spine group)
    const prof = [[0, 0.0], [0.42, 0.02], [0.39, 0.25], [0.32, 0.7], [0.25, 1.1], [0.2, 1.36], [0.21, 1.48], [0.19, 1.57], [0.13, 1.65], [0.06, 1.69], [0.0, 1.7]];
    const cg = lathe(prof, 36);
    const cp = cg.attributes.position;
    for (let i = 0; i < cp.count; i++) {
      const x = cp.getX(i), y = cp.getY(i), z = cp.getZ(i);
      const a = Math.atan2(z, x), r = Math.hypot(x, z);
      const folds = 1 + Math.sin(a * 9 + y * 1.5) * 0.04 * (1 - y / 1.7) + Math.sin(a * 23) * 0.01;
      const nr = r * folds;
      cp.setXYZ(i, Math.cos(a) * nr, y, Math.sin(a) * nr * 0.78 + (y > 1.2 ? (y - 1.2) * 0.25 : 0));
    }
    cg.computeVertexNormals();
    const body = new THREE.Mesh(cg, coat); g.add(body);
    // collar
    const collar = new THREE.Mesh(new THREE.TorusGeometry(0.17, 0.05, 8, 20), coat); collar.rotation.x = Math.PI / 2 - 0.3; collar.position.set(0, 1.66, 0.12); g.add(collar);
    // neck + head (head tilted, too far forward)
    const head = this.head = new THREE.Group(); head.position.set(0, 1.84, 0.2); g.add(head);
    const neck = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.06, 0.2, 10), skin); neck.position.set(0, 1.74, 0.16); neck.rotation.x = 0.4; g.add(neck);
    const skull = new THREE.Mesh(new THREE.SphereGeometry(0.11, 20, 16), skin); skull.scale.set(0.92, 1.25, 1); head.add(skull);
    // face patch, long
    const face = new THREE.Mesh(new THREE.SphereGeometry(0.113, 20, 16, Math.PI / 2 - 0.95, 1.9, 0.45, 1.9), faceMat);
    face.scale.set(0.92, 1.3, 1.02); head.add(face);
    // headscarf with opening for the face
    const sg = cutOpening(new THREE.SphereGeometry(0.135, 28, 20, 0, Math.PI * 2, 0, Math.PI * 0.72), (x, y, z) => z > 0.045 && y < 0.07 && y > -0.12);
    const scarf = new THREE.Mesh(sg, scarfMat); scarf.scale.set(1, 1.28, 1.05); scarf.position.y = 0.01; head.add(scarf);
    // scarf tails knotted under the chin
    const knot = new THREE.Mesh(new THREE.SphereGeometry(0.035, 10, 8), scarfMat); knot.position.set(0, -0.16, 0.06); head.add(knot);
    for (const s of [-1, 1]) { const tail = new THREE.Mesh(tube([[0, -0.16, 0.06], [s * 0.04, -0.24, 0.07], [s * 0.05, -0.32, 0.04]], 0.018, 8, 6), scarfMat); head.add(tail); }
    // drape of scarf over shoulders/back
    const drape = new THREE.Mesh(lathe([[0.13, 0], [0.16, -0.08], [0.22, -0.2], [0.24, -0.24]], 24), scarfMat); drape.position.set(0, 1.78, 0.12); drape.rotation.x = 0.35; g.add(drape);
    this.glints = [];
    const glintMat = new THREE.MeshBasicMaterial({color: 0xd8d4c8});
    for (const s of [-1, 1]) { const e = new THREE.Mesh(new THREE.SphereGeometry(0.004, 6, 4), glintMat); e.position.set(s * 0.032, 0.025, 0.105); head.add(e); this.glints.push(e); }
    // arms: sleeves + long pale hands
    this.arms = [];
    for (const s of [-1, 1]) {
      const arm = new THREE.Group(); arm.position.set(s * 0.2, 1.52, 0.12); g.add(arm);
      const sl = lathe([[0.0, 0.02], [0.06, 0], [0.058, -0.3], [0.05, -0.62], [0.06, -0.68], [0.0, -0.68]], 12);
      arm.add(new THREE.Mesh(sl, coat));
      const hand = new THREE.Mesh(tube([[0, -0.66, 0.0], [0.0, -0.78, 0.01], [0.005, -0.88, 0.02]], 0.03, 10, 8), skin); arm.add(hand);
      for (let f = 0; f < 4; f++) {
        const fx = (f - 1.5) * 0.014;
        arm.add(new THREE.Mesh(tube([[fx, -0.87, 0.02], [fx * 1.4, -0.98, 0.03 + f * 0.003], [fx * 1.6, -1.1 - f * 0.012, 0.015], [fx * 1.5, -1.16 - f * 0.015, -0.01]], 0.0075, 10, 5), skin));
      }
      arm.add(new THREE.Mesh(tube([[s * -0.025, -0.82, 0.03], [s * -0.04, -0.9, 0.05], [s * -0.035, -0.97, 0.04]], 0.008, 6, 5), skin));
      arm.rotation.z = s * 0.1;
      this.arms.push(arm);
    }
    // feet: old felt boots barely visible
    for (const s of [-1, 1]) { const b = new THREE.Mesh(new THREE.CapsuleGeometry(0.06, 0.12, 4, 8), new THREE.MeshStandardMaterial({color: 0x1a1612, roughness: 1})); b.rotation.x = Math.PI / 2; b.position.set(s * 0.1, 0.05, 0.08); g.add(b); }
    g.traverse(o => { if (o.isMesh) { o.castShadow = true; o.receiveShadow = true; } });
    g.visible = false;
    scene.add(g);
    this.t = 0; this.walking = false; this.speed = 1.0; this.path = []; this.lookAt = null;
    g.scale.setScalar(1.12);
  }
  show(x, y, z, ry = 0) { this.baseY = y; this.group.position.set(x, y, z); this.group.rotation.set(0, ry, 0); this.head.rotation.set(0, 0, 0); this.group.visible = true; }
  hide() { this.group.visible = false; this.walking = false; this.path = []; }
  get visible() { return this.group.visible; }
  get pos() { return this.group.position; }
  faceTo(p, k = 1) {
    const d = new THREE.Vector3().subVectors(p, this.group.position);
    const target = Math.atan2(d.x, d.z);
    let a = target - this.group.rotation.y; a = Math.atan2(Math.sin(a), Math.cos(a));
    this.group.rotation.y += a * k;
  }
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
        g.position.addScaledVector(d, Math.min(dist, this.speed * dt));
        this.faceTo(target, Math.min(1, dt * 4));
        this.stepAcc = (this.stepAcc ?? 0) + this.speed * dt;
        if (this.stepAcc > 0.7) { this.stepAcc = 0; onStep?.(g.position); }
      }
    }
    const w = this.walking ? 1 : 0.25;
    // jerky, unnatural idle: head ticks in small steps
    const tick = Math.floor(this.t * 3) / 3;
    this.head.rotation.z = Math.sin(tick * 0.9) * 0.15 + 0.22;
    this.head.rotation.x = 0.38 + Math.sin(this.t * 0.4) * 0.04;
    g.children[0].rotation.z = Math.sin(this.t * 3.1) * 0.02 * w;
    g.position.y = (this.baseY ?? 0) + Math.abs(Math.sin(this.t * 2.2)) * 0.02 * w;
    this.arms.forEach((a, i) => { a.rotation.x = Math.sin(this.t * 2.2 + i * Math.PI) * 0.15 * w; });
    if (this.lookAt) {
      const hp = new THREE.Vector3(); this.head.getWorldPosition(hp);
      const d = new THREE.Vector3().subVectors(this.lookAt, hp);
      const local = Math.atan2(d.x, d.z) - g.rotation.y;
      const want = Math.max(-2.6, Math.min(2.6, Math.atan2(Math.sin(local), Math.cos(local))));
      this.head.rotation.y += (want - this.head.rotation.y) * Math.min(1, dt * 2);
    }
    for (const e of this.glints) e.visible = Math.random() > 0.03;
  }
}
