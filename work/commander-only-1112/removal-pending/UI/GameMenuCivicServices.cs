using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void CivicServices()
        { Roster(); }

        public void FinalBattleReserveWarning(Action activate = null, IEnumerable<ReserveRequirement> fuelAndAmmo = null)
        {
            var f = Frame("최종전 비축"); var p = L.Plate(f, "FinalBattleReserves", 300, 100, 840, 660);
            L.Label(p, "가동 전 · 권장 비축", 20, 24, 16, 792, 34);
            L.Label(p, "비축 부족은 경고입니다. 가동하면 15일 준비를 중단할 수 없습니다.", 13, 24, 58, 792, 32, MenuTheme.Warning);
            var list = L.List(p, 24, 104, 792, 470);
            foreach (var line in FinalBattleEconomy.ReserveLines(fuelAndAmmo))
            {
                var row = L.Cell(list, "Reserve", 54);
                L.Label(row, line, 13, 8, 0, 760, 50);
            }
            L.Button(p, "Back", "돌아가기", 24, 600, 140, 36, CivicServices);
            if (activate != null) L.Button(p, "Activate Confirm", "확인 · 가동", 636, 600, 180, 36, () => activate(), primary: true);
        }
    }
}
