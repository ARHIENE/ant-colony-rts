from pathlib import Path
import json,collections,subprocess
root=Path('E:/Git/ant');j=json.loads((root/'work/save-20261026/inventory.json').read_text(encoding='utf-8'))
files=sorted(set(p for p in j['tracked']+j['untracked_relevant'] if p.startswith(('Assets/_Project/Scripts/','Assets/_Project/Editor/','AgentScripts/')) and p.endswith('.cs')))
text='''# Claude·Codex 통합 SAVE 대조 — 2026-10-26

## 범위와 근거
- 10월25일 낮 SAVE `6b9cfb1` 이후, 10월25일~26일 이 채팅까지 이어진 작업이다. 작업 보드, HEAD 대비 실제 diff, Claude 세션 3개, Codex E1/E2/F1/F2/F3/G1/G2 인계와 사본을 대조했다.
- Claude 기록: `13fff952-aaf1-4403-8a25-e1595cbeb6dd`(E1/E2 통합·UI·G26·메뉴), `df6935fe-43ea-4908-a8f7-dd131d73587a`(H1/H2·그래픽 감사), 앞선 `c55db1a6-569b-4adb-b360-2c810c7449ec`(낮 SAVE 전 검사). 원본 대화는 로컬 .claude에 보존하며 저장소에는 복사하지 않는다.
- 과거 보고를 현재 검증으로 과장하지 않는다. 강제 Animator.Update로 통과했던 초기 검사 뒤 사용자 화면에서 전체 재생 정지가 재현됐고, 공통 리그 가시성 판정 수정 후 실제 프레임 검사로 재검증했다.

## 본 프로젝트에 반영된 작업
| 묶음 | 결과 | 근거/한계 |
|---|---|---|
| E1 D04 | 평진·삼국지풍7·현대식6, 적성·연구6·전투/도보 보정·저장 호환 | Claude 통합, 출전 UI 연결. ImplD04Checks 과거363, 이번 SAVE 재확인 결과는 아래 기록 |
| E2 D06/D07/D09 | 철수 단계·실물 짐/시신·긴급 출발·수송 파손/수리/해체·순회 저장 | Claude 통합과 UI, 과거82/208. 이번 SAVE 결과 별도 |
| Claude H1 | 특성 칩, 원정대 패널 높이/버튼 넘침, 불시착 잔해6×4 | 4개 패치 중 H1 3개 반영. 화살표는 후속 사용자 요청으로 제거 |
| Claude H2 | 지면 고정 드래그·마우스 놓침 복구, 설정 초기화/품질/해상도/스크롤/키 설명 | 적용 후17검사. 수면 장수 자동 제외는 미확정 규칙 변경이라 빼고 기존 선택 유지 |
| F3+Codex 그래픽 | 시설 축/비균일 부모 배율, 자원46 모델·생물6종·주민·공사/철거·전리품·시체, URP 머티리얼/런타임 셰이더 참조 | 시설710, 자원200, 사건/공사/시체14, 시민/전리품10. 원본 Blender art는 로컬 보존 정책 유지 |
| 입력/시작 | 맵 설정 입력차단·스크롤/재추첨, 장수 키/클릭 범위, 개인식량 보유자 이탈 방지·선택한2명 유지, 가장자리 이동 복구 | 메뉴/날씨14, 시작/클릭/보급13, 원정UI27 |
| 공통 애니메이션 후속 | 클립 경로→실제 리그, 중복90도 제거·초상 Idle_A, 메시가 리그 형제여서 생략되던 재생 AlwaysAnimate/스킨 갱신 | 강제 Animator.Update 없이 장수13동작·생물6리그·시민40검사 |
| 채집 후속 | 잔여거리0 고착 해소·도착시 경로 정지·작업동작 우선, 이동/채집/운반 대상 링/문구 | 실제5m 이동·걷는 메시·벌목·채집량·취소14검사 |
| 날씨 후속 | 생물군계×기후×계절, 사막 드문 강수/모래폭풍·숲/물가 비/안개·한랭/열대 강수형·동굴 제외, 계절꽃잎/낙엽/적설 제한·복원 보정 | 445검사. 실측 기후 자료가 아니라 게임 내 잠정 확률 |
| 즉시 재생처럼 보이던 자원 | 갈 곳 없는 바닥 물품 반복 줍기/내려놓기 차단, 실제 저장 Host·품목 허용·경로 확인 | 저장 공간만 있고 장소 없는 경우·금지/허용·실제3개 입고10검사. 식물120~300초 재성장 변경 없음 |
| 기존 G26 작업 보존 | SceneObjects 등록 캐시·호출 경로 교체·원산지 식품 등 기존 미커밋 변경 | 사용자의 후속 성능 최적화 보류 지시 이후 추가 성능 작업 안 함. 목표 성능 달성 주장 안 함 |

## 미통합·남은 작업
- F1 D02/D03 평가·외부 조사: 사본 코드/오프라인 컴파일 완료, 본 프로젝트/Unity/UI 연결 미완. `docs/patches/unfinished-20261026/F1-source-snapshot.patch`와 인계 보존.
- F2 D08 탐사·발견·저장: 사본 코드/오프라인 컴파일 완료, UI/채굴 생산·Unity 미완. 동일 폴더 F2 스냅샷/인계 보존. 스냅샷은 기준 E 변경도 포함하므로 main에 그대로 apply하지 않는다.
- 전용 자원 모델 Chitin/Wax/Component/Water/Soil 5종은 Claude에게 전달할 제작 범위를 제안했으나, 납품/통합 증거는 없다. 낚시터 표시·미사용 DigSite 모델·원정지 평판 지형도 남는다.
- `docs/NOTION_APPLY_1025_codex.md`의 미반영 기획 대조, D02/D03/D08 후속, 도구/광물/의료의 기존 미완 범위를 완료로 바꾸지 않는다.
- 성능 최적화와 림월드식 채취는 사용자 명시로 보류.

## SAVE 보존 정책
- develop에 실제 코드·직접 제작한 Unity FBX/애니메이션/텍스처/머티리얼·meta·검사·문서를 포함한다. prefab/prefab.meta는 커밋하지 않는다.
- 기존 제외 정책에 따라 씬·Water.mat·TimeManager·URP asset 재직렬화·Planet Earth Free/Resources WorldMap·raw art·graphify·Temp/캐시·개인 도구는 로컬에 보존하고 이번 커밋에서 제외한다. 원본 art와 사본은 삭제하지 않는다.
- 새 캡처는 SAVE에서만 생성. 실제 화면 확인 결과·최종 커밋·Notion 링크는 log.md에 기록한다.

## 전체 소스 대조 목록
'''
for group,ps in __import__('itertools').groupby(files,key=lambda p:'/'.join(p.split('/')[:4]) if p.startswith('Assets/') else 'AgentScripts'):
 text+='\n### '+group+'\n'+''.join('- `'+p+'`\n' for p in ps)
