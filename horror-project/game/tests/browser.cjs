const {createRequire}=require('node:module');
const path=require('node:path');const fs=require('node:fs');
const assert=require('node:assert/strict');
let playwright;
try{playwright=require('playwright');}catch{playwright=createRequire('C:/Users/User/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/package.json')('playwright');}
(async()=>{
 const browser=await playwright.chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader','--no-sandbox']});
 const page=await browser.newPage({viewport:{width:1440,height:900}});const errors=[];page.on('pageerror',e=>errors.push(String(e)));
 const out=path.resolve(__dirname,'../qa');fs.mkdirSync(out,{recursive:true});
 await page.goto('http://127.0.0.1:4177/?debug=1');await page.waitForFunction(()=>window.__game);await page.waitForTimeout(1000);
 await page.screenshot({path:path.join(out,'01-menu.png')});
 await page.click('#start');await page.waitForTimeout(1500);
 await page.screenshot({path:path.join(out,'02-start.png')});
 const state=()=>page.evaluate(()=>window.__game.state);
 async function aim(id){await page.evaluate(id=>{const p=window.__game.targets[id];window.__game.lookAt(...p);},id);await page.waitForTimeout(180);}
 async function pressE(){await page.keyboard.press('KeyE');await page.waitForTimeout(200);}
 async function pose(x,z,yaw=0,pitch=0){await page.evaluate(p=>window.__game.pose(...p),[x,z,yaw,pitch]);await page.waitForTimeout(180);}
 async function closeDialog(){while((await state()).mode==='dialog'){await page.click('#dialog-next');await page.waitForTimeout(80);}}
 await aim('coffee');await pressE();assert.equal((await state()).flags.coffee,true,'E must reach the coffee from starting position');
 await aim('note');await pressE();assert.equal((await state()).mode,'reader');await page.click('#reader-close');
 // Walking into the table must stop at its edge, not pass through it.
 await pose(-3.5,-.25,0);await page.keyboard.down('KeyW');await page.waitForTimeout(900);await page.keyboard.up('KeyW');assert.ok((await state()).position[2]>-.73,'table collision');
 await pose(-4.52,-2.8);await aim('phone');await pressE();assert.equal((await state()).mode,'dialog');await closeDialog();
 await pose(-3.25,-3.55);await page.evaluate(()=>window.__game.lookAt(-3.25,1.65,-4.9));await page.waitForTimeout(180);await pressE();assert.equal((await state()).flags.window,true);
 await page.screenshot({path:path.join(out,'03-window.png')});
 await page.evaluate(()=>window.__game.advance(90));assert.equal((await state()).stage,'neighbor');
 await pose(1.4,3.8,Math.PI);await aim('front-door');await pressE();assert.equal((await state()).mode,'dialog');await closeDialog();assert.equal((await state()).stage,'checking');
 await pose(1.3,-1.35,-Math.PI/2);await aim('kitchen-door');await pressE();assert.equal((await state()).doorClosed,false);
 await pose(1.3,-1.35,-Math.PI/2);await page.keyboard.down('KeyW');await page.waitForTimeout(1100);await page.keyboard.up('KeyW');assert.ok((await state()).position[0]>2.6,'can walk through opened kitchen door: '+JSON.stringify(await state()));
 await pose(3.4,-3.2);await aim('tap');await pressE();assert.equal((await state()).stage,'warning');
 await pose(3.0,-.3,-.5);await page.screenshot({path:path.join(out,'04-kitchen.png')});
 await pose(1.4,1.5,Math.PI);await page.evaluate(()=>window.__game.advance(61));assert.equal((await state()).stage,'attack');await page.waitForTimeout(700);
 await page.screenshot({path:path.join(out,'05-attack.png')});
 await page.evaluate(()=>window.__game.advance(16));assert.equal((await state()).loop,1);await page.waitForTimeout(6300);assert.equal((await state()).transition,false);
 // Retry preserves the intro and can be survived; now use the same visible door.
 await pose(1.3,-1.35,-Math.PI/2);await aim('kitchen-door');await pressE();assert.equal((await state()).doorClosed,false);
 await pose(1.3,-1.35,-Math.PI/2);await page.keyboard.down('KeyW');await page.waitForTimeout(1100);await page.keyboard.up('KeyW');await pose(3.1,-1.1,Math.PI/2);await aim('kitchen-door');await pressE();assert.equal((await state()).doorClosed,true);
 await page.evaluate(()=>window.__game.advance(77));assert.equal((await state()).stage,'after');
 await pose(1.4,3.8,Math.PI);await page.screenshot({path:path.join(out,'06-changed-hall.png')});
 await pose(-4.52,-2.8);await aim('phone');await pressE();assert.equal((await state()).mode,'dialog');await closeDialog();assert.equal((await state()).mode,'ending');
 await page.screenshot({path:path.join(out,'07-ending.png')});
 await page.click('#ending-explore');await page.keyboard.press('Escape');await page.waitForTimeout(200);assert.equal((await state()).mode,'pause');const before=(await state()).elapsed;await page.waitForTimeout(500);assert.equal((await state()).elapsed,before,'pause freezes narrative');
 await page.reload();await page.waitForFunction(()=>window.__game);await page.click('#continue');assert.equal((await state()).stage,'free','saved ending restores');
 assert.deepEqual(errors,[]);fs.writeFileSync(path.join(out,'browser-results.json'),JSON.stringify({passed:true,checks:['startup','coffee E','note E','collision','phone dialogue','window','doorbell','neighbor','kitchen door','walk doorway','tap','death','rewind','survival','changed space','ending','pause','save restore'],errors,state:await state()},null,2));
 console.log(fs.readFileSync(path.join(out,'browser-results.json'),'utf8'));
 await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
