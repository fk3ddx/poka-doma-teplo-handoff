# Headless engine checks and bedroom06 placement — 12.10.2026

## What actually ran
Unity6000.3.26f1 `-batchmode -nographics`, hidden startup and CREATE_NO_WINDOW, below-normal priority, two job workers. No player window, screenshot or audio playback. Logs confirm Null graphics; sampled MainWindowHandle was0. These runs do not prove visual quality or full gameplay.
Official command-line reference: https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html

## Door
- First new analytic guard:1/4 EditMode tests passed;3 failed resume, leaving~1degree before the requested angle.
- Cause: Quaternion.Angle deliberately considers extremely close rotations equal. Using it to discard each smoothed step creates a larger residual error. The guard now measures rotation with atan2 of the relative quaternion; collision logic and player transform remain separate.
- After fix:5/5, including a direct small-step regression.
- Strengthened suite:7/7. Resume now has a1second deadline. Two extra cases actually call native CharacterController.Move under gravity, with controller-before-door and door-before-controller orders: position remains(0.86,0.01,3.75) after120steps. No sideways push.
- Still not proven: actual Windows-player complete route, moving approach, cinematics, restart/recovery with these latest assets. The earlier failed bedroom06-door-probe is retained, not retroactively marked passed.

## Asset installation
Final HeadlessBedroom06-rootaxes exited0, saved Flat.unity. Shelf top1.62, flame1.75, halo.018/range.5, spot inner40. Accepted spot intensity.14 retained. Ready clock13.17×6.67×17.42cm, spectacles14×11.12×4.12cm, both bottomY.552 on tabletopY.550, margins/overlap checks passed.
Failures before success are included: wrong FBX unit defaults caused a huge clock; original clock uses SkinnedMeshRenderer, not MeshFilter; overriding spectacles' imported root rotation changed their orientation. Fixed with explicit ModelImporter units and independently measured dimensions, rigid export of original authored clock meshes around authored deform-bone pivots, and composing yaw with imported basis. Shapes/UV were not remodeled.
Clock hands are bound to flat.clock time. Their rendered direction/readability and all loop times are still pending gameplay verification. Source originals retained, including the rigged FBX.

## Limits / safety
- URP logged `Host type is not matching any asset type` for its package TraceRenderingLayerMask.urtshader in null-graphics import. Therefore these are geometry/physics checks, NOT a successful GPU shader/render verification. No vendor package code or accepted LightLab asset was changed to suppress it.
- A failed editor run left an unheld lock file. Automatic approval review initially rejected a retry because two unknown unity.exe processes existed. Read-only WMI showed both are Unity Hub/helper binaries with no projectPath; no Unity Editor remained. The failed process exit was confirmed and an exclusive read of the lock succeeded. Re-review then approved a conditional OS-lock check. Lock file was never deleted/renamed; no unrelated process was stopped. Helper remains fail-closed for a held lock and defaults to refusing a pre-existing lock without the explicit check option.
- No new images and no human acceptance. Glass of water/medicine still missing. Claude browser connection still unavailable; this packet must be sent when it recovers.
