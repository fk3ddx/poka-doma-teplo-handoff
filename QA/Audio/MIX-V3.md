# Audio mix v3 — 2026-10-11

Working mix, not listening acceptance. Preceding reference is `963eaf8` (phone v2). Lighting, scene geometry and story timings were not changed.

## Implemented

- Removed legacy per-call `volume/.4` gain. Base levels live in the cue bank; the two temporary speech cues retain named gain constants. Footstep walking/crouching attenuation remains a deliberate movement modifier.
- Source preparation: transient peak -1 dBFS; long/loop RMS target -20 dBFS bounded by -3 dBFS peak. No forced RMS target when it would break peak headroom. PCM16, original rates, mono. Updated processed manifest and audio credits.
- Recorded sources preload. No additional Unity downmix/normalisation of already mono WAVs. Import regression test verifies mono, preload and <= -1 dBFS sample peak for every distinct clip.
- Distance curve has a near plateau; curves cached per cue. Chime within 2 m avoids false self-occlusion by the clock casing.
- Each new chime ducks earlier tails by 8 dB; at most three survive, oldest quiet tail is replaced. All 10 attacks started; seven tail replacements expected.
- Running footstep stride changed from 1.1 to 1.8 m: at 5.85 m/s about 3.25 rather than 5.3 events/s. Walking stride .85 m, crouching .7 m. The game still uses actual travelled distance.
- Cue diagnostics distinguish starts, concurrency suppression, full-pool suppression and tail replacement.
- Optional `-audio-calibrate -audio-capture <path.wav>` tests isolated sources 1.5 m in front of listener, with world/obstruction disabled. Receiver cues remain 2D. Normal launches do not run it.

## Evidence

Windows build succeeded. 19/19 Scenario EditMode tests passed. Full scripted Windows menu → six laps → chase death/restart → warm return passed, errors=0. It is automated input, not a manual mouse or headphone playthrough.

Full capture: sample peak **-5.08 dBFS**, RMS **-28.43 dBFS**, no samples >=0 dBFS, no dropped samples, no format changes. Maximum 13 active voices. Exactly one suppressed request: `breath` (6 requested / 5 started); its long clip overlaps the repeat timer under the bed. No pool exhaustion. Chime 10 requested / 10 started / 7 prior-tail replacements. Counters and event markers: `mix-v3/full-mix.json`.

| Excerpt | Sample peak dBFS | Whole excerpt RMS dBFS |
|---|---:|---:|
| Lap 1 including walking | -15.73 | -43.48 |
| Under bed | -6.87 | -24.31 |
| Phone sequence | -8.73 | -30.05 |
| Hanging | -6.76 | -23.46 |
| Death | -6.74 | -20.59 |
| Chase end | -8.48 | -24.09 |

These are **not LUFS or true peak**. Excerpts are not loudness matched; see `comparison/mix-v3-*.wav` and `mix-v3-metrics.json`. `quiet` excerpt includes player footsteps, so its maximum is not an ambient-only measurement.

The isolated calibration was captured before the final import preload/downmix flags, with the same source/gain edits. Its 100 ms stereo RMS maxima: clock -32.13; phone ring -19.92; chime -19.52; shriek -14.23; guest breath -27.58; step -31.72; phone speech -28.94. Calibration is for level diagnosis, not evidence that the mix matches FtF or all suggested consultant targets. Full result and command marker data: `calibration-v3/`.

## Still open

- No human listening assessment or headphones/speaker A/B. Room acoustics and source suitability cannot be accepted from level measurements.
- Phone speech remains too quiet relative to consultant's suggested range; TTS itself is temporary. Do not just boost the master to fix it.
- Chime source contains a cleanly decaying tail by per-second peak envelope, but its strongest peak occurs 254 ms after clip start. Attack/timbre still need listening and tighter edit; do not claim a 5 ms onset.
- Death still uses a door creak as an incorrect finger-crack placeholder and lacks the wet tear at 2.0 s. Needs correct source recordings and exact §7 timing.
- Missing ceiling scratch, flies/drops, TV hiss, winding, acoustic continuity at vestibules remain in AUDIO-PLAN.
- Visual blockers from Entry-02 remain: dark vestibules, material boundary, temporary Guest model, crude collapse. This audio work does not close visual acceptance.

Unity references used for import/attenuation review: [Audio Clip settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html), [Audio Source settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioSource.html), [SetCustomCurve](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource.SetCustomCurve.html).