// Automated walkthrough with state checks and screenshots at key beats.
// node tools/qa-flow.mjs [warm|event|death|win]
import {chromium} from 'playwright';
const mode = process.argv[2] || 'all';
const browser = await chromium.launch({args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--autoplay-policy=no-user-gesture-required']});
const page = await browser.newPage({viewport: {width: 1280, height: 720}});
const errors = [];
page.on('console', m => { const t = m.text(); if (m.type() === 'error' && !t.includes('pointer')) errors.push(t.slice(0, 300)); });
page.on('pageerror', e => errors.push('pageerror: ' + e.message));
const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);
const shot = async name => { await page.screenshot({path: `/tmp/shots/qa_${name}.png`, timeout: 120000}); log('shot', name); };
const ev = (fn, arg) => page.evaluate(fn, arg);
const waitFor = (fn, arg, t = 600000) => page.waitForFunction(fn, arg, {timeout: t, polling: 500});
const state = () => ev(() => { const g = window.__game; return {phase: g.phase, loop: g.loop, deaths: g.deaths, ringing: g.ringing, inCall: g.inCall, clock: g.timeStr(), creatureAt: g.creatureAt, cv: g.creature.visible, objectives: g.ui.objectives.map(o => o.text)}; });

await page.goto((process.env.URL || 'http://127.0.0.1:5174/') + '?q=low');
await page.waitForFunction(() => window.__ready === true, null, {timeout: 180000});
await page.click('#btn-start');
log('started');
await waitFor(() => window.__game.phase === 'arrive', null, 120000);
await ev(() => { window.__fixedDt = 0.25; });
await page.waitForTimeout(2500);
await shot('01_landing');
log(await state());

const skipWarm = mode.startsWith('fast');
const act = async (code, wait = 1500) => { await page.evaluate(`(${code.toString()})(); null`); await page.waitForTimeout(wait); };
if (!skipWarm) {
// --- warm phase via direct actions
await act(() => window.__game.frontAction(), 6000);
await act(() => window.__game.player.teleport(8.05, 5.2, 0), 3000);
await act(() => window.__game.frontAction(), 6000);
log(await state());
await act(() => { const g = window.__game; g.player.teleport(7.4, 3.75, Math.PI / 2, 0.1); }, 2500);
await shot('02_hall_mirror');
await act(() => window.__game.readNote(), 2500);
await shot('03_note');
await act(() => window.__game.closeNote(), 1000);
await act(() => window.__game.slippers(), 800);
await act(() => window.__game.hangCoat(), 800);
await waitFor(() => window.__game.ringing === 'call0', null, 60000);
log('phone ringing', await state());
await act(() => { window.__game.player.teleport(7.3, 3.75, Math.PI / 2, -0.25); window.__game.answerPhone(); }, 2500);
await shot('04_call0');
// answer choices as they come
for (let i = 0; i < 40; i++) {
  const s = await ev(() => document.getElementById('choices').classList.contains('visible'));
  if (s) await page.keyboard.press('Digit1');
  if (!(await ev(() => window.__game.inCall))) break;
  await page.waitForTimeout(1500);
}
log('after call0', await state());
await act(() => { const g = window.__game; g.flags.matches = true; g.world.matches.visible = false; g.player.teleport(7.9, 1.9, -Math.PI / 2, -0.35); }, 1500);
await act(() => window.__game.stoveAction(), 2500);
await shot('05_stove');
await act(() => { const g = window.__game; g.lights.toggle('kitchen'); g.onSwitch('kitchen', true); g.lights.toggle('kitchen'); g.onSwitch('kitchen', false); g.lights.toggle('kitchen'); g.onSwitch('kitchen', true); g.player.teleport(7.7, 1.0, 0, -0.05); }, 4000);
await shot('06_kitchen_window_blink');
await act(() => { window.__game.player.teleport(4.6, 2.4, -Math.PI / 2, -0.05); window.__game.tvToggle(); }, 3000);
await shot('07_tv_concert');
await act(() => { const g = window.__game; g.flags.pirozhok = true; g.done('pirozhok'); }, 300);
await waitFor(() => window.__game.kettleT > 38, null, 300000);
await act(() => window.__game.stoveAction(), 1500);
await act(() => window.__game.takeFlashlight(), 500);
log('warm done?', await state());
await act(() => window.__game.sitArmchair(), 2500);
await shot('08_armchair');
}
if (mode === 'warm') { console.log(errors); await browser.close(); process.exit(0); }

if (skipWarm) { await act(() => { const g = window.__game; g.flags = {...g.flags, flashlight: true, slippers: true, tea: true}; g.ambience(); g.phase = 'warm'; g.startEvent(true); }, 1000); }
else { await act(() => window.__game.rest(), 1000); }
// --- rest -> event
await waitFor(() => window.__game.phase === 'event', null, 60000);
log('event', await state());
await waitFor(() => window.__game.world.tv.mode === 'testcard', null, 300000);
await page.waitForTimeout(4000);
await shot('09_announcement');
await waitFor(() => window.__game.ringing === 'call1', null, 300000);
await act(() => window.__game.answerPhone(), 500);
for (let i = 0; i < 40; i++) {
  if (await ev(() => document.getElementById('choices').classList.contains('visible'))) await page.keyboard.press('Digit1');
  if (!(await ev(() => window.__game.inCall))) break;
  await page.waitForTimeout(1500);
}
log('call1 done', await state());
// close the balcony now (memory!) — first loop we leave it open to test the balcony death if mode === 'death'
if (!mode.endsWith('death')) await act(() => window.__game.balconyToggle(), 1500);
await act(() => window.__game.player.stand(4.0, 1.0, 0), 500);
await act(() => window.__game.player.teleport(4.0, 1.0, 0, 0), 500);
await waitFor(() => !window.__game.lights.mains, null, 300000);
await page.waitForTimeout(2000);
await shot('10_blackout_yard');
await act(() => { const g = window.__game; g.hands.hold('flashlight'); g.hands.charge = 1; g.player.teleport(7.6, 4.4, 0.3, 0); }, 2000);
await shot('11_flashlight_hall');
await waitFor(() => window.__game.creatureAt === 'door', null, 300000);
await act(() => { const g = window.__game; g.player.teleport(8.05, 5.65, Math.PI, 0.05); }, 1000);
await act(() => window.__game.peep(), 6000);
await shot('12_peephole');
await page.waitForTimeout(6000);
await shot('12b_peephole_turn');
await act(() => window.__game.peeping && window.__game.unpeep(), 500);
log('door phase', await state());
if (mode.endsWith('death')) {
  await waitFor(() => window.__game.creatureAt === 'inside' || window.__game.phase === 'death', null, 400000);
  await act(() => { const g = window.__game; g.player.teleport(4.4, 2.6, 0.6, 0); }, 3000);
  await shot('13_balcony_entry');
  await waitFor(() => window.__game.phase === 'death', null, 300000);
  await page.waitForTimeout(800);
  await shot('14_death');
  await waitFor(() => window.__game.phase === 'event' && window.__game.loop === 1, null, 120000);
  await page.waitForTimeout(1500);
  await shot('15_rewound');
  log('after death', await state());
  console.log(errors); await browser.close(); process.exit(0);
}
// fake call: ignore it
await waitFor(() => window.__game.creatureAt === 'balcony' || window.__game.creatureAt === 'roof', null, 400000);
await act(() => { const g = window.__game; g.player.teleport(4.6, 1.6, 0.2, 0.05); }, 2500);
await shot('13_balcony_visit');
await waitFor(() => window.__game.ringing === 'call2', null, 400000);
await act(() => { window.__game.player.teleport(7.3, 3.75, Math.PI / 2, 0); window.__game.answerPhone(); }, 500);
for (let i = 0; i < 40; i++) {
  if (await ev(() => document.getElementById('choices').classList.contains('visible'))) await page.keyboard.press('Digit1');
  if (!(await ev(() => window.__game.inCall))) break;
  await page.waitForTimeout(1500);
}
log('call2 done', await state());
await waitFor(() => window.__game.flags.insideNow, null, 300000);
await page.waitForTimeout(3000);
await shot('14_inside_eyes_open');
await page.keyboard.down('KeyC');
await page.waitForTimeout(2000);
await shot('15_eyes_closed');
await waitFor(() => window.__game.lights.mains, null, 400000);
await page.waitForTimeout(1500);
await page.keyboard.up('KeyC');
log('survived?', await state());
await waitFor(() => window.__game.ringing === 'end', null, 300000);
await act(() => window.__game.answerPhone(), 500);
for (let i = 0; i < 40; i++) {
  if (await ev(() => document.getElementById('choices').classList.contains('visible'))) await page.keyboard.press('Digit1');
  if (!(await ev(() => window.__game.inCall))) break;
  await page.waitForTimeout(1500);
}
await waitFor(() => window.__game.phase === 'credits' || document.getElementById('card').classList.contains('visible'), null, 300000);
await page.waitForTimeout(3000);
await shot('16_end');
log('end', await state());
console.log('ERRORS:', errors);
await browser.close();
