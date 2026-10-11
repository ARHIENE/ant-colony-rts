'use strict';
// HUD v5 인터랙티브 시안(2026-10-11). 모든 수치·이름·결과는 시안 예시 데이터이며 Unity 게임과 연결되지 않는다.
// 기준: Notion 최신 기획(메인 게임 화면 구체화 · 자원 시스템 목장 확정안 · 장수·유닛 · 월드맵·외교). 미정 항목은 화면에 '미정'으로 표시한다.
// 구성: 1) 도구·아이콘  2) 예시 데이터  3) 상태  4) 상단·지도·콘솔 HUD  5) 명령 칸  6) 창(작업표·장수·인구·건설·생산·외교·목장)  7) 시간 흐름  8) 이벤트 연결

/* ───────── 1) 도구·아이콘 ───────── */
const $ = (s, r = document) => r.querySelector(s);
const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const fmt = n => Math.round(n).toLocaleString('ko-KR');
const pct = v => Math.round(v * 100) + '%';
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const mmss = s => `${Math.floor(s / 60)}:${String(Math.floor(s % 60)).padStart(2, '0')}`;
const P = {
  flag: '<path d="M5 21V4h11l-2 4 2 4H5"/>', bed: '<path d="M3 18V8M3 14h18v4M21 14v-3a3 3 0 0 0-3-3h-7v6"/>',
  hammer: '<path d="M14 4l6 6-3 3-6-6zM11 7l-8 8 3 3 8-8"/>', grid: '<path d="M4 4h16v16H4zM4 10h16M4 15h16M10 4v16M15 4v16"/>',
  info: '<circle cx="12" cy="12" r="9"/><path d="M12 11v6M12 7.5h.01"/>', swap: '<path d="M4 8h13l-3-3M20 16H7l3 3"/>',
  flask: '<path d="M9 3h6M10 3v6l-5 9a2 2 0 0 0 2 3h10a2 2 0 0 0 2-3l-5-9V3"/>', stop: '<rect x="6" y="6" width="12" height="12" rx="1"/>',
  users: '<circle cx="9" cy="8" r="3"/><path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6M16 11h5M18.5 8.5v5"/>', house: '<path d="M4 11l8-7 8 7v9H4zM10 20v-5h4v5"/>',
  hand: '<path d="M7 12l3-3 4 4 3-3M3 12l4-4 3 3M21 12l-4-4"/><path d="M7 12l5 5 5-5"/>', target: '<circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="3"/>',
  level: '<path d="M5 19h3v-5H5zM10.5 19h3V9h-3zM16 19h3V5h-3z"/>', siren: '<path d="M6 18v-6a6 6 0 0 1 12 0v6M4 18h16M12 3v2M4.5 6.5l1.4 1.4M19.5 6.5l-1.4 1.4"/>',
  sword: '<path d="M5 19L19 5M14 5h5v5M5 9h4M7 7v4"/>', wing: '<path d="M3 15c5-1 8-4 9-10 1 6 4 9 9 10-4 1-7 3-9 5-2-2-5-4-9-5z"/>',
  bolt: '<path d="M13 3L5 14h6l-1 7 8-11h-6z"/>', back: '<path d="M9 14L4 9l5-5M4 9h11a5 5 0 0 1 0 10h-3"/>', x: '<path d="M6 6l12 12M18 6L6 18"/>',
  trash: '<path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/>', move: '<path d="M12 3v18M3 12h18M12 3l-3 3M12 3l3 3M12 21l-3-3M12 21l3-3M3 12l3-3M3 12l3 3M21 12l-3-3M21 12l-3 3"/>',
  layers: '<path d="M12 3l9 5-9 5-9-5zM3 13l9 5 9-5"/>', paw: '<circle cx="8" cy="7" r="2"/><circle cx="16" cy="7" r="2"/><circle cx="5" cy="13" r="2"/><circle cx="19" cy="13" r="2"/><path d="M12 12c3 0 5 3 5 5.5S15 20 12 20s-5-.5-5-2.5S9 12 12 12z"/>',
  cart: '<path d="M3 5h3l2 10h10l2-7H7M9 20h.01M17 20h.01"/>', knife: '<path d="M4 20L16 8l3 3L7 21zM16 8l2-5 3 3-5 2"/>',
  cross: '<path d="M10 4h4v6h6v4h-6v6h-4v-6H4v-4h6z"/>', heart: '<path d="M12 20s-7-4.5-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.5-7 10-7 10z"/>',
  door: '<path d="M5 21V4h9v17M14 12h6M17 9l3 3-3 3M10 12h.01"/>', pen: '<path d="M4 20h4L19 9l-4-4L4 16zM14 6l4 4"/>',
  rope: '<path d="M6 4c6 3 6 13 0 16M18 4c-6 3-6 13 0 16M8 8h8M8 16h8"/>', pause: '<path d="M9 5v14M15 5v14"/>',
  warn: '<path d="M12 4l9 16H3z"/><path d="M12 10v4M12 17h.01"/>', mood: '<circle cx="12" cy="12" r="9"/><path d="M8.5 16c1-1.2 2.2-1.8 3.5-1.8s2.5.6 3.5 1.8M9 9.5h.01M15 9.5h.01"/>',
  check: '<path d="M5 12.5l4.5 4.5L19 7.5"/>', food: '<path d="M12 3c4 4 6 7 6 10a6 6 0 0 1-12 0c0-3 2-6 6-10z"/>',
  chev: '<path d="M6 9l6 6 6-6"/>', left: '<path d="M15 6l-6 6 6 6"/>', right: '<path d="M9 6l6 6-6 6"/>', lock: '<rect x="5" y="11" width="14" height="9" rx="1"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>',
  flame: '<path d="M12 3c1 4 5 5 5 10a5 5 0 0 1-10 0c0-3 2-4 2-6 1 1 2 2 3 2 0-2-1-4 0-6z"/>', chat: '<path d="M4 5h16v11H9l-5 4z"/>',
  ant: '<ellipse cx="12" cy="7" rx="3" ry="3"/><ellipse cx="12" cy="13" rx="2.5" ry="2.5"/><ellipse cx="12" cy="19" rx="3.5" ry="3"/><path d="M9 5L6 2M15 5l3-3M9.5 13H5M14.5 13H19"/>',
  pin: '<path d="M12 21s-6-5.5-6-11a6 6 0 0 1 12 0c0 5.5-6 11-6 11z"/><circle cx="12" cy="10" r="2"/>', clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  list: '<path d="M8 6h12M8 12h12M8 18h12M4 6h.01M4 12h.01M4 18h.01"/>', scroll: '<path d="M6 4h11a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6M9 9h6M9 13h6"/>',
  globe: '<circle cx="12" cy="12" r="8"/><path d="M4 12h16M12 4c3 3 3 13 0 16M12 4c-3 3-3 13 0 16"/>', menu: '<path d="M4 7h16M4 12h16M4 17h16"/>',
};
const ic = (k, s = 18) => `<svg width="${s}" height="${s}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${P[k] || ''}</svg>`;
const antFace = c => `<svg class="pf" viewBox="0 0 24 24" aria-hidden="true"><path d="M8 6L5 1.5M16 6l3-4.5" stroke="${c}" stroke-width="1.4" stroke-linecap="round" fill="none"/><ellipse cx="12" cy="12" rx="6.5" ry="6" fill="${c}" opacity=".9"/><ellipse cx="9.3" cy="10.8" rx="1.3" ry="1.6" fill="#120f0b"/><ellipse cx="14.7" cy="10.8" rx="1.3" ry="1.6" fill="#120f0b"/></svg>`;

/* ───────── 2) 예시 데이터 ───────── */
const SKILLS = [['gather', '채집'], ['build', '건설'], ['farm', '농사'], ['fish', '낚시'], ['craft', '제작'], ['research', '연구'], ['melee', '근접'], ['ranged', '원거리'],
  ['command', '지휘'], ['medicine', '의료'], ['cook', '요리'], ['strength', '근력'], ['art', '예술'], ['politics', '정치'], ['husbandry', '사육']];
const SKILL_NAME = Object.fromEntries(SKILLS);
// 작업표 15종(Unity 작업표 순서: 같은 우선순위면 왼쪽부터). 간호 = 기획의 의료 작업(투약·수술·약제대 생산).
const JOBS = [['nursing', '간호\n(의료)', 'medicine'], ['repair', '수리', 'build'], ['clean', '치우기', null], ['build', '건설', 'build'], ['art', '예술', 'art'], ['craft', '제작', 'craft'],
  ['research', '연구', 'research'], ['admin', '행정', 'politics'], ['cook', '요리', 'cook'], ['hunt', '사냥', 'melee'], ['haul', '운반', 'strength'], ['husbandry', '사육', 'husbandry'],
  ['farm', '농사', 'farm'], ['fish', '낚시', 'fish'], ['gather', '채집', 'gather']];
const PRIO = ['금지', '매우 낮음', '낮음', '보통', '높음', '매우 높음'];
const sk = o => Object.fromEntries(SKILLS.map(([k]) => [k, o[k] ?? 3]));
const C = [
  { id: 'gw', name: '관우', sex: '♂', color: '#6cc46a', weapon: '큰턱 날', job: '조약돌 채집', jobKey: 'gather', hp: 88, mood: 58, need: [40, 25, 30, 55], x: 1120, y: 318,
    skills: sk({ gather: 14, melee: 12, command: 9, strength: 8, build: 6 }), passion: { gather: 2, melee: 1 }, traits: ['용맹', '두더지 큰턱', '코골이'], locked: [], attack: 18, defense: 9,
    injuries: [], equip: [['무기', '큰턱 날 · 정교'], ['갑옷', '외골격 코팅 · 보통'], ['장신구', null]], rel: [['장비', '친구 52'], ['조운', '라이벌 −44']], moodWhy: [['좋은 식사', 5], ['숙소에서 잠', 3], ['노숙한 동료', -2]] },
  { id: 'jb', name: '장비', sex: '♂', color: '#c9634a', weapon: '방패 갑각', job: '노숙 중 · 숙소 침대 부족', jobKey: 'build', hp: 71, mood: 18, need: [62, 80, 15, 40], x: 455, y: 262,
    skills: sk({ build: 11, melee: 13, strength: 12 }), passion: { melee: 2 }, traits: ['무모', '대식'], locked: ['art'], attack: 16, defense: 14, injuries: [['다리', '경상']],
    equip: [['무기', '방패 갑각 · 보통'], ['갑옷', null], ['장신구', null]], rel: [['관우', '친구 52']], moodWhy: [['노숙', -15], ['3일 연속 노숙', -5], ['배고픔', -6]] },
  { id: 'yb', name: '유비', sex: '♂', color: '#d9b45a', weapon: '페로몬 신호기', job: '연구 · 균류 농법', jobKey: 'research', hp: 100, mood: 72, need: [30, 45, 50, 70], x: 420, y: 390,
    skills: sk({ research: 15, politics: 13, command: 11 }), passion: { research: 2, politics: 1 }, traits: ['사교적', '빨리 배움'], locked: [], attack: 9, defense: 8, injuries: [],
    equip: [['무기', '페로몬 신호기 · 보통'], ['갑옷', null], ['장신구', '지휘 표식 · 정교']], rel: [['관우', '친구 61'], ['장비', '친구 58']], moodWhy: [['열정 작업', 6], ['좋은 식사', 5]] },
  { id: 'jg', name: '제갈량', sex: '♂', color: '#86a9e6', weapon: '산 분사기', job: '요리 · 좋은 식사', jobKey: 'cook', hp: 94, mood: 80, need: [20, 30, 45, 80], x: 840, y: 214,
    skills: sk({ cook: 12, research: 16, medicine: 9, ranged: 10 }), passion: { research: 1, cook: 1 }, traits: ['천재', '야행성'], locked: [], attack: 12, defense: 6, injuries: [],
    equip: [['무기', '산 분사기 · 걸작'], ['갑옷', null], ['장신구', null]], rel: [['유비', '친구 70']], moodWhy: [['고급 식사', 10]] },
  { id: 'jw', name: '조운', sex: '♂', color: '#d8d0c0', weapon: '큰턱 날', job: '출전 · 서쪽 성벽 방어', jobKey: null, hp: 92, mood: 65, need: [35, 50, 40, 60], x: 470, y: 520, deployed: true, troops: 16, limit: 20,
    skills: sk({ melee: 16, command: 12, strength: 10 }), passion: { melee: 2, command: 1 }, traits: ['용맹', '날렵'], locked: [], attack: 22, defense: 10, injuries: [], skillCd: 0, wings: true,
    equip: [['무기', '큰턱 날 · 정교'], ['갑옷', '비행 날개 · 보통'], ['장신구', null]], rel: [['관우', '라이벌 −44']], moodWhy: [['전투 참여', 0]] },
  { id: 'hz', name: '황충', sex: '♂', color: '#b98a52', weapon: '산 분사기', job: '원정 중 · 동쪽 숲', jobKey: null, hp: 77, mood: 61, need: [40, 40, 40, 40], away: true,
    skills: sk({ ranged: 15, medicine: 7 }), passion: { ranged: 2 }, traits: ['노망'], locked: [], attack: 15, defense: 7, injuries: [], equip: [['무기', '산 분사기 · 정교'], ['갑옷', null], ['장신구', null]], rel: [], moodWhy: [['원정 중', 0]] },
  { id: 'ns', name: '노숙', sex: '♀', color: '#a8a07a', weapon: '맨 큰턱', job: '간호 · 동물 투약', jobKey: 'nursing', hp: 85, mood: 49, need: [55, 60, 35, 50], x: 760, y: 470,
    skills: sk({ medicine: 14, research: 8, husbandry: 6 }), passion: { medicine: 2 }, traits: ['간병인', '결벽'], locked: [], attack: 5, defense: 5, injuries: [],
    equip: [['무기', null], ['갑옷', null], ['장신구', '향기 주머니 · 보통']], rel: [], moodWhy: [['결벽: 사체 근처', -3], ['열정 작업', 6]] },
  { id: 'gn', name: '감녕', sex: '♀', color: '#5fb2b8', weapon: '큰턱 날', job: '사육 · 우리 돌봄', jobKey: 'husbandry', hp: 90, mood: 66, need: [30, 40, 50, 60], x: 980, y: 520,
    skills: sk({ husbandry: 13, melee: 11, strength: 9 }), passion: { husbandry: 1 }, traits: ['진딧물 목동'], locked: ['research'], attack: 14, defense: 8, injuries: [],
    equip: [['무기', '큰턱 날 · 보통'], ['갑옷', null], ['장신구', null]], rel: [], moodWhy: [['열정 작업', 3]] },
  { id: 'gp', name: '관평', sex: '♂', color: '#8fd08a', weapon: '—', job: '어린 장수 · 배우는 중', jobKey: null, hp: 100, mood: 70, need: [30, 30, 30, 30], young: true, x: 560, y: 160,
    skills: sk({}), passion: {}, traits: [], locked: JOBS.map(j => j[0]), attack: 0, defense: 0, injuries: [], equip: [['무기', null], ['갑옷', null], ['장신구', null]], rel: [['관우', '부모 +40']], moodWhy: [] },
];
const prioOf = c => Object.fromEntries(JOBS.map(([k], i) => [k, c.young ? 0 : c.locked.includes(k) ? 0 : k === c.jobKey ? 5 : (i * 7 + c.name.length) % 4 ? 3 : 0]));
C.forEach(c => c.prio = prioOf(c));
const byId = id => C.find(c => c.id === id);

