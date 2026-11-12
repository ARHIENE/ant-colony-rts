# 외형 겹침 원인 / 준비 상태
- Unity 점유: COMMANDER-ONLY-1112. Assets 변경/CLI 제어 없이 오프라인 진단.
- 확인된 원인: Native1112SourceParts.Group의 위치 임계값 분류가 앞머리·옆머리를 Face에 포함. face-only.png / head-colored-hair.png에서 재현. 전체 캐릭터 2개 중첩이라는 추정과 구분한다.
- 얼굴/표정: 단일 Face 메시의 blendshape 동기화 구조. 얼굴에 남은 머리도 표정 변형을 받는다. 얼굴 여러 개가 동시에 표시되는지는 Unity 재현 검사 전.
- 수정 후보: Face 9,658삼각형 중 4,917면을 Hair로 이동, Face4,741면 유지. 원본 UV/텍스처/연결성을 기준으로 오프라인 후보 선정, explicit triangle keys 생성. 피부/눈/입 보존은 candidate-face.png / candidate-head.png 정면 확인. 옆·뒤/6변형/2리그 실제 재생 검증 필요. 이 결과는 Unity 캡처가 아닌 저장된 메시의 오프라인 진단 렌더.
- pending/Native1112SourcePartsCorrected.cs: 원본 삼각형 키 수 일치 preflight 후 원본리그 파츠 재생성. 원본 관절/클립 변환 없음. 적용 전 SourceParts 백업 필요. 아직 실행 안 함.
- 장비: NativeMotionPlayer가 MotionRevision 변경에만 기존 렌더러 목록 갱신. 모션 변화 없는 Wearables 교체는 누락 가능. Wearables.Revision 및 이를 확인하는 숨김 목록 갱신 초안 2파일 준비. 실제 중복 재현/회귀 전.
- pending/Appearance1112Audit.cs: 두 리그×6머리/단일얼굴/머리의 표정변형 없음/장신구3회교체 후 기존렌더러 숨김 검사. 기존 Native1112Cosmetics의8표정 동기화, Wearable22조건 검사 함께 재사용 예정.
- legacy fallback: art/anime-settler-1105/build_textured_modular.py도 위치 기반 파츠 분류. 기존 Kemono 원본에도 같은 증상이 남는지 추가 확인 필요. 원본모션 경로만 수정하고 전체 해결로 기록하지 않는다.
- 이미 승인된 도끼/활/방패 방향은 수정하지 않는다. 장비판정/스탯 변경 없음.
- 적용 전 pending 파일과 실제 Assets 최신 diff 확인, 설정/저장경로 백업 및 격리게임 검증 후 복원/Unity 반환.

## 외형 슬롯 반환 — 2026-11-12
- 얼굴 내부 흰머리 잔류는 오프라인 재현. UV4917면 재분리 후보의 구조검사12조합 통과했지만 실제 Game 화면이 깨져 사용자 거부. 구조 PASS를 해결로 기록하지 않음.
- SourceParts94파일(머리/얼굴/표정/재질 포함) sourceparts-before.zip으로 두 번 복원, 현재 원본해시 일치 및 Unity Refresh 후 두 리그 face9658/hair34186 확인. head shadow reception OFF 후보도 복원. 원래 외형 결함은 남음.
- 장비 교체 숨김목록 누락 수정(MeshyEquipmentWearables.Revision, NativeMotionPlayer 비교)은 유지. 컴파일0, 모션변경 없는 장신구3회교체 기존렌더러 숨김 PASS. 전체 장비 파지 해결 판정 아님.
- 원본 전체 몸 renderer 자동 재활성화는 두 클립 검사에서 재현 안 됨. Edit Bake 렌더는 smooth, 실제 Game은 Unlit/CPU Bake/깊이범위/그림자수신OFF에서도 깨짐. 자기 그림자 단독 원인 가설은 배제, 다음 세션은 live JSON(source/baked/UV/weights)로 정확 비교할 것. 게임 시점의 head mesh/재질 정보를 work/appearance-overlap-1112에 보존.
- 사용자설정/SaveStorage root 복원, Play 종료, 공식 Edit/ready 확인. 사용자 슬롯 쓰기 없음. 임시 SSAO와 재질 변경은 finally로 복원. 추가 Assets 변경 중지.
- 인계 순서: COMMANDER-ONLY 남은 규칙 전환 적용/컴파일 → THREE-WAVES 연속플레이 → 외형 다음 검증 슬롯.

