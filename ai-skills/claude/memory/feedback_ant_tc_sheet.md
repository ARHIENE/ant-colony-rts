---
name: feedback-ant-tc-sheet
description: "ant 게임테스트 TC 시트(Google Sheets) 수행 규칙 — 판정 7종 의미, 파일로 내려받지 말고 시트에서 직접 읽고 직접 기록"
metadata:
  node_type: memory
  type: feedback
  originSessionId: 967359c9-6524-4976-a253-371e6ef46fc9
  modified: 2026-10-31T07:30:05.237Z
---

ant TC 시트: https://docs.google.com/spreadsheets/d/1uFO2jj09xPoA3wRVq8kbIBpLyeUzfwWiNShNlcR4BJc (9개 분류 탭, 열 A=ID B=우선순위 C=기능 D=제목 E=사전조건 F=절차 G=기대결과 H=수행결과 I=평가환경 …).
판정(2026-10-31 사용자 지정): 테스트 안 함=Untest, 정상 작동=Pass, 기대결과와 다름=Fail, 미구현=N/A, TC가 기획과 다름=NeedtoModify, 테스트할 데이터 없음=NoData.
범위: P0·P1 먼저. Fail은 기록만(수정은 별도 요청 시).

**Why:** 사용자가 탭 전체를 파일로 내려받으려던 시도를 중단시키고 "파일로 받지 말고 스프레드시트에 직접 해"라고 지시.
**How to apply:** 시트를 작은 범위로 직접 읽고(get_values), Unity Play 검사 후 H(수행결과)·I(평가환경)만 update_values로 기록. TC 본문·서식 수정 금지. 관련: [[env-python-store-alias]]