const MATERIALS = [ // 시안 예시: 재료 목록·수치는 기획 미정
  { k: 'leaf', n: '나뭇잎', g: '원재료', have: 80 }, { k: 'bark', n: '나무껍질', g: '원재료', have: 34 }, { k: 'pebble', n: '조약돌', g: '원재료', have: 40 },
  { k: 'clay', n: '점토', g: '원재료', have: 22 }, { k: 'fiber', n: '식물 섬유', g: '원재료', have: 18 }, { k: 'web', n: '거미줄', g: '원재료', have: 7 },
  { k: 'chitin', n: '갑각', g: '원재료', have: 4 }, { k: 'plank', n: '판재', g: '가공품', have: 12 }, { k: 'block', n: '석재 블록', g: '가공품', have: 9 },
  { k: 'cloth', n: '직물', g: '가공품', have: 3 }, { k: 'honeydew', n: '감로', g: '목장 생산물', have: 6 }, { k: 'medicine', n: '동물용 의약품', g: '의료', have: 1 },
  { k: 'kit', n: '동물 수술 키트', g: '의료', have: 0 }, { k: 'special', n: '특수(병뚜껑·단추)', g: '특수', have: 12 },
];
const mat = k => MATERIALS.find(m => m.k === k);
const STRUCT = { // 재료 비교(시안 예시 수치): 내구도·미관·단열·가연성
  bark: { dur: 120, beauty: 0, insul: 1, flam: true }, pebble: { dur: 260, beauty: 1, insul: 2, flam: false }, clay: { dur: 180, beauty: 0, insul: 3, flam: false },
  plank: { dur: 200, beauty: 2, insul: 2, flam: true }, block: { dur: 420, beauty: 2, insul: 3, flam: false }, leaf: { dur: 80, beauty: -1, insul: 0, flam: true },
};
const BUILD = [
  ['타일', [['벽', 20, 1], ['문', 15, 1], ['바닥', 4, 1], ['기둥', 10, 1]]], ['생활', [['자리', 6, 1], ['침대', 18, 1], ['큰침대', 30, 1], ['식탁', 16, 1], ['화장실', 14, 1]]],
  ['저장', [['수납장', 20, 1], ['저장고', 40, 1]]], ['환경', [['화롯불', 10, 0], ['등불', 8, 0], ['난로', 30, 1]]], ['전력', [['쳇바퀴', 30, 1], ['전선', 2, 0], ['전등', 12, 0]]],
  ['자동화', null], ['배관', null], ['식량', [['밭', 10, 0], ['조리대', 24, 1], ['먹이통', 16, 1], ['우리 표지', 6, 0], ['돌봄대', 18, 1]]],
  ['작업', [['목공대', 30, 1], ['석공대', 30, 1], ['물레', 24, 1], ['무기 공방', 40, 1], ['연구대', 30, 1], ['행정 책상', 30, 1]]],
  ['의료', [['병상', 24, 1], ['약제대', 28, 1], ['동물 치료대', 32, 1], ['도축대', 25, 1]]], ['방어', [['성벽', 80, 1], ['성문', 60, 1], ['가시 함정', 20, 0], ['끈끈이 함정', 18, 0]]],
  ['휴게', [['이야기 모닥불', 15, 0], ['놀이판', 20, 1], ['목욕통', 26, 1]]], ['장식', [['화분', 10, 0], ['조각상', 30, 1], ['깃발', 8, 0]]],
  ['군사', [['징집소', 40, 1], ['훈련대', 25, 1], ['무기고', 30, 1]]], ['마을', [['초가집', 30, 0], ['흙집', 80, 0], ['큰 아파트', 200, 0], ['행정 책상', 30, 1]]], ['이동수단', [['수송 차량', 120, 1]]],
];
// 지도 위 건물(시안 배치). type은 명령 칸과 상세 창을 정한다.
const B = [
  { id: 'dorm', type: 'dorm', name: '숙소', x: 360, y: 120, w: 264, h: 168, mat: 'bark', beds: 4, assigned: 5 },
  { id: 'kitchen', type: 'room', name: '식당·부엌', x: 648, y: 120, w: 240, h: 168, mat: 'plank' },
  { id: 'lab', type: 'room', name: '연구실', x: 360, y: 312, w: 216, h: 144, mat: 'bark' },
  { id: 'post', type: 'post', name: '징집소', x: 220, y: 330, w: 70, h: 60, mat: 'pebble' },
  { id: 'smith', type: 'prod', name: '무기 공방', prod: 'smith', x: 610, y: 312, w: 90, h: 60, mat: 'block' },
  { id: 'carp', type: 'prod', name: '목공대', prod: 'carp', x: 610, y: 386, w: 90, h: 50, mat: 'plank' },
  { id: 'med', type: 'prod', name: '약제대', prod: 'med', x: 712, y: 386, w: 70, h: 50, mat: 'plank' },
  { id: 'pen', type: 'pen', name: '우리(목장)', x: 900, y: 420, w: 220, h: 150, mat: 'pebble' },
  { id: 'feeder', type: 'feeder', name: '먹이통', x: 1080, y: 432, w: 30, h: 22, mat: 'bark' },
  { id: 'butcher', type: 'butcher', name: '도축대', x: 800, y: 520, w: 44, h: 26, mat: 'plank' },
  { id: 'clinic', type: 'clinic', name: '동물 치료대', x: 860, y: 590, w: 48, h: 30, mat: 'plank' },
  { id: 'hut1', type: 'house', name: '초가집', x: 140, y: 150, w: 44, h: 34, mat: 'leaf', residents: 18 },
  { id: 'hut2', type: 'house', name: '초가집', x: 210, y: 150, w: 44, h: 34, mat: 'leaf', residents: 20 },
];
const bld = id => B.find(b => b.id === id);
const SPECIES = { aphid: { n: '진딧물', c: '#9bd06a', temper: '도주형' }, worm: { n: '누에', c: '#efe8d8', temper: '도주형' }, spider: { n: '거미', c: '#3a3030', temper: '공격형' }, bee: { n: '벌', c: '#f2c440', temper: '조건부' } };
// 동물(시안 예시). pen: 'pen' 또는 null(야생). clinic: 치료 상태.
const A = [
  { id: 3, sp: 'aphid', custom: '꿀이', stage: '성체', tame: 1, hp: 100, hunger: 72, x: 940, y: 470 },
  { id: 5, sp: 'aphid', custom: '', stage: '성체', tame: 1, hp: 64, hunger: 58, x: 980, y: 500, injury: '경상' },
  { id: 7, sp: 'aphid', custom: '', stage: '새끼', tame: 0, tameProg: .35, hp: 100, hunger: 40, x: 1010, y: 460 },
  { id: 8, sp: 'aphid', custom: '', stage: '노령', tame: 1, hp: 90, hunger: 66, x: 1050, y: 520 },
  { id: 11, sp: 'worm', custom: '', stage: '성체', tame: 0, tameProg: .62, hp: 100, hunger: 80, x: 960, y: 540 },
  { id: 12, sp: 'worm', custom: '', stage: '성체', tame: 1, hp: 30, hunger: 55, x: 884, y: 604, injury: '중상', clinic: { phase: 'wait' } },
  { id: 14, sp: 'spider', custom: '', stage: '성체', tame: 0, tameProg: 0, hp: 100, hunger: 28, x: 1090, y: 480, untameable: true },
  { id: 15, sp: 'bee', custom: '', stage: '성체', tame: 0, tameProg: 0, hp: 100, hunger: 70, x: 1250, y: 300, wild: true, bound: true },
  { id: 16, sp: 'aphid', custom: '', stage: '성체', tame: 0, tameProg: 0, hp: 100, hunger: 85, x: 1300, y: 200, wild: true },
];
// 기본 이름은 종 이름, 바꾼 이름만 유지한다(기획: 코어키퍼 방식). 알림·목록은 '이름(종)' 또는 '종'.
const AGE = { '새끼': 1, '성체': 5, '노령': 14 };
A.forEach(a => { Object.defineProperty(a, 'name', { get() { return this.custom ? `${this.custom}(${SPECIES[this.sp].n})` : SPECIES[this.sp].n; } }); a.age = AGE[a.stage] + (a.id % 3) * .4;
  a.pen = a.wild ? null : 'pen'; a.req = {}; a.seq = 0; a.task = a.clinic ? '수술 대기' : a.wild ? '야생' : a.hunger < 50 ? '먹이 찾는 중' : '배회'; });
const animal = id => A.find(a => a.id === id);
const PENS = [{ n: '우리(목장)', acc: () => S.ranch.accepts }, { n: '우리 B(시안)', acc: () => ({ aphid: true, worm: false, spider: true, bee: true }) }];
const FEEDS = [['leaf', '나뭇잎', 20, 14, true], ['fiber', '식물 섬유', 20, 9, true], ['honeydew', '감로(귀한 먹이)', 0, 0, false], ['grain', '곡식', 20, 6, true]];
const FACTIONS = [
  { id: 'hb', name: '호박 군락', rel: '중립', trust: 42, patience: 4, reaction: '망설임' },
  { id: 'cg', name: '청솔 군락', rel: '우호', trust: 68, patience: 5, reaction: '호의적' },
  { id: 'hk', name: '흑석 군락', rel: '적대', trust: 12, patience: 2, reaction: '냉담', ultimatum: 140 },
  { id: 'jk', name: '적토 군락', rel: '중립', trust: 35, patience: 3, reaction: '망설임', invalid: '포로 교환 대상이던 「적토」 장수 마초가 이미 회유되어 아군이 되었습니다.' },
];
const RECIPES = {
  smith: { name: '무기 공방', job: '제작', items: [['큰턱 날', '재료 40 · 특수 5'], ['산 분사기', '재료 30 · 특수 10'], ['외골격 코팅', '재료 40 · 특수 5']] },
  carp: { name: '목공대', job: '제작', items: [['판재', '나무껍질 2'], ['석재 블록', '조약돌 2']] },
  med: { name: '약제대', job: '간호(의료)', items: [['동물용 의약품', '나뭇잎 2 + 감로 1'], ['동물 수술 키트', '직물 1 + 거미줄 2 + 갑각 1']] },
};

/* ───────── 3) 상태 ───────── */
const S = {
  sel: { kind: 'commander', ids: ['gw'] }, win: null, winArg: null, tab: {}, speed: 2, paused: false, negoPause: null,
  red: false, prioMode: false, prioLevel: 5, yellow: false, targetPrio: { hut1: 3, butcher: 7, rock: 5 }, view: [0, 0, 1440, 660],
  filters: { units: true, res: true, wild: false }, sim: 0, taxIn: 75, clockMin: 190, toasts: [], tid: 0, place: null,
  pop: { total: 420, young: 48, adult: 312, old: 60, deployed: 16, housing: 470, sentiment: 68, demand: 72, rate: 12, focus: '균형', prefer: '조약돌', policy: '징병제', elder: false, admin: .64, comp: 1 },
  orders: { smith: [{ item: 0, mode: 'count', n: 2, prog: .4 }], carp: [{ item: 0, mode: 'keep', n: 20, prog: .1 }], med: [] },
  mode: 'count', amount: 1, ranch: { autoTreat: true, autoHarvest: true, accepts: { aphid: true, worm: true, spider: false, bee: true }, care: { aphid: true, worm: true, spider: false, bee: true }, target: { aphid: 6, worm: 4, spider: 1, bee: 2 } },
  pick: new Set(), feeder: FEEDS.map(f => ({ k: f[0], n: f[1], target: f[2], stock: f[3], allow: f[4] })), feederPrio: 3,
  butcher: { paused: false, allow: { aphid: true, worm: true, spider: true, bee: true }, prog: 0, queue: [{ sp: 'aphid', meat: 12, chitin: 1 }, { sp: 'worm', meat: 8, chitin: 0, baby: true }] },
  flt: { sp: 'all', stage: 'all', tame: 'all', hurt: 'all', work: 'all' }, sort: 'name',
  floorCarcass: [], nego: null, buildCat: 0, buildItem: null, buildMat: 'bark', redev: null, events: { predation: false },
};
const kindOf = () => S.sel.kind;
const selCommanders = () => (S.sel.ids || []).map(byId).filter(Boolean);

/* ───────── 4) 상단·지도·콘솔 HUD ───────── */
function renderMenu() {
  $('#menu').innerHTML = `<button class="btn" data-act="menu" aria-label="메뉴(설정·저장)">${ic('menu', 14)}</button>
    <button class="btn" data-act="open" data-w="duty">작업표</button><button class="btn" data-act="say" data-t="과학 화면은 이번 시안 범위 밖입니다.">과학</button>
    <button class="btn" data-act="open" data-w="diplomacy">월드맵</button><button class="btn" data-act="say" data-t="기록 화면은 이번 시안 범위 밖입니다.">기록</button>`;
}
function renderRoster() {
  const home = C.filter(c => !c.away && !c.young).length;
  $('#roster').innerHTML = `<button class="btn ghost" data-act="scroll" data-d="-1" aria-label="이전 장수들">${ic('left', 12)}</button><div class="rlist" id="rlist">${C.map(c => {
    const sel = (S.sel.ids || []).includes(c.id); const st = c.mood < 25 ? 'danger' : c.mood < 50 ? 'attention' : '';
    const mark = c.deployed ? ic('sword', 10) : c.away ? ic('globe', 10) : c.young ? '' : '';
    return `<button class="btn cm ${sel ? 'sel' : ''} ${c.away ? 'away' : ''} ${st}" data-act="selC" data-id="${c.id}" aria-pressed="${sel}" title="${esc(c.name)} · ${esc(c.job)} · 기분 ${c.mood}${c.young ? ' · 어린 장수(일·출전 불가)' : ''}">${antFace(c.color)}<span>${esc(c.name)}</span><span class="st">${mark}</span>${c.young ? '' : `<span class="md"><i style="width:${c.mood}%;background:${c.mood < 25 ? 'var(--danger)' : c.mood < 50 ? 'var(--warn)' : 'var(--hp)'}"></i></span>`}</button>`;
  }).join('')}</div><button class="btn ghost" data-act="scroll" data-d="1" aria-label="다음 장수들">${ic('right', 12)}</button>
  <button class="btn rcount" data-act="open" data-w="commander" title="장수 관리 · 본거지 ${home} / 전체 ${C.length}(어린 장수 1)"><span class="n">${home}</span>/<span class="n">${C.length}</span></button>`;
}
function weekly() { // 시안 예시 계산: 기획의 잠정식(월 세액 = 납세자 × 세율 × 5 × 생산력 × 징수율, 주간 = 월/4)
  const p = S.pop, payers = p.adult + p.old - p.deployed, prod = 1.12, collect = .85 + .15 * p.admin;
  const month = payers * (p.rate / 100) * 5 * prod * collect, share = { '식량 중심': .8, '균형': .5, '재료 중심': .2 }[p.focus];
  return { food: Math.round(month / 4 * share), mat: Math.round(month / 4 * (1 - share)), payers };
}
function renderRes() {
  const p = S.pop, w = weekly(), col = p.sentiment >= 60 ? 'var(--hp-ink)' : p.sentiment >= 35 ? 'var(--warn-ink)' : 'var(--danger-ink)';
  const matTotal = MATERIALS.filter(m => m.g !== '의료').reduce((s, m) => s + m.have, 0);
  $('#res').innerHTML = `<span class="lab" title="대기 인력과 병역 제도 상한 안의 추가 동원 가능 수">대기 <b class="n">46</b> · 동원 가능 <b class="n">23</b></span><span class="sep"></span>
    <span title="식량 1,240 / 저장 한도 2,000">${ic('food', 15)}</span><span class="val">1,240</span>
    <button class="btn" data-act="pop" aria-expanded="${!$('#pop').hidden}" aria-controls="pop">재료 <span class="val" style="font-size:15px">${matTotal}</span>${ic('chev', 10)}</button><span class="sep"></span>
    <button class="btn pop-btn" data-act="open" data-w="population" title="인구 ${p.total} · 민심 ${p.sentiment}(숫자 색) · 이주 수요 ${p.demand}(막대) · 다음 납세 ${mmss(S.taxIn)}: 식량 +${w.food} · 재료 +${w.mat}">
      <span class="row">${ic('ant', 15)}<span class="val" style="color:${col}">${p.total}</span><span class="tax">다음 납세 <b class="n">${mmss(S.taxIn)}</b> · +${w.food}/+${w.mat}</span></span>
      <span class="demand" aria-label="이주 수요 ${p.demand}"><i style="width:${p.demand}%"></i></span></button>`;
}
function renderPop() {
  const groups = [...new Set(MATERIALS.map(m => m.g))];
  $('#pop').innerHTML = `<b>재료 세부(시안 예시)</b><table>${groups.map(g => `<tr><td colspan="2" class="dim" style="padding-top:6px">${g}</td></tr>` + MATERIALS.filter(m => m.g === g).map(m =>
    `<tr><td>${m.n}</td><td class="n ${m.have < 1 ? 'bad-ink' : ''}">${m.have}${m.k === 'medicine' || m.k === 'kit' ? ` <span class="dim">(예약 ${reserved(m.k)})</span>` : ''}</td></tr>`).join('')).join('')}</table>`;
}
function renderLeft() {
  const red = S.red ? `<div class="mode-chip plate red" role="status">${ic('siren', 15)}<div><b>빨간 경보</b> — 식사·수면·치료받기·간호 중단, 작업 금지는 유지</div><button class="btn danger" data-act="red" style="margin-left:auto">해제</button></div>` : '';
  const prio = S.prioMode ? `<div class="mode-chip plate yellow" role="status">${ic('target', 15)}<div><b>우선 지정 · 단계 ${S.prioLevel === 10 ? '노란 경보' : S.prioLevel}</b><br>지도에서 자원·건물·사체·동물을 클릭해 지정(같은 단계면 해제)</div><button class="btn" data-act="prioMode" style="margin-left:auto">끝내기</button></div>` : '';
  $('#left').innerHTML = `<aside class="goal plate" aria-label="현재 목표">${ic('flag', 16)}<div><b>숙소 침대 1개 추가</b><span>밤 전에 장비의 노숙 해결 · <span class="n">0/1</span></span></div></aside>${red}${prio}
    <div class="preview plate" aria-label="시안 상태 바꾸기"><b>시안 선택 상태</b><div class="row">
      ${[['none', '선택 없음'], ['commander', '장수'], ['multi', '장수 3명'], ['deployed', '출전 장수'], ['building', '건물'], ['animal', '동물']].map(([k, n]) => `<button class="btn ${kindOf() === k ? 'on' : ''}" data-act="preview" data-k="${k}" aria-pressed="${kindOf() === k}">${n}</button>`).join('')}</div>
      <div class="row">${kindOf() === 'building' ? B.filter(b => b.type !== 'room').map(b => `<button class="btn ${S.sel.id === b.id ? 'on' : ''}" data-act="selB" data-id="${b.id}">${b.name}</button>`).join('') : ''}</div>
      <span class="disclaimer">예시 데이터 · Unity와 연결되지 않은 HTML 시안입니다. 지도 위 장수·건물·동물을 눌러도 선택됩니다.</span></div>`;
}
function renderClock() {
  const day = S.clockMin % 15, night = day >= 10, pausedBy = S.negoPause ? '협상 중 시간 정지' : S.paused ? '일시정지' : '';
  $('#clock').innerHTML = `<div class="cal"><div class="cal-row"><b title="날씨: 맑음">3년 가을 · ${night ? '밤' : '낮'} · <span style="color:var(--hp)">숲</span></b><span class="dim">${night ? '낮까지' : '밤까지'} <span class="n">${mmss(((night ? 15 : 10) - day) * 60 % 900)}</span></span></div>
    <div class="dn" title="하루 15분: 낮 10분 · 밤 5분"><span class="d"></span><span class="nt"></span><span class="mark" style="left:${day / 15 * 100}%"></span></div>${pausedBy ? `<span class="paused-note">${pausedBy}</span>` : ''}</div>
    <div class="speed well" role="group" aria-label="게임 속도"><button class="btn" data-act="pause" aria-pressed="${S.paused || !!S.negoPause}" aria-label="일시정지" ${S.negoPause ? 'disabled' : ''}>${ic('pause', 12)}</button>
    ${[1, 2, 3, 5].map(v => `<button class="btn" data-act="speed" data-v="${v}" aria-pressed="${!S.paused && !S.negoPause && S.speed === v}" ${S.negoPause ? 'disabled' : ''}>${v}×</button>`).join('')}</div>`;
}
function toast(t) { t.id = ++S.tid; S.toasts.unshift(t); S.toasts = S.toasts.filter((x, i) => x.kind === 'crisis' || i < 5); renderToasts(); return t; }
function renderToasts() {
  $('#alerts').innerHTML = [...S.toasts].sort((a, b) => (b.pinned ? 1 : 0) - (a.pinned ? 1 : 0)).map(t => `<li class="toast plate ${t.kind}" role="${t.kind === 'crisis' ? 'alert' : 'status'}">
    <span class="ic">${ic(t.icon || 'info', 14)}</span><span><span class="tt">${esc(t.title)}${t.kind === 'crisis' ? '<span class="pin">고정</span>' : ''}</span><span class="dd">${esc(t.text)}</span></span>
    ${t.go ? `<button class="btn go" data-act="toastGo" data-id="${t.id}">${esc(t.goLabel || '위치')} ${ic('pin', 12)}</button>` : '<span></span>'}
    ${t.pinned ? '<span></span>' : `<button class="btn ghost x" data-act="toastX" data-id="${t.id}" aria-label="닫기">${ic('x', 12)}</button>`}</li>`).join('');
}
function feedback(text) { const f = $('#feedback'); f.textContent = text; f.classList.add('show'); clearTimeout(feedback.t); feedback.t = setTimeout(() => f.classList.remove('show'), 2600); }

