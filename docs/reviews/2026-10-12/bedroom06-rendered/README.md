# Bedroom06 actual GPU evidence — 12.10.2026

The user explicitly allowed windows again. Audio remains muted through the listener capture; Windows volume is unchanged.

## Survey
48 original 1280x720 Unity screenshots. Staged camera survey, NOT a player walkthrough. `survey.json` discloses camera positions and lighting. Rendering finished with exit0; no Error/Exception/Failed matches in player log. Production accepted lighting/VHS and pose unchanged.

## Findings before further changes
- Icons are adjacent and legible, shelf no longer the brightest object; red lampada still awaits Claude judgement.
- Views16 and17 (lace on/off) show essentially the same dark cushion fronts. Lace alone does not explain the issue. Source linen color/normal have no red stripe; the mattress stripe could be projected light/shadow, not proved yet.
- Pillow normal map is a coarse tufted/quilted fabric. A separate runtime-only probe is being prepared: same camera/materials/light baseline, normal-map-off, then flashlight. These diagnostics are explicitly NOT production changes.
- The ready glass is visible in the wide view, but the bedside is too small to judge transparency/support; a dedicated camera18 is added for inspection.
- Door clearance render shows free floor, but only runtime movement can validate traversal.

## Not accepted
This is not equivalence with FtF, completed apartment, completed voice or user acceptance. Claude access is currently blocked by browser tooling; no outbound message claimed. Followup is in the existing NOT SENT draft.

## Actual full route
Direct3D11 runtime exited0, passed all43 assertions, coreFinished=true/errors0. Full original screenshots and result attached. Audio capture cleared8633 output callbacks, droppedSamples0; no audible audition. The result intentionally keeps visualVerified=false: screenshots exist but art is not yet approved. Overview is a downscaled labelled contact sheet at original exposure, not a replacement for the original PNGs.

## Material probe findings
9 more original frames in material-probe-before, scope disclosed in survey.json. Removing pillow/mattress normal maps softens the micro-pattern but does not restore frontal illumination. Adding the diagnostic flashlight makes the same geometry and original material light and readable. Lace-off produces no major improvement. Therefore do not claim the lace or normal map is the primary cause of darkness, and do not change accepted room exposure/light without art review.

Camera18 does reveal overly cloudy glass/water. A small material-only adjustment is in progress: alpha glass0.14→0.045, water0.075→0.02. Source shape, units, placement and production lighting remain unchanged. Before images preserved. This is an actual visual defect, not a new feature.

## Glass after render
material-probe-after contains the nine actual same-camera checks after lower alpha. The glass body is less opaque, but its final appearance remains subject to Claude review; do not call this realistic glass. `glass-before-after.jpg` juxtaposes original-exposure frames, downscaled with labels only. Accepted production light/VHS/pose unchanged.

## Final production build after glass adjustment
`final-route/` contains69 original actual gameplay frames,43 PASS assertions, errors0/coreFinished=true on Direct3D11. The final independent ZIP is in build-manifest.json, CRC checked; older builds preserved. The production change is glass/water alpha only. Diagnostic normal-map/flashlight modes are opt-in QA and do not affect normal play. Visual/art approval remains false.

Capture limitation: final-route has69 frames, versus70 in the earlier route. HangingVision3.6 was missed by the existing100ms sampling window; its earlier baseline frame is retained in full-route. Required review05 frames1.1 and5.1 exist in final-route. Neither filenames nor these screenshots establish frame-exact Timeline compliance.
