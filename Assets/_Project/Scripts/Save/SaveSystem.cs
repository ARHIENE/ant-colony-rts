using System;
using System.IO;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Save
{
    public sealed class SaveSystem : MonoBehaviour
    {
        public static float PlaySeconds => GameSession.Instance.PlaySeconds;
        public static bool Busy { get; internal set; }
        internal static SaveFileV1 Rollback;
        private float elapsed, retry;
        private void LateUpdate()
        {
            if (!GameSession.Instance.GameStarted || Busy) return;
            elapsed += Time.deltaTime; retry -= Time.unscaledDeltaTime;
            if (!UserSettings.Current.autoSaveEnabled || elapsed < UserSettings.Current.autoSaveMinutes * 60 || retry > 0) return;
            if (TrySave(true, 0, out var error)) { elapsed = 0; ToastManager.Show("Autosaved."); }
            else { retry = 30; ToastManager.Show("Autosave deferred: " + error); }
        }
        public static bool TrySave(bool auto, int index, out string error)
        {
            error = null;
            try
            {
                if (Busy || !GameSession.Instance.GameStarted) throw new InvalidOperationException("No playable session to save.");
                if (index < 0 || index >= (auto ? SaveSlots.AutoSlotCount : SaveSlots.ManualSlotCount)) throw new ArgumentOutOfRangeException(nameof(index));
                // Capture and atomic write do not yield. Autosaves run after Update; menu saves run while paused.
                var file = SaveSnapshot.Capture();
                if (!SavePreflight.ForScene(file, out error)) return false;
                SaveStorage.WriteAtomic(SaveSlots.PathFor(auto, index), SaveOptionalStates.ToJson(file, true));
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
        public static bool TryLoad(string path, out string error)
        {
            error = null;
            if (Busy) { error = "A game is already loading."; return false; }
            if (!SaveStorage.TryReadText(path, out var text, out error) || !SaveValidator.TryParse(text, out var file, out error)
                || !SavePreflight.ForScene(file, out error)) return false;
            try
            {
                Rollback = GameSession.Instance.GameStarted ? SaveSnapshot.Capture() : null;
                if (Rollback != null) SaveStorage.WriteAtomic(System.IO.Path.Combine(SaveStorage.Root, "load-recovery.json"), SaveOptionalStates.ToJson(Rollback));
                Reload(file, new NewGameOptions { mapSize = (MapSize)file.options.mapSize, difficulty = (DifficultyLevel)file.options.difficulty, commanderDeath = (CommanderDeathMode)file.options.commanderDeath, seed = file.options.seed, biome = (MapBiome)file.options.biome, climate = (MapClimate)file.options.climate });
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
        public static void NewGame(NewGameOptions options)
        {
            if (Busy) return;
            CrashLandingStart.Prepare(options);
            Rollback = null; Units.CommanderAnt.CommonDiet = Units.CommanderAnt.AllDiet; World.Memorial.Restore(null); Reload(null, options);
        }
        internal static void Reload(SaveFileV1 file, NewGameOptions options)
        {
            if (!Application.CanStreamedLevelBeLoaded("AntColony")) throw new InvalidOperationException("AntColony is missing from Build Settings.");
            var session = GameSession.Instance;
            var previousOptions = session.Options.Clone(); var previousLoad = session.PendingLoad;
            var playing = session.GameStarted; var playSeconds = session.PlaySeconds; var gameSeconds = session.GameSeconds;
            var pause = SimulationPause.Capture(); var scale = Time.timeScale;
            Busy = true;
            try
            {
                Time.timeScale = 0;
                session.SetOptions(options); session.MarkStarted(); session.PendingLoad = file;
                GameMenuController.Instance.ShowLoading();
                SceneManager.LoadScene("AntColony");
            }
            catch
            {
                // A synchronous loading exception must not leave every subsequent check waiting on Busy.
                Busy = false; session.SetOptions(previousOptions); session.PendingLoad = previousLoad;
                if (playing) session.MarkStarted(playSeconds, gameSeconds); else session.MarkNotStarted();
                SimulationPause.Restore(pause); Time.timeScale = scale;
                throw;
            }
        }
    }
}
