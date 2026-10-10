import * as THREE from 'three';
import * as T from './textures.js';

const texUrls = import.meta.glob('../assets/textures/*.webp', {eager: true, query: '?url', import: 'default'});
const loader = new THREE.TextureLoader();
const cache = new Map();
let maxAniso = 8;
export function setAniso(a) { maxAniso = a; }

function load(name, srgb) {
  const key = name;
  if (cache.has(key)) return cache.get(key);
  const url = texUrls[`../assets/textures/${name}.webp`];
  if (!url) return null;
  const t = loader.load(url);
  t.wrapS = t.wrapT = THREE.RepeatWrapping;
  t.anisotropy = maxAniso;
  if (srgb) t.colorSpace = THREE.SRGBColorSpace;
  cache.set(key, t);
  return t;
}

// PBR material from a Poly Haven texture set. `size` = metres covered by one texture tile.
export function pbr(id, {size = 1, color = 0xffffff, rough = 1, roughMap = true, normal = 1, metal = 0, env = 1, rot = 0} = {}) {
  const map = load(id + '_diff', true);
  const nor = load(id + '_nor_gl', false);
  const rgh = roughMap ? load(id + '_rough', false) : null;
  const m = new THREE.MeshStandardMaterial({
    map, normalMap: nor, roughnessMap: rgh, roughness: rough, metalness: metal, color,
    normalScale: new THREE.Vector2(normal, normal), envMapIntensity: env,
  });
  for (const t of [map, nor, rgh]) if (t) { t.repeat.set(1 / size, 1 / size); t.rotation = rot; }
  return m;
}

// Clone a tiled material with a different tile size (textures are shared via clone()).
export function retile(mat, size) {
  const m = mat.clone();
  for (const k of ['map', 'normalMap', 'roughnessMap']) if (m[k]) { m[k] = m[k].clone(); m[k].repeat.set(1 / size, 1 / size); m[k].needsUpdate = true; }
  return m;
}

export function std(color, rough = 0.8, metal = 0, extra = {}) {
  return new THREE.MeshStandardMaterial({color, roughness: rough, metalness: metal, ...extra});
}