function renderScene() {
  const sel = S.sel, isSel = (k, id) => sel.kind === k && (sel.id === id) || (k === 'c' && (sel.ids || []).includes(id));
  const prioBadge = (id, x, y) => S.targetPrio[id] ? `<g transform="translate(${x},${y})"><rect x="-1" y="-11" width="${S.targetPrio[id] === 10 ? 28 : 16}" height="14" rx="2" fill="${S.targetPrio[id] === 10 ? '#f5c77a' : '#f2a93b'}"/><text class="prio" x="${S.targetPrio[id] === 10 ? 2 : 4}" y="0">${S.targetPrio[id] === 10 ? '경보' : S.targetPrio[id]}</text></g>` : '';
  const bs = B.map(b => {
    const s = isSel('building', b.id);
    if (b.type === 'room' || b.type === 'dorm') return `<g class="pick" data-act="selB" data-id="${b.id}"><rect x="${b.x}" y="${b.y}" width="${b.w}" height="${b.h}" fill="#6b5a44"/><rect x="${b.x}" y="${b.y}" width="${b.w}" height="${b.h}" fill="none" stroke="${s ? '#8bc8ba' : '#2a221a'}" stroke-width="${s ? 4 : 10}"/>
      <text class="tag" x="${b.x + 14}" y="${b.y + b.h - 22}">${b.name}</text>${b.beds ? `<text class="sub" x="${b.x + 14}" y="${b.y + b.h - 8}">침대 ${b.beds} · 배정 ${b.assigned} → 1명 노숙</text>` : ''}</g>`;
    if (b.type === 'pen') return `<g class="pick" data-act="selB" data-id="pen"><rect x="${b.x}" y="${b.y}" width="${b.w}" height="${b.h}" fill="#5e6e34" opacity=".55"/><rect x="${b.x}" y="${b.y}" width="${b.w}" height="${b.h}" fill="none" stroke="${s ? '#8bc8ba' : '#3a3128'}" stroke-width="${s ? 4 : 8}"/>
      <rect x="${b.x - 4}" y="${b.y + 60}" width="8" height="24" fill="#a8885e"/><text class="tag" x="${b.x + 10}" y="${b.y + 18}">우리 · ${A.filter(a => a.pen === 'pen' && !a.clinic && !a.dead).length}마리</text></g>`;
    const fill = { house: '#8a7458', post: '#7c6a52', prod: '#7c8db5', feeder: '#a39060', butcher: '#9a5a4e', clinic: '#7aa3a0' }[b.type];
    return `<g class="pick" data-act="selB" data-id="${b.id}"><rect x="${b.x}" y="${b.y}" width="${b.w}" height="${b.h}" rx="2" fill="${fill}" stroke="${s ? '#8bc8ba' : '#2a221a'}" stroke-width="${s ? 3 : 2}"/>
      <text class="lbl" x="${b.x}" y="${b.y + b.h + 13}">${b.name}${b.type === 'butcher' ? ` · ${S.butcher.queue.length}구${S.butcher.paused ? ' · 정지' : ''}` : ''}</text>${prioBadge(b.id, b.x + b.w - 8, b.y - 2)}</g>`;
  }).join('');
  const as = A.filter(a => !a.dead && !a.released).map(a => {
    const sp = SPECIES[a.sp], s = isSel('animal', a.id), r = a.stage === '새끼' ? 5 : 7;
    return `<g class="pick" data-act="selA" data-id="${a.id}"><ellipse cx="${a.x}" cy="${a.y}" rx="${r + 3}" ry="${r}" fill="${sp.c}" stroke="#1b1712" stroke-width="1.5"/>${s ? `<ellipse class="ring" cx="${a.x}" cy="${a.y}" rx="${r + 8}" ry="${r + 6}"/>` : ''}
      ${a.req.slaughter ? `<text class="lbl" x="${a.x + 10}" y="${a.y - 6}" style="fill:#ffb3aa">도축 ${a.req.slaughter === 'started' ? '중' : '대기'}</text>` : a.bound ? `<text class="lbl" x="${a.x + 10}" y="${a.y - 6}">결박</text>` : ''}${prioBadge('a' + a.id, a.x + 8, a.y - 10)}</g>`;
  }).join('');
  const cs = C.filter(c => !c.away && c.x).map(c => {
    const s = isSel('c', c.id);
    return `<g class="pick" data-act="selC" data-id="${c.id}"><circle cx="${c.x}" cy="${c.y}" r="11" fill="${c.color}" stroke="${s ? '#8bc8ba' : c.deployed ? '#e8574a' : '#1b1712'}" stroke-width="${s ? 3.5 : 2}"/>
      <text class="lbl" x="${c.x + 16}" y="${c.y + 4}">${c.name} · ${c.deployed ? '출전' : c.young ? '배우는 중' : esc(c.job.split(' ')[0])}</text>
      ${c.deployed ? `<rect x="${c.x + 16}" y="${c.y + 9}" width="70" height="4" fill="rgba(0,0,0,.5)"/><rect x="${c.x + 16}" y="${c.y + 9}" width="${70 * c.troops / c.limit}" height="4" fill="#6cc46a"/>` : ''}</g>`;
  }).join('');
  const prod = S.orders.smith[0] ? `<rect x="612" y="300" width="86" height="4" fill="rgba(0,0,0,.5)"/><rect x="612" y="300" width="${86 * S.orders.smith[0].prog}" height="4" fill="#f2a93b"/>` : '';
  $('#scene').setAttribute('viewBox', S.view.join(' '));
  $('#scene').innerHTML = `<rect width="1440" height="660" fill="#4b4436" data-act="selNone"/><rect width="1440" height="660" fill="#55603f" opacity=".35" data-act="selNone"/>
    <path d="M330 96H960M330 96V300M330 340V480" fill="none" stroke="#3a3128" stroke-width="16"/><text class="lbl" x="700" y="84">성벽 · 안쪽 = 본거지 영역</text>
    <g class="pick" data-act="prioTarget" data-id="rock"><rect x="1000" y="300" width="70" height="46" rx="16" fill="#7d776c"/><text class="lbl" x="996" y="372">바위 → 조약돌</text>${prioBadge('rock', 1062, 300)}</g>
    <circle cx="960" cy="200" r="30" fill="#4f7a3c"/><text class="lbl" x="920" y="252">풀·나무</text>
    ${S.floorCarcass.map(k => `<g><rect x="${k.x}" y="${k.y}" width="16" height="9" rx="3" fill="#8a3a32"/><text class="lbl" x="${k.x + 20}" y="${k.y + 8}">${SPECIES[k.sp].n} 사체</text></g>`).join('')}
    ${bs}${prod}${as}${cs}
    <g><circle cx="250" cy="500" r="12" fill="#e8574a"/><circle cx="278" cy="520" r="12" fill="#e8574a"/><circle cx="232" cy="528" r="12" fill="#e8574a"/><text class="lbl" x="200" y="560">침입자 · 성벽 공격 중</text></g>`;
}
function renderMini() {
  const dot = (x, y, c, r = 2.4) => `<rect x="${x / 1440 * 100 - r / 2}" y="${y / 660 * 100 - r / 2}" width="${r}" height="${r}" fill="${c}"/>`;
  $('#mini').innerHTML = `<svg viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true"><rect width="100" height="100" fill="#2a251c"/><path d="M8 10Q50 2 92 12L95 88Q50 98 6 90z" fill="#3d3628"/>
    ${S.filters.units ? C.filter(c => c.x && !c.away).map(c => dot(c.x, c.y, '#6cc46a')).join('') + B.map(b => dot(b.x + b.w / 2, b.y + b.h / 2, '#a39880', 3)).join('') + dot(250, 510, '#e8574a', 4) : ''}
    ${S.filters.res ? dot(1035, 323, '#c49a74') + dot(960, 200, '#e5bd6b') : ''}${S.filters.wild ? A.filter(a => a.wild).map(a => dot(a.x, a.y, a.sp === 'spider' ? '#e8574a' : '#b9a2f2')).join('') : ''}
    <rect x="${S.view[0] / 14.4}" y="${S.view[1] / 6.6}" width="${S.view[2] / 14.4}" height="${S.view[3] / 6.6}" fill="none" stroke="#efe7da" stroke-width=".8"/></svg>`;
  $('#miniSide').innerHTML = [['units', '유닛', 'users', '장수·건물·적'], ['res', '자원', 'food', '자원 노드'], ['wild', '야생', 'paw', '야생 개체(온순·반격·포식자)']].map(([k, n, i, t]) =>
    `<button class="btn" data-act="filter" data-k="${k}" aria-pressed="${S.filters[k]}" title="${t}">${ic(i, 16)}<span>${n}</span></button>`).join('');
}

function renderCenter() {
  const k = kindOf(), el = $('#center');
  if (k === 'none') {
    const w = weekly();
    el.innerHTML = `<div class="summary"><h2>소굴 현황</h2><p>장수나 건물·동물을 선택하세요. 평시 장수는 작업표에 따라 일합니다. 출전은 <b>징집소 건물</b>을 선택해 ‘출전’으로 편성합니다(명령 칸의 징집소 바로가기는 삭제).</p>
      <div class="facts"><span class="chip">대기 인력 <b class="n">46</b></span><span class="chip">동원 가능 <b class="n">23</b></span><span class="chip warn">숙소 1명 부족</span><span class="chip">다음 납세 <b class="n">${mmss(S.taxIn)}</b> · 식량 +${w.food} · 재료 +${w.mat}</span>
      <span class="chip ${S.red ? 'bad' : ''}">빨간 경보 ${S.red ? '켜짐' : '꺼짐'}</span><span class="chip info">우선 지정 단계 ${S.prioLevel === 10 ? '노란 경보' : S.prioLevel}</span><span class="chip bad">수술 키트 부족</span></div></div>`;
    return;
  }
  if (k === 'commander' || k === 'deployed') {
    const c = selCommanders()[0];
    const top3 = SKILLS.map(([key, n]) => [n, c.skills[key], c.passion[key]]).sort((a, b) => b[1] - a[1]).slice(0, 3);
    el.innerHTML = `<div class="por"><button class="btn por-img well" data-act="open" data-w="commander" data-id="${c.id}" aria-label="${c.name} 상세 열기">${antFace(c.color).replace('class="pf"', 'width="64" height="60"')}<span>${c.name} 상세</span></button>
      <div class="slots">${c.equip.map(([s, v]) => `<button class="btn slot ${v ? '' : 'empty'}" data-act="open" data-w="commander" data-id="${c.id}" data-tab="장비" title="${s}: ${v || '비어 있음'}">${ic(s === '무기' ? 'sword' : s === '갑옷' ? 'layers' : 'heart', 14)}</button>`).join('')}</div></div>
      <div class="info"><div class="idrow"><span class="name">${c.name}</span><span class="role">${c.sex} · ${c.weapon}</span><span class="job" title="${k === 'deployed' ? '출전 중: 직접 조작' : '평시 자율 작업'}">${esc(c.job)}</span></div>
      ${k === 'deployed' ? `<div><div class="hp-h"><span>병력(지휘한도 ${c.limit})</span><span class="n">${c.troops} / ${c.limit}</span></div><div class="hpbar well"><i style="width:${c.troops / c.limit * 100}%"></i></div></div>` : ''}
      <div><div class="hp-h"><span>개인 체력${c.injuries.length ? ` · <span class="bad-ink">${c.injuries.map(i => i.join(' ')).join(', ')}</span>` : ''}</span><span class="n">${c.hp} / 100</span></div><div class="hpbar well"><i style="width:${c.hp}%;background:${c.hp < 40 ? 'var(--danger)' : 'var(--hp)'}"></i></div></div>
      <div class="stats"><div class="stat"><span class="k">기분</span><span class="v n" style="color:${c.mood < 25 ? 'var(--danger-ink)' : c.mood < 50 ? 'var(--warn-ink)' : 'var(--text)'}">${c.mood}</span></div>
      <div class="stat"><span class="k">${k === 'deployed' ? '공격 / 방어' : '출전 시 공격 / 방어'}</span><span class="v n">${c.attack} / ${c.defense}</span></div>
      <div class="stat"><span class="k">주요 기술</span><span class="v" style="font-size:13px;display:flex;gap:6px;flex-wrap:wrap">${top3.map(([n, v, p]) => `<span style="display:inline-flex;align-items:center;gap:2px">${n} <b class="n">${v}</b>${p ? `<span class="flame">${ic('flame', 10)}</span>` : ''}</span>`).join('')}</span></div></div>
      <div class="needs">${['배고픔', '피로', '오락', '위생'].map((n, i) => `<div class="need"><span class="need-h"><span>${n}</span><span class="n">${c.need[i]}</span></span><span class="meter"><i style="transform:scaleX(${c.need[i] / 100});background:${c.need[i] > 70 ? 'var(--warn)' : 'var(--loyal)'}"></i></span></div>`).join('')}</div></div>`;
    return;
  }
  if (k === 'multi') {
    const cs = selCommanders();
    el.innerHTML = `<div class="summary" style="flex:1"><h2>장수 ${cs.length}명 선택</h2><p>공통으로 가능한 명령만 표시합니다. 칸 클릭 = 그 장수만 선택, Shift+클릭 = 선택에서 제외.</p>
      <div class="multi-grid">${cs.map(c => `<button class="btn" data-act="multiPick" data-id="${c.id}" title="${c.name} · ${esc(c.job)}">${antFace(c.color)}<span>${c.name}</span><span class="bar"><i style="width:${c.deployed ? c.troops / c.limit * 100 : c.hp}%"></i></span></button>`).join('')}</div>
      <div class="facts">${cs.map(c => `<span class="chip">${c.name} · ${esc(c.job.split(' · ')[0])}</span>`).join('')}</div></div>`;
    return;
  }
  if (k === 'building') { el.innerHTML = buildingSummary(bld(S.sel.id)); return; }
  if (k === 'animal') { el.innerHTML = animalSummary(animal(S.sel.id)); }
}
function buildingSummary(b) {
  const room = b.type === 'room' || b.type === 'dorm';
  const lines = {
    dorm: `침대 ${b.beds}개 · 배정 장수 ${b.assigned}명 <span class="chip warn">침대 1개 부족 — 장비가 노숙</span>`,
    room: '방 등급: 보통(임시 점수제) · 같은 종류 방 안 가구 작업 +15%', post: '출전 편성: 장수와 데려갈 병력(지휘한도까지)을 고릅니다.',
    prod: () => { const o = S.orders[b.prod]; return `${RECIPES[b.prod].job} 작업 장수가 생산 목록을 처리 · 목록 ${o.length}건${o[0] ? ` · 진행 ${pct(o[0].prog)}` : ''}${b.prod === 'med' ? ' · 할 수 있는 치료가 있으면 치료가 먼저' : ''}`; },
    pen: `우리 ${b.w / 10 | 0}칸(시안) · ${A.filter(a => a.pen === 'pen' && !a.clinic && !a.dead && !a.released).length}마리 · 과밀 없음 · 수용: ${Object.entries(S.ranch.accepts).filter(e => e[1]).map(e => SPECIES[e[0]].n).join('·')}`,
    feeder: S.feeder.filter(f => f.allow).map(f => `${f.n} ${f.stock}/${f.target}`).join(' · ') + ` · 운반 우선순위 ${S.feederPrio}`,
    butcher: `${S.butcher.paused ? '<span class="chip warn">일시정지</span> ' : ''}대기 사체 ${S.butcher.queue.length}구 · 등록 순서대로 요리 작업 장수가 해체`,
    clinic: () => { const p = A.find(a => a.clinic && !a.dead && a.clinic.phase !== 'out'); return p ? `${p.name} · ${clinicLine(p)}` : '비어 있음 · 중상 동물 수술용(키트 필요)'; },
    house: `거주 ${b.residents} · 성벽 밖 · 안전 − · 재개발로 더 큰 주거를 이 위에 지을 수 있습니다.`,
  }[b.type]; const text = typeof lines === 'function' ? lines() : lines;
  return `<div class="summary" style="flex:1"><h2>${b.name}</h2><p>${text}</p><div class="facts"><span class="chip">주재료 ${mat(b.mat)?.n || '—'}</span>${room ? '<span class="chip info">방</span>' : ''}${S.targetPrio[b.id] ? `<span class="chip warn">대상 우선순위 ${S.targetPrio[b.id] === 10 ? '노란 경보' : S.targetPrio[b.id]}</span>` : ''}</div></div>`;
}
function clinicLine(a) {
  const c = a.clinic; if (!c) return '';
  return { wait: `수술 대기${a.surgeon ? ` · 집도의 ${byId(a.surgeon).name}` : highRisk(a) ? ' · 집도의 지정 필요(고위험·재수술)' : ''}${stock('kit') < 1 ? ' · 수술 키트 부족' : ''}`,
    surgery: `수술 중 ${pct(c.prog)} · 집도의 ${c.by ? byId(c.by).name : '이탈 — 다른 의료 장수 대기'}`, recovery: `회복 중 · 남은 ${c.left}초(배고픔 정지)`,
    failed: `수술 실패(${c.result}) · 후유증 ${a.seq} — 재수술 또는 우리 복귀 선택`, ready: '회복 완료 · 원래 우리로 운반 대기' }[c.phase] || '';
}
function animalSummary(a) {
  if (!a || a.dead) return '<div class="summary"><h2>개체 없음</h2></div>';
  const sp = SPECIES[a.sp];
  return `<div class="summary" style="flex:1"><h2>${esc(a.name)} <span class="role">${sp.n} · ${sp.temper} · ${a.stage}</span></h2>
    <div class="facts"><span class="chip ${a.tame ? 'ok' : ''}">${a.tame ? '길들임' : a.untameable ? '야생(길들이기 불가)' : `야생 · 길들이기 ${pct(a.tameProg || 0)}`}</span>
    <span class="chip ${a.hp < 40 ? 'bad' : a.hp < 100 ? 'warn' : ''}">체력 ${a.hp}%${a.injury ? ' · ' + a.injury : ''}</span><span class="chip ${a.hunger < 30 ? 'bad' : ''}">배고픔 ${a.hunger}</span>
    <span class="chip">${a.clinic ? clinicLine(a) : esc(a.task)}</span>${a.seq ? `<span class="chip bad">후유증 ${a.seq}</span>` : ''}${reqChips(a)}</div>
    <p>${a.pen ? '우리(목장) 소속' : '야생 — 포획해 결박한 뒤 운반 요청'}${a.bound ? ' · 현장 결박됨' : ''}</p></div>`;
}
const reqChips = a => (a.req.capture ? '<span class="chip info">포획 요청</span>' : '') + (a.req.carry ? `<span class="chip info">운반 → ${a.req.carry}</span>` : '') +
  (a.req.slaughter ? `<span class="chip bad">${a.req.slaughter === 'started' ? '도축 중(취소 불가)' : '도축 대기'}</span>` : '');

