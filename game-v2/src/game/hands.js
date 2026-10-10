import * as THREE from 'three';
import {lathe, tube, cyl, rboxGeo, boxGeo} from '../world/geo.js';
import {buildHand} from './creature.js';

// First-person held items, attached to the camera.
export class Hands {
  constructor(camera, scene) {
    this.cam = camera;
    this.root = new THREE.Group(); camera.add(this.root); scene.add(camera);
    this.items = {};
    this.current = null; this.blend = 0; this.t = 0;
    // tea glass in a metal holder (подстаканник)
    {
      const g = new THREE.Group();
      const metal = new THREE.MeshStandardMaterial({color: 0xb8a070, roughness: 0.3, metalness: 1});
      const holder = new THREE.Mesh(lathe([[0.0, 0], [0.034, 0], [0.036, 0.004], [0.034, 0.03], [0.037, 0.06], [0.038, 0.062], [0.033, 0.062]], 20), metal);
      g.add(holder);
      const handle = new THREE.Mesh(tube([[0.035, 0.012, 0], [0.06, 0.02, 0], [0.065, 0.05, 0], [0.037, 0.058, 0]], 0.004, 10, 6), metal); g.add(handle);
      const glass = new THREE.Mesh(lathe([[0, 0.004], [0.031, 0.004], [0.034, 0.1], [0.033, 0.1]], 20), new THREE.MeshPhysicalMaterial({color: 0xffffff, roughness: 0.05, transparent: true, opacity: 0.25, depthWrite: false}));
      g.add(glass);
      this.tea = new THREE.Mesh(cyl(0.03, 0.029, 0.075, 18), new THREE.MeshStandardMaterial({color: 0x5a2a0a, roughness: 0.1, transparent: true, opacity: 0.88}));
      this.tea.position.y = 0.045; g.add(this.tea);
      const spoon = new THREE.Mesh(cyl(0.002, 0.002, 0.14, 4), metal); spoon.position.set(0.012, 0.09, 0.005); spoon.rotation.z = -0.25; g.add(spoon);
      // steam
      const sc = document.createElement('canvas'); sc.width = sc.height = 64; const sx = sc.getContext('2d');
      const rg = sx.createRadialGradient(32, 32, 0, 32, 32, 32); rg.addColorStop(0, 'rgba(255,255,255,0.8)'); rg.addColorStop(1, 'rgba(255,255,255,0)'); sx.fillStyle = rg; sx.fillRect(0, 0, 64, 64);
      const steamMat = new THREE.SpriteMaterial({map: new THREE.CanvasTexture(sc), color: 0xffffff, transparent: true, opacity: 0.0, depthWrite: false});
      this.steam = [];
      for (let i = 0; i < 4; i++) { const s = new THREE.Sprite(steamMat.clone()); s.scale.set(0.03, 0.05, 1); g.add(s); this.steam.push(s); }
      g.position.set(0.2, -0.25, -0.5); g.rotation.set(0.05, -0.2, 0); g.scale.setScalar(0.85);
      this.items.tea = g;
    }
    // dynamo flashlight "жучок"
    {
      const g = new THREE.Group();
      const plastic = new THREE.MeshStandardMaterial({color: 0x2a3a2a, roughness: 0.5});
      const body = new THREE.Mesh(rboxGeo(0.055, 0.035, 0.11, 0.015), plastic); g.add(body);
      const lens = new THREE.Mesh(cyl(0.02, 0.02, 0.01, 16), new THREE.MeshStandardMaterial({color: 0xffffff, emissive: 0xfff0c0, emissiveIntensity: 0})); lens.rotation.x = Math.PI / 2; lens.position.z = -0.058; g.add(lens);
      this.lever = new THREE.Mesh(rboxGeo(0.012, 0.02, 0.1, 0.005), new THREE.MeshStandardMaterial({color: 0x888880, roughness: 0.4, metalness: 0.8}));
      this.lever.position.set(0.03, -0.01, 0.0); g.add(this.lever);
      this.lens = lens;
      const spot = this.spot = new THREE.SpotLight(0xffe6b8, 0, 10, 0.62, 0.85, 1.5);
      spot.position.set(0, 0, -0.06); spot.target.position.set(-0.12, 0.2, -2);
      spot.castShadow = true; spot.shadow.mapSize.set(512, 512); spot.shadow.bias = -0.0015; spot.shadow.camera.near = 0.1; spot.shadow.camera.far = 9;
      g.add(spot, spot.target);
      g.position.set(0.15, -0.17, -0.3); g.rotation.set(0.05, 0.05, 0);
      this.items.flashlight = g;
    }
    {
      const c = document.createElement('canvas'); c.width = c.height = 128; const x = c.getContext('2d');
      x.fillStyle = '#4e6b5a'; x.fillRect(0, 0, 128, 128);
      for (let i = 0; i < 128; i += 32) { x.fillStyle = 'rgba(200,190,150,0.45)'; x.fillRect(i, 0, 8, 128); x.fillRect(0, i, 128, 8); x.fillStyle = 'rgba(120,40,30,0.4)'; x.fillRect(i + 14, 0, 3, 128); x.fillRect(0, i + 14, 128, 3); }
      const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(2, 2);
      const g = new THREE.PlaneGeometry(0.9, 0.7, 12, 8); const p = g.attributes.position;
      for (let i = 0; i < p.count; i++) { const px = p.getX(i), py = p.getY(i); p.setZ(i, Math.sin(px * 9) * 0.015 - px * px * 0.5 + Math.sin(py * 7 + px * 3) * 0.01); }
      g.computeVertexNormals();
      this.lap = new THREE.Mesh(g, new THREE.MeshStandardMaterial({map: t, roughness: 1, side: THREE.DoubleSide}));
      this.lap.position.set(0, -0.55, -0.45); this.lap.rotation.x = -1.25; this.lap.visible = false;
      camera.add(this.lap);
    }
    // the thing's hands, for the death: they come in from the screen edges and grab the face
    {
      const skin = new THREE.MeshStandardMaterial({color: 0x4c483e, roughness: 0.6, envMapIntensity: 0.15});
      const nail = new THREE.MeshStandardMaterial({color: 0x140c08, roughness: 0.25});
      this.claws = [];
      for (const s of [-1, 1]) {
        const h = buildHand(skin, nail, s, 1.9); h.scale.setScalar(0.75);
        const g = new THREE.Group(); g.add(h); g.visible = false; camera.add(g);
        const fore = new THREE.Mesh(new THREE.CylinderGeometry(0.025, 0.035, 0.9, 10), skin); fore.position.y = 0.45; h.add(fore);
        h.traverse(o => { if (o.isMesh) o.renderOrder = 6; });
        this.claws.push({g, h, s});
      }
      this.claw = 0;
    }
    for (const it of Object.values(this.items)) { it.visible = false; this.root.add(it); it.traverse(o => { if (o.isMesh) { o.renderOrder = 5; } }); }
    this.charge = 0; this.squeezeT = 0;
  }
  hold(name) {
    for (const [k, it] of Object.entries(this.items)) it.visible = k === name;
    this.current = name;
    this.blend = 0;
    if (name !== 'flashlight') { this.spot.intensity = 0; this.spot.shadow.autoUpdate = false; }
  }
  squeeze() { this.charge = Math.min(1, this.charge + 0.34); this.squeezeT = 0.18; }
  get lightOn() { return this.current === 'flashlight' && this.charge > 0.02; }
  update(dt, moving) {
    this.t += dt;
    // claws: 0 hidden, 1 gripping the face
    for (const c of this.claws) {
      const k = this.claw;
      c.g.visible = k > 0.01;
      const ease = 1 - Math.pow(1 - Math.min(1, k), 3);
      c.g.position.set(c.s * (0.62 - ease * 0.3), 0.04 + c.s * 0.03 + Math.sin(this.t * 40) * 0.006 * ease, -0.3);
      c.g.rotation.set(0.25, c.s * 0.5, -c.s * (Math.PI / 2 - 0.25 + ease * 0.15));
      c.h.curl(0.15 + ease * 0.75 + Math.sin(this.t * 30 + c.s) * 0.05 * ease);
    }
    this.blend = Math.min(1, this.blend + dt * 3);
    const it = this.current ? this.items[this.current] : null;
    this.root.position.y = (1 - this.blend) * -0.25 + Math.sin(this.t * 1.3) * 0.003 + (moving ? Math.sin(this.t * 9) * 0.006 : 0);
    this.root.position.x = moving ? Math.cos(this.t * 4.5) * 0.004 : 0;
    if (this.current === 'tea') {
      this.steam.forEach((s, i) => {
        const p = (this.t * 0.35 + i / 4) % 1;
        s.position.set(Math.sin(this.t * 2 + i) * 0.01, 0.1 + p * 0.12, 0); s.material.opacity = Math.sin(p * Math.PI) * 0.18; s.scale.set(0.02 + p * 0.03, 0.03 + p * 0.04, 1);
      });
    }
    if (this.current === 'flashlight') {
      this.charge = Math.max(0, this.charge - dt * 0.085);
      this.squeezeT = Math.max(0, this.squeezeT - dt);
      this.lever.position.x = 0.03 - (this.squeezeT > 0 ? 0.012 : 0);
      const flick = this.charge < 0.25 ? (0.6 + Math.random() * 0.4) : 1;
      const k = Math.pow(this.charge, 0.6) * flick;
      this.spot.intensity = 20 * k;
      this.spot.shadow.autoUpdate = k > 0.01;
      this.lens.material.emissiveIntensity = 2.5 * k;
    }
  }
}
