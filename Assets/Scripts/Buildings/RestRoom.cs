using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 휴게실: 8m 안에서 대기 중인 장수 최대 4명이 피로를 두 배로 풀고 "휴식" 기분 +5를 얻는다.
    public sealed class RestRoom : BuildingBase
    {
        private static readonly List<RestRoom> Active = new List<RestRoom>();
        private readonly List<CommanderAnt> seats = new List<CommanderAnt>();
        public IReadOnlyList<CommanderAnt> Seats => seats;
        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { Active.Remove(this); seats.Clear(); base.OnDisable(); }

        public static bool Serves(CommanderAnt c) => Active.Any(r => r != null && !r.IsDead && r.seats.Contains(c));

        private void Update() => RefreshSeats();

        public void RefreshSeats()
        {
            seats.Clear();
            if (IsDead || CommanderRoster.Instance == null) return;
            foreach (var c in CommanderRoster.Instance.Commanders
                .Where(c => c.CanReceiveOrders && !c.IsAwayFromHome && !c.IsWorking && !c.LabUpgradeBusy
                    && Vector3.Distance(c.Position, Position) <= GameBalance.RestRoomRadius
                    && !Active.Any(r => r != this && r.seats.Contains(c)))
                .OrderBy(c => (c.Position - Position).sqrMagnitude).Take(GameBalance.RestRoomSeats))
                seats.Add(c);
        }
    }
}
