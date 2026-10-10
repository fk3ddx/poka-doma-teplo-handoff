// v3 walkthrough: loop 0 must end in death; loop 1: belt + tie balcony, ignore fake call,
// answer the coded call, hide, hold breath, survive → ending face-peel → final card.
import {chromium} from 'playwright';
const browser = await chromium.launch({args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--autoplay-policy=no-user-gesture-required']});
const page = await browser.newPage({viewport: {width: 960, height: 540}});
const errors = [];
page.on('console', m => { const t = m.text(); if (m.type() === 'error' && !t.includes('pointer')) errors.push(t.slice(0, 300)); });
page.on('pageerror', e => errors.push('pageerror: ' + e.message));
const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);
const shot = async n => { await page.screenshot({path: `/tmp/shots/v3_${n}.png`, timeout: 120000}); log('shot', n); };
const ev = fn => page.evaluate(fn);
const act = async (fn, wait = 800) => { await page.evaluate(`(${fn.toString()})(); null`); await page.waitForTimeout(wait); };
const waitFor = (fn, t = 900000) => page.waitForFunction(fn, null, {timeout: t, polling: 500});
const state = () => ev(() => { const g = window.__game; return JSON.stringify({phase: g.phase, loop: g.loop, deaths: g.deaths, ringing: g.ringing, clock: g.timeStr(), at: g.creatureAt, hidden: g.hidden, memory: g.memory, tied: g.flags.tied}); });
const answer = async () => {
  await act(() => window.__game.answerPhone(), 600);
  for (let i = 0; i < 60; i++) {
    if (await ev(() => document.getElementById('choices').classList.contains('visible'))) await page.keyboard.press('Digit1');
    if (!(await ev(() => window.__game.inCall))) break;
    await page.waitForTimeout(1000);
  }
};

await page.goto((process.env.URL || 'http://127.0.0.1:5174/') + '?q=low');
await page.waitForFunction(() => window.__ready === true, null, {timeout: 180000});
await page.click('#btn-start');
await waitFor(() => window.__game.phase === 'arrive', 120000);
await act(() => { window.__fixedDt = 0.5; const g = window.__game; g.flags = {...g.flags, flashlight: true, slippers: true, tea: true}; g.ambience(); g.phase = 'warm'; g.startEvent(true); }, 1500);
log('loop0 start', await state());
// loop 0: try to close the balcony (latch is broken), answer call1, then just wait
await waitFor(() => window.__game.ringing === 'call1');
await answer();
await act(() => { const g = window.__game; g.player.stand(4.6, 1.0, 0); g.balconyToggle(); }, 4000);
log('after latch try', await state());
await waitFor(() => window.__game.phase === 'death' || window.__game.loop >= 1);
await page.waitForTimeout(1200); await shot('01_loop0_death');
await waitFor(() => window.__game.phase === 'event' && window.__game.loop === 1, 300000);
await page.waitForTimeout(1500); await shot('02_loop1_cold');
log('loop1', await state());
// loop 1: belt, tie balcony
await act(() => { const g = window.__game; g.player.stand(2.0, 2.3, -Math.PI / 2); g.flags.belt = true; g.player.stand(4.6, 1.0, 0); g.balconyToggle(); }, 2500);
log('tied?', await state());
await waitFor(() => window.__game.ringing === 'call1');
await answer();
await waitFor(() => !window.__game.lights.mains);
// TV feed scare: look at the TV
await act(() => { const g = window.__game; g.player.stand(4.4, 2.0, -Math.PI / 2 + 0.05, 0); g.player.pitch = -0.08; }, 500);
await waitFor(() => window.__game.feed.active, 120000);
await page.waitForTimeout(3000); await shot('03_tv_feed');
// fake call: ignore. wait for coded call2
await waitFor(() => window.__game.ringing === 'call2');
await answer();
log('call2 done', await state());
await waitFor(() => window.__game.canHide);
await act(() => window.__game.hide('bed'), 1500);
await shot('04_hidden_bed');
// hold breath once it stands next to the hiding place
await waitFor(() => { const g = window.__game, c = g.creature; return c.visible && Math.hypot(c.pos.x - 1.45, c.pos.z - 3.0) < 3.0; });
await page.keyboard.down('Space');
await page.waitForTimeout(1500); await shot('05_breath_hold');
await waitFor(() => window.__game.lights.mains || window.__game.phase === 'death', 300000);
await page.keyboard.up('Space');
log('after search', await state());
await waitFor(() => window.__game.ringing === 'end' || window.__game.phase === 'death', 300000);
if (await ev(() => window.__game.ringing === 'end')) await answer();
await waitFor(() => window.__game.creature.visible && window.__game.creature.disguised, 300000);
await page.waitForTimeout(2500); await shot('06_grandma');
await waitFor(() => window.__game.film.uTear.value > 0.4, 300000);
await shot('07_face_rip');
await waitFor(() => window.__game.phase === 'credits', 300000);
await page.waitForTimeout(2500); await shot('08_end');
log('end', await state());
console.log('ERRORS:', JSON.stringify(errors));
await browser.close();
