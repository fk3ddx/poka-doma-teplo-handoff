# Finale iterative diagnosis — not an accepted build

C introduced a pre-animation reset of the procedural rig layer. It exposed that coroutine reach/hold measurements were reading the reset pose before LateUpdate: props received wrong attachment offsets. D corrects that by yielding WaitForEndOfFrame after reaching before measuring/attaching. All D reach and prop checks pass again; runtime errors 0 and epilogue reached in both attempts.

D also brings transformation IK weight to 1 (rather than leaving 45% of the stretched idle pose), so the photographed arms point toward the player rather than through the ceiling. One remaining check reports BakeMesh+TransformPoint top 3.20 m against head bind-geometry top 2.40 m. Before changing actual scale further, build-e compares every weighted world vertex against scale-compensated BakeMesh and the old un-compensated measurement. The mismatch is not waived or threshold lowered.

API reference consulted: [Unity 6000.3 BakeMesh](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html). The overload's scale compensation matters when converting a scaled baked mesh back into world space. Final judgement requires the pending independent measurements and rendered frames.

No human visual/audio acceptance. Scene still has absent facial geometry and placeholders documented by pc25.
