using System;
using UnityEngine;

namespace AntColony.Core
{
    // 일반 개미는 개별 GameObject가 아니라 숫자 풀로만 존재한다.
    // 총합 = Free(대기) + Assigned(지휘관 배속) + Reserved(건설 예약).
    public class AntPool : MonoBehaviour
    {
        public static AntPool Instance { get; private set; }

        // Inert legacy save adapter. Counts are archived, never spendable workforce.
        internal int LegacyFree, LegacyAssigned, LegacyReserved;
        public int Free => 0;
        public int Assigned => 0;
        public int Reserved => 0;
        public int Total => 0;

        public event Action OnPoolChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            
            if (GetComponent<ColonyPopulation>() == null) gameObject.AddComponent<ColonyPopulation>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 저장 복원 전용. 장수 병력은 CommanderAnt.RestoreTroops가 따로 맞추므로 여기서는 숫자만 덮어쓴다.
        internal void RestoreCounts(int free, int assigned, int reserved)
        { LegacyFree = Mathf.Max(0, free); LegacyAssigned = Mathf.Max(0, assigned); LegacyReserved = Mathf.Max(0, reserved); OnPoolChanged?.Invoke(); }

        public void Breed(int count)
        {  }

        public bool TryAssign(int count)
        { return false; }

        public void ReturnAssigned(int count)
        {  }

        // 전투 손실: 풀 총합에서 그대로 사라진다(환급·회복 없음).
        public void LoseAssigned(int count)
        {  }

        public bool TryReserve(int count)
        { return count <= 0; }

        public void ReleaseReserved(int count)
        {  }

        // Phase 4: 노화·탈주·식량 부족으로 대기 개미를 뺀다. lost면 손실로 기록. 실제로 뺀 수를 돌려준다.
        public int RemoveFree(int count, bool lost)
        { return 0; }

        // 식량 부족으로 개미 한 마리를 잃는다. 대기 개미를 먼저 소모한다.
        public bool StarveOne()
        { return false; }
    }
}
