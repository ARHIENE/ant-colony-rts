'use strict';
// 최신 물류·식품 기획의 인터랙티브 예시. 진행 버튼은 게임 시뮬레이션 대신 상태를 한 단계씩 보여준다.
const LG = {
  store: 0, diet: '공통', copy: null, selected: new Set(),
  stores: ['창고', '냉장고', '냉동고'].map((name, i) => ({ name, priority: 5, capacity: 100, power: true, cold: 1,
    allow: { 판재: !i, 날것: true, '간단한 요리': true, '좋은 요리': true, '썩은 식품': false },
    batches: [{ name: i ? '좋은 요리' : '날것', qty: 12, fresh: 25 }, { name: i ? '좋은 요리' : '판재', qty: 8, fresh: i ? 80 : null }] })),
  floor: [], diets: { 공통: { 날것: true, '간단한 요리': true, '좋은 요리': true } },
  jobs: [
    { id: 1, name: '침대 건설', need: 18, reserved: 12, delivered: 6, priority: 5, phase: '자재 부족', worker: 'jb' },
    { id: 2, name: '냉장고 건설', need: 30, reserved: 30, delivered: 0, priority: 5, phase: '운반 대기', worker: 'gw' },
    { id: 3, name: '벽 건설', need: 20, reserved: 20, delivered: 20, priority: 5, phase: '접근 불가', worker: null },
    { id: 4, name: '조리대 개보수', need: 30, reserved: 30, delivered: 30, priority: 5, phase: '적격 장수 없음', worker: null }
  ], compost: { input: 6, progress: 0, finished: 0, target: 20, fertilize: true }, next: 5
};
const lgButton = (action, label, data = '', disabled = false) => `<button class="btn" data-act="${action}" ${data} ${disabled ? 'disabled' : ''}>${label}</button>`;
const lgPrio = (value, key) => `<label class="row">우선순위 <select data-input="${key}" aria-label="우선순위">${Array.from({ length: 9 }, (_, i) => `<option ${value === i + 1 ? 'selected' : ''}>${i + 1}</option>`).join('')}</select><span class="dim">기본 5 · 높은 값 우선</span></label>`;
function lgDrop(store, name) {
  const items = store.batches.filter(b => !name || b.name === name);
  LG.floor.push(...items.map(b => ({ ...b, location: `${store.name} 주변` })));
  store.batches = store.batches.filter(b => !items.includes(b));
}
function lgStorage() {
  const st = LG.stores[LG.store], used = st.batches.reduce((n, b) => n + b.qty, 0);
  return `<div class="row">${LG.stores.map((s, i) => lgButton('lgStore', s.name, `data-i="${i}" aria-pressed="${i === LG.store}"`)).join('')}</div>
    <div class="grid2"><div class="box well"><h3>${st.name} <span class="dim">${used} / ${st.capacity}</span></h3>${lgPrio(st.priority, 'lgPriority')}
    <p class="dim">고정 용량 · 최대 보관량 조절 없음<br>보관한 물자는 창고 우선순위를 바꿔도 자동 이동하지 않습니다.</p>
    ${LG.store ? `<div class="row"><span class="chip ${st.power ? 'ok' : 'warn'}">${st.power ? st.cold === 1 ? '가동 중' : '냉각 중' : '정전 · 냉기 감소'} · 냉기 ${pct(st.cold)}</span>${lgButton('lgPower', '전력 전환(시안)')}${lgButton('lgCold', '냉각 시간 진행(시안)')}</div><p class="dim">${LG.store === 1 ? '부패 속도 감소' : '완전 냉각 시 부패 정지'} · 내부 재고에만 적용 · 신선도 복구 없음 · 바로 식사/요리에 사용</p>` : ''}
    <h3>허용 품목</h3>${Object.entries(st.allow).map(([name, allowed]) => `<label class="row"><input type="checkbox" data-input="lgAllow" data-name="${name}" ${allowed ? 'checked' : ''}>${name}</label>`).join('')}
    <p class="dim">허용을 끄면 해당 재고 전부가 주변 바닥에 나옵니다.</p>${lgButton('lgEmpty', '전체 비우기', '', !used)}</div>
    <div class="box well"><h3>재고 · 먼저 썩을 물량부터</h3><ul class="list">${[...st.batches].sort((a,b) => (a.fresh ?? 101) - (b.fresh ?? 101)).map(b => `<li><span class="grow"><b>${b.name}</b> ${b.qty}<br><span class="dim">${b.fresh === null ? '부패하지 않음' : `신선도 ${b.fresh}%`}</span></span>${lgButton('lgOut', '꺼내기', `data-name="${b.name}"`)}</li>`).join('') || '<li>재고 없음</li>'}</ul>
    ${lgButton('lgSpoil', '식품 부패 진행(시안)')}<p class="dim">이동해도 물량별 신선도 유지. 썩은 식품을 허용하지 않으면 바닥으로 배출됩니다.</p></div></div>
    <div class="box well"><h3>바닥 물자</h3><ul class="list">${LG.floor.map(b => `<li>${b.name} ${b.qty} · ${b.location} · ${b.fresh === null ? '비부패' : `신선도 ${b.fresh}%`}</li>`).join('') || '<li>없음</li>'}</ul>${lgButton('lgHaul', '운반 진행(시안)')}<p class="dim">받을 창고가 없으면 그대로 둡니다. 허용된 품목은 다시 입고될 수 있습니다.</p></div>`;
}
function lgFood() {
  const diet = LG.diets[LG.diet] || LG.diets.공통;
  return `<div class="grid2"><div class="box well"><h3>식단</h3><select data-input="lgDiet" aria-label="식단 대상">${['공통', ...C.map(c=>c.name)].map(n=>`<option ${n===LG.diet?'selected':''}>${n}</option>`).join('')}</select>
    <p class="dim">${LG.diet === '공통' ? '모든 장수의 기본 식단' : LG.diets[LG.diet] ? '개별 설정 사용 중' : '공통 식단 사용 중'}</p>
    ${Object.entries(diet).map(([n,v])=>`<label class="row"><input type="checkbox" data-input="lgDietAllow" data-name="${n}" ${v?'checked':''}>${n}</label>`).join('')}
    <div class="row">${lgButton('lgCopy','복사')}${lgButton('lgPaste','붙여넣기','',!LG.copy)}${lgButton('lgDietAll','모든 장수에게 적용')}${lgButton('lgResetDiet','공통 식단 따르기','',LG.diet==='공통')}</div></div>
    <div class="box well"><h3>먹는 순서</h3><p>접근 가능한 허용 정상 식품 → 좋은 요리 우선 → 같은 종류는 썩기 전 신선도가 낮은 순.</p><p>정상 식품을 먹을 수 없으면 썩은 식품을 먹습니다. 허기를 채우지만 기분이 나빠지고 식중독 위험이 있습니다.</p><p class="dim">바닥·창고 모두 이용 · 식중독 증상·치료는 질병 기획에서 결정 · 오염/청결 시스템 없음</p></div></div>
    <div class="box well"><h3>요리 주문</h3><p>레시피로 등급 결정 · 시작 시 재료 소비 · 완성품은 조리대 주변 바닥 · 재조리로 신선도 초기화 불가.</p>${lgButton('open','조리대 생산 목록','data-w="production" data-id="cook"')}<p class="dim">지정 수량 / 목표 재고 유지 / 계속 생산. 요리 레시피·수치는 예시입니다.</p></div>`;
}
function lgJobs() {
  const picked = LG.jobs.filter(j=>LG.selected.has(j.id));
  return `<div class="row">${lgButton('lgSelectAll','전체 선택')}${lgButton('lgClear','선택 해제')}<span>선택 ${picked.length}개</span>${lgButton('lgBulk','일시정지','data-mode="pause"',!picked.length)}${lgButton('lgBulk','재개','data-mode="resume"',!picked.length)}${lgButton('lgBulk','일괄 취소','data-mode="cancel"',!picked.length)}${lgPrio(5,'lgBulkPriority')}</div>
    <p class="dim">목록을 드래그하거나 체크하여 일괄 지정. 일괄 지시 뒤 하나씩 취소 가능. 이미 예약·배송한 재료는 우선순위를 바꿔도 빼앗지 않습니다.</p>
    <ul class="list lg-jobs">${LG.jobs.map(j=>`<li data-job="${j.id}"><label><input type="checkbox" data-input="lgPick" data-id="${j.id}" ${LG.selected.has(j.id)?'checked':''} aria-label="${j.name} 선택"></label><span class="grow"><b>${esc(j.name)}</b> <span class="chip">우선순위 ${j.priority}</span><br><span class="dim">필요 ${j.need} · 예약 ${j.reserved} · 도착 ${j.delivered}</span><br><span class="${j.paused?'warn-ink':'muted'}">${j.paused?'일시정지':j.phase}</span></span>${j.worker?lgButton('lgWorker',byId(j.worker).name,`data-id="${j.worker}"`):'<span class="dim">담당 없음</span>'}${lgButton('lgAdvance','진행(시안)',`data-id="${j.id}"`,j.paused || ['완료','접근 불가','적격 장수 없음'].includes(j.phase))}${lgButton('lgCancel','개별 취소',`data-id="${j.id}"`,j.phase==='완료')}</li>`).join('')}</ul>
    <p class="dim">청사진은 재료 부족에도 배치 → 예약·부분 배송 → 전량 도착 후 시공 시작 때 소비. 중단 원인별 대기, 정지는 도착 재료·진행도 유지/미수거 예약 해제. 건설 취소 재료 100% 반환.</p>
    <div class="box well"><h3>시설 철거·이동·개보수</h3><p>철거 시작 시 내용물 바닥 배출·생산 취소, 완료 시 건설 재료 70% 반환. 소비한 생산 재료는 반환하지 않습니다.</p><p>가구·장식만 회수·포장·운반·재설치. 내용물은 원래 자리 바닥에 배출. 회수 후 취소하면 포장품으로 남습니다.</p><p>개보수 시작 시 사용 중지·생산 진행 보존. 취소 시 새 재료 100%, 완료 시 기존 재료 70% 반환. 환자·포로가 있으면 비우기 전 철거·이동·개보수 불가.</p></div>`;
}
function lgCompost() {
  const c=LG.compost;
  return `<div class="grid2"><div class="box well"><h3>퇴비통</h3><p>썩은 식품 ${c.input} · 숙성 ${pct(c.progress)} · 완성 비료 ${c.finished}</p><label>비료 목표 재고 <input type="number" min="0" value="${c.target}" data-input="lgCompostTarget" aria-label="비료 목표 재고"></label><p class="dim">생산 목표입니다. 목표 도달·통이 가득 차면 새 투입만 멈춥니다. 숙성 중 작업자 불필요, 완성 비료는 운반 전까지 통에 남습니다.</p><div class="row">${lgButton('lgCompostAdvance','숙성 진행(시안)','',!(c.input+c.manure))}${lgButton('lgCompostOut','숙성 재료 꺼내기','',!(c.input+c.manure))}${lgButton('lgFertilizer','완성 비료 반출','',!c.finished)}</div></div><div class="box well"><h3>밭 · 비료</h3><label><input type="checkbox" data-input="lgFertilize" ${c.fertilize?'checked':''}>비료 사용</label><p>성장 주기당 한 번 적용. 비료가 없어도 계속 성장합니다. 성장 도중 사용하면 이후 성장만 빨라집니다.</p><p class="dim">숙성 중 꺼내면 진행도는 사라지고 썩은 식품으로 바닥에 나옵니다. 퇴비통 안 원료는 먹을 수 없습니다.</p></div></div>`;
}
function winLogistics() {
  const t=S.tab.logistics || '창고';
  return {title:'물류·식품 관리',sub:'최신 기획 · 예시 데이터',body:tabs('logistics',['창고','식단','건설·작업','퇴비·비료'])+({'창고':lgStorage,'식단':lgFood,'건설·작업':lgJobs,'퇴비·비료':lgCompost}[t])(),foot:'<span class="note">Unity 연동 없음 · 수치 잠정 · 시안 진행 버튼으로 상태 확인</span>'+lgButton('close','닫기')};
}
function lgCancelJob(j) {
  if(j.phase==='완료') return;
  if(j.delivered) LG.floor.push({name:j.material || '건설 재료',qty:j.delivered,fresh:null,location:j.name+' 주변'});
  if(j.building) bld(j.building).renovating=null;
  LG.jobs=LG.jobs.filter(x=>x!==j);LG.selected.delete(j.id);
}
Object.assign(ACT, {
  lgStore:d=>{LG.store=+d.i;renderWin();}, lgOut:d=>{lgDrop(LG.stores[LG.store],d.name);renderWin();},
  lgEmpty:()=>{lgDrop(LG.stores[LG.store]);renderWin();}, lgPower:()=>{const s=LG.stores[LG.store];s.power=!s.power;renderWin();},
  lgCold:()=>{const s=LG.stores[LG.store];s.cold=clamp(s.cold+(s.power?.25:-.25),0,1);renderWin();},
  lgSpoil:()=>{const s=LG.stores[LG.store]; if(LG.store===2 && s.cold===1)return feedback('냉동 중에는 부패가 멈춥니다.');let n=0;s.batches.forEach(b=>{if(b.fresh===null || b.name==='썩은 식품')return;b.fresh=Math.max(0,b.fresh-(LG.store && s.cold>0?5:25));if(!b.fresh){b.name='썩은 식품';n+=b.qty;}});if(!s.allow['썩은 식품'])lgDrop(s,'썩은 식품');if(n)toast({kind:'warn',icon:'food',title:'식품 부패',text:`${s.name} · ${n}개가 썩었습니다.`});renderAll();},
  lgHaul:()=>{for(const b of [...LG.floor]){for(const s of [...LG.stores].sort((a,b)=>b.priority-a.priority)){if(!s.allow[b.name])continue;const room=s.capacity-s.batches.reduce((n,x)=>n+x.qty,0);const qty=Math.min(room,b.qty);if(qty>0){s.batches.push({name:b.name,qty,fresh:b.fresh});b.qty-=qty;}if(!b.qty)break;}}LG.floor=LG.floor.filter(b=>b.qty);renderWin();},
  lgCopy:()=>{LG.copy={...(LG.diets[LG.diet]||LG.diets.공통)};renderWin();},lgPaste:()=>{if(LG.copy)LG.diets[LG.diet]={...LG.copy};renderWin();},
  lgDietAll:()=>{const d={...(LG.diets[LG.diet]||LG.diets.공통)};C.forEach(c=>LG.diets[c.name]={...d});renderWin();},lgResetDiet:()=>{if(LG.diet!=='공통')delete LG.diets[LG.diet];renderWin();},
  lgSelectAll:()=>{LG.selected=new Set(LG.jobs.map(j=>j.id));renderWin();},lgClear:()=>{LG.selected.clear();renderWin();},
  lgWorker:d=>{closeWin();ACT.selC(d,null,{shiftKey:false});const c=byId(d.id);setView(c.x,c.y);renderAll();},
  lgCancel:d=>{const j=LG.jobs.find(j=>j.id===+d.id);if(j)lgCancelJob(j);renderWin();},
  lgBulk:d=>{let count=0;LG.jobs.filter(j=>LG.selected.has(j.id)).forEach(j=>{if(j.phase==='완료')return;count++;if(d.mode==='cancel')lgCancelJob(j);else{j.paused=d.mode==='pause';if(j.paused)j.reserved=j.delivered;}});feedback(`적용 ${count}개 · 제외 ${LG.selected.size-count>0?LG.selected.size-count:0}개(완료된 작업)`);renderWin();},
  lgAdvance:d=>{const j=LG.jobs.find(j=>j.id===+d.id);if(!j||j.paused)return;if(j.phase==='자재 부족'){j.reserved=j.need;j.phase='운반 대기';}else if(j.delivered<j.need){j.reserved=j.delivered=j.need;j.phase='시공 대기';}else if(j.phase==='시공 대기'){j.phase='시공 중 · 재료 소비 완료';}else if(j.phase.startsWith('시공 중'))j.phase='완료';renderWin();},
  lgCompostAdvance:()=>{const c=LG.compost;if(c.input){c.progress=Math.min(1,c.progress+.25);if(c.progress===1){c.finished+=c.input;c.input=0;c.progress=0;}}renderWin();},
  lgCompostOut:()=>{const c=LG.compost;if(c.input)LG.floor.push({name:'썩은 식품',qty:c.input,fresh:0,location:'퇴비통 주변'});c.input=0;c.progress=0;renderWin();},
  lgFertilizer:()=>{const c=LG.compost;if(c.finished)LG.floor.push({name:'비료',qty:c.finished,fresh:null,location:'퇴비통 주변'});c.finished=0;renderWin();}
});
Object.assign(INPUT, {
  lgPriority:el=>{LG.stores[LG.store].priority=+el.value;},lgAllow:el=>{const s=LG.stores[LG.store];s.allow[el.dataset.name]=el.checked;if(!el.checked)lgDrop(s,el.dataset.name);renderWin();},
  lgDiet:el=>{LG.diet=el.value;renderWin();},lgDietAllow:el=>{LG.diets[LG.diet]={...(LG.diets[LG.diet]||LG.diets.공통),[el.dataset.name]:el.checked};},
  lgPick:el=>{el.checked?LG.selected.add(+el.dataset.id):LG.selected.delete(+el.dataset.id);renderWin();},
  lgBulkPriority:el=>{LG.jobs.filter(j=>LG.selected.has(j.id)&&j.phase!=='완료').forEach(j=>j.priority=+el.value);renderWin();},
  lgCompostTarget:el=>{LG.compost.target=Math.max(0,Math.round(+el.value||0));},lgFertilize:el=>{LG.compost.fertilize=el.checked;}
});
// 실제 지시 버튼에서 작업 관리 목록으로 이어지게 한다.
const lgPlace=ACT.place;
ACT.place=d=>{const item=BUILD[S.buildCat][1]?.find(x=>x[0]===d.n);LG.pending={name:d.n+' 건설',need:item?.[1]||30,material:S.buildMat||'bark'};lgPlace(d);};
document.querySelector('#scene').addEventListener('click',e=>{if(!S.place||!LG.pending)return;const p=LG.pending;LG.jobs.push({...p,id:LG.next++,reserved:0,delivered:0,priority:5,phase:'자재 부족',worker:'jb'});feedback('청사진 배치 — 물류·식품의 건설·작업에서 배송 상태 확인');if(!e.shiftKey)ACT.placeEnd();});
ACT.renovate=d=>{const b=bld(d.id);b.renovating=d.k;LG.jobs.push({id:LG.next++,name:b.name+' 개보수',building:b.id,material:d.k,need:30,reserved:0,delivered:0,priority:5,phase:'자재 부족',worker:'jb'});openWin('logistics',{tab:'건설·작업'});};
ACT.renovateCancel=d=>{LG.jobs.filter(j=>j.building===d.id).forEach(lgCancelJob);bld(d.id).renovating=null;renderAll();};
let lgDragging=false;
document.addEventListener('pointerdown',e=>{const row=e.target.closest('[data-job]');if(!row||e.target.closest('button,input,select'))return;lgDragging=true;LG.selected.add(+row.dataset.job);row.querySelector('input').checked=true;});
document.addEventListener('pointerover',e=>{const row=e.target.closest('[data-job]');if(lgDragging&&row){LG.selected.add(+row.dataset.job);row.querySelector('input').checked=true;}});
document.addEventListener('pointerup',()=>{if(lgDragging){lgDragging=false;renderWin();}});
RECIPES.cook={name:'조리대',job:'요리',items:[['간단한 요리','허용 날것 · 재료량 미정'],['좋은 요리','허용 날것·부재료 · 재료량 미정']]};
BUILD.find(([n])=>n==='저장')[1].push(['냉장고',30,1],['냉동고',50,1]);
BUILD.find(([n])=>n==='식량')[1].push(['퇴비통',20,1]);
S.orders.cook=[];
S.productionPaused={};
LG.ingredients={날것:true,감로:true,귀한재료:false};
const lgProduction=winProduction;
winProduction=function(arg){const r=lgProduction(arg),id=arg.id||'smith';r.body=`<div class="row">${lgButton('lgProductionPause',S.productionPaused[id]?'생산 재개':'생산 일시정지',`data-id="${id}"`)}<span class="dim">진행도 보존 · 새 작업 중지</span></div>`+r.body;
  if(id==='cook')r.body+=`<div class="box well"><h3>레시피 허용 재료</h3>${Object.entries(LG.ingredients).map(([n,v])=>`<label class="row"><input type="checkbox" data-input="lgIngredient" data-name="${n}" ${v?'checked':''}>${n}</label>`).join('')}<p class="dim">썩은 식품은 사용 불가. 시작 전 취소는 손실 없음, 시작 후 취소는 소비 재료 반환 없음. 레시피와 생산 진행은 시안 예시입니다.</p></div>`;return r;};
ACT.lgProductionPause=d=>{S.productionPaused[d.id]=!S.productionPaused[d.id];renderWin();};
INPUT.lgIngredient=el=>{LG.ingredients[el.dataset.name]=el.checked;};
ACT.delOrder=d=>{const list=S.orders[d.id],o=list[+d.i];const remove=()=>{const i=list.indexOf(o);if(i>=0)list.splice(i,1);renderAll();};if(o?.prog>0)confirmBox('생산 취소','진행 중인 생산을 취소합니다. 이미 소비한 재료는 반환하지 않습니다.','취소 확정',remove,true);else remove();};
