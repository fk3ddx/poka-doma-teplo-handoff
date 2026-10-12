# Second finale attempt — not accepted

Build-b open branch exits 1: one failure, zero runtime errors, reaches final menu. Hug distance now 0.287 m (PASS) but image remains a close shoulder view, not approved art. Hand bind geometry calibration reduces the transformed maximum from 4.51 m to 3.36 m, still above the 2.5 m ceiling. The capture visibly shows limbs beyond the ceiling; a numerical bone-head target alone is not sufficient.

Next correction under test: restore the previous frame's procedural bone rotations BEFORE legacy animation samples, then capture and apply the new layer in LateUpdate. An enabled legacy animation does not necessarily overwrite unkeyed bones: pc25 assumed it did, allowing IK/neck rotations to accumulate. Build-c tests this separately. No thresholds lowered. Full face transformation, face mesh and voices still absent.
