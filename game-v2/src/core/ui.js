// HUD: prompt, objectives, subtitles, thoughts, choices, notes, title cards, fades.
const $ = id => document.getElementById(id);
const sleep = ms => new Promise(r => setTimeout(r, ms));

export class UI {
  constructor() {
    this.el = {prompt: $('prompt'), cross: $('crosshair'), obj: $('objective'), objList: $('objective-list'), thought: $('thought'), sub: $('subtitle'), subName: $('sub-name'), subText: $('sub-text'), choices: $('choices'), hint: $('hint'), fade: $('fade'), flash: $('flash'), card: $('card'), note: $('note'), noteText: $('note-text'), eyelids: $('eyelids')};
    this.objectives = [];
    this.thoughtTimer = null; this.subTimer = null;
    this.paused = false;
  }
  setPrompt(text) {
    if (text) {
      const html = `<span class="key">E</span>${text}`;
      if (this.el.prompt.innerHTML !== html) this.el.prompt.innerHTML = html;
      this.el.prompt.classList.add('visible'); this.el.cross.classList.add('active');
    } else { this.el.prompt.classList.remove('visible'); this.el.cross.classList.remove('active'); }
  }
  crosshair(on) { this.el.cross.classList.toggle('hidden', !on); }

  // objectives: [{id, text, done}]
  setObjectives(list) {
    this.objectives = list.map(o => ({...o}));
    this.renderObjectives(true);
  }
  addObjective(id, text) {
    if (this.objectives.find(o => o.id === id)) return;
    this.objectives.push({id, text, done: false, fresh: true}); this.renderObjectives();
  }
  complete(id) {
    const o = this.objectives.find(o => o.id === id);
    if (!o || o.done) return false;
    o.done = true; this.renderObjectives();
    setTimeout(() => { this.objectives = this.objectives.filter(x => x !== o); this.renderObjectives(); }, 2600);
    return true;
  }
  clearObjectives() { this.objectives = []; this.renderObjectives(); }
  renderObjectives(fresh) {
    this.el.objList.innerHTML = '';
    for (const o of this.objectives) {
      const d = document.createElement('div'); d.textContent = o.text;
      if (o.done) d.className = 'done'; else if (o.fresh || fresh) d.className = 'new';
      o.fresh = false;
      this.el.objList.appendChild(d);
    }
    this.el.obj.classList.toggle('visible', this.objectives.length > 0);
  }

  thought(text, ms) {
    clearTimeout(this.thoughtTimer);
    this.el.thought.textContent = text; this.el.thought.classList.add('visible');
    ms ??= 1800 + text.length * 55;
    this.thoughtTimer = setTimeout(() => this.el.thought.classList.remove('visible'), ms);
    return sleep(ms + 300);
  }
  subtitle(name, text, ms, cls = '') {
    clearTimeout(this.subTimer);
    this.el.subName.textContent = name || '';
    this.el.subText.textContent = text;
    this.el.sub.className = 'visible ' + cls;
    ms ??= 1400 + text.length * 52;
    this.subTimer = setTimeout(() => { this.el.sub.className = ''; }, ms);
    return sleep(ms + 250);
  }
  hideSubtitle() { clearTimeout(this.subTimer); this.el.sub.className = ''; }

  choose(options, input) {
    return new Promise(resolve => {
      const box = this.el.choices; box.innerHTML = '';
      const pick = i => { box.classList.remove('visible'); cleanup(); resolve(i); };
      options.forEach((t, i) => {
        const b = document.createElement('button');
        b.innerHTML = `<span class="n">${i + 1}</span>${t}`;
        b.onclick = () => pick(i);
        box.appendChild(b);
      });
      box.classList.add('visible');
      const key = e => { const n = parseInt(e.key); if (n >= 1 && n <= options.length) pick(n - 1); };
      addEventListener('keydown', key);
      const cleanup = () => removeEventListener('keydown', key);
      this.onChoiceScroll = null;
    });
  }

  hint(html, ms = 6000) {
    this.el.hint.innerHTML = html; this.el.hint.classList.add('visible');
    clearTimeout(this.hintTimer);
    if (ms) this.hintTimer = setTimeout(() => this.el.hint.classList.remove('visible'), ms);
  }
  hideHint() { this.el.hint.classList.remove('visible'); }

  fade(to, ms = 1200) {
    this.el.fade.style.transition = `opacity ${ms}ms`;
    this.el.fade.style.opacity = to;
    return sleep(ms);
  }
  flash(ms = 120) {
    this.el.flash.style.transition = 'none'; this.el.flash.style.opacity = 1;
    requestAnimationFrame(() => { this.el.flash.style.transition = `opacity ${ms}ms`; this.el.flash.style.opacity = 0; });
  }
  hideOverlay(kind) {
    const el = document.getElementById('hideview');
    el.className = kind ? 'on ' + kind : '';
  }
  breath(v, holding) {
    const el = document.getElementById('breath');
    if (v === null) { el.className = ''; return; }
    el.className = 'on' + (holding ? ' holding' : '') + (v < 0.25 ? ' low' : '');
    el.firstElementChild.style.width = (v * 100).toFixed(0) + '%';
  }
  eyes(closed, warm = false) {
    this.el.eyelids.classList.toggle('closed', closed);
    this.el.eyelids.classList.toggle('warm', warm);
  }
  async card(lines, {hold = 2600, gap = 900, cls = []} = {}) {
    const c = this.el.card; c.innerHTML = '';
    const els = lines.map((t, i) => { const d = document.createElement('div'); d.className = 'line ' + (cls[i] || ''); d.innerHTML = t; c.appendChild(d); return d; });
    c.classList.add('visible');
    for (const d of els) { await sleep(60); d.classList.add('on'); await sleep(gap); }
    await sleep(hold);
    c.classList.remove('visible');
    await sleep(1400);
  }
  note(text) { this.el.noteText.innerHTML = text; this.el.note.classList.add('visible'); }
  hideNote() { this.el.note.classList.remove('visible'); }
  get noteOpen() { return this.el.note.classList.contains('visible'); }
}
export {sleep};
