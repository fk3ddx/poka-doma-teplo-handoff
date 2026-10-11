// Сравнение "картинки" нашей игры с Fears to Fathom по объективным метрикам.
//   npm install
//   node look-check.js refs                 — скачать скриншоты FtF (refs.txt) и посчитать метрики
//   node look-check.js shots/*.png [...jpg] — посчитать метрики своих скриншотов и сравнить с целями
// Метрики: яркость (медиана, 1-й и 99-й перцентили), цвет "чёрного" (самые тёмные 2%),
// средний цвет кадра, отношение горизонтальной/вертикальной резкости (VHS мылит по горизонтали)
// для яркости и для цвета (растекание цвета), зерно в тёмных участках.
const fs = require('fs'), path = require('path'), https = require('https');
const jpeg = require('jpeg-js'), {PNG} = require('pngjs');

function decode(file) {
  const buf = fs.readFileSync(file);
  if (file.toLowerCase().endsWith('.png')) { const p = PNG.sync.read(buf); return {w: p.width, h: p.height, d: p.data}; }
  const j = jpeg.decode(buf, {useTArray: true}); return {w: j.width, h: j.height, d: j.data};
}

function metrics(file) {
  const {w, h, d} = decode(file);
  const N = w * h, L = new Float32Array(N);
  let sr = 0, sg = 0, sb = 0;
  for (let i = 0; i < N; i++) {
    const r = d[i * 4], g = d[i * 4 + 1], b = d[i * 4 + 2];
    L[i] = 0.2126 * r + 0.7152 * g + 0.0722 * b; sr += r; sg += g; sb += b;
  }
  const s = Float32Array.from(L).sort();
  const p = q => s[Math.floor(q * (N - 1))];
  const thr = p(0.02);
  let dr = 0, dg = 0, db = 0, n = 0;
  for (let i = 0; i < N; i++) if (L[i] <= thr) { dr += d[i * 4]; dg += d[i * 4 + 1]; db += d[i * 4 + 2]; n++; }
  // резкость по осям: средний модуль разницы соседних пикселей
  let lh = 0, lv = 0, ch = 0, cv = 0, c = 0, grain = 0, gc = 0;
  const cr = k => d[k * 4] - d[k * 4 + 2];
  for (let y = 1; y < h - 1; y += 2) for (let x = 1; x < w - 1; x += 2) {
    const i = y * w + x;
    lh += Math.abs(L[i] - L[i + 1]); lv += Math.abs(L[i] - L[i + w]);
    ch += Math.abs(cr(i) - cr(i + 1)); cv += Math.abs(cr(i) - cr(i + w)); c++;
    // зерно: отклонение от среднего 3x3 в тёмных участках
    if (L[i] < 40) {
      // только ровные участки (без краёв): соседи 5x5 через пиксель, перепад меньше 12
      const nb = [L[i - 2 * w - 2], L[i - 2 * w + 2], L[i + 2 * w - 2], L[i + 2 * w + 2], L[i - 2], L[i + 2], L[i - 2 * w], L[i + 2 * w]];
      if (y > 2 && y < h - 3 && x > 2 && x < w - 3 && Math.max(...nb) - Math.min(...nb) < 12) {
        const m = (L[i - w - 1] + L[i - w] + L[i - w + 1] + L[i - 1] + L[i + 1] + L[i + w - 1] + L[i + w] + L[i + w + 1]) / 8;
        grain += Math.abs(L[i] - m); gc++;
      }
    }
  }
  return {
    file: path.basename(file),
    p1: p(0.01), p50: p(0.5), p99: p(0.99),
    black: [dr / n, dg / n, db / n], mean: [sr / N, sg / N, sb / N],
    lumaHV: lh / lv, chromaHV: ch / cv, grain: gc ? grain / gc : 0,
  };
}

// Цели сняты с 34 официальных скриншотов FtF (Home Alone, Norwood, Carson, Ironbark), см. LIGHT-AND-VHS.md
const TARGETS = {
  night: {p50: [6, 30], p99: [25, 255], blackMax: 14, lumaHV: [0.6, 1.0], chromaHV: [0.4, 1.0]},
  warm: {p50: [30, 70], p99: [120, 255], blackMax: 25, lumaHV: [0.55, 1.0], chromaHV: [0.4, 1.0]},
};

