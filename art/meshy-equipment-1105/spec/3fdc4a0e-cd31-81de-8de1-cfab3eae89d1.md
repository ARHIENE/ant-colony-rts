Here is the result of "fetch" for the Page with URL https://app.notion.com/p/3fdc4a0ecd3181de8de1cfab3eae89d1 as of 2026-10-19T14:21:26.661Z:
<page url="https://app.notion.com/p/3fdc4a0ecd3181de8de1cfab3eae89d1" icon="🛡️">
<ancestor-path>
<parent-page url="https://app.notion.com/p/3fbc4a0ecd3181699e2cc0c008aece39" title="정착지 오브젝트 도감"/>
<ancestor-2-page url="https://app.notion.com/p/334c4a0ecd3180668dbdc7d6d1aed848" title="게임 개발 노션 정리"/>
</ancestor-path>
<properties>
{"title":"장비"}
</properties>
<iconMetadata>{"type":"emoji","emoji":"🛡️"}</iconMetadata>
<content>
<mention url="https://app.notion.com/p/3fbc4a0ecd3181699e2cc0c008aece39">정착지 오브젝트 도감</mention>

공방에서 만드는 장비 10종(무기 4·갑옷 2·장신구 4).

<date start="2026-10-19" /> 현재 목표 기획: 귀·꼬리가 장비 틈으로 보이는 사람형 캐릭터, 내구도·고정 담당 수리/수동 변경·수리 후 자동 재장착, 금속 성장 단계와 다종 자원·식품·생산 규칙을 적용한다. 기존 모델·검사·제작비는 당시 구현 기록이며 새 기획 적용 완료가 아니다. 최신 상세: <mention url="https://app.notion.com/p/3d0c4a0ecd31812d80fbeceabb70193e" /> · <mention url="https://app.notion.com/p/3d8c4a0ecd318155a477fda677e21fd1" />.

공방에서 만드는 장비 10종(<code>EquipmentRecipes</code> 순서). 사진은 수동 조합 캐릭터가 착용·장착한 Blender 렌더, GIF는 장비 착용 미리보기. 모델은 resident-kit 소켓·부품 규격. Unity 착용 표시 연결은 미구현. <b>2026-10-18 오후 캐릭터가 토끼 주민 조합 키트로 바뀌었지만 이 체형용 장비 부품은 아직 없어, 이 페이지 사진·GIF는 이전 주민(resident-kit) 버전이다.</b>

| 칸 | 장비 |
| --- | --- |
| 무기 | 근접 무기 · 산 분사기 · 방패 · 지휘 신호기 |
| 갑옷 | 보호 갑옷 · 비행 장비 |
| 장신구 | 가벼운 발목 띠 · 감각 보조 장신구 · 향기 주머니 · 지휘 표식 |

<table-of-contents />

## 근접 무기

<image source="notion-file-block://83e1625d-4403-44c9-bd9e-e0eb051e8aca/4e5aecfd-21c2-45c8-af76-6c2490b3167a?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Mandible.png">
근접 무기
</image>

<b>칸·효과:</b> 무기 — 근접 전투용 창. 공격 동작 Attack에서 찌른다.

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 0 / 재료 40 / Special 5, 제작 보조 능력 근력. 품질 4단계(조잡\~걸작).

<image source="notion-file-block://aeb563dd-0f97-4128-9c8c-91543ac7b671/21da76e5-60f1-434d-ab2b-3f7bc99de4ef?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Motion__Attack.gif">
공격 미리보기 Motion\_\_Attack
</image>

<toggle>
제작 정보

키트 모델: Prop\_Spear · 부착: 오른손 Socket.Tool.R

파일: art/resident-kit/exports/tools/Prop\_Spear.fbx

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>

## 산 분사기

<image source="notion-file-block://644a4bf6-54aa-4fe3-b617-193d3fa404fd/c905f619-c87d-478c-9272-7eff3689bc81?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=AcidSprayer.png">
산 분사기
</image>

