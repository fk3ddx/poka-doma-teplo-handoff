# Bedside ready props — 12.10.2026

## Sources and adaptation
- [Cups and bottles little pack](https://opengameart.org/content/cups-and-bottles-little-pack), DREAM_SEARCH_REPEAT, CC0. Original Blender preserved in art-sources/BedsideGlass. Selected VasoChico, 40 source vertices/76triangles. Recentered, uniformly scaled to10cm, dimensions8.31×8.31×10cm; no glass topology redesign.
- The original smoothed rim normals made the CPU diagnostic look like metal. Flat normals for the actual flat panes produce a clear glass silhouette/water line. Both actual CPU renders are retained; these are **Blender diagnostic previews, NOT Unity screenshots or FtF comparison**. No GPU/desktop window was used.
- Water VFX derives its cross-section from the author's inner walls at6.4cm;28triangles. Does not change the glass geometry.
- [Pills 3D Models](https://opengameart.org/content/pills-3d-models), mxc2 / Marcus-Indrek Simmer, CC0. Original zip and files preserved in art-sources/BedsidePills. Ready complexPill geometry/material regions normalized uniformly to18mm long,6.714mm diameter and laid flat. Two instances,1920triangles each. No branded bottle, drug name, diagnosis or new interaction.

## Engine verification
- First silent import failed when repeated FBX reimport reverted an earlier instance's name; geometry report rejected the missing name and Flat was not saved. Evidence retained.
- Unpack staged instances before further imports of the same source; keep shared author meshes/material assignments. Missing/duplicate report names remain errors.
- Second silent Unity import exited0 and saved Flat. Glass: minY.5505, height.1 on tabletopY.55. Capsules: minY.5501. Each fits inside the table with1cm margin; pairwise bounding boxes do not intersect clock, glasses or each other. Actual geometry report attached.
- Scene diff adds3 prop roots and their renderer data; no removals. Accepted clock/spectacles/corner/pose/light unchanged. No colliders or gameplay features added.
- Shader/render quality and actual in-game transparency have NOT been verified. Existing null-graphics URP TraceRenderingLayerMask import diagnostic remains, not suppressed.
- Windows rebuild exited0. Actual Windows player passed 43 assertions through6laps/chase/repeat-death/warm return; coreFinished=true, errors0, Null graphics, listenerVolume0. Evidence attached. Fresh ZIP CRC-checked; previous checked ZIP preserved. This is NOT visual/audio acceptance.

## Review
Claude chat remains inaccessible through the existing browser connection (HTML/JSON response error). No new window/focus workaround. These props still need actual Unity camera15/16/warm06 and Claude review when safe visible testing is allowed.

Published engine log copies omit local licensing identifiers; original raw logs remain in unity-game/QA. Diagnostics and failures are retained.
