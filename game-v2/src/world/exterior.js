import * as THREE from 'three';
import {Batcher, Builder, boxGeo, rboxGeo, cyl, planeGeo, tube, rnd, rr, srand, mergeGeometries} from './geo.js';
import {std} from './materials.js';
import * as T from './textures.js';
import {GROUND_Y, FLOOR_H, EXT_T, openings, balcony} from './layout.js';

// ---- window texture: frame cross + curtains, used for lit/dark windows
function windowAtlas() {
  const W = 512, H = 128; const [c, g] = T.canvas(W, H);
  // 4 variants side by side: tulle, curtains, blinds-ish, bare
  for (let v = 0; v < 4; v++) {
    const x0 = v * 128;
    g.fillStyle = '#ffffff'; g.fillRect(x0, 0, 128, 128);
    // curtains
    if (v === 0 || v === 1) {
      const grd = g.createLinearGradient(x0, 0, x0 + 128, 0);
      grd.addColorStop(0, 'rgba(120,60,30,0.85)'); grd.addColorStop(0.22, 'rgba(160,90,40,0.4)'); grd.addColorStop(0.35, 'rgba(255,255,255,0)');
      grd.addColorStop(0.65, 'rgba(255,255,255,0)'); grd.addColorStop(0.78, 'rgba(160,90,40,0.4)'); grd.addColorStop(1, 'rgba(120,60,30,0.85)');
      g.fillStyle = grd; g.fillRect(x0, 0, 128, 128);
    }
    if (v === 0) { for (let i = 0; i < 128; i += 6) { g.fillStyle = 'rgba(255,255,255,0.25)'; g.fillRect(x0 + i, 0, 2, 128); } }
    if (v === 2) { for (let y = 0; y < 128; y += 8) { g.fillStyle = 'rgba(80,60,40,0.3)'; g.fillRect(x0, y, 128, 3); } }
    // lamp hotspot
    const r = g.createRadialGradient(x0 + 64 + (v - 1.5) * 8, 30, 4, x0 + 64, 40, 90);
    r.addColorStop(0, 'rgba(255,255,255,0)'); r.addColorStop(1, 'rgba(0,0,0,0.45)');
    g.fillStyle = r; g.fillRect(x0, 0, 128, 128);
    // frame
    g.fillStyle = '#2a2a2a';
    g.fillRect(x0, 0, 128, 6); g.fillRect(x0, 122, 128, 6); g.fillRect(x0, 0, 6, 128); g.fillRect(x0 + 122, 0, 6, 128);
    g.fillRect(x0 + 60, 0, 8, 128); g.fillRect(x0 + 68, 40, 54, 5);
  }
  return T.tex(c, {repeat: false});
}

