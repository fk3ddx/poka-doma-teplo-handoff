import * as THREE from 'three';

// Procedural canvas textures for things that are specific to a Soviet flat
// and do not exist in CC0 libraries: wallpaper, carpets, oilcloth, book spines, photos…

let seed = 777;
const R = () => { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296; };
const rs = s => { seed = s >>> 0; };
const lerp = (a, b, t) => a + (b - a) * t;

function canvas(w, h = w) {
  const c = document.createElement('canvas'); c.width = w; c.height = h;
  return [c, c.getContext('2d')];
}

// tileable value noise
function makeNoise(size, cells, s = 1) {
  rs(s);
  const g = new Float32Array(cells * cells);
  for (let i = 0; i < g.length; i++) g[i] = R();
  const out = new Float32Array(size * size);
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const fx = x / size * cells, fy = y / size * cells;
    const x0 = Math.floor(fx), y0 = Math.floor(fy);
    const tx = fx - x0, ty = fy - y0;
    const sx = tx * tx * (3 - 2 * tx), sy = ty * ty * (3 - 2 * ty);
    const a = g[(y0 % cells) * cells + x0 % cells], b = g[(y0 % cells) * cells + (x0 + 1) % cells];
    const c = g[((y0 + 1) % cells) * cells + x0 % cells], d = g[((y0 + 1) % cells) * cells + (x0 + 1) % cells];
    out[y * size + x] = lerp(lerp(a, b, sx), lerp(c, d, sx), sy);
  }
  return out;
}
function fbm(size, base = 4, oct = 5, s = 1) {
  const out = new Float32Array(size * size);
  let amp = 0.5, tot = 0;
  for (let o = 0; o < oct; o++) {
    const n = makeNoise(size, base << o, s + o * 101);
    for (let i = 0; i < out.length; i++) out[i] += n[i] * amp;
    tot += amp; amp *= 0.5;
  }
  for (let i = 0; i < out.length; i++) out[i] /= tot;
  return out;
}

function grain(ctx, w, h, amount = 0.06, s = 3, cells = 64) {
  const n = fbm(w, cells / 16 | 0 || 2, 4, s);
  const img = ctx.getImageData(0, 0, w, h);
  const d = img.data;
  for (let i = 0; i < w * h; i++) {
    const v = (n[(i % w) + Math.floor(i / w) % w * w] - 0.5) * 255 * amount + (Math.random() - 0.5) * 255 * amount * 0.5;
    d[i * 4] += v; d[i * 4 + 1] += v; d[i * 4 + 2] += v;
  }
  ctx.putImageData(img, 0, 0);
}

// height (from canvas luminance) -> tangent-space normal map
export function heightToNormal(src, strength = 2) {
  const w = src.width, h = src.height;
  const sd = src.getContext('2d').getImageData(0, 0, w, h).data;
  const [c, ctx] = canvas(w, h);
  const img = ctx.createImageData(w, h);
  const L = (x, y) => { x = (x + w) % w; y = (y + h) % h; const i = (y * w + x) * 4; return (sd[i] + sd[i + 1] + sd[i + 2]) / 765; };
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const dx = (L(x + 1, y) - L(x - 1, y)) * strength;
    const dy = (L(x, y + 1) - L(x, y - 1)) * strength;
    const len = Math.hypot(dx, dy, 1);
    const i = (y * w + x) * 4;
    img.data[i] = (-dx / len * 0.5 + 0.5) * 255;
    img.data[i + 1] = (dy / len * 0.5 + 0.5) * 255;
    img.data[i + 2] = (1 / len * 0.5 + 0.5) * 255;
    img.data[i + 3] = 255;
  }
  ctx.putImageData(img, 0, 0);
  return c;
}

export function tex(c, {srgb = true, repeat = true, aniso = 8} = {}) {
  const t = new THREE.CanvasTexture(c);
  if (srgb) t.colorSpace = THREE.SRGBColorSpace;
  if (repeat) t.wrapS = t.wrapT = THREE.RepeatWrapping;
  t.anisotropy = aniso;
  t.generateMipmaps = true;
  t.minFilter = THREE.LinearMipmapLinearFilter;
  return t;
}

