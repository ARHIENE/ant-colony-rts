# Native appearance source partition

`hair-uv-triangle-keys.txt` identifies 4,917 original Meshy UV triangles previously included in the face by the height-based classifier. Both original rigs share these triangles. The builder checks the full key count against each original mesh before writing.

Rebuild with `AgentScripts/Native1112SourceParts.cs` in Edit mode. Existing asset GUIDs stay intact. Meshes are cleared and their vertex/index/skin/blendshape streams copied explicitly; `CopySerialized` previously left the old hair vertex buffer with the enlarged triangle buffer. Pre/post-copy validation rejects out-of-range indices and mismatched streams.

2026-11-12 validation: 26 rebuilt meshes passed memory and saved-file index checks. The follow-up builder uses continuous PONY/UPDO/TWIN deformation and fades facial morph deltas to zero at shared part boundaries. Its 26-mesh memory audit and 12 rig/hair combinations passed. All 52 actual Game captures (six hairstyles at three angles plus eight expressions, on both rigs) were reviewed under `work/appearance-overlap-1112/angle-review`.

The former exploded geometry and white front-hair overlap are absent in these captures. A subsequent 2026-11-13 pass reclassified 1,143 rear-neck triangles from Hair to Body using `nape-body-uv-triangle-keys.txt`, preserving their source skin weights and untinted material. The builder checks the keys against both source rigs. Each kit now has Body 22,954 / Hair 37,960 / Face 4,741 triangles. The 26-mesh and 12-appearance runtime audits passed; new rear/side captures show the former raised hair-colored neck plate reduced. Latest captures are in `nape-review`; representative views of all six styles and both rigs were inspected, not all 52 new captures.

The latest live audit compared 119 clips at three phases: all tested limb/torso joint rotations matched the source and moved between phases. Hand/finger grip adjustments are excluded from that comparison. Equipment validation separately covered 52 mapped model profiles, 22 wearable conditions, 14 weapon action routes, 122 bow poses, and ten weapons during live preview.

This is not full visual acceptance: Blink/Sleep still do not close the eyes fully, and bedside lying alignment remains unimplemented. Long styles remain deformations of the base mesh, not separately authored hairstyles. Three sampled phases and grip metrics do not prove absence of every surface penetration. The checked builder matches the currently applied assets; do not revert to CopySerialized or the old hard deformation branches.
