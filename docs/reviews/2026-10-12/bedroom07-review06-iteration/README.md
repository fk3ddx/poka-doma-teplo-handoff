# Review06 implementation — bedroom07 iteration

Claude review06: ../claude-review-06/README.md. Previous rollback checkpoint: cf0093413e0e4f352dc7b38a7a2352bb8556255c and Ataka-bedroom06-review05-complete-win64.zip.

## Implemented
- Coverlet: continuous smooth drape, removed head-end step and repeated7mm waves. Existing CC0 fabric maps and authored iron frame preserved.
- Pillows: widths.70/.60/.50m, tilt67degrees, nominal parallel surface gap3.67cm. Largest top1.156m remains below frame1.200m; moved toward headboard without going behind it. Lace follows the large cushion; reversed face winding fixed after a diagnostic flashlight exposed unlit threads.
- Corner: ready frames uniformly scaled to24cm height, diagonal placement on walnut triangle shelf; halo warm(1,.28,.10), radius.4,intensity.012. Red glass emission.6. Main Spot serialized state checked unchanged by installer.
- Glass: alpha.025, smoothness.95. Readability remains unapproved; no extra lamp or emissive fake lighting added.
- Slipper: first trial towards stool FAILED visually, despite complete gameplay checks passing. See rejected-floor-placement. Corrected placement verification pending below.

## Evidence and limits
- survey/:50 original1280x720 frames, lap1/warm-return, same cameras as prior iteration. These are staged views, not a walkthrough.
- bed-probe/:15 original frames. Modes: normal, no textile normals, no icon light, no bed-frame shadows, flashlight. Diagnostic overrides never run without the QA argument and restore original state.
- bed-shadow-diagnosis.jpg proves that the apparent coverlet bars disappear when ONLY the authored frame stops casting shadows. Geometry/material/light otherwise unchanged. These are bed-frame shadows, not separate cloth blocks. Production shadows preserved; ask Claude to reassess this specific finding.
- before-after.jpg is a uniform resize/layout of real frames, no exposure adjustment. Originals preserved.
- First full rendered route:43 PASS, errors0, coreFinished true, Direct3D11. Not final proof after placement correction; see rejected trial above.
- All QA uses audio-capture which clears the listener output; no system-volume changes or audible audition.

## Still open
White pill carton not sourced. Existing ready capsules and spectacles remain. No hallway integration in this iteration. Voice remains unapproved. Grandma pose, rope and main lighting/VHS unchanged. Final artistic approval, human playtest and equivalence to FtF NOT claimed.

Claude questions: confirm frame shadows should stay; assess pillows from5.1 (may still look too planar at distance), icon corner, actual glass vs dark alarm-clock silhouette; suggest a ready/photo-textured white medicine carton source. Hallway plan is recorded and will follow the bedroom corrections.

## Final verified route — supersedes intermediate trials above
Build review06-utf8-verified-build; full-route: 70 original game frames,43 PASS,errors0,coreFinished true,Direct3D11. Slipper now(1.75,.004,2.32), visibly clear of stool and beside stain at5.1. It moves away from the stool because the literal toward-stool trial occluded it. Russian5.1 subtitle visually checked and restored: original HEAD UTF-8, only one position constant differs in ScenarioTimeline. Completed six laps/chase/death skip/pause/winding/ten strikes/warm return; automated synthetic in-process input, not human playtest.
Audio: 8517 cleared callbacks, dropped0; no audible audition. Survey and bed-probe use the earlier same-art build; only the later slipper position and temporary subtitle encoding differed, neither occurs in those survey cameras. Main lighting/VHS/pose unchanged.
Archive in build-manifest.json is the corrected checkpoint; the earlier archive/failed placement are explicitly kept under rejected-floor-placement. No new Claude approval yet.