/* ───────── 5) 명령 칸(3×3) ───────── */
function commands() {
  const k = kindOf(), c = selCommanders()[0];
  if (k === 'none') return [
    { l: '작업표', i: 'grid', a: 'open', d: { w: 'duty' } }, { l: '장수 관리', i: 'users', a: 'open', d: { w: 'commander' } }, { l: '건설', i: 'hammer', a: 'open', d: { w: 'build' }, primary: 1 },
    { l: '연구', i: 'flask', a: 'say', d: { t: '과학 화면은 이번 시안 범위 밖입니다.' } }, { l: '인구', i: 'house', a: 'open', d: { w: 'population' } }, { l: '외교', i: 'hand', a: 'open', d: { w: 'diplomacy' } },
    { l: S.prioMode ? '지정 끝' : '우선 지정', i: 'target', a: 'prioMode', on: S.prioMode, why: '고른 단계를 대상에 클릭해 지정합니다. 노란 경보 → 작업 종류 → 대상 우선순위(9 최우선) → 거리 순.' },
    { l: S.prioLevel === 10 ? '노란 경보' : `단계 ${S.prioLevel}`, i: 'level', a: 'prioLevel', why: '1(낮음)~9(높음), 노란 경보(그 대상을 최우선, 식사·수면·치료는 중단하지 않음)' },
    { l: S.red ? '경보 해제' : '빨간 경보', i: 'siren', a: 'red', alarm: S.red, why: '소굴 전체: 식사·수면·치료받기·간호를 멈추고 작업. 작업 금지는 유지.' }];
  if (k === 'commander') return [
    { l: '우선', i: 'flag', a: 'say', d: { t: `${c.name}: 대상을 우클릭하면 그 일부터 하고 끝나면 자율 작업으로 돌아갑니다.` } },
    { l: c.resting ? '휴식 중' : c.injuries.length ? '치료' : '휴식', i: 'bed', a: 'rest', why: '숙소로 보내 쉬게 합니다. 부상이 있으면 빈 침상이 있는 의무실로.' },
    { l: '건설', i: 'hammer', a: 'open', d: { w: 'build' }, primary: 1 }, { l: '작업표', i: 'grid', a: 'open', d: { w: 'duty' } }, { l: '상세', i: 'info', a: 'open', d: { w: 'commander', id: c.id } },
    { l: '무기', i: 'swap', a: 'say', d: { t: '보유한 다음 무기로 바꿉니다(시안: 바꿀 무기 없음).' } }, { l: '연구', i: 'flask', a: 'say', d: { t: '과학 화면은 이번 시안 범위 밖입니다.' } },
    { l: '정지', i: 'stop', a: 'say', d: { t: `${c.name}: 하던 일을 멈춥니다. 자율 작업은 다시 이어집니다.` } }, null];
  if (k === 'multi') {
    const all = selCommanders(), dep = all.every(x => x.deployed);
    return [{ l: dep ? '공격 이동' : '우선', i: dep ? 'sword' : 'flag', a: 'say', d: { t: dep ? '지면을 클릭해 이동하며 공격합니다.' : '대상을 우클릭하면 선택한 장수들이 그 일부터 합니다.' } },
      { l: dep ? '정지' : '개별 선택 필요', i: 'stop', a: 'say', d: { t: '정지' }, off: !dep, why: '평시 휴식·치료는 장수마다 조건이 달라 개별 선택이 필요합니다.' },
      { l: '귀환', i: 'back', a: 'say', d: { t: '귀환' }, off: !dep, why: '전원 출전 중일 때만' }, { l: '선택 해제', i: 'x', a: 'selNone' }, null, null, null, null, null];
  }
  if (k === 'deployed') return [
    { l: c.skillCd > 0 ? '강타' : '강타', i: 'bolt', a: 'skill', cd: c.skillCd, why: '무기 스킬: 큰턱 강타(근접 2). 다음 타격 2배, 재사용 15초.' },
    c.wings ? { l: '급강하', i: 'wing', a: 'say', d: { t: '지면을 클릭하면 급강하합니다(시안).' }, why: '날개 + 근접/원거리 5' } : null,
    { l: '공격 이동', i: 'sword', a: 'say', d: { t: '지면을 클릭하면 이동하며 만나는 적을 공격합니다.' } }, { l: '정지', i: 'stop', a: 'say', d: { t: '이동과 공격을 멈춥니다.' } },
    { l: '귀환', i: 'back', a: 'say', d: { t: '징집소로 돌아가 생존 병력을 반납하고 자율 작업을 재개합니다.' } }, { l: '상세', i: 'info', a: 'open', d: { w: 'commander', id: c.id } },
    { l: '무기', i: 'swap', a: 'say', d: { t: '보유한 다음 무기로 바꿉니다.' } }, null, null];
  if (k === 'building') {
    const b = bld(S.sel.id);
    const first = { dorm: { l: '배정 보기', i: 'users', a: 'say', d: { t: '숙소 배정: 관우·유비·제갈량·노숙 / 노숙: 장비' } }, post: { l: '출전', i: 'sword', a: 'say', d: { t: '출전 편성 창(징집소)은 이번 시안 범위 밖입니다.' }, primary: 1 },
      prod: { l: '생산 목록', i: 'list', a: 'open', d: { w: 'production', id: b.prod }, primary: 1 }, pen: { l: '목장 관리', i: 'paw', a: 'open', d: { w: 'ranch' }, primary: 1 },
      feeder: { l: '먹이통', i: 'food', a: 'open', d: { w: 'feeder' }, primary: 1 }, butcher: { l: '도축대', i: 'knife', a: 'open', d: { w: 'butcher' }, primary: 1 },
      clinic: { l: '환자 보기', i: 'cross', a: 'open', d: { w: 'animal', id: 12 }, primary: 1, off: !A.some(a => a.clinic && !a.dead) }, house: { l: '재개발', i: 'house', a: 'open', d: { w: 'redevelop', id: b.id }, primary: 1 },
      room: { l: '대상 정보', i: 'info', a: 'say', d: { t: buildingSummary(b).replace(/<[^>]+>/g, ' ') } } }[b.type];
    const clinicBusy = b.type === 'clinic' && A.some(a => a.clinic && !a.dead), fixed = ['house', 'post'].includes(b.type), noMat = ['pen'].includes(b.type);
    return [first, b.type === 'dorm' ? { l: '숙소 건설', i: 'hammer', a: 'open', d: { w: 'build', cat: 1 } } : { l: '건설', i: 'hammer', a: 'open', d: { w: 'build' } },
      { l: '방 정보', i: 'info', a: 'say', d: { t: `${b.name}: 방 종류·등급 확인(시안)` } }, { l: '선택 해제', i: 'x', a: 'selNone' },
      { l: '철거', i: 'trash', a: 'say', d: { t: `${b.name} 철거 예정지를 지정했습니다(장수가 현장에서 철거, 건설비 70% 반환).` }, off: clinicBusy, why: clinicBusy ? '동물이 있는 치료대는 철거·이동할 수 없습니다. 먼저 비우세요.' : '장수가 현장에서 철거하면 건설비의 70%를 돌려받습니다.' },
      { l: '이동', i: 'move', a: 'say', d: { t: `${b.name}: 새 위치를 고르면 장수가 비용 없이 옮깁니다.` }, off: clinicBusy || fixed, why: fixed ? '주거·방어 시설은 철거 후 다시 지으세요.' : clinicBusy ? '동물이 있는 치료대는 이동할 수 없습니다.' : '방 가구·장식만 이동' },
      { l: '재료', i: 'layers', a: 'open', d: { w: 'renovate', id: b.id }, off: noMat, why: noMat ? '이 건물은 재료를 고를 수 없습니다.' : '다른 재료로 개보수합니다.' }, null, null];
  }
  const a = animal(S.sel.id); if (!a || a.dead) return [];
  const inClinic = !!a.clinic, carriable = a.bound || a.hp < 20, started = a.req.slaughter === 'started';
  return [
    { l: a.req.capture ? '포획 취소' : '포획 요청', i: 'rope', a: 'animalReq', d: { r: 'capture', id: a.id }, off: a.bound || inClinic, why: a.bound ? '이미 결박됨' : a.tame ? '길들인 개체는 저항 없이 결박됩니다.' : '사육 장수가 붙잡아 현장에 결박합니다.' },
    { l: a.req.carry ? '운반 취소' : '운반 요청', i: 'cart', a: 'animalReq', d: { r: 'carry', id: a.id }, off: !carriable || inClinic && a.clinic.phase !== 'ready', why: carriable ? '받는 우리를 목적지로 운반합니다.' : '결박되거나 쓰러진 개체만 운반합니다. 먼저 포획 요청하세요.' },
    { l: started ? '도축 중' : a.req.slaughter ? '도축 취소' : '도축 요청', i: 'knife', a: 'animalReq', d: { r: 'slaughter', id: a.id }, off: inClinic || started, why: inClinic ? '치료 시설 안 동물은 도축할 수 없습니다.' : started ? '실제 도축을 시작하면 취소할 수 없습니다.' : '이름·마릿수 확인 후 요청' },
    { l: '치료 지시', i: 'cross', a: 'open', d: { w: 'animal', id: a.id, tab: '의료' }, off: a.hp >= 100 && !inClinic, why: a.hp >= 100 ? '부상 없음' : '경상 투약 / 중상 수술' },
    { l: '돌봄 1회', i: 'heart', a: 'say', d: { t: `${a.name}: 돌봄 1회 지시(사육 장수가 돌봄대에서 부름).` }, off: !a.pen || inClinic, why: a.pen ? '' : '우리 안 개체만' },
    { l: '이름 변경', i: 'pen', a: 'open', d: { w: 'animal', id: a.id, tab: '개요' } }, { l: '상세', i: 'info', a: 'open', d: { w: 'animal', id: a.id } },
    { l: '방생', i: 'door', a: 'release', d: { id: a.id }, off: inClinic, why: inClinic ? '치료 시설 안에서는 방생할 수 없습니다(우리로 복귀한 뒤).' : '확인 후 즉시 사라집니다(사체·생산물 없음).' }, { l: '선택 해제', i: 'x', a: 'selNone' }];
}
function renderCmd() {
  const cs = commands(); while (cs.length < 9) cs.push(null);
  $('#cmd').innerHTML = cs.slice(0, 9).map(c => c ? `<button class="btn c ${c.primary ? 'primary' : ''} ${c.on ? 'on' : ''} ${c.alarm ? 'alarm' : ''}" data-act="${c.a}" ${Object.entries(c.d || {}).map(([k, v]) => `data-${k}="${esc(v)}"`).join(' ')}
    ${c.off ? 'disabled' : ''} title="${esc(c.why || c.l)}" aria-label="${esc(c.l)}${c.off && c.why ? ' — ' + esc(c.why) : ''}">${ic(c.i, 20)}<span>${esc(c.l)}</span>${c.cd > 0 ? `<span class="cd" style="--p:${c.cd / 15 * 100}%"></span><span class="sec">${Math.ceil(c.cd)}</span>` : ''}</button>`
    : '<span class="btn c nil" aria-hidden="true"></span>').join('');
}

/* ───────── 6) 창 ───────── */
const reservedMap = { medicine: 0, kit: 0 };
const reserved = k => reservedMap[k] || 0;
const stock = k => (mat(k)?.have || 0) - reserved(k);
function openWin(w, arg = {}) {
  const prev = S.win; S.win = w; S.winArg = arg; if (arg.tab) S.tab[w] = arg.tab;
  if (w === 'negotiation' && !S.negoPause) { S.negoPause = { paused: S.paused, speed: S.speed }; } // 협상 페이지가 열린 동안 반드시 시간 정지
  if (w === 'build' && arg.cat !== undefined) S.buildCat = +arg.cat;
  $('#pop').hidden = true; renderWin(); const sc = $('#scrim'); sc.classList.add('open'); sc.setAttribute('aria-hidden', 'false');
  if (!prev) { lastFocus = document.activeElement; setTimeout(() => $('#window .btn, #window button, #window input')?.focus(), 30); }
  renderClock();
}
let lastFocus = null;
function closeWin() {
  if (!S.win) return;
  if (S.win === 'negotiation') { const f = FACTIONS.find(x => x.id === S.nego?.f); if (f && !f.invalid && S.nego && !S.nego.done) holdNegotiation(f); S.negoPause = null; }
  S.win = null; const sc = $('#scrim'); sc.classList.remove('open'); sc.setAttribute('aria-hidden', 'true'); renderAll(); lastFocus?.focus?.();
}
function head(title, sub, extra = '') { return `<header class="win-head"><h2 id="winTitle">${title}</h2><span class="sub">${sub || ''}</span><span class="grow"></span>${extra}<button class="btn ghost" data-act="close" aria-label="닫기">${ic('x', 14)}</button></header>`; }
function renderWin() {
  if (!S.win) return;
  const fn = { duty: winDuty, commander: winCommander, population: winPopulation, build: winBuild, renovate: winRenovate, redevelop: winRedevelop, production: winProduction,
    diplomacy: winDiplomacy, negotiation: winNegotiation, ranch: winRanch, animal: winAnimal, feeder: winFeeder, butcher: winButcher }[S.win];
  const r = fn(S.winArg); const w = $('#window');
  w.className = 'window plate' + (r.narrow ? ' narrow' : '');
  w.innerHTML = head(r.title, r.sub, r.extra) + `<div class="win-body">${r.body}</div>` + (r.foot ? `<footer class="win-foot">${r.foot}</footer>` : '');
}
const tabs = (w, names, soon = []) => { const cur = S.tab[w] || names[0]; return `<div class="tabs" role="tablist">${names.map(n => `<button role="tab" class="${soon.includes(n) ? 'soon' : ''}" aria-selected="${cur === n}" data-act="tab" data-w="${w}" data-n="${n}">${n}${soon.includes(n) ? ' · 준비 중' : ''}</button>`).join('')}</div>`; };

