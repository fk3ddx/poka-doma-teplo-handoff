import * as THREE from 'three';
import {boxGeo, rboxGeo, cyl, lathe, tube, rnd, srand} from '../geo.js';
import {std} from '../materials.js';
import * as T from '../textures.js';
import {model} from '../models.js';
import {radiator, curtains, books, frame, pendulumClock, chandelier, floorLamp, geranium, jar, crystalVase, wineGlass, cup, socket, lightSwitch, foldedPlane} from './common.js';

// Cabinet door/drawer front with slight gap & knob
function doorPanel(B, mat, x, y, z, w, h, knob, knobMat, knobSide = 1) {
  B.add(rboxGeo(w - 0.006, h - 0.006, 0.018, 0.004), mat, x, y, z + 0.009);
  // raised molding
  B.add(rboxGeo(w - 0.08, h - 0.08, 0.006, 0.003), mat, x, y, z + 0.021);
  if (knob) B.add(rboxGeo(0.014, 0.08, 0.022, 0.006), knobMat, x + knobSide * (w / 2 - 0.05), y, z + 0.03);
}

function wallUnit(ctx) {
  const {B, M, scene} = ctx;
  const wood = M.cherry, inner = std(0x3a2016, 0.8, 0, {envMapIntensity: 0.2});
  const D = 0.45, Hh = 2.2;
  B.push(6.5 - D / 2, 0, 1.8, -Math.PI / 2); // local +z = room (west), local x = +z world
  const mods = [[-1.5, -0.6, 'glass'], [-0.6, 0.6, 'tv'], [0.6, 1.5, 'wardrobe']];
  for (const [a, b, type] of mods) {
    const w = b - a, cx = (a + b) / 2;
    // carcass sides, top, bottom, back
    B.add(rboxGeo(0.02, Hh, D, 0.004), wood, a + 0.01, Hh / 2, 0);
    B.add(rboxGeo(0.02, Hh, D, 0.004), wood, b - 0.01, Hh / 2, 0);
    B.add(rboxGeo(w, 0.025, D + 0.01, 0.005), wood, cx, Hh - 0.0125, 0.005);
    B.add(boxGeo(w, 0.06, D - 0.04), std(0x2c1a12, 0.8), cx, 0.03, -0.02); // plinth
    B.add(boxGeo(w - 0.04, Hh - 0.08, 0.01), inner, cx, Hh / 2, -D / 2 + 0.005);
    if (type === 'glass') {
      doorPanel(B, wood, cx - w / 4, 0.4, D / 2 - 0.02, w / 2, 0.68, true, M.brass, 1);
      doorPanel(B, wood, cx + w / 4, 0.4, D / 2 - 0.02, w / 2, 0.68, true, M.brass, -1);
      B.add(boxGeo(w - 0.04, 0.02, D), wood, cx, 0.76, 0);
      // mirrored back + glass shelves
      B.add(new THREE.PlaneGeometry(w - 0.05, 0.95), std(0x4a4440, 0.12, 1, {envMapIntensity: 0.35}), cx, 1.27, -D / 2 + 0.012);
      for (const y of [1.08, 1.42]) B.add(boxGeo(w - 0.05, 0.006, D - 0.08), M.glassThick, cx, y, -0.02, 0, 0, 0, {cast: false});
      // contents: crystal, glasses, postcards
      crystalVase(B, M, cx - 0.22, 0.77, -0.05, 0.9);
      for (let i = 0; i < 5; i++) wineGlass(B, M, cx - 0.05 + i * 0.07, 1.086, -0.04);
      for (let i = 0; i < 4; i++) wineGlass(B, M, cx - 0.12 + i * 0.07, 1.426, 0.04);
      B.add(boxGeo(w - 0.04, 0.02, D), wood, cx, 1.76, 0);
      // sliding glass
      B.add(new THREE.PlaneGeometry(w / 2 + 0.02, 0.96), M.glassThick, cx - w / 4 + 0.01, 1.28, D / 2 - 0.02, 0, 0, 0, {cast: false});
      B.add(new THREE.PlaneGeometry(w / 2 + 0.02, 0.96), M.glassThick, cx + w / 4 - 0.01, 1.28, D / 2 - 0.035, 0, 0, 0, {cast: false});
      doorPanel(B, wood, cx, 1.98, D / 2 - 0.02, w - 0.02, 0.42, true, M.brass, 0);
      ctx.placeModel('tea_set_01', {width: 0.62}, cx + 0.05, 0.775, 0.0, 0, B);
      ctx.placeModel('postcard_set_01', {height: 0.22}, cx + 0.25, 1.43, -0.1, 0, B);
    } else if (type === 'tv') {
      doorPanel(B, wood, cx - w / 4, 0.3, D / 2 - 0.02, w / 2, 0.48, true, M.brass, 1);
      doorPanel(B, wood, cx + w / 4, 0.3, D / 2 - 0.02, w / 2, 0.48, true, M.brass, -1);
      B.add(boxGeo(w - 0.04, 0.025, D), wood, cx, 0.55, 0);
      B.add(boxGeo(w - 0.04, 0.02, D - 0.05), wood, cx, 1.34, -0.025);
      books(B, cx - 0.55, cx + 0.15, 1.35, -0.2, 0.2, 0, {maxH: 0.27});
      ctx.placeModel('mantel_clock_01', {height: 0.22}, cx + 0.38, 1.35, -0.06, 0, B);
      B.add(boxGeo(w - 0.04, 0.02, D - 0.05), wood, cx, 1.68, -0.025);
      books(B, cx - 0.5, cx + 0.5, 1.69, -0.2, 0.2, 0, {maxH: 0.24});
      doorPanel(B, wood, cx - w / 4, 1.96, D / 2 - 0.02, w / 2, 0.46, true, M.brass, 1);
      doorPanel(B, wood, cx + w / 4, 1.96, D / 2 - 0.02, w / 2, 0.46, true, M.brass, -1);
      // lace napkin where the TV sits
      B.add(new THREE.CircleGeometry(0.3, 24), M.lace, cx, 0.564, 0.02, 0, -Math.PI / 2, 0, {cast: false});
    } else {
      doorPanel(B, wood, cx - w / 4, 1.05, D / 2 - 0.02, w / 2, 1.95, true, M.brass, 1);
      doorPanel(B, wood, cx + w / 4, 1.05, D / 2 - 0.02, w / 2, 1.95, true, M.brass, -1);
      // key in the lock
      B.add(rboxGeo(0.01, 0.03, 0.03, 0.003), M.brass, cx - 0.03, 1.1, D / 2 + 0.02);
    }
  }
  B.pop();
  ctx.col.addBox(6.05, 0.3, 6.5, 3.3, {y1: 2.2});
}

