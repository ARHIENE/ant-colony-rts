# 출시용 핵심 시설 에셋 — 2026-11-12

- 사용자 지시: 오늘 신규 기획 목록보다 실제 게임에서 자주 사용되는 필수 에셋 우선. 현재 Meshy 375크레딧까지 사용 승인, 추가 결제 없음.
- 기존 Meshy 나무/바위/광맥/초가집/창고/망루는 중복 생성하지 않음.
- 실제 연결 대상: Carpentry, QueenChamber(시작 Stockpile), ScienceLab, Kitchen, Smelter, SpikeTrap, Workshop, Campfire.
- 선택 근거: 첫 제작·연구, 상시 재고/식사, 금속·장비 제작, 반복 웨이브 방어/휴식. 기존 Facilities 모델의 외형 교체 후보이며 신규 게임 규칙이 아니다.
- 이미지 시안 8장 ×9 =72. Meshy7.1 Ultra2K 모델+Meshy7 2K 텍스처 8종 ×35 =280. 합계352, 화면 잔액23 확인. 텍스처 단계 차감은 비동기.
- 모든 시안 형태 확인. 모델 생성 진행 후 무료 리메시(시설10K, 작은 모닥불/함정3K 목표), 파일 다운로드·Unity 적용 검증 예정.
- 현재 Unity 환경/UI 담당에게 양도된 상태. Assets 수정·게임 연결 완료 아님.
- 벌목 신규 애니메이션 생성하지 않음. 승인된 도구 위아래 방향 유지.

| 파일명 예정 | 실제 행동/기능 |
|---|---|
| Carpentry | 초기 목공 제작 |
| Stockpile | 시작 물자·운반 재고 |
| ResearchDesk | 정착지 연구 |
| Kitchen | 식사 생산 |
| Smelter | 광석 제련 |
| SpikeTrap | 웨이브 방어 |
| Workshop | 장비 제작 |
| Campfire | 장수 휴식·보온 |

## 11-12 다운로드 후속
- 시설7종 다운로드/CRC/Unity 임포트 완료. 위 초기 예정 상태를 대체함.
- 용광로도 무료10K 리메시 및 FBX 다운로드 완료(23,962,207bytes, ZIP CRC통과,6파일). Meshy 표시10,250삼각형, 잔액23 유지.
- 용광로 정규화 원본: art/release-assets-1112/models/Smelter. Unity FIRST-DEFENSE 검사 중이므로 Assets 복사/임포트는 반환 이후.
- 시설7종 실제 게임 연결은 FIRST-DEFENSE 담당 FacilityVisual.cs 작업 중.
