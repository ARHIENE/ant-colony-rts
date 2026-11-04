using System;
using UnityEngine;

namespace AntColony.Boss
{
    // 목록 인덱스는 도보 원정대 해산 시 바뀐다. 저장되는 ID로 보상 소유자를 고정한다.
    public sealed class BossCargoCarrier : MonoBehaviour
    {
        public string Id = "transport:" + Guid.NewGuid().ToString("N");
    }
}
