import * as THREE from 'three';
import {boxGeo, rboxGeo, cyl, lathe, tube, rnd, srand} from '../geo.js';
import {std} from '../materials.js';
import * as T from '../textures.js';
import {radiator, curtains, books, jar, cup, socket, lightSwitch, kitchenClock, domeLamp, geranium, foldedPlane} from './common.js';

function stove(ctx) {
  const {B, M, scene} = ctx;
  const x = 8.8 - 0.3, z = 1.7;
  const body = M.enamelWhite;
  B.push(x, 0, z, -Math.PI / 2);
  B.add(rboxGeo(0.5, 0.8, 0.58, 0.02), body, 0, 0.42, 0);
  B.add(boxGeo(0.46, 0.06, 0.5), std(0x1c1c1c, 0.6), 0, 0.03, 0); // plinth
  // oven door
  B.add(rboxGeo(0.46, 0.44, 0.03, 0.012), body, 0, 0.36, 0.3);
  B.add(rboxGeo(0.26, 0.17, 0.006, 0.01), std(0x0d0d0d, 0.1), 0, 0.4, 0.317);
  B.add(cyl(0.01, 0.01, 0.36, 10), M.chrome, 0, 0.6, 0.345, 0, 0, Math.PI / 2);
  for (const s of [-1, 1]) B.add(boxGeo(0.015, 0.015, 0.035), M.chrome, s * 0.17, 0.6, 0.33);
  // towel on handle
  B.add(foldedPlane(0.2, 0.32, 3, 0.01, 10, 6), M.checkCloth, 0.05, 0.46, 0.355, 0, 0.05, 0);
  // control strip with knobs
  B.add(boxGeo(0.48, 0.07, 0.03), std(0xdcdad2, 0.3), 0, 0.73, 0.29, 0, -0.25, 0);
  const knobs = [];
  for (let i = 0; i < 5; i++) {
    const k = new THREE.Mesh(cyl(0.017, 0.02, 0.022, 14), M.bakelite);
    k.rotation.x = Math.PI / 2 - 0.25;
    k.position.copy(B.worldPos(-0.18 + i * 0.09, 0.73, 0.31));
    k.quaternion.premultiply(new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), -Math.PI / 2));
    scene.add(k); knobs.push(k);
  }
  // cooktop
  B.add(rboxGeo(0.5, 0.025, 0.58, 0.008), body, 0, 0.83, 0);
  const burners = [[-0.11, -0.12], [0.11, -0.12], [-0.11, 0.12], [0.11, 0.12]];
  for (const [bx, bz] of burners) {
    B.add(cyl(0.05, 0.055, 0.015, 20), std(0x2a2a2a, 0.5, 0.4), bx, 0.85, bz);
    B.add(cyl(0.035, 0.035, 0.012, 20), std(0x111111, 0.4, 0.2), bx, 0.862, bz);
  }
  // grates
  const grate = std(0x161616, 0.6, 0.3);
  for (const bz of [-0.12, 0.12]) {
    B.add(boxGeo(0.46, 0.012, 0.012), grate, 0, 0.88, bz - 0.09);
    B.add(boxGeo(0.46, 0.012, 0.012), grate, 0, 0.88, bz + 0.09);
    for (const bx of [-0.22, -0.11, 0, 0.11, 0.22]) B.add(boxGeo(0.012, 0.012, 0.19), grate, bx, 0.88, bz);
  }
  // back splash lid
  B.add(rboxGeo(0.5, 0.12, 0.02, 0.006), body, 0, 0.9, -0.29, 0, 0.1, 0);
  B.pop();
  ctx.col.addBox(8.2, 1.45, 8.8, 1.95, {y1: 0.9});
  // flame on front-right burner (where the kettle sits): world pos
  const fp = new THREE.Vector3(x - 0.12, 0.865, z + 0.11);
  const flameMat = new THREE.MeshBasicMaterial({color: 0x5aa0ff, transparent: true, opacity: 0.85, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide});
  const flame = new THREE.Group();
  for (let i = 0; i < 14; i++) {
    const a = i / 14 * Math.PI * 2;
    const f = new THREE.Mesh(new THREE.ConeGeometry(0.008, 0.035, 5, 1, true), flameMat);
    f.position.set(Math.cos(a) * 0.04, 0.012, Math.sin(a) * 0.04);
    f.rotation.z = -Math.cos(a) * 0.5; f.rotation.x = Math.sin(a) * 0.5;
    flame.add(f);
  }
  flame.position.copy(fp); flame.visible = false; scene.add(flame);
  ctx.stove = {knobs, flame, flamePos: fp, potPos: new THREE.Vector3(x + 0.12, 0.865, z - 0.11), kettlePos: new THREE.Vector3(x - 0.12, 0.89, z + 0.11)};
}

