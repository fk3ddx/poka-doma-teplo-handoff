// Keyboard / mouse with pointer lock.
export class Input {
  constructor(canvas) {
    this.canvas = canvas;
    this.keys = new Set();
    this.pressed = new Set();
    this.dx = 0; this.dy = 0;
    this.locked = false;
    this.sens = 1;
    this.mouseDown = false; this.clicked = false;
    addEventListener('keydown', e => {
      if (e.repeat) return;
      this.keys.add(e.code); this.pressed.add(e.code);
      if (['Space', 'Tab'].includes(e.code)) e.preventDefault();
    });
    addEventListener('keyup', e => this.keys.delete(e.code));
    addEventListener('blur', () => this.keys.clear());
    addEventListener('mousemove', e => {
      if (!this.locked) return;
      this.dx += e.movementX; this.dy += e.movementY;
    });
    addEventListener('mousedown', e => { if (this.locked && e.button === 0) { this.mouseDown = true; this.clicked = true; } if (this.locked && e.button === 2) this.pressed.add('Mouse2'); });
    addEventListener('mouseup', e => { if (e.button === 0) this.mouseDown = false; });
    addEventListener('contextmenu', e => e.preventDefault());
    document.addEventListener('pointerlockchange', () => {
      this.locked = document.pointerLockElement === canvas;
      this.onLockChange?.(this.locked);
    });
  }
  lock() { this.canvas.requestPointerLock?.()?.catch?.(() => {}); }
  unlock() { document.exitPointerLock?.(); }
  down(code) { return this.keys.has(code); }
  hit(code) { return this.pressed.has(code); }
  endFrame() { this.pressed.clear(); this.dx = 0; this.dy = 0; this.clicked = false; }
}