// 작업표: 금지 / 1~5, 기술 수치·열정, 불가 칸 잠금. 게임은 멈추지 않는다.
function winDuty() {
  const head = `<tr><th class="who">장수 / 현재 상태</th>${JOBS.map(([, n]) => `<th>${n.replace('\n', '<br>')}</th>`).join('')}</tr>`;
  const rows = C.map(c => `<tr><td class="who"><b>${c.name}</b> <span class="dim">· ${esc(c.job.split(' · ')[0])}</span></td>${JOBS.map(([k, n, skill]) => {
    const locked = c.locked.includes(k), p = c.prio[k], lv = skill ? c.skills[skill] : null, fl = skill ? c.passion[skill] : 0;
    const label = locked ? `${n.replace('\n', '')}: ${c.young ? '어린 장수는 일하지 않음' : '특성으로 불가'}` : `${c.name} ${n.replace('\n', '')}: ${PRIO[p]}${lv !== null ? ` · 기술 ${lv}` : ''}${fl ? ` · 열정 ${fl === 2 ? '큰' : '작은'} 불꽃` : ''}`;
    return `<td><button class="cell ${locked ? 'locked' : ''}" data-act="${locked ? 'say' : 'duty'}" data-t="${esc(label)}" data-id="${c.id}" data-k="${k}" data-p="${locked ? 0 : p}" aria-label="${esc(label)}" title="${esc(label)}${locked ? '' : ' — 클릭: 올림 / 우클릭: 내림'}">
      ${locked ? ic('lock', 12) : `<span class="p">${p ? p : '금지'}</span>`}<span class="sk">${lv !== null ? lv : k === 'clean' ? '—' : ''}${fl ? `<span class="flame">${ic('flame', 9)}${fl === 2 ? ic('flame', 9) : ''}</span>` : ''}</span></button></td>`;
  }).join('')}</tr>`).join('');
  return { title: '작업표', sub: '장수 × 작업 15종 · 게임은 멈추지 않음', body: `<p class="muted" style="margin:0">숫자 = 우선순위 1(매우 낮음)~5(매우 높음), ‘금지’ = 하지 않음(경보 중에도 금지 유지). 높은 숫자부터, 같으면 대상 우선순위 → 거리 → 왼쪽 순. 진행 중인 작업은 마친 뒤 적용. 칸 아래 = 기술 수치·열정. 간호는 기획의 의료 작업(투약·수술·약제대 생산).</p>
    <div style="overflow:auto"><table class="duty">${head}${rows}</table></div>`, foot: `<span class="note">예시 데이터</span><button class="btn" data-act="say" data-t="채집 금지 지정: 지도에서 자원을 클릭(시안)">채집 금지</button><button class="btn" data-act="say" data-t="지정 취소: 지도에서 지정을 지움(시안)">지정 취소</button><button class="btn primary" data-act="close">닫기</button>` };
}
// 장수 상세: 개요·기분·특성·관계·장비·건강(기획 탭). CA/PA 공개 범위는 미정이라 넣지 않는다.
function winCommander(arg) {
  const c = byId(arg.id || (S.sel.ids || [])[0]) || C[0]; const t = S.tab.commander || '개요';
  const list = `<div class="row">${C.map(x => `<button class="btn ${x.id === c.id ? 'on' : ''}" data-act="open" data-w="commander" data-id="${x.id}">${x.name}</button>`).join('')}</div>`;
  const skills = `<div class="skills">${SKILLS.map(([k, n]) => `<div class="skill"><span>${n}</span><span class="bar"><i style="width:${c.skills[k] / 20 * 100}%"></i></span><span class="n">${c.skills[k]}</span><span class="flame">${c.passion[k] ? ic('flame', 11) : ''}</span></div>`).join('')}</div>`;
  const body = {
    '개요': `<div class="grid2"><div class="box well"><h3>상태</h3><dl class="kv"><dt>나이</dt><dd>${c.young ? '0세 · 어린 장수(12개월간 일·출전 불가)' : '장년 · 4세'}</dd><dt>무기</dt><dd>${c.weapon}</dd><dt>하는 일</dt><dd>${esc(c.job)}</dd><dt>기분</dt><dd class="n">${c.mood}</dd><dt>개인 체력</dt><dd class="n">${c.hp} / 100</dd></dl></div>
      <div class="box well"><h3>기술 15종(0~20) · 불꽃 = 열정</h3>${skills}</div></div>`,
    '기분': `<div class="box well"><h3>기분 ${c.mood} — 요인</h3><ul class="list">${c.moodWhy.map(([n, v]) => `<li><span class="grow">${n}</span><b class="n ${v < 0 ? 'bad-ink' : 'ok-ink'}">${v > 0 ? '+' : ''}${v}</b></li>`).join('') || '<li class="dim">요인 없음</li>'}</ul><p class="dim" style="margin:0">탈주 기준: 기분 20 이하가 한 달 내내 이어지면(충직 10 · 교활 30). 충성심 수치는 없음.</p></div>`,
    '특성': `<div class="box well"><h3>특성(최대 4개)</h3><div class="row">${c.traits.map(x => `<span class="chip">${x}</span>`).join('') || '<span class="dim">없음</span>'}</div><p class="dim" style="margin:0">빨강·실버·골드·다이아 등급 배정은 기획 미정이라 표시하지 않습니다.</p></div>`,
    '관계': `<div class="box well"><h3>관계</h3><ul class="list">${c.rel.map(([n, v]) => `<li><span class="grow">${n}</span><span>${v}</span></li>`).join('') || '<li class="dim">아직 관계 없음</li>'}</ul></div>`,
    '장비': `<div class="box well"><h3>장비 슬롯 3</h3><ul class="list">${c.equip.map(([s, v]) => `<li><span class="grow">${s}</span><span class="${v ? '' : 'dim'}">${v || '비어 있음'}</span><button class="btn" data-act="say" data-t="장착·해제는 장비 보관함에서(시안)">${v ? '교체' : '장착'}</button></li>`).join('')}</ul></div>`,
    '건강': `<div class="box well"><h3>건강</h3><dl class="kv"><dt>개인 체력</dt><dd class="n">${c.hp} / 100</dd><dt>부상</dt><dd>${c.injuries.map(i => i.join(' · ')).join(', ') || '없음'}</dd><dt>치료</dt><dd>경상 자연 회복 · 중상은 의무실</dd></dl></div>`,
  }[t];
  return { title: `${c.name} 상세`, sub: `${c.sex} · ${c.weapon}`, body: list + tabs('commander', ['개요', '기분', '특성', '관계', '장비', '건강']) + body, foot: `<span class="note">예시 데이터 · 포상은 상세 창에서(기획)</span><button class="btn primary" data-act="close">닫기</button>` };
}
function taxLine() {
  const p = S.pop, w = weekly();
  return `<span class="chip ok">예상 주간 수입: 식량 +<b class="n">${w.food}</b> · 재료 +<b class="n">${w.mat}</b></span><span class="chip">다음 납세 <b class="n">${mmss(S.taxIn)}</b> 후(약 75초마다, 1개월 = 4주)</span>
    <span class="chip ${p.rate > 25 ? 'bad' : p.rate > 15 ? 'warn' : ''}">민심 영향: ${p.rate > 25 ? '크게 하락' : p.rate > 15 ? '하락' : '작음'}(방향만 · 수치 미정)</span>`;
}
// 인구·세금: 1% 슬라이더 + 숫자 입력, 방침 3종, 예상 주간 수입, 다음 납세 시점, 민심·주거·이주·행정.
function winPopulation() {
  const p = S.pop, w = weekly();
  const causes = [['주거', 74], ['공공서비스', 58], ['안전', 41], ['민심', p.sentiment]];
  return { title: '인구 · 세금', sub: '시민(일반개미)은 자체 생계 · 주간 현물 납세 · 병역', body: `
    <div class="grid3"><div class="box well"><h3>인구 <span class="n">${p.total}</span> / 살 자리 <span class="n">${p.housing}</span></h3><dl class="kv"><dt>어린</dt><dd class="n">${p.young}</dd><dt>성체</dt><dd class="n">${p.adult}</dd><dt>늙은</dt><dd class="n">${p.old}</dd><dt>동원 중</dt><dd class="n">${p.deployed}(납세 제외)</dd></dl></div>
      <div class="box well"><h3>민심 <span class="n">${p.sentiment}</span></h3><div class="meter"><i style="transform:scaleX(${p.sentiment / 100})"></i></div><p class="dim" style="margin:0">세율·주거·서비스·안전·병역·행정에 맞춘 목표치로 서서히 이동합니다. 목표식은 기획 미정.</p></div>
      <div class="box well"><h3>이주 수요 <span class="n">${p.demand}</span></h3>${causes.map(([n, v]) => `<div class="row between" style="font-size:12px"><span>${n}</span><span class="n">${v}</span></div><div class="meter"><i style="transform:scaleX(${v / 100})"></i></div>`).join('')}</div></div>
    <div class="box well"><h3>세금</h3>
      <div class="row"><label for="taxRange" class="muted">세율</label><input id="taxRange" type="range" min="0" max="50" step="1" value="${p.rate}" data-input="rate" style="max-width:360px" aria-valuetext="${p.rate}%">
        <input id="taxNum" type="number" min="0" max="50" step="1" value="${p.rate}" data-input="rateNum" aria-label="세율 직접 입력(%)"><span>%</span><span class="dim">0~50%는 현재 구현의 잠정 범위</span></div>
      <div class="row"><span class="muted">납세 방침</span><span class="seg" role="group" aria-label="납세 방침">${['식량 중심', '균형', '재료 중심'].map(f => `<button data-act="focus" data-v="${f}" aria-pressed="${p.focus === f}">${f}</button>`).join('')}</span>
        <span class="muted" style="margin-left:12px">우선 납부 재료</span><select data-input="prefer" aria-label="우선 납부 재료">${['조약돌', '나뭇잎', '나무껍질'].map(m => `<option ${m === p.prefer ? 'selected' : ''}>${m}</option>`).join('')}</select></div>
      <div class="row" id="taxLine">${taxLine()}</div>
      <p class="dim" style="margin:0">납세 대상 ${w.payers}명(성체·늙은 시민, 동원 병력·어린 개미 제외). 세율·방침 변경은 변경 시점부터 누적해 주간 납세 때 지급합니다. 예상치는 시안 예시 계산입니다.</p></div>
    <div class="grid2"><div class="box well"><h3>병역</h3><div class="row">${['모병제', '징병제', '예비군제', '국민개병'].map((m, i) => `<button class="btn ${p.policy === m ? 'on' : ''}" data-act="policy" data-v="${m}" ${i > 1 ? 'disabled title="과학 연구로 해금"' : ''}>${m}</button>`).join('')}</div>
      <label class="row"><input type="checkbox" data-input="elder" ${p.elder ? 'checked' : ''}> 병역 나이: 늙은 개미 포함</label></div>
      <div class="box well"><h3>행정 성과 ${pct(p.admin)}</h3><p class="dim" style="margin:0">행정 책상에서 정치 장수가 업무 처리 → 징수 손실·민심·이주·재개발 보정. 보정 수치는 잠정.</p>
      <div class="row"><span class="muted">재개발 보상 수준</span><input type="range" min="50" max="200" step="10" value="${p.comp * 100}" data-input="comp" style="max-width:200px" aria-label="재개발 보상 수준"><b class="n">${Math.round(p.comp * 100)}%</b></div></div></div>`,
    foot: `<span class="note">예시 데이터 · 시간은 계속 흐름</span><button class="btn primary" data-act="close">닫기</button>` };
}
// 건설: 16분류, 미구현 분류는 '준비 중'. 재료 선택·비교·부족 표시.
function winBuild() {
  const [cat, items] = BUILD[S.buildCat];
  const tabsHtml = `<div class="tabs" role="tablist">${BUILD.map(([n, it], i) => `<button role="tab" class="${it ? '' : 'soon'}" aria-selected="${i === S.buildCat}" data-act="buildCat" data-i="${i}">${n}${it ? '' : ' · 준비 중'}</button>`).join('')}</div>`;
  if (!items) return { title: '건설', sub: '16개 분류', body: tabsHtml + `<div class="soon-box">${ic('lock', 20)}<p><b>${cat}</b> 분류는 기획 규칙(흐름·저장·신호)이 정해지지 않아 준비 중입니다. 배치할 수 있는 항목이 없습니다.</p></div>`, foot: `<button class="btn primary" data-act="close">닫기</button>` };
  const it = items.find(x => x[0] === S.buildItem) || items[0], need = it[1], selectable = it[2];
  const mats = selectable ? Object.keys(STRUCT) : [];
  const m = selectable ? (mats.includes(S.buildMat) ? S.buildMat : mats[0]) : null, short = m && mat(m).have < need;
  return { title: '건설', sub: '좌클릭 1개 배치 · Shift 유지 · Shift+드래그 연속', body: tabsHtml + `<div class="build">
    <div class="items">${items.map(([n, cost, sel]) => `<button class="btn item ${n === it[0] ? '' : ''}" aria-pressed="${n === it[0]}" data-act="buildItem" data-n="${n}"><b>${n}</b><small>재료 ${cost}${sel ? ' · 재료 선택' : ''}</small></button>`).join('')}</div>
    <div class="box well"><h3>${it[0]} — ${selectable ? '주재료 선택' : '고정 재료'}</h3>
      ${selectable ? `<div class="mat head" role="presentation"><span>재료(보유/필요)</span><span>내구도</span><span>미관</span><span>단열</span><span>가연성</span><span></span></div>
      <div role="radiogroup" aria-label="주재료" style="display:flex;flex-direction:column;gap:3px">${mats.map(k => { const s = STRUCT[k], have = mat(k).have, lack = have < need;
        return `<div class="mat ${lack ? 'short' : ''}" role="radio" tabindex="0" aria-checked="${k === m}" data-act="buildMat" data-k="${k}"><span>${mat(k).n} <span class="n ${lack ? 'bad-ink' : ''}">${have}/${need}</span></span><span class="n">${s.dur}</span><span class="n">${s.beauty > 0 ? '+' : ''}${s.beauty}</span><span class="n">${s.insul}</span><span>${s.flam ? '<span class="warn-ink">탐</span>' : '안 탐'}</span><span>${lack ? '<span class="chip bad">부족</span>' : ''}</span></div>`; }).join('')}</div>
      <p class="dim" style="margin:0">필수 부품은 고정, 귀한 부재료는 자동 사용 제외. 재료 목록·수치는 기획 미정(예시).</p>` : `<p class="muted" style="margin:0">재료 ${need} · 이 항목은 재료를 고르지 않습니다.</p>`}
      <div class="row">${short ? '<span class="chip bad">재료 부족 — 배치할 수 없습니다</span>' : '<span class="chip ok">배치 가능 · 배치 순간 차감, 취소하면 100% 반환</span>'}</div>
      <p class="dim" style="margin:0">완성 후 철거는 70% 반환. 주거를 기존 집 위에 배치하면 재개발(보상비·민심 견적 표시).</p></div></div>`,
    foot: `<span class="note">예시 데이터</span><button class="btn" data-act="close">취소</button><button class="btn primary" data-act="place" data-n="${it[0]}" ${short ? 'disabled' : ''}>배치 시작</button>` };
}
function winRenovate(arg) {
  const b = bld(arg.id), cur = b.mat, need = 30;
  return { title: `${b.name} · 재료 개보수`, sub: `현재 주재료 ${mat(cur)?.n}`, narrow: 1, body: `<p class="muted" style="margin:0">지시할 때 새 재료 ${need}을(를) 먼저 차감하고, 장수가 작업하는 동안 건물을 쓸 수 없습니다. 취소하면 새 재료를 모두 돌려받고, 완료하면 기존 재료의 70%를 돌려받습니다(창고가 차면 건물 주변 바닥).</p>
    <ul class="list">${Object.keys(STRUCT).filter(k => k !== cur).map(k => { const s = STRUCT[k], lack = mat(k).have < need;
      return `<li class="${lack ? 'excluded' : ''}"><span class="grow"><b>${mat(k).n}</b> <span class="dim">내구도 ${s.dur} · 미관 ${s.beauty} · 단열 ${s.insul} · ${s.flam ? '탐' : '안 탐'}</span></span><span class="n ${lack ? 'bad-ink' : ''}">${mat(k).have}/${need}</span><button class="btn" data-act="renovate" data-k="${k}" data-id="${b.id}" ${lack ? 'disabled title="재료 부족"' : ''}>변경 지시</button></li>`; }).join('')}</ul>
    ${b.renovating ? `<div class="row"><span class="chip warn">${mat(b.renovating).n}(으)로 개보수 예정 — 사용 중지</span><button class="btn danger" data-act="renovateCancel" data-id="${b.id}">취소(새 재료 100% 반환)</button></div>` : ''}`, foot: `<button class="btn primary" data-act="close">닫기</button>` };
}
function winRedevelop(arg) {
  const b = bld(arg.id), p = S.pop; const target = S.redev || '흙집';
  const t = { '흙집': { cost: 80, cap: 50 }, '큰 아파트': { cost: 200, cap: 120 } }[target];
  const comp = Math.round(b.residents * .5 * p.comp), build = Math.round(t.cost * .6), unrest = p.comp < 1 ? (b.residents * .2 * (1 - p.comp)).toFixed(1) : 0;
  return { title: `${b.name} 재개발`, sub: `거주 ${b.residents} · 기존 집 위에 상위 주거 배치`, narrow: 1, body: `
    <div class="row"><span class="muted">새 주거</span><span class="seg" role="group">${['흙집', '큰 아파트'].map(n => `<button data-act="redevTarget" data-v="${n}" aria-pressed="${n === target}">${n}</button>`).join('')}</span></div>
    <dl class="kv"><dt>건설비</dt><dd><b class="n">${build}</b> 재료(신축 ${t.cost}의 60%)</dd><dt>보상비</dt><dd>식량 <b class="n">${comp}</b> · 재료 <b class="n">${comp}</b>(실거주 × 0.5 × 보상 수준 ${Math.round(p.comp * 100)}%) — 배치 순간 지급, 취소해도 반환 안 됨</dd>
    <dt>예상 민심</dt><dd class="${unrest > 0 ? 'bad-ink' : 'ok-ink'}">${unrest > 0 ? `−${unrest}(보상 부족)` : '불만 없음(보상 충분)'} · 공사 120초 초과 시 지연 불만</dd><dt>행정 보정</dt><dd>행정 성과 ${pct(p.admin)} → 보상비·불만 완화(잠정)</dd><dt>공사 중</dt><dd>기존 집·인구·납세 유지, 완공 시 정원 ${t.cap}</dd></dl>
    <div class="row"><span class="muted">보상 수준</span><input type="range" min="50" max="200" step="10" value="${p.comp * 100}" data-input="comp" style="max-width:220px" aria-label="보상 수준"><b class="n">${Math.round(p.comp * 100)}%</b></div>
    <p class="dim" style="margin:0">취소하면 기존 집·주거 효과 복구, 건설 자원 전액 반환. 이미 지급한 보상비는 반환하지 않습니다. 수치는 잠정.</p>`,
    foot: `<button class="btn" data-act="close">취소</button><button class="btn primary" data-act="place" data-n="${target}(재개발)">배치 시작</button>` };
}
// 생산: 지정 수량 / 목표 재고 유지 / 계속 생산, 목록 추가·삭제·진행.
function winProduction(arg) {
  const id = arg.id || 'smith', R = RECIPES[id], list = S.orders[id];
  const modeName = { count: '지정 수량', keep: '목표 재고 유지', forever: '계속 생산' };
  return { title: `${R.name} · 생산 목록`, sub: `${R.job} 작업 장수가 자율로 처리`, body: `<div class="row"><span class="seg" role="group" aria-label="생산 방식">${Object.entries(modeName).map(([k, n]) => `<button data-act="mode" data-v="${k}" aria-pressed="${S.mode === k}">${n}</button>`).join('')}</span>
    ${S.mode !== 'forever' ? `<span class="stepper"><button class="btn" data-act="amount" data-d="-1" aria-label="줄이기">−</button><input type="number" min="1" max="99" value="${S.amount}" data-input="amount" aria-label="${S.mode === 'count' ? '만들 수량' : '목표 재고'}"><button class="btn" data-act="amount" data-d="1" aria-label="늘리기">+</button></span><span class="muted">${S.mode === 'count' ? '만들 수량' : '목표 재고'}</span>` : '<span class="muted">재료가 있는 동안 반복</span>'}</div>
    <div class="grid2"><div class="box well"><h3>품목</h3><ul class="list">${R.items.map(([n, cost], i) => `<li><span class="grow"><b>${n}</b> <span class="dim">${cost}</span></span>${id === 'med' ? `<span class="dim">재고 ${i === 0 ? mat('medicine').have : mat('kit').have}</span>` : ''}<button class="btn" data-act="addOrder" data-id="${id}" data-i="${i}" ${list.length >= 8 ? 'disabled title="목록은 최대 8개"' : ''}>목록 추가</button></li>`).join('')}</ul></div>
    <div class="box well"><h3>생산 목록 ${list.length}/8</h3>${list.length ? `<ul class="list">${list.map((o, j) => `<li><span class="grow"><b>${j + 1}. ${R.items[o.item][0]}</b> <span class="dim">${modeName[o.mode]}${o.mode === 'count' ? ` · 남은 ${o.n}` : o.mode === 'keep' ? ` · 목표 ${o.n}` : ''}</span><span class="meter" style="margin-top:4px"><i style="transform:scaleX(${o.prog})"></i></span></span><span class="n">${pct(o.prog)}</span><button class="btn danger" data-act="delOrder" data-id="${id}" data-i="${j}" aria-label="삭제">${ic('trash', 14)}</button></li>`).join('')}</ul>` : '<div class="empty">목록이 비었습니다. 왼쪽 품목을 추가하세요.</div>'}</div></div>
    <p class="dim" style="margin:0">${id === 'med' ? '약제대는 간호(의료) 작업·의료 능력만 사용(능력은 속도만, 산출·품질 동일). 할 수 있는 치료가 있으면 치료가 먼저입니다.' : '재료는 한 묶음을 시작할 때 차감, 창고가 차면 결과물은 창고 주변 바닥.'} 정지 중에는 새 예약을 하지 않습니다.</p>`,
    foot: `<span class="note">예시 데이터 · 레시피 잠정</span><button class="btn primary" data-act="close">닫기</button>` };
}
// 외교: 세력 목록 → 협상. 보류 협상은 오른쪽 알림에 남는다.
function winDiplomacy() {
  return { title: '월드맵 · 외교', sub: '외교는 월드맵 안으로 합침(시안은 세력 목록만)', narrow: 1, body: `<ul class="list">${FACTIONS.map(f => `<li><span class="grow"><b>${f.name}</b> <span class="dim">${f.rel} · 외교 신뢰 ${f.trust}${f.ultimatum ? ` · <span class="bad-ink">최후통첩 ${mmss(f.ultimatum)}</span>` : ''}${f.held ? ' · 보류 협상' : ''}${f.invalid ? ' · <span class="warn-ink">무효</span>' : ''}</span></span>
    <button class="btn ${f.invalid || f.held ? 'primary' : ''}" data-act="nego" data-id="${f.id}">${f.invalid ? '확인' : f.held ? '협상 재개' : '협상'}</button></li>`).join('')}</ul>`, foot: `<button class="btn primary" data-act="close">닫기</button>` };
}
function startNego(f) { S.nego = S.nego?.f === f.id ? S.nego : { f: f.id, give: [['식량', 120]], get: [['조약돌', 60]], envoy: 'yb', log: [], done: false }; openWin('negotiation'); }
function holdNegotiation(f) {
  f.held = true; if (!S.toasts.some(t => t.nego === f.id)) toast({ kind: 'note', icon: 'chat', title: `협상 보류: ${f.name}`, text: '보류 중 자원은 예약하지 않습니다. 알림에서 다시 엽니다.', go: () => startNego(f), goLabel: '열기', nego: f.id });
}
function winNegotiation() {
  const f = FACTIONS.find(x => x.id === S.nego.f), n = S.nego;
  if (f.invalid) return { title: `${f.name} · 협상`, sub: '무효', narrow: 1, body: `<div class="timestop">${ic('pause', 12)} 협상 페이지가 열린 동안 게임 시간이 멈춥니다.</div><p class="line-quote">“(상황 변화 대사 — 문구 미정)”</p><p class="muted" style="margin:0">${f.invalid}</p>`,
    foot: `<button class="btn primary" data-act="negoInvalid" data-id="${f.id}">확인</button>` };
  const items = [['식량', 1], ['조약돌', 1], ['판재', 2], ['특수', 10], ['큰턱 날(정교)', 120]];
  const side = (key, title) => `<div class="box well"><h3>${title}</h3><ul class="list">${n[key].map(([nm, q], i) => `<li><span class="grow">${nm}</span><input type="number" min="1" value="${q}" data-input="nego" data-side="${key}" data-i="${i}" aria-label="${nm} 수량"><button class="btn ghost" data-act="negoDel" data-side="${key}" data-i="${i}" aria-label="빼기">${ic('x', 12)}</button></li>`).join('') || '<li class="dim">없음</li>'}</ul>
    <div class="row"><select data-input="negoPick" data-side="${key}" aria-label="${title}에 추가할 항목">${items.map(([nm]) => `<option>${nm}</option>`).join('')}</select><button class="btn" data-act="negoAdd" data-side="${key}">추가</button></div></div>`;
  const val = s => n[s].reduce((t, [nm, q]) => t + q * (items.find(x => x[0] === nm)?.[1] || 1), 0), ratio = val('give') / Math.max(1, val('get'));
  const mood = f.patience <= 0 ? '결렬' : ratio >= 1.15 ? '호의적' : ratio >= .85 ? '망설임' : '냉담'; f.reaction = mood;
  return { title: `${f.name} · 협상`, sub: `${f.rel} · 외교 신뢰 ${f.trust}`, extra: `<span class="timestop">${ic('pause', 12)} 시간 정지 중</span>`, body: `
    <div class="row"><span class="muted">담당 장수</span><select data-input="envoy" aria-label="담당 장수">${C.filter(c => !c.young && !c.away).map(c => `<option value="${c.id}" ${c.id === n.envoy ? 'selected' : ''}>${c.name} · 정치 ${c.skills.politics}</option>`).join('')}</select>
      <span class="dim">정치력은 요구 파악·조건 개선에 쓰입니다. 좋은 조건은 낮은 정치력으로도 성사될 수 있습니다.</span>${f.ultimatum ? `<span class="chip bad">최후통첩 남은 ${mmss(f.ultimatum)}(정지 중)</span>` : ''}</div>
    <div class="nego">${side('give', '우리가 줄 것')}<div class="box well reaction"><h3>상대 반응(대략)</h3><span class="face ${mood === '호의적' ? 'ok-ink' : mood === '망설임' ? 'warn-ink' : 'bad-ink'}">${mood}</span>
      <span class="dim">정확한 성공 확률은 보여주지 않습니다.</span><div><span class="dim">인내</span><div class="patience" aria-label="상대 인내 ${f.patience}/5">${[0, 1, 2, 3, 4].map(i => `<i class="${i < f.patience ? 'on' : ''}"></i>`).join('')}</div></div>
      ${n.log.length ? `<ul class="list" style="width:100%">${n.log.slice(-3).map(l => `<li style="min-height:0">${esc(l)}</li>`).join('')}</ul>` : ''}</div>${side('get', '받을 것')}</div>
    <p class="dim" style="margin:0">성립 직전 양쪽 보유량을 확인하고 즉시 교환합니다. 보류 중 자원은 예약하지 않으며, 창고가 차면 초과분은 창고 주변 바닥(창고가 없으면 비축더미 주변)에 둡니다.</p>`,
    foot: `<span class="note">예시 데이터 · 결과는 시안의 무작위</span><button class="btn" data-act="close">보류하고 닫기 <span class="dim">Esc</span></button><button class="btn primary" data-act="propose" ${f.patience <= 0 || n.done ? 'disabled' : ''}>제안하기</button>` };
}

