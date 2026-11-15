# 생물형 적 동작 검수 — 2026-11-14

17종 86클립을 검사했고, 관절 튐·과한 늘어남이 드러난 10종의 51클립을 다시 출력해 Unity에 반영했다. 원본 Meshy GLB와 보정 전 편집본은 보존했다. 후속 작업으로 게임 스폰·전투 AI를 연결했다([전투 연결 기록](ORGANIC_COMBAT_1114.md)). 이 문서는 동작 자체의 검수 범위를 기록한다.

## 수정 내용

- BloomMender: 공격 시 전방 돌진 대신 제자리 수축·팽창을 사용해 다리 IK가 급히 접히던 문제를 줄였다.
- WallRam, AcidSpitter: 보폭·발 높이와 몸통 이동량을 줄여 이동 끝부분의 관절 튐을 완화했다.
- BreachBeast, Bulwark, CarrionReclaimer: 공격 준비·타격 곡선을 부드럽게 하고 과도한 팔다리 회전을 줄였다. Bulwark와 CarrionReclaimer 공격은 추가로 진폭을 65%로 낮추고 지면 높이를 다시 맞췄다.
- BroodAlien, SiegeBrooder, VeilWeaver를 포함한 보정 10종: 피격 시작을 완만하게 하고 구워진 회전 키를 평활화했다. 반복 동작은 경계까지 이어지게 처리했다.
- WallRam, NeedleStalker, BloomMender의 사망도 실제 변형 메시를 기준으로 낮은 자세를 선택했다. 사망 시 다리 접힘을 줄이고 전체 프레임의 지면 높이를 보정했다.

## 검증 결과

| 검사 | 결과 | 범위 |
| --- | --- | --- |
| 보정 FBX 프레임 검사 | 51/51 통과 | 유한 좌표, 지면 아래 0.005 초과 없음, 반복 경계 오차 0.003 미만, 실제 변형 존재 |
| Unity 스킨 검사 | 86/86 통과 | 클립별 4개 시간 샘플, 메시 변형·텍스처·반복 설정 |
| Unity Animator 전환 | 436/436 통과 | 17종의 모든 상태 쌍, 60fps로 전환 진행 확인 |
| 중복 동작 소유 | 통과 | Animator 1개, 레이어 1개, 전환 중 가중치 있는 클립 최대 2개 |

전환 중 클립 2개는 정상적인 크로스페이드다. 여러 Animator나 레이어가 같은 모델을 동시에 구동하는 현상은 검사 장면에서 발견하지 못했다. 전환 검사는 독립 PreviewScene에서 실행했으며 실제 게임 AI의 명령 중복까지 검증한 것은 아니다.

30fps 프레임별 최대 관절 변화는 BloomMender 공격 39.7°→4.9°, BroodAlien 피격 24.0°→6.4°, WallRam 이동 30.4°→11.9°로 감소했다. 이 수치는 부드러움의 보조 지표이며 모든 관통이나 자연스러움을 보장하지 않는다.

17종의 동작별 연속 포즈를 육안 확인했다. 치유체는 3초 재생용 프레임도 추가 확인했다. 탑승체의 탑승자와 생물은 한 메시여서 독립적인 탑승자 반응이 부족하고, 일부 원거리·지원형은 효과가 연결되지 않은 상태에서 공격 동작이 작게 보인다.

## 남은 검수

실제 경로 이동 속도에 맞춘 발 미끄러짐, 벽·다른 적과의 접촉, 모든 프레임의 자체 관통, 공격 판정과 타격 시점 일치, 다중 팔 및 탑승자의 독립 동작은 후속이다. 전체 동작의 최종 품질 완료로 판정하지 않는다.

## 근거 파일

- [Unity 86클립 검사](../art/enemy-concepts-1114/unity-skin-response-after.txt)
- [Unity 436전환 검사](../art/enemy-concepts-1114/unity-transition-response-after.txt)
- [관절·늘어남 전후 비교](../art/enemy-concepts-1114/motion-polish-comparison.json)
- [원본 출력과 Unity 파일 SHA-256](../art/enemy-concepts-1114/motion-polish-hashes.json)
- 작업 프레임·검수 스크립트: `work/organic-combat-1114/`
- 보정 전 백업: `work/organic-combat-1114/before-polish/`

기존 `motion-strip.png`는 보정 전 미리보기다. 최신 연속 포즈는 `work/organic-combat-1114/polished-audit/<이름>/sequence.png`에 있으며, Bulwark·CarrionReclaimer의 해당 그림은 마지막 공격 진폭 보정 전이다. 최종 판정에는 FBX와 Unity 재검사 결과를 사용했다.