function kettle(ctx) {
  const {M, scene} = ctx;
  const g = new THREE.Group();
  const steel = std(0xc9c9c4, 0.22, 1);
  const body = new THREE.Mesh(lathe([[0, 0], [0.085, 0], [0.1, 0.02], [0.105, 0.06], [0.095, 0.11], [0.06, 0.15], [0.04, 0.16], [0.0, 0.16]], 32), steel);
  body.castShadow = true; g.add(body);
  const knob = new THREE.Mesh(new THREE.SphereGeometry(0.018, 12, 8), M.blackPlastic); knob.position.y = 0.17; g.add(knob);
  const handle = new THREE.Mesh(tube([[-0.06, 0.14, 0], [-0.045, 0.24, 0], [0.045, 0.24, 0], [0.06, 0.14, 0]], 0.009, 20, 8), M.blackPlastic); g.add(handle);
  const spout = new THREE.Mesh(tube([[0.08, 0.05, 0], [0.13, 0.1, 0], [0.16, 0.16, 0]], 0.013, 12, 8), steel); g.add(spout);
  const whistle = new THREE.Mesh(cyl(0.016, 0.016, 0.03, 10), M.blackPlastic); whistle.position.set(0.165, 0.175, 0); whistle.rotation.z = -0.6; g.add(whistle);
  g.position.copy(ctx.stove.kettlePos); g.rotation.y = Math.PI * 0.85;
  scene.add(g);
  ctx.kettle = g;
  ctx.placeModel('pot_enamel_01', {height: 0.17}, ctx.stove.potPos.x, ctx.stove.potPos.y, ctx.stove.potPos.z, 0.3);
}

function fridge(ctx) {
  const {B, M} = ctx;
  const x = 8.8 - 0.32, z = 2.55;
  B.push(x, 0, z, -Math.PI / 2);
  const w = 0.6, h = 1.42, d = 0.6;
  B.add(rboxGeo(w, h - 0.08, d, 0.07, 4), M.enamelCream, 0, h / 2 + 0.04, 0);
  B.add(boxGeo(w - 0.06, 0.08, d - 0.06), std(0x1b1b1b, 0.6), 0, 0.04, 0);
  // door seam & chrome
  B.add(boxGeo(w - 0.03, 0.004, 0.004), std(0x9c968a, 0.6), 0, 1.32, d / 2 + 0.001);
  B.add(rboxGeo(0.04, 0.26, 0.05, 0.015), M.chrome, w / 2 - 0.06, 1.0, d / 2 + 0.02);
  B.add(rboxGeo(0.18, 0.035, 0.008, 0.005), M.chrome, 0, 1.26, d / 2 + 0.002);
  const logo = new THREE.MeshStandardMaterial({map: T.label('ЗИЛ', {w: 128, h: 32, bg: '#b8b4aa', fg: '#7a1b14', font: 'bold 24px PT Serif, serif'}), roughness: 0.3, metalness: 0.6});
  B.add(new THREE.PlaneGeometry(0.1, 0.025), logo, 0, 1.26, d / 2 + 0.008);
  // top: napkin, matches, jar
  B.add(new THREE.CircleGeometry(0.2, 20), M.lace, 0, h + 0.002, 0, 0, -Math.PI / 2, 0, {cast: false});
  B.pop();
  ctx.col.addBox(8.17, 2.24, 8.8, 2.86, {y1: 1.45});
  // matches on top of the fridge (interaction object)
  const box = new THREE.Group();
  const mb = new THREE.Mesh(boxGeo(0.05, 0.015, 0.036), new THREE.MeshStandardMaterial({map: T.label('СПИЧКИ', {w: 128, h: 96, bg: '#c8a24a', fg: '#3a1d10', font: 'bold 22px PT Serif, serif'}), roughness: 0.8}));
  box.add(mb); box.position.set(8.4, 1.43, 2.5); box.rotation.y = 0.4;
  ctx.scene.add(box);
  ctx.matches = box;
}

