# 2026-11-13 세 세션 통합 SAVE 복원

기존 11/7, 11/10, 11/12 복원을 먼저 적용하고 아래를 실행한다. 큰 원본과 바이너리는 SHA256 검증 gzip/base64 묶음으로 저장된다. 원본은 로컬에 그대로 있으므로 git dirty가 남을 수 있다. prefab/prefab.meta와 실패 후보·중복 촬영 프레임은 제외한다.

```
python work/save-20261113/restore.py --check
python work/save-20261113/restore.py --apply
```

검증과 남은 범위: docs/FINAL_MOTION_1113.md, THREE_WAVES_1112.md, THREE_WAVES_REVIEW_1112.md, COMMANDER_ONLY_1112.md.
최종 사용자 지시로 컴퓨터 종료는 하지 않는다.
