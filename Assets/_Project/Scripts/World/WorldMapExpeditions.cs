using System.Collections.Generic;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class WorldMapManager
    {
        public ExpeditionTransport CreateWalkingParty(IReadOnlyList<CommanderAnt> commanders)
        {
            if (transportTemplate == null || commanders == null || commanders.Count == 0 || commanders[0] == null) return null;
            var party = CreateTransport(false, commanders[0].Position, true);
            if (party.TryBoard(commanders)) return party;
            transports.Remove(party);
            Destroy(party.gameObject);
            return null;
        }
    }
}
