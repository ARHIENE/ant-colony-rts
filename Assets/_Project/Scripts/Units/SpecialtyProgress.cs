using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    [Serializable]
    public sealed class SpecialtyProgress
    {
        public int version;
        // 계열13개 × 골드/다이아. 0미도달/1소급제외/2확률실패/3슬롯부족/4획득/5동급이상보유.
        public int[] results = new int[26];
        public List<CommanderTrait> innate = new List<CommanderTrait>();
        public static SpecialtyProgress Copy(SpecialtyProgress value) => value == null ? null
            : JsonUtility.FromJson<SpecialtyProgress>(JsonUtility.ToJson(value));
        public static bool Valid(SpecialtyProgress value)
        {
            if (value == null) return true; // 옛 저장
            if (value.version < 0 || value.version > 1 || value.results == null || value.results.Length != 26
                || value.results.Any(r => r < 0 || r > 5) || value.innate == null || value.innate.Count > CommanderTraits.MaxTraits) return false;
            if (value.version == 0 && (value.results.Any(r => r != 0) || value.innate.Count > 0)) return false;
            var traits = new CommanderTraits();
            return value.innate.All(t => Enum.IsDefined(typeof(CommanderTrait), t) && traits.TryAdd(t));
        }
    }
}