const f0 = v => v.toFixed(0), f2 = v => v.toFixed(2);
function row(m) {
  return `${m.file.padEnd(28)} яркость p1/p50/p99 ${f0(m.p1).padStart(3)}/${f0(m.p50).padStart(3)}/${f0(m.p99).padStart(3)}` +
    ` | чёрный ${m.black.map(f0).join(',').padEnd(9)} | средний ${m.mean.map(f0).join(',').padEnd(11)}` +
    ` | H/V яркость ${f2(m.lumaHV)} цвет ${f2(m.chromaHV)} | зерно ${f2(m.grain)}`;
}

function judge(m) {
  const kind = m.p50 < 30 ? 'night' : 'warm', t = TARGETS[kind], bad = [];
  const inR = (v, [a, b]) => v >= a && v <= b;
  if (!inR(m.p50, t.p50)) bad.push(`медиана яркости ${f0(m.p50)} вне ${t.p50}`);
  if (!inR(m.p99, t.p99)) bad.push(`p99 ${f0(m.p99)} вне ${t.p99}`);
  if (Math.max(...m.black) > t.blackMax) bad.push(`чёрный слишком поднят (${m.black.map(f0)})`);
  if (Math.max(...m.black) < 1) bad.push('чёрный = 0: нет подъёма чёрного/зерна');
  if (!inR(m.lumaHV, t.lumaHV)) bad.push(`H/V яркости ${f2(m.lumaHV)} (нужно ${t.lumaHV}: горизонтальное мыло VHS)`);
  if (!inR(m.chromaHV, t.chromaHV)) bad.push(`H/V цвета ${f2(m.chromaHV)} (нужно ${t.chromaHV}: растекание цвета)`);
  if (m.grain < 0.45) bad.push(`нет зерна в тенях (${f2(m.grain)})`);
  return `  [${kind === 'night' ? 'ночь' : 'тёплый'}] ` + (bad.length ? 'НЕ ПОХОЖЕ: ' + bad.join('; ') : 'в диапазоне FtF');
}

function get(url, dest) {
  return new Promise((ok, fail) => https.get(url, r => {
    if (r.statusCode !== 200) return fail(new Error(url + ' ' + r.statusCode));
    r.pipe(fs.createWriteStream(dest)).on('finish', ok);
  }).on('error', fail));
}

(async () => {
  const args = process.argv.slice(2);
  if (!args.length) { console.log('node look-check.js refs | node look-check.js <png/jpg...>'); return; }
  if (args[0] === 'refs') {
    fs.mkdirSync('refs', {recursive: true});
    const lines = fs.readFileSync(path.join(__dirname, 'refs.txt'), 'utf8').split('\n').filter(l => l && !l.startsWith('#'));
    for (const l of lines) {
      const [name, url] = l.trim().split(/\s+/), dest = path.join('refs', name + '.jpg');
      if (!fs.existsSync(dest)) await get(url, dest);
      console.log(row(metrics(dest)));
    }
    return;
  }
  const all = [];
  for (const f of args) { const m = metrics(f); all.push(m); console.log(row(m)); console.log(judge(m)); }
  // итог по набору: отдельный кадр может выпадать (меню, вспышка), важна медиана по 6+ кадрам
  if (all.length > 1) for (const kind of ['night', 'warm']) {
    const g = all.filter(m => (m.p50 < 30) === (kind === 'night'));
    if (!g.length) continue;
    const med = k => { const v = g.map(m => m[k]).sort((a, b) => a - b); return v[v.length >> 1]; };
    const blk = [0, 1, 2].map(i => { const v = g.map(m => m.black[i]).sort((a, b) => a - b); return v[v.length >> 1]; });
    const m = {file: `ИТОГ ${kind === 'night' ? 'ночь' : 'тёплые'} (${g.length})`, p1: med('p1'), p50: med('p50'), p99: med('p99'), black: blk, mean: [0, 0, 0], lumaHV: med('lumaHV'), chromaHV: med('chromaHV'), grain: med('grain')};
    console.log(row(m)); console.log(judge(m));
  }
})();
