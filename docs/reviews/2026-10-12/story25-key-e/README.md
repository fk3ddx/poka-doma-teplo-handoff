# Independent geometry measurement: old top was double-scaled

Windows key-e: all original weighted vertices transformed by bone.localToWorldMatrix × bindpose give top **2.396 m**. BakeMesh(true) + TransformPoint independently gives **2.396 m**. Old BakeMesh(false) + TransformPoint gives **3.189 m**. Thus the remaining D/E numerical height failure came from the old measuring method, after the actual upward-arm defect was corrected. No growth threshold was relaxed. All world vertices fit the ceiling in this final pose.

Exit 1 remains honest: one hand-in-gap query still reads the pre-LateUpdate reset pose. It reports (7.86, 0.64, 6.68) while completed-pose reach is on target. Fix in progress: cache the rendered fingertip in actor-local space for coroutine queries; chain follows at execution order600, after GranRig500. Prop attachment waits for EndOfFrame. Zero runtime errors, epilogue reached, refused card correct, other reach checks pass.

Actual face transformation/voices still not implemented. The incoming Claude pc25b addresses readable chain lighting/framing and the empty-hand gesture, without changing transformation growth.
