/* 동물 의료 시안: 게임 시간 기준. 종·질병별 수치는 잠정값이며 Unity 데이터와 연결하지 않는다. */
'use strict';
const diseaseNames = { food: '식중독', wound: '상처 감염', illness: '질환', contagious: '전염성 질환' };
const symptomatic = a => !!a.disease && a.disease.elapsed >= .25;
const infectious = a => symptomatic(a) && a.disease.kind === 'contagious';
const diseaseSevere = a => symptomatic(a) && a.disease.severity >= .65;
function infectAnimal(a, kind) {
  if (!a.disease && diseaseNames[kind]) a.disease = { kind, elapsed: 0, severity: .1, medication: 0 };
}
function tickAnimalDisease(a, months) {
  const d = a.disease; if (!d) return;
  d.elapsed += months;
  const treated = Math.min(months, d.medication);
  d.medication = Math.max(0, d.medication - months);
  d.severity = clamp(d.severity + months * .22 - treated * .5, 0, 1);
  if (d.elapsed >= (d.kind === 'food' ? 1.2 : 4)) { a.disease = null; return; }
  if (infectious(a) && !a.clinic && !a.wild)
    live().filter(b => b !== a && b.pen === a.pen && b.sp === a.sp && !b.clinic && !b.wild)
      .forEach(b => { if (Math.random() < 1 - Math.exp(-months * .35)) infectAnimal(b, 'contagious'); });
}
const baseAnimalMedical = medical;
medical = function(a) {
  let body = baseAnimalMedical(a);
  const d = a.disease;
  body += `<div class="box well"><h3>질병</h3><p>${symptomatic(a) ? `${diseaseNames[d.kind]} · ${diseaseSevere(a) ? '중증 · 생산 중단' : '경증 · 생산 감소'}${infectious(a) ? ' · 전염 가능' : ''}` : '질병 증상 없음'}</p>
    <p class="dim">증상이 생긴 뒤 같은 우리의 같은 종에 전염됩니다. 치료대에서는 전염하지 않습니다. 격리는 다른 우리로 직접 운반합니다. 식중독은 경과를 기다립니다.</p>
    ${symptomatic(a) && d.kind !== 'food' ? `<button class="btn" data-act="diseaseDose" data-id="${a.id}" ${d.medication > 0 || a.dosing || stock('medicine') < 1 ? 'disabled' : ''}>${d.medication > 0 ? '투약 효과 중' : '질병 투약 지시'}</button>` : ''}
    <details><summary>시안 확인용 질병 발생</summary>${Object.entries(diseaseNames).map(([k,n]) => `<button class="btn" data-act="demoDisease" data-id="${a.id}" data-k="${k}">${n}</button>`).join('')}</details></div>`;
  return body;
};
ACT.demoDisease = b => { infectAnimal(animal(+b.id), b.k); renderAll(); };
ACT.diseaseDose = b => {
  const a = animal(+b.id), d = a.disease;
  if (!symptomatic(a) || d.kind === 'food' || d.medication > 0 || a.dosing || stock('medicine') < 1) return;
  a.diseaseDose = true; a.dosing = 3; reservedMap.medicine++; renderAll();
};
const baseHealthTick = tick;
tick = function() {
  if (S.paused || S.negoPause) return;
  const dt = S.speed;
  live().forEach(a => {
    tickAnimalDisease(a, dt / 75);
    if (a.diseaseDose && a.dosing <= dt) {
      if (a.disease && a.disease.kind !== 'food') a.disease.medication = 1;
      a.diseaseDose = false;
    }
  });
  baseHealthTick();
};
