# pc24c — actual Windows pause/crouch regression

Build story24-build-d, pc24c fedaa629cc52dc566bff9da8f75be69f30887a9615b4b17a1256214fd37d4a1d, source8a2d234. PASS/errors0. Silent listener capture validated by external runner.

- Pause: zero new cues, zero ticking requests; ticking requested/started exactly once, limit0.
- 945 crouched and1560 standing records;104 crouched/184 standing replay frames, zero height mismatches over5cm.
- Died crouched; PLAY returns standing eye1.55m; cancel restores crouch eye0.94m; subsequent Night2 stands correctly.
- 9.606s active, reverse chronology0 violations, saved memories4/4, no light enable changes, cache5.97MB freed.
- Reverse one-shots:55requested/47started/8limited by overlap budget. Not all requested audio events sounded. Not auditioned.

Evening memories here are staged. The separate story24-real-evening package verifies real evening recording through both nights. Actual death close-up while crouched is NOT visually evidenced in this runner (only rewind screenshots), despite the path executing it; needs captures. Belt frame remains dark; pc24d requested from Claude, no final art acceptance. Story23 unchanged.