// Panel building block with instanced windows. Returns API for window lights.
function panelBuilding(scene, B, M, {x0, x1, z, facing = 1, floors = 5, groundY = GROUND_Y, depth = 12, winW = 1.4, winH = 1.45, pitch = 3.0, seed = 1, litChance = 0.45, mat}) {
  const len = x1 - x0, H = floors * FLOOR_H + 0.9 + 0.6;
  const cz = z - facing * depth / 2;
  B.add(boxGeo(len, H, depth, 3.6), mat, (x0 + x1) / 2, groundY + H / 2, cz, 0, 0, 0, {cast: false});
  // roof parapet & plinth
  B.add(boxGeo(len + 0.1, 0.3, depth + 0.1), std(0x3a3a38, 0.9), (x0 + x1) / 2, groundY + H + 0.15, cz, 0, 0, 0, {cast: false});
  B.add(boxGeo(len + 0.02, 0.9, depth + 0.02), std(0x4a4440, 0.95), (x0 + x1) / 2, groundY + 0.45, cz, 0, 0, 0, {cast: false});
  // windows
  const cols = Math.floor(len / pitch);
  const count = cols * floors;
  const geo = new THREE.PlaneGeometry(winW, winH);
  const atlas = windowAtlas();
  const wm = new THREE.MeshBasicMaterial({map: atlas, toneMapped: true, fog: true});
  wm.onBeforeCompile = sh => {
    sh.vertexShader = sh.vertexShader.replace('#include <uv_vertex>', '#include <uv_vertex>\nvMapUv = vec2((uv.x + aVariant) * 0.25, uv.y);').replace('void main() {', 'attribute float aVariant;\nvoid main() {');
  };
  const mesh = new THREE.InstancedMesh(geo, wm, count);
  const variant = new Float32Array(count);
  const info = [];
  srand(seed);
  let i = 0;
  const m4 = new THREE.Matrix4();
  for (let f = 0; f < floors; f++) for (let cI = 0; cI < cols; cI++) {
    const wx = x0 + pitch / 2 + cI * pitch + (len - cols * pitch) / 2;
    const wy = groundY + 0.9 + f * FLOOR_H + 0.85 + winH / 2;
    m4.compose(new THREE.Vector3(wx, wy, z + facing * 0.02), new THREE.Quaternion().setFromEuler(new THREE.Euler(0, facing > 0 ? 0 : Math.PI, 0)), new THREE.Vector3(1, 1, 1));
    mesh.setMatrixAt(i, m4);
    variant[i] = Math.floor(rnd() * 4);
    const warm = rnd() > 0.25;
    const base = warm ? new THREE.Color().setHSL(0.08 + rnd() * 0.04, 0.6 + rnd() * 0.3, 0.45 + rnd() * 0.15) : new THREE.Color().setHSL(0.55 + rnd() * 0.1, 0.25, 0.4 + rnd() * 0.15);
    const lit = rnd() < litChance;
    info.push({x: wx, y: wy, floor: f, col: cI, base, lit, tv: !warm && rnd() > 0.5});
    i++;
  }
  geo.setAttribute('aVariant', new THREE.InstancedBufferAttribute(variant, 1));
  const dark = new THREE.Color(0x0b0d10);
  const api = {
    mesh, info,
    set(idx, on, k = 1) { const w = info[idx]; w.lit = on; mesh.setColorAt(idx, on ? w.base.clone().multiplyScalar(k * 1.6) : dark); mesh.instanceColor.needsUpdate = true; },
    find(floor, col) { return info.findIndex(w => w.floor === floor && w.col === col); },
    refresh() { info.forEach((w, k) => mesh.setColorAt(k, w.lit ? w.base.clone().multiplyScalar(1.6) : dark)); mesh.instanceColor.needsUpdate = true; },
  };
  api.refresh();
  scene.add(mesh);
  // balconies on some columns
  for (let cI = 1; cI < cols; cI += 3) for (let f = 1; f < floors; f++) {
    const wx = x0 + pitch / 2 + cI * pitch + (len - cols * pitch) / 2 + pitch / 2;
    const wy = groundY + 0.9 + f * FLOOR_H;
    B.add(boxGeo(2.6, 0.12, 1.0), std(0x4a4a46, 0.9), wx, wy, z + facing * 0.5, 0, 0, 0, {cast: false});
    B.add(boxGeo(2.6, 1.0, 0.04), std(0x5c5a52, 0.9), wx, wy + 0.5, z + facing * 1.0, 0, 0, 0, {cast: false});
  }
  return api;
}

// Bare deciduous tree (poplar/birch) from recursive branches, merged geometry
function treeGeo(seed, height = 9, birch = false) {
  srand(seed);
  const parts = [];
  const branch = (p, dir, len, r, depth) => {
    const end = p.clone().addScaledVector(dir, len);
    const g = new THREE.CylinderGeometry(r * 0.7, r, len, depth > 2 ? 5 : 3, 1, true);
    g.translate(0, len / 2, 0);
    const q = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir);
    g.applyQuaternion(q); g.translate(p.x, p.y, p.z);
    parts.push(g);
    if (depth <= 0 || r < 0.012) return;
    const n = depth > 2 ? 3 : 2 + Math.floor(rnd() * 2);
    for (let i = 0; i < n; i++) {
      const nd = dir.clone().add(new THREE.Vector3(rr(-0.9, 0.9), rr(0.1, 0.8), rr(-0.9, 0.9))).normalize();
      if (birch) nd.y = Math.max(nd.y, depth < 2 ? -0.6 : 0.2);
      branch(end, nd.normalize(), len * rr(0.55, 0.78), r * 0.6, depth - 1);
    }
    if (depth > 2) branch(end, dir.clone().add(new THREE.Vector3(rr(-0.2, 0.2), 0.9, rr(-0.2, 0.2))).normalize(), len * 0.8, r * 0.75, depth - 1);
  };
  branch(new THREE.Vector3(0, 0, 0), new THREE.Vector3(rr(-0.05, 0.05), 1, rr(-0.05, 0.05)).normalize(), height * 0.32, 0.18, 5);
  return mergeGeometries(parts.map(g => g.toNonIndexed()));
}

