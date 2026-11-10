# 2026-11-10 통합 SAVE 복원

대용량 원본은 350KB 이하 gzip 조각을 base64로 감싸 그룹별로 `restore-data-v2`에 저장한다. 동일 SHA256 데이터는 한 번만 보관한다. manifest는 원래 경로·크기·SHA256·조각 목록을 기록한다. 현재 PC의 원본은 그대로 유지한다. 따라서 대용량 원본 경로가 Git상 수정/미추적으로 보이는 것은 예상된 상태다.

새 복제본은 기존 `work/save-20261107/README.md` 복원을 먼저 수행하고, 프로젝트 루트에서 아래를 실행한다. Python 표준 라이브러리만 필요하다.

```text
python work/save-20261110/restore.py --check
python work/save-20261110/restore.py --apply
```

복원 전에 모든 조각의 압축/원본 SHA256을 검증한다. 다른 내용의 기존 파일은 `restore-backup`으로 보존한다. 사용자 규칙에 따라 prefab/prefab.meta와 외부 Asset Store 패키지는 포함하지 않는다.

오늘의 모션 GIF4개는24클립의 Unity 리타게팅 미리보기다. 각30프레임을3초 주기로 통일했으므로 실제 재생속도와 다르다. `actual-mining-two-frames.gif`는 실제 Game 화면2장을450ms 간격으로 반복한다. 이를 연속 녹화로 해석하지 않는다. 별도 시도한 ScreenCapture 녹화는 에디터가 Edit 상태여서 파일을 생성하지 못했고 결과물에 포함하지 않았다.

개발 일지: https://app.notion.com/p/414c4a0ecd3181e8b625dff8183a2df1
문제 해결: https://app.notion.com/p/414c4a0ecd318156b06fe5e29a770738

검증/미완료는 `log.md`와 각 담당 검증 문서에 구분했다. 전 무기·전 외형 무관통, 미구현 기획13종, 모든 지형의 새 게임 검증 완료를 의미하지 않는다.

