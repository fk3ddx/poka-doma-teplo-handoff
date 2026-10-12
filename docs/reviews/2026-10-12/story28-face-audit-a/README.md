# Same-pose face diagnosis, 1.2 seconds

Windows audit, 0 runtime errors. Time frozen at 1.2 seconds; camera, pose, room, exposure and VHS unchanged. Shader changes below are diagnostic only and are not saved to the scene.

- 01: reconstructed head, normal Lit material.
- 02: original full Granny skinned mesh, reconstructed parts hidden. Face is also dark here.
- 03: original full Granny with unlit atlas. Face becomes readable.
- 04: reconstructed parts and headless body with the same unlit atlas. Face also readable; no black Guest geometry present before its reveal.

This disproves the claim that the dark mask at 1.2 is solely caused by Guest poking through the face. Both original and reconstructed heads suffer in this kitchen light. It does not prove that the later transformation is correct: the 2.3-second gap still needs a separate diagnosis. Do not fix this by shipping unlit characters.
