from pathlib import Path
root=Path('E:/Git/ant')
for name in ['AdvancedIndustry1027Checks.cs','Save1027Views.cs','UndergroundPowerOrder1027Checks.cs','UndergroundProduction1027Checks.cs']:
 p=root/'AgentScripts'/name
 p.write_text(p.read_text(encoding='utf-8').rstrip()+'\n',encoding='utf-8')
p=root/'log.md'
s=p.read_text(encoding='utf-8')
s=s.replace('커밋/push·캡처·Notion 기록은 아직 진행 전이다.','실제 메뉴 캡처5장 검수·Notion 첨부/관련 페이지 갱신 완료. 선택 파일364개를 대조했으며 커밋/push 마무리 중이다.')
s=s.replace('임시 저장 경로로 검사했고 Play 종료. 지금까지 새 Unity 캡처/전 화면 육안/Player 빌드는 미수행이며 이번 SAVE에서 관련 실제 화면을 준비해 확인한다.','임시 저장 경로에서 실제 Game 메뉴5장(제련/평가/외부 조사/지하 탐사/작업 관리)을 검수·첨부했다. SAVE 후속 UI16 재통과, 메뉴 위 지정 표식 겹침/제련 안내 보완. 건설 작업 진입·잔량 감소는 실행 확인했으나 추가 건설 캡처는 검은 화면으로 채택하지 않았다. 설정/저장 경로 복원·Play 종료. 전 화면/전 장비 조합 육안·Player 빌드는 미수행이다.')
s=s.replace('유체 용기 충전/반출·유출 처리·추가 화학 제조법·전용 자원/소품 일부,','유체 용기 충전/반출·유출 처리·추가 화학 제조법·정제소 작업자 요구/심층 채굴기 재배치 규칙·전용 자원/소품 일부,')
s+='\n## SAVE 기록\n- 감사: `docs/SAVE_AUDIT_20261027.md`. 실행 검사 스크립트를 저장하며 Temp 결과 파일은 재시작 시 없어질 수 있다.\n- 개발 일지/캡처: https://app.notion.com/p/405c4a0ecd3181b2beddfd53845cd723\n- Trouble Shooting: https://app.notion.com/p/405c4a0ecd3181179153eaf8cf639539\n- 자동 작업·자원·건물·연구·유닛·UI·메인 게임·구현 점검·미정·외부 조사·전날 일지의 관련 표기도 갱신했다.\n'
p.write_text(s,encoding='utf-8')
p=root/'docs/IMPLEMENTATION_BOARD.md'
with p.open('a',encoding='utf-8') as f:f.write('\n- **SAVE-1027 / Codex** — 위 항목은 각 작업 당시 기록이며 최신 상태는 log.md와 docs/SAVE_AUDIT_20261027.md 기준. F1/F2·실제 지하 생산·상위 산업 연결까지 통합했고 메뉴5장 직접 검수·Notion 첨부. SAVE 중 제련 조건 안내/메뉴 위 월드 표식 겹침 보완, UI16 재통과. 전체 장비 조합 육안/Player 빌드·기획 전체는 미완료. 기존 로컬 변경 보존·검증 파일 선택 저장.\n')
p=root/'README.md'
s=p.read_text(encoding='utf-8')
needle='> **작업 지정·자원:**'
intro='> **2026-10-27 SAVE:** 자동 작업·실물 공급, 장비/동작, Claude 평가·외부 조사·지하 탐사, 유한 지하 생산과 가스/티타늄 산업을 통합했습니다. 실제 메뉴5장을 검수해 개발 일지에 첨부했습니다. 저장 범위·검증 한계와 잔여는 [SAVE 대조](docs/SAVE_AUDIT_20261027.md)를 참조합니다.\n\n'
s=s.replace(needle,intro+needle,1)
p.write_text(s,encoding='utf-8')
