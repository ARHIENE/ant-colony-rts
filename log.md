# 프로젝트 로그

## 현재 상태 — 2026-11-12 통합 SAVE 진행
- 사용자 지시: 두 채팅 작업 통합 저장, 실제 캐릭터 모션·장비 파지 GIF와 에셋 사진 Notion 등록/검증 후 컴퓨터 종료. 아직 원격 저장·Notion·종료 완료 아님.
- Meshy 원본 뼈대/관절 곡선 유지: 모든 colony commander에 native player 적용. 119 수집클립 원본/실제 외형 5시점 검수 및 Play 재생 검사. Runtime35클립/51행동 API 연결 검사 통과. 이는119개 자연 발생 게임 행동을 뜻하지 않음.
- 실제 이동·무 수확·야간 수면, 채굴/벌목/점토채굴 검증. 채굴/벌목/점토 GIF는 실제 행동, preview 없음, 0.25배 시뮬레이션 촬영. 무기10종 GIF는 일시정지 시뮬레이션의 원본 모션 미리보기이며 실제 전투 녹화 아님.
- 승인된 곡괭이·도끼·망치 머리/자루 방향 유지. 삽/괭이 양손 자루, 총기 양손 IK, 활122자세 머리 여유최소.229m 및 팔길이 보존 통과. 실제 도구 끝과 대상 표면의 정밀 충돌맞춤은 미완료. 요리·치료·톱질·창찌르기 등 적합수집클립 없는 기존 행동은 legacy 유지.
- 원본 바디/가중치 유지한 SourceParts로 머리6종, 의상4색변형, 표정8채널 동기화. 48조합 및 3장수x5모션 검사. 머리는 늘어짐 방지 위해 Head 단일가중치. 의상4종은 기존 앞치마/주머니 형상이 아닌 재질변형이며 흉터·눈색 추가는 이번 구현 범위 아님. 실패한 기존 메시이식 후보는 work/animation-1112/rejected-appearance에 보존, 게임 Resources에서 제외.
- 원본 휴식 bounds 키1.6m 보정, 수면 바닥-.007m/죽음바닥0m. 반복피격100회+Pop34회 후 스케일 복원오차0. 바닥 화물 Stone/Wood/Clay 폭.6m, 재고5.25 유지 검사.
- Meshy 신규시설8종 게임연결: Campfire/Carpentry/Kitchen/ResearchDesk/SpikeTrap/Stockpile/Workshop/Smelter. 용광로10K remesh FBX10250삼각형 다운로드 완료. 352크레딧 사용/23남음, 추가과금 없음. 실제8시설 크기/바닥/재질전환 통과.
- 다른 채팅 FIRST-DEFENSE 완료 및 Unity반환 확인: 자연준비 창고72초/농장216초/산성375초, 시작버튼525초, 전투60초+복구60초. 별도 최종 회귀는 준비생략 후54초승리(같은회차아님), 오류0/경고6. 소량화물 무한회수, 명령공격 자기치료/수면 간섭, 파괴된 몬스터 MissingReference 수정. 환경·UI11/11~12 변경 함께 저장. 세부 docs/FIRST_DEFENSE_1112.md, RELEASE_VISUAL_1112.md, PRESENTATION_1111.md, PLAYABILITY_1111.md.
- 최종 공식 재컴파일 오류0 경고0. 실제 GUI에서 채굴 프레임 변화 직접확인, HUD포함 캡처확인. 사용자 설정/SaveStorage root 복구 후 Play종료. 사용자 슬롯 저장/덮어쓰기 없음. Graphify 갱신 진행.
- 남은 품질: 모든 장비/외형의 모든프레임 무관통 보장 아님. 정밀 도구접촉, legacy 행동 원본대체, 기존 missing-script 경고, HUD밀도 등 출시전체완료로 기록하지 않음.
- SAVE 후속: changelog/README/검증문서 정리→develop commit/push→당일 Notion 개발일지와 Trouble Shooting에 기존내용 보존 및 GIF/사진 첨부·확인→두세션 종료와 저장성공 확인 후 정상종료. master 및 prefab/prefab.meta 커밋 제외.

- SAVE 후속: Notion 첨부20개 검증(GIF13/사진7) 완료. 개발일지 https://app.notion.com/p/415c4a0ecd3181d39ec6e4778a426c0e / 문제해결 https://app.notion.com/p/415c4a0ecd3181168942e05f007b617b. 다른채팅 최종 completed/idle 재확인. Graphify47007노드/123843관계/3361커뮤니티 갱신 종료0. 원격 develop 전송/검증 진행 중이며 종료는 아직 하지 않았다.
