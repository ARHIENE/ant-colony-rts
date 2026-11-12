using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using AntColony.Core;
using AntColony.Units;
public static class Life1112WalkingUse
{
    public static async Task<string> Run()
    {
        var c = CommanderRoster.Instance.Commanders.First(x => x && x.IsColonyMember);
        var p = c.GetComponent<MeshyNativeMotionPlayer>();
        var oldHealth = c.PersonalState.health; var oldJobs = c.WorkState.jobs;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var motion = typeof(CommanderAnt).GetField("idleLeisureMotion", flags);
        var until = typeof(CommanderAnt).GetField("idleLeisureUntil", flags);
        var report = new System.Collections.Generic.List<string>();
        try
        {
            p.PreviewMotion = null; p.PreviewPhase = -1;
            foreach (int damage in new[] { 3, 5, 0 })
            {
                c.CommandStop(); c.WorkState.jobs = CommanderJobs.None;
                c.PersonalState.health = new HealthState { migrated = true, selfTend = false };
                if (damage > 0) foreach (var leg in new[] { BodyPart.LegL, BodyPart.LegR })
                    c.Health.wounds.Add(new Wound { part = leg, kind = WoundKind.Scar, damage = damage });
                string expected = damage == 3 ? "Limping_Walk_1" : damage == 5 ? "Limping_Walk_3" : "Casual_Walk";
                Vector3 start = c.Position, target = start; bool found = false;
                for (int i = 0; i < 12; i++)
                {
                    if (NavMesh.SamplePosition(start + Quaternion.Euler(0, i * 30, 0) * Vector3.forward * 8, out var hit, 1, NavMesh.AllAreas) && c.CanReach(hit.position))
                    { target = hit.position; found = true; break; }
                }
                if (!found) throw new Exception("No walking fixture destination");
                c.GetComponent<AntVisual>().Action("Idle_A", 0);
                if (damage > 0) c.CommandMove(target);
                else
                {
                    motion.SetValue(c, "IdleLook"); until.SetValue(c, Time.time + 30);
                    typeof(SoldierAnt).GetMethod("SetMoveDestination", flags).Invoke(c, new object[] { target });
                }
                AntColony.UI.SimulationPause.UserSpeed(1);
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (p.PlayingMotion != expected && DateTime.UtcNow < deadline) await Task.Delay(50);
                if (p.PlayingMotion != expected) throw new Exception(expected + " not selected: " + p.PlayingMotion);
                var bones = p.NativeModel.GetComponentsInChildren<Transform>();
                var rotations = bones.Select(b => b.localRotation).ToArray(); int frame = Time.frameCount;
                await Task.Delay(650);
                float change = Enumerable.Range(0, bones.Length).Max(i => Quaternion.Angle(rotations[i], bones[i].localRotation));
                float distance = Vector3.Distance(start, c.Position);
                if (Time.frameCount <= frame || change < .1f || distance < .05f) throw new Exception(expected + " frozen or not travelling");
                report.Add(expected + " mobility=" + c.Health.Moving + " moved=" + distance + " frames=" + (Time.frameCount - frame) + " jointDegrees=" + change + "; no preview");
                AntColony.UI.SimulationPause.UserSpeed(0);
            }
            File.WriteAllLines("work/life-assets-1112/walking-use.txt", report); return string.Join("\n", report);
        }
        finally
        {
            AntColony.UI.SimulationPause.UserSpeed(0); c.CommandStop(); c.PersonalState.health = oldHealth; c.WorkState.jobs = oldJobs;
            motion.SetValue(c, null); until.SetValue(c, 0f);
        }
    }
}
