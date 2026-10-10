import * as THREE from 'three';
import {boxGeo, rboxGeo, cyl, lathe, tube, rnd, srand, planeGeo} from '../geo.js';
import {std} from '../materials.js';
import * as T from '../textures.js';
import {radiator, curtains, books, frame, jar, cup, socket, lightSwitch, domeLamp, geranium, foldedPlane} from './common.js';

// Rotary phone (dynamic: handset can be lifted)
function rotaryPhone(ctx, x, y, z, ry) {
  const {scene, M} = ctx;
  const g = new THREE.Group(); g.position.set(x, y, z); g.rotation.y = ry; scene.add(g);
  const plastic = std(0x7a1e18, 0.3);
  const add = (geo, mat, px, py, pz, rx = 0, ryy = 0, rz = 0, parent = g) => { const m = new THREE.Mesh(geo, mat); m.position.set(px, py, pz); m.rotation.set(rx, ryy, rz); m.castShadow = true; parent.add(m); return m; };
  // body: tapered box
  const body = new THREE.CylinderGeometry(0.075, 0.11, 0.09, 4, 1); body.rotateY(Math.PI / 4); body.scale(1.15, 1, 1);
  add(body, plastic, 0, 0.045, 0);
  add(rboxGeo(0.21, 0.012, 0.2, 0.005), std(0x2a0e0b, 0.5), 0, 0.006, 0);
  // dial plate
  const dial = add(cyl(0.05, 0.05, 0.008, 28), std(0xe8e2d2, 0.3), 0, 0.075, 0.035, -0.55);
  const ring = add(new THREE.TorusGeometry(0.038, 0.006, 6, 24), std(0xd8d0c0, 0.25), 0, 0.08, 0.036, -0.55 + Math.PI / 2);
  for (let i = 0; i < 10; i++) { const a = -0.6 + i / 10 * Math.PI * 1.6; add(cyl(0.007, 0.007, 0.012, 8), std(0x1a1a1a, 0.6), Math.cos(a) * 0.036, 0.081 + Math.sin(a) * 0.036 * 0.52, 0.036 + Math.sin(a) * 0.036 * 0.85, -0.55); }
  // cradle prongs
  for (const s of [-1, 1]) add(rboxGeo(0.02, 0.03, 0.03, 0.008), plastic, s * 0.07, 0.1, -0.03);
  // handset group (rests on cradle)
  const hs = new THREE.Group(); hs.position.set(0, 0.115, -0.03); g.add(hs);
  add(tube([[-0.1, 0, 0], [-0.06, 0.02, 0], [0.06, 0.02, 0], [0.1, 0, 0]], 0.014, 16, 8), plastic, 0, 0, 0, 0, 0, 0, hs);
  for (const s of [-1, 1]) add(cyl(0.028, 0.024, 0.04, 16), plastic, s * 0.1, -0.012, 0, 0, 0, 0, hs);
  // coiled cord
  const pts = [];
  for (let i = 0; i <= 120; i++) { const t = i / 120; pts.push([-0.13 + Math.cos(t * 50) * 0.008 - t * 0.02, 0.05 - t * 0.04, -0.02 + Math.sin(t * 50) * 0.008 + t * 0.05]); }
  add(tube(pts, 0.0025, 240, 4), std(0x3a100c, 0.5), 0, 0, 0);
  return {group: g, handset: hs, rest: hs.position.clone()};
}

