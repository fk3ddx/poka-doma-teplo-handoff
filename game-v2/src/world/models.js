import * as THREE from 'three';
import {GLTFLoader} from 'three/addons/loaders/GLTFLoader.js';

const urls = import.meta.glob('../assets/models/*.glb', {eager: true, query: '?url', import: 'default'});
const loaded = {};

export async function loadModels(onProgress) {
  const loader = new GLTFLoader();
  const entries = Object.entries(urls);
  let done = 0;
  await Promise.all(entries.map(async ([path, url]) => {
    const name = path.split('/').pop().replace('.glb', '');
    try {
      const g = await loader.loadAsync(url);
      g.scene.traverse(o => {
        if (o.isMesh) {
          o.castShadow = true; o.receiveShadow = true;
          const m = o.material;
          if (m.map) m.map.anisotropy = 4;
          m.envMapIntensity = 0.6;
        }
      });
      loaded[name] = g.scene;
    } catch (e) { console.warn('model failed', name, e); }
    onProgress?.(++done / entries.length);
  }));
  return loaded;
}

// Clone a model, optionally pick a child by name substring, scale to target size.
export function model(name, {child = null, height = null, width = null, scale = 1} = {}) {
  const src = loaded[name];
  if (!src) return new THREE.Group();
  let obj = src;
  if (child) {
    src.traverse(o => { if (!obj || obj === src) if (o.name.toLowerCase().includes(child.toLowerCase())) obj = o; });
  }
  const c = obj.clone(true);
  c.position.set(0, 0, 0); c.rotation.set(0, 0, 0); c.updateMatrixWorld(true);
  const box = new THREE.Box3().setFromObject(c);
  const size = box.getSize(new THREE.Vector3());
  let s = scale;
  if (height) s = height / size.y; else if (width) s = width / Math.max(size.x, size.z);
  const wrap = new THREE.Group();
  c.scale.multiplyScalar(s);
  // put pivot at bottom centre
  c.position.set(-(box.min.x + size.x / 2) * s, -box.min.y * s, -(box.min.z + size.z / 2) * s);
  wrap.add(c);
  wrap.userData.size = size.clone().multiplyScalar(s);
  return wrap;
}
export function names(name) { const out = []; loaded[name]?.traverse(o => out.push(o.name)); return out; }
