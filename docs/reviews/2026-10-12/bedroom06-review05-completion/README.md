# Review05: remaining requirements completed — 12.10.2026

User forwarded Claude review05 again. This is an implementation follow-up, not a new Claude approval. Previous build/archive and all failed QA runs remain intact.

## Actual changes

- Door feedback missing from the previous implementation is now present: a requested opening blocked before 20 degrees uses the existing latch cue and a safe opening target of 5 degrees. The same live capsule/OBB collision guard always applies, so the crack may be smaller when even 5 degrees is unsafe. No push, teleport or camera rotation. Once the player steps clear, the request continues to 95 degrees. Shared ScenarioDoor logic covers player-operated leaves; scripted scene controls remain separate.
- Existing door movement guard is preserved. It samples the proposed rotation in steps no larger than 1 degree using actual collider bounds and a 2 cm capsule clearance; it is a conservative sampled sweep, not an analytic exact theta-max solver.
- The white headboard block was exposed mattress, not a separate sheet prop. Extended the existing permitted textured coverlet over it, slightly lower beneath the pillows. Bed frame, mattress geometry, pillow placement, accepted hanging pose, rope and production lighting/VHS unchanged.

## Rendered diagnosis

`headboard-probe/` has 12 original Unity Direct3D frames at 1280x720. View16 is normal production lighting, view17 hides the lace. Other prefixes explicitly identify temporary diagnostic overrides, NOT production changes.

Turning off icon spot/halo removes the red illumination but leaves the dark band. Source textile has no painted red stripe. The band resembles the projected window/bed-frame shadow; its exact occluder has not been isolated, so this is an inference. We did not change the accepted lighting to conceal it. The exposed white mattress strip is now covered. Lace-off and normal-map-off still do not make the pillow fronts bright; a diagnostic flashlight does.

## Verification

- 8/8 Unity EditMode physical door tests passed. Includes restricted handle cue, safe 3–5 degree crack at the selected test position, no penetration or position change, then full reopening after retreat.
- Rendered door probe: original E point (0.86, 3.75), unchanged player position over 2 seconds, stopped leaf at approximately40.33 degrees; retreat to (1.10, 3.05), full95 degrees within1 second; approaching player sideways displacement0; vestibule closes/transfers to lap2.
- Final Windows Direct3D11 build: all43 runtime assertions passed, coreFinished=true/errors0;70 original route screenshots. Separate final survey has50 frames. The route includes all6 loops, chase, pause, repeat death/skip, warm module, winding and10 clock strikes. Audio capture cleared8518 callbacks with0 dropped samples.
- Shadow atlas logs report automatically reduced punctual-light shadow resolution (4096 atlas). No exposure/light changes were made for this patch; this existing render-budget limitation remains recorded.
- Independent ZIP passed CRC verification; SHA256 ec0a25ee5d9f5d152ef139570cda03513bf630b359cb21e18252bdfb8f753c7e. Previous archive preserved. See build-manifest.json.

## Limits

This is not human playtesting, frame-exact death-sequence validation, voice approval, final apartment art approval or equivalence to Fears to Fathom. Nightstand remains Claude-authorized temporary Kenney furniture. Screenshots have no exposure enhancement. Audio output is cleared by QA capture; no audible audition is claimed.

Claude browser access was unavailable; the follow-up request is prepared but not delivered. The forwarded review05 authorizes the listed implementation and names the hallway as next room after bedroom06/P0; it does not constitute approval of screenshots Claude has not seen.

## Inspect these originals

- [Bed, normal lighting](survey/lap1-16-bedroom-bedding-detail.png), [same without lace](survey/lap1-17-bedroom-bedding-without-lace.png).
- [Icon corner](survey/lap1-15-bedroom-icon-detail.png), [warm-return bedroom](survey/warm-return-06-bedroom-bed.png).
- [HangingVision1.1](full-route/scene-HangingVision-1.1.png), [5.1](full-route/scene-HangingVision-5.1.png).
- [Door original approach](door-probe/door-blocked-original-approach.png), [after retreat](door-probe/door-fully-open-after-retreat.png).

Representative normal/diagnostic headboard, icon, warm-return, HangingVision5.1 and blocked-door frames were inspected at original exposure; not every captured frame received individual visual review. The bedroom remains dark in warm-return, as previously documented. Do not interpret passing mechanics as visual acceptance.
