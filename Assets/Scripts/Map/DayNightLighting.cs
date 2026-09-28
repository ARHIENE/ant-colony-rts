using AntColony.Core;
using UnityEngine;

namespace AntColony.Map
{
    // 밤 화면: 해(방향광)와 환경광을 푸른빛으로 어둡게 한다. 낮↔밤은 20초에 걸쳐 바뀐다.
    // 반딧불 램프·모닥불 같은 따뜻한 빛은 그 시설이 자기 조명을 켠다.
    public sealed class DayNightLighting : MonoBehaviour
    {
        public const float Blend = 20f;
        private static readonly Color NightTint = new Color(.35f, .45f, .8f);
        private Light sun;
        private Color dayColor;
        private float dayIntensity, dayAmbient;

        // 0 = 한낮, 1 = 한밤.
        public static float NightAmount
        {
            get
            {
                var t = GameCalendar.TimeOfDay;
                if (t < Blend * .5f) t += GameCalendar.SecondsPerDay; // 새벽 전환은 전날 끝에 이어서 계산한다.
                var toNight = Mathf.Clamp01((t - GameCalendar.DaySeconds + Blend * .5f) / Blend);
                var toDay = Mathf.Clamp01((t - GameCalendar.SecondsPerDay + Blend * .5f) / Blend);
                return Mathf.Clamp01(toNight - toDay);
            }
        }

        private void Start()
        {
            sun = RenderSettings.sun;
            if (sun == null)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun != null) { dayColor = sun.color; dayIntensity = sun.intensity; }
            dayAmbient = RenderSettings.ambientIntensity;
        }

        private void LateUpdate()
        {
            var n = NightAmount;
            if (sun != null) { sun.color = Color.Lerp(dayColor, dayColor * NightTint, n); sun.intensity = Mathf.Lerp(dayIntensity, dayIntensity * .25f, n); }
            RenderSettings.ambientIntensity = Mathf.Lerp(dayAmbient, dayAmbient * .4f, n);
        }

        private void OnDestroy()
        {
            if (sun != null) { sun.color = dayColor; sun.intensity = dayIntensity; }
            RenderSettings.ambientIntensity = dayAmbient;
        }
    }
}