function sinkAndCounter(ctx) {
  const {B, M, scene} = ctx;
  const front = M.kitchenWood;
  B.push(8.8 - 0.3, 0, 0.9, -Math.PI / 2); // local x along +z world: from z 0.3 to 1.45 → local -0.6..0.55
  // counter cabinet z 0.3..0.85 (local -0.6..-0.05)
  B.add(rboxGeo(0.55, 0.8, 0.56, 0.008), front, -0.325, 0.42, 0);
  B.add(rboxGeo(0.52, 0.16, 0.02, 0.004), front, -0.325, 0.72, 0.29);
  B.add(rboxGeo(0.52, 0.56, 0.02, 0.004), front, -0.325, 0.33, 0.29);
  B.add(rboxGeo(0.12, 0.015, 0.025, 0.004), M.chrome, -0.325, 0.72, 0.31);
  B.add(rboxGeo(0.015, 0.1, 0.025, 0.004), M.chrome, -0.12, 0.42, 0.31);
  B.add(rboxGeo(0.57, 0.03, 0.6, 0.006), std(0x9a8f7a, 0.6), -0.325, 0.845, 0.0); // worktop
  // sink cabinet local 0..0.55
  B.add(rboxGeo(0.56, 0.8, 0.56, 0.008), front, 0.28, 0.42, 0);
  B.add(rboxGeo(0.26, 0.72, 0.02, 0.004), front, 0.145, 0.42, 0.29);
  B.add(rboxGeo(0.26, 0.72, 0.02, 0.004), front, 0.415, 0.42, 0.29);
  // steel sink top with bowl
  const steel = std(0xb7b9b8, 0.3, 0.9);
  B.add(boxGeo(0.58, 0.02, 0.1), steel, 0.28, 0.85, 0.25);
  B.add(boxGeo(0.58, 0.02, 0.12), steel, 0.28, 0.85, -0.24);
  B.add(boxGeo(0.08, 0.02, 0.38), steel, 0.03, 0.85, 0.01);
  B.add(boxGeo(0.06, 0.02, 0.38), steel, 0.54, 0.85, 0.01);
  B.add(boxGeo(0.44, 0.005, 0.38), std(0x8c8e8d, 0.35, 0.9), 0.28, 0.69, 0.01);
  for (const s of [-1, 1]) B.add(boxGeo(0.44, 0.16, 0.005), steel, 0.28, 0.77, 0.01 + s * 0.19);
  for (const s of [-1, 1]) B.add(boxGeo(0.005, 0.16, 0.38), steel, 0.28 + s * 0.22, 0.77, 0.01);
  // wall mixer tap
  B.add(cyl(0.012, 0.012, 0.2, 10), M.chrome, 0.28, 1.08, -0.27, 0, 0, Math.PI / 2);
  for (const s of [-1, 1]) {
    B.add(cyl(0.012, 0.012, 0.05, 10), M.chrome, 0.28 + s * 0.1, 1.08, -0.25, 0, Math.PI / 2);
    B.add(boxGeo(0.05, 0.008, 0.008), s < 0 ? std(0xa02020, 0.4) : std(0x2040a0, 0.4), 0.28 + s * 0.1, 1.08, -0.215);
    B.add(boxGeo(0.008, 0.05, 0.008), s < 0 ? std(0xa02020, 0.4) : std(0x2040a0, 0.4), 0.28 + s * 0.1, 1.08, -0.215);
  }
  B.add(tube([[0.28, 1.06, -0.27], [0.28, 1.12, -0.15], [0.28, 1.1, -0.05], [0.28, 1.02, -0.02]], 0.01, 16, 8), M.chrome, 0, 0, 0);
  // cutting board + bread box on counter
  ctx.placeModel('russian_food_cans_01', {width: 0.32}, -0.35, 0.86, -0.12, 0.3, B);
  B.add(rboxGeo(0.3, 0.35, 0.02, 0.01), M.kitchenWood, -0.4, 1.03, -0.26, 0, -0.15, 0);
  // bread box (хлебница)
  B.add(rboxGeo(0.34, 0.16, 0.24, 0.05), std(0xc8a070, 0.5), -0.2, 0.94, -0.12);
  B.add(rboxGeo(0.06, 0.012, 0.012, 0.004), M.chrome, -0.2, 0.96, 0.0);
  // towel on a hook
  B.add(foldedPlane(0.25, 0.4, 3, 0.012, 10, 6), M.checkCloth, 0.55, 1.2, -0.29, 0, 0, 0);
  B.pop();
  ctx.col.addBox(8.2, 0.3, 8.8, 1.45, {y1: 0.9});
  // tiles behind counter, sink, stove
  B.add(new THREE.PlaneGeometry(1.75, 0.65), M.tiles, 8.795, 1.2, 1.1, -Math.PI / 2, 0, 0, {cast: false});
  // wall cabinets
  B.push(8.8 - 0.16, 0, 0.88, -Math.PI / 2);
  for (const lx of [-0.29, 0.29]) {
    B.add(rboxGeo(0.57, 0.6, 0.31, 0.008), front, lx, 1.78, 0);
    B.add(rboxGeo(0.54, 0.57, 0.02, 0.005), front, lx, 1.78, 0.16);
    B.add(rboxGeo(0.015, 0.1, 0.025, 0.004), M.chrome, lx + (lx < 0 ? 0.22 : -0.22), 1.6, 0.18);
  }
  B.pop();
}

