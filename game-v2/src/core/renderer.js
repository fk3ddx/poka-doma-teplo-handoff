import * as THREE from 'three';
import {EffectComposer} from 'three/addons/postprocessing/EffectComposer.js';
import {RenderPass} from 'three/addons/postprocessing/RenderPass.js';
import {ShaderPass} from 'three/addons/postprocessing/ShaderPass.js';
import {OutputPass} from 'three/addons/postprocessing/OutputPass.js';
import {UnrealBloomPass} from 'three/addons/postprocessing/UnrealBloomPass.js';
import {RoomEnvironment} from 'three/addons/environments/RoomEnvironment.js';
import {N8AOPass} from 'n8ao';

// Final "camcorder tape" look: soft image, grain, a little chroma bleed, vignette,
// subtle grading. Everything is gentle by default — the picture comes first.
const FilmShader = {
  uniforms: {
    tDiffuse: {value: null},
    uTime: {value: 0},
    uRes: {value: new THREE.Vector2(1, 1)},
    uVhs: {value: 0.6},         // overall tape strength (option)
    uBright: {value: 1},
    uRewind: {value: 0},        // 0..1 rewind distortion
    uGlitch: {value: 0},        // 0..1 death glitch
    uFear: {value: 0},          // 0..1 tension: desaturate + tighter vignette
    uWarm: {value: 1},          // grading warmth
    uFish: {value: 0},          // peephole lens
  },
  vertexShader: `varying vec2 vUv; void main(){ vUv = uv; gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.0); }`,
  fragmentShader: `
    uniform sampler2D tDiffuse; uniform float uTime, uVhs, uBright, uRewind, uGlitch, uFear, uWarm, uFish; uniform vec2 uRes;
    varying vec2 vUv;
    float hash(vec2 p){ p = fract(p*vec2(123.34, 456.21)); p += dot(p, p+45.32); return fract(p.x*p.y); }
    float noise(float x){ float i=floor(x); float f=fract(x); return mix(hash(vec2(i,0.)), hash(vec2(i+1.,0.)), smoothstep(0.,1.,f)); }
    vec3 sampleSoft(vec2 uv, float spread){
      vec2 px = spread / uRes;
      vec3 c = texture2D(tDiffuse, uv).rgb * 0.4;
      c += texture2D(tDiffuse, uv + vec2(px.x, 0.)).rgb * 0.15;
      c += texture2D(tDiffuse, uv - vec2(px.x, 0.)).rgb * 0.15;
      c += texture2D(tDiffuse, uv + vec2(0., px.y)).rgb * 0.15;
      c += texture2D(tDiffuse, uv - vec2(0., px.y)).rgb * 0.15;
      return c;
    }
    void main(){
      vec2 uv = vUv;
      float t = uTime;
      float fishMask = 1.0;
      if (uFish > 0.0) {
        vec2 c = (vUv - 0.5) * vec2(uRes.x / uRes.y, 1.0);
        float r = length(c);
        uv = 0.5 + (vUv - 0.5) * mix(1.0, 0.5 + 1.6 * r * r, uFish);
        fishMask = mix(1.0, smoothstep(0.47, 0.4, r), uFish);
      }
      float v = uVhs;
      // horizontal line jitter (tape), stronger during rewind/glitch
      float line = floor(uv.y * uRes.y / 2.0);
      float jit = (hash(vec2(line, floor(t*30.))) - 0.5) * (0.0006*v + uRewind*0.02 + uGlitch*0.03);
      // slow tracking band
      float bandPos = fract(t*0.045 + noise(t*0.3)*0.2);
      float band = smoothstep(0.035, 0.0, abs(uv.y - bandPos)) * (0.25*v + uRewind);
      jit += band * 0.004 * (hash(vec2(line, t)) - 0.5);
      // rewind: big skew bands
      if (uRewind > 0.0) {
        float rb = step(0.92, fract(uv.y*3.0 + t*2.7));
        jit += rb * uRewind * 0.05;
      }
      uv.x += jit;
      // chroma bleed
      float ca = (0.0012 * v + uGlitch*0.012 + uRewind*0.006);
      vec3 col;
      col.r = sampleSoft(uv + vec2(ca, 0.), 0.8).r;
      col.g = sampleSoft(uv, 0.8).g;
      col.b = sampleSoft(uv - vec2(ca, 0.), 0.8).b;
      // grading: gentle filmic lift, warm highlights / cool shadows
      float lum = dot(col, vec3(0.299, 0.587, 0.114));
      vec3 shadowTint = vec3(0.92, 0.98, 1.06);
      vec3 highTint = mix(vec3(1.0), vec3(1.06, 1.0, 0.9), uWarm);
      col *= mix(shadowTint, highTint, smoothstep(0.05, 0.6, lum));
      col = mix(col, vec3(lum), uFear * 0.45);
      col = col * uBright;
      col = col + vec3(0.012, 0.011, 0.014) * v; // black lift like tape
      // grain
      float g = hash(uv * uRes + fract(t*7.13)*100.0) - 0.5;
      col += g * (0.035 + 0.03*v + uGlitch*0.2) * (1.0 - lum*0.5);
      // scanlines (very faint)
      col *= 1.0 - v * 0.035 * (0.5 + 0.5*sin(uv.y * uRes.y * 3.14159));
      col += band * 0.05 * vec3(1.0);
      // vignette
      vec2 q = vUv - 0.5;
      float vig = smoothstep(0.85 - uFear*0.2, 0.25, length(q*vec2(1.0, 0.9)));
      col *= mix(0.55, 1.0, vig);
      // glitch blocks
      if (uGlitch > 0.0) {
        float blk = step(1.0 - uGlitch*0.5, hash(floor(vUv*vec2(12.0, 30.0)) + floor(t*20.)));
        col = mix(col, vec3(hash(vUv+t)), blk*0.8);
      }
      if (uRewind > 0.0) {
        col = mix(col, col.bgr*vec3(0.9,1.0,1.1), uRewind*0.3);
        col += step(0.985, hash(vec2(line*0.37, floor(t*50.)))) * uRewind * 0.6;
      }
      if (uFish > 0.0) col = mix(col, col * vec3(0.95, 1.0, 0.85), uFish) * fishMask;
      gl_FragColor = vec4(col, 1.0);
    }`,
};

