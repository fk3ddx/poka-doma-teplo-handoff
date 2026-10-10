import * as THREE from 'three';
import {boxGeo, rboxGeo, cyl} from './geo.js';
import {std} from './materials.js';

// CRT TV "Рубин" with a live canvas screen and a light that follows the picture.
export class TV {
  constructor(scene, M, {x, y, z, ry}) {
    this.mode = 'off'; this.t = 0; this.crawl = ''; this.crawlT = 0; this.scareFace = 0;
    const g = this.group = new THREE.Group(); g.position.set(x, y, z); g.rotation.y = ry; scene.add(g);
    const wood = M.walnut;
    const add = (geo, mat, px, py, pz, rx = 0) => { const m = new THREE.Mesh(geo, mat); m.position.set(px, py, pz); m.rotation.x = rx; m.castShadow = true; m.receiveShadow = true; g.add(m); return m; };
    const W = 0.74, H = 0.56, D = 0.4;
    add(rboxGeo(W, H, D, 0.015), wood, 0, H / 2, 0);
    // rear CRT housing
    const back = new THREE.CylinderGeometry(0.12, 0.2, 0.12, 4, 1); back.rotateX(Math.PI / 2); back.rotateZ(Math.PI / 4); back.scale(1.3, 1, 1);
    add(back, std(0x2a221c, 0.7), -0.04, H / 2 + 0.01, -D / 2 - 0.055);
    // front panel
    add(rboxGeo(W - 0.03, H - 0.03, 0.012, 0.006), std(0x1c1a18, 0.5), 0, H / 2, D / 2);
    // screen bezel (rounded)
    const bez = std(0x262422, 0.45);
    add(rboxGeo(0.54, 0.05, 0.03, 0.01), bez, -0.09, H / 2 + 0.01 + 0.2, D / 2 + 0.01);
    add(rboxGeo(0.54, 0.05, 0.03, 0.01), bez, -0.09, H / 2 + 0.01 - 0.2, D / 2 + 0.01);
    add(rboxGeo(0.05, 0.44, 0.03, 0.01), bez, -0.09 - 0.245, H / 2 + 0.01, D / 2 + 0.01);
    add(rboxGeo(0.05, 0.44, 0.03, 0.01), bez, -0.09 + 0.245, H / 2 + 0.01, D / 2 + 0.01);
    // canvas screen, slightly bulged
    this.canvas = document.createElement('canvas'); this.canvas.width = 256; this.canvas.height = 192;
    this.ctx = this.canvas.getContext('2d');
    this.tex = new THREE.CanvasTexture(this.canvas); this.tex.colorSpace = THREE.SRGBColorSpace;
    const sg = new THREE.PlaneGeometry(0.46, 0.35, 16, 12);
    const p = sg.attributes.position;
    for (let i = 0; i < p.count; i++) { const sx = p.getX(i) / 0.23, sy = p.getY(i) / 0.175; p.setZ(i, (1 - (sx * sx + sy * sy) * 0.35) * 0.014); }
    sg.computeVertexNormals();
    this.screenMat = new THREE.MeshStandardMaterial({color: 0x111111, roughness: 0.15, metalness: 0.1, emissive: 0xffffff, emissiveMap: this.tex, emissiveIntensity: 0});
    this.screen = add(sg, this.screenMat, -0.09, H / 2 + 0.01, D / 2 + 0.012);
    // control column: knobs + speaker grille
    for (let i = 0; i < 4; i++) add(cyl(0.016, 0.018, 0.02, 14), std(0x0e0e0e, 0.4), 0.26, H - 0.1 - i * 0.06, D / 2 + 0.016, Math.PI / 2);
    for (let i = 0; i < 8; i++) add(boxGeo(0.1, 0.004, 0.004), std(0x9a8a6a, 0.4, 0.6), 0.26, 0.09 + i * 0.016, D / 2 + 0.01);
    add(rboxGeo(0.02, 0.012, 0.01, 0.003), std(0xc02010, 0.4, 0, {emissive: 0x400000}), 0.26, 0.07, D / 2 + 0.012);
    // logo
    const lc = document.createElement('canvas'); lc.width = 128; lc.height = 32; const lx = lc.getContext('2d');
    lx.fillStyle = '#1c1a18'; lx.fillRect(0, 0, 128, 32); lx.fillStyle = '#c8b070'; lx.font = 'italic bold 22px PT Serif, serif'; lx.textAlign = 'center'; lx.fillText('Рубин', 64, 23);
    const lt = new THREE.CanvasTexture(lc); lt.colorSpace = THREE.SRGBColorSpace;
    add(new THREE.PlaneGeometry(0.08, 0.02), new THREE.MeshStandardMaterial({map: lt, roughness: 0.4, metalness: 0.5}), -0.08, 0.035, D / 2 + 0.008);
    // antenna on top
    add(cyl(0.025, 0.03, 0.02, 12), std(0x1a1a1a, 0.5), 0.15, H + 0.01, -0.05);
    for (const s of [-1, 1]) { const a = add(cyl(0.003, 0.002, 0.45, 5), M.chrome, 0.15 + s * 0.1, H + 0.2, -0.05); a.rotation.z = -s * 0.5; }
    // light
    this.light = new THREE.PointLight(0x9fb8ff, 0, 7, 1.6);
    this.light.position.set(-0.08, H / 2, D / 2 + 0.35);
    g.add(this.light);
    this.lightBase = 3.2;
    this.avg = new THREE.Color();
    this.frame = 0;
    this.power = true;
  }