// 목장: 우리 상태·과밀·수용 종·개체 목록, 일괄 요청·취소·개별 취소(실행 불가 개체는 사유·마릿수).
const live = () => A.filter(a => !a.dead && !a.released);
function exclusions(kind, list) {
  const why = a => kind === 'capture' ? (a.bound ? '이미 결박' : a.clinic ? '치료 시설 안' : a.req.capture ? '이미 요청됨' : null)
    : kind === 'carry' ? (a.clinic && a.clinic.phase !== 'ready' ? '치료 시설 안' : !(a.bound || a.hp < 20) ? '결박·쓰러짐 아님' : a.req.carry ? '이미 요청됨' : null)
      : kind === 'slaughter' ? (a.clinic ? '치료 시설 안' : a.req.slaughter ? '이미 요청됨' : null) : null;
  const ok = list.filter(a => !why(a)), out = {}; list.filter(why).forEach(a => out[why(a)] = (out[why(a)] || 0) + 1);
  return { ok, out };
}
function winRanch() {
  const r = S.ranch, all = live().filter(a => a.pen === 'pen' || a.wild || a.clinic), f = S.flt;
  const working = a => !!(a.clinic || a.req.capture || a.req.carry || a.req.slaughter);
  const list = all.filter(a => (f.sp === 'all' || a.sp === f.sp) && (f.stage === 'all' || a.stage === f.stage) && (f.tame === 'all' || (f.tame === 'tame') === !!a.tame)
    && (f.hurt === 'all' || (f.hurt === 'hurt') === (a.hp < 100)) && (f.work === 'all' || (f.work === 'busy') === working(a)))
    .sort((x, y) => S.sort === 'age' ? y.age - x.age : S.sort === 'hp' ? x.hp - y.hp : S.sort === 'hunger' ? x.hunger - y.hunger : x.name.localeCompare(y.name, 'ko'));
  const sel = (k, label, opts) => `<label class="row" style="gap:4px"><span class="muted">${label}</span><select data-input="flt" data-k="${k}" aria-label="${label} 필터">${opts.map(([v, n]) => `<option value="${v}" ${f[k] === v ? 'selected' : ''}>${n}</option>`).join('')}</select></label>`;
  const filters = `<div class="row">${sel('sp', '종', [['all', '전체'], ...Object.entries(SPECIES).map(([k, v]) => [k, v.n])])}${sel('stage', '성장', [['all', '전체'], ['새끼', '새끼'], ['성체', '성체'], ['노령', '노령']])}
    ${sel('tame', '길들임', [['all', '전체'], ['tame', '길들임'], ['wild', '야생']])}${sel('hurt', '부상', [['all', '전체'], ['hurt', '부상'], ['ok', '정상']])}${sel('work', '현재 작업', [['all', '전체'], ['busy', '요청·치료 중'], ['idle', '없음']])}
    <label class="row" style="gap:4px;margin-left:auto"><span class="muted">정렬</span><select data-input="sort" aria-label="정렬">${[['name', '이름'], ['age', '나이'], ['hp', '건강'], ['hunger', '허기']].map(([v, n]) => `<option value="${v}" ${S.sort === v ? 'selected' : ''}>${n}</option>`).join('')}</select></label></div>
    <p class="dim" style="margin:0">필터·정렬은 목록 표시에만 영향을 주며 작업 순서를 바꾸지 않습니다. ${list.length}/${all.length}마리 표시.</p>`;
  const picked = all.filter(a => S.pick.has(a.id));
  const ex = k => exclusions(k, picked), exTxt = e => Object.entries(e.out).map(([w, c]) => `${w} ${c}`).join(', ');
  const cancellable = picked.filter(a => a.req.capture || a.req.carry || a.req.slaughter === 'wait'), startedN = picked.filter(a => a.req.slaughter === 'started').length;
  const bulk = picked.length ? `<div class="box well"><h3>선택 ${picked.length}마리 일괄 요청</h3><div class="row">
    ${[['capture', '포획 요청', 'rope'], ['carry', '운반 요청', 'cart'], ['slaughter', '도축 요청', 'knife']].map(([k, n, i]) => { const e = ex(k); return `<button class="btn ${k === 'slaughter' ? 'danger' : ''}" data-act="bulk" data-k="${k}" ${e.ok.length ? '' : 'disabled'}>${ic(i, 14)}${n} ${e.ok.length}마리</button>${exTxt(e) ? `<span class="dim">제외: ${exTxt(e)}</span>` : ''}`; }).join('')}
    <button class="btn" data-act="bulkCancel" ${cancellable.length ? '' : 'disabled'}>일괄 취소 ${cancellable.length}마리</button>${startedN ? `<span class="dim">진행 중 도축 ${startedN}마리는 취소 불가</span>` : ''}</div>
    <p class="dim" style="margin:0">포획(현장 결박)과 운반(목적 우리 지정)은 별도 요청입니다. 도축은 이름·마릿수를 확인한 뒤 요청합니다.</p></div>` : '';
  const sp = Object.keys(SPECIES);
  return { title: '우리(목장) 관리', sub: '표지를 둔 벽·문 방 = 우리 · 합사·마릿수 무제한', body: `
    <div class="grid3"><div class="box well"><h3>우리 상태</h3><dl class="kv"><dt>운영</dt><dd class="ok-ink">운영 중(벽·문으로 둘러싸임)</dd><dt>공간</dt><dd>22칸 · 필요 공간 14.5 · <span class="ok-ink">과밀 없음</span></dd><dt>마릿수</dt><dd class="n">${all.filter(a => a.pen === 'pen' && !a.clinic).length}</dd></dl>
      <div class="row"><label><input type="checkbox" data-input="autoTreat" ${r.autoTreat ? 'checked' : ''}> 자동 치료</label><label><input type="checkbox" data-input="autoHarvest" ${r.autoHarvest ? 'checked' : ''}> 자동 채취</label></div></div>
      <div class="box well" style="grid-column:span 2"><h3>종별 설정(목표 마릿수 = 현황·초과 알림)</h3>${sp.map(k => `<div class="row" style="font-size:12px"><b style="width:44px">${SPECIES[k].n}</b><label><input type="checkbox" data-input="accept" data-k="${k}" ${r.accepts[k] ? 'checked' : ''}> 받기</label>
        <label><input type="checkbox" data-input="care" data-k="${k}" ${r.care[k] ? 'checked' : ''}> 자동 돌봄</label><span class="muted">목표</span><span class="stepper"><button class="btn" data-act="target" data-k="${k}" data-d="-1" aria-label="${SPECIES[k].n} 목표 줄이기">−</button><b class="n" style="width:22px;text-align:center">${r.target[k]}</b><button class="btn" data-act="target" data-k="${k}" data-d="1" aria-label="${SPECIES[k].n} 목표 늘리기">+</button></span>
        <span class="dim">현재 ${all.filter(a => a.sp === k && a.pen === 'pen').length}마리${all.filter(a => a.sp === k && a.pen === 'pen').length > r.target[k] ? ' · <span class="warn-ink">목표 초과</span>' : ''}</span></div>`).join('')}</div></div>
    ${bulk}
    <div class="box well"><h3>개체 목록</h3>${filters}<div class="critter-row head"><span></span><span>이름(변경 가능)</span><span>성장</span><span>길들임</span><span>건강</span><span>허기</span><span>현재 작업 · 요청</span><span></span></div>
      <ul class="list">${list.map(a => `<li><div class="critter-row" style="width:100%"><input type="checkbox" data-input="pick" data-id="${a.id}" ${S.pick.has(a.id) ? 'checked' : ''} aria-label="${esc(a.name)} 선택">
        <span class="nm"><input type="text" value="${esc(a.custom)}" placeholder="${SPECIES[a.sp].n}" data-input="rename" data-id="${a.id}" aria-label="이름(비우면 종 이름)" maxlength="16"></span><span>${a.stage}</span><span>${a.tame ? '길들임' : a.untameable ? '불가' : pct(a.tameProg || 0)}</span>
        <span class="${a.hp < 40 ? 'bad-ink' : a.hp < 100 ? 'warn-ink' : ''}">${a.hp}%${a.injury ? ' ' + a.injury : ''}</span><span class="n ${a.hunger < 30 ? 'bad-ink' : ''}">${a.hunger}</span>
        <span class="state-line">${a.clinic ? `<span class="chip warn">${clinicLine(a).split(' · ')[0]}</span>` : `<span class="dim">${esc(a.task)}</span>`}${reqChips(a)}${a.wild ? '<span class="chip">야생(우리 밖)</span>' : ''}</span>
        <span class="row">${a.req.slaughter === 'wait' || a.req.capture || a.req.carry ? `<button class="btn" data-act="cancelOne" data-id="${a.id}">요청 취소</button>` : ''}<button class="btn ghost" data-act="open" data-w="animal" data-id="${a.id}" aria-label="${esc(a.name)} 상세">${ic('right', 12)}</button></span></div></li>`).join('')}</ul></div>`,
    foot: `<span class="note">예시 데이터 · 자동 도축·도축 금지 설정은 기획에서 폐기</span><button class="btn primary" data-act="close">닫기</button>` };
}
function highRisk(a) { return a.seq > 0 || bestChance(a) < .5; }
const surgeons = () => C.filter(c => !c.young && !c.away && !c.deployed && !c.locked.includes('nursing'));
// 수술 예상치(사용자 확정 잠정안 A): 50% + 의료×2.5%p − 부상 보정(체력 40%→0, 0%→20%p) − 후유증×10%p, 5~95%. 실패 시 사망 10% + 후유증×5%p.
const chanceFor = (a, c) => clamp(.5 + c.skills.medicine * .025 - clamp(1 - a.hp / 100 / .4, 0, 1) * .2 - a.seq * .1, .05, .95);
const bestChance = a => Math.max(0, ...surgeons().map(c => chanceFor(a, c)));
const deathFor = (a, c) => (1 - chanceFor(a, c)) * Math.min(1, .1 + a.seq * .05);
function winAnimal(arg) {
  const a = animal(+arg.id); if (!a || a.dead) return { title: '개체', body: '<div class="empty">이 개체는 더 이상 없습니다.</div>', foot: `<button class="btn primary" data-act="close">닫기</button>` };
  const t = S.tab.animal || '개요', sp = SPECIES[a.sp];
  const over = `<div class="grid2"><div class="box well"><h3>이름</h3><div class="row"><input type="text" value="${esc(a.custom)}" placeholder="${SPECIES[a.sp].n}" data-input="rename" data-id="${a.id}" maxlength="16" aria-label="개체 이름"><span class="dim">비우면 기본 이름(종). 알림은 이름(종)으로 표시</span></div>
      <dl class="kv"><dt>종 · 성향</dt><dd>${sp.n} · ${sp.temper}</dd><dt>성장</dt><dd>${a.stage}(나이·노령 상태만 표시, 사망 시점은 표시 안 함)</dd><dt>길들임</dt><dd>${a.tame ? '길들임(영구)' : a.untameable ? '길들이기 불가 종' : `진행 ${pct(a.tameProg || 0)}(돌봄 효과 동안 증가, 끊기면 감소)`}</dd>
      <dt>건강</dt><dd>${a.hp}%${a.injury ? ' · ' + a.injury : ''}${a.seq ? ` · <span class="bad-ink">후유증 ${a.seq}: 재수술 성공률 −${a.seq * 10}%p · 실패 시 사망 +${a.seq * 5}%p(우리를 옮겨도 유지)</span>` : ''}</dd><dt>허기</dt><dd class="n">${a.hunger}</dd><dt>현재 작업</dt><dd>${a.clinic ? clinicLine(a) : esc(a.task)}</dd></dl></div>
    <div class="box well"><h3>요청</h3><div class="state-line">${reqChips(a) || '<span class="dim">요청 없음</span>'}</div>
      <div class="row"><button class="btn" data-act="animalReq" data-r="capture" data-id="${a.id}" ${a.bound || a.clinic ? 'disabled' : ''}>${a.req.capture ? '포획 취소' : '포획 요청'}</button>
      <button class="btn" data-act="animalReq" data-r="carry" data-id="${a.id}" ${!(a.bound || a.hp < 20) || a.clinic && a.clinic.phase !== 'ready' ? 'disabled' : ''}>${a.req.carry ? '운반 취소' : '운반 요청'}</button>
      <button class="btn danger" data-act="animalReq" data-r="slaughter" data-id="${a.id}" ${a.clinic || a.req.slaughter === 'started' ? 'disabled' : ''}>${a.req.slaughter === 'started' ? '도축 중(취소 불가)' : a.req.slaughter ? '도축 취소' : '도축 요청'}</button>
      <button class="btn" data-act="release" data-id="${a.id}" ${a.clinic ? 'disabled title="치료 시설 안에서는 방생할 수 없습니다"' : ''}>방생</button></div>
      ${a.clinic ? '<p class="dim" style="margin:0">치료 시설 안 동물은 도축·방생할 수 없습니다(우리로 복귀한 뒤).</p>' : ''}</div></div>`;
  return { title: esc(a.name), sub: `${sp.n} · ${a.stage}${a.wild ? ' · 야생' : ''}`, body: tabs('animal', ['개요', '의료']) + (t === '의료' ? medical(a) : over), foot: `<span class="note">예시 데이터</span><button class="btn primary" data-act="close">닫기</button>` };
}
function medical(a) {
  const stockLine = `<div class="row"><span class="chip ${stock('medicine') < 1 ? 'bad' : ''}">동물용 의약품 사용 가능 ${stock('medicine')}(예약 ${reserved('medicine')})${stock('medicine') < 1 ? ' · 부족' : ''}</span><span class="chip ${stock('kit') < 1 ? 'bad' : ''}">수술 키트 사용 가능 ${stock('kit')}(예약 ${reserved('kit')})${stock('kit') < 1 ? ' · 부족' : ''}</span>
    ${stock('kit') < 1 || stock('medicine') < 1 ? `<button class="btn" data-act="open" data-w="production" data-id="med">약제대 생산 목록</button>` : ''}<button class="btn ghost" data-act="demoStock" title="게임 기능이 아닌 시안 확인용 조작">시안 조작: 키트·의약품 +1</button></div>`;
  const pick = verb => `<ul class="list">${surgeons().sort((x, y) => chanceFor(a, y) - chanceFor(a, x)).map(c => `<li><span class="grow"><b>${c.name}</b> <span class="dim">의료 ${c.skills.medicine}</span></span><span>예상 성공 <b class="n">${pct(chanceFor(a, c))}</b> · 사망 위험 <b class="n">${pct(deathFor(a, c))}</b></span>
    <button class="btn ${a.surgeon === c.id ? 'on' : ''}" data-act="surgeon" data-id="${a.id}" data-c="${c.id}" data-v="${verb}" ${stock('kit') < 1 && verb !== '집도의 교대' ? 'disabled title="수술 키트 부족"' : ''}>${verb}</button></li>`).join('')}</ul>`;
  if (!a.injury && !a.clinic) return stockLine + '<div class="empty">부상 없음 · 치료할 것이 없습니다.</div>';
  if (a.injury === '경상' && !a.clinic) return stockLine + `<div class="box well"><h3>경상 — 투약</h3><p class="muted" style="margin:0">목장에서 자연 회복하며 동물용 의약품 투약으로 빨라집니다. 투약 효과는 <b>투약 당시 있던 경상</b>에만 적용되고, 이후 생긴 경상은 보통 속도로 회복합니다. 효과 중에는 중복 투약하지 않습니다.</p>
    <div class="row">${a.medicated ? `<span class="chip ok">투약 효과 중 · ${a.medicated}초</span>` : a.dosing ? `<span class="chip info">투약 대기 · 의약품 1개 예약</span><button class="btn" disabled>투약 지시</button>` : `<button class="btn primary" data-act="dose" data-id="${a.id}" ${stock('medicine') < 1 ? 'disabled' : ''}>투약 지시</button>`}${!a.dosing && stock('medicine') < 1 ? '<span class="bad-ink">의약품이 없어 투약을 시작할 수 없습니다 — 자연 회복은 계속됩니다.</span>' : ''}</div></div>`;
  const c = a.clinic || { phase: 'none' };
  const phase = {
    none: `<div class="box well"><h3>중상 — 수술 필요</h3><p class="muted" style="margin:0">빈 치료대와 수술 키트를 모두 확보(예약)한 뒤 운반합니다. ${highRisk(a) ? '<b class="warn-ink">고위험 첫 수술 또는 재수술 — 집도의를 지정해야 시작합니다.</b>' : '가장 유능한 의료 장수 기준 예상 성공 ' + pct(bestChance(a)) + ' — 자동 치료로 시작'}</p>${pick('집도의 지정')}</div>`,
    wait: `<div class="box well"><h3>치료대 · 수술 대기</h3><p class="muted" style="margin:0">${highRisk(a) && !a.surgeon ? '<b class="warn-ink">집도의 지정 대기</b> — 대기 중 배고픔은 진행합니다.' : a.surgeon ? `집도의 ${byId(a.surgeon).name} 지정됨.` : '의료 장수 대기 중.'}</p>${pick('집도의 지정')}
      <div class="row"><button class="btn primary" data-act="surgeryStart" data-id="${a.id}" ${(highRisk(a) && !a.surgeon) || stock('kit') < 1 ? 'disabled' : ''}>수술 시작(시안 진행)</button>${stock('kit') < 1 ? '<span class="bad-ink">수술 키트가 없어 시작할 수 없습니다.</span>' : ''}</div></div>`,
    surgery: `<div class="box well"><h3>수술 중 ${pct(c.prog)}</h3><span class="meter"><i style="transform:scaleX(${c.prog})"></i></span><p class="muted" style="margin:0">집도의 ${c.by ? byId(c.by).name : '<span class="warn-ink">이탈 — 진행 일시정지(배고픔 진행), 다른 의료 장수가 진행도·키트를 이어받음</span>'} · 실제 수술 중 배고픔 정지 · 유저 중도 취소 불가</p>
      <p class="dim" style="margin:0">교대 시 성공률은 장수별 실제 진행 작업량 비중으로 의료 능력을 계산합니다(대기 시간 제외, 마지막 집도의만 쓰지 않음).</p>${c.by ? `<button class="btn" data-act="surgeonLeave" data-id="${a.id}">집도의 이탈(시안)</button>` : pick('집도의 교대')}</div>`,
    recovery: `<div class="box well"><h3>회복 중</h3><p class="muted" style="margin:0">수술 성공 — 경상·중상 완치, 후유증 해소. 치료대에서 활동·식사 불가로 회복(배고픔 정지, 나이·자연사는 진행). 남은 <b class="n">${c.left}</b>초(시안). 완료하면 원래 우리로 운반을 요청합니다.</p></div>`,
    failed: `<div class="box well"><h3 class="bad-ink">수술 실패 — ${c.result}</h3><p class="muted" style="margin:0">후유증 ${a.seq}회 누적 → 재수술 성공률↓·사망 위험↑. 자동 재시도하지 않습니다. 치료대에서 재수술 또는 우리 복귀를 고르세요(대기 중 배고픔 진행).</p>${pick('재수술')}
      <div class="row"><button class="btn" data-act="returnPen" data-id="${a.id}">우리 복귀(부상 유지)</button></div></div>`,
    ready: `<div class="box well"><h3>회복 완료</h3><p class="muted" style="margin:0">원래 우리로 운반 대기 — 운반 장수가 데려갈 때까지 치료대 한 자리를 차지합니다(배고픔 진행).</p></div>`,
  }[a.clinic ? c.phase : 'none'];
  return stockLine + phase;
}
function winFeeder() {
  return { title: '먹이통', sub: '운반 장수가 허용 먹이를 목표량까지 채움 · 우리 전용 아님', narrow: 1, body: `<div class="row"><span class="muted">운반 우선순위</span><span class="stepper"><button class="btn" data-act="feederPrio" data-d="-1" aria-label="낮추기">−</button><b class="n" style="width:20px;text-align:center">${S.feederPrio}</b><button class="btn" data-act="feederPrio" data-d="1" aria-label="높이기">+</button></span><span class="dim">1~5 · 높은 통부터 보충</span></div>
    <ul class="list">${S.feeder.map((f, i) => `<li><label class="grow"><input type="checkbox" data-input="feedAllow" data-i="${i}" ${f.allow ? 'checked' : ''}> <b>${f.n}</b></label><span class="dim">재고 <b class="n">${f.stock}</b> / 목표</span>
      <span class="stepper"><button class="btn" data-act="feedTarget" data-i="${i}" data-d="-5" aria-label="${f.n} 목표 −5">−</button><b class="n" style="width:26px;text-align:center">${f.target}</b><button class="btn" data-act="feedTarget" data-i="${i}" data-d="5" aria-label="${f.n} 목표 +5">+</button></span>
      <button class="btn" data-act="feedDump" data-i="${i}" ${f.stock < 1 ? 'disabled' : ''}>반출</button></li>`).join('')}</ul>
    <p class="dim" style="margin:0">허용을 끄면 그 먹이 전부, 목표를 낮추면 초과분이 바로 바닥에 떨어집니다. 귀한 먹이는 기본 제외. 반출 후 재보충을 피하려면 목표량도 함께 낮추세요.</p>`, foot: `<button class="btn primary" data-act="close">닫기</button>` };
}
function winButcher() {
  const b = S.butcher;
  return { title: '도축대', sub: '등록 순서대로 해체 · 요리 능력은 속도만', narrow: 1, body: `<div class="row"><button class="btn ${b.paused ? 'primary' : ''}" data-act="butcherPause">${b.paused ? '재개(진행도 이어서)' : '일시정지'}</button><span class="dim">${b.paused ? '일시정지 — 사체·진행도 보존, 새 운반·해체 없음. 이미 운반 중인 사체는 받습니다.' : '운반 장수가 허용 종 사체를 옮기고 요리 장수가 해체합니다.'}</span></div>
    <div class="row">${Object.keys(SPECIES).map(k => `<label class="chip ${b.allow[k] ? 'ok' : ''}"><input type="checkbox" data-input="butcherAllow" data-k="${k}" ${b.allow[k] ? 'checked' : ''}> ${SPECIES[k].n} 허용</label>`).join('')}</div>
    <p class="dim" style="margin:0">허용을 해제하면 대기 중인 그 종 사체는 도축대 옆 바닥에 사체로 내려놓습니다(이미 시작한 해체는 끝까지).</p>
    <h3 style="margin:0;font-size:13px;color:var(--select)">대기열 ${b.queue.length}구</h3>${b.queue.length ? `<ol class="list queue">${b.queue.map((e, i) => `<li><span class="grow">${i + 1}. ${SPECIES[e.sp].n}${e.baby ? '(새끼 · 산출 50%)' : ''} · 고기 ${e.meat} · 갑각 ${e.chitin}</span>${i === 0 && b.prog > 0 ? `<span class="n">해체 ${pct(b.prog)}</span>` : ''}</li>`).join('')}</ol>` : '<div class="empty">대기 사체 없음</div>'}
    <p class="dim" style="margin:0">완료하면 생산물을 도축대 옆 바닥에 한 번에 둡니다(창고 포화는 해체를 막지 않음). 순서 변경·개별 제외 기능은 없습니다.</p>`, foot: `<button class="btn primary" data-act="close">닫기</button>` };
}

