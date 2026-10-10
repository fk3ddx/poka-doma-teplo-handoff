import * as THREE from 'three';
import * as T from './textures.js';

// P.T.-style degradation of the flat between loops:
// 0 warm → 1 cold → 2 rot → 3+ nightmare. Decals live on walls and are toggled per phase.

function decalTex(kind, seed) {
  const [c, g] = T.canvas(256);
  let s = seed;
  const R = () => { s = (s * 16807) % 2147483647; return s / 2147483647; };
  g.clearRect(0, 0, 256, 256);
  if (kind === 'frost') {
    for (let i = 0; i < 700; i++) {
      const x = R() * 256, y = R() * 256, edge = Math.min(x, y, 256 - x, 256 - y);
      if (R() * 90 < edge) continue;
      g.strokeStyle = `rgba(230,240,255,${0.15 + R() * 0.3})`; g.lineWidth = 0.8;
      g.beginPath(); g.moveTo(x, y); g.lineTo(x + (R() - 0.5) * 20, y + (R() - 0.5) * 20); g.stroke();
    }
  } else if (kind === 'mold') {
    for (let i = 0; i < 26; i++) {
      const x = 128 + (R() - 0.5) * 180, y = 128 + (R() - 0.5) * 180, r = 8 + R() * 40;
      const gr = g.createRadialGradient(x, y, 0, x, y, r);
      gr.addColorStop(0, `rgba(${30 + R() * 30 | 0},${40 + R() * 25 | 0},${20 + R() * 15 | 0},0.85)`); gr.addColorStop(0.7, 'rgba(40,45,25,0.4)'); gr.addColorStop(1, 'rgba(40,45,25,0)');
      g.fillStyle = gr; g.beginPath(); g.arc(x, y, r, 0, Math.PI * 2); g.fill();
    }
    // water stain ring and drips
    g.strokeStyle = 'rgba(90,70,40,0.5)'; g.lineWidth = 3; g.beginPath(); g.ellipse(128, 70, 100, 40, 0, 0, Math.PI * 2); g.stroke();
    for (let i = 0; i < 10; i++) { const x = 40 + R() * 176; g.fillStyle = 'rgba(70,55,30,0.45)'; g.fillRect(x, 70, 2 + R() * 3, 60 + R() * 120); }
  } else if (kind === 'peel') {
    // wallpaper strip curling off the wall
    g.fillStyle = 'rgba(60,45,30,0.75)'; g.fillRect(90, 0, 80, 256);
    g.fillStyle = 'rgba(120,100,70,0.6)'; for (let y = 0; y < 256; y += 5) g.fillRect(90 + R() * 6, y, 70 + R() * 10, 2);
    g.fillStyle = 'rgba(20,15,10,0.6)'; g.fillRect(168, 0, 6, 256);
  } else if (kind === 'blood') {
    for (let i = 0; i < 5; i++) {
      const x = 60 + R() * 140, y = 40 + R() * 80, r = 14 + R() * 30;
      g.fillStyle = `rgba(${70 + R() * 50 | 0},4,6,0.9)`; g.beginPath(); g.arc(x, y, r, 0, Math.PI * 2); g.fill();
      for (let k = 0; k < 6; k++) { const dx = x + (R() - 0.5) * r * 1.6; g.fillRect(dx, y, 2 + R() * 5, 40 + R() * 150); }
    }
    // hand smear
    g.fillStyle = 'rgba(90,6,8,0.8)';
    for (let f = 0; f < 4; f++) g.fillRect(150 + f * 14, 150 + Math.sin(f) * 6, 9, 90);
  } else if (kind === 'scrawl') {
    g.strokeStyle = 'rgba(80,5,5,0.9)'; g.lineWidth = 4; g.font = 'bold 54px PT Serif, serif';
    g.fillStyle = 'rgba(90,6,6,0.85)'; g.fillText('ПОМНИ', 20, 110); g.fillText('ПОМНИ', 40, 190);
  }
  return T.tex(c, {repeat: false});
}