export class Renderer {
  constructor(canvas) {
    this.canvas = canvas;
    const r = this.r = new THREE.WebGLRenderer({canvas, antialias: false, powerPreference: 'high-performance', stencil: false});
    r.outputColorSpace = THREE.SRGBColorSpace;
    r.toneMapping = THREE.AgXToneMapping;
    r.toneMappingExposure = 1.0;
    r.shadowMap.enabled = true;
    r.shadowMap.type = THREE.PCFSoftShadowMap;
    r.shadowMap.autoUpdate = false;
    this.shadowFrames = 3;
    this.scale = 0.75;
    this.quality = 'medium';
  }

  setup(scene, camera) {
    this.scene = scene; this.camera = camera;
    const pm = new THREE.PMREMGenerator(this.r);
    scene.environment = pm.fromScene(new RoomEnvironment(), 0.04).texture;
    scene.environmentIntensity = 0.18;
    const w = innerWidth, h = innerHeight;
    this.composer = new EffectComposer(this.r, new THREE.WebGLRenderTarget(w, h, {type: THREE.HalfFloatType}));
    this.ao = new N8AOPass(scene, camera, w, h);
    Object.assign(this.ao.configuration, {aoRadius: 0.45, distanceFalloff: 0.6, intensity: 2.6, halfRes: true, gammaCorrection: false, aoSamples: 12, denoiseSamples: 6, denoiseRadius: 8, color: new THREE.Color(0, 0, 0)});
    this.renderPass = new RenderPass(scene, camera);
    this.bloom = new UnrealBloomPass(new THREE.Vector2(w / 2, h / 2), 0.16, 0.5, 0.95);
    this.film = new ShaderPass(FilmShader);
    this.composer.addPass(this.ao);
    this.composer.addPass(this.renderPass); this.renderPass.enabled = false;
    this.composer.addPass(this.bloom);
    this.composer.addPass(new OutputPass());
    this.composer.addPass(this.film);
    this.resize();
    addEventListener('resize', () => this.resize());
  }

  setQuality(q) {
    this.quality = q;
    this.scale = q === 'low' ? 0.55 : q === 'medium' ? 0.75 : 1.0;
    const useAO = q !== 'low';
    this.ao.enabled = useAO; this.renderPass.enabled = !useAO;
    this.ao.configuration.halfRes = q !== 'high';
    this.r.shadowMap.type = q === 'high' ? THREE.PCFSoftShadowMap : THREE.PCFShadowMap;
    this.resize();
  }

  resize() {
    const w = innerWidth, h = innerHeight;
    const pr = Math.min(devicePixelRatio, 1.5) * this.scale;
    this.r.setPixelRatio(pr);
    this.r.setSize(w, h, false);
    this.composer.setPixelRatio(pr);
    this.composer.setSize(w, h);
    this.film.uniforms.uRes.value.set(w * pr, h * pr);
    this.camera.aspect = w / h; this.camera.updateProjectionMatrix();
  }

  markShadows(frames = 1) { this.shadowFrames = Math.max(this.shadowFrames, frames); }

  render(dt, t) {
    if (this.shadowFrames > 0) { this.r.shadowMap.needsUpdate = true; this.shadowFrames--; }
    this.film.uniforms.uTime.value = t;
    this.composer.render(dt);
  }
}
