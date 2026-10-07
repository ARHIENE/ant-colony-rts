using AntColony.Data;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Buildings
{
    // 씬 템플릿이 없는 과학 시설의 비활성 템플릿을 만든다(이름이 Template으로 끝나 이후 FindTemplate이 재사용한다).
    // 비용·인력·시간·체력은 작업 지시서 2단계 표 값이다.
    internal static class RuntimeBuildingTemplates
    {
        internal static GameObject Create(BuildingKind kind)
        {
            // 함정·매설지는 밟혀야 하므로 길을 막지 않는 납작한 발판이다.
            var (name, scale, color, food, soil, special, ants, seconds, hp, walkable) = kind switch
            {
                BuildingKind.ConscriptionPost => ("징집소", new Vector3(3, 2, 3), new Color(.6f, .4f, .2f), GameBalance.ConscriptionFood, GameBalance.ConscriptionSoil, 0, GameBalance.ConscriptionAnts, GameBalance.ConscriptionBuildSeconds, 300f, false),
                BuildingKind.SoilWall => ("Soil Wall", new Vector3(2, 1.5f, .6f), new Color(.45f, .33f, .2f), 0, 25, 0, 3, 4f, 400f, false),
                BuildingKind.TrapPit => ("끈끈이 함정", new Vector3(1.6f, .1f, 1.6f), new Color(.3f, .22f, .12f), 10, 30, 0, 3, 5f, 100f, true),
                BuildingKind.SpikeTrap => ("가시 함정", new Vector3(1.6f, .1f, 1.6f), new Color(.45f, .4f, .35f), 10, 30, 0, 3, 5f, 100f, true),
                BuildingKind.AreaAcidTower => ("Area Acid Tower", new Vector3(1.6f, 2.4f, 1.6f), new Color(.55f, .8f, .3f), 40, 70, 0, 6, 10f, 220f, false),
                BuildingKind.Watchtower => ("Watchtower", new Vector3(1.2f, 3.5f, 1.2f), new Color(.6f, .55f, .4f), 20, 40, 0, 3, 6f, 150f, false),
                BuildingKind.MineField => ("Mine Field", new Vector3(1.4f, .1f, 1.4f), new Color(.6f, .25f, .2f), 20, 20, 2, 2, 3f, 50f, true),
                BuildingKind.DefenseLab => ("Defense Lab", new Vector3(3, 2, 3), new Color(.4f, .5f, .6f), 60, 80, 0, 6, 12f, 300f, false),
                BuildingKind.RestRoom => ("Rest Room", new Vector3(3, 1.5f, 3), new Color(.8f, .7f, .55f), 40, 60, 0, 4, 8f, 300f, false),
                BuildingKind.Workshop => ("공방", new Vector3(3, 2, 3), new Color(.7f, .5f, .3f), GameBalance.WorkshopFood, GameBalance.WorkshopSoil, GameBalance.WorkshopSpecial, GameBalance.WorkshopAnts, GameBalance.WorkshopBuildSeconds, 300f, false),
                BuildingKind.Dormitory => ("숙소", new Vector3(3, 1.4f, 2.5f), new Color(.75f, .6f, .45f), GameBalance.DormitoryFood, GameBalance.DormitorySoil, 0, GameBalance.DormitoryAnts, GameBalance.DormitoryBuildSeconds, 250f, false),
                BuildingKind.Kitchen => ("식당", new Vector3(3, 1.5f, 3), new Color(.8f, .6f, .3f), 20, 40, 0, 0, 8f, 250f, false),
                BuildingKind.FlowerPot or BuildingKind.ShellDecoration or BuildingKind.MarbleMosaic or BuildingKind.BottleMobile or BuildingKind.FireflyLamp => (kind.ToString(), new Vector3(1, 1, 1), new Color(.7f, .75f, .4f), 0, 15, 0, 0, 6f, 100f, false),
                BuildingKind.Campfire => ("이야기 모닥불", new Vector3(1.6f, .6f, 1.6f), new Color(.85f, .45f, .2f), 0, GameBalance.CampfireSoil, 0, 0, 6f, 150f, false),
                BuildingKind.GamblingDen => ("도박장", new Vector3(2.5f, 1.2f, 2.5f), new Color(.55f, .35f, .5f), 0, GameBalance.GamblingDenSoil, 0, 0, 6f, 200f, false),
                BuildingKind.LeafWall => ("나뭇잎 벽", new Vector3(1, 1.5f, 1), new Color(.45f, .6f, .25f), 0, 10, 0, 1, 2f, 150f, false),
                BuildingKind.CapWall => ("병뚜껑 벽", new Vector3(1, 1.6f, 1), new Color(.6f, .62f, .66f), 0, 40, 2, 3, 6f, 700f, false),
                BuildingKind.Door => ("문", new Vector3(1, 1.5f, 1), new Color(.5f, .35f, .2f), 0, 15, 0, 2, 4f, 200f, true),
                BuildingKind.CastleWall => ("성벽", new Vector3(2, 2.6f, 2), new Color(.55f, .52f, .48f), 0, 80, 0, 6, 12f, 1500f, false),
                BuildingKind.Gate => ("성문", new Vector3(2, 2.6f, 2), new Color(.55f, .45f, .3f), 0, 60, 0, 5, 10f, 1200f, false),
                BuildingKind.Hut => ("초가집", new Vector3(2, 1.2f, 2), new Color(.78f, .68f, .42f), 0, GameBalance.HutSoil, 0, 3, 6f, 150f, false),
                BuildingKind.House => ("흙집", new Vector3(2.6f, 1.6f, 2.6f), new Color(.6f, .45f, .3f), 0, GameBalance.HouseSoil, 0, 5, 10f, 300f, false),
                // 위생·전력 1차(2026-10-05, 잠정 수치). 전선은 밟고 지나가는 납작한 칸.
                BuildingKind.Toilet => ("화장실", new Vector3(1, 1, 1), new Color(.85f, .85f, .8f), 0, 20, 0, 1, 4f, 100f, false),
                BuildingKind.Washbasin => ("세면대", new Vector3(1, .9f, 1), new Color(.75f, .85f, .9f), 0, 15, 0, 1, 3f, 100f, false),
                BuildingKind.Shower => ("샤워기", new Vector3(1, 1.8f, 1), new Color(.6f, .8f, .9f), 0, 30, 0, 2, 5f, 100f, false),
                BuildingKind.Treadmill => ("쳇바퀴", new Vector3(2, 1.6f, 1), new Color(.7f, .55f, .35f), 0, 30, 0, 2, 5f, 150f, false),
                BuildingKind.WoodGenerator => ("장작 발전기", new Vector3(2, 1.4f, 2), new Color(.45f, .3f, .2f), 0, 50, 0, 3, 8f, 200f, false),
                BuildingKind.PowerWire => ("전선", new Vector3(1, .08f, 1), new Color(.85f, .7f, .2f), 0, 2, 0, 0, 1f, 30f, true),
                BuildingKind.Battery => ("배터리", new Vector3(1, 1.2f, 1), new Color(.3f, .6f, .35f), 0, 40, 2, 2, 6f, 150f, false),
                BuildingKind.ElectricLamp => ("전등", new Vector3(.6f, 1.8f, .6f), new Color(.95f, .9f, .6f), 0, 10, 0, 1, 3f, 80f, false),
                // 가구 2차(2026-10-05, 잠정). 바닥은 밟고 지나가는 납작한 칸, 잠금문·창살문은 문과 같은 통로.
                BuildingKind.Hearth => ("화덕", new Vector3(1.5f, 1.2f, 1), new Color(.55f, .3f, .2f), 0, 30, 0, 2, 6f, 200f, false),
                BuildingKind.SleepingMat => ("자리", new Vector3(1, .1f, 2), new Color(.7f, .62f, .4f), 0, 5, 0, 0, 2f, 50f, true),
                BuildingKind.DoubleBed => ("큰침대", new Vector3(2, .6f, 2), new Color(.8f, .6f, .5f), 0, 40, 0, 2, 6f, 150f, false),
                BuildingKind.Floor => ("바닥", new Vector3(1, .05f, 1), new Color(.6f, .5f, .38f), 0, 3, 0, 0, 1f, 50f, true),
                BuildingKind.LockedDoor => ("잠금문", new Vector3(1, 1.5f, 1), new Color(.4f, .3f, .2f), 0, 25, 0, 2, 5f, 250f, true),
                BuildingKind.BarredDoor => ("창살문", new Vector3(1, 1.5f, 1), new Color(.45f, .45f, .5f), 0, 30, 0, 2, 5f, 300f, true),
                BuildingKind.SingleBed => ("침대", new Vector3(1, .5f, 2), new Color(.75f, .55f, .45f), 0, 20, 0, 1, 4f, 120f, false),
                BuildingKind.Bookshelf => ("책장", new Vector3(2, 2, .6f), new Color(.5f, .35f, .25f), 0, 30, 0, 0, 6f, 150f, false),
                BuildingKind.Bathtub => ("목욕통", new Vector3(2, .8f, 1.2f), new Color(.6f, .75f, .8f), 0, 35, 0, 0, 6f, 150f, false),
                BuildingKind.FoodStore => ("저장고", new Vector3(2, 1.4f, 2), new Color(.55f, .65f, .7f), 10, 40, 0, 3, 6f, 250f, false),
                BuildingKind.Jar => ("항아리", new Vector3(1, 1, 1), new Color(.7f, .5f, .35f), 0, 15, 0, 1, 3f, 100f, false),
                BuildingKind.Armory => ("무기고", new Vector3(2, 1.8f, 1.5f), new Color(.4f, .4f, .45f), 20, 60, 2, 3, 8f, 300f, false),
                // 가구 5차(2026-10-06, 잠정)
                BuildingKind.BunkBed => ("2층침대", new Vector3(1, 1.6f, 2), new Color(.7f, .5f, .4f), 0, 35, 0, 2, 6f, 150f, false),
                BuildingKind.Hammock => ("해먹", new Vector3(1, .6f, 2), new Color(.8f, .75f, .55f), 0, 10, 0, 0, 3f, 60f, false),
                BuildingKind.BigTable => ("큰 식탁", new Vector3(2, .8f, 3), new Color(.75f, .55f, .3f), 20, 60, 0, 0, 10f, 300f, false),
                BuildingKind.BoardGame => ("놀이판", new Vector3(1, .6f, 1), new Color(.7f, .6f, .4f), 0, 15, 0, 0, 4f, 80f, false),
                BuildingKind.Janggi => ("장기판", new Vector3(1, .6f, 1), new Color(.75f, .6f, .35f), 0, 20, 0, 0, 4f, 80f, false),
                BuildingKind.Baduk => ("바둑판", new Vector3(1, .6f, 1), new Color(.8f, .7f, .45f), 0, 20, 0, 0, 4f, 80f, false),
                BuildingKind.WrestlingRing => ("씨름판", new Vector3(3, .15f, 3), new Color(.85f, .75f, .5f), 0, 25, 0, 0, 5f, 100f, true),
                BuildingKind.DartBoard => ("가시 다트", new Vector3(1, 1.5f, .3f), new Color(.6f, .4f, .3f), 0, 15, 0, 0, 4f, 60f, false),
                BuildingKind.ExerciseRig => ("운동기구", new Vector3(1.5f, 1.2f, 1), new Color(.5f, .5f, .5f), 0, 30, 0, 0, 5f, 120f, false),
                BuildingKind.Instrument => ("악기", new Vector3(1, 1, 1), new Color(.6f, .35f, .2f), 0, 25, 0, 0, 5f, 80f, false),
                BuildingKind.WebSwing => ("거미줄 그네", new Vector3(1, 1.8f, 1), new Color(.9f, .9f, .9f), 0, 8, 0, 0, 2f, 40f, false),
                BuildingKind.Statue => ("조각상", new Vector3(1, 1.8f, 1), new Color(.75f, .75f, .7f), 0, 25, 0, 0, 8f, 150f, false),
                BuildingKind.Flag => ("깃발", new Vector3(.3f, 2, .3f), new Color(.8f, .3f, .25f), 0, 10, 0, 0, 3f, 60f, false),
                BuildingKind.Painting => ("그림", new Vector3(1, 1, .2f), new Color(.5f, .6f, .8f), 0, 15, 0, 0, 6f, 50f, false),
                BuildingKind.Pillar => ("기둥", new Vector3(.6f, 2.2f, .6f), new Color(.65f, .6f, .55f), 0, 15, 0, 0, 4f, 200f, false),
                // 가구 6차(2026-10-07, 잠정). 카펫은 밟고 지나가는 납작한 칸.
                BuildingKind.Brazier => ("화롯불", new Vector3(1, .8f, 1), new Color(.55f, .3f, .2f), 0, 15, 0, 0, 4f, 100f, false),
                BuildingKind.Lantern => ("등불", new Vector3(.5f, 1.2f, .5f), new Color(.9f, .8f, .5f), 0, 10, 0, 0, 3f, 60f, false),
                BuildingKind.Torch => ("횃불", new Vector3(.3f, 1.5f, .3f), new Color(.6f, .4f, .25f), 0, 5, 0, 0, 2f, 40f, false),
                BuildingKind.Carpet => ("카펫", new Vector3(2, .03f, 3), new Color(.65f, .25f, .25f), 0, 15, 0, 0, 4f, 50f, true),
                BuildingKind.Tapestry => ("태피스트리", new Vector3(2, 1.5f, .1f), new Color(.5f, .3f, .55f), 0, 20, 0, 0, 6f, 50f, false),
                BuildingKind.Monument => ("기념비", new Vector3(1.5f, 2.5f, 1.5f), new Color(.7f, .7f, .7f), 0, 60, 2, 0, 12f, 400f, false),
                BuildingKind.BronzeStatue => ("동상", new Vector3(1.2f, 2.4f, 1.2f), new Color(.6f, .45f, .25f), 0, 50, 3, 0, 12f, 300f, false),
                BuildingKind.TrophyCase => ("전리품 진열대", new Vector3(2, 1.6f, .8f), new Color(.45f, .35f, .3f), 0, 30, 1, 0, 8f, 150f, false),
                BuildingKind.CuriosDisplay => ("사람 물건 전시대", new Vector3(2, 1.4f, 1), new Color(.55f, .55f, .6f), 0, 40, 2, 0, 8f, 150f, false),
                BuildingKind.HotSpring => ("온천", new Vector3(3, .5f, 3), new Color(.55f, .75f, .8f), 0, 60, 0, 0, 10f, 250f, false),
                BuildingKind.BanquetTable => ("연회용 긴 식탁", new Vector3(1.5f, .8f, 5), new Color(.6f, .4f, .2f), 20, 80, 0, 0, 12f, 300f, false),
                BuildingKind.Apartment => ("큰 아파트", new Vector3(3, 3.2f, 3), new Color(.62f, .62f, .66f), 0, GameBalance.ApartmentSoil, GameBalance.ApartmentSpecial, 8, 16f, 500f, false),
                _ => (null, Vector3.one, Color.white, 0, 0, 0, 0, 0f, 0f, false)
            };
            if (name == null) return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.SetActive(false);
            go.name = kind + "Template";
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material.color = color;
            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.kind = kind; data.displayName = name;
            data.foodCost = food; data.soilCost = soil; data.specialCost = special;
            data.constructionAnts = ants; data.buildTimeSeconds = seconds; data.maxHealth = hp;
            // 저장 가구 한도(잠정): 저장고 = 식량 +150, 항아리 = 식량·재료 +50.
            if (kind == BuildingKind.FoodStore) (data.foodCapacityBonus, data.soilCapacityBonus, data.specialCapacityBonus) = (150, 0, 0);
            if (kind == BuildingKind.Jar) (data.foodCapacityBonus, data.soilCapacityBonus, data.specialCapacityBonus) = (50, 50, 0);
            BuildingBase building = kind switch
            {
                BuildingKind.ConscriptionPost => go.AddComponent<ConscriptionPost>(),
                BuildingKind.SoilWall => go.AddComponent<SoilWall>(),
                BuildingKind.TrapPit or BuildingKind.SpikeTrap => go.AddComponent<TrapPit>(),
                BuildingKind.AreaAcidTower => go.AddComponent<AreaAcidTower>(),
                BuildingKind.Watchtower => go.AddComponent<Watchtower>(),
                BuildingKind.MineField => go.AddComponent<MineField>(),
                BuildingKind.DefenseLab => go.AddComponent<DefenseLab>(),
                BuildingKind.Workshop => go.AddComponent<Workshop>(),
                BuildingKind.Dormitory => go.AddComponent<Dormitory>(),
                BuildingKind.Kitchen => go.AddComponent<Kitchen>(),
                _ when Decoration.IsKind(kind) => go.AddComponent<Decoration>(),
                _ when RecreationSpot.IsKind(kind) => go.AddComponent<RecreationSpot>(),
                BuildingKind.BigTable or BuildingKind.BanquetTable => go.AddComponent<Kitchen>(),
                BuildingKind.Hut or BuildingKind.House or BuildingKind.Apartment => go.AddComponent<Housing>(),
                BuildingKind.LeafWall or BuildingKind.CapWall or BuildingKind.CastleWall => go.AddComponent<Wall>(),
                BuildingKind.Door => go.AddComponent<Door>(),
                BuildingKind.Gate => go.AddComponent<Gate>(),
                BuildingKind.FoodStore or BuildingKind.Jar => go.AddComponent<Storage>(),
                BuildingKind.Armory => go.AddComponent<Armory>(),
                BuildingKind.Hearth => go.AddComponent<Kitchen>(),
                BuildingKind.SleepingMat or BuildingKind.DoubleBed or BuildingKind.SingleBed or BuildingKind.BunkBed or BuildingKind.Hammock => go.AddComponent<Dormitory>(),
                BuildingKind.Floor => go.AddComponent<FloorTile>(),
                BuildingKind.LockedDoor or BuildingKind.BarredDoor => go.AddComponent<Door>(),
                BuildingKind.Toilet or BuildingKind.Washbasin or BuildingKind.Shower => go.AddComponent<HygieneFixture>(),
                BuildingKind.Treadmill or BuildingKind.WoodGenerator or BuildingKind.PowerWire or BuildingKind.Battery or BuildingKind.ElectricLamp => go.AddComponent<PowerNode>(),
                _ => go.AddComponent<RestRoom>()
            };
            building.ConfigureRuntime(data);
            if (walkable) go.GetComponent<Collider>().isTrigger = true;
            else go.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
            return go;
        }
    }
}
