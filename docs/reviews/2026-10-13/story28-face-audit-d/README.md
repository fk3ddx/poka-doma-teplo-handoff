# Frozen-pose light audit D (pc28f)

At1.2, same pose, camera and original materials. 06 default-layer camera point .8/range3 visibly lights face and overexposes fridge. 07 identical clone of pc28f key(.75,range2.4,spot48,layer32) remains dark. 08 point camera-side .75/range3 on layer32 also remains dark; 09 camera spot and10 point at original position likewise. Original body renderer mask33. Thus direction alone does not explain missing light: layer-isolated variants do not affect the image. Precise pipeline cause not yet proven. Test is diagnostic; no gameplay light change retained.

Source build story28-audit-build-d. Exit0/errors0 means diagnostics completed, not art approval. QA-only FaceRenderAudit now emits10frames. Preserve Claude's FaceShed work; no conflict on gameplay code.
