using System;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;
public static class ChemicalProbe{public static string Main(){var b=SceneObjects.All<Processor>().Single(p=>p.Chemical);var c=CommanderRoster.Instance.Commanders.First();return "need="+b.NeedsWork+" reason="+b.WaitReason+" buffer="+b.BufferedOil+" powered="+b.Powered+" ready="+c.CivilianWorkReady+" job="+c.ServiceJob+" target="+(c.ServiceTarget==b)+" at="+typeof(CommanderAnt).GetMethod("AtDrill",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{b})+" path="+c.Agent.hasPath+" pending="+c.Agent.pathPending+" remaining="+c.Agent.remainingDistance+" distance="+(c.Position-b.Position).magnitude+" eating="+c.IsEating+" washing="+c.IsWashing+" joy="+c.IsPlaying+" combat="+c.IsInCombat+" order="+c.AllowsJob(CommanderJobs.Crafting);}}