function sodiumLamp(B, scene, M, x, z, gy, glowTex, ry = 0) {
  const pole = std(0x6a6a64, 0.9);
  B.add(cyl(0.08, 0.12, 7.5, 8), pole, x, gy + 3.75, z);
  B.add(tube([[x, gy + 7.4, z], [x + Math.sin(ry) * 0.6, gy + 7.9, z + Math.cos(ry) * 0.6], [x + Math.sin(ry) * 1.4, gy + 7.95, z + Math.cos(ry) * 1.4]], 0.05, 10, 6), pole, 0, 0, 0);
  const hx = x + Math.sin(ry) * 1.5, hz = z + Math.cos(ry) * 1.5;
  B.add(rboxGeo(0.5, 0.14, 0.25, 0.05), std(0x2a2a28, 0.6, 0.4), hx, gy + 7.9, hz, ry);
  const lampMat = new THREE.MeshBasicMaterial({color: 0xffa040});
  const lens = new THREE.Mesh(new THREE.PlaneGeometry(0.4, 0.18), lampMat); lens.rotation.x = Math.PI / 2; lens.position.set(hx, gy + 7.82, hz); scene.add(lens);
  const halo = new THREE.Sprite(new THREE.SpriteMaterial({map: glowTex, color: 0xff9a40, transparent: true, opacity: 0.55, depthWrite: false, blending: THREE.AdditiveBlending, fog: false}));
  halo.scale.set(5, 5, 1); halo.position.set(hx, gy + 7.7, hz); scene.add(halo);
  const l = new THREE.SpotLight(0xff9a40, 260, 26, 0.95, 0.7, 1.6);
  l.position.set(hx, gy + 7.7, hz); l.target.position.set(hx, gy, hz); scene.add(l, l.target);
  return {lens, halo, light: l, mat: lampMat, base: l.intensity};
}

function glowTexture() {
  const [c, g] = T.canvas(128);
  const r = g.createRadialGradient(64, 64, 0, 64, 64, 64);
  r.addColorStop(0, 'rgba(255,255,255,1)'); r.addColorStop(0.15, 'rgba(255,255,255,0.5)'); r.addColorStop(0.5, 'rgba(255,255,255,0.08)'); r.addColorStop(1, 'rgba(255,255,255,0)');
  g.fillStyle = r; g.fillRect(0, 0, 128, 128);
  return T.tex(c, {repeat: false});
}

// Wet sleet: falling streaks around the facade
function sleet(scene) {
  const N = 2600;
  const pos = new Float32Array(N * 3), seed = new Float32Array(N);
  for (let i = 0; i < N; i++) { pos[i * 3] = rr(-8, 20); pos[i * 3 + 1] = rr(-12, 6); pos[i * 3 + 2] = rr(-22, -0.6); seed[i] = rnd(); }
  const g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.BufferAttribute(pos, 3));
  g.setAttribute('aSeed', new THREE.BufferAttribute(seed, 1));
  const mat = new THREE.ShaderMaterial({
    transparent: true, depthWrite: false, fog: true,
    uniforms: {uTime: {value: 0}, uWind: {value: 1}, ...THREE.UniformsLib.fog},
    vertexShader: `attribute float aSeed; uniform float uTime, uWind; varying float vA;
      #include <fog_pars_vertex>
      void main(){ vec3 p = position; float t = uTime*(2.2+aSeed*1.6);
        p.y = mod(p.y - t + 12.0, 18.0) - 12.0; p.x += sin(uTime*0.7 + aSeed*20.0)*0.4*uWind + (p.y)*0.12*uWind;
        vec4 mvPosition = modelViewMatrix * vec4(p,1.0); vec4 mv = mvPosition; gl_Position = projectionMatrix * mv;
        gl_PointSize = clamp(26.0 / -mv.z, 1.0, 3.0); vA = clamp(1.2 - (-mv.z)/28.0, 0.0, 1.0);
        #include <fog_vertex>
      }`,
    fragmentShader: `varying float vA;
      #include <fog_pars_fragment>
      void main(){ vec2 d = gl_PointCoord - 0.5; float a = smoothstep(0.5, 0.15, length(d*vec2(1.0,0.5))) * vA * 0.38; gl_FragColor = vec4(vec3(0.75,0.78,0.82), a);
        #include <fog_fragment>
      }`,
  });
  const pts = new THREE.Points(g, mat);
  pts.frustumCulled = false;
  scene.add(pts);
  return mat;
}

