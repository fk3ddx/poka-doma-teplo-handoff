# Recorded audio pass 1 — 2026-10-11

Status: working integration, NOT final sound acceptance. Windows build succeeds; 18/18 Scenario EditMode tests pass; automated Windows menu → six laps → chase death/restart → warm return passes, errors=0. This is automated input, not a manual mouse/headphone playthrough.

## Changes

- Replaced ScenarioSound procedural Tone/noise placeholders with 56 reproducible mono edits from 15 explicitly CC0 Freesound preview recordings and Kenney CC0 packs. Source/licensing/edit records: CREDITS-AUDIO.md and art-sources/Audio manifests.
- 32 pooled voices, separate mixer buses, cue concurrency limits, non-repeating choice where alternatives exist, spatial attenuation, smoothed obstruction low-pass/gain, movement-distance footsteps, fade of ongoing world sounds during quiet.
- Phone speech and phone breathing at the listener; behind-player breathing remains a different world cue. Original synthetic speech remains temporary.
- Guest/player chase breathing uses persistent loops instead of stacking a new clip every 0.7 seconds. Palm impacts follow the Guest. Ten clock cues use recorded chime tails.
- Added opt-in -audio-capture <path.wav>: float listener-mix recording on worker thread, state markers, dropped-buffer report. No microphone, no OS-wide recording, no capture without CLI flag.
- Existing scene lighting/geometry/timeline timing unchanged. Corrected malformed Russian ellipsis.

## Measurements (same automated route, unnormalised captures)

| Measurement | Before | After v1 |
|---|---:|---:|
| Sample peak dBFS | -9.34 | -17.69 |
| Whole recording RMS dBFS | -36.31 | -43.05 |
| Samples at/above full scale | 0 | 0 |
| Dropped capture samples | 0 | 0 |

The first mix is too conservatively attenuated: source -9 dBFS plus cue/mixer/distance gains. Do not call the quieter mix an improvement by itself. Needs context listening and event-by-event gain review. These are sample peaks/RMS, NOT true peak or LUFS. No loudness matching applied to comparison files.

`comparison.json` includes hashes, complete markers and per-clip sample metrics. `comparison/` has six before/after pairs: quiet lap 1, under-bed, phone, hanging, death, chase end. Full float recordings remain local in before/ and after-v1/ (about 65 MB each). These excerpts do not yet cover every acceptance scene in AUDIO-PLAN §6; doorway, balcony and seam-specific listening still required.

## Open work / limits

- No claimed human listening acceptance, headphone/speaker comparison, LUFS/true-peak measurement or final mix.
- No reverb zones or multi-state mixer snapshots yet (Normal bus routing only); current obstruction raycast is approximate.
- Prepared fridge is unused; no fridge/TV ambient was added to the first loops. Fly/drip/radio-winding details remain.
- MP3 previews are not original lossless recordings; conversion to WAV does not improve source quality. Phone is Western Electric, chime candidate needs tail listening; wet pasta stands in for palm foley.
- Existing visual blockers remain: black vestibules, hallway material discontinuity, temporary Guest model, rough collapse. Claude Entry-02 review saved separately.

## Phone v2 follow-up
Phone scream and breath now use the receiver band-pass; consecutive speech releases its preceding voice explicitly. Windows full smoke errors=0, 18/18 tests. See phone-v2-metrics.json and comparison/phone-v2-phone.wav. Peak active voices 14; suppressed starts 52, cause-by-cue diagnosis pending. No gain recalibration yet; not a final sound acceptance.
