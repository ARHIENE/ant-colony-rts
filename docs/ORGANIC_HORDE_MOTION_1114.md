# 무리 동작·접지 보정 중단 상태 — 2026-11-14

사용자가 두 세션의 작업 중단과 SAVE를 요청해 현재 상태를 보존했다. 기존 17종·86클립의 대표 개체 전투 연결과 이번 무리 전체 동작 작업은 완료 범위가 다르다.

## 준비한 변경

- 무리 추가 몸체에 대기·이동·공격·피격·사망을 표시하는 OrganicMotionBank/OrganicHordeMotion과 셰이더 초안을 작성했다. 부분 손실 시 사망 표시도 초안에 포함한다.
- Assets에는 초안 코드가 있지만 HordeMotion 데이터는 아직 생성하지 않았다. 현재 추가 몸체는 기존 정적 GPU 표시 경로를 사용한다. 전종 무리 애니메이션이 적용됐다는 뜻이 아니다.
- 보행형 15종 Move와 탑승형 HuntRider/SiegeRider의 나머지 동작 8개, 총 23클립 보정 후보를 만들었다. 원본 art/Assets FBX에는 적용하지 않았다.
- 발 지지 구간의 속도와 반복 경계를 보정하고, 탑승부 상위 뼈에 흔들림·반동·피격·사망 반응을 더했다. 별도의 탑승자 인체 리그나 손 파지는 구현하지 않았다.

## 확인한 범위

17종 오프라인 접지·반복 감사와 HuntRider/SiegeRider/ClawHunter의 프레임 이미지 검수를 마쳤다. 추가 보폭 보정 후 AcidSpitter 이동 최대 관절 변화는 약 24.6도에서 10.7도, WallRam은 30.2도에서 15.3도로 줄었다. 실제 Unity 지형과 이동 속도에서의 발 미끄러짐은 아직 검사하지 않았다.

최종 코드 후보의 독립 컴파일은 오류 0이다. 해당 후보는 `work/organic-horde-motion-1114/pending-runtime`에만 있다. `apply_refined.py`는 실행하지 않았다. 데이터 베이커·17종 검사·실재생 검사도 작성했으나 실행 전이다.

## 재개 지점

`work/organic-horde-motion-1114/PENDING.md`와 최하단 업데이트를 먼저 읽는다. 다른 채팅의 Unity 사용 여부를 확인하고 원본 SHA를 대조한 뒤 후보를 적용한다. 17종 motion bank 생성, 실제 셰이더 재생·부분 사망·전체 사망·모델 방향·발 접촉 검사를 마쳐야 한다. 검증 중 스킨 배율과 지면 보정 계산을 실제 BakeMesh 결과와 비교한다. 성능 최적화는 사용자 요청으로 보류한다.

## 환경 문제

같은 Git 변경 조회를 수행하던 부모 없는 프로세스가 반복 누적돼 메모리가 부족해졌다. 정확히 일치한 읽기 전용 조회만 종료했으며 파일·Git 이력·Unity를 삭제하거나 강제 종료하지 않았다. 로컬 설정 `core.bigFileThreshold=16m`, `diff.renameLimit=100`을 적용했으나 재발을 막지는 못했다. 두 값의 이전 상태는 unset이며 필요시 `git config --local --unset core.bigFileThreshold`와 `git config --local --unset diff.renameLimit`으로 되돌릴 수 있다.