/* 확인 대화상자 */
const groupNames = list => Object.entries(list.reduce((m, a) => (m[a.name] = (m[a.name] || 0) + 1, m), {})).map(([n, c]) => `${esc(n)} ${c}마리`).join(', ');
function confirmBox(title, html, okLabel, onOk, danger, blocked) {
  const d = $('#confirm'); $('#confirmTitle').textContent = title; $('#confirmBody').innerHTML = html; const ok = $('#confirmOk');
  ok.textContent = okLabel; ok.className = 'btn ' + (danger ? 'danger' : 'primary'); ok.disabled = !!blocked;
  d.onclose = () => { if (d.returnValue === 'ok') onOk(); }; d.returnValue = ''; d.showModal();
}
function askBulk(kind, list) {
  const e = exclusions(kind, list), n = { capture: '포획', carry: '운반', slaughter: '도축' }[kind];
  if (!e.ok.length) { feedback(`${n} 요청할 수 있는 개체가 없습니다.`); return; }
  const ex = Object.entries(e.out).map(([w, c]) => `${w} ${c}마리`).join(', ');
  const species = [...new Set(e.ok.map(a => a.sp))], valid = PENS.filter(p => species.every(sp => p.acc()[sp]));
  const dest = kind !== 'carry' ? '' : valid.length ? `<label class="row">목적 우리 <select id="carryDest">${valid.map(p => `<option>${p.n}</option>`).join('')}</select></label><p class="dim" style="margin:0">선택한 모든 종(${species.map(k => SPECIES[k].n).join('·')})을 받는 우리만 목적지입니다. 과밀은 제한 사유가 아닙니다.</p>`
    : `<p class="bad-ink" style="margin:0">선택한 모든 종(${species.map(k => SPECIES[k].n).join('·')})을 받는 공통 목적지가 없습니다 — 선택을 나눠 요청하세요.</p>`;
  confirmBox(`${n} 요청 — ${e.ok.length}마리`, `<div class="names">${groupNames(e.ok)}</div>${ex ? `<p class="warn-ink" style="margin:0">제외: ${ex}</p>` : ''}${dest}
    ${kind === 'slaughter' ? '<p class="muted" style="margin:0">사육 장수가 현장에서 도축합니다(야생은 먼저 포획). 실제 도축을 시작하면 취소할 수 없습니다.</p>' : ''}`, `${e.ok.length}마리 ${n} 요청`, () => {
    const d = $('#carryDest')?.value || '우리(목장)';
    e.ok.forEach(a => { a.req[kind] = kind === 'slaughter' ? 'wait' : kind === 'carry' ? d : true; if (kind === 'slaughter') a.reqAt = S.sim; });
    feedback(`${e.ok.length}마리 ${n} 요청${ex ? ` · 제외 ${ex}` : ''}`); renderAll();
  }, kind === 'slaughter', kind === 'carry' && !valid.length);
}

/* ───────── 7) 시간 흐름(시안 시뮬레이션, 1초마다) ───────── */
function tick() {
  if (S.paused || S.negoPause) return;
  const dt = S.speed; S.sim += dt;
  S.taxIn -= dt; if (S.taxIn <= 0) { const w = weekly(); S.taxIn += 75; toast({ kind: 'done', icon: 'check', title: '주간 납세', text: `식량 +${w.food} · 재료 +${w.mat}(${S.pop.focus})` }); }
  if (S.sim % 4 < dt) S.clockMin = (S.clockMin + 1) % 900;
  C.forEach(c => { if (c.skillCd > 0) c.skillCd = Math.max(0, c.skillCd - dt); });
  Object.entries(S.orders).forEach(([id, list]) => { const o = list[0]; if (!o) return; o.prog += .02 * dt; if (o.prog >= 1) { o.prog = 0; if (id === 'med') mat(o.item === 0 ? 'medicine' : 'kit').have++; if (o.mode === 'count' && --o.n <= 0) list.shift(); } });
  // 동물: 도축 요청 처리(대기 → 포획/도축 시작 → 사체), 치료, 포식(사전 경고 없음, 실제 피해 후 처음 먹을 때만 알림)
  live().forEach(a => {
    if (a.req.slaughter === 'wait' && S.sim - a.reqAt > 6) { a.req.slaughter = 'started'; a.task = '도축 중'; }
    else if (a.req.slaughter === 'started' && S.sim - a.reqAt > 12) { a.dead = true; S.floorCarcass.push({ sp: a.sp, x: a.x, y: a.y, meat: a.stage === '새끼' ? 6 : 12, baby: a.stage === '새끼', at: S.sim }); if (S.sel.id === a.id) S.sel = { kind: 'none' }; }
    if (a.req.capture && !a.bound && S.sim % 8 < dt) { a.bound = true; a.req.capture = false; a.task = '현장 결박'; toast({ kind: 'done', icon: 'rope', title: `${a.name} 결박`, text: '개체를 선택해 운반할 우리를 지정하세요.', go: () => focusAnimal(a) }); }
    if (a.req.carry && (a.bound || a.hp < 20) && !a.clinic && S.sim % 9 < dt) { a.req.carry = false; a.bound = false; a.wild = false; a.pen = 'pen'; a.x = 940 + Math.random() * 140; a.y = 450 + Math.random() * 100; a.task = '배회'; }
    if (a.medicated) a.medicated = Math.max(0, a.medicated - dt);
    if (a.dosing && (a.dosing -= dt) <= 0) { a.dosing = 0; reservedMap.medicine--; mat('medicine').have--; a.medicated = 120; } // 예약분을 완료 때 한 번만 소비
    const c = a.clinic; if (!c) return;
    if (c.phase === 'surgery' && c.by) { c.work += dt; c.skillWork += dt * byId(c.by).skills.medicine; c.prog = Math.min(1, c.prog + .05 * dt); if (c.prog >= 1) finishSurgery(a); }
    else if (c.phase === 'recovery') { c.left = Math.max(0, c.left - dt); if (!c.left) { c.phase = 'ready'; toast({ kind: 'done', icon: 'check', title: `${a.name} 회복 완료`, text: '원래 우리로 운반을 요청했습니다.', go: () => focusAnimal(a) }); } }
    else if (c.phase === 'ready' && S.sim % 7 < dt) { a.clinic = null; a.x = 980; a.y = 480; a.task = '배회'; } // 위치·시설 상태만 바꾼다. 부상·체력·후유증은 그대로(완치는 수술 성공 때만)
  });
  S.floorCarcass = S.floorCarcass.filter(k => { if (S.butcher.paused || !S.butcher.allow[k.sp] || S.sim - k.at < 5) return true; S.butcher.queue.push({ sp: k.sp, meat: k.meat, chitin: k.baby ? 0 : 1, baby: k.baby }); return false; });
  if (!S.butcher.paused && S.butcher.queue.length) { S.butcher.prog += .08 * dt; if (S.butcher.prog >= 1) { S.butcher.prog = 0; S.butcher.queue.shift(); } }
  tickPredation(dt);
  renderAll(true);
}
function tickPredation(dt) {
  if (!S.events.predation && S.sim > 36) { const prey = animal(7), spider = animal(14); if (prey && !prey.dead && !prey.clinic) { prey.hp = Math.max(0, prey.hp - 34 * dt / 2); prey.task = '쫓기는 중'; if (prey.hp <= 0) { prey.dead = true; S.events.predation = true;
    if (S.sel.id === 7) S.sel = { kind: 'none' }; toast({ kind: 'warn', icon: 'paw', title: '포식', text: `${prey.name}이(가) ${spider.name}에게 잡아먹혔습니다.`, go: () => { setView(980, 470); S.sel = { kind: 'animal', id: 14 }; } }); } } }
}
function finishSurgery(a) {
  const c = a.clinic, med = c.work ? c.skillWork / c.work : 0, chance = clamp(.5 + med * .025 - clamp(1 - c.pre / .4, 0, 1) * .2 - a.seq * .1, .05, .95);
  if (Math.random() < chance) { a.hp = 100; a.seq = 0; c.phase = 'recovery'; c.left = Math.max(8, Math.round(30 * (1 - c.pre))); a.injury = null; a.surgeon = null; toast({ kind: 'done', icon: 'check', title: `${a.name} 수술 성공`, text: '치료대에서 회복합니다.', go: () => focusAnimal(a) }); return; }
  const death = Math.min(1, .1 + a.seq * .05), r = Math.random(); a.seq++;
  if (r < death) { a.dead = true; S.floorCarcass.push({ sp: a.sp, x: 914, y: 600, meat: 8, at: S.sim }); toast({ kind: 'crisis', icon: 'warn', title: `${a.name} 수술 중 사망`, text: '사체는 치료대 옆 바닥에 남고 자리가 비었습니다.', go: () => setView(860, 590) }); if (S.sel.id === a.id) S.sel = { kind: 'none' }; return; }
  c.phase = 'failed'; c.result = r < death + .3 ? '부상 악화' : '부상 유지'; if (c.result === '부상 악화') a.hp = Math.max(1, a.hp - 10); a.surgeon = null;
  toast({ kind: 'warn', icon: 'warn', title: `${a.name} 수술 실패 — ${c.result}`, text: `후유증 ${a.seq} · 재수술 또는 우리 복귀를 고르세요.`, go: () => focusAnimal(a) });
}
function focusAnimal(a) { setView(a.x, a.y); S.sel = { kind: 'animal', id: a.id }; renderAll(); }
function setView(x, y) { // 알림 '위치': 알림은 그대로 두고 카메라 이동(220ms ease-out)
  const from = [...S.view], to = [clamp(x - 360, 0, 720), clamp(y - 165, 0, 330), 720, 330], t0 = performance.now(), reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const step = now => { const k = reduce ? 1 : Math.min(1, (now - t0) / 220), e = 1 - Math.pow(1 - k, 3); S.view = from.map((v, i) => v + (to[i] - v) * e); $('#scene').setAttribute('viewBox', S.view.join(' ')); renderMini(); if (k < 1) requestAnimationFrame(step); };
  requestAnimationFrame(step);
}