// --- Wallpaper ---------------------------------------------------------------
// Each tile is 0.53 m wide (a real roll) x 0.64 m pattern repeat.
function ornament(ctx, x, y, s, col, kind) {
  ctx.save(); ctx.translate(x, y); ctx.scale(s, s);
  ctx.fillStyle = col; ctx.strokeStyle = col;
  if (kind === 'damask') {
    ctx.lineWidth = 2.2;
    ctx.beginPath();
    ctx.moveTo(0, -60);
    ctx.bezierCurveTo(26, -40, 30, -10, 10, 8); ctx.bezierCurveTo(32, 16, 34, 44, 0, 62);
    ctx.bezierCurveTo(-34, 44, -32, 16, -10, 8); ctx.bezierCurveTo(-30, -10, -26, -40, 0, -60);
    ctx.stroke();
    ctx.beginPath(); ctx.ellipse(0, 0, 7, 12, 0, 0, Math.PI * 2); ctx.fill();
    for (const sx of [-1, 1]) {
      ctx.beginPath(); ctx.moveTo(0, 20); ctx.bezierCurveTo(sx * 22, 26, sx * 30, 40, sx * 16, 52); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(0, -24); ctx.bezierCurveTo(sx * 18, -30, sx * 24, -44, sx * 10, -54); ctx.stroke();
      ctx.beginPath(); ctx.ellipse(sx * 17, -8, 4, 9, sx * 0.6, 0, Math.PI * 2); ctx.fill();
      ctx.beginPath(); ctx.ellipse(sx * 20, 30, 3.5, 7, -sx * 0.5, 0, Math.PI * 2); ctx.fill();
    }
  } else if (kind === 'sprig') {
    ctx.lineWidth = 1.6;
    ctx.beginPath(); ctx.moveTo(0, 22); ctx.bezierCurveTo(-4, 6, 4, -6, 0, -20); ctx.stroke();
    for (const [lx, ly, a] of [[-7, 4, -0.8], [7, -4, 0.8], [-6, -12, -0.6], [6, 12, 0.6]]) {
      ctx.beginPath(); ctx.ellipse(lx, ly, 3, 7, a, 0, Math.PI * 2); ctx.fill();
    }
  } else if (kind === 'rose') {
    for (let i = 0; i < 6; i++) { const a = i / 6 * Math.PI * 2; ctx.beginPath(); ctx.ellipse(Math.cos(a) * 5, Math.sin(a) * 5, 5, 3.5, a, 0, Math.PI * 2); ctx.fill(); }
    ctx.globalAlpha *= 0.6; ctx.beginPath(); ctx.arc(0, 0, 3, 0, Math.PI * 2); ctx.fill();
  }
  ctx.restore();
}

function bouquet(ctx, x, y, P) {
  ctx.save(); ctx.translate(x, y);
  ctx.strokeStyle = P.leaf ?? 'rgba(110,112,64,0.6)'; ctx.lineWidth = 1.4;
  ctx.beginPath(); ctx.moveTo(0, 22); ctx.bezierCurveTo(-2, 10, 3, 2, 0, -8); ctx.stroke();
  ctx.fillStyle = P.leaf ?? 'rgba(110,112,64,0.6)';
  for (const [lx, ly, a] of [[-6, 10, -0.9], [6, 4, 0.9], [-5, -2, -0.6], [5, 16, 0.7]]) { ctx.beginPath(); ctx.ellipse(lx, ly, 2.4, 6, a, 0, Math.PI * 2); ctx.fill(); }
  for (const [fx, fy, r, c] of [[0, -12, 5.5, P.ink], [-8, -5, 4, P.ink2], [8, -6, 4.2, P.ink2]]) {
    ctx.fillStyle = c;
    for (let i = 0; i < 5; i++) { const a = i / 5 * Math.PI * 2; ctx.beginPath(); ctx.ellipse(fx + Math.cos(a) * r * 0.55, fy + Math.sin(a) * r * 0.55, r * 0.5, r * 0.32, a, 0, Math.PI * 2); ctx.fill(); }
    ctx.globalAlpha = 0.7; ctx.fillStyle = 'rgba(230,200,120,0.9)'; ctx.beginPath(); ctx.arc(fx, fy, r * 0.22, 0, Math.PI * 2); ctx.fill(); ctx.globalAlpha = 1;
  }
  ctx.restore();
}