function skyDome(scene) {
  const g = new THREE.SphereGeometry(300, 24, 12);
  const m = new THREE.ShaderMaterial({
    side: THREE.BackSide, depthWrite: false, fog: false,
    uniforms: {uDim: {value: 1}},
    vertexShader: `varying vec3 vP; void main(){ vP = normalize(position); gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.0); }`,
    fragmentShader: `varying vec3 vP; uniform float uDim; void main(){ float h = vP.y;
      vec3 top = vec3(0.05,0.052,0.06); vec3 hor = vec3(0.2,0.15,0.115);
      vec3 c = mix(hor, top, smoothstep(-0.05, 0.35, h));
      c = mix(c, vec3(0.1,0.11,0.12), smoothstep(0.0, -0.2, h));
      gl_FragColor = vec4(c*uDim, 1.0); }`,
  });
  const s = new THREE.Mesh(g, m); s.renderOrder = -10; scene.add(s);
  return m;
}

export function buildExterior(ctx) {
  const {scene, M, col} = ctx;
  const batch = new Batcher();
  const B = new Builder(batch);
  const gy = GROUND_Y;
  const glow = glowTexture();

  // ground
  B.add(planeGeo(140, 120, 4), M.asphalt, 10, gy, -40, 0, -Math.PI / 2, 0, {cast: false});
  srand(5);
  for (const [x, z, w, d] of [[-6, -22, 14, 10], [14, -24, 18, 12], [30, -20, 10, 14], [-14, -32, 10, 6], [8, -36, 26, 3]]) {
    B.add(planeGeo(w, d, 3), M.mud, x, gy + 0.02, z, 0, -Math.PI / 2, 0, {cast: false});
    // curbs
    B.add(boxGeo(w, 0.15, 0.12), std(0x6a6a66, 0.9), x, gy + 0.075, z - d / 2);
    B.add(boxGeo(w, 0.15, 0.12), std(0x6a6a66, 0.9), x, gy + 0.075, z + d / 2);
  }
  // puddles (glossy)
  const puddle = new THREE.MeshStandardMaterial({color: 0x0a0c0e, roughness: 0.03, metalness: 0.6});
  for (const [x, z, s] of [[2, -8, 2.2], [9, -9, 1.4], [16, -7.5, 3], [-3, -9.5, 1.6], [24, -12, 2.4]]) {
    const pg = new THREE.CircleGeometry(s, 20); pg.scale(1, 0.55, 1);
    B.add(pg, puddle, x, gy + 0.025, z, rnd(), -Math.PI / 2, 0, {cast: false});
  }

  // our building facade (outer face z = -EXT_T), our floor has real holes
  const facadeMat = M.concretePanels;
  const fz = -EXT_T;
  const topY = FLOOR_H + 0.4;
  const bx0 = -30, bx1 = 40;
  B.add(planeGeo(bx1 - bx0, -0.0 - gy + 0, 3.6), facadeMat, (bx0 + bx1) / 2, gy / 2, fz, Math.PI, 0, 0, {cast: false});
  B.add(planeGeo(bx1 - bx0, topY - 2.7 + 0.01, 3.6), facadeMat, (bx0 + bx1) / 2, 2.7 + (topY - 2.7) / 2 - 0.005, fz, Math.PI, 0, 0, {cast: false});
  // our floor band with holes
  const holes = openings.filter(o => o.axis === 'z' && o.c0 === -EXT_T).sort((a, b) => a.a0 - b.a0);
  let a = bx0;
  for (const o of holes) {
    B.add(planeGeo(o.a0 - a, 2.7, 3.6), facadeMat, (a + o.a0) / 2, 1.35, fz, Math.PI, 0, 0, {cast: false});
    if (o.y0 > 0) B.add(planeGeo(o.a1 - o.a0, o.y0, 3.6), facadeMat, (o.a0 + o.a1) / 2, o.y0 / 2, fz, Math.PI, 0, 0, {cast: false});
    B.add(planeGeo(o.a1 - o.a0, 2.7 - o.y1, 3.6), facadeMat, (o.a0 + o.a1) / 2, (o.y1 + 2.7) / 2, fz, Math.PI, 0, 0, {cast: false});
    a = o.a1;
  }
  B.add(planeGeo(bx1 - a, 2.7, 3.6), facadeMat, (a + bx1) / 2, 1.35, fz, Math.PI, 0, 0, {cast: false});
  B.add(boxGeo(bx1 - bx0, 0.3, 1.0), std(0x3a3a38, 0.9), (bx0 + bx1) / 2, topY + 0.15, fz - 0.3, 0, 0, 0, {cast: false});
  // other windows on our facade (neighbours + floors below)
  const ourWin = new THREE.Group();
  {
    const atlas = windowAtlas();
    const geo = new THREE.PlaneGeometry(1.3, 1.45);
    const list = [];
    srand(91);
    for (let f = 0; f < 5; f++) for (let k = -3; k <= 4; k++) {
      for (const o of [{c: 1.4}, {c: 3.95}, {c: 7.7}]) {
        const x = o.c + k * 9.1;
        if (f === 4 && k === 0) continue;
        list.push([x, gy + 0.9 + f * FLOOR_H + 0.85 + 0.72]);
      }
    }
    const wm = new THREE.MeshBasicMaterial({map: atlas});
    wm.onBeforeCompile = sh => { sh.vertexShader = sh.vertexShader.replace('#include <uv_vertex>', '#include <uv_vertex>\nvMapUv = vec2((uv.x + floor(mod(float(gl_InstanceID)*7.31, 4.0))) * 0.25, uv.y);'); };
    const im = new THREE.InstancedMesh(geo, wm, list.length);
    const m4 = new THREE.Matrix4(), q = new THREE.Quaternion().setFromEuler(new THREE.Euler(0, Math.PI, 0));
    list.forEach(([x, y], i) => { m4.compose(new THREE.Vector3(x, y, fz - 0.01), q, new THREE.Vector3(1, 1, 1)); im.setMatrixAt(i, m4); im.setColorAt(i, rnd() < 0.4 ? new THREE.Color().setHSL(0.08, 0.6, 0.5) : new THREE.Color(0x0a0b0d)); });
    scene.add(im);
    ctx.ourFacadeWindows = im;
  }
  // balcony stack below ours
  for (let f = 0; f < 5; f++) {
    const y = f === 4 ? 0 : gy + 0.9 + f * FLOOR_H;
    B.add(boxGeo(3.6, 0.14, 1.05), std(0x55534e, 0.9), (balcony.x0 + balcony.x1) / 2, y - 0.07, (balcony.z0 + balcony.z1) / 2 - 0.02, 0, 0, 0, {cast: f !== 4});
    if (f !== 4) B.add(boxGeo(3.6, 1.0, 0.04), std(0x6a675e, 0.9), (balcony.x0 + balcony.x1) / 2, y + 0.5, balcony.z0 - 0.02, 0, 0, 0, {cast: false});
  }
  buildBalcony(ctx, B);

  // opposite building (facade at z=-42 facing +z)
  const opp = panelBuilding(scene, B, M, {x0: -32, x1: 52, z: -42, facing: 1, seed: 3, litChance: 0.5, mat: M.concretePanels});
  ctx.opposite = opp;
  // Valya's window: 4th floor (index 3), column opposite our kitchen
  const valCol = opp.info.filter(w => w.floor === 3).reduce((best, w) => Math.abs(w.x - 7.7) < Math.abs(best.x - 7.7) ? w : best);
  ctx.valyaWindow = opp.info.indexOf(valCol);
  opp.info[ctx.valyaWindow].base = new THREE.Color(0xffb060);
  opp.set(ctx.valyaWindow, true);
  // side building (perpendicular), left far
  panelBuilding(scene, B, M, {x0: -45, x1: -30, z: -10, facing: 1, seed: 8, litChance: 0.3, mat: M.concretePanels, depth: 40});

  // far city: 9-storey silhouettes with sparse lights
  srand(13);
  const farMat = std(0x101214, 1);
  const farLights = [];
  for (let i = 0; i < 9; i++) {
    const x = -60 + i * 22 + rr(-5, 5), z = -100 - rr(0, 60), w = rr(14, 30), h = rr(22, 30);
    B.add(boxGeo(w, h, 12), farMat, x, gy + h / 2, z, 0, 0, 0, {cast: false});
    for (let k = 0; k < 30; k++) if (rnd() < 0.35) farLights.push([x + rr(-w / 2 + 1, w / 2 - 1), gy + 2 + Math.floor(rr(0, h / 2.8)) * 2.8, z + 6.1]);
  }
  {
    const geo = new THREE.PlaneGeometry(1.1, 1.2);
    const im = new THREE.InstancedMesh(geo, new THREE.MeshBasicMaterial({color: 0xffffff}), farLights.length);
    const m4 = new THREE.Matrix4();
    farLights.forEach((p, i) => { m4.makeTranslation(...p); im.setMatrixAt(i, m4); im.setColorAt(i, new THREE.Color().setHSL(0.09, 0.7, 0.35 + rnd() * 0.2)); });
    scene.add(im); ctx.farLights = im;
  }
  // chimney with red aviation lights
  B.add(cyl(1.6, 3.2, 70, 12), std(0x1a1716, 1), 70, gy + 35, -170, 0, 0, 0, {cast: false});
  const red = [];
  for (const y of [25, 50, 69]) {
    const s = new THREE.Sprite(new THREE.SpriteMaterial({map: glow, color: 0xff2010, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, fog: false, opacity: 0.8}));
    s.scale.set(6, 6, 1); s.position.set(70, gy + y, -168); scene.add(s); red.push(s);
  }
  ctx.chimneyLights = red;

  // trees
  const bark = std(0x1c1a18, 1);
  const birchBark = std(0x8a8880, 1);
  const trees = [[-4, -15, 11, false], [3, -19, 13, false], [12, -14, 10, true], [19, -21, 12, false], [27, -16, 9, true], [-10, -26, 12, false], [34, -28, 13, false], [6, -30, 10, true], [-1, -6.5, 7, true], [15, -5.5, 8, false]];
  const tg = {a: [], b: []};
  trees.forEach(([x, z, h, birch], i) => { const g = treeGeo(100 + i, h, birch); g.translate(x, gy, z); (birch ? tg.b : tg.a).push(g); });
  if (tg.a.length) { const m = new THREE.Mesh(mergeGeometries(tg.a), bark); scene.add(m); }
  if (tg.b.length) { const m = new THREE.Mesh(mergeGeometries(tg.b), birchBark); scene.add(m); }

  // playground: swing, slide, sandbox; carpet beater; benches
  const metal = std(0x3a4a5a, 0.6, 0.4), red2 = std(0x8a2a1a, 0.6, 0.3);
  const px = 10, pz = -23;
  for (const s of [-1, 1]) { B.add(cyl(0.05, 0.05, 2.6, 6), metal, px + s * 1.2, gy + 1.2, pz - 0.6, 0, 0.25 * s, 0); B.add(cyl(0.05, 0.05, 2.6, 6), metal, px + s * 1.2, gy + 1.2, pz + 0.6, 0, -0.25 * s, 0); }
  B.add(cyl(0.05, 0.05, 2.5, 6), metal, px, gy + 2.45, pz, 0, 0, Math.PI / 2);
  B.add(boxGeo(0.5, 0.05, 0.25), red2, px, gy + 0.5, pz);
  for (const s of [-1, 1]) B.add(cyl(0.01, 0.01, 1.95, 4), metal, px + s * 0.22, gy + 1.5, pz);
  // slide
  B.add(boxGeo(0.6, 0.05, 3.2), std(0x7a7a70, 0.3, 0.8), px + 5, gy + 1.0, pz + 1, 0, 0.55, 0);
  B.add(boxGeo(0.7, 1.9, 0.7), metal, px + 5, gy + 0.95, pz - 0.6);
  // sandbox
  B.add(boxGeo(2.4, 0.3, 2.4), std(0x5a4a32, 0.9), px - 4, gy + 0.15, pz + 2);
  // carpet beater frame (выбивалка)
  B.add(cyl(0.05, 0.05, 2.2, 6), metal, 24, gy + 1.1, -26); B.add(cyl(0.05, 0.05, 2.2, 6), metal, 27, gy + 1.1, -26);
  B.add(cyl(0.04, 0.04, 3.0, 6), metal, 25.5, gy + 2.0, -26, 0, 0, Math.PI / 2);
  // benches at our entrance (x ~ 9, z -3)
  for (const bx of [6.5, 11.5]) {
    B.add(boxGeo(1.6, 0.06, 0.4), std(0x3a2a1a, 0.9), bx, gy + 0.45, -3.2);
    B.add(boxGeo(1.6, 0.4, 0.05), std(0x3a2a1a, 0.9), bx, gy + 0.7, -3.4);
  }
  // entrance canopy (козырёк) below
  B.add(boxGeo(3.2, 0.15, 1.6), std(0x4a4844, 0.9), 9.0, gy + 2.8, -1.1);
  const entLight = new THREE.PointLight(0xffe0b0, 30, 8, 2); entLight.position.set(9, gy + 2.5, -1.2); scene.add(entLight);
  // cars along our building
  ctx.placeModel('covered_car', {width: 4.2}, 2.5, gy, -7.2, Math.PI / 2 + 0.05);
  const carPaints = [0x6a1a14, 0x2a3a4a, 0xcfc9b4];
  [[16, -7.0], [21.5, -7.4], [-5, -7.2]].forEach(([x, z], i) => car(B, x, gy, z, carPaints[i]));
  // garbage containers
  for (let i = 0; i < 3; i++) B.add(rboxGeo(1.4, 1.1, 1.0, 0.05), M.rustMetal, -12 + i * 1.6, gy + 0.55, -12);

  // street lamps
  ctx.streetLamps = [
    sodiumLamp(B, scene, M, 0, -10.5, gy, glow, Math.PI),
    sodiumLamp(B, scene, M, 18, -10.5, gy, glow, Math.PI),
    sodiumLamp(B, scene, M, 9, -33, gy, glow, 0),
    sodiumLamp(B, scene, M, 32, -33, gy, glow, 0),
  ];

  ctx.sleet = sleet(scene);
  ctx.sky = skyDome(scene);
  batch.build(scene);
}

