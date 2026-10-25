using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using UnityEngine;
using Object=UnityEngine.Object;
public static class BiomeClimateWeatherChecks
{
 static int n;static void Check(bool ok,string s){if(!ok)throw new Exception("FAIL "+s);n++;}
 static int Count(Season season,MapBiome biome,MapClimate climate,Func<WeatherKind,bool> test)=>Enumerable.Range(0,1000).Count(i=>test(WeatherSystem.Roll(season,biome,(i+.5f)/1000,climate)));
 public static async Task<string> Main()
 {
  n=0;var session=GameSession.Instance;var options=session.Options.Clone();var started=session.GameStarted;var play=session.PlaySeconds;var seconds=session.GameSeconds;var time=Time.timeScale;
  var weather=WeatherSystem.Instance?.CaptureState();var host=new GameObject("Biome climate weather checks");Time.timeScale=0;
  try
  {
   foreach(MapBiome biome in Enum.GetValues(typeof(MapBiome)))foreach(MapClimate climate in Enum.GetValues(typeof(MapClimate)))foreach(Season season in Enum.GetValues(typeof(Season)))
   {
    var kinds=Enumerable.Range(0,1000).Select(i=>WeatherSystem.Roll(season,biome,(i+.5f)/1000,climate)).ToArray();
    Check(kinds.All(k=>Enum.IsDefined(typeof(WeatherKind),k)),"valid rolls "+biome+climate+season);
    if(biome==MapBiome.Cave)Check(kinds.All(k=>k==WeatherKind.Clear||k==WeatherKind.Fog),"underground no outdoor precipitation");
    if(biome!=MapBiome.Desert)Check(!kinds.Contains(WeatherKind.Sandstorm),"sand only in desert");
    if(climate==MapClimate.TropicalWet)Check(!kinds.Any(k=>k==WeatherKind.Snow||k==WeatherKind.Blizzard),"tropical no snow");
    if(climate==MapClimate.Cold)Check(!kinds.Any(k=>k==WeatherKind.Rain||k==WeatherKind.Storm),"cold precipitation falls as snow");
   }
   Check(Count(Season.Spring,MapBiome.Desert,MapClimate.Temperate,k=>k==WeatherKind.Rain||k==WeatherKind.Storm)<20,"desert spring rain below 2 percent");
   Check(Count(Season.Spring,MapBiome.Desert,MapClimate.Temperate,k=>k==WeatherKind.Rain)>0,"rare desert rain still possible");
   Check(Count(Season.Winter,MapBiome.Desert,MapClimate.Temperate,k=>k==WeatherKind.Snow)<20,"desert winter snow below 2 percent");
   Check(Count(Season.Winter,MapBiome.Desert,MapClimate.Cold,k=>k==WeatherKind.Snow)>Count(Season.Winter,MapBiome.Desert,MapClimate.Temperate,k=>k==WeatherKind.Snow),"cold desert has more snow than temperate desert");
   Check(Count(Season.Spring,MapBiome.Waterside,MapClimate.Temperate,k=>k==WeatherKind.Rain||k==WeatherKind.Fog)>Count(Season.Spring,MapBiome.City,MapClimate.Temperate,k=>k==WeatherKind.Rain||k==WeatherKind.Fog),"waterside rain fog more frequent");
   var terrain=host.AddComponent<AntColony.Map.MapGenerator>();terrain.enabled=false;SeasonVisuals.Ensure(terrain,Color.white);WeatherSystem.Ensure(host);await Task.Yield();
   var visual=host.GetComponent<SeasonVisuals>();var late=typeof(SeasonVisuals).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);var fall=host.GetComponentsInChildren<ParticleSystem>().First(p=>p.name=="Season Fall");
   foreach(var biome in BiomeRules.All)foreach(MapClimate climate in Enum.GetValues(typeof(MapClimate)))foreach(Season season in Enum.GetValues(typeof(Season)))
   {
    var next=options.Clone();next.biome=biome;next.climate=climate;session.SetOptions(next);session.MarkStarted(play,(int)season*GameCalendar.SecondsPerSeason+10);WeatherSystem.Instance.Set(WeatherKind.Clear);late.Invoke(visual,null);
    var expected=season==Season.Spring&&SeasonVisuals.HasSpringPetals(biome,climate)||season==Season.Autumn&&SeasonVisuals.HasSeasonalLeaves(biome,climate);
    Check((fall.emission.rateOverTime.constant>0)==expected,"actual seasonal particles "+biome+climate+season);
    Check((Shader.GetGlobalFloat("_SeasonWinter")>0)==(season==Season.Winter&&SeasonVisuals.HasSeasonalSnow(biome,climate)),"actual snow shader "+biome+climate+season);
    WeatherSystem.Instance.RestoreState(new WeatherSystem.State{kind=(int)WeatherKind.Snow,remaining=123});
    Check(WeatherSystem.Current==WeatherSystem.ForEnvironment(WeatherKind.Snow,biome,climate),"loaded weather fits environment "+biome+climate+season);
   }
   var result="PASS "+n+" biome/climate/season weather checks; 84 roll combinations + 72 particle/shader/restore environments";System.IO.File.WriteAllText("Temp/biome-weather-result.txt",result);return result;
  }catch(Exception e){System.IO.File.WriteAllText("Temp/biome-weather-result.txt",e.ToString());throw;}
  finally{session.SetOptions(options);if(started)session.MarkStarted(play,seconds);else session.MarkNotStarted();if(weather!=null)WeatherSystem.Instance.RestoreState(weather);Time.timeScale=time;Object.Destroy(host);}
 }
}
