using System;
using System.Linq;
using UnityEditorInternal;
public static class Profiler1115Reflect {
 public static string Main()=>string.Join("\\n",typeof(ProfilerDriver).GetProperties().Select(p=>p.ToString()))+"\\n"+string.Join("\n",typeof(ProfilerDriver).GetMethods().Where(m=>m.Name.Contains("Frame")||m.Name.Contains("Profile")).Select(m=>m.ToString()))+"\nRAW\n"+string.Join("\n",typeof(UnityEditor.Profiling.RawFrameDataView).GetMethods().Where(m=>m.Name.Contains("Sample")||m.Name.Contains("valid")||m.Name.Contains("thread")||m.Name.Contains("frame")).Select(m=>m.ToString()));
}