export function wallpaper(style = 'living') {
  const W = 512, H = 640;
  const [c, ctx] = canvas(W, H);
  const [hc, hctx] = canvas(W, H);
  const P = {
    living: {base: '#dccdab', base2: '#cfbd94', ink: 'rgba(158,84,58,0.55)', ink2: 'rgba(170,120,70,0.5)', leaf: 'rgba(104,110,70,0.55)', stripe: 'rgba(186,150,92,0.28)'},
    bedroom: {base: '#cfd3bb', base2: '#c2c8ad', ink: 'rgba(110,128,96,0.55)', ink2: 'rgba(160,110,110,0.45)', stripe: 'rgba(140,150,120,0.2)'},
    hall: {base: '#b9a27c', base2: '#ad9570', ink: 'rgba(110,80,48,0.45)', ink2: 'rgba(90,64,40,0.3)', stripe: 'rgba(120,90,55,0.3)'},
  }[style];
  rs(style.length * 31);
  // paper base with subtle vertical streaks
  ctx.fillStyle = P.base; ctx.fillRect(0, 0, W, H);
  for (let x = 0; x < W; x += 2) { ctx.fillStyle = `rgba(0,0,0,${R() * 0.025})`; ctx.fillRect(x, 0, 2, H); }
  hctx.fillStyle = '#808080'; hctx.fillRect(0, 0, W, H);
  if (style === 'living') {
    // classic: thin double gold lines + half-drop small bouquets
    for (const x of [0, 128, 256, 384]) {
      ctx.fillStyle = 'rgba(176,140,80,0.35)'; ctx.fillRect(x + 62, 0, 2, H); ctx.fillRect(x + 67, 0, 1, H);
      hctx.fillStyle = '#a8a8a8'; hctx.fillRect(x + 62, 0, 3, H);
    }
    for (let row = -1; row < 6; row++) for (let col = 0; col < 4; col++) {
      const x = col * 128 + 0, y = row * 128 + (col % 2) * 64 + 40;
      bouquet(ctx, x, y, P);
      bouquet(hctx, x, y, {ink: '#a0a0a0', ink2: '#a0a0a0', leaf: '#a0a0a0'});
      bouquet(ctx, x + 128, y, P); // wrap
    }
  } else if (style === 'bedroom') {
    for (let row = 0; row < 8; row++) for (let col = 0; col < 6; col++) {
      const x = col * 86 + (row % 2) * 43 + 20, y = row * 80 + 40;
      ornament(ctx, x, y, 1.1, P.ink, 'sprig');
      ornament(ctx, x + 2, y - 22, 1.0, P.ink2, 'rose');
      ornament(hctx, x, y, 1.1, '#9a9a9a', 'sprig');
    }
  } else {
    // embossed vinyl, geometric
    for (let y = 0; y < H; y += 40) for (let x = 0; x < W; x += 32) {
      const off = (y / 40) % 2 ? 16 : 0;
      ctx.strokeStyle = P.ink; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.moveTo(x + off, y); ctx.lineTo(x + off + 16, y + 20); ctx.lineTo(x + off, y + 40); ctx.lineTo(x + off - 16, y + 20); ctx.closePath(); ctx.stroke();
      hctx.strokeStyle = '#b0b0b0'; hctx.lineWidth = 3;
      hctx.beginPath(); hctx.moveTo(x + off, y); hctx.lineTo(x + off + 16, y + 20); hctx.lineTo(x + off, y + 40); hctx.lineTo(x + off - 16, y + 20); hctx.closePath(); hctx.stroke();
    }
  }
  // seams of roll
  ctx.fillStyle = 'rgba(60,40,20,0.12)'; ctx.fillRect(0, 0, 1, H); ctx.fillRect(W - 1, 0, 1, H);
  // aging: soft blotches
  const n = fbm(128, 2, 4, 9);
  const img = ctx.getImageData(0, 0, W, H);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const v = n[(Math.floor(y / H * 128) % 128) * 128 + Math.floor(x / W * 128)] - 0.5;
    const i = (y * W + x) * 4;
    const k = 1 + v * 0.12 + (R() - 0.5) * 0.05;
    img.data[i] *= k; img.data[i + 1] *= k; img.data[i + 2] *= k * 0.98;
  }
  ctx.putImageData(img, 0, 0);
  grain(hctx, W, H, 0.25, 5);
  return {map: tex(c), normalMap: tex(heightToNormal(hc, 1.6), {srgb: false}), repeat: [1 / 0.53, 1 / 0.64]};
}