function hallway(ctx) {
  const {B, M, scene, inter} = ctx;
  // --- trellis table with mirror (трюмо) on west wall of hall A
  B.push(6.6 + 0.18, 0, 3.75, Math.PI / 2);
  const wood = M.walnut;
  B.add(rboxGeo(0.8, 0.03, 0.36, 0.006), wood, 0, 0.78, 0);
  B.add(rboxGeo(0.78, 0.12, 0.34, 0.006), wood, 0, 0.7, 0);
  B.add(rboxGeo(0.34, 0.09, 0.012, 0.004), wood, -0.2, 0.7, 0.175); // drawer front
  B.add(rboxGeo(0.06, 0.012, 0.02, 0.004), M.brass, -0.2, 0.7, 0.19);
  for (const s of [-1, 1]) B.add(rboxGeo(0.04, 0.64, 0.32, 0.006), wood, s * 0.37, 0.32, 0);
  B.add(rboxGeo(0.74, 0.02, 0.3, 0.004), wood, 0, 0.18, 0);
  // mirror
  B.add(rboxGeo(0.62, 0.92, 0.03, 0.01), wood, 0, 1.38, -0.16);
  const mirrorMat = std(0xb8c0c2, 0.02, 1, {envMapIntensity: 1});
  ctx.mirrorMats = [mirrorMat];
  B.add(new THREE.PlaneGeometry(0.54, 0.84), mirrorMat, 0, 1.38, -0.143);
  // phone book, pen, key dish
  B.add(rboxGeo(0.12, 0.02, 0.17, 0.003), std(0x2a3a5a, 0.7), 0.24, 0.805, 0.04, 0.2);
  B.add(cyl(0.004, 0.004, 0.13, 6), std(0x1a3aa0, 0.4), 0.2, 0.8, 0.12, 0, 0, Math.PI / 2);
  B.add(lathe([[0, 0], [0.05, 0], [0.06, 0.015], [0.058, 0.017], [0, 0.005]], 16), std(0x6a8aa0, 0.2), 0.3, 0.795, -0.08);
  B.pop();
  ctx.col.addBox(6.6, 3.35, 6.97, 4.15, {y1: 0.8});
  ctx.phone = rotaryPhone(ctx, 6.82, 0.795, 3.7, Math.PI / 2);
  ctx.drawer = {pos: new THREE.Vector3(6.97, 0.7, 3.95)};
  // the note stuck in the mirror frame
  const noteMat = new THREE.MeshStandardMaterial({map: T.label('Внучек!', {w: 128, h: 96, bg: '#eee6cf', fg: '#33335a', font: 'italic 26px PT Serif, serif'}), roughness: 0.9, side: THREE.DoubleSide});
  const note = new THREE.Mesh(new THREE.PlaneGeometry(0.11, 0.08), noteMat);
  note.position.set(6.64, 1.72, 3.98); note.rotation.set(0, Math.PI / 2, 0.08); scene.add(note);
  ctx.note = note;

  // --- coat rack + shoe shelf on east wall near the door
  B.push(8.8, 0, 5.15, -Math.PI / 2);
  B.add(rboxGeo(1.0, 0.12, 0.025, 0.005), wood, 0, 1.72, 0.012);
  for (let i = 0; i < 5; i++) B.add(tube([[0, 0, 0], [0, 0, 0.06], [0, 0.04, 0.08]], 0.006, 8, 6), M.brass, -0.4 + i * 0.2, 1.7, 0.02);
  B.add(rboxGeo(1.0, 0.02, 0.28, 0.005), wood, 0, 1.92, 0.14);
  // fur hat on shelf
  B.add(new THREE.SphereGeometry(0.11, 16, 10, 0, Math.PI * 2, 0, Math.PI / 2), std(0x4a3a2c, 1), 0.25, 1.93, 0.14);
  // grandma's coat (draped shape)
  const coat = lathe([[0, 0], [0.04, 0], [0.16, -0.1], [0.2, -0.5], [0.24, -1.05], [0.0, -1.05]], 14); coat.scale(1, 1, 0.45);
  B.add(coat, std(0x3e2b22, 1), -0.2, 1.72, 0.14);
  B.add(coat, std(0x2c3036, 1), 0.2, 1.72, 0.13);
  // shawl and string bag
  B.add(foldedPlane(0.3, 0.6, 3, 0.02, 12, 6), M.jacquard, 0.4, 1.4, 0.07);
  B.add(new THREE.TorusGeometry(0.1, 0.004, 4, 16), std(0x6a7a3a, 0.8), -0.42, 1.55, 0.05);
  // shoe shelf
  B.add(rboxGeo(0.9, 0.04, 0.3, 0.006), wood, 0, 0.35, 0.15);
  B.add(rboxGeo(0.9, 0.04, 0.3, 0.006), wood, 0, 0.05, 0.15);
  for (const s of [-1, 1]) B.add(rboxGeo(0.03, 0.34, 0.3, 0.004), wood, s * 0.44, 0.2, 0.15);
  // slippers
  const slip = std(0x6a2a24, 1);
  for (const [sx, sz] of [[-0.3, 0.12], [-0.22, 0.12], [0.1, 0.13], [0.18, 0.13]]) { const sg = new THREE.CapsuleGeometry(0.035, 0.18, 4, 8); sg.rotateX(Math.PI / 2); sg.scale(1, 0.5, 1); B.add(sg, slip, sx, 0.385, sz); }
  B.pop();
  ctx.placeModel('rubber_boots', {child: 'rubber_boots_l', height: 0.38}, 8.62, 0.07, 4.8, -Math.PI / 2 + 0.2);
  ctx.col.addBox(8.48, 4.65, 8.8, 5.65, {y1: 0.4});
  ctx.coatHooks = new THREE.Vector3(8.75, 1.65, 5.35);
  ctx.shoeShelf = new THREE.Vector3(8.6, 0.3, 5.1);

  // --- electric meter on south wall left of door
  B.push(7.05, 0, 6.0, Math.PI);
  B.add(rboxGeo(0.3, 0.4, 0.12, 0.008), std(0x5a5e5a, 0.6, 0.3), 0, 1.95, 0.06);
  B.add(new THREE.PlaneGeometry(0.16, 0.12), std(0x111111, 0.1), 0, 2.0, 0.121);
  B.add(cyl(0.05, 0.05, 0.004, 20), M.chrome, 0, 1.96, 0.123, 0, Math.PI / 2);
  for (const dx of [-0.08, 0.08]) B.add(rboxGeo(0.04, 0.06, 0.03, 0.006), std(0x111111, 0.4), dx, 1.82, 0.12);
  B.add(cyl(0.004, 0.004, 0.5, 4), M.black, 0.1, 2.25, 0.005);
  B.pop();
  // doorbell box above door inside
  B.add(rboxGeo(0.12, 0.08, 0.05, 0.01), M.whitePlastic, 8.05, 2.2, 5.97);

  // --- runner rug in hall
  const rt = T.carpet('floor');
  const rm = new THREE.MeshStandardMaterial({map: rt.map, roughness: 1, color: 0x9a8878});
  B.add(rboxGeo(0.7, 0.006, 2.2, 0.002), rm, 7.7, 0.003, 4.6, 0, 0, 0, {cast: false});
  B.add(rboxGeo(1.6, 0.006, 0.65, 0.002), rm, 5.7, 0.003, 5.25, 0, 0, 0, {cast: false});

  // --- mezzanine (антресоли) above hall B
  B.add(rboxGeo(1.85, 0.03, 0.6, 0.005), wood, 5.65, 2.02, 5.68);
  for (const dx of [-0.46, 0.46]) { B.add(rboxGeo(0.9, 0.44, 0.02, 0.005), wood, 5.65 + dx, 2.25, 5.37); B.add(rboxGeo(0.012, 0.06, 0.02, 0.003), M.brass, 5.65 + dx + (dx < 0 ? 0.4 : -0.4), 2.25, 5.35); }

  // lights & switches
  ctx.hallLamp = domeLamp(scene, M, 7.7, 2.5, 4.6, 0.14);
  ctx.switchHall = lightSwitch(scene, M, 8.62, 1.5, 5.99, Math.PI);
  socket(B, M, 6.6, 0.3, 4.4, Math.PI / 2);
}

