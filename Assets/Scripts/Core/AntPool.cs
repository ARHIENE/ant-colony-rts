using System;
using UnityEngine;

namespace AntColony.Core
{
    // 일반 개미는 개별 GameObject가 아니라 숫자 풀로만 존재한다.
    // 총합 = Free(대기) + Assigned(지휘관 배속) + Reserved(건설 예약).
    public class AntPool : MonoBehaviour
    {
        public static AntPool Instance { get; private set; }

        // ponytail: 초기 대기 개미 수는 잠정값이다. 밸런스 확정 시 조정한다.
        [SerializeField, Min(0)] private int startingAnts = 40;

        public int Free { get; private set; }
        public int Assigned { get; private set; }
        public int Reserved { get; private set; }
        public int Total => Free + Assigned + Reserved;

        public event Action OnPoolChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Free = startingAnts;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 저장 복원 전용. 장수 병력은 CommanderAnt.RestoreTroops가 따로 맞추므로 여기서는 숫자만 덮어쓴다.
        internal void RestoreCounts(int free, int assigned, int reserved)
        {
            Free = Mathf.Max(0, free);
            Assigned = Mathf.Max(0, assigned);
            Reserved = Mathf.Max(0, reserved);
            OnPoolChanged?.Invoke();
        }

        public void Breed(int count)
        {
            if (count <= 0) return;
            Free += count;
            OnPoolChanged?.Invoke();
        }

        public bool TryAssign(int count)
        {
            if (count <= 0 || Free < count) return false;
            Free -= count;
            Assigned += count;
            OnPoolChanged?.Invoke();
            return true;
        }

        public void ReturnAssigned(int count)
        {
            count = Mathf.Clamp(count, 0, Assigned);
            if (count == 0) return;
            Assigned -= count;
            Free += count;
            OnPoolChanged?.Invoke();
        }

        // 전투 손실: 풀 총합에서 그대로 사라진다(환급·회복 없음).
        public void LoseAssigned(int count)
        {
            count = Mathf.Clamp(count, 0, Assigned);
            if (count == 0) return;
            Assigned -= count;
            OnPoolChanged?.Invoke();
        }

        public bool TryReserve(int count)
        {
            if (count <= 0) return true;
            if (Free < count) return false;
            Free -= count;
            Reserved += count;
            OnPoolChanged?.Invoke();
            return true;
        }

        public void ReleaseReserved(int count)
        {
            count = Mathf.Clamp(count, 0, Reserved);
            if (count == 0) return;
            Reserved -= count;
            Free += count;
            OnPoolChanged?.Invoke();
        }

        // 식량 부족으로 개미 한 마리를 잃는다. 대기 개미를 먼저 소모한다.
        public bool StarveOne()
        {
            if (Free > 0)
            {
                Free--;
                OnPoolChanged?.Invoke();
                return true;
            }
            return false;
        }
    }
}
