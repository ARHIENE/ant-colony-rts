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
            ToastManager.ClearAll(); // 이전 판(패배 등) 알림이 새 게임·불러오기에 남지 않게(2026-11-01)
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
            new GameObject("CritterSpawner").AddComponent<AntColony.World.CritterSpawner>();
            new GameObject("Heating").AddComponent<AntColony.Buildings.Heating>();
            new GameObject("SkillTargeting").AddComponent<SkillTargeting>();
            new GameObject("GatherDesignation").AddComponent<GatherDesignation>();
            new GameObject("EnemyAlert").AddComponent<EnemyAlert>();
            new GameObject("CommanderOverhead").AddComponent<CommanderOverhead>();
            new GameObject("FirstHints").AddComponent<FirstHints>();
            new GameObject("MoodWatch").AddComponent<MoodWatch>();
            new GameObject("UndergroundSurvey").AddComponent<AntColony.World.UndergroundSurvey>().Initialize();
            SaveCatalog.Initialize();
            var file = GameSession.Instance.PendingLoad as SaveFileV1;
            GameSession.Instance.PendingLoad = null;
            if (file == null && GameSession.Instance.GameStarted)
            {
                CrashLandingStart.Apply();
                yield return null; // 삭제 예약된 시작 시설이 사라진 뒤에만 Busy를 해제한다.
            }
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
                ToastManager.Show("불러오기 실패: " + error + (rollback != null ? " 이전 게임을 복원합니다." : " 복구용 저장 파일은 보존했습니다."));
                if (rollback != null)
                {
                    SaveSystem.Reload(rollback, new NewGameOptions { mapSize = (MapSize)rollback.options.mapSize,
                        difficulty = (DifficultyLevel)rollback.options.difficulty, commanderDeath = (CommanderDeathMode)rollback.options.commanderDeath, seed = rollback.options.seed, biome = (MapBiome)rollback.options.biome, climate = (MapClimate)rollback.options.climate });
                    yield break;
                }
                GameSession.Instance.MarkNotStarted();
            }
            SaveSystem.Rollback = null; SaveSystem.Busy = false;
            foreach (var c in CommanderRoster.Instance.Commanders)
                if (c.GetComponent<AntColony.Units.StartingAppearance>() == null) c.gameObject.AddComponent<AntColony.Units.StartingAppearance>();
            var camera = AntColony.Core.SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
            camera?.ApplySettings(UserSettings.Current.cameraPanSpeed, UserSettings.Current.edgeScrollThickness);
            new GameObject("GameNotifications").AddComponent<GameNotifications>();
            GameMenuController.Instance.SceneReady(GameSession.Instance.GameStarted);
            new GameObject("BetaProgress").AddComponent<BetaProgress>();
        }
    }
}
