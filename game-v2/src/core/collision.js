// 2D (XZ) collision world made of axis-aligned and rotated boxes, with a floor-height map.
export class Collision {
  constructor() { this.boxes = []; this.floors = []; }
  // static solid box in XZ, y range for completeness
  addBox(x0, z0, x1, z1, opt = {}) {
    const b = {x0: Math.min(x0, x1), z0: Math.min(z0, z1), x1: Math.max(x0, x1), z1: Math.max(z0, z1), on: true, y0: 0, y1: 2.5, ...opt};
    this.boxes.push(b); return b;
  }
  addBoxC(cx, cz, w, d, opt) { return this.addBox(cx - w / 2, cz - d / 2, cx + w / 2, cz + d / 2, opt); }
  // walkable area with floor height (for stairs/landing levels)
  addFloor(x0, z0, x1, z1, y = 0, fn = null) { this.floors.push({x0, z0, x1, z1, y, fn}); }
  floorAt(x, z) {
    let best = null;
    for (const f of this.floors) if (x >= f.x0 && x <= f.x1 && z >= f.z0 && z <= f.z1) {
      const y = f.fn ? f.fn(x, z) : f.y;
      if (best === null || y > best) best = y;
    }
    return best;
  }
  // push circle (x,z,r) out of boxes; returns corrected position
  resolve(p, r) {
    for (let iter = 0; iter < 3; iter++) {
      for (const b of this.boxes) {
        if (!b.on) continue;
        const cx = Math.max(b.x0, Math.min(p.x, b.x1));
        const cz = Math.max(b.z0, Math.min(p.z, b.z1));
        const dx = p.x - cx, dz = p.z - cz;
        const d2 = dx * dx + dz * dz;
        if (d2 < r * r) {
          if (d2 > 1e-8) {
            const d = Math.sqrt(d2), k = (r - d) / d;
            p.x += dx * k; p.z += dz * k;
          } else {
            // centre inside box: push along smallest axis
            const pen = [p.x - b.x0 + r, b.x1 - p.x + r, p.z - b.z0 + r, b.z1 - p.z + r];
            const i = pen.indexOf(Math.min(...pen));
            if (i === 0) p.x = b.x0 - r; else if (i === 1) p.x = b.x1 + r; else if (i === 2) p.z = b.z0 - r; else p.z = b.z1 + r;
          }
        }
      }
    }
    return p;
  }
}