function sofa(ctx) {
  const {B, M} = ctx;
  const up = M.wool, wood = M.walnut;
  // diván-knizhka along west wall, facing +x
  B.push(2.9 + 0.43, 0, 2.15, Math.PI / 2);
  const L = 1.9;
  B.add(rboxGeo(L - 0.12, 0.22, 0.78, 0.03), std(0x3b2a20, 0.8), 0, 0.2, 0); // base box
  B.add(rboxGeo(L - 0.16, 0.18, 0.66, 0.07), up, 0, 0.4, 0.05);   // seat
  B.add(rboxGeo(L - 0.16, 0.48, 0.2, 0.08), up, 0, 0.66, -0.3, 0, -0.12, 0); // back
  for (const s of [-1, 1]) {
    B.add(rboxGeo(0.07, 0.58, 0.82, 0.025), wood, s * (L / 2 - 0.035), 0.37, 0);
    B.add(rboxGeo(0.1, 0.035, 0.84, 0.015), wood, s * (L / 2 - 0.035), 0.67, 0.01);
    for (const z of [-0.33, 0.33]) B.add(cyl(0.018, 0.012, 0.09, 10), wood, s * (L / 2 - 0.05), 0.045, z);
  }
  // cushions + plaid folded on one end
  B.add(rboxGeo(0.42, 0.38, 0.12, 0.06), M.jacquard, -0.62, 0.68, -0.16, 0.25, -0.3, 0.1);
  B.add(rboxGeo(0.4, 0.36, 0.12, 0.06), M.fabricRed, 0.6, 0.66, -0.15, -0.2, -0.3, -0.08);
  B.add(rboxGeo(0.5, 0.08, 0.55, 0.03), M.plaid, 0.55, 0.52, 0.12, 0.1);
  B.pop();
  ctx.col.addBox(2.9, 1.2, 3.78, 3.1, {y1: 0.75});
  // carpet on the wall
  const ct = T.carpet('wall');
  const cm = new THREE.MeshStandardMaterial({map: ct.map, normalMap: ct.normalMap, roughness: 1, normalScale: new THREE.Vector2(0.7, 0.7)});
  ct.normalMap.repeat.set(4, 4); ct.normalMap.wrapS = ct.normalMap.wrapT = THREE.RepeatWrapping;
  B.add(rboxGeo(0.012, 1.45, 2.05, 0.004), cm, 2.907, 1.52, 2.15);
  // the carpet hangs on a thin rod
  B.add(cyl(0.006, 0.006, 2.1, 6), M.brass, 2.92, 2.255, 2.15, 0, Math.PI / 2);
}

