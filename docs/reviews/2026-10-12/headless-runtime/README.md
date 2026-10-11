# Runtime checks without interrupting the desktop — 12.10.2026

Status: current Windows build passed full headless core route (43 assertions) and separate door probe. No visual acceptance.

## Method
- Official standalone `-batchmode -nographics`; this initializes Null graphics. https://docs.unity3d.com/6000.3/Documentation/Manual/PlayerCommandLineArguments.html
- QA-only `-scenario-headless` mutes AudioListener before scene Awake; output stays zero. No system volume changes, no microphone/loopback, no audio audition.
- ScenarioPlayer never touches Cursor in batch mode. Synthetic InputSystem keyboard AND mouse are isolated inside the player; real devices disabled only for that process. No OS input events.
- Hidden startup / CREATE_NO_WINDOW / below-normal priority / two job workers. No `-single-instance` (it could activate an existing window).
- The same route assertions run; only screenshot capture becomes explicit NO_RENDER markers. Final return retains its one-frame white-flash sequencing, yielding a normal update instead of unavailable end-of-render in batch mode.
- Each run gets a new evidence directory, executable AND runtime assembly hashes. Timeout terminates only the child launched by the helper.

## Limits
No GPU rendering, look-check, screenshots, lighting/asset/cutscene visual acceptance or human playthrough. Gameplay timing is checked in real elapsed time under a headless update loop, not a GPU frame budget. Previous rendered failures remain valid historical evidence. Do not publish this as an accepted art build.

## Claude
Attempt to bind the already-open authorized Chrome chat again failed before page access: Unexpected token '<', HTML instead of JSON. No new browser window or native focus operation. Follow-up remains unsent.

## Results
- First door run: stationary and moving capsule checks passed; probe itself accidentally entered the transfer trigger early and then tried a bedroom target from the next hallway. Preserved as FAIL. Corrected probe stops the approach at z4.05 before testing transfer separately.
- Door2: all assertions passed; original position(.86,.01,3.75) unchanged for2s; after retreat opening reaches95deg within1s; moving sideways displacement0; closure/transfer tolap2 passed.
- First full run passed all6laps then failed the camera comparison. Headless Shot yielded an update instead of end-of-render, so the test compared the new player pose with the camera from the previous LateUpdate. No camera code was changed to mask this.
- Full2 observes normal Cinemachine LateUpdate with a late-order QA observer. Same-frame sample: gap0, cornerFrame6939, sampledLateFrame6939. The assertion is retained; no forced camera update/teleport.
- Full2: all6laps, pickups/events/gates/phone/hanging line-of-sight, locked6thcarpet, original hallway entry, pause clock, stall death/restart, repeat death skip,42–50s warm-clock arrival,2s winding,10strikes and return passed. Bedside clock timeSource follows scenario across all6laps and warm return. coreFinished=true, errors0, graphicsDevice=Null, listenerVolume=0.
- Both builds exited0. Null-graphics editor still logs the known URP TraceRenderingLayerMask.urtshader importer error; it is preserved, not suppressed. Player full2 log has no runtime errors. Visual shader output remains untested.

## Next acceptance work
When a non-interfering visible run is allowed, capture bedroom06: camera16/17 lace A/B, red corner, bedside props, wet mark, route5.1; then full rendered route and sound audition, then send Claude the actual images for art review. LightLab, Flat layout and accepted pose unchanged in this iteration. Water glass/pills still absent; official Poly Haven catalogue request returned403, no asset falsely claimed downloaded.
