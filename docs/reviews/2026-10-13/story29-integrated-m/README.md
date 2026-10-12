# Integrated M FAILED: actual doorway blocker

BuildM exit0. IntegratedM exits1/errors0 at lap2 after first warp. Natural original route, no waypoint bypass: target(8.05,0,5.55), stuck(8.27,.02,6.49), yaw193, controltrue/speed0. FAILED-walk shows door surface. Core started with real EnterLoop, which explicitly sets entrance.direction=1, unlike story Homely direction=-1 (inward to hallway). Suspect outward leaf sweeps into the returning player's one-metre tambour; must diagnose actual blocker before changing anything. Do not hide by staging player or deleting collider.

Archive NOT released. Fresh prologueL and openL both passed; those do not cover this loop return. Send exact evidence to Claude; inspect door angles, capsule and collider if necessary. Goal active; failed runner ended, no Unity/player left alive.