// --- Carpet / rug ------------------------------------------------------------
export function carpet(kind = 'wall') {
  const W = 1024, H = kind === 'wall' ? 1400 : 1440;
  const [c, ctx] = canvas(W, H);
  rs(kind === 'wall' ? 11 : 23);
  const pal = kind === 'wall'
    ? {field: '#7a1f1c', field2: '#8d2a22', border: '#1d2340', b2: '#c9a76a', ink: '#e2cf9e', dark: '#2b1512', acc: '#2f5a5a'}
    : {field: '#5c2a26', field2: '#6d3429', border: '#3a2c22', b2: '#b48a55', ink: '#d5bb88', dark: '#21140f', acc: '#2a4048'};
  ctx.fillStyle = pal.field; ctx.fillRect(0, 0, W, H);
  // borders
  const bands = [[0, 30, pal.dark], [30, 70, pal.border], [70, 82, pal.b2], [82, 150, pal.border], [150, 160, pal.b2], [160, 174, pal.dark]];
  for (const [a, b, col] of bands) { ctx.strokeStyle = col; ctx.lineWidth = b - a; ctx.strokeRect(a + (b - a) / 2, a + (b - a) / 2, W - a - b, H - a - b); }
  // border motifs
  ctx.fillStyle = pal.ink;
  for (let x = 120; x < W - 100; x += 56) for (const y of [116, H - 116]) { ctx.save(); ctx.translate(x, y); ctx.rotate(Math.PI / 4); ctx.fillRect(-11, -11, 22, 22); ctx.fillStyle = pal.field; ctx.fillRect(-5, -5, 10, 10); ctx.fillStyle = pal.ink; ctx.restore(); }
  for (let y = 120; y < H - 100; y += 56) for (const x of [116, W - 116]) { ctx.save(); ctx.translate(x, y); ctx.rotate(Math.PI / 4); ctx.fillRect(-11, -11, 22, 22); ctx.fillStyle = pal.field; ctx.fillRect(-5, -5, 10, 10); ctx.fillStyle = pal.ink; ctx.restore(); }
  // field lattice of small motifs
  ctx.save(); ctx.beginPath(); ctx.rect(176, 176, W - 352, H - 352); ctx.clip();
  for (let y = 176; y < H - 150; y += 64) for (let x = 176 + ((y / 64) % 2) * 32; x < W - 150; x += 64) {
    ctx.fillStyle = pal.field2; ctx.beginPath(); ctx.moveTo(x, y - 26); ctx.lineTo(x + 26, y); ctx.lineTo(x, y + 26); ctx.lineTo(x - 26, y); ctx.fill();
    ctx.fillStyle = R() > 0.5 ? pal.acc : pal.ink; ctx.globalAlpha = 0.8;
    for (let k = 0; k < 4; k++) { const a = k * Math.PI / 2; ctx.beginPath(); ctx.ellipse(x + Math.cos(a) * 9, y + Math.sin(a) * 9, 5, 2.5, a, 0, Math.PI * 2); ctx.fill(); }
    ctx.globalAlpha = 1;
  }
  // central medallion
  const cx = W / 2, cy = H / 2;
  for (const [r, col] of [[300, pal.dark], [286, pal.border], [250, pal.b2], [240, pal.field2], [170, pal.border], [158, pal.ink], [150, pal.acc], [90, pal.field], [40, pal.ink]]) {
    ctx.fillStyle = col; ctx.beginPath();
    for (let i = 0; i <= 64; i++) { const a = i / 64 * Math.PI * 2; const rr = r * (1 + 0.08 * Math.cos(a * 8)); ctx.lineTo(cx + Math.cos(a) * rr * 0.85, cy + Math.sin(a) * rr * 1.15); }
    ctx.fill();
  }
  for (let i = 0; i < 16; i++) { const a = i / 16 * Math.PI * 2; ctx.fillStyle = i % 2 ? pal.ink : pal.acc; ctx.beginPath(); ctx.ellipse(cx + Math.cos(a) * 205 * 0.85, cy + Math.sin(a) * 205 * 1.15, 16, 7, a, 0, Math.PI * 2); ctx.fill(); }
  // corner quarter-medallions
  for (const [x, y] of [[176, 176], [W - 176, 176], [176, H - 176], [W - 176, H - 176]]) {
    for (const [r, col] of [[150, pal.border], [138, pal.b2], [128, pal.field2], [80, pal.acc]]) { ctx.fillStyle = col; ctx.beginPath(); ctx.arc(x, y, r, 0, Math.PI * 2); ctx.fill(); }
  }
  ctx.restore();
  // pile: woolly noise + wear
  const n = fbm(256, 4, 5, kind.length);
  const img = ctx.getImageData(0, 0, W, H);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const i = (y * W + x) * 4;
    const v = n[(Math.floor(y / H * 256) % 256) * 256 + Math.floor(x / W * 256)];
    const k = 0.86 + v * 0.24 + (R() - 0.5) * 0.14;
    img.data[i] *= k; img.data[i + 1] *= k; img.data[i + 2] *= k;
  }
  ctx.putImageData(img, 0, 0);
  // normal from pile noise
  const [hc, hctx] = canvas(512, 512);
  hctx.fillStyle = '#808080'; hctx.fillRect(0, 0, 512, 512); grain(hctx, 512, 512, 0.5, 8);
  return {map: tex(c, {repeat: false}), normalMap: tex(heightToNormal(hc, 3), {srgb: false})};
}

