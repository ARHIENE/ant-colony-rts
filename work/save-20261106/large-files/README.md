# 2026-11-06 큰 파일 원본 백업

GitHub 연결의 큰 blob 전송이 내부 오류로 실패하여 두 원본은 gzip+base64 분할 백업으로 보존했다. 해당 일반 경로는 이전 원격 버전이며 최신 원본은 이 백업이다. 코드·게임 모델/텍스처는 일반 파일로 반영했다.

현재 PC의 원본 파일은 이미 최신 상태다. 새로 복제한 저장소에서 최신 씬과 그래프가 필요하면 프로젝트 루트에서 아래 Python 코드를 실행한다. 조각을 모두 합친 뒤 복원하므로 각 파일의 바이트가 원본과 동일하다.

```python
import base64, gzip, hashlib
from pathlib import Path
root = Path('work/save-20261106/large-files')
for name, target in [('AntColony.unity.gz','Assets/_Project/Scenes/AntColony.unity'), ('graph.json.gz','graphify-out/graph.json')]:
    encoded = ''.join(p.read_text() for p in sorted(root.glob(name+'.b64.part*')))
    data = gzip.decompress(base64.b64decode(encoded))
    Path(target).write_bytes(data)
    print(target, hashlib.sha256(data).hexdigest())
```

원본 전체의 로컬 커밋은 6b273113fc8674f8afdb58e1df9714e9e33aeead 및 codex/local-save-20261106 보존 브랜치에서 확인한다. 대용량 제작용 .blend·다운로드 원본·Graphify 캐시는 로컬 보존하며 Git 반영에서 제외했다. 손 리그 Blender 작업은 저장했지만 파지·전체 동작은 미완료다.
