import * as THREE from 'three';
import {lathe, tube, rboxGeo, cyl} from '../world/geo.js';
import * as T from '../world/textures.js';

const LAYER_FEED = 1; // seen only by the TV "camera"

function housecoatTex() {
  const [c, g] = T.canvas(256);
  g.fillStyle = '#9a7a8a'; g.fillRect(0, 0, 256, 256);
  for (let y = 0; y < 256; y += 32) for (let x = 0; x < 256; x += 32) {
    g.fillStyle = 'rgba(200,150,90,0.6)'; g.beginPath(); g.arc(x + ((y / 32) % 2) * 16, y, 5, 0, Math.PI * 2); g.fill();
    g.fillStyle = 'rgba(90,120,70,0.5)'; g.beginPath(); g.ellipse(x + 8, y + 8, 3, 6, 0.8, 0, Math.PI * 2); g.fill();
  }
  T.grain(g, 256, 256, 0.08, 3);
  return T.tex(c);
}

// The hallucination: grandma hanging in the bedroom. Shown for a second on a flicker.
export function buildHanged(scene, faceMat) {
  const g = new THREE.Group();
  const coat = new THREE.MeshStandardMaterial({map: housecoatTex(), roughness: 1, side: THREE.DoubleSide});
  const skin = new THREE.MeshStandardMaterial({color: 0x9a8a7c, roughness: 0.7});
  const body = new THREE.Mesh(lathe([[0, 0], [0.2, 0.02], [0.22, 0.4], [0.2, 0.8], [0.22, 1.05], [0.16, 1.18], [0.06, 1.24], [0, 1.25]], 20), coat);
  body.scale.z = 0.75; g.add(body);
  const head = new THREE.Group(); head.position.set(0, 1.36, 0.02); head.rotation.set(0.55, 0, 0.5); g.add(head);
  head.add(new THREE.Mesh(new THREE.SphereGeometry(0.1, 16, 12), skin));
  const face = new THREE.Mesh(new THREE.SphereGeometry(0.102, 16, 12, Math.PI / 2 - 0.9, 1.8, 0.4, 1.4), faceMat); head.add(face);
  const scarf = new THREE.Mesh(new THREE.SphereGeometry(0.118, 16, 12, 0, Math.PI * 2, 0, Math.PI * 0.55), new THREE.MeshStandardMaterial({color: 0x6a2a2a, roughness: 1})); scarf.rotation.x = -0.5; head.add(scarf);
  for (const s of [-1, 1]) {
    const arm = new THREE.Mesh(cyl(0.035, 0.03, 0.6, 8), coat); arm.position.set(s * 0.21, 0.82, 0); arm.rotation.z = s * 0.06; g.add(arm);
    const hand = new THREE.Mesh(new THREE.SphereGeometry(0.035, 8, 6), skin); hand.position.set(s * 0.23, 0.5, 0); hand.scale.y = 1.6; g.add(hand);
    const leg = new THREE.Mesh(cyl(0.04, 0.035, 0.45, 8), skin); leg.position.set(s * 0.08, -0.2, 0); g.add(leg);
  }
  // one slipper still on, one fallen
  const slip = new THREE.MeshStandardMaterial({color: 0x6a2a24, roughness: 1});
  const s1 = new THREE.Mesh(rboxGeo(0.09, 0.05, 0.24, 0.03), slip); s1.position.set(0.08, -0.45, 0.06); g.add(s1);
  // rope to the ceiling hook
  const rope = new THREE.Mesh(tube([[0, 1.3, -0.04], [0, 1.45, -0.06], [0, 2.3, 0]], 0.012, 8, 6), new THREE.MeshStandardMaterial({color: 0x8a7a5a, roughness: 1}));
  g.add(rope);
  g.traverse(o => { if (o.isMesh) { o.castShadow = true; } });
  g.visible = false; scene.add(g);
  // knocked-over stool and the other slipper stay after she disappears
  const props = new THREE.Group();
  const wood = new THREE.MeshStandardMaterial({color: 0x8a6a48, roughness: 0.7});
  const stool = new THREE.Group();
  stool.add(new THREE.Mesh(rboxGeo(0.32, 0.03, 0.32, 0.008), wood));
  for (const [dx, dz] of [[-0.12, -0.12], [0.12, -0.12], [-0.12, 0.12], [0.12, 0.12]]) { const l = new THREE.Mesh(rboxGeo(0.035, 0.44, 0.035, 0.005), wood); l.position.set(dx, -0.22, dz); stool.add(l); }
  stool.rotation.z = Math.PI / 2; stool.position.set(0.35, 0.17, 0.1); props.add(stool);
  const s2 = new THREE.Mesh(rboxGeo(0.09, 0.05, 0.24, 0.03), slip); s2.position.set(-0.2, 0.025, 0.25); s2.rotation.y = 1.2; props.add(s2);
  props.visible = false; scene.add(props);
  return {group: g, props, place(x, z) { g.position.set(x, 0.62, z); props.position.set(x, 0, z); }};
}