// --- Oilcloth (клеёнка) ------------------------------------------------------
export function oilcloth() {
  const [c, ctx] = canvas(512);
  ctx.fillStyle = '#efe9da'; ctx.fillRect(0, 0, 512, 512);
  for (let i = 0; i < 8; i++) {
    ctx.fillStyle = 'rgba(176,52,46,0.55)'; ctx.fillRect(i * 64, 0, 28, 512); ctx.fillRect(0, i * 64, 512, 28);
  }
  rs(5);
  for (let y = 46; y < 512; y += 64) for (let x = 46; x < 512; x += 64) {
    ornament(ctx, x, y, 0.9, 'rgba(70,110,60,0.8)', 'sprig');
    ornament(ctx, x, y - 10, 0.8, 'rgba(220,140,40,0.9)', 'rose');
  }
  grain(ctx, 512, 512, 0.04, 2);
  return tex(c);
}

// --- Kitchen tiles 150mm ---------------------------------------------------------
export function tiles() {
  const [c, ctx] = canvas(512); const [hc, hctx] = canvas(512);
  ctx.fillStyle = '#9a9a92'; ctx.fillRect(0, 0, 512, 512);
  hctx.fillStyle = '#303030'; hctx.fillRect(0, 0, 512, 512);
  rs(17);
  for (let y = 0; y < 4; y++) for (let x = 0; x < 4; x++) {
    const v = 232 + R() * 14 | 0;
    ctx.fillStyle = `rgb(${v},${v - 2},${v - 8})`; ctx.fillRect(x * 128 + 3, y * 128 + 3, 122, 122);
    const g = hctx.createLinearGradient(0, 0, 0, 1);
    hctx.fillStyle = '#c8c8c8'; hctx.fillRect(x * 128 + 3, y * 128 + 3, 122, 122);
    hctx.fillStyle = '#e0e0e0'; hctx.fillRect(x * 128 + 7, y * 128 + 7, 114, 114);
  }
  grain(ctx, 512, 512, 0.02, 3);
  return {map: tex(c), normalMap: tex(heightToNormal(hc, 3), {srgb: false}), repeat: [1 / 0.6, 1 / 0.6]};
}

// --- Plaster / whitewash / painted ----------------------------------------------
export function plaster(color = '#e9e5da', s = 3, amt = 0.08) {
  const [c, ctx] = canvas(512);
  ctx.fillStyle = color; ctx.fillRect(0, 0, 512, 512);
  grain(ctx, 512, 512, amt, s);
  const [hc, hctx] = canvas(256); hctx.fillStyle = '#808080'; hctx.fillRect(0, 0, 256, 256); grain(hctx, 256, 256, 0.3, s + 1);
  return {map: tex(c), normalMap: tex(heightToNormal(hc, 1.2), {srgb: false})};
}

// --- Leatherette door (дерматин) -----------------------------------------------------
export function leatherette() {
  const [c, ctx] = canvas(512); const [hc, hctx] = canvas(512);
  ctx.fillStyle = '#4a2c20'; ctx.fillRect(0, 0, 512, 512);
  hctx.fillStyle = '#909090'; hctx.fillRect(0, 0, 512, 512);
  // quilted rhombus pattern via buttons
  for (let y = 0; y <= 512; y += 128) for (let x = 0; x <= 512; x += 128) for (const [ox, oy] of [[0, 0], [64, 64]]) {
    const px = x + ox, py = y + oy;
    const g = hctx.createRadialGradient(px, py, 2, px, py, 70);
    g.addColorStop(0, '#202020'); g.addColorStop(0.15, '#606060'); g.addColorStop(1, 'rgba(160,160,160,0)');
    hctx.fillStyle = g; hctx.beginPath(); hctx.arc(px, py, 70, 0, Math.PI * 2); hctx.fill();
    const g2 = ctx.createRadialGradient(px, py, 2, px, py, 60);
    g2.addColorStop(0, '#1d100b'); g2.addColorStop(0.3, 'rgba(40,22,15,0.6)'); g2.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = g2; ctx.beginPath(); ctx.arc(px, py, 60, 0, Math.PI * 2); ctx.fill();
  }
  grain(ctx, 512, 512, 0.06, 4); grain(hctx, 512, 512, 0.15, 6);
  return {map: tex(c), normalMap: tex(heightToNormal(hc, 4), {srgb: false})};
}