## 데이터 손상 확인 및 다음 후보
- all-mesh-integrity.json: 복원본26메시는 인덱스 정상, 거부본26메시 중 모든 머리12개에서 범위 초과. Rig0 정점37719/max43526/잘못된참조17714, Rig1 정점37715/max43524/잘못된참조17708. 실제 live JSON에서도 동일 확인.
- CopySerialized로 크기가 달라진 native Mesh 덮어쓰기 경로가 의심돼 ClearBlendShapes/Clear 이후 vertex/normal/UV/tangent/bindpose/weights/indices/shape 명시 복사로 후보 변경. 생성 직후·복사 직후·Play검사에서 인덱스/스트림 크기 검사. 아직 Unity 실행 전이며 수정 효과 확정 아님.
- 얼굴5482/5528정점의 주가중치는 Head, 나머지46정점은 y1.159~1.180 목/어깨 경계. 어깨가중치 존재만으로 얼굴 전체를 Head에 고정하지 않는다. 얼굴표정 별도 시각검수 필요.
- Unity는 COMMANDER-ONLY 검증 종료 후 THREE-WAVES에 인계. Assets 동결 유지, 외부 work 후보/오프라인 검증만 진행.

## 짧은 Unity 슬롯 결과 (13:46 UTC)
- 명시적 Mesh 스트림 복사 후보 실행 성공. 두리그 Body21811/Hair39103/Outfit35614/Ears1973/Face4741. 전체26메시 index/stream/shape검사 PASS. 후보 유지(복원 안 함).
- 격리 새 게임에서 12리그/머리 조합과 모션변경 없는 장신구3회교체 검사 PASS. 실제Unity 화면6머리/8표정 캡처 unity-fixed/. BOB/CROP/LONG/TWIN의 원래 심한 메시파손 및 흰앞머리 겹침 해소 확인. 모든표정/모든각도 합격 아님.
- PONY/UPDO에서 옆머리 경계가 직선으로 늘어나는 별도 변형문제 관찰. builder의 z<0 하드분기 원인 후보, 다음은 연속 가중치로 수정/검수. 전체 외형 완료로 기록하지 않음.
- 사용자설정/저장root복원, Play종료/Edit ready 확인 후 THREE-WAVES에 즉시반환. Assets 동결 재개. 사용자 저장슬롯 쓰기 없음.

## 후속 경계 변형 후보 (아직 Assets 미반영)
- PONY/UPDO z<0, TWIN abs(x)>.07 하드분기를 SmoothStep 연속가중치로 변경. 저장된 실제 메시의 변형 전후 모서리길이 최대비: PONY88.72→8.34, TWIN259.71→6.31, UPDO36.12→3.87. 이 수치는 각 머리 내 최단 모서리 비율이며 시각합격 기준 아님. 오프라인 정면 예비검토 후 실제 측/후면 필요.
- 실제 Game Blink/Laugh/Sleep/Pain 캡처에서 눈옆 경계 틈 관찰. Face와 다른파트 공유정점1087개 중 원래Blink 최대이동0.033657m. 머리는 움직이지 않아 틈이 생김. Face모프에 경계거리 SmoothStep 감쇠 추가, 동일 계산상 공유경계 최대이동0m. pending builder만 수정, 현 게임엔 아직 미반영.
- 제작대 코드 원인: 일반 Processor/Kitchen은 7m이내면 도착전 StopMoving+작업 시작, 화학대/Supply만 목적지 도착 확인. CommanderWork/AntVisual.Work 최소 후보 및 실제 프레임 약품생산 fixture 준비(work/life-assets-1112/station-drafts). 다른세션 연속검증 중 적용안함.

