using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 조작감·피드백(2026-11-10). 에셋 없이 합성음·떠오르는 숫자·커서만으로 "누르면 반응하는" 느낌을 만든다.
    //  - 효과음: 버튼 클릭, 선택, 이동·공격 지시, 피격, 완공, 적 경보(EnemyAlert.AlarmClip 연결)
    //  - 피해 숫자: 같은 대상이 짧은 시간에 받은 피해를 합쳐 한 번만 띄운다(무리 전투에서 숫자가 쏟아지지 않게)
    //  - 커서: 공격 이동(A) 대기 중 조준 커서
    //  - 단축키: S 선택 전원 정지, Ctrl+A 출전 장수 전원 선택(출전자가 없으면 본거지 장수 전원)
    public sealed class GameFeel : MonoBehaviour
    {
        public enum Sound { Click, Select, Move, Attack, Hit, Complete, Alarm, Deny }

        private static GameFeel instance;
        private AudioSource source;
        private readonly AudioClip[] clips = new AudioClip[8];
        private readonly float[] lastPlayed = new float[8];
        private static readonly float[] MinGap = { .04f, .06f, .08f, .08f, .09f, .3f, 2f, .15f };
        private static readonly float[] Volume = { .35f, .3f, .4f, .45f, .28f, .5f, .6f, .4f };

        private struct Pending { public Transform target; public float amount, first; public bool friendly; }
        private readonly Dictionary<Component, Pending> hits = new Dictionary<Component, Pending>();
        private readonly List<Component> flushed = new List<Component>();
        private const float HitWindow = .3f;
        private const int MaxFlushPerFrame = 6;

        private Texture2D attackCursor;
        private bool cursorShown;
        private AttackMoveController attackMove;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        public static bool Muted; // 검사용: 소리 없이 호출 횟수만 센다.
        public static readonly int[] PlayCount = new int[8];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("GameFeel");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameFeel>();
        }

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.ignoreListenerPause = true;
            for (var i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -99;
            // 비어 있던 경보음 자리에 합성 경보를 넣는다. 이전 Play의 파괴된 클립이 남아 있을 수 있어 Unity 방식(==)으로 비교한다.
            if (EnemyAlert.AlarmClip == null) EnemyAlert.AlarmClip = Clip(Sound.Alarm);
        }

        public static void Play(Sound sound)
        {
            var i = (int)sound;
            PlayCount[i]++;
            if (instance == null || Muted) return;
            var now = Time.unscaledTime;
            if (now - instance.lastPlayed[i] < MinGap[i]) return;
            instance.lastPlayed[i] = now;
            instance.source.PlayOneShot(instance.Clip(sound), Volume[i]);
        }

        // 피해 보고: 유닛·야생 생물의 TakeDamage에서 부른다. friendly = 우리 편이 맞았다.
        public static void Hit(Component target, float amount, bool friendly)
        {
            if (instance == null || target == null || !(amount > 0) || float.IsInfinity(amount)) return;
            if (instance.hits.TryGetValue(target, out var p)) { p.amount += amount; instance.hits[target] = p; }
            else instance.hits[target] = new Pending { target = target.transform, amount = amount, first = Time.unscaledTime, friendly = friendly };
        }

        private void Update()
        {
            FlushHits();
            if (!GameSession.Exists || !GameSession.Instance.GameStarted) { ShowAttackCursor(false); return; }
            UiClickSound();
            if (GameMenuController.BlocksInput) { ShowAttackCursor(false); return; }
            if (attackMove == null) attackMove = SceneObjects.Any<AttackMoveController>();
            ShowAttackCursor(attackMove != null && attackMove.IsAttackMode);
            Hotkeys();
        }

        private void FlushHits()
        {
            if (hits.Count == 0) return;
            var now = Time.unscaledTime; var camera = UnityEngine.Camera.main; var shown = 0; var heard = false;
            flushed.Clear();
            foreach (var pair in hits)
            {
                var p = pair.Value;
                if (now - p.first < HitWindow) continue;
                flushed.Add(pair.Key);
                if (p.target == null || camera == null || shown >= MaxFlushPerFrame) continue;
                var view = camera.WorldToViewportPoint(p.target.position);
                if (view.z <= 0 || view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1) continue; // 화면 밖 전투는 조용히 넘긴다.
                Juice.Float(p.target.position + Vector3.up * 2.2f, "-" + Mathf.Max(1, Mathf.RoundToInt(p.amount)),
                    p.friendly ? new Color(1f, .42f, .36f) : new Color(1f, .93f, .62f));
                shown++; heard = true;
            }
            foreach (var key in flushed) hits.Remove(key);
            if (heard) Play(Sound.Hit);
        }

        // 화면의 버튼·토글을 누르면 클릭음. 개별 버튼에 리스너를 달지 않고 한 곳에서 처리한다.
        private void UiClickSound()
        {
            var mouse = Mouse.current; var events = EventSystem.current;
            if (mouse == null || events == null || !mouse.leftButton.wasPressedThisFrame || !events.IsPointerOverGameObject()) return;
            uiHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = mouse.position.ReadValue() }, uiHits);
            if (uiHits.Count == 0) return;
            var control = uiHits[0].gameObject.GetComponentInParent<Selectable>();
            if (control == null) return;
            Play(control.IsInteractable() ? Sound.Click : Sound.Deny);
        }

        private void Hotkeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var selection = SceneObjects.Any<SelectionManager>();
            if (selection == null) return;
            var ctrl = keyboard.ctrlKey.isPressed;
            if (ctrl && keyboard.aKey.wasPressedThisFrame) { SelectArmy(selection); return; }
            if (!ctrl && keyboard.sKey.wasPressedThisFrame) StopAll(selection);
        }

        // S: 선택한 출전 장수 전원 정지. 한 명일 때는 기존 GameHotkeys가 처리하므로 여럿일 때만 맡는다.
        private static void StopAll(SelectionManager selection)
        {
            var commanders = selection.GetSelectedObjects().Where(s => s != null).Select(s => s.GetComponent<CommanderAnt>())
                .Where(c => c != null && c.IsDeployed && c.CanReceiveOrders).ToList();
            if (selection.GetSelectedObjects().Count < 2 || commanders.Count == 0) return;
            foreach (var c in commanders) c.CommandStop();
            Play(Sound.Move);
        }

        // Ctrl+A: 출전 중인 장수 전원(없으면 본거지에서 명령 가능한 장수 전원)을 선택한다.
        public static int SelectArmy(SelectionManager selection)
        {
            var roster = CommanderRoster.Instance;
            if (roster == null || selection == null) return 0;
            var all = roster.Commanders.Where(c => c != null && c.isActiveAndEnabled && c.IsColonyMember && c.CanReceiveOrders && !c.IsAwayFromHome && !c.IsEmbarked).ToList();
            var army = all.Where(c => c.IsDeployed).ToList();
            if (army.Count == 0) army = all;
            if (army.Count == 0) { ToastManager.Show("선택할 수 있는 장수가 없습니다."); return 0; }
            selection.SelectOnly(army[0].GetComponent<SelectableObject>());
            foreach (var c in army.Skip(1)) selection.Add(c.GetComponent<SelectableObject>());
            return army.Count;
        }

        private void ShowAttackCursor(bool show)
        {
            if (show == cursorShown) return;
            cursorShown = show;
            if (show && attackCursor == null) attackCursor = CrosshairTexture();
            Cursor.SetCursor(show ? attackCursor : null, show ? new Vector2(16, 16) : Vector2.zero, CursorMode.Auto);
        }

        private void OnDestroy() { if (cursorShown) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); if (instance == this) instance = null; }

        private static Texture2D CrosshairTexture()
        {
            const int size = 32; var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var red = new Color(1f, .25f, .2f, 1f); var edge = new Color(.1f, 0, 0, .9f);
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                float dx = x - 15.5f, dy = y - 15.5f; var d = Mathf.Sqrt(dx * dx + dy * dy);
                var ring = Mathf.Abs(d - 10f); var cross = (Mathf.Abs(dx) < 1.2f || Mathf.Abs(dy) < 1.2f) && d > 4f && d < 15f;
                var color = Color.clear;
                if (ring < 1.3f || cross) color = red; else if (ring < 2.4f || (Mathf.Abs(dx) < 2.2f || Mathf.Abs(dy) < 2.2f) && d > 3f && d < 15.8f) color = edge;
                if (d < 1.5f) color = red;
                tex.SetPixel(x, y, color);
            }
            tex.Apply();
            return tex;
        }

        // ── 합성음 ──────────────────────────────────────────────────────────
        private const int Rate = 44100;
        private AudioClip Clip(Sound sound)
        {
            var i = (int)sound;
            if (clips[i] != null) return clips[i];
            float[] data = sound switch
            {
                Sound.Click => Tone(.045f, t => 1250f, 28f),
                Sound.Select => Join(Tone(.05f, t => 700f, 18f), Tone(.07f, t => 940f, 16f)),
                Sound.Move => Tone(.11f, t => 560f - 900f * t, 14f),
                Sound.Attack => Mix(Tone(.13f, t => 330f - 600f * t, 12f, true), Noise(.07f, 30f, .5f)),
                Sound.Hit => Mix(Tone(.09f, t => 150f - 500f * t, 22f), Noise(.06f, 45f, .7f)),
                Sound.Complete => Join(Tone(.09f, t => 523f, 8f), Tone(.09f, t => 659f, 8f), Tone(.2f, t => 784f, 7f)),
                Sound.Alarm => Join(Tone(.16f, t => 740f, 3f, true), Tone(.16f, t => 520f, 3f, true), Tone(.16f, t => 740f, 3f, true), Tone(.16f, t => 520f, 3f, true), Tone(.22f, t => 740f, 5f, true)),
                _ => Tone(.12f, t => 190f, 10f, true),
            };
            var clip = AudioClip.Create("GameFeel " + sound, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clips[i] = clip;
        }

        // 지수 감쇠 톤. square면 살짝 거친 음색(경보·공격·거부).
        private static float[] Tone(float seconds, System.Func<float, float> frequency, float decay, bool square = false)
        {
            var n = Mathf.CeilToInt(seconds * Rate); var data = new float[n]; var phase = 0f;
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)Rate;
                phase += 2 * Mathf.PI * Mathf.Max(40f, frequency(t)) / Rate;
                var wave = Mathf.Sin(phase); if (square) wave = Mathf.Clamp(wave * 3f, -1f, 1f) * .6f;
                var attack = Mathf.Clamp01(i / (Rate * .004f)); var release = Mathf.Clamp01((n - i) / (Rate * .008f));
                data[i] = wave * Mathf.Exp(-decay * t) * attack * release;
            }
            return data;
        }
        private static float[] Noise(float seconds, float decay, float gain)
        {
            var n = Mathf.CeilToInt(seconds * Rate); var data = new float[n]; var random = new System.Random(7); var last = 0f;
            for (var i = 0; i < n; i++)
            {
                last = Mathf.Lerp(last, (float)random.NextDouble() * 2f - 1f, .35f); // 간단한 저역 통과
                data[i] = last * gain * Mathf.Exp(-decay * i / Rate);
            }
            return data;
        }
        private static float[] Join(params float[][] parts) => parts.SelectMany(p => p).ToArray();
        private static float[] Mix(float[] a, float[] b)
        {
            var data = new float[Mathf.Max(a.Length, b.Length)];
            for (var i = 0; i < data.Length; i++) data[i] = Mathf.Clamp((i < a.Length ? a[i] : 0) + (i < b.Length ? b[i] : 0), -1f, 1f);
            return data;
        }
    }
}
