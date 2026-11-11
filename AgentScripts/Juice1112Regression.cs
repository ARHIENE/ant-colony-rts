using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.UI;
public static class Juice1112Regression
{
 public static async Task<string> Run()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  var go=new GameObject("Temporary repeated hit regression");var rest=new Vector3(.7f,1.3f,.9f);go.transform.localScale=rest;float max=1;
  try {
   for(int i=0;i<100;i++) {
    Juice.Punch(go.transform);if(i%3==0)Juice.Pop(go.transform);
    await Task.Delay(12);
    max=Mathf.Max(max,go.transform.localScale.y/rest.y);
   }
   await Task.Delay(500);
   var error=Vector3.Distance(go.transform.localScale,rest);
   string result=$"Repeated100hits+34pops maxScale={max:F4} finalError={error:F6}";
   File.WriteAllText("work/animation-1112/repeated-hit-regression.txt",result);
   if(max>1.15f||error>.0001f)throw new Exception(result);
   return result+" PASS";
  } finally {UnityEngine.Object.Destroy(go);}
 }
}
