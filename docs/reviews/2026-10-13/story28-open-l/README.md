# Fresh preview checks and integrated QA

BuildL prologueL PASS/errors0/stuck0: full yard/stairs/flat route77.1m plus step-back into hall (result total78.42m),13 shots, neighbours4.9 and8.1 cleanup verified. Original grandmother-opens inspected; temporary models/voices remain unaccepted even though absent[] is empty. Finale openL PASS/errors0/epilogue,19beats, transform3.521s/DeathFinale3.498s.

Claude reviewed F04/FaceL2.30/KeyL2.8 and confirmed the gap fix. His remaining testing requests: fresh open branch (now completed) and actual six laps→chase→finale in one process. Do NOT pretend a ChaseEnd test directly calling FinishCore covers the latter.

Added QA-only -scenario-integrated-finale / rendered_player --mode integrated. Starts with real StoryDirector.EnterLoop, keeps coreTestOnly=false, walks all existing ScenarioSmoke laps/chase/clock holds. Waits for real Finale chapter rather than core-only Finished. Then FinaleQA.ContinueCurrent takes over the EXISTING chapter without QaBegin or direct FinishCore. Core and finale reports stored separately; process output silently captured. Camera staging/accelerated finale waits remain disclosed. BuildM compiled exit0; integratedM running at this checkpoint, result unknown. No production gameplay changed. Draft STORY29-PREVIEW-RU.md is not a delivered archive yet.
