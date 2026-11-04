using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public enum CivicServiceKind { PublicService, School, Clinic }

    // 시민용 진료소는 장수 치료 침상(Infirmary)과 별개다.
    public sealed class CivicService : BuildingBase
    {
        public CivicServiceKind Kind;
        public BuildingKind SavedKind => Kind switch { CivicServiceKind.School => BuildingKind.School,
            CivicServiceKind.Clinic => BuildingKind.CitizenClinic, _ => BuildingKind.CivicPublicService };

        internal static GameObject CreateTemplate(BuildingKind kind)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.SetActive(false); go.name = kind + "Template";
            go.transform.localScale = new Vector3(3, 2, 3);
            var definition = ScriptableObject.CreateInstance<BuildingData>();
            definition.kind = kind;
            definition.displayName = kind == BuildingKind.School ? "학당" : kind == BuildingKind.CitizenClinic ? "시민 진료소" : "공공서비스";
            // shortcut: 건설 수치는 BuildingData 기본값 재사용, 전용 밸런스 확정 시 교체.
            var service = go.AddComponent<CivicService>();
            service.Kind = kind == BuildingKind.School ? CivicServiceKind.School
                : kind == BuildingKind.CitizenClinic ? CivicServiceKind.Clinic : CivicServiceKind.PublicService;
            service.ConfigureRuntime(definition);
            go.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
            return go;
        }
        private static readonly List<CivicService> Active = new List<CivicService>();
        public static int Count(CivicServiceKind kind) => Active.Count(b => b != null && b.isActiveAndEnabled && !b.IsDead && b.Kind == kind);
        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { Active.Remove(this); base.OnDisable(); }
    }
}
