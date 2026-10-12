# Story14: Windows evidence, 12 October 2026

Actual build story14-build-a contains Claude patches 01–04 + Codex guards (d6d437a code). Later patches 05–07 are integrated into source but are NOT covered by these runs.

- EditMode: 85/85 pass (before the two narrow save/enumeration guards).
- Windows build: success, including both guards.
- story14-full-a: Prologue → Evening → Night1 → Night2 → Loop; passed, 0 runtime errors.
- story14-finale-a: refusal path → epilogue → ending menu; passed, 0 errors.
- story14-core-a: existing six loops and chase → warm return; passed, Direct3D11, 0 errors.

Story QA stages the player at actions, skips hold durations and accelerates time. It does NOT verify real walking/collisions, ordinary menu clicks, audiovisual acceptance or an unbroken menu-to-ending run. Core QA uses its established route. Audio output was silenced by capture, not system volume.

![Actual Windows frames](story14-contact.jpg)

## Visible gaps

Frame 1: ready granny and Kitchen13 sink/cabinet are present. Frame 2: the kettle action is still offered on an empty stove; the blue gas light is excessive in this close view. Frames do NOT justify final art acceptance. Guest/claws still use fallback assets. Voices, courtyard/stairwell, rewind and several beats remain placeholders; full list in ../claude-playable-complete/README.md.

## Asset availability

User cannot manually download the blocked Sketchfab assets now. No repeated request. Claude is finding alternative author sources. Poly Haven API fetch attempted from this machine returned HTTP 403; no protection changed. PSX Horror Entity on itch.io is a verified free CC BY candidate credited to puck_psx; a download click was made but a usable local ZIP is not yet confirmed. Do not mark it imported.

## Next

Integrate passability patch, verify scene routes with real collision/aim checks, run death and isolated-save branches, install available ready props/monster, then produce a clearly labelled Windows test build. Preserve accepted light/VHS. This goal is active, not complete.


## Later run: patches 01–08 (build-b)

Build succeeded. ChaseEnd reached DeathFull, epilogue and ending menu without runtime errors. Isolated save wrote to its QA directory and Continue resumed Night1. Both overall results are **FAILED** because the new grandmother route check found 5 blocked graph edges:

- Landing → Door: architectural door leaf (validation currently runs before story door setup; investigate as possible false positive).
- HallA → KitchenDoor: two imported wood furniture colliders.
- KitchenDoor → Kitchen: same furniture + door leaf.
- Kitchen → Stove: READY PostSoviet Fridge.
- Aisle → Armchair: READY CC0 SovietSofa.

Claude received the complete list and a request to fix paths/door states rather than disable collisions. Route test radius differs from runtime (.20 vs .22) and skips open door leaves; requested correction. Application currently exits 0 despite routeFailures; wrapper correctly rejects result.passed=false. Not a release candidate.

pc08 was applied without accidentally included Python __pycache__ binaries and with Codex guard lines preserved. No scene or lighting assets changed.