// A dark seated silhouette in the armchair: only the TV feed camera sees it.
export function buildSelf(scene, x, y, z, ry) {
  const g = new THREE.Group();
  const m = new THREE.MeshStandardMaterial({color: 0x2a2622, roughness: 1});
  const torso = new THREE.Mesh(new THREE.CapsuleGeometry(0.17, 0.4, 4, 10), m); torso.position.y = 0.45; torso.rotation.x = -0.2; g.add(torso);
  const head = new THREE.Mesh(new THREE.SphereGeometry(0.1, 12, 10), new THREE.MeshStandardMaterial({color: 0x7a6656, roughness: 0.8})); head.position.set(0, 0.86, -0.04); g.add(head);
  const hair = new THREE.Mesh(new THREE.SphereGeometry(0.105, 12, 10, 0, Math.PI * 2, 0, Math.PI / 2), m); hair.position.copy(head.position); hair.rotation.x = -0.4; g.add(hair);
  for (const s of [-1, 1]) { const thigh = new THREE.Mesh(new THREE.CapsuleGeometry(0.07, 0.35, 4, 8), m); thigh.rotation.x = Math.PI / 2; thigh.position.set(s * 0.1, 0.25, 0.2); g.add(thigh); const shin = new THREE.Mesh(new THREE.CapsuleGeometry(0.06, 0.35, 4, 8), m); shin.position.set(s * 0.1, 0.02, 0.42); g.add(shin); }
  g.position.set(x, y, z); g.rotation.y = ry;
  g.traverse(o => o.layers.set(LAYER_FEED));
  scene.add(g);
  return g;
}

// TV feed: a camera in the TV looking at the room; renders layer 0 + the feed-only layer.
export class Feed {
  constructor(renderer, scene) {
    this.r = renderer; this.scene = scene;
    this.rt = new THREE.WebGLRenderTarget(256, 192);
    this.rt.texture.colorSpace = THREE.LinearSRGBColorSpace;
    this.cam = new THREE.PerspectiveCamera(62, 4 / 3, 0.05, 30);
    this.cam.layers.enable(LAYER_FEED);
    this.active = false; this.n = 0;
  }
  aim(from, to) { this.cam.position.copy(from); this.cam.lookAt(to); }
  render() {
    if (!this.active || this.n++ % 3) return;
    const old = this.r.getRenderTarget();
    const tm = this.r.toneMapping;
    this.r.setRenderTarget(this.rt); this.r.render(this.scene, this.cam);
    this.r.setRenderTarget(old); this.r.toneMapping = tm;
  }
}

export function setLayerDeep(obj, layer, also0 = false) {
  obj.traverse(o => { o.layers.set(layer); if (also0) o.layers.enable(0); });
}
export {LAYER_FEED};