// --- Tulle (тюль), alpha pattern ----------------------------------------------------
export function tulle() {
  const [c, ctx] = canvas(256);
  ctx.fillStyle = 'rgba(255,255,255,0.07)'; ctx.fillRect(0, 0, 256, 256);
  ctx.strokeStyle = 'rgba(255,255,255,0.3)'; ctx.lineWidth = 1;
  for (let y = 0; y < 256; y += 32) for (let x = 0; x < 256; x += 32) {
    const ox = (y / 32) % 2 ? 16 : 0;
    ctx.beginPath(); ctx.ellipse(x + ox, y + 16, 5, 9, 0, 0, Math.PI * 2); ctx.stroke();
  }
  for (let i = 0; i < 256; i += 3) { ctx.fillStyle = 'rgba(255,255,255,0.05)'; ctx.fillRect(i, 0, 1, 256); }
  for (let i = 0; i < 256; i += 3) { ctx.fillStyle = 'rgba(255,255,255,0.03)'; ctx.fillRect(0, i, 256, 1); }
  const t = tex(c); return t;
}

export function lace() {
  const [c, ctx] = canvas(256);
  ctx.clearRect(0, 0, 256, 256);
  ctx.strokeStyle = '#fff'; ctx.fillStyle = 'rgba(255,255,255,0.85)'; ctx.lineWidth = 2;
  ctx.beginPath(); ctx.arc(128, 128, 120, 0, Math.PI * 2); ctx.fill();
  ctx.globalCompositeOperation = 'destination-out';
  for (let r = 20; r < 118; r += 16) for (let i = 0; i < Math.floor(r / 3); i++) {
    const a = i / Math.floor(r / 3) * Math.PI * 2; ctx.beginPath(); ctx.arc(128 + Math.cos(a) * r, 128 + Math.sin(a) * r, 3.2, 0, Math.PI * 2); ctx.fill();
  }
  for (let i = 0; i < 24; i++) { const a = i / 24 * Math.PI * 2; ctx.beginPath(); ctx.arc(128 + Math.cos(a) * 124, 128 + Math.sin(a) * 124, 9, 0, Math.PI * 2); ctx.fill(); }
  ctx.globalCompositeOperation = 'source-over';
  return tex(c, {repeat: false});
}

// --- Book spines atlas -----------------------------------------------------------
const TITLES = ['Л. Толстой', 'Война и мир', 'А. Пушкин', 'Стихи', 'Кулинария', 'Справочник садовода', 'Ф. Достоевский', 'Тихий Дон', 'Жюль Верн', 'Здоровье', 'Детская энциклопедия', 'Мастер и Маргарита', 'А. Чехов', 'Рассказы', 'Книга о вкусной и здоровой пище', 'Домоводство', 'М. Шолохов', 'Вязание', 'Н. Гоголь', 'С. Есенин', 'Атлас мира', 'Медицинская энциклопедия', 'Три мушкетёра', 'Ю. Семёнов', 'Роман-газета', 'И. Тургенев', 'К. Паустовский', 'Огонёк'];
const SPINE = ['#6b1f1a', '#1f3b2a', '#273452', '#5a4a2a', '#7a5a2a', '#3a2a1e', '#4d1c2c', '#2b4d4a', '#8a7a5a', '#1e1e24', '#6a3a1a', '#3d5a3a', '#a08a62', '#5c2e2e'];
export function bookAtlas() {
  const W = 1024, H = 256; const [c, ctx] = canvas(W, H);
  rs(99);
  let x = 0; const spines = [];
  while (x < W) {
    const w = 18 + R() * 26 | 0;
    const col = SPINE[R() * SPINE.length | 0];
    ctx.fillStyle = col; ctx.fillRect(x, 0, w, H);
    const g = ctx.createLinearGradient(x, 0, x + w, 0);
    g.addColorStop(0, 'rgba(0,0,0,0.35)'); g.addColorStop(0.3, 'rgba(255,255,255,0.08)'); g.addColorStop(0.7, 'rgba(0,0,0,0)'); g.addColorStop(1, 'rgba(0,0,0,0.4)');
    ctx.fillStyle = g; ctx.fillRect(x, 0, w, H);
    ctx.fillStyle = 'rgba(214,180,100,0.8)';
    if (R() > 0.3) { ctx.fillRect(x + 2, 18, w - 4, 2); ctx.fillRect(x + 2, H - 22, w - 4, 2); }
    ctx.save(); ctx.translate(x + w / 2 + 4, H - 34); ctx.rotate(-Math.PI / 2);
    ctx.font = `${Math.min(14, w * 0.55) | 0}px PT Serif, serif`; ctx.fillStyle = R() > 0.5 ? 'rgba(220,190,120,0.9)' : 'rgba(230,225,210,0.85)';
    ctx.fillText(TITLES[R() * TITLES.length | 0], 0, 0); ctx.restore();
    spines.push([x / W, w / W]); x += w;
  }
  grain(ctx, W, H, 0.05, 6);
  return {map: tex(c, {repeat: false}), spines};
}