function gasPipe(ctx) {
  const {B, M} = ctx;
  const p = M.pipeYellow;
  // riser in the window corner, horizontal run above cabinets, drop to stove
  B.add(cyl(0.022, 0.022, 2.5, 12), p, 8.73, 1.25, 0.09);
  B.add(cyl(0.014, 0.014, 1.65, 10), p, 8.75, 2.22, 0.92, 0, Math.PI / 2);
  B.add(cyl(0.014, 0.014, 1.3, 10), p, 8.75, 1.57, 1.74);
  B.add(new THREE.SphereGeometry(0.018, 10, 8), p, 8.75, 2.22, 1.74);
  // valve with red handle
  B.add(cyl(0.024, 0.024, 0.06, 12), M.brass, 8.75, 1.25, 1.74);
  B.add(boxGeo(0.012, 0.012, 0.12), std(0xb02018, 0.4), 8.72, 1.25, 1.74 + 0.04);
  for (let y = 0.4; y < 2.4; y += 0.6) B.add(boxGeo(0.03, 0.02, 0.04), M.darkMetal, 8.78, y, 0.09);
}

function table(ctx) {
  const {B, M} = ctx;
  const x = 6.6 + 0.32, z = 0.85;
  B.add(rboxGeo(0.62, 0.03, 0.92, 0.006), M.kitchenWood, x, 0.73, z);
  // oilcloth slightly hanging over edges
  B.add(boxGeo(0.66, 0.004, 0.96), M.oilcloth, x, 0.748, z, 0, 0, 0, {cast: false});
  for (const s of [-1, 1]) B.add(boxGeo(0.66, 0.05, 0.003), M.oilcloth, x, 0.725, z + s * 0.48);
  B.add(boxGeo(0.003, 0.05, 0.96), M.oilcloth, x + 0.33, 0.725, z);
  for (const [dx, dz] of [[-0.26, -0.41], [0.26, -0.41], [-0.26, 0.41], [0.26, 0.41]]) B.add(rboxGeo(0.045, 0.71, 0.045, 0.006), M.kitchenWood, x + dx, 0.355, z + dz);
  B.add(rboxGeo(0.5, 0.06, 0.02, 0.004), M.kitchenWood, x, 0.66, z + 0.42);
  ctx.col.addBox(6.6, 0.38, 7.25, 1.32, {y1: 0.76});
  // stools
  for (const [sx, sz] of [[7.45, 0.6], [7.45, 1.15]]) {
    B.add(rboxGeo(0.32, 0.03, 0.32, 0.008), M.kitchenWood, sx, 0.45, sz);
    for (const [dx, dz] of [[-0.12, -0.12], [0.12, -0.12], [-0.12, 0.12], [0.12, 0.12]]) B.add(rboxGeo(0.035, 0.44, 0.035, 0.005), M.kitchenWood, sx + dx, 0.22, sz + dz);
    for (const s of [-1, 1]) { B.add(boxGeo(0.25, 0.02, 0.02), M.kitchenWood, sx, 0.12, sz + s * 0.12); B.add(boxGeo(0.02, 0.02, 0.25), M.kitchenWood, sx + s * 0.12, 0.12, sz); }
    ctx.col.addBoxC(sx, sz, 0.32, 0.32, {y1: 0.46, noOcclude: true});
  }
  // on the table: sugar bowl, cup, bread under towel, pirozhki plate under towel
  cup(B, M, x + 0.05, 0.75, z + 0.25, std(0xf2eee4, 0.25), 0.4);
  B.add(lathe([[0, 0], [0.05, 0], [0.065, 0.06], [0.06, 0.07], [0, 0.07]], 18), std(0xe9e0d0, 0.3), x - 0.1, 0.75, z - 0.25);
  B.add(new THREE.SphereGeometry(0.018, 10, 8), std(0xe9e0d0, 0.3), x - 0.1, 0.83, z - 0.25);
  // teapot (заварник) with a red polka-dot pattern feel
  const teaMat = std(0xf2eee4, 0.2), red = std(0xb02a20, 0.3);
  B.add(lathe([[0, 0], [0.05, 0], [0.065, 0.03], [0.062, 0.07], [0.04, 0.09], [0, 0.09]], 20), teaMat, x - 0.18, 0.75, z + 0.3);
  B.add(new THREE.SphereGeometry(0.012, 8, 6), red, x - 0.18, 0.85, z + 0.3);
  B.add(tube([[x - 0.13, 0.78, z + 0.3], [x - 0.09, 0.82, z + 0.3], [x - 0.08, 0.86, z + 0.3]], 0.008, 8, 6), teaMat, 0, 0, 0);
  B.add(new THREE.TorusGeometry(0.025, 0.006, 6, 12, Math.PI), teaMat, x - 0.235, 0.8, z + 0.3, 0, 0, Math.PI / 2);
  for (let i = 0; i < 5; i++) B.add(new THREE.SphereGeometry(0.008, 6, 4), red, x - 0.18 + Math.cos(i * 1.3) * 0.063, 0.8, z + 0.3 + Math.sin(i * 1.3) * 0.063);
  // plate with pirozhki
  const pz = z - 0.02, px = x + 0.08;
  B.add(lathe([[0, 0], [0.12, 0], [0.15, 0.015], [0.15, 0.02], [0.11, 0.008], [0, 0.008]], 28), std(0xf2eee4, 0.25), px, 0.75, pz);
  const dough = std(0xc98a43, 0.65);
  srand(8);
  for (let i = 0; i < 6; i++) {
    const g = new THREE.CapsuleGeometry(0.025, 0.06, 4, 10); g.rotateZ(Math.PI / 2); g.scale(1, 0.7, 1);
    B.add(g, dough, px + (rnd() - 0.5) * 0.12, 0.775 + (i > 3 ? 0.03 : 0), pz + (i % 3 - 1) * 0.06, rnd() * 0.6);
  }
  const towel = foldedPlane(0.34, 0.34, 2, 0.015, 12, 12);
  towel.rotateX(-Math.PI / 2);
  const tp = towel.attributes.position;
  for (let i = 0; i < tp.count; i++) { const dx = tp.getX(i), dz = tp.getZ(i); const r = Math.hypot(dx, dz); tp.setY(i, tp.getY(i) + Math.max(0, 0.06 - r * 0.25) - (r > 0.13 ? (r - 0.13) * 0.8 : 0)); }
  towel.computeVertexNormals();
  const towelMesh = new THREE.Mesh(towel, M.checkCloth); towelMesh.position.set(px, 0.765, pz); towelMesh.castShadow = true;
  ctx.scene.add(towelMesh);
  ctx.pirozhki = {towel: towelMesh, pos: new THREE.Vector3(px, 0.8, pz)};
}

