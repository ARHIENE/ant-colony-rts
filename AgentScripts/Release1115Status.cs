using UnityEngine;
using UnityEditor;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using System.Linq;
public static class Release1115Status
{
    public static string Main()
    {
        bool paused=EditorApplication.isPaused; EditorApplication.isPaused=false;
        return $"playing={Application.isPlaying} editorPaused={paused} busy={SaveSystem.Busy} started={GameSession.Instance?.GameStarted} roster={CommanderRoster.Instance?.Commanders.Count} frame={Time.frameCount} scale={Time.timeScale} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}" + string.Join(";",CommanderRoster.Instance.Commanders.Select(c=>$"{c.CommanderName}: hp={c.PersonalHealth} away={RocketCampaign.AwayOnMothership(c)} pos={c.Position} order={c.OrderRefusalReason} down={c.Health.Downed}"));
    }
    public static string Refresh() { AssetDatabase.Refresh(); return "Refreshed"; }
}
