using System;
using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 목장 생물 사체(2026-10-11): 자연사·굶주림·전투·포식·도축 모두 사체가 남는다. 운반 장수가 도축대로 옮기고 요리 장수가 해체한다.
    // 포식자는 먹은 만큼 해체 자원(고기)을 줄이고 다 먹으면 사라진다. 장수가 집으면 섭식이 중단된다.
    public sealed class CritterCarcass : MonoBehaviour
    {
        public static readonly List<CritterCarcass> All = new List<CritterCarcass>();
        [Serializable] public class State { public int species; public float meat; public int chitin; public bool announced, predation; public string predator = ""; }
        internal State Data = new State();
        public Species Species => (Species)Data.species;
        public float Meat => Data.meat;
        public int Chitin => Data.chitin;
        public bool Announced => Data.announced;
        public Corpse Corpse => GetComponent<Corpse>();
        public CommanderAnt Carrier { get; internal set; }
        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        internal static CritterCarcass Drop(Critter critter, Critter killer)
        {
            var info = critter.Info;
            var corpse = Corpse.Spawn(new Corpse.State { position = critter.transform.position, kind = CorpseKind.Wildlife, name = info.name, count = 1 });
            var carcass = corpse.gameObject.AddComponent<CritterCarcass>();
            carcass.Data = new State { species = (int)critter.Species, meat = info.meat, chitin = info.chitin, predation = killer != null, predator = killer != null ? killer.Info.name : "" };
            corpse.name = info.name + " 사체";
            return carcass;
        }
        // 포식: 먹잇감 사체를 포식자가 처음 먹는 순간에만 한 번 알린다(클릭 시 위치로).
        internal bool Eat(float units, Critter eater)
        {
            if (Data.meat <= 0 || Carrier != null) return false;
            if (!Data.announced && Data.predation)
            {
                Data.announced = true; var at = transform.position;
                ToastManager.ShowAt($"{SpeciesInfo.For(Species).name}이(가) {eater.Info.name}에게 잡아먹혔습니다.", Critter.FocusAction(at));
            }
            Data.meat = Mathf.Max(0, Data.meat - units);
            if (Data.meat <= 0) { gameObject.SetActive(false); Destroy(gameObject); }
            return true;
        }
        internal void FollowCarrier() { if (Carrier != null) transform.position = Carrier.Position + Vector3.up * 1.1f; }
        private void Update() => FollowCarrier();
    }
}
