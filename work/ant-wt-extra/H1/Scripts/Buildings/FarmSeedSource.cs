using System.Globalization;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class FarmSeedSource : MonoBehaviour
    {
        [SerializeField] private FarmCrop crop;
        [SerializeField] private string fixedKey;
        public FarmCrop Crop => crop;
        public string FixedKey => fixedKey;
        // 생성 담당이 준 고정 키를 우선 쓴다(위치가 바뀌어도 같은 개체). 없으면 옛 위치 키(구저장 호환).
        public string Key => !string.IsNullOrEmpty(fixedKey) ? fixedKey : crop + ":" + transform.position.ToString("R", CultureInfo.InvariantCulture);
        public void Configure(FarmCrop value, string key = null) { crop = value; if (!string.IsNullOrEmpty(key)) fixedKey = key; }
    }
}