function car(B, x, gy, z, color) {
  const paint = std(color, 0.35, 0.3);
  const glass = std(0x0a0c10, 0.1, 0.5);
  B.push(x, gy, z, Math.PI / 2);
  B.add(rboxGeo(1.62, 0.6, 4.1, 0.12), paint, 0, 0.62, 0);
  B.add(rboxGeo(1.45, 0.5, 2.1, 0.1), paint, 0, 1.12, -0.25);
  B.add(rboxGeo(1.47, 0.36, 1.95, 0.06), glass, 0, 1.13, -0.25);
  B.add(rboxGeo(1.48, 0.06, 2.15, 0.03), std(0xd8dcdc, 0.9), 0, 1.4, -0.25); // wet snow on roof
  for (const [wx, wz] of [[-0.72, 1.3], [0.72, 1.3], [-0.72, -1.3], [0.72, -1.3]]) B.add(cyl(0.3, 0.3, 0.2, 14), std(0x111111, 0.8), wx, 0.3, wz, 0, 0, Math.PI / 2);
  for (const s of [-1, 1]) B.add(cyl(0.08, 0.08, 0.04, 10), std(0x8a8a80, 0.2, 0.8), s * 0.55, 0.65, 2.06, 0, Math.PI / 2);
  B.pop();
}

