// Скачивает CC0-ресурсы Poly Haven и готовит их для игры:
// модели -> GLB с webp-текстурами 512px, материалы -> webp 512/1024px.
// Запуск: node tools/fetch-assets.mjs
import fs from 'node:fs';
import path from 'node:path';
import {execFileSync} from 'node:child_process';
import sharp from 'sharp';

const ROOT = path.resolve(import.meta.dirname, '..');
const RAW = path.join(ROOT, '.cache/raw');
const OUT_M = path.join(ROOT, 'src/assets/models');
const OUT_T = path.join(ROOT, 'src/assets/textures');
for (const d of [RAW, OUT_M, OUT_T]) fs.mkdirSync(d, {recursive: true});

const MODELS = {
  jug_01: 512, tea_set_01: 512, pot_enamel_01: 512, russian_food_cans_01: 512,
  old_bed_frame: 512, alarm_clock_01: 256, rubber_boots: 256, potted_plant_01: 512,
  potted_plant_04: 512, vintage_suitcase: 512, wicker_basket_01: 512, mantel_clock_01: 512,
  postcard_set_01: 512, street_lamp_02: 256, covered_car: 512,
};

// [id, maps, size]
const TEXTURES = [
  ['herringbone_parquet', ['diff', 'nor_gl', 'rough'], 1024],
  ['old_linoleum_flooring_01', ['diff', 'nor_gl', 'rough'], 1024],
  ['linoleum_brown', ['diff', 'nor_gl', 'rough'], 512],
  ['painted_concrete', ['diff', 'nor_gl', 'rough'], 1024],
  ['curly_teddy_checkered', ['diff', 'nor_gl'], 512],
  ['fabric_pattern_07', ['diff', 'nor_gl'], 512],
  ['floral_jacquard', ['diff', 'nor_gl'], 512],
  ['caban', ['diff', 'nor_gl'], 512],
  ['lacquered_cherry_wood', ['diff', 'nor_gl', 'rough'], 1024],
  ['walnut_veneer', ['diff', 'nor_gl', 'rough'], 512],
  ['kitchen_wood', ['diff', 'nor_gl', 'rough'], 512],
  ['grey_plaster', ['diff', 'nor_gl'], 512],
  ['asphalt_02', ['diff', 'nor_gl', 'rough'], 1024],
  ['brown_mud_leaves_01', ['diff', 'nor_gl'], 1024],
  ['concrete_panels', ['diff', 'nor_gl'], 1024],
  ['painted_plaster_wall', ['diff', 'nor_gl'], 512],
  ['green_metal_rust', ['diff', 'nor_gl', 'rough'], 512],
];

async function get(url, dest) {
  if (fs.existsSync(dest) && fs.statSync(dest).size > 0) return dest;
  fs.mkdirSync(path.dirname(dest), {recursive: true});
  for (let a = 0; a < 3; a++) {
    const r = await fetch(url);
    if (r.ok) { fs.writeFileSync(dest, Buffer.from(await r.arrayBuffer())); return dest; }
    await new Promise(res => setTimeout(res, 1000));
  }
  throw new Error('download failed ' + url);
}
const api = async id => (await fetch('https://api.polyhaven.com/files/' + id)).json();
const manifest = [];

for (const [id, size] of Object.entries(MODELS)) {
  const out = path.join(OUT_M, id + '.glb');
  if (!fs.existsSync(out)) {
    const f = await api(id);
    const g = f.gltf['1k'].gltf;
    const dir = path.join(RAW, id);
    await get(g.url, path.join(dir, 'model.gltf'));
    for (const [rel, inc] of Object.entries(g.include)) await get(inc.url, path.join(dir, rel));
    execFileSync(path.join(ROOT, 'node_modules/.bin/gltf-transform'),
      ['optimize', path.join(dir, 'model.gltf'), out, '--texture-compress', 'webp',
       '--texture-size', String(size), '--compress', 'false', '--simplify', 'false', '--instance', 'false'],
      {stdio: 'inherit'});
  }
  manifest.push({id, type: 'model', license: 'CC0', source: 'https://polyhaven.com/a/' + id});
  console.log('model', id, (fs.statSync(out).size / 1e6).toFixed(2), 'MB');
}

for (const [id, maps, size] of TEXTURES) {
  let f = null;
  for (const m of maps) {
    const out = path.join(OUT_T, `${id}_${m}.webp`);
    if (fs.existsSync(out)) continue;
    f ??= await api(id);
    const key = m === 'diff' ? 'Diffuse' : m === 'nor_gl' ? 'nor_gl' : 'Rough';
    const src = f[key] ?? (m === 'diff' ? f.col_1 : null);
    const entry = src?.['1k']?.jpg ?? src?.['1k']?.png;
    if (!entry) { console.warn('missing', id, m); continue; }
    const raw = await get(entry.url, path.join(RAW, 'tex', `${id}_${m}.${entry.url.split('.').pop()}`));
    await sharp(raw).resize(size, size).webp({quality: m === 'diff' ? 86 : 80}).toFile(out);
  }
  manifest.push({id, type: 'texture', license: 'CC0', source: 'https://polyhaven.com/a/' + id});
  console.log('texture', id);
}
fs.writeFileSync(path.join(ROOT, 'src/assets/CREDITS.json'), JSON.stringify(manifest, null, 2));
