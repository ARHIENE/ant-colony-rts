using System;
using System.Collections;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            Time.timeScale = 0; SaveSystem.Busy = true;
            var root = GameSession.Instance.gameObject;
            root.AddComponent<ToastManager>(); root.AddComponent<SaveSystem>();
            root.AddComponent<GameMenuController>(); root.AddComponent<GameBootstrap>();
        }
        private void Awake() => SceneManager.sceneLoaded += Loaded;
        private void OnDestroy() { SceneManager.sceneLoaded -= Loaded; Time.timeScale = 1; }
        private void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "AntColony") return;
            UnityEngine.Random.InitState(GameSession.Instance.Options.seed);
            if (GameSession.Instance.GameStarted)
            {
                var terrain = new GameObject("HomeMapBuilder").AddComponent<HomeMapBuilder>();
                terrain.ApplyFromSession();
            }
            StartCoroutine(FinishLoad());
        }
        private IEnumerator FinishLoad()
        {
            yield return null; yield return null; // WorldMapManager.Start と 생성된 거점 Start 완료.
            new GameObject("EquipmentInventory").AddComponent<AntColony.Units.EquipmentInventory>();
            new GameObject("CampaignResearch").AddComponent<CampaignResearch>();
            new GameObject("CampaignHistory").AddComponent<CampaignHistory>();
            new GameObject("ColonyEvents").AddComponent<AntColony.World.ColonyEvents>();
            new GameObject("SkillTargeting").AddComponent<SkillTargeting>();
            SaveCatalog.Initialize();
            var file = GameSession.Instance.PendingLoad as SaveFileV1;
            GameSession.Instance.PendingLoad = null;
            string error = null;
            if (file != null)
            {
                var routine = SaveSnapshot.Restore(file);
                while (true)
                {
                    bool more;
                    try { more = routine.MoveNext(); }
                    catch (Exception e) { error = e.Message; break; }
                    if (!more) break;
                    yield return routine.Current;
                }
            }
            if (error != null)
            {
                var rollback = SaveSystem.Rollback; SaveSystem.Rollback = null;
                ToastManager.Show("Load failed: " + error + (rollback != null ? " Restoring previous game." : " Recovery file preserved."));
                if (rollback != null)
                {
                    SaveSystem.Reload(rollback, new NewGameOptions { mapSize = (MapSize)rollback.options.mapSize,
                        difficulty = (DifficultyLevel)rollback.options.difficulty, commanderDeath = (CommanderDeathMode)rollback.options.commanderDeath, seed = rollback.options.seed });
                    yield break;
                }
                GameSession.Instance.MarkNotStarted();
            }
            SaveSystem.Rollback = null; SaveSystem.Busy = false;
            var camera = FindFirstObjectByType<AntColony.Camera.IsometricCameraController>();
            camera?.ApplySettings(UserSettings.Current.cameraPanSpeed, UserSettings.Current.edgeScrollThickness);
            new GameObject("GameNotifications").AddComponent<GameNotifications>();
            GameMenuController.Instance.SceneReady(GameSession.Instance.GameStarted);
            new GameObject("BetaProgress").AddComponent<BetaProgress>();
        }
    }
}
