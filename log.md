# 프로젝트 로그

## 개요
- 개미 소굴 RTS(가제): 자원 채집 + 건설 + 유닛 강화 + 레이드 RTS
- 시점: 아이소메트릭(쿼터뷰), 싱글플레이 메인(멀티는 후순위)
- 엔진: Unity 6000.5.8f1, URP
- 프로젝트 루트: `E:\Git\ant`
- 버전관리: Git/GitHub (`github.com/ARHIENE/ant-colony-rts`, public)
- 기획 원본: Notion "게임 개발 노션 정리 > 기획(스펙 문서)" 페이지 참고
- 상세 작업 히스토리는 `changelog.md` 참고 — 이 파일은 "현재 상태 + 다음 할 일" 요약만 유지

## 한 줄 컨셉
플레이어는 개미 여왕의 지휘자가 되어 일개미로 자원을 캐고 소굴을 확장하며, 병정개미로 야생 몬스터를 상대하거나 다른 개미집을 약탈하고 거대 보스를 물량으로 레이드하는 RTS.

## 목표
**MVP가 아니라 Notion "기획(스펙 문서)" 페이지에 적힌 내용 전체 구현이 목표.** 기획이 개발 중 바뀌면 Notion 해당 하위 페이지를 교체(추가 아님)하고 관련 페이지도 같이 동기화할 것.

## 현재 상태 (Codex 인수인계 및 연결 설정 — 2026-09-05)

### 이번 세션
- 기존 로그와 주요 코드를 검토했다. 게임 코드/씬 수정 및 새 Play 테스트는 하지 않았다.
- Notion MCP 로그인 및 개발 일지 읽기 성공. SAVE 시 지정된 개발 일지 아래 날짜 제목/표정 아이콘/번호 목록으로 새 페이지를 작성한다.
- 카카오 PlayMCP는 자동 OAuth 등록이 IP 제한(403)으로 실패하여 공식 일회용 토큰 교환 방식으로 연결했다. `mcporter call mcp-gateway.KakaotalkChat-MemoChat 'message=내용'`으로 실제 전송 성공(200자 이하).
- mcporter 설정/인증은 사용자 홈 `.mcporter/`에 있으며 Git에 포함하지 않는다. 비밀값은 로그에도 기록하지 않는다.

### 코드 확인으로 구분한 미완성/검증 범위
- WorkerAnt는 수동 채집 명령 및 반납 코드가 있으나 반납 후 Idle로 끝난다. 지정 노드로 돌아가는 반복 채집은 없다. 실제 클릭→채집→반납 전체 흐름은 추가 검증 필요.
- 반란은 WildMonster 전환까지만 구현되어 있고, WildMonster에는 플레이어를 공격하는 AI가 없다.
- 일개미의 건물 배치/건설 과정은 미구현이다. 현재 건물은 생산/저장과 확장 구역 활성화 수준이다.
- SnapToTerrainMenu의 Raycast는 지형 Collider만 대상으로 하지 않아 자기 자신/다른 물체를 맞힐 가능성이 있다.
- 이전 isOnNavMesh 확인은 스폰 위치 검증이며 목적지까지 이동 성공을 의미하지 않는다.

### 이전 세션 구현 상태 (상세 이력은 changelog.md)
- **일개미 수동 채집 지시**: 세션 3 막바지에 꺼둔 자동 채집을 대체 — `WorkerAnt.CommandGather(ResourceNode)` 신규, 자원노드 우클릭 시 이동·채집·반납 코드 연결(반납 후 Idle, 실제 전체 흐름 추가 검증 필요). Notion 자원/유닛 시스템 문서의 "미해결" 상태 갱신 완료
- **RTS 카메라 무한 패닝 버그 수정**: 엣지스크롤 시 지형(400×400) 범위를 클램프 없이 벗어나던 문제. `IsometricCameraController`에 `minX/maxX/minZ/maxZ`(0~400) 추가해 `focusPoint`를 지형 범위 안으로 제한
- **건물/자원노드/보스 클러스터 지형 중앙 재배치 + NavMesh 재베이크**: 원점 구석(0~9 범위)에 몰려있던 걸 지형 중앙(약 200,200)으로 상대 배치 유지한 채 이동, `SnapToTerrainMenu`로 실제 지형 높이 스냅, `NavMeshBakeArea`도 같이 옮겨 재베이크 완료(`isOnNavMesh` 확인)

### unity-cli 관련 (이 프로젝트 코드 아님, 메모리 [[unity_cli_bridge_local_patch]] 참고)
- unity-cli는 MCP 도구가 아니라 로컬 CLI 바이너리(Bash로 직접 실행, `--port 16401` 필수)라는 점 재확인
- Play 모드에서 마우스 클릭/드래그(버튼 이벤트) 시뮬레이션은 실제 게임 Input System에 반영 안 됨 — 단순 위치값 읽기만 신뢰 가능. UX 클릭 흐름 자동검증은 컴포넌트 필드 직접 조회로 대체할 것
- 이번 세션도 재컴파일 데드락 2회 재발(둘 다 taskkill 후 재시작으로 해결) — 미저장 변경사항이 있을 때는 taskkill 전에 Win32 `SetForegroundWindow`+`SendKeys`로 Ctrl+S를 직접 보내 저장 여부를 파일 mtime으로 확인하는 방법으로 데이터 손실 방지 가능함을 확인

## 알아둘 것
- `Assets/_TeamImport/`는 SIMUL-TeaamProject(팀 원본 저장소, private) 반입본 — git에 절대 안 올라감(`.gitignore` 처리 완료). 씬의 `TerrainGenerator` 오브젝트는 실제로 이 팀 원본의 `TerrainGenerator.cs`(우리 `MapGenerator.cs`가 아님)를 그대로 쓰고 있음 — 헷갈리지 말 것
- `Assets/Resources/Telegraph/*.prefab`은 우리 컴포넌트로 만든 프리팹이다. 현재 규칙은 `.prefab`과 `.prefab.meta` 모두 커밋 제외다. 다른 환경에서 재생성이 필요할 수 있다(방법은 changelog.md 세션3 참고).
- NavMesh는 이제 지형 중앙(200,200 근처) 60x60 볼륨으로 베이크돼 있음 — 플레이 영역을 더 넓히면(건물 확장 등) 재베이크 필요

## 다음 세션 할 일 (우선순위 순)
1. 선택된 유닛 정보 표시 UI(체력바 등)
2. 역할군별 병영 분리, 카테고리별 강화(연구소), 농사/낚시, 특수자원, 약탈, 방어전(적 침공 이벤트), 대형개미/특수배양소 — 기획서 대비 대부분 미구현 상태 유지

## 미정/논의 필요 항목 (기획서 5번)
- [ ] 개미 종족(불개미/베짜기개미 등) 다양화 여부
- [ ] 보스 종류 및 패턴 상세 설계(보스 스케일 다양화는 아직 MiniBird 하나뿐)
- [ ] 세션 길이 목표
- [ ] 세션 내 성장 vs 세션 간 영구 성장 분리 여부(기획서 3.5)

## 이번 SAVE 개발 일지
- https://app.notion.com/p/3d2c4a0ecd3181cd8487eb72b1809b8d
- 이전 unity-cli 캡처(2026-09-05 12:18:35) 재사용 첨부. 이번 세션 새 캡처/Play 검증 없음.
