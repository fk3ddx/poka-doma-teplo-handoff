# Package04 implementation notes

- All staging/layout decisions handled with Claude under the user's direct delegation. No repeat permission questions.
- The Katcho FBX has one Granny mesh (256 vertices) and one KatchoGranny material plus separate CaneDummy. A separate removable shoe with an underlying bare foot was not established. Chosen review03 fallback: retain approved character, reveal a real slipper at5.0.
- Table/stool coordinate in review03 is interpreted as the actual rotated renderer-bounds centre, not the upright model foot pivot. The first package04 pass was still offset by22.5cm; corrected.
- Ready slipper: original disconnected mesh components, not generated geometry. Length.28m, width.1351, height.0672.
- Frame support measured in Blender: central spring mesh mostly z=.49m, peak~.50m. Mattress bottom corrected to .51m, top .65m. This prevents fabric clipping through the author frame.
- Bed textile geometry explicitly allowed in review02 G2. No new furniture or character mesh. Pillows moved to the taller headboard; source dye neutralized with importer RGB swizzle on copied texture maps. Original shared texture importers restored byte-for-byte. Lace veil is absent.
- Drop is visual only for now: existing ScenarioBank has no drip recording. Do not substitute a gore/impact sound. Audio review is still pending and all tests have silent output.
- The 10%-screen-height gap and convincing drip remain artistic criteria, not claimed as automatically passed. Claude must inspect the actual two-entry images.
- Side QA shot uses a temporary neutral key plus repositioned flashlight and hides occluding geometry; it is not representative gameplay lighting.
