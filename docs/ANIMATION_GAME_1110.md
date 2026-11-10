# 실제 행동 애니메이션 연결 — 2026-11-10

## 적용 범위

수집한 119종 중 현재 게임 행동에 맞는 24개 원본 클립을 공통 주민 리그(몸 20본·손가락 30본)에 변환하고 Animator 27개 상태에 배치했다. 원본 FBX는 `_Project/Art/MeshyAnimation1110`, 변환 결과는 `Resources/Kemono/MeshyMotions`에 있다. 메시는 교체하지 않으므로 기존 헤어·의상·얼굴·색상 변형과 표정을 유지한다.

| 게임 상태 | 연결 모션 |
|---|---|
| 채굴 / 망치 건설·수리·제작 | Heavy_Hammer_Swing |
| 벌목 | Charged_Axe_Chop |
| 식물 채집 | Pull_Radish (맨손) |
| 바닥 작업 | Collect_Object |
| 자원 운반 / 사람 운반 | Carry_Heavy_Object_Walk |
| 보행 / 달리기 / 저체력 이동 | Walking / Running / Injured_Walk |
| 수면 / 쓰러짐 | Sleep_Normally / Prone_Reach_Help |
| 휴식 / 산책 여가 / 관찰 | Sit_Cross_Legged_on_Floor / Long_Breathe_and_Look_Around / Female_Bend_Over_Pick_Up_Inspect |
| 피격 / 사망 | Hit_Reaction / Dead |
| 근접 / 방패 / 활 공격 | Right_Hand_Sword_Slash / Shield_Push_Left / Archery_Shot_2 |
| 대기 3종 / 모닥불 이야기 | Idle_3~5 / Talk_with_Hands_Open / Listening_Gesture |

Victory_Fist_Pump는 Celebrate 상태에 준비만 했으며 실제 승리 이벤트 연결은 아직 없다. 수영·사다리 등 대응 게임 기능이 없는 나머지 후보는 보관 상태다. 기존 굴착·경작 등 나머지 상태는 기존 클립을 유지한다. 근접/활 공격 재생 속도는 실제 공격 연출 시간에 맞춘다. 피해·채취 타이머 자체는 변경하지 않았다.

## 파지와 접근 수정

- 도구 FBX의 원래 축 변환을 보존한다.
- 작업 모션의 몸통/하체는 클립을 사용하고 양팔은 같은 도구 자루의 두 접점을 추적한다. 큰 머리와 짧은 팔에 맞춰 전방 작업 범위를 제한한다.
- 손가락 관절 굽힘과 엄지 접점 보정, 손가락 마디를 가로지르는 자루 방향을 적용했다. 장비 변경 시 소켓 회전을 복원한다.
- 바위 중심으로 들어가 채굴하던 접근점을 자원 외곽의 도달 가능한 지점으로 옮겼다. 건물·바닥 화물 접근은 기존 규칙을 유지한다.

## 검증과 한계

공식 Unity CLI 컴파일 오류 0. 60개 채굴 자세의 보조 손 소켓 오차 최대 0.00000047m, 주 손과 머리 본 최소 거리 0.207m. 이는 소켓·본 위치 검사이며 모든 손가락 표면의 무관통을 증명하지 않는다.

실제 게임 검증은 자동 저장을 끈 임시 새 게임에서 진행했다. `CommandGather` 호출 후 실제 작업 상태, Animator 시간 변화와 손 본 이동량을 기록했다. 단순히 미리보기 클립을 강제 재생한 검증과 구분한다. 기록: `work/animation-1110/game-audit.txt`. 검사 도구: `AgentScripts/Animation1110Game.cs`, `Animation1110GripAudit.cs`.

걷기·달리기·수확·운반·수면·활·검·채굴은 각 6시점 렌더를 확인했다. 모든 외형 조합·모든 무기·모든 시설에서의 접촉 품질을 전수 검증한 것은 아니다. 손가락·의상 등의 미세 교차와 도구별 타격 지점 정밀 조정은 별도 검수 범위다.

채굴을 오래 지속하면 발밑에 생성되는 Dropped Stone 모델이 상체를 가리는 기존 별도 문제를 확인했다. 모션 검증 사진은 첫 작업 주기의 실제 Game 화면이다. 환경 반입 중 나타난 Tree_02 missing prefab 경고는 애니메이션 컴파일 오류와 별개이며 이 작업에서 해결했다고 주장하지 않는다.

SAVE·커밋·사용자 저장 슬롯 덮어쓰기는 하지 않았다.

통합 SAVE 단계에서 모션24종 GIF4개와 실제 채굴2프레임GIF를 개발 일지에 첨부했다. SAVE 미실행 문장은 구현 검증 종료 당시 기록이다. 일지 https://app.notion.com/p/414c4a0ecd3181e8b625dff8183a2df1 .
