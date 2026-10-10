export class Sound {
  constructor(){this.ctx=null;this.volume=.5;this.nodes=[];this.dark=false;this.music=true;this.clock=0;this.note=0;}
  start(){
    if(this.ctx){this.ctx.resume();return;}
    this.ctx=new (window.AudioContext||window.webkitAudioContext)();
    this.master=this.ctx.createGain();this.master.gain.value=this.volume;this.master.connect(this.ctx.destination);
    const buffer=this.ctx.createBuffer(1,this.ctx.sampleRate*4,this.ctx.sampleRate),d=buffer.getChannelData(0);
    let last=0;for(let i=0;i<d.length;i++){last=(last+(Math.random()*2-1)*.04)/1.025;d[i]=last*2;}
    const src=this.ctx.createBufferSource();src.buffer=buffer;src.loop=true;
    const filter=this.ctx.createBiquadFilter();filter.type='lowpass';filter.frequency.value=1100;
    this.rain=this.ctx.createGain();this.rain.gain.value=.22;src.connect(filter);filter.connect(this.rain);this.rain.connect(this.master);src.start();
  }
  setVolume(v){this.volume=v;if(this.master)this.master.gain.setTargetAtTime(v,this.ctx.currentTime,.1);}
  tone(freq,duration=.3,volume=.1,type='sine',delay=0){
    if(!this.ctx)return;const t=this.ctx.currentTime+delay,o=this.ctx.createOscillator(),g=this.ctx.createGain();
    o.type=type;o.frequency.setValueAtTime(freq,t);g.gain.setValueAtTime(.0001,t);g.gain.exponentialRampToValueAtTime(Math.max(.0002,volume),t+.012);g.gain.exponentialRampToValueAtTime(.0001,t+duration);o.connect(g);g.connect(this.master);o.start(t);o.stop(t+duration+.05);
  }
  bell(){this.tone(659,1.5,.22);this.tone(523,1.9,.19,'sine',.35);}
  phone(){for(let i=0;i<3;i++){this.tone(880,.12,.10,'triangle',i*.22);this.tone(960,.12,.055,'sine',i*.22+.07);}}
  pipe(){this.tone(190,.14,.2,'triangle');this.tone(75,.3,.12);}
  step(){this.tone(65,.12,.045,'triangle');}
  door(){this.tone(110,.2,.17,'triangle');this.tone(310,.09,.05,'sawtooth',.13);}
  tick(dt,x=0,z=0){
    if(!this.ctx)return;this.clock+=dt;
    this.rain.gain.setTargetAtTime(z<-3?.4:.18,this.ctx.currentTime,.4);
    if(this.dark){if(this.clock>2){this.clock=0;this.tone(43,3,.12);this.tone(44.4,3,.07);}return;}
    if(this.music&&this.clock>2.6){this.clock=0;const notes=[261.63,329.63,392,349.23,293.66,261.63,220,293.66,329.63,261.63,196,220];const f=notes[this.note++%notes.length];this.tone(f,2.3,.033,'sine');this.tone(f/2,2.6,.026);this.tone(f*2,1,.009,'triangle',.02);}
  }
  pause(){this.ctx?.suspend();}
  resume(){this.ctx?.resume();}
}
