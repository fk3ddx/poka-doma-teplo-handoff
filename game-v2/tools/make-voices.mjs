// Generates voice lines with Piper TTS (offline) into src/assets/voice/*.wav (16 kHz mono).
// Requires ~/tts/piper and the ru_RU voices. node tools/make-voices.mjs
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import {execFileSync} from 'node:child_process';
import * as S from '../src/game/script.js';

const ROOT = path.resolve(import.meta.dirname, '..');
const OUT = path.join(ROOT, 'src/assets/voice');
fs.mkdirSync(OUT, {recursive: true});
const TTS = path.join(os.homedir(), 'tts');

const lines = new Map(); // text -> {voice, speed}
const add = (text, voice = 'irina', speed = 1.12) => { if (text && !lines.has(text)) lines.set(text, {voice, speed}); };
for (const l of S.CALL0) if (l.text) add(l.text);
for (const loop of [0, 1]) for (const b of [true, false]) for (const h of [true, false]) for (const l of S.call1(loop, b, h)) if (l.text) add(l.text, 'irina', 1.05);
for (const l of S.FAKE_CALL) add(l.text, 'irina', 1.25);
for (const t of S.DOOR_VOICE) add(t, 'irina', 1.3);
for (const l of S.call2(0)) if (l.text) add(l.text, 'irina', 1.05);
add(S.EAR_VOICE, 'irina', 1.35);
for (const l of S.CALL_END) if (l.text) add(l.text);
for (const t of S.ANNOUNCE) add(t, 'denis', 1.08);
add(S.BALCONY_VOICE, 'irina', 1.35);
add(S.FAKE_MEMORY, 'irina', 1.3);
for (const t of S.GRANDMA_END) add(t, 'irina', 1.15);
for (const t of S.SEARCH_VOICE) add(t, 'irina', 1.45);

export function slug(text) {
  let h = 2166136261;
  for (const c of text) { h ^= c.codePointAt(0); h = Math.imul(h, 16777619) >>> 0; }
  return h.toString(36);
}

function resampleTo16k(buf) {
  const sr = buf.readUInt32LE(24);
  let off = 12; let dataOff = 0, dataLen = 0;
  while (off < buf.length) { const id = buf.toString('ascii', off, off + 4), len = buf.readUInt32LE(off + 4); if (id === 'data') { dataOff = off + 8; dataLen = len; break; } off += 8 + len; }
  const n = dataLen / 2, src = new Float32Array(n);
  for (let i = 0; i < n; i++) src[i] = buf.readInt16LE(dataOff + i * 2) / 32768;
  // simple low-pass (moving average) before decimation
  const lp = new Float32Array(n);
  for (let i = 0; i < n; i++) lp[i] = (src[i] + (src[i - 1] ?? 0) + (src[i + 1] ?? 0)) / 3;
  const ratio = sr / 16000, m = Math.floor(n / ratio);
  const out = Buffer.alloc(44 + m * 2);
  out.write('RIFF', 0); out.writeUInt32LE(36 + m * 2, 4); out.write('WAVE', 8); out.write('fmt ', 12);
  out.writeUInt32LE(16, 16); out.writeUInt16LE(1, 20); out.writeUInt16LE(1, 22); out.writeUInt32LE(16000, 24); out.writeUInt32LE(32000, 28); out.writeUInt16LE(2, 32); out.writeUInt16LE(16, 34);
  out.write('data', 36); out.writeUInt32LE(m * 2, 40);
  // trim/fade edges
  for (let i = 0; i < m; i++) {
    const x = i * ratio, i0 = Math.floor(x), f = x - i0;
    let v = (lp[i0] ?? 0) * (1 - f) + (lp[i0 + 1] ?? 0) * f;
    const fade = Math.min(1, i / 160, (m - i) / 400);
    out.writeInt16LE(Math.max(-32767, Math.min(32767, Math.round(v * fade * 32767))), 44 + i * 2);
  }
  return out;
}

const manifest = {};
for (const [text, {voice, speed}] of lines) {
  const id = slug(text);
  const file = path.join(OUT, id + '.wav');
  manifest[text] = id;
  if (fs.existsSync(file)) continue;
  const spoken = text.replace(/—/g, '').replace(/…/g, '...');
  const tmp = path.join(os.tmpdir(), 'v_' + id + '.wav');
  execFileSync(path.join(TTS, 'piper/piper'), ['--model', path.join(TTS, `ru_RU-${voice}-medium.onnx`), '--length_scale', String(speed), '--sentence_silence', '0.15', '--output_file', tmp], {input: spoken, stdio: ['pipe', 'ignore', 'ignore']});
  fs.writeFileSync(file, resampleTo16k(fs.readFileSync(tmp)));
  console.log(voice, id, text);
}
fs.writeFileSync(path.join(OUT, 'manifest.json'), JSON.stringify(manifest, null, 1));
console.log('lines:', lines.size);
