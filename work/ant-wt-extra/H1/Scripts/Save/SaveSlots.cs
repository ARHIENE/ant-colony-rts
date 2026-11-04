using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AntColony.Save
{
    // 슬롯 구성. 기본값은 수동 3칸 + 자동 1칸이며 상수만 고치면 늘릴 수 있다.
    public static class SaveSlots
    {
        public const int ManualSlotCount = 3;
        public const int AutoSlotCount = 1;

        public static string FileName(bool auto, int index) => auto ? $"auto{index + 1}.json" : $"manual{index + 1}.json";

        public static string PathFor(bool auto, int index) => Path.Combine(SaveStorage.SavesFolder, FileName(auto, index));

        public static string DisplayName(bool auto, int index) => auto ? $"Auto {index + 1}" : $"Slot {index + 1}";

        public sealed class SlotInfo
        {
            public bool Auto;
            public int Index;
            public string Path;
            public bool Exists;
            public bool Valid;
            public string Error;
            public string SavedAtUtc;
            public string Label;
            public float PlaySeconds;
            public int Version;

            public string DisplayName => SaveSlots.DisplayName(Auto, Index);

            public string Summary()
            {
                if (!Exists) return "Empty";
                if (!Valid) return "Damaged: " + Error;
                var local = SavedAtUtc;
                if (DateTime.TryParse(SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    local = parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                var minutes = Mathf.FloorToInt(PlaySeconds / 60f);
                return $"{local}   played {minutes}m   {Label}";
            }
        }

        public static List<SlotInfo> List()
        {
            var slots = new List<SlotInfo>();
            for (var i = 0; i < ManualSlotCount; i++) slots.Add(Inspect(false, i));
            for (var i = 0; i < AutoSlotCount; i++) slots.Add(Inspect(true, i));
            return slots;
        }

        public static SlotInfo Inspect(bool auto, int index)
        {
            var info = new SlotInfo { Auto = auto, Index = index, Path = PathFor(auto, index) };
            info.Exists = File.Exists(info.Path);
            if (!info.Exists) return info;
            if (!SaveStorage.TryReadText(info.Path, out var text, out var readError))
            {
                info.Error = readError;
                return info;
            }
            if (!SaveValidator.TryParse(text, out var file, out var error))
            {
                info.Error = error;
                return info;
            }
            info.Valid = true;
            info.SavedAtUtc = file.savedAtUtc;
            info.Label = file.label;
            info.PlaySeconds = file.playSeconds;
            info.Version = file.version;
            return info;
        }
    }
}