let M = null;
export function materials() {
  if (M) return M;
  const wpL = T.wallpaper('living'), wpB = T.wallpaper('bedroom'), wpH = T.wallpaper('hall');
  const wp = (w, tint = 0xffffff) => {
    const m = new THREE.MeshStandardMaterial({map: w.map, normalMap: w.normalMap, roughness: 0.92, color: tint, normalScale: new THREE.Vector2(0.6, 0.6)});
    m.map.repeat.set(...w.repeat); m.normalMap.repeat.set(...w.repeat);
    return m;
  };
  const ceil = T.plaster('#ece9e1', 4, 0.05);
  const whitewash = T.plaster('#e6e3d8', 6, 0.07);
  const tl = T.tiles(); tl.map.repeat.set(...tl.repeat); tl.normalMap.repeat.set(...tl.repeat);
  const lth = T.leatherette();
  const kitchenPaint = T.plaster('#b9c9b0', 7, 0.05);
  const stairGreen = T.plaster('#4f6b55', 8, 0.09);
  const stairWhite = T.plaster('#c9c6b8', 9, 0.12);
  const doorPaint = T.plaster('#ece8dc', 10, 0.035);
  const oil = T.oilcloth(); oil.repeat.set(2.2, 2.2);

  const fab = (k, size) => { const f = T.fabric(k); f.map.repeat.set(1 / size, 1 / size); f.normalMap.repeat.set(4 / size, 4 / size); return new THREE.MeshStandardMaterial({map: f.map, normalMap: f.normalMap, roughness: 1, normalScale: new THREE.Vector2(0.5, 0.5)}); };
  M = {
    wallLiving: wp(wpL), wallBedroom: wp(wpB), wallHall: wp(wpH),
    ceiling: new THREE.MeshStandardMaterial({map: ceil.map, normalMap: ceil.normalMap, roughness: 0.95, color: 0xf4f1ea}),
    whitewash: new THREE.MeshStandardMaterial({map: whitewash.map, normalMap: whitewash.normalMap, roughness: 0.95}),
    kitchenWall: new THREE.MeshStandardMaterial({map: kitchenPaint.map, normalMap: kitchenPaint.normalMap, roughness: 0.55}),
    tiles: new THREE.MeshStandardMaterial({map: tl.map, normalMap: tl.normalMap, roughness: 0.18}),
    parquet: pbr('herringbone_parquet', {size: 1.6, rough: 0.9, normal: 0.8, color: 0xc49a72}),
    linoleum: pbr('old_linoleum_flooring_01', {size: 1.4, rough: 0.8, color: 0xe6d8bf}),
    linoBrown: pbr('linoleum_brown', {size: 1.2, rough: 0.8}),
    stairConcrete: pbr('painted_concrete', {size: 2.5}),
    stairGreen: new THREE.MeshStandardMaterial({map: stairGreen.map, normalMap: stairGreen.normalMap, roughness: 0.45}),
    stairWhite: new THREE.MeshStandardMaterial({map: stairWhite.map, normalMap: stairWhite.normalMap, roughness: 0.95}),
    plaid: pbr('curly_teddy_checkered', {size: 0.7, rough: 1, roughMap: false}),
    checkCloth: pbr('fabric_pattern_07', {size: 0.35, rough: 1, roughMap: false}),
    jacquard: fab('upholstery', 0.45),
    jacquardCurtain: fab('damask', 0.7),
    bedspread: fab('bedspread', 0.6),
    greenFabric: fab('green', 0.5),
    wool: pbr('caban', {size: 0.5, rough: 1, roughMap: false, color: 0x9a6a50}),
    cherry: pbr('lacquered_cherry_wood', {size: 0.9, rough: 0.85, roughMap: false, color: 0xa8705a}),
    walnut: pbr('walnut_veneer', {size: 0.8, rough: 0.6, color: 0x8a5a3a}),
    kitchenWood: pbr('kitchen_wood', {size: 0.8, rough: 0.7, color: 0xe4d4b8}),
    greyPlaster: pbr('grey_plaster', {size: 2, roughMap: false}),
    asphalt: pbr('asphalt_02', {size: 4, rough: 0.55}),
    mud: pbr('brown_mud_leaves_01', {size: 3, roughMap: false, rough: 0.75}),
    concretePanels: pbr('concrete_panels', {size: 3.6, roughMap: false}),
    paintedPlaster: pbr('painted_plaster_wall', {size: 2, roughMap: false}),
    rustMetal: pbr('green_metal_rust', {size: 1.5}),
    leatherette: new THREE.MeshStandardMaterial({map: lth.map, normalMap: lth.normalMap, roughness: 0.45, normalScale: new THREE.Vector2(1.4, 1.4)}),
    doorPaint: new THREE.MeshStandardMaterial({map: doorPaint.map, normalMap: doorPaint.normalMap, roughness: 0.35, color: 0xf2eee4}),
    windowPaint: new THREE.MeshStandardMaterial({map: doorPaint.map, roughness: 0.4, color: 0xf0ede6}),
    oilcloth: new THREE.MeshStandardMaterial({map: oil, roughness: 0.25}),
    enamelWhite: std(0xf1eee6, 0.2),
    enamelCream: std(0xe9e0c8, 0.25),
    chrome: std(0xd8d8d8, 0.18, 1),
    brass: std(0xb08a4a, 0.35, 1),
    darkMetal: std(0x2a2a2a, 0.5, 0.7),
    blackPlastic: std(0x151515, 0.4),
    bakelite: std(0x3a2418, 0.35),
    whitePlastic: std(0xe8e4d8, 0.4),
    radiator: std(0xe8e4d8, 0.45, 0.1),
    glass: new THREE.MeshPhysicalMaterial({color: 0x7a8a8c, roughness: 0.04, metalness: 0, transparent: true, opacity: 0.07, depthWrite: false, envMapIntensity: 0.8, side: THREE.DoubleSide}),
    glassThick: new THREE.MeshPhysicalMaterial({color: 0x9fb0ac, roughness: 0.22, transparent: true, opacity: 0.18, depthWrite: false, side: THREE.DoubleSide, envMapIntensity: 0.5}),
    frosted: new THREE.MeshStandardMaterial({color: 0xf4efe2, roughness: 0.6, transparent: true, opacity: 0.75, side: THREE.DoubleSide}),
    crystal: new THREE.MeshPhysicalMaterial({color: 0xffffff, roughness: 0.02, transparent: true, opacity: 0.35, depthWrite: false, envMapIntensity: 3, side: THREE.DoubleSide}),
    tulle: new THREE.MeshStandardMaterial({map: (() => { const t = T.tulle(); t.repeat.set(7, 9); return t; })(), transparent: true, side: THREE.DoubleSide, roughness: 1, color: 0xe8e2d4, depthWrite: false}),
    lace: new THREE.MeshStandardMaterial({map: T.lace(), transparent: true, alphaTest: 0.4, side: THREE.DoubleSide, roughness: 1}),
    shade: new THREE.MeshStandardMaterial({color: 0xe9c38a, roughness: 1, side: THREE.DoubleSide, emissive: 0x000000}),
    paper: std(0xefe8d6, 0.95),
    fabricRed: std(0x7a2e26, 1), fabricBeige: std(0xc9b58f, 1), fabricBrown: std(0x5a3f2c, 1), fabricGreen: std(0x4f5d40, 1), fabricBlue: std(0x3e4a62, 1),
    rubber: std(0x1a1a1a, 0.7),
    pipeYellow: std(0xd8b43a, 0.5, 0.2),
    soil: std(0x2a1d14, 1),
    leaf: std(0x3f5e2c, 0.7),
    black: std(0x000000, 1),
  };
  return M;
}