  setMode(m) { this.mode = m; this.t = 0; this.frame = 0; if (this.screenMat.emissiveMap !== this.tex) { this.screenMat.emissiveMap = this.tex; this.screenMat.needsUpdate = true; } }
  setFeed(texture) { this.mode = 'feed'; this.screenMat.emissiveMap = texture; this.screenMat.needsUpdate = true; }
  get on() { return this.mode !== 'off'; }

  update(dt) {
    this.t += dt; this.crawlT += dt;
    const powered = this.power && this.mode !== 'off';
    const target = powered ? 1 : 0;
    this.screenMat.emissiveIntensity += (target * 1.15 - this.screenMat.emissiveIntensity) * Math.min(1, dt * (target ? 3 : 12));
    if (!powered) { this.light.intensity = 0; return; }
    if (this.mode === 'feed') { this.light.color.setRGB(0.6, 0.65, 0.75); this.light.intensity = this.lightBase * 2.6 * (0.85 + Math.random() * 0.15); return; }
    this.frame += dt;
    if (this.frame < 1 / 18) return;
    this.frame = 0;
    const c = this.ctx, W = 256, H = 192, t = this.t;
    c.save();
    let avg = [0.3, 0.3, 0.4];
    if (this.mode === 'concert') avg = this.drawConcert(c, W, H, t);
    else if (this.mode === 'clock') avg = this.drawClock(c, W, H, t);
    else if (this.mode === 'testcard') avg = this.drawTestcard(c, W, H, t);
    else if (this.mode === 'static') avg = this.drawStatic(c, W, H, t);
    else if (this.mode === 'face') avg = this.drawFace(c, W, H, t);
    else if (this.mode === 'black') { c.fillStyle = '#050505'; c.fillRect(0, 0, W, H); avg = [0.02, 0.02, 0.02]; }
    // scanlines + noise always
    c.fillStyle = 'rgba(0,0,0,0.18)';
    for (let y = 0; y < H; y += 2) c.fillRect(0, y, W, 1);
    c.restore();
    this.tex.needsUpdate = true;
    const lum = (avg[0] + avg[1] + avg[2]) / 3;
    this.light.color.setRGB(0.4 + avg[0], 0.4 + avg[1], 0.4 + avg[2]);
    this.light.intensity = this.lightBase * (0.25 + lum * 1.5) * (0.92 + Math.random() * 0.08);
  }

