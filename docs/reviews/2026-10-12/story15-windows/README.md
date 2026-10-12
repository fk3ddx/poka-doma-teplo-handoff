# Story15 — actual Windows integration checks, 2026-10-12

## Installed
- Claude pc09 guest-source importer, pc10 household recordings, pc11 capsule-grid grandmother routes.
- TV interaction now aims at `televisionScreen.bounds.center` instead of the hidden/edge logical pivot. Kept when merging pc10.
- ScenarioAudioBuilder installed the new bank in the existing scene; no scene regeneration or lighting migration.

## Actual results
- story15-build-b: Windows build exit 0, Unity 6000.3.26f1.
- story15-edit-b: 85/85 EditMode tests passed, including household cue presence/mono checks.
- Audio file audit: 17 prepared WAV hashes match the manifest, mono, zero clipped PCM samples. Listening pending. Radio uses the existing temporary original waltz.
- story15-flow-b (older build): failed TV aim.
- story15-flow-a (TV fix): TV action succeeded (image below). After this no next objective appeared for over eight minutes; terminated ONLY this QA process and recorded interrupted.json. No passing flow result; suspected player/grandmother obstruction still needs diagnostics.
- story15-flow-c (pc11 + bank): 0 runtime errors, FAIL, routeFailures=3. Living-room walks now have paths; Door→Kitchen, Kitchen→HallA and Door→Stove have no route. Runtime capsule matches test r=.22/h=1.5.
- Runtime logs also report GRAN_ROUTE_FAIL Door→Kitchen. She stays in the hall (image below).
- Next action fails: matches on fridge, target (8.24,1.58,2.43), eyes (8.14,1.57,3.24). Autopilot stages camera outside kitchen, so wall/door LOS must be diagnosed; do not weaken LOS to pass.

## Next corrections
1. Resolve kitchen throat: dresser and kitchen south wall/leaf; actual world bounds are in checks.txt and sibling story15-spatial.
2. Improve QA positioning/diagnostics and yield to grandmother so a staged test cannot deadlock itself.
3. Continue real Windows flow and the two death branches after fixes. Current result is NOT release-ready.

![Verified TV action](tv-action-fixed.png)
![Kitchen objective while grandmother remains in hall](kitchen-task-gran-still-in-hall.png)

These are actual game captures, not concept images. Test stages the player and skips held durations; no full human walking/menu/audio/art acceptance is claimed.