## 후속 Unity 슬롯 및 화면 검수 — 2026-11-12 23:27 KST 반환
- PONY/UPDO/TWIN 연속 변형·표정 공유경계 감쇠 적용. 공식 재컴파일 오류0/경고0, 26메시 스트림·12리그/머리 조합·장신구3교체 PASS. 최초 run_script는 도메인리로드 도중 연결종료, 에디터복구/미적용 확인 후 재시도 성공.
- 실제 Game 52장(두 리그 × [6머리×3각도+8표정]) 직접 검토 완료. 이전 폭발형 메시 파손/흰앞머리 중복 및 PONY/UPDO 하드경계 늘어짐은 해소. 추가 잔여: CROP/UPDO 뒤통수 아래 목에 머리색 판 형태가 노출; Blink/Sleep 눈이 완전히 닫히지 않음. 전체 외형 합격 아님. 후면 이상은 angle-review/*-CROP-180.png 및 *-UPDO-180.png에 명확.
- AgentScripts/Native1112SourceParts.cs에 이번 검증한 builder 승격. 현재 적용 에셋 재현용. 이후 목 후보는 별도 준비하며 Assets 동결 중.
- 51개 직접 행동은 요청된 action과 실제 MotionName/원본 재생 일치 검사 PASS. 이전 action 감사의 stale MotionName 통과 가능성을 배제. 전체119 클립 최신 외형 재검사와 전체장비 접점 검사는 다음 슬롯.
- 사용자설정/저장root복원, editor_stop 및 공식 status ready 확인 후 THREE-WAVES에 반환. 사용자 슬롯 쓰기 없음.
- 시체 통합 Unity 검사 결과 파일 확인: corpse-drafts/paused-check.txt PASS. 일시정지 복원3종(장수2종 native종단/바닥, 일반Ant즉시묶음), 새사망시작0 유지. THREE-WAVES의 원본3웨이브 실제 화면은 진행 중.
- THREE-WAVES 원본3웨이브 실제 캡처 screens/tower-after.png 직접 확인: 서있던 다수 일반시체가 묶음으로 표시. corpse-result.json 공식 run_script success 확인. Graphify 최종갱신은 REVIEW 세션이 담당해 중복실행 안 함.

## 2026-11-13 00:05 KST 후속 슬롯 반환
- 목뒤1143면을 Body로 재분류하고 원본 가중치/피부재질 보존. 두리그 Body22954/Hair37960/Face4741, 총삼각형수 유지. 26메시검사/12머리조합/장신구3교체 PASS. nape-review52장 촬영, 대표10여장(6머리/2리그/후측면) 검토. 큰 머리색 목판 개선, 피부/옷 경계 품질 및 완전눈감기는 전체합격아님. 재현생성기/UV목록 art/native-appearance-1112 갱신.
- 실제원본119클립×3위상 비교 PASS119/REVIEW0. torso/limb localRotation 원본오차0도, 각클립 관절움직임>.1도. 손/손가락은 파지보정을 허용하므로 제외. source-fidelity.txt. 모든프레임/표면무관통 증거아님.
- 장비52매핑항목 파지 PASS, 별도지원하지않는 모델37항목은 LEGACY 표시 유지. 장신구22조건/14무기행동/도구교체lifecycle/활122자세(머리-화살선최소.229m) PASS. 10무기 실재생검사 최대보조손오차.0000433m, 팔길이변화.0000226m 미만. equipment-review30장 중 10무기 중간캡처 직접확인. 승인된곡괭이/도끼/활/방패방향 변경없음.
- 무기API검사는 소집상태로 수정해 여가 IdleRest가 전투자세를 덮는 fixture오해 방지. 실제무기14행동은 expected MotionName/HeldTool/renderer 확인, 모든종류의 전투판정 신규검증 아님.
- RestoreSettings/root→editor_stop→status ready 확인, THREE-WAVES에00:05반환. 사용자저장 쓰기 없음. Assets 동결. 이번 슬롯은 SourceParts만변경. 침대눕기·눈완전감기·정밀도구-자원접점은 여전히남음.
