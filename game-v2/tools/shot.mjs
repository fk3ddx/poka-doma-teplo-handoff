// Headless screenshots of the running dev server.
// node tools/shot.mjs name "x,z,yaw,pitch[,extra]" [name2 "..."] ...
import {chromium} from 'playwright';
const base = process.env.URL || 'http://127.0.0.1:5173/';
const args = process.argv.slice(2);
const W = +(process.env.W || 1280), Hh = +(process.env.H || 720);
const browser = await chromium.launch({args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--autoplay-policy=no-user-gesture-required']});
const page = await browser.newPage({viewport: {width: W, height: Hh}});
const logs = [];
page.on('console', m => { if (['error', 'warning'].includes(m.type()) || m.text().startsWith('[')) logs.push(m.type() + ': ' + m.text()); });
page.on('pageerror', e => logs.push('pageerror: ' + e.message));
for (let i = 0; i < args.length; i += 2) {
  const name = args[i], spec = args[i + 1];
  const q = new URLSearchParams({shot: spec, q: process.env.Q || 'medium', ...(process.env.EXTRA ? Object.fromEntries(new URLSearchParams(process.env.EXTRA)) : {})});
  await page.goto(base + '?' + q.toString());
  await page.waitForFunction(() => window.__ready === true, null, {timeout: 180000});
  await page.waitForTimeout(+(process.env.WAIT || 1500));
  await page.screenshot({path: `/tmp/shots/${name}.png`, timeout: 120000});
  console.log('saved', name);
}
if (logs.length) console.log(logs.slice(0, 40).join('\n'));
await browser.close();