/* ───────── 8) 이벤트 연결 ───────── */
// 다시 그려도 키보드 포커스·스크롤 위치를 잃지 않게 한다. 시간 흐름(tick)으로 다시 그릴 때 창 안 입력란을 쓰는 중이면 창은 건너뛴다.
const keyOf = el => el && el.dataset && (el.dataset.act || el.dataset.input) ? JSON.stringify(Object.entries(el.dataset).sort()) : null;
function renderAll(fromTick) {
  const active = document.activeElement, key = keyOf(active), typing = active && active.closest?.('#window') && /INPUT|SELECT|TEXTAREA/.test(active.tagName);
  const rl = $('#rlist')?.scrollLeft || 0, wb = $('#window .win-body')?.scrollTop || 0;
  renderMenu(); renderRoster(); renderRes(); renderLeft(); renderClock(); renderToasts(); renderScene(); renderMini(); renderCenter(); renderCmd();
  if (S.win && !(fromTick && typing)) { renderWin(); const b = $('#window .win-body'); if (b) b.scrollTop = wb; }
  if (!$('#pop').hidden) renderPop();
  const r = $('#rlist'); if (r) r.scrollLeft = rl;
  if (key && !(fromTick && typing) && (document.activeElement === document.body || !document.activeElement?.isConnected)) for (const el of document.querySelectorAll('[data-act],[data-input]')) if (keyOf(el) === key) { el.focus({ preventScroll: true }); break; }
}
function select(kind, extra) { S.sel = { kind, ...extra }; renderAll(); }
const ACT = {
  open: d => openWin(d.w, d), close: () => closeWin(), say: d => feedback(d.t), menu: () => feedback('메뉴(설정·저장)는 이번 시안 범위 밖입니다.'),
  preview: d => { const k = d.k; if (k === 'none') select('none'); if (k === 'commander') select('commander', { ids: ['gw'] }); if (k === 'multi') select('multi', { ids: ['gw', 'yb', 'jg'] });
    if (k === 'deployed') select('deployed', { ids: ['jw'] }); if (k === 'building') select('building', { id: 'dorm' }); if (k === 'animal') select('animal', { id: 5 }); },
  selC: (d, el, e) => { const c = byId(d.id); if (S.prioMode) return;
    if (e.shiftKey && (S.sel.ids || []).length && !c.young) { const ids = new Set(S.sel.ids); ids.has(c.id) ? ids.delete(c.id) : ids.add(c.id); const arr = [...ids]; return select(arr.length > 1 ? 'multi' : byId(arr[0])?.deployed ? 'deployed' : arr.length ? 'commander' : 'none', { ids: arr }); }
    select(c.deployed ? 'deployed' : 'commander', { ids: [c.id] }); },
  multiPick: (d, el, e) => { if (e.shiftKey) { const ids = S.sel.ids.filter(x => x !== d.id); return select(ids.length > 1 ? 'multi' : 'commander', { ids }); } ACT.selC(d, el, { shiftKey: false }); },
  selB: d => { if (S.prioMode) return ACT.prioTarget(d); select('building', { id: d.id }); }, selA: d => { if (S.prioMode) return ACT.prioTarget({ id: 'a' + d.id }); select('animal', { id: +d.id }); },
  selNone: () => { if (!S.prioMode) select('none'); },
  prioMode: () => { S.prioMode = !S.prioMode; renderAll(); }, prioLevel: () => { S.prioLevel = S.prioLevel >= 10 ? 1 : S.prioLevel + 1; renderAll(); },
  prioTarget: d => { if (!S.prioMode) return; S.targetPrio[d.id] === S.prioLevel ? delete S.targetPrio[d.id] : S.targetPrio[d.id] = S.prioLevel; feedback(S.targetPrio[d.id] ? `대상 우선순위 ${S.prioLevel === 10 ? '노란 경보' : S.prioLevel} 지정` : '지정 해제'); renderAll(); },
  red: () => { S.red = !S.red; if (S.red) toast({ kind: 'crisis', icon: 'siren', title: '빨간 경보', text: '식사·수면·치료받기·간호를 멈추고 작업합니다.', go: null, red: 1 }); else S.toasts = S.toasts.filter(t => !t.red); renderAll(); },
  rest: () => { const c = selCommanders()[0]; c.resting = !c.resting; feedback(c.injuries.length ? `${c.name}: 빈 침상이 있는 의무실로 갑니다.` : `${c.name}: 숙소로 쉬러 갑니다.`); renderAll(); },
  skill: () => { const c = selCommanders()[0]; if (c.skillCd > 0) return; c.skillCd = 15; feedback('강타 장전: 다음 타격 피해 2배'); renderAll(); },
  pause: () => { S.paused = !S.paused; renderAll(); }, speed: d => { S.speed = +d.v; S.paused = false; renderAll(); },
  filter: d => { S.filters[d.k] = !S.filters[d.k]; renderMini(); },
  scroll: d => $('#rlist').scrollBy({ left: +d.d * 180, behavior: 'smooth' }),
  pop: () => { const p = $('#pop'); p.hidden = !p.hidden; if (!p.hidden) { renderPop(); const r = $('#res [data-act=pop]').getBoundingClientRect(); p.style.left = Math.max(8, r.right - 260) + 'px'; p.style.top = r.bottom + 6 + 'px'; } renderRes(); },
  toastGo: d => { const t = S.toasts.find(x => x.id === +d.id); t?.go?.(); renderAll(); }, toastX: d => { S.toasts = S.toasts.filter(t => t.id !== +d.id || t.pinned); renderToasts(); },
  tab: d => { S.tab[d.w] = d.n; renderWin(); },
  duty: (d, el, e) => { const c = byId(d.id); const dir = e.type === 'contextmenu' ? -1 : 1; c.prio[d.k] = (c.prio[d.k] + dir + 6) % 6; renderWin(); $(`#window .cell[data-id="${d.id}"][data-k="${d.k}"]`)?.focus(); },
  focus: d => { S.pop.focus = d.v; renderAll(); }, policy: d => { S.pop.policy = d.v; renderAll(); },
  buildCat: d => { S.buildCat = +d.i; S.buildItem = null; renderWin(); }, buildItem: d => { S.buildItem = d.n; renderWin(); }, buildMat: d => { S.buildMat = d.k; renderWin(); },
  place: d => { closeWin(); S.place = d.n; const pb = $('#placebar'); pb.hidden = false; pb.innerHTML = `${ic('hammer', 14)}<b>${esc(d.n)}</b> 배치 중 — 좌클릭 1개 · Shift 유지 · Shift+드래그 연속(시안)<button class="btn" data-act="placeEnd">끝내기</button>`; },
  placeEnd: () => { $('#placebar').hidden = true; S.place = null; },
  renovate: d => { const b = bld(d.id); b.renovating = d.k; mat(d.k).have -= 30; feedback(`${b.name} 개보수 예정지 지정 — 새 재료 30 차감, 장수가 현장 작업`); renderAll(); },
  renovateCancel: d => { const b = bld(d.id); mat(b.renovating).have += 30; b.renovating = null; feedback('개보수 취소 — 새 재료 100% 반환'); renderAll(); },
  redevTarget: d => { S.redev = d.v; renderWin(); },
  mode: d => { S.mode = d.v; renderWin(); }, amount: d => { S.amount = clamp(S.amount + +d.d, 1, 99); renderWin(); },
  addOrder: d => { S.orders[d.id].push({ item: +d.i, mode: S.mode, n: S.amount, prog: 0 }); renderAll(); }, delOrder: d => { S.orders[d.id].splice(+d.i, 1); renderAll(); },
  nego: d => { const f = FACTIONS.find(x => x.id === d.id); startNego(f); },
  negoInvalid: d => { const f = FACTIONS.find(x => x.id === d.id); FACTIONS.splice(FACTIONS.indexOf(f), 1); S.toasts = S.toasts.filter(t => t.nego !== f.id); S.nego = null; S.negoPause = null; S.win = null; closeWinQuiet(); feedback('무효 협상을 확인했습니다 — 협상·알림 종료'); renderAll(); },
  negoAdd: d => { const v = $(`#window select[data-side="${d.side}"]`).value; S.nego[d.side].push([v, 10]); renderWin(); }, negoDel: d => { S.nego[d.side].splice(+d.i, 1); renderWin(); },
  propose: () => { const f = FACTIONS.find(x => x.id === S.nego.f), r = Math.random(), m = f.reaction;
    const res = m === '호의적' ? (r < .7 ? '수락' : '역제안') : m === '망설임' ? (r < .3 ? '수락' : r < .75 ? '역제안' : '거절') : (r < .1 ? '수락' : r < .4 ? '역제안' : '거절');
    if (res === '수락') { S.nego.done = true; f.held = false; S.toasts = S.toasts.filter(t => t.nego !== f.id); S.nego.log.push('상대: 수락 — 성립 직전 보유량 확인 후 즉시 교환'); }
    if (res === '역제안') { S.nego.get.forEach(x => x[1] = Math.round(x[1] * .8)); S.nego.log.push('상대: 역제안 — 받을 양을 줄여 다시 제시'); }
    if (res === '거절') { f.patience = Math.max(0, f.patience - 1); S.nego.log.push(f.patience ? '상대: 거절 — 인내가 줄었습니다' : '상대: 결렬 — 일정 시간 뒤 다시 협상할 수 있습니다'); }
    renderWin(); },
  bulk: d => askBulk(d.k, live().filter(a => S.pick.has(a.id))),
  bulkCancel: () => { let n = 0; live().filter(a => S.pick.has(a.id)).forEach(a => { if (a.req.capture || a.req.carry || a.req.slaughter === 'wait') n++; a.req.capture = false; a.req.carry = false; if (a.req.slaughter === 'wait') a.req.slaughter = null; }); feedback(`${n}마리 요청 취소(포획 후 취소하면 결박은 유지)`); renderAll(); },
  cancelOne: d => { const a = animal(+d.id); a.req.capture = false; a.req.carry = false; if (a.req.slaughter === 'wait') a.req.slaughter = null; feedback(`${a.name} 요청 취소`); renderAll(); },
  animalReq: d => { const a = animal(+d.id), r = d.r;
    if (a.req[r]) { if (r === 'slaughter' && a.req.slaughter === 'started') return feedback('실제 도축을 시작해 취소할 수 없습니다.'); a.req[r] = r === 'slaughter' ? null : false; feedback(`${a.name} ${r === 'capture' ? '포획' : r === 'carry' ? '운반' : '도축'} 요청 취소`); return renderAll(); }
    askBulk(r, [a]); },
  release: d => { const a = animal(+d.id); if (a.clinic) return feedback('치료 시설 안에서는 방생할 수 없습니다.');
    confirmBox(`방생 — 1마리`, `<div class="names">${esc(a.name)}</div><p class="muted" style="margin:0">확인하면 즉시 맵에서 사라집니다. 사체·생산물이 없고 관련 작업이 취소됩니다.</p>`, '방생', () => { a.released = true; S.pick.delete(a.id); if (S.sel.id === a.id) S.sel = { kind: 'none' }; if (S.win === 'animal') closeWin(); renderAll(); }, true); },
  dose: d => { const a = animal(+d.id); if (a.dosing || a.medicated || stock('medicine') < 1) return; // 대기 중·효과 중 추가 요청 금지
    reservedMap.medicine++; a.dosing = 3; feedback(`${a.name} 투약 지시 — 의약품 1개 예약(게임 시간으로 진행)`); renderAll(); },
  surgeon: d => { const a = animal(+d.id);
    if (d.v === '집도의 교대') { a.clinic.by = d.c; return renderAll(); }
    if (d.v === '재수술') { confirmBox('재수술 확인', `<p style="margin:0">${esc(a.name)} · 집도의 ${byId(d.c).name}</p><p class="muted" style="margin:0">예상 성공 <b class="n">${pct(chanceFor(a, byId(d.c)))}</b> · 사망 위험 <b class="n">${pct(deathFor(a, byId(d.c)))}</b> · 새 수술 키트 1개 필요</p>`, '재수술 지시', () => { a.surgeon = d.c; a.clinic.phase = 'wait'; renderAll(); }); return; }
    a.surgeon = d.c; if (!a.clinic) { a.clinic = { phase: 'wait' }; a.task = '치료대로 운반'; } feedback(`${a.name} 집도의 ${byId(d.c).name} 지정`); renderAll(); },
  surgeryStart: d => { const a = animal(+d.id); if (stock('kit') < 1) return; mat('kit').have--; a.clinic = { phase: 'surgery', prog: 0, by: a.surgeon || surgeons().sort((x, y) => chanceFor(a, y) - chanceFor(a, x))[0].id, work: 0, skillWork: 0, pre: a.hp / 100 }; renderAll(); },
  surgeonLeave: d => { const a = animal(+d.id); a.clinic.by = null; renderAll(); },
  returnPen: d => { const a = animal(+d.id); a.clinic = { phase: 'ready' }; feedback(`${a.name}: 원래 우리로 운반 요청(부상 유지)`); renderAll(); },
  target: d => { S.ranch.target[d.k] = Math.max(0, S.ranch.target[d.k] + +d.d); renderWin(); },
  feederPrio: d => { S.feederPrio = clamp(S.feederPrio + +d.d, 1, 5); renderWin(); },
  feedTarget: d => { const f = S.feeder[+d.i]; f.target = clamp(f.target + +d.d, 0, 200); if (f.stock > f.target) { feedback(`${f.n} 초과분 ${f.stock - f.target} 바닥에 떨어짐`); f.stock = f.target; } renderWin(); },
  feedDump: d => { const f = S.feeder[+d.i]; feedback(`${f.n} ${f.stock} 바닥에 반출 — 재보충을 피하려면 목표량도 낮추세요`); f.stock = 0; renderWin(); },
  butcherPause: () => { S.butcher.paused = !S.butcher.paused; renderAll(); },
  demoStock: () => { mat('kit').have++; mat('medicine').have++; feedback('시안 조작: 수술 키트·동물용 의약품 +1(게임 기능 아님)'); renderAll(); },
};
function closeWinQuiet() { const sc = $('#scrim'); sc.classList.remove('open'); sc.setAttribute('aria-hidden', 'true'); }
const INPUT = {
  // 슬라이더는 드래그 중 다시 만들지 않고 숫자 칸·수입 줄만 갱신한다.
  rate: el => { S.pop.rate = +el.value; el.setAttribute('aria-valuetext', el.value + '%'); $('#taxNum').value = el.value; $('#taxLine').innerHTML = taxLine(); renderRes(); },
  rateNum: el => { S.pop.rate = clamp(Math.round(+el.value || 0), 0, 50); renderAll(); },
  prefer: el => { S.pop.prefer = el.value; }, elder: el => { S.pop.elder = el.checked; },
  comp: el => { S.pop.comp = +el.value / 100; el.nextElementSibling.textContent = el.value + '%'; }, compDone: () => renderWin(),
  amount: el => { S.amount = clamp(Math.round(+el.value || 1), 1, 99); },
  rename: el => { const a = animal(+el.dataset.id); a.custom = el.value.trim().slice(0, 16); renderScene(); renderCenter(); },
  pick: el => { const id = +el.dataset.id; el.checked ? S.pick.add(id) : S.pick.delete(id); renderWin(); },
  flt: el => { S.flt[el.dataset.k] = el.value; renderWin(); }, sort: el => { S.sort = el.value; renderWin(); },
  autoTreat: el => S.ranch.autoTreat = el.checked, autoHarvest: el => S.ranch.autoHarvest = el.checked,
  accept: el => S.ranch.accepts[el.dataset.k] = el.checked, care: el => S.ranch.care[el.dataset.k] = el.checked,
  feedAllow: el => { const f = S.feeder[+el.dataset.i]; f.allow = el.checked; if (!f.allow && f.stock) { feedback(`${f.n} ${f.stock} 전부 바닥에 떨어짐`); f.stock = 0; } renderWin(); },
  butcherAllow: el => { const k = el.dataset.k, b = S.butcher; b.allow[k] = el.checked;
    if (!el.checked) { const keep = b.queue.filter((e, i) => e.sp !== k || i === 0 && b.prog > 0), dropped = b.queue.length - keep.length; b.queue = keep; for (let i = 0; i < dropped; i++) S.floorCarcass.push({ sp: k, x: 850 + i * 10, y: 556, meat: 10, at: S.sim + 999 }); if (dropped) feedback(`대기 사체 ${dropped}구를 도축대 옆 바닥에 내려놓았습니다.`); }
    renderAll(); },
  envoy: el => { S.nego.envoy = el.value; }, nego: el => { S.nego[el.dataset.side][+el.dataset.i][1] = Math.max(1, Math.round(+el.value || 1)); renderWin(); }, negoPick: () => {},
};
document.addEventListener('click', e => {
  if (!e.target.closest('#pop, [data-act=pop]') && !$('#pop').hidden) { $('#pop').hidden = true; renderRes(); }
  const t = e.target.closest('[data-act]'); if (!t || t.disabled) return;
  const fn = ACT[t.dataset.act]; if (fn) fn({ ...t.dataset }, t, e);
});
document.addEventListener('contextmenu', e => { const t = e.target.closest('.cell[data-act=duty]'); if (!t) return; e.preventDefault(); ACT.duty({ ...t.dataset }, t, e); });
document.addEventListener('keydown', e => {
  const r = e.target.closest?.('[role=radio][data-act]'); if (r && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); ACT[r.dataset.act]({ ...r.dataset }, r, e); return; }
  if (e.key === 'Escape') { if ($('#confirm').open) return; if (!$('#pop').hidden) { $('#pop').hidden = true; renderRes(); return; } if (S.place) return ACT.placeEnd(); if (S.win) { e.preventDefault(); closeWin(); } else if (S.prioMode) ACT.prioMode(); }
  if (e.key === ' ' && !S.win && !e.target.closest('input,select,button,textarea')) { e.preventDefault(); if (!S.negoPause) ACT.pause(); }
});
document.addEventListener('input', e => { const f = INPUT[e.target.dataset?.input]; if (f && (e.target.type === 'range' || e.target.type === 'text')) f(e.target); });
document.addEventListener('change', e => { const d = e.target.dataset?.input; if (d === 'comp') return INPUT.compDone(); const f = INPUT[d]; if (f && e.target.type !== 'range' && e.target.type !== 'text') f(e.target); });
$('#scrim').addEventListener('click', e => { if (e.target === e.currentTarget) closeWin(); });

// 초기 알림(예시)
toast({ kind: 'done', icon: 'check', title: '방 완성: 식당·부엌', text: '식탁·조리대를 벽으로 둘러쌈 · 식사 기분 +' });
toast({ kind: 'note', icon: 'chat', title: '협상 보류: 호박 군락', text: '알림에서 다시 엽니다. 보류 중 자원은 예약하지 않습니다.', go: () => startNego(FACTIONS[0]), goLabel: '열기', nego: 'hb' }); FACTIONS[0].held = true;
toast({ kind: 'warn', icon: 'cross', title: '집도의 지정 대기: 누에 #12', text: '고위험 수술 — 개체를 선택해 집도의를 지정하세요.', go: () => { setView(884, 604); S.sel = { kind: 'animal', id: 12 }; openWin('animal', { id: 12, tab: '의료' }); }, goLabel: '보기' });
toast({ kind: 'warn', icon: 'mood', title: '기분 경고', text: '장비 · 숙소 침대 부족으로 노숙', go: () => { setView(490, 200); S.sel = { kind: 'building', id: 'dorm' }; }, goLabel: '숙소' });
const inv = toast({ kind: 'crisis', icon: 'warn', title: '침입 경보', text: '서쪽 성벽 공격 중 · 징집소에서 출전 편성', go: () => { setView(300, 470); S.sel = { kind: 'deployed', ids: ['jw'] }; }, goLabel: '전장' }); inv.pinned = true;
renderAll();
setInterval(tick, 1000);