function wallStuff(ctx) {
  const {B, M, scene} = ctx;
  // wired radio (радиоточка) on west wall above table
  B.push(6.6, 0, 1.0, Math.PI / 2);
  B.add(rboxGeo(0.24, 0.15, 0.08, 0.012), std(0x6a4a32, 0.4), 0, 1.62, 0.04);
  B.add(new THREE.PlaneGeometry(0.15, 0.1), new THREE.MeshStandardMaterial({map: T.noiseTex(64, 3), color: 0x2a2620, roughness: 0.9}), -0.03, 1.62, 0.081);
  B.add(cyl(0.015, 0.015, 0.02, 12), M.bakelite, 0.08, 1.62, 0.085, 0, Math.PI / 2);
  // wire down to socket
  B.add(cyl(0.0025, 0.0025, 1.2, 4), M.black, 0.1, 1.0, 0.005);
  B.pop();
  ctx.radioPos = new THREE.Vector3(6.65, 1.62, 1.0);
  // calendar
  B.push(6.6, 0, 1.6, Math.PI / 2);
  B.add(boxGeo(0.2, 0.14, 0.01), std(0x6b3a22, 0.6), 0, 1.68, 0.005);
  B.add(boxGeo(0.14, 0.18, 0.02), new THREE.MeshStandardMaterial({map: T.calendarPage(15), roughness: 0.9}), 0, 1.52, 0.012);
  B.pop();
  // kitchen clock
  ctx.kitchenClock = kitchenClock(scene, M, 6.605, 2.0, 0.85, Math.PI / 2);
  // spice shelf
  B.push(6.6, 0, 2.2, Math.PI / 2);
  B.add(rboxGeo(0.5, 0.02, 0.14, 0.004), M.kitchenWood, 0, 1.45, 0.07);
  for (let i = 0; i < 5; i++) jar(B, M, -0.2 + i * 0.1, 1.46, 0.07, 0.11 + (i % 2) * 0.03, std([0x8a5a20, 0xd8c8a0, 0x5a3a1a, 0xc0a060, 0x7a2a1a][i], 0.8));
  B.pop();
  // window
  radiator(B, M, 7.7, 0.09, 7, 0);
  curtains(B, M, scene, {a0: 7.05, a1: 8.35, z: 0, heavy: false, tulle0: 8.0, tulle1: 8.6});
  // café curtain on lower half
  B.add(foldedPlane(1.2, 0.5, 9, 0.015, 40, 4), M.checkCloth, 7.7, 1.15, 0.035);
  B.add(cyl(0.005, 0.005, 1.3, 6), M.chrome, 7.7, 1.41, 0.035, 0, 0, Math.PI / 2);
  geranium(B, M, 7.35, 0.88, -0.1, 0.9);
  jar(B, M, 8.0, 0.88, -0.1, 0.2, std(0x6a2a1a, 0.5));
  jar(B, M, 8.12, 0.88, -0.06, 0.15, std(0xc0702a, 0.5));
  // lamp, switch, socket
  ctx.kitchenLamp = domeLamp(scene, M, 7.7, 2.5, 1.45, 0.17);
  ctx.switchKitchen = lightSwitch(scene, M, 7.78, 1.5, 2.89, Math.PI);
  socket(B, M, 8.795, 1.1, 1.38, -Math.PI / 2);
  // bag of bags on the door handle side
  const bag = std(0xd9d2c0, 0.6);
  B.add(new THREE.SphereGeometry(0.12, 12, 10), bag, 6.66, 1.2, 2.6, 0, 0, 0);
}

export function buildKitchen(ctx) {
  stove(ctx);
  kettle(ctx);
  fridge(ctx);
  sinkAndCounter(ctx);
  gasPipe(ctx);
  table(ctx);
  wallStuff(ctx);
}
