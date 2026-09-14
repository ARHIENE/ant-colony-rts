namespace AntColony.Regression
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using AntColony.Buildings;
    using AntColony.Core;
    using AntColony.Data;
    using AntColony.Units;
    using AntColony.World;
    using UnityEngine;
    using UnityEngine.AI;
    using Object = UnityEngine.Object;
    public static class InvasionChecks
    {
        public static async Task<string> Main()
        {
            if (!Application.isPlaying) throw new Exception("Play mode required.");
            var origin = new Vector3(1000,0,1000);
            var objects = new List<Object>();
            var sceneInvasion = GameObject.Find("EnemyNestPrototype").GetComponent<ColonyInvasion>();
            sceneInvasion.enabled = false;
            var nav = default(NavMeshDataInstance);
            var random = UnityEngine.Random.state;
            var waves = default(ColonyInvasion);
            var loopEvents = 0;
            Action onLoop = () => loopEvents++;
            GameManager.Instance.OnLoopComplete += onLoop;
            GameObject New(string name, Vector3 position) { var go = new GameObject(name); go.transform.position=position; objects.Add(go); return go; }
            try
            {
                var navData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> { new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Box, size=new Vector3(60,.2f,60), transform=Matrix4x4.TRS(origin-Vector3.up*.1f,Quaternion.identity,Vector3.one) } },new Bounds(origin,new Vector3(60,10,60)),Vector3.zero,Quaternion.identity);
                objects.Add(navData); nav=NavMesh.AddNavMeshData(navData);
                var target = New("InvasionTestBase",origin+Vector3.right*8).AddComponent<BuildingBase>();
                var enemyObject = New("InvasionTestNestBuilding",origin+Vector3.back*12);
                enemyObject.SetActive(false);
                var enemyBuilding=enemyObject.AddComponent<BuildingBase>();
                Set(enemyBuilding,"countsTowardPlayerDefeat",false); enemyObject.SetActive(true);
                var root=New("InvasionTestColony",origin); root.SetActive(false);
                var colony=root.AddComponent<EnemyColony>(); Set(colony,"buildings",new[]{enemyBuilding});
                var foodObject=New("InvasionTestFood",origin); foodObject.transform.SetParent(root.transform);
                var food=foodObject.AddComponent<ResourceNode>(); Set(food,"resourceType",ResourceType.Food); Set(food,"amountRemaining",100f); Set(food,"ownerColony",colony);
                var soilObject=New("InvasionTestSoil",origin); soilObject.transform.SetParent(root.transform);
                var soil=soilObject.AddComponent<ResourceNode>(); Set(soil,"resourceType",ResourceType.Soil); Set(soil,"amountRemaining",100f); Set(soil,"ownerColony",colony);
                var templateObject=New("InvasionTestTemplate",origin); templateObject.SetActive(false);
                var template=templateObject.AddComponent<WildMonster>();
                Set(template,"moveSpeed",10f); Set(template,"detectionRadius",4f);
                waves=root.AddComponent<ColonyInvasion>();
                Set(waves,"raiderTemplate",template); Set(waves,"spawnPoint",root.transform);
                Set(waves,"firstWaveDelay",.15f); Set(waves,"waveInterval",100f);
                Set(waves,"spawnScatter",0f); Set(waves,"firstWaveCount",1); Set(waves,"maxActiveRaiders",3);
                Set(waves,"maxBuildings",1);
                root.SetActive(true);
                Check(Raiders(waves).Count==0,"first wave delay");
                await Until(()=>Raiders(waves).Count==1,"timed first spawn");
                Check(food.AmountRemaining==95f,"first raider spends enemy food stock");
                var raider=Raiders(waves)[0];
                Check(raider.GetComponent<NavMeshAgent>().isOnNavMesh,"spawn on NavMesh");
                Check(raider.transform.parent==null,"raiders independent of nest");
                await Until(()=>target.CurrentHealth<target.MaxHealth,"real march and building damage");
                var defenderObject=New("InvasionTestDefender",raider.Position+Vector3.forward);
                var defender=defenderObject.AddComponent<CommanderAnt>();
                var data=ScriptableObject.CreateInstance<UnitData>(); objects.Add(data);
                data.role=UnitRole.Defense; data.maxHealth=1000; data.attackDamage=0; data.attackRange=3; data.attackInterval=.1f;
                defender.ConfigureCommander("Defender",CommanderRank.General,new[]{UnitRole.Defense},UnitRole.Defense);
                defender.Initialize(data,null,null);
                AntPool.Instance.Breed(40);
                Check(defender.TryAssign(40),"commander receives defense troops");
                await Until(()=>ReferenceEquals(Get(raider,"currentTarget"),defender),"defender intercepts building attacker");
                defender.CommandAttack(raider);
                defender.Data.attackDamage=200f;
                await Until(()=>raider==null,"real soldier defense kills raider");
                Check(loopEvents==0,"raider death is not wild-monster victory");
                defenderObject.SetActive(false);
                Set(waves,"timer",0f);
                await Until(()=>Raiders(waves).Count==2,"second wave grows to two");
                Set(waves,"timer",0f);
                await Until(()=>Raiders(waves).Count==3,"active cap limits wave");
                Set(waves,"timer",0f); await Task.Delay(100);
                Check(Raiders(waves).Count==3,"cap prevents accumulation");
                colony.enabled=false; foreach(var r in Raiders(waves)) if(r!=null) r.TakeDamage(10000);
                await Task.Delay(80); Set(waves,"timer",0f); await Task.Delay(100);
                Check(Raiders(waves).TrueForAll(r=>r==null||r.IsDead),"disabled colony cannot spawn");
                colony.enabled=true; Set(waves,"timer",0f);
                await Until(()=>Raiders(waves).Exists(r=>r!=null&&!r.IsDead),"enabled colony resumes");
                enemyBuilding.TakeDamage(10000); Set(waves,"timer",0f);
                var survivors=Raiders(waves).FindAll(r=>r!=null&&!r.IsDead).Count;
                await Task.Delay(100);
                Check(colony.IsDefeated && Raiders(waves).FindAll(r=>r!=null&&!r.IsDead).Count==survivors,"nest destruction stops reinforcements and preserves deployed force");
                var ordinary=New("InvasionTestOrdinary",origin).AddComponent<WildMonster>();
                Set(ordinary,"detectionRadius",0f); await Task.Delay(600);
                Check(Get(ordinary,"currentTarget")==null,"ordinary monster does not raid buildings");
                return "PASS: delayed/growing/capped waves, valid NavMesh spawn, real march/building attack, defender interception and kill, no false victory, disable/resume, defeated nest stops spawning, ordinary monsters unchanged.";
            }
            finally
            {
                GameManager.Instance.OnLoopComplete -= onLoop;
                if(waves!=null) { waves.enabled=false; foreach(var r in Raiders(waves)) if(r!=null) Object.Destroy(r.gameObject); }
                for(var i=objects.Count-1;i>=0;i--) if(objects[i]!=null) Object.DestroyImmediate(objects[i]);
                if(nav.valid) nav.Remove();
                UnityEngine.Random.state=random;
                if(sceneInvasion!=null) sceneInvasion.enabled=true;
            }
        }
        static List<WildMonster> Raiders(ColonyInvasion w)=>(List<WildMonster>)Get(w,"raiders");
        static FieldInfo Field(object o,string n) { for(var t=o.GetType();t!=null;t=t.BaseType) { var f=t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); if(f!=null)return f; } throw new Exception(n); }
        static object Get(object o,string n)=>Field(o,n).GetValue(o);
        static void Set(object o,string n,object v)=>Field(o,n).SetValue(o,v);
        static void Check(bool ok,string n) { if(!ok)throw new Exception("FAIL: "+n); }
        static async Task Until(Func<bool> condition,string n) { var end=DateTime.UtcNow.AddSeconds(8); while(!condition()) { if(DateTime.UtcNow>end)throw new Exception("TIMEOUT: "+n); await Task.Delay(25); } }
    }
}
