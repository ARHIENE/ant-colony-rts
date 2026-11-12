using System;
using System.Linq;
using System.Reflection;
public static class KiteUser1112Control{
 public static string Restore()=>Call("Restore");
 public static string Snapshot()=>Call("Snapshot");
 static string Call(string method){var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("KiteUser1112")).Where(t=>t!=null).LastOrDefault(t=>t.GetField("saved",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)!=null);return t==null?"no saved fixture":(string)t.GetMethod(method).Invoke(null,null);}
}