<b>칸·효과:</b> 무기 — 원거리 전투용 분사기.

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 0 / 재료 30 / Special 10, 제작 보조 능력 연구. 품질 4단계(조잡\~걸작).

<image source="notion-file-block://d7903476-b2bd-44f8-8ef0-a7162f2aacf3/b16149bf-a539-40a4-be2d-9487b452884d?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Equip__Ranged.gif">
원거리 세트 미리보기(산 분사기·보호 갑옷·감각 보조 장신구·발목 띠)
</image>

<toggle>
제작 정보

키트 모델: Equip\_AcidSprayer · 부착: 오른손 Socket.Tool.R(노즐 정면)

파일: art/resident-kit/exports/tools/Equip\_AcidSprayer.fbx

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>

## 방패

<image source="notion-file-block://7bed4d53-b2d7-43eb-875e-fb7438a9af01/a8b24e15-9d37-479f-982d-f19370593e3a?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Shield.png">
방패
</image>

<b>칸·효과:</b> 무기 — 방어형 무기 칸 장비.

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 0 / 재료 60 / Special 5, 제작 보조 능력 근력. 품질 4단계(조잡\~걸작).

<image source="notion-file-block://8f289f6f-3347-40bf-b7c0-ae0003e399fb/f40ffe13-9aeb-46cd-b3a9-e26db7603083?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Equip__Defense.gif">
방어 세트 미리보기(방패·보호 갑옷)
</image>

<toggle>
제작 정보

키트 모델: Prop\_Shield · 부착: 왼손 Socket.Tool.L

파일: art/resident-kit/exports/tools/Prop\_Shield.fbx

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>

## 지휘 신호기

<image source="notion-file-block://3ada027f-59a5-4639-9eb9-5d83924a3b53/e7278961-6b58-4e32-9884-fdfb77de1a26?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Pheromone.png">
지휘 신호기
</image>

<b>칸·효과:</b> 무기 — 지원형 지휘 장비. 무기 품질만큼 지휘 범위가 넓어진다.

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 20 / 재료 0 / Special 10, 제작 보조 능력 연구. 품질 4단계(조잡\~걸작).

<image source="notion-file-block://e3563eb3-496d-49ef-a4fc-430347178d90/98eaab21-5cce-4601-9c5b-ce8f3994f533?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Equip__Support.gif">
지원 세트 미리보기(지휘 신호기·지휘 표식·향기 주머니)
</image>

<toggle>
제작 정보

키트 모델: Equip\_Signaller · 부착: 오른손 Socket.Tool.R(깃발)

파일: art/resident-kit/exports/tools/Equip\_Signaller.fbx

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>

## 보호 갑옷

<image source="notion-file-block://46e366df-0869-49db-9e96-9c3248ebecaa/12aa428f-dfda-4c84-af6a-551b8e83ef8a?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Coating.png">
보호 갑옷
</image>

<b>칸·효과:</b> 갑옷 — 갑옷 칸 장비.

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 0 / 재료 40 / Special 5, 제작 보조 능력 근력. 품질 4단계(조잡\~걸작).

<b>착용 미리보기:</b> 위 방패 항목의 방어 세트 GIF.

<toggle>
제작 정보

키트 모델: Equip\_Armor · 부착: 몸통 부품(가중치 Body)

파일: art/resident-kit/exports/character/Resident\_Parts.fbx(부품)

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>

## 비행 장비

<image source="notion-file-block://8465c07a-9823-4eb4-86e1-fc119cbcbfe8/000c6b6a-a5f7-4b15-8d57-5d8a4b580959?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Wings.png">
비행 장비
</image>

<b>칸·효과:</b> 갑옷 — 비행 이동. 해당 부위 부상 시 착용·비행 불가. 비행 동작 Fly.

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 30 / 재료 0 / Special 15, 제작 보조 능력 연구. 품질 4단계(조잡\~걸작).