  drawConcert(c, W, H, t) {
    const hue = (t * 8) % 360;
    const g = c.createLinearGradient(0, 0, 0, H);
    g.addColorStop(0, `hsl(${230 + Math.sin(t * 0.3) * 20},50%,${14 + Math.sin(t * 0.7) * 3}%)`);
    g.addColorStop(1, `hsl(${280 + Math.sin(t * 0.2) * 30},40%,8%)`);
    c.fillStyle = g; c.fillRect(0, 0, W, H);
    // spotlights
    for (let i = 0; i < 4; i++) {
      const x = W * (0.2 + i * 0.2) + Math.sin(t * 0.6 + i * 1.7) * 30;
      const gr = c.createLinearGradient(x, 0, x, H);
      gr.addColorStop(0, `hsla(${(hue + i * 60) % 360},70%,70%,0.35)`); gr.addColorStop(1, 'hsla(0,0%,100%,0)');
      c.fillStyle = gr; c.beginPath(); c.moveTo(x - 6, 0); c.lineTo(x + 6, 0); c.lineTo(x + 50, H); c.lineTo(x - 50, H); c.fill();
    }
    // stage floor + singer
    c.fillStyle = 'rgba(30,20,40,0.9)'; c.fillRect(0, H * 0.78, W, H);
    const sx = W * 0.5 + Math.sin(t * 0.8) * 6, sw = Math.sin(t * 2.1) * 0.06;
    c.save(); c.translate(sx, H * 0.8); c.rotate(sw);
    c.fillStyle = '#e8d8c0'; c.beginPath(); c.ellipse(0, -84, 9, 11, 0, 0, Math.PI * 2); c.fill();
    c.fillStyle = '#5a1020'; c.beginPath(); c.moveTo(-14, -72); c.lineTo(14, -72); c.lineTo(26, 0); c.lineTo(-26, 0); c.fill();
    c.fillStyle = '#2a1a10'; c.beginPath(); c.ellipse(0, -90, 11, 9, 0, Math.PI, 0); c.fill();
    c.strokeStyle = '#ccc'; c.lineWidth = 2; c.beginPath(); c.moveTo(8, -64); c.lineTo(4, -80); c.stroke();
    c.restore();
    // audience heads
    c.fillStyle = '#05040a';
    for (let i = 0; i < 14; i++) { c.beginPath(); c.arc(i * 20 + 6, H - 6 + Math.sin(t * 2 + i) * 1.5, 12, 0, Math.PI * 2); c.fill(); }
    // channel logo + title
    c.fillStyle = 'rgba(255,255,255,0.75)'; c.font = 'bold 14px PT Mono, monospace'; c.fillText('1', 12, 20);
    if (t % 40 < 8) { c.font = 'italic 14px PT Serif, serif'; c.fillStyle = 'rgba(255,240,200,0.85)'; c.fillText('«Голубой вечер»', 140, 176); }
    return [0.18, 0.12, 0.3];
  }
  drawClock(c, W, H, t) {
    c.fillStyle = '#0a1a3a'; c.fillRect(0, 0, W, H);
    const cx = W / 2, cy = H / 2 - 6, r = 66;
    c.fillStyle = '#e8eef4'; c.beginPath(); c.arc(cx, cy, r, 0, Math.PI * 2); c.fill();
    c.fillStyle = '#0a1a3a';
    for (let i = 0; i < 60; i++) { const a = i / 60 * Math.PI * 2; const l = i % 5 ? 3 : 8; c.save(); c.translate(cx + Math.sin(a) * (r - 8), cy - Math.cos(a) * (r - 8)); c.rotate(a); c.fillRect(-1, -l / 2, i % 5 ? 2 : 4, l); c.restore(); }
    const sec = 53 + t; // seconds to 22:00
    const ang = (s) => s / 60 * Math.PI * 2;
    const hand = (a, l, w) => { c.save(); c.translate(cx, cy); c.rotate(a); c.fillRect(-w / 2, -l, w, l + 6); c.restore(); };
    hand(ang(59 + Math.min(sec, 60) / 60) , r - 14, 4);
    hand(ang(10 * 5 - 0.3), r - 30, 6);
    c.fillStyle = '#c02020'; hand(ang(Math.floor(sec) % 60), r - 10, 2);
    c.fillStyle = '#e8eef4'; c.font = '13px PT Serif, serif'; c.textAlign = 'center'; c.fillText('сигналы точного времени', cx, H - 12); c.textAlign = 'left';
    return [0.35, 0.4, 0.55];
  }
  drawTestcard(c, W, H, t) {
    c.fillStyle = '#6c6c6c'; c.fillRect(0, 0, W, H);
    c.strokeStyle = '#eee'; c.lineWidth = 1;
    for (let x = 8; x < W; x += 20) { c.beginPath(); c.moveTo(x, 0); c.lineTo(x, H); c.stroke(); }
    for (let y = 6; y < H; y += 20) { c.beginPath(); c.moveTo(0, y); c.lineTo(W, y); c.stroke(); }
    c.fillStyle = '#222'; c.beginPath(); c.arc(W / 2, H / 2, 70, 0, Math.PI * 2); c.fill();
    const bars = ['#fff', '#ff0', '#0ff', '#0f0', '#f0f', '#f00', '#00f', '#000'];
    bars.forEach((b, i) => { c.fillStyle = b; c.fillRect(W / 2 - 64 + i * 16, H / 2 - 40, 16, 34); });
    for (let i = 0; i < 8; i++) { c.fillStyle = `rgb(${i * 36},${i * 36},${i * 36})`; c.fillRect(W / 2 - 64 + i * 16, H / 2 - 6, 16, 16); }
    c.strokeStyle = '#fff'; c.lineWidth = 2; c.beginPath(); c.arc(W / 2, H / 2, 70, 0, Math.PI * 2); c.stroke();
    c.fillStyle = '#fff'; c.font = 'bold 12px PT Mono, monospace'; c.textAlign = 'center'; c.fillText('ВНИМАНИЕ', W / 2, H / 2 + 30);
    c.textAlign = 'left';
    if (this.crawl) {
      c.fillStyle = 'rgba(0,0,40,0.85)'; c.fillRect(0, H - 34, W, 26);
      c.fillStyle = '#fff'; c.font = '15px PT Serif, serif';
      const tw = c.measureText(this.crawl).width;
      const x = W - ((this.crawlT * 46) % (tw + W));
      c.fillText(this.crawl, x, H - 15);
    }
    if (Math.random() < 0.04) { c.fillStyle = 'rgba(255,255,255,0.2)'; c.fillRect(0, Math.random() * H, W, 3); }
    return [0.42, 0.42, 0.45];
  }
  drawStatic(c, W, H) {
    const img = c.createImageData(W / 2, H / 2);
    for (let i = 0; i < img.data.length; i += 4) { const v = Math.random() * 255; img.data[i] = img.data[i + 1] = img.data[i + 2] = v; img.data[i + 3] = 255; }
    const tmp = this._tmp ??= Object.assign(document.createElement('canvas'), {width: W / 2, height: H / 2});
    tmp.getContext('2d').putImageData(img, 0, 0);
    c.imageSmoothingEnabled = false; c.drawImage(tmp, 0, 0, W, H);
    return [0.5, 0.5, 0.5];
  }
  drawFace(c, W, H, t) {
    this.drawTestcard(c, W, H, t);
    c.fillStyle = 'rgba(0,0,0,0.85)'; c.fillRect(0, 0, W, H);
    // a figure standing in a dark room, very still
    c.fillStyle = '#c8c0b0'; c.globalAlpha = 0.55 + Math.random() * 0.1;
    c.beginPath(); c.ellipse(W / 2, 70, 14, 18, 0, 0, Math.PI * 2); c.fill();
    c.fillStyle = '#1a1612'; c.beginPath(); c.moveTo(W / 2 - 28, 190); c.lineTo(W / 2 - 18, 90); c.lineTo(W / 2 + 18, 90); c.lineTo(W / 2 + 28, 190); c.fill();
    c.fillStyle = '#2a2420'; c.beginPath(); c.moveTo(W / 2 - 22, 82); c.quadraticCurveTo(W / 2, 40, W / 2 + 22, 82); c.lineTo(W / 2 + 26, 100); c.lineTo(W / 2 - 26, 100); c.fill();
    c.globalAlpha = 1;
    return [0.06, 0.06, 0.06];
  }
}
