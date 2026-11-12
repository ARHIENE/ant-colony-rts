# First three waves cross-session review

2026-11-12. User explicitly requested checking THREE-WAVES results and coordinating follow-up fixes.

Owner: THREE-WAVES maintains Unity and CommanderDuty / BuildingConstructionSite. This session performs read-only review; no concurrent Assets edits.

Current results are NOT final three-wave results: baseline stopped at natural 271 game seconds, warehouse16/20 soil and farm11/30. Baseline raised build/haul priorities; final run uses defaults, so no same-condition speedup claim.

Review findings sent to owner:
1. Ready construction WaitReason must require CivilianWorkReady on the final reachable-worker predicate, matching the material-delivery branch. A sleeping reachable worker must not mask an awake unreachable worker.
2. Delivery-distance estimator must follow CommandFetchFor's accessible-store selection and floor fallback. Any store existing does not guarantee an accessible source; taking minimum total trip also differs from the nearest accessible source actually selected.

Await owner implementation and runtime regression evidence, then first/second/third wave results. No Unity calls or Assets mutations from this review session.

Follow-up: owner accepted both findings. Shared TryMaterialSource now matches the actual first accessible store -> floor source; CommandFetchFor and WorkTravelDistance use it. Ready WaitReason now filters CivilianWorkReady. Read-only follow-up confirms source-level fixes.

Runtime boundary cases requested: inaccessible store with reachable floor stock; reachable nearest store priority; awake unreachable worker plus sleeping reachable worker.

Verification currently interrupted by Unity reload/CLI timeouts (set_autotick, editor_play, run_script). THREE-WAVES remains sole recovery owner. No first-three-wave success claim; no duplicate Prepare or Editor control from reviewer.

Night lighting ownership agreed with THREE-WAVES: Map/DayNightLighting.cs only, candidate outside Assets until playthrough ends. Evidence: screens/final-01-end.png at120game seconds. Existing .25 directional intensity multiplied by (.35,.45,.8) leaves .0875/.1125/.2 of day channels before weather; the object silhouettes become hard to identify against ground.

Provisional candidate: tint(.65,.72,.95), directional .45, ambient .65. Still cooler/dimmer than day, leaves time/weather/sleep/combat mechanics untouched. Values require same-camera actual night A/B review (clear and storm), day unchanged check, transition continuity and teardown restoration. Not yet applied or verified; original SHA recorded before handoff.

Continuous play results confirmed with THREE-WAVES:
- Wave1:481.4s preparation; normal WaveAction skips419s;15 enemies in15.4s; HP98/70/98, no structural loss.
- Wave2:80 enemies in51.3s; HP97/11.5/77; one sleeping mat destroyed, another damaged.
- Wave3 ends1379.4s,78.7s battle including repeated orders; HP30/10.5/26; research desk and water source destroyed.
- All three waves defended, but living personal HP did not recover over240s. Runtime property write search confirms missing recovery connection. THREE-WAVES owns recovery/combat/overhead changes.
- Herb normal supply missing: no MaterialDeposits or crop entry; only random fishing treasure. Candidate appends2 regrowing herb nodes to preserve existing material keys and RNG sequence; old-world supply becomes available on reload. Existing Crop_Herb model is reused.
- Expired enemy commander remains confirmed by expired-corpses.jsonl:2 entries kind2,remaining0,nonempty commanderId. Runtime intentionally keeps both commander kinds; save validator allowed only colony remains. One-condition compatibility candidate prepared.
- Original failed snapshot preserved at work/three-waves-1112/after-wave3-raw.json; this is diagnostic data, not a successfully saved game.
- Regression helpers prepared outside Assets: WaveSaveHerbs1112 (raw validation, malformed-corpse rejection, real save/load, old material-key preservation), HerbProduction1112 (test storage/bench only; actual gathering/hauling/medicine production), NightReadability1112 (paused visual comparison/restoration). Not executed yet.

Final review is now documented in docs/THREE_WAVES_REVIEW_1112.md. 57 save/herb checks,9 rollback checks, natural-herb medicine production PASS. Six production files owned (including WeatherSystem). Final night clear/storm/day screenshots verified. Early shader-compilation-only conclusion was disproved; explicit linear fog ranges preserve model color in the current pipeline. Unity returned Edit ready with settings/root restored. Await final combined Graphify signal.
