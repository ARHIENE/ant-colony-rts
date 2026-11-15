# 생물형 적 제작 상태 — 2026-11-14

승인 범위: 생물형 17종, 무인기 제외. 종마다 Idle/Move/Attack/Hit/Death, BloomMender는 Heal 추가. 후속 작업으로 게임 스폰·AI·회복 효과와 저장 복원을 연결했다. [전투 구현·검증](../../docs/ORGANIC_COMBAT_1114.md) 참조.

## 완료된 원본과 동작

WallRam, NeedleStalker, BloomMender: Meshy 7.1 Ultra 2K 생성 → 30K 리메시 → Smart Rig GLB 다운로드 완료. MESHY/<이름>_1114_SmartRig.glb 보존.
Smart Rig는 Meshy 애니메이션 라이브러리를 지원하지 않아 Blender에서 실제 스켈레탈 클립 16개 제작. FBX 재임포트 지면·루프·변형 검사 및 Unity 샘플링16개 통과(unity-motion-checks.txt). Assets/Resources/OrganicEnemies1114에 Generic FBX/URP 머티리얼/AnimatorController 임포트. 아직 게임 플레이 적에 연결하지 않은 프로토타입.

## 추가 14종 완료

입력 이미지 meshy-inputs에서 검수. Bulwark는 분리된 다리 오류를 연결된 형태로 수정한 뒤 업로드.

- 첫 배치10종: ClawHunter, BroodAlien, BreachBeast, AcidSpitter, HuntRider, SiegeRider, WallMelter, SiegeBrooder, BroodCarrier, HookMaw. 형상 생성 완료.
- 둘째 배치4종: Bulwark, BurrowLurker, CarrionReclaimer, VeilWeaver. 형상 생성 완료.
- 14종 모두 2K 텍스처, 30K 리메시, Smart Rig 완료. `MESHY/<이름>_1114_SmartRig.glb` 14개 다운로드 및 Blender 열기 확인. 잔액499.
- 종별 뼈대 추출 `*-rig.json`, 관절 선택 `remaining_motion_profiles.py`. Blender 5종 동작 70개 제작·FBX 재검사·Unity 임포트 완료. 14종 사망 자세를 체형에 맞게 보정하고 다시 검사했다. 기존16개 포함 총86클립 Unity 스킨 변형·루프 설정 검사 통과. 종별 `export-checks.json`, `unity-import.txt`, `final-batch-results.json`, `unity-death-refresh.txt`, `unity-motion-checks.txt`가 확인 근거다.

배치 생성은 형상만 생성하므로 색상 텍스처는 별도 진행한다. 기존 생성 모델을 다시 만들지 않는다.

## 크레딧 기록

첫3종 1094→989(각35). 추가14종 형상 생성 350 사용, 989→639. 텍스처 14종×10=140 사용 후499. 이번 추가14종 합계490. 리메시·Smart Rig 표시비용0. 구매/충전 없음.

## 검증 기준

Idle/Move 루프 연결, Attack 준비·타격·회복, Hit 짧은 반응, Death 바닥 붕괴 후 마지막프레임 유지. 모델별 다리/무기기관 뼈를 확인해서 키를 작성한다. 전체 오브젝트 이동만으로 동작 완료 처리하지 않는다.

## 파일과 사용법

- 원본: `MESHY/<이름>_1114_SmartRig.glb`. 17개 파일 크기와 SHA-256은 [다운로드 목록](download-manifest.json)에 기록했다.
- 편집본: 이 폴더 아래 `<이름>/<이름>_5Motions.blend`. 액션과 개별 FBX, 원본 PBR 텍스처를 함께 보존한다.
- Unity: `Assets/Resources/OrganicEnemies1114/<이름>/`. Generic FBX, URP Lit 머티리얼, AnimatorController를 포함한다.
- Animator의 정수 `Action`: 0 대기, 1 이동, 2 공격, 3 피격, 4 사망. BloomMender만 5 회복을 추가한다. 대기·이동만 루프이며, 공격·피격 후 복귀 명령은 게임 코드에서 보내야 한다.
- 추가14종의 `motion-strip.png`는 다섯 동작을 샘플링한 미리보기다. 실제 애니메이션 데이터는 FBX와 Blender 액션에 있다.

## 남은 제작 범위

현재 동작은 관절 키와 발 IK를 사용한 프로토타입이다. 전투 속도에 맞춘 보폭·발 미끄러짐, 타격 타이밍, 세밀한 접촉·관통은 게임 화면에서 추가 조정해야 한다. 자동 리깅은 다중 팔다리를 모두 독립적으로 표현하지 못한 부분이 있으며, BroodAlien의 추가 팔과 탑승자의 독립 동작은 후속 수작업 대상이다. 탑승체는 탑승자와 생물이 합쳐진 모델이다.

스폰·웨이브·공격 판정·투사체·벽 부식·운반/회수·장막·보호·굴착·회복은 후속 전투 작업에서 연결했다. 효과 외형은 프로토타입이고 장기 밸런스는 미검증이다. 원본 Metal/Rough 텍스처는 보존했지만 Unity 채널 재패킹은 아직 하지 않았고, 현재 URP 머티리얼에는 BaseColor/Normal과 고정 Smoothness를 사용한다. 런타임 메모리 최적화 및 별도 LOD도 후속이다.


Unity 적용 중 일부 호출은 30초/120초 응답 제한을 넘었으나 완료 기록을 확인했다. 플레이 모드를 종료한 뒤 나머지 임포트는 성공했고, 최종86클립 샘플링 검사는 오류 없이 완료했다. 에디터는 정지 상태로 두었다.

최종 파일 대조: 17종 FBX 86개 모두 Blender 출력본과 Unity 임포트 파일의 SHA-256 일치(불일치0). 코드 변경 후 graphify update 완료(exit0).

## 추가 동작 검수와 보정

2026-11-14 추가 검수에서 다리 IK 튐·과한 공격/피격 회전·사망 변형을 발견해 10종 51클립을 보정했다. 보정본 Unity 반영 후 전체86클립 스킨 검사와 436가지 상태 전환 검사가 통과했다. 단일 Animator/레이어에서 정상 크로스페이드 외 동작 중복은 발견하지 못했다. 실제 이동 중 발 접촉과 세밀한 관통, 탑승자의 독립 동작은 남아 있다. [보정 내용과 검증 한계](../../docs/ORGANIC_MOTION_AUDIT_1114.md)를 참고한다. 기존 motion-strip.png는 보정 전 미리보기다.
