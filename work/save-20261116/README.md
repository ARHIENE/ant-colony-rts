# 2026-11-16 SAVE 복원

11월 14일 저녁의 미완료 원격 업로드와 11월 15~16일 변경을 함께 보존한다. 코드·작은 문서는 일반 파일이며 큰 파일과 바이너리는 SHA256 검증 압축 아카이브다. 원본은 현재 PC에 그대로 남는다.

새 복제본에서는 이전 정식 SAVE의 복원 안내를 먼저 따른 뒤 이 폴더의 `restore.py --check`로 아카이브를 검증하고 `restore.py --apply`로 현재 버전을 복원한다. 이 manifest는 저녁 SAVE에서 이어진 변경을 포함하므로 미완료 저녁 manifest를 별도로 적용하지 않는다. 일부 아카이브 조각은 `work/save-20261114-evening/archive`의 이미 업로드된 파일을 재사용한다.

복원은 기존 파일이 다르면 `restore-backup`에 먼저 보관한다. `.prefab`과 `.prefab.meta`, 기존 정책의 제3자 에셋은 원격 커밋에서 제외한다. 이 파일들은 사용자가 별도로 보관한 원본이 필요하다. `work`의 과거 검사·후보는 실행용 Assets로 자동 적용하지 않는다.

구현과 검증 범위: [통합 기록](../../docs/SAVE_20261116.md), [문제 해결 기록](../../docs/TROUBLESHOOTING_20261116.md). 최신 Windows 배포 빌드·정상 경제 엔딩 완주는 미완료다.

Notion: [개발 일지](https://app.notion.com/p/419c4a0ecd31818f9f58cddd983071b7), [문제 해결 기록](https://app.notion.com/p/419c4a0ecd31814bb4c8cdf4bd551e5c).

반복 검사 중간 PNG/JPG는 로컬 원본으로 보존한다(`local-only-images.json`). 원격에는 코드·제작 원본·검증 저장과 문서에 사용한 화면을 포함한다. manifest v2는 여러 파일이 같은 압축 묶음을 참조하도록 `bundles`와 `files`를 나누었다. `restore.py`가 이를 자동으로 처리한다.
