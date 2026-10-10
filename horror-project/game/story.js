// Game time advances only while the player is in the apartment, not in menus.
export class Story {
  constructor(saved) {
    this.stage='warm'; this.elapsed=0; this.total=0; this.loop=0;
    this.flags={coffee:false,note:false,phone:false,window:false,tap:false};
    this.signals=[]; this.events=[]; this.ended=false;
    if(saved?.version===1) {
      const valid=['warm','neighbor','checking','warning','attack','after','free'];
      if(valid.includes(saved.stage)) {
        this.stage=saved.stage==='attack'?'warning':saved.stage;
        this.flags={...this.flags,...saved.flags}; this.loop=Number(saved.loop)||0;
        this.total=Number(saved.total)||0; this.ended=!!saved.ended;
      }
    }
  }
  emit(type,data={}) { this.events.push({type,...data}); }
  drain() { return this.events.splice(0); }
  enter(stage) { this.stage=stage;this.elapsed=0;this.signals=[];this.emit('stage',{stage});this.emit('save'); }
  checkpoint() { return {version:1,stage:this.stage,flags:{...this.flags},loop:this.loop,total:this.total,ended:this.ended}; }
  mark(id) { if(id in this.flags) this.flags[id]=true; this.emit('save'); }
  interact(id) {
    if(id==='coffee') { this.mark('coffee');this.emit('coffee'); }
    if(id==='note') { this.mark('note');this.emit('note'); }
    if(id==='window') { this.mark('window');this.emit('window'); }
    if(id==='phone') {
      if(this.stage==='warm') {this.mark('phone');this.emit('call-home');}
      else if(this.stage==='after') { this.ended=true;this.enter('free');this.emit('ending'); }
      else this.emit('dead-line');
    }
    if(id==='front-door') {
      if(this.stage==='neighbor') {this.enter('checking');this.emit('neighbor-talk');}
      else if(this.stage==='warning') {this.emit('door-voice');}
      else if(this.stage==='attack') {this.emit('door-attack');}
      else this.emit('locked');
    }
    if(id==='tap') {
      this.mark('tap');
      if(this.stage==='checking') { this.enter('warning');this.emit('tap-wrong'); }
      else this.emit('tap');
    }
  }
  tick(dt,{inKitchen=false,doorClosed=false}={}) {
    dt=Math.max(0,Math.min(dt,0.1));this.elapsed+=dt;this.total+=dt;
    const once=(id,t,type,data={})=>{if(this.elapsed>=t&&!this.signals.includes(id)){this.signals.push(id);this.emit(type,data);}};
    if(this.stage==='warm') {
      once('intro',2,'line',{text:'Бабушка уехала к сестре на выходные. Я обещал полить цветы.\nНо в такую погоду домой уже не хотелось.'});
      once('phone',35,'phone-ring');
      if(this.elapsed>=85 && ['coffee','note','phone','window'].every(k=>this.flags[k])) {this.enter('neighbor');this.emit('bell');}
      else if(this.elapsed>110) once('hint',110,'line',{text:'Можно никуда не спешить. Позвоню бабушке, пока она не легла.'});
    }
    if(this.stage==='neighbor') { once('again',35,'bell'); }
    if(this.stage==='warning') {
      once('quiet',4,'line',{text:this.loop?'Тот же мокрый снег. Те же слова по телевизору.\nЭто уже было.':'Кран сухой. А за стеной всё равно слышно воду.'});
      once('bell',15,'bell');
      once('voice',22,'line',{text:this.loop?'Сейчас телевизор скажет про погоду. Потом два щелчка.':'Из-за двери: «Я ключи забыл. Открой».\nГолос был мой.'});
      once('tv',34,'forecast');
      once('hint',41,'line',{text:this.loop?'В прошлый раз свет погас везде, кроме кухни.\nЗакрытая дверь остановила тень. Нужно успеть туда.':'На кухне радио продолжало играть.\nИз-под закрытой двери тянулась тёплая полоска света.'});
      once('click1',48,'pipe');
      once('click2',54,'pipe');
      if(this.elapsed>=59){this.enter('attack');this.emit('attack-start');}
    }
    if(this.stage==='attack') {
      once('step',3,'footsteps');
      once('door',7,'line',{text:inKitchen&&doorClosed?'Оно остановилось за дверью. Не открывать.':'Шаги были уже в квартире.'});
      if(this.elapsed>=15) {
        if(inKitchen&&doorClosed) {this.enter('after');this.emit('survived');}
        else {this.loop++;this.enter('warning');this.emit('death');}
      }
    }
    if(this.stage==='after') {
      once('quiet',5,'line',{text:'Шаги стихли. Можно выходить.\nНо коридор… раньше он был короче.'});
      once('call',10,'phone-ring');
    }
  }
  objective() {
    if(this.stage==='warm') {
      if(!this.flags.coffee)return 'Никуда не спешить. Кофе ещё тёплый.';
      if(!this.flags.note)return 'Бабушка оставила записку на столе.';
      if(!this.flags.phone)return 'Позвонить бабушке. Телефон у дивана.';
      if(!this.flags.window)return 'Посмотреть, что там за окном.';
      return 'Остаться на вечер. Можно включить приставку.';
    }
    return {neighbor:'Кто-то позвонил. Посмотреть в глазок.',checking:'Проверить кран на кухне.',warning:this.loop?'Я помню кухню. И закрытую дверь.':'Странный голос за входной дверью.',attack:this.loop?'На кухню. Закрыть дверь и переждать.':'Найти место, где ещё горит свет.',after:'Телефон снова звонит.',free:'Вечер продолжится. Можно осмотреть квартиру.'}[this.stage]||'';
  }
}
