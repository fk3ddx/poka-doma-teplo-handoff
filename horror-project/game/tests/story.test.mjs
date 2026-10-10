import test from 'node:test';
import assert from 'node:assert/strict';
import {Story} from '../story.js';
function advance(s,seconds,state={}){for(let i=0;i<seconds*20;i++)s.tick(.05,state);}
function ready(){const s=new Story();for(const k of ['coffee','note','phone','window'])s.mark(k);advance(s,86);return s;}
test('opening cannot skip directly to threat, even if player waits',()=>{const s=new Story();advance(s,600);assert.equal(s.stage,'warm');assert.ok(!s.drain().some(e=>e.type==='attack-start'));});
test('all domestic actions and quiet time are required',()=>{const s=new Story();for(const k of ['coffee','note','phone','window'])s.mark(k);advance(s,80);assert.equal(s.stage,'warm');advance(s,6);assert.equal(s.stage,'neighbor');});
test('neighbor and checking the tap precede supernatural warning',()=>{const s=ready();s.interact('tap');assert.equal(s.stage,'neighbor');s.interact('front-door');assert.equal(s.stage,'checking');s.interact('tap');assert.equal(s.stage,'warning');});
test('death rewinds only the dangerous scene and remembers completed chores',()=>{const s=ready();s.interact('front-door');s.interact('tap');advance(s,76);assert.equal(s.stage,'warning');assert.equal(s.loop,1);assert.ok(Object.values(s.flags).every(Boolean));assert.ok(s.drain().some(e=>e.type==='death'));});
test('a closed kitchen saves the player on the first try too',()=>{const s=ready();s.interact('front-door');s.interact('tap');advance(s,76,{inKitchen:true,doorClosed:true});assert.equal(s.stage,'after');assert.equal(s.loop,0);s.interact('phone');assert.equal(s.stage,'free');assert.equal(s.ended,true);});
test('kitchen with open door does not protect player',()=>{const s=new Story({version:1,stage:'attack',loop:1});s.enter('attack');advance(s,16,{inKitchen:true,doorClosed:false});assert.equal(s.loop,2);assert.equal(s.stage,'warning');});
test('reload during attack returns to safe warning checkpoint',()=>{const s=new Story();s.enter('attack');s.loop=2;s.mark('coffee');const restored=new Story(s.checkpoint());assert.equal(restored.stage,'warning');assert.equal(restored.elapsed,0);assert.equal(restored.flags.coffee,true);assert.equal(restored.loop,2);});
test('zero time does not progress story and invalid checkpoints are ignored',()=>{const s=new Story({version:1,stage:'malformed'});s.tick(0);assert.equal(s.elapsed,0);assert.equal(s.stage,'warm');});