// --- Old photos ---------------------------------------------------------------
export function photo(kind = 0) {
  const W = 256, H = 320; const [c, ctx] = canvas(W, H);
  rs(1000 + kind * 13);
  const sepia = kind % 2 === 0;
  const bg = sepia ? ['#c9b58f', '#8a7354'] : ['#b8bab4', '#5d605c'];
  const g = ctx.createLinearGradient(0, 0, 0, H); g.addColorStop(0, bg[0]); g.addColorStop(1, bg[1]);
  ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
  ctx.filter = 'blur(2.5px)';
  const people = kind === 0 ? 2 : kind === 1 ? 1 : kind === 2 ? 3 : 1;
  for (let p = 0; p < people; p++) {
    const cx = W * (people === 1 ? 0.5 : 0.25 + p * 0.5 / (people - 1 || 1)), sc = people === 1 ? 1.25 : 0.95;
    ctx.fillStyle = sepia ? '#3e2e1e' : '#2d2f2e';
    ctx.beginPath(); ctx.ellipse(cx, H * 0.86, 70 * sc, 120 * sc, 0, Math.PI, 0); ctx.fill();
    ctx.fillStyle = sepia ? '#b89a76' : '#a3a39e';
    ctx.beginPath(); ctx.ellipse(cx, H * 0.42, 27 * sc, 34 * sc, 0, 0, Math.PI * 2); ctx.fill();
    ctx.fillStyle = sepia ? '#3a2a1a' : '#262626';
    ctx.beginPath(); ctx.ellipse(cx, H * 0.36, 29 * sc, 22 * sc, 0, Math.PI, 0); ctx.fill();
    ctx.fillStyle = 'rgba(40,25,15,0.6)';
    ctx.fillRect(cx - 12 * sc, H * 0.41, 7 * sc, 3); ctx.fillRect(cx + 5 * sc, H * 0.41, 7 * sc, 3);
  }
  ctx.filter = 'none';
  grain(ctx, W, H, 0.12, 4);
  // vignette + white border
  const v = ctx.createRadialGradient(W / 2, H / 2, 60, W / 2, H / 2, 220);
  v.addColorStop(0, 'rgba(0,0,0,0)'); v.addColorStop(1, 'rgba(40,25,10,0.55)');
  ctx.fillStyle = v; ctx.fillRect(0, 0, W, H);
  ctx.strokeStyle = '#ece4d2'; ctx.lineWidth = 16; ctx.strokeRect(0, 0, W, H);
  return tex(c, {repeat: false});
}

// --- Tear-off calendar page ----------------------------------------------------------
export function calendarPage(day = 15) {
  const [c, ctx] = canvas(256, 320);
  ctx.fillStyle = '#f2ede0'; ctx.fillRect(0, 0, 256, 320);
  ctx.fillStyle = '#b02a22'; ctx.font = 'bold 22px PT Serif, serif'; ctx.textAlign = 'center';
  ctx.fillText('НОЯБРЬ', 128, 40);
  ctx.font = 'bold 150px PT Serif, serif'; ctx.fillStyle = day === 15 ? '#b02a22' : '#1d1d24'; ctx.fillText(String(day), 128, 190);
  ctx.font = '24px PT Serif, serif'; ctx.fillStyle = '#1d1d24'; ctx.fillText('ПЯТНИЦА', 128, 232);
  ctx.font = '14px PT Serif, serif'; ctx.fillText('1996', 128, 260);
  ctx.font = '12px PT Serif, serif'; ctx.fillStyle = '#555'; ctx.fillText('Восход 8.21   Заход 16.17', 128, 296);
  grain(ctx, 256, 320, 0.03, 2);
  return tex(c, {repeat: false});
}

// --- Generic label/sign -------------------------------------------------------
export function label(text, {w = 256, h = 64, bg = '#ddd', fg = '#111', font = 'bold 34px PT Serif, serif'} = {}) {
  const [c, ctx] = canvas(w, h);
  ctx.fillStyle = bg; ctx.fillRect(0, 0, w, h);
  ctx.fillStyle = fg; ctx.font = font; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
  ctx.fillText(text, w / 2, h / 2 + 2);
  return tex(c, {repeat: false});
}