function bedroom(ctx) {
  const {B, M, scene} = ctx;
  // iron bed with mattress, blanket and pillow pyramid under lace cover
  ctx.placeModel('old_bed_frame', {width: 2.0}, 0.52, 0, 3.3, 0);
  const bx = 0.52, bz = 3.3;
  B.add(rboxGeo(0.88, 0.16, 1.9, 0.06), std(0xe8e0cc, 1), bx, 0.5, bz);
  const blanket = foldedPlane(1.0, 1.55, 3, 0.02, 20, 12); blanket.rotateX(-Math.PI / 2);
  const bp = blanket.attributes.position;
  for (let i = 0; i < bp.count; i++) { const xx = bp.getX(i); if (Math.abs(xx) > 0.44) bp.setY(i, bp.getY(i) - (Math.abs(xx) - 0.44) * 3); }
  blanket.computeVertexNormals();
  B.add(blanket, M.bedspread, bx, 0.59, bz - 0.15);
  // pillows pyramid at head (south)
  const pil = std(0xf4efe4, 1);
  for (const [w, h, y] of [[0.7, 0.2, 0.68], [0.6, 0.18, 0.86], [0.48, 0.16, 1.02]]) B.add(rboxGeo(w, h, 0.5, 0.08, 3), pil, bx, y, bz + 0.65 - y * 0.05);
  B.add(foldedPlane(0.82, 0.55, 8, 0.01, 30, 4), M.tulle, bx, 0.86, bz + 0.38, 0, -0.35, 0);
  ctx.col.addBox(0, 2.25, 1.02, 4.4, {y1: 0.8});
  // grandpa portrait with mourning ribbon above bed (west wall)
  frame(B, M, 0, 1.6, 3.4, Math.PI / 2, 0.3, 0.4, 1, M.cherry);
  B.add(boxGeo(0.01, 0.12, 0.025), std(0x050505, 0.8), 0.02, 1.76, 3.27, 0, 0, 0.7);
  frame(B, M, 0, 1.55, 2.8, Math.PI / 2, 0.2, 0.26, 4);
  // nightstand with alarm clock, glasses, pills, water
  B.add(rboxGeo(0.4, 0.55, 0.38, 0.008), M.walnut, 0.25, 0.275, 1.95);
  B.add(rboxGeo(0.36, 0.14, 0.012, 0.004), M.walnut, 0.25, 0.45, 2.145);
  ctx.placeModel('alarm_clock_01', {height: 0.12}, 0.2, 0.55, 1.88, 0.6);
  B.add(lathe([[0, 0], [0.03, 0], [0.032, 0.1], [0, 0.1]], 14), M.glassThick, 0.36, 0.55, 2.02, 0, 0, 0, {cast: false});
  B.add(new THREE.TorusGeometry(0.02, 0.002, 4, 12), std(0x222222, 0.3, 0.6), 0.3, 0.556, 1.82, 0, Math.PI / 2, 0);
  ctx.col.addBox(0.05, 1.75, 0.45, 2.15, {y1: 0.6});
  // night lamp on nightstand
  B.add(cyl(0.04, 0.05, 0.02, 14), M.darkMetal, 0.12, 0.56, 2.05);
  B.add(cyl(0.006, 0.006, 0.2, 6), M.brass, 0.12, 0.66, 2.05);
  const nlMat = new THREE.MeshStandardMaterial({color: 0xe8b47a, roughness: 1, side: THREE.DoubleSide, emissive: 0xff9440, emissiveIntensity: 1});
  const nl = new THREE.Mesh(lathe([[0.09, 0], [0.085, 0.02], [0.06, 0.12]], 20, true), nlMat); nl.position.set(0.12, 0.7, 2.05); scene.add(nl);
  ctx.nightLamp = {mat: nlMat, pos: new THREE.Vector3(0.12, 0.78, 2.05)};
  // wardrobe (шифоньер) on east wall
  B.push(2.8 - 0.28, 0, 2.3, -Math.PI / 2);
  B.add(rboxGeo(1.0, 1.85, 0.55, 0.01), M.walnut, 0, 0.98, 0);
  B.add(boxGeo(0.94, 0.06, 0.5), std(0x2a1a12, 0.8), 0, 0.03, 0);
  for (const s of [-1, 1]) { B.add(rboxGeo(0.48, 1.7, 0.02, 0.005), M.walnut, s * 0.245, 0.98, 0.28); B.add(rboxGeo(0.014, 0.1, 0.025, 0.004), M.brass, s * 0.03, 1.05, 0.3); }
  B.add(new THREE.PlaneGeometry(0.36, 1.2), std(0x8a9294, 0.05, 1, {envMapIntensity: 0.6}), 0.245, 1.05, 0.292);
  B.pop();
  ctx.placeModel('vintage_suitcase', {child: 'vintage_suitcase_01', width: 0.65}, 2.5, 1.905, 2.3, Math.PI / 2 + 0.05);
  ctx.col.addBox(2.24, 1.8, 2.8, 2.8, {y1: 1.9});
  // icon corner (красный угол) in the north-west corner, with lampada
  B.add(new THREE.CylinderGeometry(0.3, 0.3, 0.025, 4, 1, false, 0, Math.PI / 2), M.walnut, 0, 1.85, 0, Math.PI / 2);
  const iconMat = new THREE.MeshStandardMaterial({map: iconTex(), roughness: 0.4, metalness: 0.3});
  B.add(rboxGeo(0.15, 0.2, 0.02, 0.004), iconMat, 0.14, 2.0, 0.14, Math.PI / 4);
  B.add(rboxGeo(0.11, 0.15, 0.02, 0.004), iconMat, 0.03, 1.95, 0.26, Math.PI / 2);
  const lamp = new THREE.Mesh(lathe([[0, 0], [0.03, 0.01], [0.04, 0.05], [0.042, 0.07]], 16), new THREE.MeshStandardMaterial({color: 0xa01010, roughness: 0.2, transparent: true, opacity: 0.85, emissive: 0xff2010, emissiveIntensity: 0.8}));
  lamp.position.set(0.16, 1.865, 0.16); scene.add(lamp);
  const flame = new THREE.Mesh(new THREE.SphereGeometry(0.008, 8, 6), new THREE.MeshBasicMaterial({color: 0xffc070}));
  flame.scale.y = 2; flame.position.set(0.16, 1.94, 0.16); scene.add(flame);
  ctx.lampada = {mat: lamp.material, flame, pos: new THREE.Vector3(0.18, 1.95, 0.2)};
  // chair with shawl by the window and a small sewing table
  B.push(2.2, 0, 0.75, -2.5);
  for (const [lx, lz] of [[-0.18, -0.18], [0.18, -0.18], [-0.18, 0.18], [0.18, 0.18]]) B.add(rboxGeo(0.035, 0.45, 0.035, 0.005), M.walnut, lx, 0.225, lz);
  B.add(rboxGeo(0.42, 0.04, 0.42, 0.01), M.walnut, 0, 0.46, 0);
  for (const s of [-1, 1]) B.add(rboxGeo(0.035, 0.5, 0.035, 0.005), M.walnut, s * 0.18, 0.72, -0.19);
  B.add(rboxGeo(0.4, 0.12, 0.02, 0.005), M.walnut, 0, 0.9, -0.19);
  B.add(foldedPlane(0.4, 0.55, 4, 0.02, 16, 6), std(0x5a3a5a, 1), 0, 0.7, -0.22, 0, 0.1, 0);
  B.pop();
  ctx.col.addBoxC(2.2, 0.75, 0.5, 0.5, {y1: 0.5});
  radiator(B, M, 1.4, 0.09, 7, 0);
  curtains(B, M, scene, {a0: 0.7, a1: 2.1, z: 0, heavy: true, drapeMat: std(0x6a5a3a, 1)});
  ctx.placeModel('potted_plant_01', {height: 0.42}, 1.0, 0.88, -0.1, 0);
  geranium(B, M, 1.7, 0.88, -0.1, 0.85);
  // small rug
  const rt = T.carpet('floor');
  B.add(rboxGeo(0.9, 0.006, 1.6, 0.002), new THREE.MeshStandardMaterial({map: rt.map, roughness: 1}), 1.5, 0.003, 3.1, 0, 0, 0, {cast: false});
  ctx.switchBedroom = lightSwitch(scene, M, 2.79, 1.5, 3.2, -Math.PI / 2);
}

