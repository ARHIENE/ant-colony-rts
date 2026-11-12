using AntColony.Units;

namespace AntColony.UI
{
    // 저장된 원인·도감 범주는 바꾸지 않고 표시할 때만 변환한다.
    public static class PersonalDisplayNames
    {
        public static string Break(MentalBreak kind) => kind switch
        {
            MentalBreak.Idle => "멍하니 있음", MentalBreak.Flee => "정착지 이탈",
            MentalBreak.AttackBuilding => "시설 공격", MentalBreak.AttackCommander => "동료 공격",
            MentalBreak.SelfHarm => "자해", MentalBreak.Binge => "폭식", MentalBreak.Wander => "배회", _ => ""
        };
        public static string Slot(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => "무기", EquipmentSlot.Armor => "방어구", EquipmentSlot.Trinket => "장신구", _ => "장비"
        };
        public static string Category(string value) => value switch
        { "Ants" => "장수", "World" => "세계", "Bosses" => "보스", _ => value };
        public static string DiscoveryTitle(AntColony.Save.DiscoveryDto entry)
        {
            if (entry.key == "boss:MiniBird") return "미니버드";
            if (entry.category == "World" && entry.key.StartsWith("site:")) return SiteNames.Korean(entry.title);
            if (entry.category == "Ants" && entry.key.StartsWith("weapon:"))
            {
                var weapon = entry.key.Substring(7);
                if (weapon == "Bare mandibles") return "맨손";
                if (System.Enum.TryParse<WeaponKind>(weapon, out var kind) && System.Enum.IsDefined(typeof(WeaponKind), kind))
                    return CommanderAnt.WeaponDisplayName(kind);
            }
            return entry.title;
        }
        public static string DiscoveryBody(AntColony.Save.DiscoveryDto entry)
        {
            var body = entry.body ?? "";
            if (entry.category == "Ants" && entry.key.StartsWith("weapon:") && body.StartsWith("All commanders can work and fight"))
                return "모든 장수는 작업과 전투를 할 수 있습니다. 장비가 전투 방식을, 지휘 기술이 병력 한도를 정합니다.";
            if (entry.key == "boss:MiniBird" && body.StartsWith("Encountered at "))
            {
                var end = body.IndexOf(". Watch the marked ground attack areas", System.StringComparison.Ordinal);
                if (end > 15) return SiteNames.Korean(body.Substring(15, end - 15))
                    + "에서 발견. 지면의 공격 예고 범위를 확인하고 공격 전에 피하세요. 구체적인 약점은 아직 발견하지 못했습니다.";
            }
            if (entry.category == "World" && entry.key.StartsWith("site:"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(body, @"^(.*?) / (.*?)\. Fixed difficulty (\d+)\.$");
                if (match.Success) return SiteNames.Korean(match.Groups[1].Value) + " · " + SiteNames.Korean(match.Groups[2].Value)
                    + " · 고정 위험도 D" + match.Groups[3].Value;
            }
            return body;
        }
        public static string Mood(string value)
        {
            if (value == null) return "";
            if (value.StartsWith("Death: ", System.StringComparison.Ordinal)) return "사망: " + value.Substring(7);
            if (value.StartsWith("Downed: ", System.StringComparison.Ordinal)) return "쓰러짐: " + value.Substring(8);
            return value switch
            {
                "Hunger" => "허기", "Rest" => "휴식", "Catharsis" => "감정 해소",
                "DeathEvent" => "죽음 목격", _ => value
            };
        }
    }
}