<image source="notion-file-block://b8aee714-885a-4b15-99f2-74493dbaa8fe/25aa0cee-f610-4f50-94a3-251d6152f0ab?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Motion__Fly.gif">
비행 미리보기 Motion\_\_Fly
</image>

<toggle>
제작 정보

키트 모델: Equip\_FlightGear · 부착: 등 부품(가중치 Body)

파일: art/resident-kit/exports/character/Resident\_Parts.fbx(부품)

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>

## 가벼운 발목 띠

<image source="notion-file-block://923e8fab-5481-414f-8c27-801c582701b1/84ff23dc-397c-45f9-bc8b-6ab3c860f3fd?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Anklet.png">
가벼운 발목 띠
</image>

<b>칸·효과:</b> 장신구 — 이동 속도 +5\~20%(품질별).

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 20 / 재료 20 / Special 0, 제작 보조 능력 예술. 품질 4단계(조잡\~걸작).

<b>착용 미리보기:</b> 위 산 분사기 항목의 원거리 세트 GIF.

<toggle>
제작 정보

키트 모델: Trinket\_Anklet · 부착: 발목 부품(가중치 Foot)

파일: art/resident-kit/exports/character/Resident\_Parts.fbx(부품)

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현. 발목 띠는 작아 사진에서 잘 보이지 않음.
</toggle>

## 감각 보조 장신구

<image source="notion-file-block://3fb91e01-cd84-4656-8142-042689a86826/2d5b19a4-1dec-404d-ba60-40bf16e84c78?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Antenna.png">
감각 보조 장신구
</image>

<b>칸·효과:</b> 장신구 — 채집 +8\~30%(품질별).

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 20 / 재료 20 / Special 0, 제작 보조 능력 예술. 품질 4단계(조잡\~걸작).

<b>착용 미리보기:</b> 위 산 분사기 항목의 원거리 세트 GIF.

<toggle>
제작 정보

키트 모델: Trinket\_SensorGoggles · 부착: 머리 부품(고글, 가중치 Head)

파일: art/resident-kit/exports/character/Resident\_Parts.fbx(부품)

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>

## 향기 주머니

<image source="notion-file-block://0a6c90e5-3a03-428d-a1ed-7e52bdd32b1f/386513b3-61c8-475c-8b05-1900f27de568?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Scent.png">
향기 주머니
</image>

<b>칸·효과:</b> 장신구 — 기분 +3\~10(품질별).

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 30 / 재료 0 / Special 5, 제작 보조 능력 예술. 품질 4단계(조잡\~걸작).

<b>착용 미리보기:</b> 위 지휘 신호기 항목의 지원 세트 GIF.

<toggle>
제작 정보

키트 모델: Trinket\_ScentPouch · 부착: 허리 부품(가중치 Body)

파일: art/resident-kit/exports/character/Resident\_Parts.fbx(부품)

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>

## 지휘 표식

<image source="notion-file-block://23515acd-8869-4fb2-8081-a8eac5e4178c/c2305bdb-17b9-4c91-9339-b19a21f1f967?space_id=452d9f9b-5258-4c65-97c1-4271c45e3be2&name=Insignia.png">
지휘 표식
</image>

<b>칸·효과:</b> 장신구 — 지휘 +3\~10(품질별).

<b>기존 구현 제작비(새 기획 적용 전):</b> 공방, Food 0 / 재료 30 / Special 10, 제작 보조 능력 예술. 품질 4단계(조잡\~걸작).

<b>착용 미리보기:</b> 위 지휘 신호기 항목의 지원 세트 GIF.

<toggle>
제작 정보

키트 모델: Trinket\_Insignia · 부착: 가슴 부품(가중치 Body)

파일: art/resident-kit/exports/character/Resident\_Parts.fbx(부품)

모델 완료 / Blender 착용 미리보기 완료 / Unity 착용 표시 미구현.
</toggle>
</content>
</page>
