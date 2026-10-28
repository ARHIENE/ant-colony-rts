using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class QualityProfiles1028Checks
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before changing build inclusion");
        var backup = "Temp/performance1028/quality-before-inclusion.asset";
        if (!File.Exists(backup)) File.Copy("ProjectSettings/QualitySettings.asset", backup);
        var asset = Unsupported.GetSerializedAssetInterfaceSingleton("QualitySettings");
        var serialized = new SerializedObject(asset);
        var profiles = serialized.FindProperty("m_QualitySettings");
        var changed = false;
        for (var i = 0; i < profiles.arraySize; i++)
        {
            var profile = profiles.GetArrayElementAtIndex(i);
            if (profile.FindPropertyRelative("name").stringValue != "Mobile") continue;
            var excluded = profile.FindPropertyRelative("excludedTargetPlatforms");
            for (var n = excluded.arraySize - 1; n >= 0; n--)
                if (excluded.GetArrayElementAtIndex(n).stringValue == "Standalone")
                { excluded.DeleteArrayElementAtIndex(n); changed = true; }
        }
        if (changed) { serialized.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(asset); }
        serialized.Update();
        for (var i = 0; i < profiles.arraySize; i++)
        {
            var profile = profiles.GetArrayElementAtIndex(i);
            if (profile.FindPropertyRelative("name").stringValue != "Mobile") continue;
            var excluded = profile.FindPropertyRelative("excludedTargetPlatforms");
            for (var n = 0; n < excluded.arraySize; n++)
                if (excluded.GetArrayElementAtIndex(n).stringValue == "Standalone") throw new Exception("Low quality still excluded from PC builds");
        }
        return "PASS existing low-quality profile included in Standalone builds; existing PC default/pipeline unchanged";
    }
}