function buildBalcony(ctx, B) {
  const {M, col} = ctx;
  const b = balcony;
  const cx = (b.x0 + b.x1) / 2;
  // floor slab top at y=0 already from stack; concrete floor surface
  B.add(planeGeo(b.x1 - b.x0, b.z1 - b.z0, 1.5), M.stairConcrete, cx, 0.005, (b.z0 + b.z1) / 2, 0, -Math.PI / 2, 0, {cast: false});
  // parapet: steel frame + asbestos-cement corrugated panels
  const frame = std(0x2a2a28, 0.6, 0.5);
  const panel = std(0x8a8a82, 0.95);
  const corr = new THREE.PlaneGeometry(1, 1, 40, 1);
  const cp = corr.attributes.position; for (let i = 0; i < cp.count; i++) cp.setZ(i, Math.sin(cp.getX(i) * Math.PI * 2 * 20) * 0.015);
  corr.computeVertexNormals();
  const front = corr.clone(); front.scale(b.x1 - b.x0, 0.95, 1);
  B.add(front, panel, cx, 0.5, b.z0 + 0.03);
  B.add(front, panel, cx, 0.5, b.z0 + 0.03, Math.PI);
  for (const sx of [b.x0, b.x1]) {
    const side = corr.clone(); side.scale(b.z1 - b.z0, 0.95, 1);
    B.add(side, panel, sx, 0.5, (b.z0 + b.z1) / 2, Math.PI / 2);
    B.add(side, panel, sx, 0.5, (b.z0 + b.z1) / 2, -Math.PI / 2);
  }
  B.add(boxGeo(b.x1 - b.x0 + 0.06, 0.05, 0.08), frame, cx, 1.0, b.z0 + 0.02);
  for (const sx of [b.x0, b.x1]) B.add(boxGeo(0.08, 0.05, b.z1 - b.z0), frame, sx, 1.0, (b.z0 + b.z1) / 2);
  col.addBox(b.x0 - 0.05, b.z0 - 0.05, b.x1 + 0.05, b.z0 + 0.06, {y1: 1.0});
  col.addBox(b.x0 - 0.06, b.z0, b.x0 + 0.04, b.z1, {y1: 1.0});
  col.addBox(b.x1 - 0.04, b.z0, b.x1 + 0.06, b.z1, {y1: 1.0});
  // stuff: skis, sled, jars, box of potatoes, basin, clothesline
  B.add(boxGeo(0.07, 1.7, 0.02), std(0x7a2a1a, 0.5), b.x0 + 0.12, 0.85, b.z1 - 0.12, 0, 0, 0.08);
  B.add(boxGeo(0.07, 1.7, 0.02), std(0x7a2a1a, 0.5), b.x0 + 0.2, 0.85, b.z1 - 0.12, 0, 0, 0.1);
  B.add(rboxGeo(0.5, 0.35, 0.4, 0.02), std(0x7a5a3a, 0.9), b.x1 - 0.4, 0.175, b.z0 + 0.4);
  for (let i = 0; i < 5; i++) B.add(new THREE.SphereGeometry(0.05, 6, 5), std(0x8a6a4a, 1), b.x1 - 0.5 + (i % 3) * 0.1, 0.36, b.z0 + 0.35 + Math.floor(i / 3) * 0.1);
  for (let i = 0; i < 6; i++) {
    const x = b.x0 + 0.6 + i * 0.13;
    B.add(cyl(0.05, 0.05, 0.22, 12), M.glassThick, x, 0.11, b.z1 - 0.15, 0, 0, 0, {cast: false});
    if (i % 2 === 0) B.add(cyl(0.045, 0.045, 0.15, 12), std(0x7a2a14, 0.5), x, 0.08, b.z1 - 0.15);
  }
  B.add(cyl(0.3, 0.25, 0.12, 20, true), std(0x8a9aa0, 0.3, 0.7), b.x1 - 1.2, 0.06, b.z0 + 0.35);
  // sled (санки)
  B.add(boxGeo(0.35, 0.03, 0.8), std(0x6a4a2a, 0.7), b.x0 + 0.5, 0.15, b.z0 + 0.45, 0.2);
  for (const s of [-1, 1]) B.add(tube([[0, 0.02, -0.45], [0, 0.02, 0.4], [0, 0.12, 0.48]], 0.01, 8, 4), std(0x2a5a8a, 0.4, 0.6), b.x0 + 0.5 + s * 0.15, 0, b.z0 + 0.45);
  // clothesline posts + line + towel
  for (const sx of [b.x0 + 0.05, b.x1 - 0.05]) B.add(boxGeo(0.04, 0.5, 0.04), frame, sx, 1.25, b.z0 + 0.5);
  B.add(cyl(0.003, 0.003, b.x1 - b.x0, 4), std(0xcccccc, 0.8), cx, 1.48, b.z0 + 0.5, 0, 0, Math.PI / 2);
  ctx.balconyTowelPos = new THREE.Vector3(cx + 0.6, 1.2, b.z0 + 0.5);
}
