using System.Collections.Generic;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class WorldMapManager
    {
        public ExpeditionTransport CreateWalkingParty(IReadOnlyList<CommanderAnt> commanders)
        {
            if (ExpeditionRules.Paused) return null;
            if (transportTemplate == null || commanders == null || commanders.Count == 0 || commanders[0] == null) return null;
            if (!AntColony.Core.HomeReturnPoint.TryGround(commanders[0].Position, out var gate)
                || !commanders[0].TryWorkApproach(gate, out gate))
            { AntColony.UI.ToastManager.Show("출정 길목까지 이어지는 경로가 없습니다."); return null; }
            var party = CreateTransport(false, gate, true);
            if (party.TryBoard(commanders)) return party;
            transports.Remove(party);
            Destroy(party.gameObject);
            return null;
        }
    }
}