// --- Clock face --------------------------------------------------------------------
export function clockFace(style = 'kitchen') {
  const [c, ctx] = canvas(256);
  ctx.fillStyle = style === 'kitchen' ? '#f3efe2' : '#e9dcc0'; ctx.beginPath(); ctx.arc(128, 128, 126, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = '#222'; ctx.font = (style === 'kitchen' ? 'bold 28px' : '26px') + ' PT Serif, serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
  for (let i = 1; i <= 12; i++) { const a = i / 12 * Math.PI * 2 - Math.PI / 2; ctx.fillText(String(i), 128 + Math.cos(a) * 96, 128 + Math.sin(a) * 96); }
  for (let i = 0; i < 60; i++) { const a = i / 60 * Math.PI * 2; ctx.fillRect(128 + Math.cos(a) * 116 - 1, 128 + Math.sin(a) * 116 - 1, i % 5 ? 2 : 4, i % 5 ? 2 : 4); }
  ctx.font = '11px PT Serif, serif'; ctx.fillText(style === 'kitchen' ? 'ЯНТАРЬ' : 'МАЯК', 128, 170);
  grain(ctx, 256, 256, 0.03, 2);
  return tex(c, {repeat: false});
}

// --- Facade with windows (for distant panel buildings) ----------------------------
export function panelFacade(seedN = 1) {
  const W = 512, H = 512; const [c, ctx] = canvas(W, H);
  rs(seedN);
  ctx.fillStyle = '#7c7c76'; ctx.fillRect(0, 0, W, H);
  // panels
  for (let y = 0; y < H; y += 128) for (let x = 0; x < W; x += 128) {
    const v = 112 + R() * 22 | 0; ctx.fillStyle = `rgb(${v},${v},${v - 4})`; ctx.fillRect(x + 2, y + 2, 124, 124);
  }
  grain(ctx, W, H, 0.1, seedN);
  return tex(c);
}

// Fabric with a woven pattern: 'damask' drapes, 'bedspread' quilt, 'upholstery'
export function fabric(kind = 'damask') {
  const S = 512; const [c, ctx] = canvas(S);
  const P = {
    damask: {base: '#7a5a2e', ink: 'rgba(170,130,70,0.55)', ink2: 'rgba(60,40,20,0.35)'},
    bedspread: {base: '#a8574a', ink: 'rgba(235,200,150,0.6)', ink2: 'rgba(90,30,30,0.4)'},
    upholstery: {base: '#6a4a3a', ink: 'rgba(190,150,110,0.45)', ink2: 'rgba(40,25,20,0.35)'},
    green: {base: '#55603e', ink: 'rgba(190,180,120,0.45)', ink2: 'rgba(30,35,20,0.4)'},
  }[kind];
  ctx.fillStyle = P.base; ctx.fillRect(0, 0, S, S);
  rs(kind.length * 7);
  if (kind === 'bedspread') {
    for (let y = 0; y < S; y += 64) for (let x = 0; x < S; x += 64) {
      ctx.strokeStyle = P.ink2; ctx.lineWidth = 2; ctx.strokeRect(x + 2, y + 2, 60, 60);
      bouquet(ctx, x + 32, y + 34, {ink: P.ink, ink2: 'rgba(240,220,170,0.6)', leaf: 'rgba(120,130,80,0.6)'});
    }
  } else {
    for (let row = -1; row < 5; row++) for (let col = -1; col < 5; col++) {
      const x = col * 128 + (row % 2 ? 64 : 0), y = row * 128 + 64;
      ornament(ctx, x, y, 0.9, P.ink, 'damask');
      ornament(ctx, x + 64, y + 64, 0.8, P.ink2, 'sprig');
    }
  }
  // weave
  for (let i = 0; i < S; i += 2) { ctx.fillStyle = 'rgba(0,0,0,0.06)'; ctx.fillRect(i, 0, 1, S); ctx.fillStyle = 'rgba(255,255,255,0.03)'; ctx.fillRect(0, i, S, 1); }
  grain(ctx, S, S, 0.06, kind.length);
  const [hc, hctx] = canvas(256); hctx.fillStyle = '#808080'; hctx.fillRect(0, 0, 256, 256);
  for (let i = 0; i < 256; i += 2) { hctx.fillStyle = i % 4 ? '#909090' : '#707070'; hctx.fillRect(i, 0, 1, 256); hctx.fillRect(0, i, 256, 1); }
  return {map: tex(c), normalMap: tex(heightToNormal(hc, 1.5), {srgb: false})};
}

export function noiseTex(size = 256, s = 5) {
  const [c, ctx] = canvas(size); ctx.fillStyle = '#808080'; ctx.fillRect(0, 0, size, size); grain(ctx, size, size, 0.6, s);
  return tex(c, {srgb: false});
}

export {canvas, grain, fbm};
