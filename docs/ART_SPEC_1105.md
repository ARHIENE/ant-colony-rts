# 2026-11-05 캐릭터·시설 리디자인 및 기획 대조

사용자 방향: 기존 쿼터뷰, 손으로 만든 듯한 절제된 로우폴리, 흙·목재, 아기자기한 동화풍 디오라마. HUD 시안의 큰 머리·작은 몸·개미 더듬이·파스텔 머리색 반영.

## 반영

- Blender MCP로 캐릭터 메시를 새로 제작했다. 기존 메시 재사용 0, 기존 제작용 60본 리그·소켓·애니메이션 규약 유지. 리그까지 전부 새로 만든 것은 아니다. 34개 모듈, 17개 표정의 기존 이름/선택 경로 유지.
- 둥근 얼굴, 점눈·볼, 곡선 더듬이, 짧은 팔다리, 녹색 튜닉. 은빛/금빛/분홍/초록/연보라 머리색. 시작 외형 색이 피부·눈·의상까지 덮던 문제 수정.
- 오두막(CC_Hut), 집(CC_House), 창고(CC_Storage), 망루(CC_Watchtower) 4종 교체. 흙벽·목재 프레임·겹지붕·돌받침과 작은 소품. 기존 작업점 Empty 보존. 전체 시설 교체는 아니다.
- 생산용 Resources FBX 및 URP 재질에 연결. 실제 모델을 복제하는 CharacterPortraits 경로도 같은 원본을 사용한다. 별도 새 HUD 구현은 이 작업 범위에 없다.
- 노션 건물 기획의 개축 완료 시 외형 변화 누락: MainMaterial setter와 FacilityVisual 생성 시점에 재료별 표면색/금속성을 연결했다. 공유 재질은 바꾸지 않고 인스턴스별 적용. 지붕·창·장식색 보존. Story 구조 재질과 기존 CC_Wood/Bark/Stone에 적용하며, 재료별 메시 형태 교체나 모든 시설 재질 지원을 뜻하지 않는다.

## 대조 근거와 남은 범위

노션 주 기획 334c4a0ecd3180c4a796e5220302a0bd, 전투 3d0c4a0ecd31818a90e5d1ffac4ca4ec, 미정 3d0c4a0ecd3181699ae1cb864e3f1119, 건물 3d0c4a0ecd3181c28b59e26337c15aba를 읽고 기존 구현/인계와 대조했다. 노션에 남은 과거 미구현 표기를 모두 현재 누락으로 간주하지 않는다.

- 원정은 11/03 보류 결정이다. 신규 게임 로켓 진행의 원정 의존 경로는 남아 있으며 임의 대체하지 않았다.
- 공성 적 종류·첫 평화기간 기능 제한·로켓 최종 웨이브는 확정되지 않은 기획이다.
- 벽 밖 적 잔류로 웨이브가 끝나지 않는 기존 보고는 이 작업에서 해결되지 않았다.
- 전체 노션 기능 감사, 나머지 시설·자원·생물 리디자인, 장기 플레이와 장비 관통 검수는 완료하지 않았다.
- 최종 콘솔 확인에서 AntColony 장면의 Tree_01/Tree_02/Rock_05/MiniBirdBoss Missing Prefab 오류가 남아 있다. 이번에 해당 장면/프리팹은 수정하지 않았다. 컴파일 실패는 false지만 콘솔 무오류로 보고할 수 없다.

## 검증·재현

공식 Unity CLI + 6000.5.8f1 에디터. Storybook1105Import로 기존 GUID를 유지해 FBX 5개를 가져온다. Generic avatar 유효. Storybook1105Checks: 205개 검사 통과(본 연결, URP 슬롯, 7개 동작의 유한 메시/월드 크기, 표정·방어구·도구, 재료 setter와 인스턴스 격리). 전체 애니메이션의 육안 검수를 뜻하지 않는다.

실제 Unity 렌더: art/storybook-1105/Unity_Characters.png, Unity_Buildings.png. 카메라 검수 중 발견한 스킨 100배 축소는 기존 제작 규약인 FBX_SCALE_ALL로 수정했고, 머리 캡 노멀도 바로잡았다. Game HUD 스크린샷이 아닌 실제 런타임 모델의 임시 스튜디오 촬영이다.

원본 Blender와 재현 스크립트: art/storybook-1105/storybook-source.blend, build_character.py, build_buildings.py, studio.py. original-*.fbx는 변경 전 보관본이다. Blender 검증 JSON은 Blender 단계, unity-validation.txt는 별도 Unity 단계 결과다. SAVE/커밋/push/노션 기록 없음.
