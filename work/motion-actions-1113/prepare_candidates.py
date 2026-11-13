from pathlib import Path
root = Path('F:/UnityProjects/ant-colony-rts')
src = root / 'Assets/_Project/Scripts/Units'
out = root / 'work/motion-actions-1113'
def replace(text, old, new, count=1):
    assert text.count(old) >= count, old
    return text.replace(old, new, count)

s = (src/'WorkerAnt.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'public override void CommandStop()\n        {\n            RestoreGatherArrival();', 'public override void CommandStop()\n        {\n            RestoreGatherArrival();\n            RestoreConstructionArrival();')
s = replace(s, 'protected override void OnDisable()\n        {\n            RestoreGatherArrival();', 'protected override void OnDisable()\n        {\n            RestoreGatherArrival();\n            RestoreConstructionArrival();')
s = replace(s, 'SetMoveDestination(ConstructionPoint(0, out var point) ? point : site.Position);', 'if (!ConstructionPoint(0, out var point)) { CommandStop(); return; }\n            PreciseConstructionArrival();\n            SetMoveDestination(point);')
s = replace(s, 'if (HasReachedDestination() || NearDestination) { StopMoving(); state = State.Building; }', 'PreciseConstructionArrival();\n            if (HasReachedDestination()) { StopMoving(); RestoreConstructionArrival(); state = State.Building; }')
s = replace(s, '{ SetMoveDestination(point); state = State.MovingToBuildSite; return; }', '{ PreciseConstructionArrival(); SetMoveDestination(point); state = State.MovingToBuildSite; return; }')
s = replace(s, 'ConstructionPoint(constructionStep, out var point)', 'ConstructionPoint(constructionStep, out var point, true)')
# Null-target arrival must also release its temporary stopping distance.
s = replace(s, 'private void TickMovingToBuildSite()\n        {\n            if (targetConstruction == null)\n            {', 'private void TickMovingToBuildSite()\n        {\n            if (targetConstruction == null)\n            {\n                RestoreConstructionArrival();')
(out/'WorkerAnt.cs').write_text(s, encoding='utf-8')
s = (src/'WorkerAnt.GatherApproach.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'node.IsLooseCargo||node.GetComponent<AntColony.Buildings.BuildingBase>()!=null', 'node.IsLooseCargo||!node.GetComponent<AntColony.Buildings.FarmPlot>() && node.GetComponent<AntColony.Buildings.BuildingBase>()!=null')
s = replace(s, 'MeshyWorkContact.TryRadius(node,KemonoModel.Gather(node.ResourceType).clip,outward,candidate.y,out var fitted)', 'MeshyWorkContact.TryRadius(node,node.NeedsSowing ? "Tool_Till" : KemonoModel.Gather(node.ResourceType).clip,outward,candidate.y,out var fitted)')
(out/'WorkerAnt.GatherApproach.cs').write_text(s, encoding='utf-8')
s = (src/'AntVisual.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'worker.ConstructionStep == 1 ? "Tool_Saw" : "Tool_Hammer_Stone"', '"Tool_Hammer_Stone"')
(out/'AntVisual.cs').write_text(s, encoding='utf-8')
s = (src/'MeshyNativeMotionPlayer.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'commander && commander.IsLeisureWalking),out next)', 'commander && commander.IsLeisureWalking, StableVariant()),out next)')
s = replace(s, 'int RigIndex(int motion)', 'int StableVariant()\n        {\n            var id = commander ? commander.PersonalState.id : name;\n            unchecked { int hash = 17; foreach (char c in id ?? "") hash = hash * 31 + c; return hash; }\n        }\n        int RigIndex(int motion)')
(out/'MeshyNativeMotionPlayer.cs').write_text(s, encoding='utf-8')
s = (root/'Assets/_Project/Editor/MeshyNativeActionBindings.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'return map;', '''map["WalkLimpAlternate"] = "Limping_Walk_2";
            var runs = new[] { "Running", "Run_02", "Run_03" };
            var talks = new[] { "Talk_Passionately", "Talk_with_Hands_Open", "Talk_with_Left_Hand_Raised", "Talk_with_Right_Hand_Open" };
            var cheers = new[] { "Victory_Fist_Pump", "Cheer_with_Both_Hands", "Victory_Cheer", "victory" };
            for (int i = 0; i < runs.Length; i++) map["RunVariant" + i] = runs[i];
            for (int i = 0; i < talks.Length; i++) map["TalkVariant" + i] = talks[i];
            for (int i = 0; i < cheers.Length; i++) map["CelebrateVariant" + i] = cheers[i];
            for (int i = 0; i < 11; i++) map["StandVariant" + i] = "Idle_" + (i + 3);
            map["ListenAgree"] = "Agree_Gesture";
            return map;''')
(out/'MeshyNativeActionBindings.cs').write_text(s, encoding='utf-8')
s = (src/'CommanderJoy.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'if (!j.freeWalk && PlaySpot != null && !j.groupStarted)', '''if (!j.freeWalk && PlaySpot != null && !ReachPlaySpot(PlaySpot))
                {
                    j.waitSeconds += seconds;
                    if (j.waitSeconds >= GameBalance.PlaySeconds) { LeavePlay(); return BeginFreeWalk(); }
                    return true;
                }
                if (!j.freeWalk && PlaySpot != null && !j.groupStarted)''')
start = s.index('            if ((spot.Position - Position).sqrMagnitude > 49)')
end = s.index('            if (!spot.Join(this))', start)
s = s[:start] + '''            if (!TryPlayApproach(spot, out var approach))
            { j.retrySeconds = GameBalance.StarveRetrySeconds; return false; }
            approachingPlaySpot = spot; playApproach = approach;
''' + s[end:]
s = replace(s, 'PlaySpot?.Leave(this); PlaySpot = null;', 'ResetPlayApproach();\n            PlaySpot?.Leave(this); PlaySpot = null;')
(out/'CommanderJoy.cs').write_text(s, encoding='utf-8')
s = (root/'Assets/_Project/Scripts/Buildings/RecreationSpot.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'u.PlaySpot == this) >= MinimumUsers', 'u.PlaySpot == this && u.AtPlaySpot) >= MinimumUsers')
(out/'RecreationSpot.cs').write_text(s, encoding='utf-8')
s = (src/'AntVisual.Work.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'private string JoyClip() => commander.PersonalState.joy.freeWalk', 'private string JoyClip() => !commander.PersonalState.joy.freeWalk && !commander.AtPlaySpot ? "StandIdle" : commander.PersonalState.joy.freeWalk')
(out/'AntVisual.Work.cs').write_text(s, encoding='utf-8')
s = (src/'CommanderIdleLeisure.cs').read_text(encoding='utf-8-sig')
s = replace(s, 'if (!spot.Join(this)) return;', 'if (!TryPlayApproach(spot, out var approach) || !spot.Join(this)) return;\n            approachingPlaySpot = spot; playApproach = approach;')
(out/'CommanderIdleLeisure.cs').write_text(s, encoding='utf-8')
print('Prepared current motion and interaction candidates outside Assets; no Editor reload triggered.')