function iconTex() {
  const [c, g] = T.canvas(128, 160);
  const gr = g.createLinearGradient(0, 0, 0, 160); gr.addColorStop(0, '#c89a40'); gr.addColorStop(1, '#7a5a20');
  g.fillStyle = gr; g.fillRect(0, 0, 128, 160);
  g.fillStyle = '#3a2412'; g.fillRect(10, 10, 108, 140);
  g.fillStyle = '#d8b050'; g.beginPath(); g.arc(64, 58, 30, 0, Math.PI * 2); g.fill();
  g.fillStyle = '#b08060'; g.beginPath(); g.ellipse(64, 62, 15, 19, 0, 0, Math.PI * 2); g.fill();
  g.fillStyle = '#5a1a14'; g.beginPath(); g.moveTo(30, 150); g.quadraticCurveTo(64, 70, 98, 150); g.fill();
  g.fillStyle = '#1a2a4a'; g.beginPath(); g.moveTo(40, 150); g.quadraticCurveTo(64, 92, 88, 150); g.fill();
  T.grain(g, 128, 160, 0.08, 4);
  return T.tex(c, {repeat: false});
}

function landing(ctx) {
  const {B, M, scene, col} = ctx;
  const L = {x0: 6.0, x1: 10.4, z0: 6.3, z1: 8.0};
  const CEIL = 2.55;
  // tile floor (метлахская плитка)
  const tileMat = new THREE.MeshStandardMaterial({map: metlakh(), roughness: 0.55});
  tileMat.map.repeat.set(1 / 0.6, 1 / 0.6);
  B.add(planeGeo(L.x1 - L.x0, L.z1 - L.z0 + 0.3, 1), tileMat, (L.x0 + L.x1) / 2, 0, (L.z0 + L.z1 + 0.3) / 2, 0, -Math.PI / 2, 0, {cast: false});
  col.addFloor(L.x0, L.z0, L.x1, L.z1 + 0.3, 0);
  // walls: lower green paint, upper whitewash, dividing line
  const wallSeg = (x0, z0, x1, z1, ry, holes = []) => {
    const len = Math.hypot(x1 - x0, z1 - z0);
    const cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
    B.push(cx, 0, cz, ry);
    let a = -len / 2;
    const pieces = [];
    for (const [h0, h1, hy] of holes) { pieces.push([a, h0, 0]); pieces.push([h0, h1, hy]); a = h1; }
    pieces.push([a, len / 2, 0]);
    for (const [p, q, hy] of pieces) {
      if (q - p < 0.001) continue;
      const w = q - p, mx = (p + q) / 2;
      if (hy) { // only above opening
        B.add(planeGeo(w, CEIL - hy, 1), M.stairWhite, mx, (hy + CEIL) / 2, 0, 0, 0, 0, {cast: false});
        continue;
      }
      B.add(planeGeo(w, 1.5, 1), M.stairGreen, mx, 0.75, 0, 0, 0, 0, {cast: false});
      B.add(planeGeo(w, CEIL - 1.5, 1), M.stairWhite, mx, 1.5 + (CEIL - 1.5) / 2, 0, 0, 0, 0, {cast: false});
      B.add(boxGeo(w, 0.02, 0.004), std(0x2a3a2a, 0.5), mx, 1.5, 0.002, 0, 0, 0, {cast: false});
    }
    B.add(planeGeo(len, 3.0, 1), M.stairGreen, 0, -1.5, 0, 0, 0, 0, {cast: false});
    B.pop();
  };
  // north wall (our facade side) faces +z: our door 7.6..8.5, neighbour door 9.4..10.25
  wallSeg(L.x0, 6.3, L.x1, 6.3, 0, [[7.6 - 8.2, 8.5 - 8.2, 2.05], [9.4 - 8.2, 10.25 - 8.2, 2.05]]);
  // west wall faces +x, neighbour door 59 at z 6.65..7.5
  wallSeg(6.0, 6.3, 6.0, 12.0, Math.PI / 2, [[-(7.5 - 9.15), -(6.65 - 9.15), 2.05]].map(([a, b, h]) => [Math.min(a, b), Math.max(a, b), h]));
  wallSeg(10.4, 12.0, 10.4, 6.3, -Math.PI / 2);
  wallSeg(10.4, 12.0, 6.0, 12.0, Math.PI);
  B.add(planeGeo(4.4, 5.7, 1), M.stairWhite, 8.2, CEIL, 9.15, 0, Math.PI / 2, 0, {cast: false});
  // stairwell below our landing: edge of the landing slab and the 4th-floor landing
  B.add(planeGeo(2.9, 2.85, 1), M.stairGreen, 7.45, -1.42, 8.0, 0, 0, 0, {cast: false});
  B.add(planeGeo(4.4, 5.7, 1), tileMat, 8.2, -2.7, 9.15, 0, -Math.PI / 2, 0, {cast: false});
  col.addBox(5.7, 6.3, 6.0, 12, {});
  col.addBox(10.4, 6.3, 10.7, 12, {});
  // neighbour doors
  const brown = std(0x5a3424, 0.5);
  B.add(rboxGeo(0.05, 2.0, 0.85, 0.008), brown, 6.02, 1.0, 7.07);
  B.add(rboxGeo(0.02, 0.12, 0.02, 0.004), M.brass, 6.05, 1.0, 7.38);
  B.add(new THREE.PlaneGeometry(0.08, 0.06), new THREE.MeshStandardMaterial({map: T.label('59', {w: 128, h: 96, bg: '#c0b090', fg: '#222', font: 'bold 60px PT Serif, serif'}), roughness: 0.4}), 6.05, 1.65, 7.07, Math.PI / 2);
  const steel = std(0x3a2a22, 0.45, 0.6);
  B.add(rboxGeo(0.85, 2.02, 0.06, 0.006), steel, 9.82, 1.01, 6.3);
  for (const y of [0.5, 1.0, 1.5]) B.add(boxGeo(0.7, 0.01, 0.01), std(0x2a1a14, 0.5, 0.6), 9.82, y, 6.335);
  B.add(rboxGeo(0.03, 0.15, 0.03, 0.006), M.chrome, 10.15, 1.0, 6.345);
  B.add(new THREE.PlaneGeometry(0.08, 0.06), new THREE.MeshStandardMaterial({map: T.label('61', {w: 128, h: 96, bg: '#b0a890', fg: '#222', font: 'bold 60px PT Serif, serif'}), roughness: 0.4}), 9.82, 1.7, 6.34);
  // our door frame (outside casings) + doorbell button
  B.add(rboxGeo(0.06, 0.05, 0.03, 0.01), std(0x1a1a1a, 0.5), 8.62, 1.5, 6.315);
  B.add(cyl(0.008, 0.008, 0.012, 10), std(0xb8b0a0, 0.4), 8.62, 1.5, 6.33, 0, Math.PI / 2);
  // doormat
  B.add(rboxGeo(0.7, 0.012, 0.45, 0.004), std(0x5a4a32, 1), 8.05, 0.006, 6.6, 0, 0, 0, {cast: false});
  // electrical panel on east wall
  B.add(rboxGeo(0.06, 0.95, 0.6, 0.008), std(0x6a706a, 0.5, 0.4), 10.37, 1.5, 7.3);
  B.add(new THREE.PlaneGeometry(0.4, 0.04), new THREE.MeshStandardMaterial({map: T.label('ЩИТОК', {w: 256, h: 32, bg: '#6a706a', fg: '#b03020', font: 'bold 22px PT Mono, monospace'}), roughness: 0.6}), 10.335, 1.85, 7.3, -Math.PI / 2);
  // roof ladder + hatch
  for (const z of [6.95, 7.45]) B.add(boxGeo(0.03, 2.5, 0.03), std(0x2a2a2a, 0.6, 0.5), 6.08, 1.25, z);
  for (let y = 0.4; y < 2.4; y += 0.3) B.add(cyl(0.01, 0.01, 0.5, 6), std(0x2a2a2a, 0.6, 0.5), 6.08, y, 7.2, 0, Math.PI / 2);
  B.add(boxGeo(0.7, 0.03, 0.7), std(0x3a3a34, 0.6, 0.5), 6.4, CEIL - 0.01, 7.2);
  B.add(rboxGeo(0.06, 0.08, 0.03, 0.01), std(0x6a6a60, 0.4, 0.8), 6.5, CEIL - 0.07, 7.2);
  col.addBox(6.0, 6.85, 6.15, 7.55, {});
  // stairs down (flight 1) x 8.9..10.4 from z 8.0 descending, flight 2 x 6.0..8.6 below
  const stepMat = M.stairConcrete;
  for (let i = 0; i < 9; i++) {
    const y = -0.15 * (i + 1), z = 8.0 + i * 0.29;
    B.add(boxGeo(1.5, 0.15, 0.29, 1), stepMat, 9.65, y + 0.075 - 0.0, z + 0.145);
    B.add(boxGeo(1.5, 1.2, 0.29, 1), stepMat, 9.65, y - 0.6, z + 0.145);
  }
  for (let i = 0; i < 9; i++) {
    const y = -1.35 - 0.15 * (i + 1), z = 10.6 - (i + 1) * 0.29;
    B.add(boxGeo(2.5, 0.15, 0.29, 1), stepMat, 7.3, y + 0.075, z + 0.145);
  }
  B.add(boxGeo(4.4, 0.15, 1.35, 1), stepMat, 8.2, -1.35 - 0.075, 11.3);
  // railings with wooden handrail
  const rail = std(0x22201e, 0.6, 0.6), hand = M.walnut;
  for (let x = 6.1; x < 8.85; x += 0.12) B.add(cyl(0.008, 0.008, 0.9, 5), rail, x, 0.45, 8.02);
  B.add(rboxGeo(2.8, 0.05, 0.06, 0.02), hand, 7.45, 0.92, 8.02);
  for (let i = 0; i < 9; i++) B.add(cyl(0.008, 0.008, 0.9, 5), rail, 8.86, 0.45 - i * 0.15, 8.1 + i * 0.29);
  const hr = tube([[8.86, 0.92, 8.0], [8.86, 0.92 - 1.35, 10.6]], 0.025, 4, 6);
  B.add(hr, hand, 0, 0, 0);
  col.addBox(6.0, 8.0, 8.9, 8.05, {y1: 0.95});
  // invisible block at stairs ("мне не вниз")
  ctx.stairBlock = col.addBox(8.9, 8.2, 10.4, 8.3, {noOcclude: true});
  col.addFloor(8.9, 8.0, 10.4, 8.3, -0.08);
  // stairwell window on mid landing (faint cold light)
  B.add(new THREE.PlaneGeometry(1.2, 1.1), std(0x10161c, 0.2, 0, {emissive: 0x1c2630, emissiveIntensity: 1}), 8.2, -0.1, 11.99, Math.PI);
  // bulb
  const bulbMat = new THREE.MeshStandardMaterial({color: 0xfff2d0, emissive: 0xffd28a, emissiveIntensity: 3});
  const bulb = new THREE.Mesh(new THREE.SphereGeometry(0.035, 12, 10), bulbMat); bulb.position.set(8.05, CEIL - 0.16, 7.15); bulb.scale.y = 1.3; scene.add(bulb);
  B.add(cyl(0.025, 0.03, 0.06, 10), std(0x111111, 0.5), 8.05, CEIL - 0.1, 7.15);
  B.add(cyl(0.003, 0.003, 0.07, 4), M.black, 8.05, CEIL - 0.035, 7.15);
  ctx.landingBulb = {mat: bulbMat, pos: new THREE.Vector3(8.05, CEIL - 0.2, 7.15)};
  // wiring along the ceiling from the panel to the bulb
  const wire = std(0x1a1a1a, 0.6);
  B.add(cyl(0.006, 0.006, 2.3, 4), wire, 9.2, CEIL - 0.03, 7.25, 0, 0, Math.PI / 2);
  B.add(cyl(0.006, 0.006, 0.7, 4), wire, 10.36, CEIL - 0.38, 7.3);
  B.add(cyl(0.006, 0.006, 1.1, 4), wire, 9.85, CEIL - 0.03, 7.27, 0, 0, Math.PI / 2);
  // dirt band along the floor and scuffs
  const dirt = new THREE.MeshBasicMaterial({map: dirtStrip(), transparent: true, depthWrite: false, color: 0x000000});
  for (const [x, z, w, ry] of [[8.2, 6.31, 4.4, 0], [6.01, 9.15, 5.7, Math.PI / 2], [10.39, 9.15, 5.7, -Math.PI / 2]]) B.add(new THREE.PlaneGeometry(w, 0.35), dirt, x, 0.175, z, ry, 0, 0, {cast: false, receive: false});
  // scratched graffiti on the green paint
  const gm = new THREE.MeshStandardMaterial({map: graffiti(), transparent: true, depthWrite: false, roughness: 0.6});
  B.add(new THREE.PlaneGeometry(0.7, 0.35), gm, 6.012, 1.05, 7.9, Math.PI / 2, 0, 0, {cast: false});
  // sled by flat 59 and a rag rug at 61
  B.add(boxGeo(0.34, 0.03, 0.8), std(0x6a4a2a, 0.7), 6.25, 0.4, 7.75, 0, 0, 1.2);
  B.add(rboxGeo(0.6, 0.01, 0.4, 0.003), std(0x4a3a4a, 1), 9.82, 0.005, 6.6, 0, 0, 0, {cast: false});
  // glow halo on the bulb
  const halo = new THREE.Sprite(new THREE.SpriteMaterial({map: haloTex(), color: 0xffc070, transparent: true, opacity: 0.35, depthWrite: false, blending: THREE.AdditiveBlending}));
  halo.scale.set(0.6, 0.6, 1); halo.position.set(8.05, CEIL - 0.16, 7.15); scene.add(halo);
  ctx.landingHalo = halo;
}

