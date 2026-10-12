# gran-nav-offline

Runs the real `GranNav.cs` (A* over her capsule, door crossings, swept smoothing) on a read-only collider export from `Editor/StorySpatialAudit` (Codex story15). The physics is faked with world AABBs, so the result is conservative for rotated or mesh shapes; the runtime check is still the `-story-smoke` ROUTE report.

```
dotnet run -- <dir with colliders-serialized.json, colliders-open.json, graph.json> "<doors staged open, comma-separated>" [Node=x:z ...]
dotnet run -- docs/reviews/2026-10-12/story15-spatial "Bedroom door,Living frosted doorway — temporary leaf"
```
Allowed crossings here: Kitchen door and Entrance60 (as in the story). Stage "Kitchen door,Entrance60" as open as well to re-check the routes with the open leaves as obstacles.
