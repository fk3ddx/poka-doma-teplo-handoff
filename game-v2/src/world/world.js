import * as THREE from 'three';
import {Batcher, Builder} from './geo.js';
import {materials} from './materials.js';
import {model} from './models.js';
import {buildApartment} from './apartment.js';
import {buildLiving} from './props/living.js';
import {buildKitchen} from './props/kitchen.js';
import {buildHall} from './props/hall.js';
import {buildDoors, interiorLeaf} from './doors.js';
import {buildExterior} from './exterior.js';
import {TV} from './tv.js';

// Builds the whole static world and returns a context with handles to dynamic objects.
export function buildWorld(scene, col, extra = {}) {
  const M = materials();
  const batch = new Batcher();
  const B = new Builder(batch);
  const statics = new THREE.Group(); statics.name = 'statics'; scene.add(statics);
  const ctx = {
    scene, col, M, B, batch, ...extra,
    placeModel(name, opts, x, y, z, ry = 0, builder = null) {
      const o = model(name, opts);
      const local = new THREE.Matrix4().compose(new THREE.Vector3(x, y, z), new THREE.Quaternion().setFromEuler(new THREE.Euler(0, ry, 0)), new THREE.Vector3(1, 1, 1));
      const m = builder ? builder.m.clone().multiply(local) : local;
      m.decompose(o.position, o.quaternion, o.scale);
      scene.add(o);
      return o;
    },
  };
  buildApartment(scene, col);
  buildLiving(ctx);
  buildKitchen(ctx);
  buildHall(ctx);
  ctx.doors = buildDoors(scene, col, M);
  // the door that was never there (appears after several loops)
  {
    const g = new THREE.Group(); interiorLeaf(M)(g, 0.72, 1.98, 0.04);
    const frame = new THREE.Mesh(new THREE.BoxGeometry(0.86, 2.06, 0.03), M.doorPaint); frame.position.set(0.36, 1.03, -0.02); g.add(frame);
    g.position.set(5.75, 0, 5.985); g.rotation.y = Math.PI; scene.add(g); g.visible = false;
    g.traverse(o => { if (o.isMesh) o.castShadow = true; });
    ctx.extraDoor = g;
  }
  ctx.tv = new TV(scene, M, {x: 6.27, y: 0.565, z: 1.8, ry: -Math.PI / 2});
  batch.build(statics);
  buildExterior(ctx);
  return ctx;
}
