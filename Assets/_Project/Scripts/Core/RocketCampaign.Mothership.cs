using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Core
{
    // Runtime encounter, saved by RocketCampaign rather than the original scene enemy catalog.
    public sealed class MothershipBossMarker : MonoBehaviour { }

    public sealed partial class RocketCampaign
    {
        [Serializable] public sealed class MothershipState
        {
            public bool reached, victorious, viewing;
            public float bossHp = -1;
            public AntColony.Boss.AoE.BossAoEState circle, cone, line;
            public MothershipEncounter.State encounter;
            public Vector3 returnPoint;
            public List<string> away = new List<string>();
            public List<string> boarding = new List<string>();
            public List<string> onboard = new List<string>();
        }
        static readonly Vector3 ShipCenter = new Vector3(-5000, 0, 5000);
        public static Vector3 ShipLanding => ShipCenter + Vector3.back * 24;
        public static Bounds ShipBounds => new Bounds(ShipCenter, new Vector3(48, 100, 64));
        GameObject shipSurface;
        NavMeshDataInstance shipNavigation;
        NavMeshData shipNavigationData;
        readonly List<Material> shipMaterials = new List<Material>();
        BossHealth shipBoss;
        public MothershipState Mothership => state.mothership;
        public BossHealth MothershipBoss => shipBoss;
        public bool MothershipVictory => Mothership != null && Mothership.victorious;
        public static bool AwayOnMothership(CommanderAnt c) => c != null && Instance?.Mothership?.away.Contains(c.PersonalState.id) == true;
        public IEnumerable<CommanderAnt> MothershipCrew => CommanderRoster.Instance == null ? Enumerable.Empty<CommanderAnt>()
            : CommanderRoster.Instance.Commanders.Where(c => c != null && Mothership.away.Contains(c.PersonalState.id));

        // Build navigation before commander save positions are restored. This remote surface has no home simulation objects.
        void EnsureMothershipSurface()
        {
            if (shipSurface != null) { EnsureMothershipNavigation(); return; }
            shipSurface = new GameObject("외계 본선 전장");
            shipSurface.transform.position = ShipCenter;
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "본선 갑판"; deck.transform.SetParent(shipSurface.transform, false);
            deck.transform.localPosition = Vector3.down * .4f; deck.transform.localScale = new Vector3(48, .8f, 64);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(.19f, .24f, .28f); deck.GetComponent<Renderer>().sharedMaterial = material;
            var landing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            landing.name = "착륙선 · 공격받지 않음"; landing.transform.SetParent(shipSurface.transform, false);
            landing.transform.position = ShipLanding + Vector3.up * .2f; landing.transform.localScale = new Vector3(6, .2f, 6);
            var landingMaterial = new Material(material); landingMaterial.color = new Color(.3f, .65f, .7f);
            shipMaterials.Add(material); shipMaterials.Add(landingMaterial);
            landing.GetComponent<Renderer>().sharedMaterial = landingMaterial;
            Destroy(landing.GetComponent<Collider>());
            BuildShipDetails(material, landingMaterial);
            shipNavigationData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> {
                new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Box, size=new Vector3(48,.8f,64),
                    transform=Matrix4x4.TRS(ShipCenter + Vector3.down*.4f,Quaternion.identity,Vector3.one) }
            }, new Bounds(ShipCenter,new Vector3(50,20,66)),Vector3.zero,Quaternion.identity);
            EnsureMothershipNavigation();
            if (GetComponent<AntColony.UI.MothershipHud>() == null) gameObject.AddComponent<AntColony.UI.MothershipHud>();
        }
        internal void EnsureMothershipNavigation()
        {
            if (!shipNavigation.valid && shipNavigationData != null) shipNavigation = NavMesh.AddNavMeshData(shipNavigationData);
        }

        bool EnsureMothershipBoss()
        {
            EnsureMothershipSurface();
            if (shipBoss != null) return true;
            var template = WorldMapManager.Instance?.CampaignBossTemplate;
            if (template == null) return false;
            shipBoss = Instantiate(template,ShipCenter + Vector3.forward*16,Quaternion.identity,shipSurface.transform);
            shipBoss.name = "본선 지휘체";
            shipBoss.gameObject.AddComponent<MothershipBossMarker>();
            shipBoss.CampaignId = null;
            CampaignBossBody.Ensure(shipBoss);
            // Inactive templates run Awake on first activation; restore health afterwards.
            shipBoss.gameObject.SetActive(true);
            foreach (var loop in shipBoss.GetComponents<BossBasicPatternLoop>()) loop.enabled = false;
            foreach (var sequence in shipBoss.GetComponents<BossPatternSequenceSimple>()) sequence.enabled = false;
            var finalEncounter = shipBoss.GetComponent<MothershipEncounter>() ?? shipBoss.gameObject.AddComponent<MothershipEncounter>();
            finalEncounter.Restore(Mothership.encounter);
            if (Mothership.bossHp >= 0) shipBoss.RestoreHp(Mothership.bossHp);
            else Mothership.bossHp = shipBoss.CurrentHp;
            shipBoss.onDead ??= new UnityEngine.Events.UnityEvent();
            shipBoss.onDead.AddListener(CompleteMothership);
            shipBoss.gameObject.SetActive(!Mothership.victorious);
            shipBoss.GetComponent<AntColony.Boss.AoE.BossCircleAoE>()?.Restore(Mothership.circle);
            shipBoss.GetComponent<AntColony.Boss.AoE.BossConeAoE>()?.Restore(Mothership.cone);
            shipBoss.GetComponent<AntColony.Boss.AoE.BossLineAoE>()?.Restore(Mothership.line);
            return true;
        }

        internal bool LaunchToMothership(AirshipYard yard)
        {
            if (yard == null || MothershipVictory || CampaignResearch.Instance == null || CampaignResearch.Instance.Departed) return false;
            var crew = yard.Passengers.Where(c => CampaignOutcome.Survivor(c) && c.IsEmbarked).ToArray();
            if (crew.Length == 0 || !EnsureMothershipBoss()) return false;
            Mothership.reached = true; Mothership.returnPoint = yard.Position;
            for (int i=0;i<crew.Length;i++)
            {
                var c=crew[i];
                if (!Mothership.away.Contains(c.PersonalState.id)) Mothership.away.Add(c.PersonalState.id);
                c.BeginMothershipSortie(ShipLanding + new Vector3((i%4-1.5f)*1.5f,0,4+i/4*2));
            }
            CampaignHistory.Record("본선 출전","로켓",$"장수 {crew.Length}명");
            ViewMothership();
            AntColony.UI.ToastManager.Show("본선 도착 · 지휘체를 처치하세요. 착륙선으로 걸어 돌아와 지구로 귀환할 수 있습니다.");
            return true;
        }

        public void ViewMothership()
        {
            if (!Mothership.reached) return;
            WorldMapManager.Instance?.ViewSite(null);
            Mothership.viewing=true;
            SceneObjects.Any<SelectionManager>()?.ClearSelection();
            var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
            if (camera!=null)
            {
                var crew = MothershipCrew.FirstOrDefault(c => !c.IsEmbarked && !c.IsDead);
                var focus = crew != null ? crew.Position + Vector3.forward * 6 : ShipLanding + Vector3.forward * 8;
                focus += Vector3.up * 3;
                camera.SetRegion(focus,ShipBounds,0); camera.RestoreView(focus,0,14);
            }
        }
        public void ViewEarth()
        {
            Mothership.viewing=false;
            WorldMapManager.Instance?.ViewSite(null);
            SceneObjects.Any<SelectionManager>()?.ClearSelection();
            SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.SetRegion(Home,AntColony.Map.HomeMapBuilder.CurrentWorldBounds);
        }
        internal void LeaveMothershipView() => Mothership.viewing=false;
        void CaptureMothershipCombat()
        {
            if (shipBoss==null) return;
            Mothership.bossHp=shipBoss.CurrentHp;
            Mothership.encounter=shipBoss.GetComponent<MothershipEncounter>()?.Capture();
            Mothership.circle=shipBoss.GetComponent<AntColony.Boss.AoE.BossCircleAoE>()?.Capture();
            Mothership.cone=shipBoss.GetComponent<AntColony.Boss.AoE.BossConeAoE>()?.Capture();
            Mothership.line=shipBoss.GetComponent<AntColony.Boss.AoE.BossLineAoE>()?.Capture();
        }
        public bool RequestMothershipBoarding()
        {
            bool accepted=false;
            foreach(var c in MothershipCrew.Where(c => CampaignOutcome.Survivor(c) && !c.IsEmbarked && c.CanReceiveOrders))
            {
                if (!c.TryWorkApproach(ShipLanding,out var point)) continue;
                c.CommandMove(point);
                if (!Mothership.boarding.Contains(c.PersonalState.id)) Mothership.boarding.Add(c.PersonalState.id);
                accepted=true;
            }
            return accepted;
        }
        public static void CancelMothershipBoarding(CommanderAnt commander)
        {
            if (commander != null) Instance?.Mothership?.boarding.Remove(commander.PersonalState.id);
        }
        public bool ReturnFromMothership()
        {
            var aboard=MothershipCrew.Where(c => CampaignOutcome.Survivor(c) && c.IsEmbarked && Mothership.onboard.Contains(c.PersonalState.id)).ToArray();
            if (aboard.Length==0 || !NavMesh.SamplePosition(Mothership.returnPoint,out var home,12,NavMesh.AllAreas)) return false;
            for(int i=0;i<aboard.Length;i++)
            {
                var c=aboard[i];
                Mothership.away.Remove(c.PersonalState.id); Mothership.onboard.Remove(c.PersonalState.id); Mothership.boarding.Remove(c.PersonalState.id);
                c.EndMothershipSortie(home.position + Vector3.right*(i+2));
            }
            foreach(var yard in SceneObjects.All<AirshipYard>().Where(y => !y.IsDead && y.Phase==RocketPhase.Launched)) yard.ReadyForMothershipReentry();
            ViewEarth();
            CampaignHistory.Record("귀환","본선 원정대",$"{aboard.Length}명 귀환");
            return true;
        }
        void TickMothership()
        {
            if (Save.SaveSystem.Busy || !GameSession.Exists || !GameSession.Instance.GameStarted || !Mothership.reached || Mothership.victorious) return;
            if (!EnsureMothershipBoss()) return;
            foreach(var c in MothershipCrew.Where(c => Mothership.boarding.Contains(c.PersonalState.id)).ToArray())
                if (c.CanReceiveOrders && !c.IsEmbarked && Vector3.Distance(c.Position,ShipLanding)<=3)
                {
                    c.CommandStop(); c.SetEmbarked(true,ShipLanding);
                    Mothership.boarding.Remove(c.PersonalState.id); Mothership.onboard.Add(c.PersonalState.id);
                }
            shipBoss.gameObject.SetActive(MothershipCrew.Any(c => CampaignOutcome.Survivor(c) && !c.IsEmbarked));
            Mothership.bossHp=shipBoss.CurrentHp;
        }
        void CompleteMothership()
        {
            if (!Mothership.reached || Mothership.victorious || shipBoss==null || !shipBoss.IsDead) return;
            Mothership.victorious=true; Mothership.bossHp=0;
            var crew=MothershipCrew.Where(CampaignOutcome.Survivor).ToArray();
            state.ending=CampaignOutcome.Capture(crew);
            var survivors=new HashSet<string>(crew.Select(c=>c.PersonalState.id));
            foreach(var entry in state.ending.commanders) if(survivors.Contains(entry.id)) entry.category=EndingCategory.Escaped;
            CampaignHistory.Record("본선 격파","본선 지휘체","침공 저지",true);
            CampaignResearch.Instance.CompleteDeparture(crew);
        }
        void RestoreMothership()
        {
            state.mothership ??= new MothershipState();
            EnsureMothershipSurface();
            if (shipBoss != null) { Destroy(shipBoss.gameObject); shipBoss=null; }
            if (!Mothership.reached) return;
            EnsureMothershipBoss();
            foreach(var c in MothershipCrew)
                if (Mothership.onboard.Contains(c.PersonalState.id)) c.SetEmbarked(true,ShipLanding);
            if (Mothership.viewing) ViewMothership();
        }
        void DisposeMothership()
        {
            if (shipNavigation.valid) shipNavigation.Remove();
            if (shipNavigationData != null) Destroy(shipNavigationData);
            foreach (var material in shipMaterials) if (material != null) Destroy(material);
            if (shipSurface!=null) Destroy(shipSurface);
        }
        static bool ValidMothership(MothershipState s)
        {
            if(s==null) return true;
            bool Ids(List<string> ids) => ids!=null && ids.Count<=10000 && ids.All(id=>!string.IsNullOrWhiteSpace(id)) && ids.Distinct().Count()==ids.Count;
            return !float.IsNaN(s.bossHp) && !float.IsInfinity(s.bossHp) && s.bossHp>=-1 && s.bossHp<=1000000
                && MothershipEncounter.Valid(s.encounter)
                && AntColony.Boss.AoE.BossAoEState.Valid(s.circle) && AntColony.Boss.AoE.BossAoEState.Valid(s.cone) && AntColony.Boss.AoE.BossAoEState.Valid(s.line)
                && Ids(s.away) && Ids(s.boarding) && Ids(s.onboard) && s.boarding.All(s.away.Contains) && s.onboard.All(s.away.Contains)
                && !s.boarding.Intersect(s.onboard).Any() && (!s.victorious || s.reached && s.bossHp==0)
                && (!s.reached ? s.away.Count==0 && !s.victorious && !s.viewing : true)
                && !float.IsNaN(s.returnPoint.sqrMagnitude) && !float.IsInfinity(s.returnPoint.sqrMagnitude);
        }
    }
}