(root/'docs/SAVE_AUDIT_20261026.md').write_text(text,encoding='utf-8')
old=(root/'work/save-20261026/log-before-save.md').read_text(encoding='utf-8-sig')
summary='''# 2026-10-25 SAVE — 2026-10-26 이관
- 6af9e5a(링크 후속 d0501a0/6b9cfb1). 전 세션 미실행 검사 완료, 기획 전체 대조(구현108/부분69/미구현54), D01/D05/D09 1차 구현과 월드맵 정리. C2~C6·Phase4·Stage1·SettlementDefense·TransportRoute·WorldMap·SaveRoundtrip·ImplA2/ImplB4/Impl1025Codex 통과 기록.
- G26은 기준 미달(fps1.1/저장9.7초/로드38초), 픽스처 인원 부족. 당시는 D04·D06/D07·D02/D03·D08 및 다거점 순회 검사가 남았음. 이후 상태는 최신 log와 SAVE_AUDIT 참조.
- Notion 개발 일지 https://app.notion.com/p/404c4a0ecd3181339045f8aca1e3ae1f / Trouble https://app.notion.com/p/404c4a0ecd3181738670fc51725d045e. 일부 기획 미반영 목록은 NOTION_APPLY_1025_codex.md.

'''
cl=root/'changelog.md';current=cl.read_text(encoding='utf-8-sig')
if not current.startswith('# 2026-10-25 SAVE — 2026-10-26 이관'):cl.write_text(summary+current,encoding='utf-8')
r=root/'README.md';s=r.read_text(encoding='utf-8-sig');s=s[s.index('# 개미 소굴 RTS'):]
start=s.index('현재 외형은');end=s.index('\n\n아래 날짜별',start)
s=s[:start]+'''현재 외형은 SD kemono 주민 모델입니다. 직접 제작한 시설·자원·생물·시민·소품 모델을 연결했습니다. 실제 프레임에서 이동·전투·작업 메시가 변하는 검사와 그래픽 연결 검사를 통과했습니다. 기획 전체 완성·모든 화면 검수·Player 빌드 완료를 뜻하지 않습니다.

> **2026-10-26 통합 SAVE:** Claude/Codex의 10월25일 낮 SAVE 이후 작업을 대조했습니다. 진형14종·연구6종과 출전 선택 UI, 철수·실물 수송·파손/수리/해체 및 원정 패널, 설정/드래그/시작 인원, 그래픽·공통 애니메이션/초상, 등급 특성 칩을 반영했습니다. 특성 칩 화살표는 제거했습니다. 상세 근거·검증 범위·미통합 사본은 [통합 대조](docs/SAVE_AUDIT_20261026.md)와 [현재 상태](log.md)를 참조합니다.

> **조작·자원:** 좌클릭은 선택, 선택한 장수로 자원을 우클릭하면 채집합니다. 대상 링과 ‘채집하러 이동 중/채집 중/자원 운반 중’으로 명령을 구분합니다. 갈 곳 없는 바닥 물품을 반복해서 줍고 내려놓지 않으며 실제로 도달 가능하고 품목을 허용한 창고·비축더미가 필요합니다. 자연 식물은 기존 규칙대로120~300초에 재성장하고 돌·광석은 고갈됩니다. 화면 가장자리 마우스로 카메라를 이동하며 설정에서 켜고 끌 수 있습니다.

> **날씨:** 생물군계·기후·계절을 함께 반영합니다. 사막은 비/눈이 드물고 모래폭풍이 가능하며, 물가·숲은 비/안개가 더 잦습니다. 한랭은 강수를 눈으로, 열대는 비로 바꾸고, 동굴에는 야외 강수가 없습니다. 봄 꽃잎·가을 낙엽·겨울 적설도 환경에 맞춰 제한합니다. 확률은 게임용 잠정값입니다.

> **미완료·보류:** D02/D03 평가·외부 조사(F1), D08 탐사(F2)는 사본의 오프라인 구현이며 본 프로젝트 Unity/UI 연결은 미완료입니다. 소스 스냅샷을 보존했습니다. 전용 자원 모델5종·낚시터·원정지 지형 등이 남고, 성능 최적화와 림월드식 채취 변경은 사용자 요청으로 보류합니다.''' +s[end:]
r.write_text(s,encoding='utf-8')
print('Wrote SAVE audit:',len(files),'source files; changelog migrated; README updated')