function armchair(ctx, x, z, ry) {
  const {B, M} = ctx;
  B.push(x, 0, z, ry);
  const wood = M.walnut, up = M.jacquard;
  for (const [lx, lz] of [[-0.3, -0.28], [0.3, -0.28], [-0.3, 0.28], [0.3, 0.28]]) {
    B.add(cyl(0.022, 0.014, 0.24, 10), wood, lx, 0.11, lz, 0, lz < 0 ? 0.12 : -0.12, lx < 0 ? -0.12 : 0.12);
  }
  B.add(rboxGeo(0.66, 0.08, 0.66, 0.02), wood, 0, 0.25, 0);
  B.add(rboxGeo(0.54, 0.14, 0.58, 0.06), up, 0, 0.35, 0.03);
  B.add(rboxGeo(0.56, 0.56, 0.14, 0.07), up, 0, 0.66, -0.27, 0, -0.2, 0);
  for (const s of [-1, 1]) {
    B.add(rboxGeo(0.07, 0.035, 0.62, 0.015), wood, s * 0.33, 0.56, 0.02);
    B.add(rboxGeo(0.04, 0.3, 0.04, 0.01), wood, s * 0.33, 0.41, 0.26);
    B.add(rboxGeo(0.04, 0.3, 0.04, 0.01), wood, s * 0.33, 0.41, -0.24);
  }
  // plaid hanging over the back
  const pg = foldedPlane(0.5, 0.7, 4, 0.02, 20, 10);
  B.add(pg, M.plaid, 0.02, 0.78, -0.36, 0, -0.25, 0);
  B.pop();
  ctx.col.addBoxC(x, z, 0.72, 0.72, {y1: 0.6});
}

function coffeeTable(ctx, x, z) {
  const {B, M} = ctx;
  const wood = M.walnut;
  B.add(rboxGeo(0.5, 0.03, 0.9, 0.008), wood, x, 0.5, z);
  B.add(rboxGeo(0.44, 0.015, 0.84, 0.004), wood, x, 0.2, z);
  for (const [dx, dz] of [[-0.2, -0.38], [0.2, -0.38], [-0.2, 0.38], [0.2, 0.38]]) B.add(cyl(0.018, 0.012, 0.5, 10), wood, x + dx, 0.25, z + dz, 0, dz < 0 ? 0.06 : -0.06, dx < 0 ? -0.06 : 0.06);
  // newspaper, TV guide
  const np = new THREE.MeshStandardMaterial({map: T.label('ТЕЛЕНЕДЕЛЯ', {w: 256, h: 340, bg: '#ddd6c4', fg: '#2a2a2a', font: 'bold 30px PT Serif, serif'}), roughness: 0.95});
  B.add(boxGeo(0.21, 0.004, 0.29), np, x - 0.05, 0.517, z + 0.18, 0.3);
  B.add(boxGeo(0.3, 0.006, 0.4), M.paper, x + 0.04, 0.52, z - 0.15, -0.2);
  ctx.placeModel('jug_01', {height: 0.2}, x + 0.12, 0.515, z + 0.3, 0.5, B);
  // lower shelf: magazines
  B.add(boxGeo(0.21, 0.02, 0.29), std(0x8a3a2a, 0.8), x, 0.215, z, 0.1);
  ctx.col.addBoxC(x, z, 0.52, 0.92, {y1: 0.52});
}

