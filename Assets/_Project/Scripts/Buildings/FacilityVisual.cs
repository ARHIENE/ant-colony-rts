using System.Collections.Generic;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    // 건물 상자 대신 Blender 시설 모델(art/resident-kit → Resources/Facilities)을 보여 준다.
    // 상자 렌더러는 배치·방 판정·철거 표시가 크기를 읽으므로 끄지 않고 그리기만 멈춘다(forceRenderingOff).
    // 모델은 상자 바닥 면적에 맞춰 균일 배율로 줄이거나 늘리고, 높이는 모델 비율을 따른다.
    public sealed class FacilityVisual : MonoBehaviour
    {
        private static readonly Dictionary<BuildingKind, string> Models = new Dictionary<BuildingKind, string>
        {
            { BuildingKind.AcidTower, "CC_AcidTower" }, { BuildingKind.AdminDesk, "CC_AdminDesk" }, { BuildingKind.AirshipYard, "CC_AirshipYard" }, { BuildingKind.AnimalClinic, "CC_AnimalTreatment" },
            { BuildingKind.Apartment, "CC_Apartment" }, { BuildingKind.AphidPen, "CC_AphidPen" }, { BuildingKind.AreaAcidTower, "CC_AreaAcidTower" }, { BuildingKind.Armory, "CC_Armory" },
            { BuildingKind.Baduk, "CC_Baduk" }, { BuildingKind.BanquetTable, "CC_BanquetTable" }, { BuildingKind.Barracks, "CC_Barracks" }, { BuildingKind.BarredDoor, "CC_BarredDoor" },
            { BuildingKind.Bathtub, "CC_Bathtub" }, { BuildingKind.Battery, "CC_Battery" }, { BuildingKind.BigTable, "CC_DiningTable" }, { BuildingKind.BoardGame, "CC_BoardGame" },
            { BuildingKind.Bookshelf, "CC_Bookshelf" }, { BuildingKind.BottleMobile, "CC_BottleMobile" }, { BuildingKind.Brazier, "CC_Brazier" }, { BuildingKind.BronzeStatue, "CC_BronzeStatue" },
            { BuildingKind.BunkBed, "CC_BunkBed" }, { BuildingKind.ButcherTable, "CC_ButcherTable" }, { BuildingKind.Campfire, "CC_Campfire" }, { BuildingKind.CapWall, "CC_CapWall" },
            { BuildingKind.CareStation, "CC_CareStation" }, { BuildingKind.Carpentry, "CC_Carpentry" }, { BuildingKind.Carpet, "CC_Carpet" }, { BuildingKind.CastleWall, "CC_CastleWall" },
            { BuildingKind.CompostBin, "CC_CompostBin" }, { BuildingKind.ConscriptionPost, "CC_ConscriptionPost" }, { BuildingKind.CuriosDisplay, "CC_CuriosDisplay" }, { BuildingKind.DartBoard, "CC_DartBoard" },
            { BuildingKind.Door, "CC_DoorFrame" }, { BuildingKind.Dormitory, "CC_Dormitory" }, { BuildingKind.DoubleBed, "CC_DoubleBed" }, { BuildingKind.ElectricLamp, "CC_Lamp" },
            { BuildingKind.ExerciseRig, "CC_ExerciseRig" }, { BuildingKind.Farm, "CC_FarmTile" }, { BuildingKind.Feeder, "CC_Feeder" }, { BuildingKind.FireflyLamp, "CC_FireflyLamp" },
            { BuildingKind.Flag, "CC_Flag" }, { BuildingKind.Floor, "CC_Floor" }, { BuildingKind.FlowerPot, "CC_FlowerPot" }, { BuildingKind.FoodStore, "CC_FoodStore" },
            { BuildingKind.Freezer, "CC_Freezer" }, { BuildingKind.Fridge, "CC_Refrigerator" }, { BuildingKind.GamblingDen, "CC_GamblingDen" }, { BuildingKind.Gate, "CC_CastleGate" },
            { BuildingKind.Hammock, "CC_Hammock" }, { BuildingKind.Hearth, "CC_Hearth" }, { BuildingKind.HotSpring, "CC_HotSpring" }, { BuildingKind.House, "CC_House" },
            { BuildingKind.Hut, "CC_Hut" }, { BuildingKind.Infirmary, "CC_CommanderBed" }, { BuildingKind.Instrument, "CC_Instrument" }, { BuildingKind.Janggi, "CC_Janggi" },
            { BuildingKind.Jar, "CC_Jar" }, { BuildingKind.Kiln, "CC_Kiln" }, { BuildingKind.Kitchen, "CC_Kitchen" }, { BuildingKind.Lantern, "CC_Lantern" },
            { BuildingKind.LeafWall, "CC_LeafWall" }, { BuildingKind.LockedDoor, "CC_LockedDoor" }, { BuildingKind.MarbleMosaic, "CC_MarbleMosaic" }, { BuildingKind.MedicineBench, "CC_Apothecary" },
            { BuildingKind.MineField, "CC_MineField" }, { BuildingKind.Monument, "CC_Monument" }, { BuildingKind.MushroomFarm, "CC_MushroomFarm" }, { BuildingKind.Nursery, "CC_Nursery" },
            { BuildingKind.Painting, "CC_Painting" }, { BuildingKind.Pen, "CC_RanchFence" }, { BuildingKind.Pillar, "CC_Pillar" }, { BuildingKind.PowerWire, "CC_PowerWire" },
            { BuildingKind.PrisonerCamp, "CC_PrisonerCamp" }, { BuildingKind.ScienceLab, "CC_ResearchDesk" }, { BuildingKind.ScoutPost, "CC_ScoutPost" }, { BuildingKind.ShellDecoration, "CC_ShellDecoration" },
            { BuildingKind.Shower, "CC_Shower" }, { BuildingKind.SingleBed, "CC_SingleBed" }, { BuildingKind.SleepingMat, "CC_SleepingMat" }, { BuildingKind.Smelter, "CC_Smelter" },
            { BuildingKind.SoilWall, "CC_SoilWall" }, { BuildingKind.SpikeTrap, "CC_SpikeTrap" }, { BuildingKind.SpinningWheel, "CC_SpinningWheel" }, { BuildingKind.Statue, "CC_Statue" },
            { BuildingKind.Stonecutter, "CC_Stonecutter" }, { BuildingKind.Storage, "CC_Storage" }, { BuildingKind.StrongPen, "CC_RanchFence" }, { BuildingKind.Tapestry, "CC_Tapestry" },
            { BuildingKind.Toilet, "CC_Toilet" }, { BuildingKind.Tombstone, "CC_Tombstone" }, { BuildingKind.Torch, "CC_Torch" }, { BuildingKind.TrapPit, "CC_StickyTrap" },
            { BuildingKind.Treadmill, "CC_Treadmill" }, { BuildingKind.TrophyCase, "CC_TrophyCase" }, { BuildingKind.Washbasin, "CC_Washbasin" }, { BuildingKind.Watchtower, "CC_Watchtower" },
            { BuildingKind.WebSwing, "CC_WebSwing" }, { BuildingKind.WoodGenerator, "CC_WoodGenerator" }, { BuildingKind.Workshop, "CC_Workshop" }, { BuildingKind.WrestlingRing, "CC_WrestlingRing" },
            // 2026-10-19 새로 만든 기반 시설 5종(DigSite는 쓰이지 않아 제외)
            { BuildingKind.QueenChamber, "CC_Stockpile" }, { BuildingKind.ResearchLab, "CC_ResearchLab" }, { BuildingKind.DefenseLab, "CC_DefenseLab" },
            { BuildingKind.RestRoom, "CC_RestRoom" },
        };

        public static string ModelFor(BuildingKind kind) => Models.TryGetValue(kind, out var id) ? id : null;
        public Transform Model { get; private set; }

        // 씬에 놓인 시작 비축더미는 BuildingData 없이 Stockpile 컴포넌트만 있다.
        private static BuildingKind? KindOf(BuildingBase b) => b.Data != null ? b.Data.kind : b is Stockpile ? BuildingKind.QueenChamber : (BuildingKind?)null;

        public static void Attach(BuildingBase building)
        {
            if (building.GetComponent<FacilityVisual>() != null || KindOf(building) is not BuildingKind kind) return;
            var id = ModelFor(kind);
            if (id != null && Resources.Load<GameObject>("Facilities/" + id) != null) building.gameObject.AddComponent<FacilityVisual>();
        }

        private void Start()
        {
            var prefab = Resources.Load<GameObject>("Facilities/" + ModelFor(KindOf(GetComponent<BuildingBase>()).Value));
            var box = GetComponent<Renderer>();
            var mesh = GetComponent<MeshFilter>();
            if (prefab == null || box == null || mesh == null) { Destroy(this); return; } // 씬 프리팹처럼 상자가 없는 건물은 그대로 둔다
            var size = Vector3.Scale(mesh.sharedMesh.bounds.size, transform.lossyScale);
            var center = transform.TransformPoint(mesh.sharedMesh.bounds.center);
            var bottom = center.y - size.y / 2f;

            var model = Instantiate(prefab).transform;
            model.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Destroy(model.gameObject); Destroy(this); return; }
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var scale = Mathf.Min(size.x / Mathf.Max(bounds.size.x, .01f), size.z / Mathf.Max(bounds.size.z, .01f));
            model.localScale = Vector3.one * scale;
            var offset = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
            model.rotation = transform.rotation;
            model.position = new Vector3(center.x, bottom, center.z) - transform.rotation * offset;
            var lossy = transform.lossyScale;
            model.SetParent(transform, true);
            model.localScale = new Vector3(scale / lossy.x, scale / lossy.y, scale / lossy.z);
            foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            box.forceRenderingOff = true;
            Model = model;
        }
    }
}
