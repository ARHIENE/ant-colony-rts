# 2026-11-14 통합 SAVE 복원

이전 SAVE(11/7, 11/10, 11/12, 11/13)를 먼저 복원한 뒤 실행한다.

python work/save-20261114/restore.py --check
python work/save-20261114/restore.py --apply

원본과 큰 텍스트는 SHA256 검증 gzip/base64 묶음으로 보존한다. 실제 원본은 로컬에도 유지하므로 Git dirty가 남을 수 있다. prefab/prefab.meta 및 제3자 팩은 커밋하지 않는다. 검증/잔여는 docs/SAVE_20261114.md. 오늘 노션은 https://app.notion.com/p/417c4a0ecd31814a9512debff0cb4e91 .
