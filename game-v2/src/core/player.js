import * as THREE from 'three';

const EYE = 1.52;

export class Player {
  constructor(camera, input, collision) {
    this.cam = camera; this.input = input; this.col = collision;
    this.pos = new THREE.Vector3(0, 0, 0);
    this.yaw = 0; this.pitch = 0;
    this.vel = new THREE.Vector3();
    this.radius = 0.24;
    this.walk = 1.45; this.run = 2.6;
    this.bobT = 0; this.bobAmp = 0; this.bobEnabled = true;
    this.canMove = true; this.canLook = true;
    this.seated = null;        // {pos, yaw, pitch, yawRange, pitchRange}
    this.forced = null;        // forced look target {target: Vector3, speed}
    this.eyeY = EYE; this.crouch = 0;
    this.stepDist = 0; this.onStep = null;
    this.shake = 0;
    this.lookLimit = null;
    this.floorY = 0;
  }

  teleport(x, z, yaw = this.yaw, pitch = 0) {
    this.pos.set(x, this.col.floorAt(x, z) ?? 0, z); this.yaw = yaw; this.pitch = pitch; this.vel.set(0, 0, 0);
    this.floorY = this.pos.y;
  }

  sit(s) { this.sitFrom = this.eyePos(); this.sitT = 0; this.seated = s; this.yaw = s.yaw; this.pitch = s.pitch ?? 0; }
  stand(x, z, yaw) { this.seated = null; this.teleport(x, z, yaw ?? this.yaw, 0); }

  update(dt) {
    const inp = this.input;
    if (this.canLook && !this.forced) {
      const s = 0.0022 * inp.sens;
      this.yaw -= inp.dx * s; this.pitch -= inp.dy * s;
    }
    this.pitch = Math.max(-1.45, Math.min(1.45, this.pitch));
    if (this.seated) {
      const s = this.seated;
      if (s.yawRange != null) {
        let d = this.yaw - s.yaw; d = Math.atan2(Math.sin(d), Math.cos(d));
        this.yaw = s.yaw + Math.max(-s.yawRange, Math.min(s.yawRange, d));
      }
      this.pitch = Math.max(-0.9, Math.min(0.7, this.pitch));
    }
    if (this.forced) {
      const f = this.forced;
      const eye = this.eyePos();
      const dir = f.target.clone().sub(eye);
      const ty = Math.atan2(-dir.x, -dir.z);
      const tp = Math.atan2(dir.y, Math.hypot(dir.x, dir.z));
      let dy = ty - this.yaw; dy = Math.atan2(Math.sin(dy), Math.cos(dy));
      const k = 1 - Math.exp(-dt * (f.speed ?? 6));
      this.yaw += dy * k; this.pitch += (tp - this.pitch) * k;
    }

    let moving = false;
    if (!this.seated && this.canMove && !this.frozen) {
      const f = (inp.down('KeyW') || inp.down('ArrowUp') ? 1 : 0) - (inp.down('KeyS') || inp.down('ArrowDown') ? 1 : 0);
      const r = (inp.down('KeyD') || inp.down('ArrowRight') ? 1 : 0) - (inp.down('KeyA') || inp.down('ArrowLeft') ? 1 : 0);
      const speed = inp.down('ShiftLeft') || inp.down('ShiftRight') ? this.run : this.walk;
      const fw = new THREE.Vector3(-Math.sin(this.yaw), 0, -Math.cos(this.yaw));
      const rt = new THREE.Vector3(Math.cos(this.yaw), 0, -Math.sin(this.yaw));
      const want = fw.multiplyScalar(f).add(rt.multiplyScalar(r));
      if (want.lengthSq() > 0) { want.normalize().multiplyScalar(speed); moving = true; }
      const acc = moving ? 10 : 12;
      this.vel.x += (want.x - this.vel.x) * Math.min(1, dt * acc);
      this.vel.z += (want.z - this.vel.z) * Math.min(1, dt * acc);
      const prev = this.pos.clone();
      this.pos.x += this.vel.x * dt; this.pos.z += this.vel.z * dt;
      this.col.resolve(this.pos, this.radius);
      const fy = this.col.floorAt(this.pos.x, this.pos.z);
      if (fy === null || Math.abs(fy - this.floorY) > 0.45) { this.pos.x = prev.x; this.pos.z = prev.z; }
      else this.floorY += (fy - this.floorY) * Math.min(1, dt * 14);
      this.pos.y = this.floorY;
      const moved = Math.hypot(this.pos.x - prev.x, this.pos.z - prev.z);
      this.stepDist += moved;
      const stride = speed > 2 ? 0.78 : 0.62;
      if (this.stepDist > stride) { this.stepDist = 0; this.onStep?.(this.pos, speed > 2); }
      const sp = Math.hypot(this.vel.x, this.vel.z);
      this.bobT += dt * sp * 4.2;
      this.bobAmp += ((moving ? Math.min(1, sp / this.walk) : 0) - this.bobAmp) * Math.min(1, dt * 6);
    } else {
      this.vel.set(0, 0, 0);
      this.bobAmp *= Math.max(0, 1 - dt * 6);
    }
    this.applyCamera(dt);
  }

  eyePos() {
    if (this.seated) {
      if (this.sitFrom && this.sitT < 1) { const k = this.sitT * this.sitT * (3 - 2 * this.sitT); return this.sitFrom.clone().lerp(this.seated.pos, k); }
      return this.seated.pos.clone();
    }
    return new THREE.Vector3(this.pos.x, this.pos.y + this.eyeY, this.pos.z);
  }

  applyCamera(dt) {
    if (this.seated && this.sitT < 1) this.sitT = Math.min(1, (this.sitT ?? 0) + dt * 1.8);
    const e = this.eyePos();
    const amp = this.bobEnabled ? this.bobAmp : 0;
    const by = Math.sin(this.bobT * 2) * 0.022 * amp;
    const bx = Math.cos(this.bobT) * 0.014 * amp;
    // idle breathing
    const t = performance.now() / 1000;
    const br = Math.sin(t * 1.3) * 0.004;
    this.shake = Math.max(0, this.shake - dt * 1.5);
    const sh = this.shake * this.shake;
    const sx = (Math.random() - 0.5) * 0.03 * sh, sy = (Math.random() - 0.5) * 0.03 * sh;
    const rt = new THREE.Vector3(Math.cos(this.yaw), 0, -Math.sin(this.yaw));
    this.cam.position.copy(e).addScaledVector(rt, bx + sx);
    this.cam.position.y += by + br + sy;
    this.cam.rotation.set(this.pitch + Math.sin(this.bobT * 2) * 0.004 * amp, this.yaw, Math.cos(this.bobT) * 0.004 * amp, 'YXZ');
  }

  forward() { return new THREE.Vector3(0, 0, -1).applyQuaternion(this.cam.quaternion); }
}