function foldingTable(ctx) {
  const {B, M} = ctx;
  const x = 3.75, z = 4.18;
  B.add(rboxGeo(1.0, 0.025, 0.42, 0.006), M.walnut, x, 0.75, z);
  B.add(rboxGeo(0.96, 0.7, 0.38, 0.006), M.walnut, x, 0.37, z);
  B.add(boxGeo(0.94, 0.004, 0.25), M.lace, x, 0.765, z - 0.02, 0, 0, 0, {cast: false});
  crystalVase(B, M, x - 0.25, 0.765, z - 0.02, 1.1);
  // dried flowers in vase
  srand(4);
  for (let i = 0; i < 9; i++) {
    const a = rnd() * 6.28;
    const top = [x - 0.25 + Math.cos(a) * 0.1, 1.15 + rnd() * 0.15, z - 0.02 + Math.sin(a) * 0.06];
    B.add(tube([[x - 0.25, 0.8, z - 0.02], [(x - 0.25 + top[0]) / 2, 0.98, (z - 0.02 + top[2]) / 2], top], 0.003, 6, 4), std(0x6a6a3a, 0.9), 0, 0, 0);
    B.add(new THREE.SphereGeometry(0.02, 6, 5), std(rnd() > 0.5 ? 0xb8984a : 0x9a4a3a, 0.9), ...top);
  }
  // standing photo frames and a sewing box
  ctx.placeModel('wicker_basket_01', {width: 0.3}, x + 0.28, 0.765, z, 0.1, B);
  ctx.col.addBox(x - 0.5, z - 0.22, x + 0.5, 4.4, {y1: 0.78});
}

export function buildLiving(ctx) {
  const {B, M, scene} = ctx;
  wallUnit(ctx);
  sofa(ctx);
  armchair(ctx, 3.45, 0.85, Math.atan2(2.65, 0.95));
  coffeeTable(ctx, 4.45, 2.15);
  foldingTable(ctx);
  radiator(B, M, 3.95, 0.09, 9, 0);
  // rug
  const rt = T.carpet('floor');
  const rm = new THREE.MeshStandardMaterial({map: rt.map, normalMap: rt.normalMap, roughness: 1});
  rt.normalMap.repeat.set(5, 5); rt.normalMap.wrapS = rt.normalMap.wrapT = THREE.RepeatWrapping;
  B.add(rboxGeo(1.9, 0.008, 2.8, 0.003), rm, 4.6, 0.004, 2.2, 0, 0, 0, {cast: false});
  // window-sill plants
  ctx.placeModel('potted_plant_04', {height: 0.22}, 3.55, 0.88, -0.1, 0.4, B);
  geranium(B, M, 4.0, 0.88, -0.1, 1);
  jar(B, M, 4.35, 0.88, -0.08, 0.15, std(0x3a7a3a, 0.4, 0, {transparent: true, opacity: 0.8}));
  // floor lamp in the corner
  const fl = floorLamp(B, scene, M, 3.12, 0.35);
  ctx.livingLamp = fl;
  // curtains over window + balcony door
  ctx.livingTulle = curtains(B, M, scene, {a0: 3.15, a1: 4.72, z: 0, heavy: false, tulle0: 3.0, tulle1: 4.75});
  for (const [cx, w] of [[3.2, 0.5], [5.8, 0.55]]) {
    B.add(foldedPlane(w, 2.4, 5, 0.05, 40, 8), M.jacquardCurtain, cx, 1.2, 0.2);
  }
  B.add(rboxGeo(3.2, 0.05, 0.06, 0.01), M.walnut, 4.5, 2.44, 0.09);
  // wall clock + photos on the south wall (facing -z)
  ctx.livingClock = pendulumClock(scene, M, 4.75, 1.65, 4.4, Math.PI);
  frame(B, M, 3.45, 1.62, 4.4, Math.PI, 0.26, 0.34, 0);
  frame(B, M, 3.85, 1.7, 4.4, Math.PI, 0.22, 0.28, 1);
  frame(B, M, 3.65, 1.25, 4.4, Math.PI, 0.3, 0.22, 2, M.cherry);
  // chandelier
  ctx.chandelier = chandelier(scene, M, 4.7, 2.5, 2.2);
  // sockets & switch
  socket(B, M, 6.05, 0.3, 3.5, -Math.PI / 2);
  socket(B, M, 2.9, 0.3, 0.8, Math.PI / 2);
  ctx.switchLiving = lightSwitch(scene, M, 5.12, 1.5, 4.39, Math.PI);
}