function dirtStrip() {
  const [c, g] = T.canvas(256, 64);
  const gr = g.createLinearGradient(0, 64, 0, 0); gr.addColorStop(0, 'rgba(255,255,255,0.55)'); gr.addColorStop(1, 'rgba(255,255,255,0)');
  g.fillStyle = gr; g.fillRect(0, 0, 256, 64);
  for (let i = 0; i < 40; i++) { g.fillStyle = `rgba(255,255,255,${Math.random() * 0.3})`; g.fillRect(Math.random() * 256, 30 + Math.random() * 34, 2 + Math.random() * 20, 2 + Math.random() * 6); }
  const t = T.tex(c); t.wrapS = THREE.RepeatWrapping; t.repeat.set(4, 1);
  // use as alpha via black color
  return t;
}
function graffiti() {
  const [c, g] = T.canvas(256, 128);
  g.clearRect(0, 0, 256, 128);
  g.strokeStyle = 'rgba(230,225,200,0.55)'; g.lineWidth = 2.2; g.font = 'bold 44px PT Mono, monospace';
  g.strokeText('ЦОЙ ЖИВ', 18, 70);
  g.lineWidth = 1.2; g.font = '20px PT Serif, serif'; g.strokeText('Лена + Витя', 60, 108);
  return T.tex(c, {repeat: false});
}
function haloTex() {
  const [c, g] = T.canvas(64);
  const r = g.createRadialGradient(32, 32, 0, 32, 32, 32); r.addColorStop(0, 'rgba(255,255,255,1)'); r.addColorStop(0.3, 'rgba(255,255,255,0.3)'); r.addColorStop(1, 'rgba(255,255,255,0)');
  g.fillStyle = r; g.fillRect(0, 0, 64, 64);
  return T.tex(c, {repeat: false});
}

function metlakh() {
  const [c, g] = T.canvas(256);
  g.fillStyle = '#4a3a2c'; g.fillRect(0, 0, 256, 256);
  srand(77);
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const v = rnd();
    g.fillStyle = (x + y) % 2 ? `rgb(${150 + v * 20 | 0},${120 + v * 18 | 0},${90 + v * 14 | 0})` : `rgb(${110 + v * 18 | 0},${70 + v * 12 | 0},${50 + v * 10 | 0})`;
    g.fillRect(x * 16 + 1, y * 16 + 1, 14, 14);
  }
  T.grain(g, 256, 256, 0.1, 3);
  return T.tex(c);
}

export function buildHall(ctx) { hallway(ctx); bedroom(ctx); landing(ctx); }