export function buildPhases(ctx) {
  const {scene} = ctx;
  const groups = {1: new THREE.Group(), 2: new THREE.Group(), 3: new THREE.Group()};
  for (const g of Object.values(groups)) { g.visible = false; scene.add(g); }
  const decal = (group, kind, seed, x, y, z, ry, w, h) => {
    const m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), new THREE.MeshStandardMaterial({map: decalTex(kind, seed), transparent: true, depthWrite: false, roughness: kind === 'blood' ? 0.25 : 1, polygonOffset: true, polygonOffsetFactor: -2}));
    m.position.set(x, y, z); m.rotation.y = ry; m.renderOrder = 1; group.add(m); return m;
  };
  // cold: frost on every window pane
  for (const [x, w] of [[1.4, 1.3], [3.95, 1.4], [7.7, 1.2], [5.17, 0.7]]) decal(groups[1], 'frost', 7 + x * 10, x, 1.55, 0.01, 0, w, 1.4);
  // rot: mold in corners, peeling wallpaper, water stains (living, hall, bedroom, kitchen)
  const rot = [[2.92, 2.2, 0.7, Math.PI / 2, 1.2, 1.2], [6.48, 2.0, 3.6, -Math.PI / 2, 1.0, 1.0], [4.0, 1.9, 4.39, Math.PI, 1.4, 1.2], [6.62, 1.6, 3.6, Math.PI / 2, 0.8, 1.6],
    [8.78, 1.9, 4.0, -Math.PI / 2, 1.0, 1.4], [0.02, 1.9, 1.2, Math.PI / 2, 1.2, 1.3], [7.4, 2.1, 0.01, 0, 1.0, 0.8], [5.2, 1.6, 5.99, Math.PI, 0.8, 1.6]];
  rot.forEach(([x, y, z, ry, w, h], i) => decal(groups[2], i % 3 === 1 ? 'peel' : 'mold', 31 + i * 7, x, y, z, ry, w, h));
  const more = [[2.92, 0.9, 2.9, Math.PI / 2], [6.48, 1.2, 1.0, -Math.PI / 2], [3.2, 1.0, 4.39, Math.PI], [5.0, 2.0, 0.01, 0], [8.78, 1.0, 5.4, -Math.PI / 2], [7.0, 1.2, 5.99, Math.PI], [6.62, 0.8, 1.9, Math.PI / 2], [8.78, 2.0, 0.6, -Math.PI / 2], [0.02, 1.0, 3.6, Math.PI / 2], [1.4, 1.9, 4.39, Math.PI], [2.78, 1.8, 1.2, -Math.PI / 2], [4.69, 1.4, 5.2, -Math.PI / 2]];
  more.forEach(([x, y, z, ry], i) => decal(groups[2], i % 4 === 2 ? 'peel' : 'mold', 101 + i * 11, x, y, z, ry, 1.5 + (i % 3) * 0.4, 1.5));
  for (const [x, z] of [[4.6, 2.2], [7.7, 1.4], [1.4, 2.2], [7.7, 4.6], [5.6, 5.2]]) { const c = decal(groups[2], 'mold', 400 + x * 10, x, 2.495, z, 0, 2.2, 2.2); c.rotation.set(Math.PI / 2, 0, x); }
  // nightmare: blood, scrawls
  const nm = [[2.92, 1.3, 2.3, Math.PI / 2, 1.2, 1.2, 'blood'], [6.62, 1.4, 4.0, Math.PI / 2, 1.0, 1.4, 'scrawl'], [7.7, 1.2, 5.99, Math.PI, 1.2, 1.2, 'blood'], [8.78, 1.3, 1.0, -Math.PI / 2, 1.0, 1.0, 'blood'], [4.6, 1.5, 4.39, Math.PI, 1.6, 1.0, 'scrawl'], [0.02, 1.2, 3.0, Math.PI / 2, 1.2, 1.2, 'blood']];
  nm.forEach(([x, y, z, ry, w, h, k], i) => decal(groups[3], k, 77 + i * 13, x, y, z, ry, w, h));
  // a puddle under the bathroom door in nightmare
  const pud = new THREE.Mesh(new THREE.CircleGeometry(0.5, 20), new THREE.MeshStandardMaterial({color: 0x2a0204, roughness: 0.05, metalness: 0.3}));
  pud.rotation.x = -Math.PI / 2; pud.scale.y = 0.6; pud.position.set(4.95, 0.004, 5.25); groups[3].add(pud);

  return {
    groups,
    set(phase) {
      groups[1].visible = phase === 1;
      groups[2].visible = phase >= 2;
      groups[3].visible = phase >= 3;
    },
  };
}

// lighting mood per phase: colour multiplier and intensity factor for warm fixtures
export const PHASE_LIGHT = [
  {color: null, k: 1, env: 0.18, fog: 0x1b1c1f},
  {color: 0x9ab4ff, k: 0.6, env: 0.1, fog: 0x161c26, mix: 0.8, wall: 0xc8d0e0},
  {color: 0x8aa060, k: 0.45, env: 0.07, fog: 0x161a10, mix: 0.85, wall: 0x9a9a78},
  {color: 0xff3020, k: 0.5, env: 0.05, fog: 0x200808, mix: 0.85, wall: 0x8a5a50},
];
