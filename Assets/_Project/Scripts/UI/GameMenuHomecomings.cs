using System.Globalization;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private void HomecomingEntries()
        {
            var dm = DiplomacyManager.Instance;
            if (dm == null) { MenuTheme.Text(content, "외교 정보 준비 대기", 13, 28); return; }
            foreach (var r in dm.Data.homecomings)
            {
                string reason = dm.HomecomingWaitReason(r);
                MenuTheme.Text(content, $"{r.commander?.Name} · {(string.IsNullOrEmpty(r.to) ? "우리 본거지로" : "상대 세력으로")} · {DiplomacyManager.HomecomingTimeLabel(r.arrive - dm.Data.elapsed)}"
                    + (r.gear.Count > 0 ? $" · 동반 장비 {r.gear.Count}" : "") + (reason == "" ? "" : "\n대기: " + reason), 13, reason == "" ? 52 : 76);
            }
            if (dm.Data.homecomings.Count == 0) MenuTheme.Text(content, "귀환 중인 장수가 없습니다.", 13, 28);
            var at = dm.HomecomingAnchor;
            MenuTheme.Text(content, $"진입 기준: {(dm.Data.homecomingEntrySet ? "직접 지정" : "본거지 기준점에서 가장 가까운 지도 경계")}\nX {at.x:0.##} / Y {at.y:0.##} / Z {at.z:0.##}", 13, 56);
            MenuTheme.Text(content, "대체 진입 좌표 X / Y(높이) / Z\n본거지 안의 빈 보행 지점만 허용. 적 잔류 시 귀환 대기.", 13, 52);
            InputField Coordinate(string name, float value)
            {
                var field = MenuTheme.Input(content, value.ToString("0.##", CultureInfo.InvariantCulture));
                field.gameObject.name = "HomecomingEntry" + name;
                field.contentType = InputField.ContentType.DecimalNumber;
                return field;
            }
            var x = Coordinate("X", at.x); var y = Coordinate("Y", at.y); var z = Coordinate("Z", at.z);
            MenuTheme.Button(content, "대체 안전 진입점 지정", () =>
            {
                if (!float.TryParse(x.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float px)
                    || !float.TryParse(y.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float py)
                    || !float.TryParse(z.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float pz))
                { ToastManager.Show("X / Y / Z에 숫자를 입력하세요."); return; }
                if (!dm.SetHomecomingEntry(new Vector3(px, py, pz), out var error)) { ToastManager.Show(error); return; }
                ShowPrisoners(null);
            });
            MenuTheme.Button(content, "기본 경계 진입으로 복귀", () => { dm.ResetHomecomingEntry(); ShowPrisoners(null); });
        }
    }
}
