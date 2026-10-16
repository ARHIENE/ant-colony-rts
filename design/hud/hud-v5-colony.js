'use strict';
const COLONY = {
  manure: [{qty:6,age:3}], area:24, hauling:false, hygieneAlert:false,
  graves:[{id:1,name:'고순',date:'2년 봄 3일',cause:'본거지 방어 중 전사',body:'시신',buried:false,memorial:false,request:false},
    {id:2,name:'마초',date:'2년 봄 5일',cause:'원정 중 전사',body:null,buried:false,memorial:true,request:false}],
  prisoners:[{id:1,name:'적 장수 · 흑송',mobile:true,hp:62,hunger:25,policy:3,bed:'포로 침대 1',state:'수용 중',request:null},
    {id:2,name:'적 장수 · 적엽',mobile:false,hp:28,hunger:48,policy:2,bed:'포로 침대 2',state:'수용 중',request:null}],
  guard:'gw',guarding:true,enclosed:true,autoPause:true,food:6,medicine:4,tab:'의료'
};
Object.assign(LG.compost,{manure:0,allowManure:true,allowRotten:true,priority:5,capacity:30});
const hygieneLevel = () => {
  const burden=COLONY.manure.reduce((n,b)=>n+b.qty*Math.min(b.age,5),0)/COLONY.area;
  return burden>=1?'위험':burden>=.3?'주의':'양호';
};
function hygienePanel() {
  const count=COLONY.manure.reduce((n,b)=>n+b.qty,0),level=hygieneLevel();
  return `<div class="section-head"><h3>동쪽 우리</h3><span class="chip ${level==='양호'?'ok':'warn'}">위생 ${level}</span></div>
    <dl class="kv"><dt>바닥 배설물</dt><dd class="n">${count}개</dd><dt>우리 면적</dt><dd class="n">${COLONY.area}칸</dd><dt>운반</dt><dd>${COLONY.hauling?'수거 요청됨':'지시 없음'}</dd></dl>
    <div class="row">${lgButton('manureHaul',COLONY.hauling?'수거 취소':'배설물 수거 지시','',!count)}${lgButton('open','퇴비통 열기','data-w="logistics" data-tab="퇴비·비료"')}${lgButton('open','동물 목록','data-w="ranch"')}</div>
    <p class="dim">바닥 아이템처럼 운반합니다. 쌓인 양·우리 면적·방치 시간이 질병 위험에 영향을 줍니다. 저장된 배설물은 위생에 영향을 주지 않으며, 청소해도 이미 걸린 병은 남습니다.</p>
    <div class="table-wrap"><table class="care-table"><thead><tr><th>동물</th><th>건강</th><th>생산·번식</th><th>관리</th></tr></thead><tbody>${live().filter(a=>!a.wild).map(a=>`<tr><td>${esc(a.name)}</td><td>${symptomatic(a)?diseaseNames[a.disease.kind]:'증상 없음'}</td><td>${diseaseSevere(a)?'중단':symptomatic(a)?'생산 감소 · 새 번식 중단':a.stage==='새끼'?'생산 불가':'정상'}</td><td>${lgButton('open','의료·격리',`data-w="animal" data-id="${a.id}" data-tab="의료"`)}</td></tr>`).join('')}</tbody></table></div>
    <details><summary>시안 상태 진행</summary><div class="row">${lgButton('manureAdd','배설물 1개 발생(시안)')}${lgButton('manureAge','방치 1일(시안)','',S.paused||!!S.negoPause)}${lgButton('manureMove','수거 작업 진행(시안)','',!COLONY.hauling||S.paused||!!S.negoPause)}</div></details>`;
}
const oldCompost=lgCompost;
lgCompost=function(){const c=LG.compost;return `<div class="row"><span class="chip">배설물 ${c.manure}</span><span class="chip">썩은 식품 ${c.input}</span><span class="chip">통 사용 ${c.manure+c.input+c.finished} / ${c.capacity}</span></div>
  <div class="row"><label><input type="checkbox" data-input="compostAllowManure" ${c.allowManure?'checked':''}>배설물 받기</label><label><input type="checkbox" data-input="compostAllowRotten" ${c.allowRotten?'checked':''}>썩은 식품 받기</label>${lgPrio(c.priority,'compostPriority')}</div>
  <div class="row">${lgButton('compostLoad','원료 운반 진행(시안)','',S.paused||!!S.negoPause)}</div><p class="dim">바닥·창고에서 원료를 가져옵니다. 허용을 꺼도 이미 숙성 중인 원료는 유지됩니다.</p>${oldCompost().replace('썩은 식품으로 바닥에 나옵니다.','원래 투입한 종류 그대로 바닥에 나옵니다.')}`;};
function compostLoad() {
  const c=LG.compost;if(c.finished>=c.target)return;
  const sources=[LG.floor,...LG.stores.map(s=>s.batches)];
  for(const source of sources)for(const b of source){const valid=b.name==='배설물'?c.allowManure:b.name==='썩은 식품'&&c.allowRotten;if(!valid)continue;const n=Math.min(b.qty,c.capacity-c.input-c.manure-c.finished,Math.max(0,c.target-c.finished-c.input-c.manure));if(n<=0)continue;b.qty-=n;if(b.name==='배설물')c.manure+=n;else c.input+=n;}
  LG.floor=LG.floor.filter(b=>b.qty);LG.stores.forEach(s=>s.batches=s.batches.filter(b=>b.qty));
}
function memorialPanel(){return `<p class="dim">장례식은 없습니다. 지나가며 추모하고 친한 장수는 여가에 찾아옵니다. 긴급 작업을 중단하거나 위로 효과를 중복 적용하지 않습니다.</p>
  <div class="table-wrap"><table class="care-table"><thead><tr><th>장수 기록</th><th>시신·묘비</th><th>지시</th></tr></thead><tbody>${COLONY.graves.map(g=>`<tr><td><b>${esc(g.name)}</b><br>${g.date}<br><span class="dim">${g.cause}</span></td><td>${g.body||'시신 미회수'}<br>${g.buried?'안장됨':g.memorial?'빈 추모비':g.request?'매장 운반 대기':'미안장'}</td><td><div class="row">${lgButton('graveBury',g.request?'매장 취소':'매장 지시',`data-id="${g.id}"`,!g.body||g.buried)}${lgButton('graveMemorial','빈 추모비',`data-id="${g.id}"`,g.buried||g.memorial)}${lgButton('graveRemove','시신 꺼내기',`data-id="${g.id}"`,!g.buried)}${lgButton('graveDemolish','묘비 철거',`data-id="${g.id}"`,g.buried||!g.memorial)}</div></td></tr>`).join('')}</tbody></table></div>
  <p class="dim">시신은 부패해 유해가 되어도 사라지지 않습니다. 음식·재료로 쓰지 않으며 질병을 퍼뜨리지 않습니다. 매장 후에는 부패하지 않습니다. 시신이 있으면 꺼낸 뒤 묘비를 철거하세요.</p>
  <details><summary>시안 상태 진행</summary><div class="row">${lgButton('graveStep','매장 운반 진행(시안)','',S.paused||!!S.negoPause)}${lgButton('graveDecay','미안장 시신 부패(시안)','',S.paused||!!S.negoPause)}${lgButton('graveRecover','마초 시신 회수(시안)','',!!COLONY.graves[1].body)}${lgButton('graveVisit','친구의 여가 추모(시안)')}</div><p>${COLONY.mourning||'추모 기록 없음'}</p></details>`;}
function prisonerPanel(){return `<div class="row">${careSelect('경비 장수','prisonGuard',COLONY.guard,C.filter(c=>!c.young).map(c=>[c.id,c.name]))}<label><input type="checkbox" data-input="prisonGuarding" ${COLONY.guarding?'checked':''}>경비 근무</label><label><input type="checkbox" data-input="prisonEnclosed" ${COLONY.enclosed?'checked':''}>시설 폐쇄 상태(시안)</label><label><input type="checkbox" data-input="prisonPause" ${COLONY.autoPause?'checked':''}>탈출 시 자동 일시정지</label></div>
  <p class="dim">경비는 감시, 의료 장수는 처치·급식. 이동 불가 포로는 탈출할 수 없습니다. 석방과 몸값 교환은 별도이며 장비는 자동 반환하지 않습니다.</p>
  <div class="table-wrap"><table class="care-table"><thead><tr><th>포로</th><th>상태·침대</th><th>치료 방침</th><th>지시</th></tr></thead><tbody>${COLONY.prisoners.map(p=>`<tr><td><b>${p.name}</b><br>건강 ${p.hp} · 허기 ${p.hunger}</td><td>${p.state}<br>${p.mobile?'이동 가능':'이동 불가'} · ${p.bed}<br>${p.request||''}</td><td><select aria-label="${p.name} 치료 방침" data-input="prisonCare" data-id="${p.id}">${CARE_POLICY.map((n,i)=>`<option value="${i}" ${i===p.policy?'selected':''}>${n}</option>`).join('')}</select></td><td><div class="row">${lgButton('prisonOrder','처치',`data-id="${p.id}" data-kind="처치"`,p.state!=='수용 중'||!p.policy)}${lgButton('prisonOrder','급식',`data-id="${p.id}" data-kind="급식"`,p.state!=='수용 중'||!COLONY.food)}${lgButton('prisonRelease','석방',`data-id="${p.id}"`,p.state!=='수용 중')}${lgButton('prisonRecapture','제압·재포획',`data-id="${p.id}"`,p.state!=='탈출 중')}${lgButton('prisonRansom','몸값 협상',`data-id="${p.id}"`,p.state!=='수용 중')}</div></td></tr>`).join('')||'<tr><td colspan="4">수용 중인 포로가 없습니다.</td></tr>'}</tbody></table></div>
  <details><summary>시안 상태 진행</summary><p>식사 ${COLONY.food} · 약품 ${COLONY.medicine} · ${COLONY.enclosed&&COLONY.guarding?'경비·시설 정상':'탈출 위험 증가'}</p>${lgButton('prisonStep','의료 작업 진행(시안)','',S.paused||!!S.negoPause)}${lgButton('prisonEscape','탈출 상황 발생(시안)','',S.paused||!!S.negoPause)}</details>`;}
function winColony(){const t=S.tab.colony||'의료';return {title:'군체 관리',sub:'의료·생활·기록',body:`${tabs('colony',['의료','동물 위생','묘비·추모','포로'])}<div class="colony-panel">${({'의료':carePanel,'동물 위생':hygienePanel,'묘비·추모':memorialPanel,'포로':prisonerPanel}[t])()}</div>`,foot:`<span class="note">예시 상태 · 진행 버튼은 시안용</span>${lgButton('open','원정·수송 관리','data-w="expedition"')}${lgButton('close','닫기')}`};}
Object.assign(ACT,{
  manureAdd:()=>{COLONY.manure.push({qty:1,age:0});renderWin();},manureHaul:()=>{COLONY.hauling=!COLONY.hauling;renderWin();},
  manureAge:()=>{if(S.paused||S.negoPause)return;COLONY.manure.forEach(b=>b.age++);if(hygieneLevel()==='위험'&&!COLONY.hygieneAlert){COLONY.hygieneAlert=true;toast({kind:'warn',title:'동쪽 우리 위생 위험',text:'배설물을 수거하면 질병 위험을 줄일 수 있습니다.',go:()=>openWin('colony',{tab:'동물 위생'})});}renderAll();},
  manureMove:()=>{if(!COLONY.hauling||S.paused||S.negoPause)return;const qty=COLONY.manure.reduce((n,b)=>n+b.qty,0);LG.floor.push({name:'배설물',qty,fresh:null,location:'퇴비통 주변'});compostLoad();const left=LG.floor.find(b=>b.name==='배설물'&&b.location==='퇴비통 주변');if(left){COLONY.manure=[{qty:left.qty,age:3}];LG.floor=LG.floor.filter(b=>b!==left);feedback('퇴비통이 받지 못한 배설물은 우리 바닥에 남습니다.');}else{COLONY.manure=[];COLONY.hauling=false;COLONY.hygieneAlert=false;}renderAll();},
  compostLoad:()=>{if(S.paused||S.negoPause)return;compostLoad();renderWin();},
  lgCompostAdvance:()=>{const c=LG.compost;if(c.input+c.manure){c.progress=Math.min(1,c.progress+.25);if(c.progress===1){c.finished+=c.input+c.manure;c.input=c.manure=c.progress=0;}}renderWin();},
  lgCompostOut:()=>{const c=LG.compost;for(const [name,qty] of [['배설물',c.manure],['썩은 식품',c.input]])if(qty)LG.floor.push({name,qty,fresh:name==='배설물'?null:0,location:'퇴비통 주변'});c.input=c.manure=c.progress=0;renderWin();},
  graveBury:d=>{const g=COLONY.graves.find(g=>g.id===+d.id);if(g.body&&!g.buried)g.request=!g.request;renderWin();},
  graveMemorial:d=>{const g=COLONY.graves.find(g=>g.id===+d.id);g.memorial=true;renderWin();},
  graveRemove:d=>{const g=COLONY.graves.find(g=>g.id===+d.id);g.buried=false;renderWin();},
  graveDemolish:d=>{const g=COLONY.graves.find(g=>g.id===+d.id);if(!g.buried)g.memorial=false;renderWin();},
  graveStep:()=>{if(S.paused||S.negoPause)return;COLONY.graves.filter(g=>g.request&&g.body).forEach(g=>{g.buried=g.memorial=true;g.request=false;});renderWin();},
  graveDecay:()=>{if(S.paused||S.negoPause)return;COLONY.graves.filter(g=>g.body&&!g.buried).forEach(g=>g.body='유해');renderWin();},
  graveRecover:()=>{COLONY.graves[1].body='유해';renderWin();},graveVisit:()=>{COLONY.mourning=S.red?'비상 상황 · 추모 보류':'친구가 여가에 추모했습니다. 같은 위로 효과는 중복되지 않습니다.';renderWin();},
  prisonOrder:d=>{const p=COLONY.prisoners.find(p=>p.id===+d.id);if(p.state==='수용 중')p.request=p.request===d.kind?null:d.kind;renderWin();},
  prisonRelease:d=>{const p=COLONY.prisoners.find(p=>p.id===+d.id);confirmBox('포로 석방',`${p.name}을 조건 없이 풀어줍니다. 압수 장비는 반환하지 않습니다.`,'석방',()=>{p.state='석방됨';p.request=null;renderWin();},true);},
  prisonRansom:d=>{const p=COLONY.prisoners.find(p=>p.id===+d.id);if(p.state!=='수용 중')return;S.negoPause={paused:S.paused,speed:S.speed};renderClock();confirmBox('포로 몸값 협상',`${p.name}의 몸값으로 금화 80을 제안합니다(시안). 장비 반환은 별도 조건이며, 성립하면 안전 귀환을 시작합니다.`,'상대 수락(시안)',()=>{if(p.state!=='수용 중')return;p.state='안전 귀환 중 · 최소 3개월 + 거리';p.returnAt=RETURN.clock+returnDuration();p.request=null;EXP.coins+=80;renderWin();});const dialog=$('#confirm'),finish=dialog.onclose;dialog.onclose=()=>{S.negoPause=null;finish();renderClock();};},
  prisonRecapture:d=>{const p=COLONY.prisoners.find(p=>p.id===+d.id);if(p.state==='탈출 중'){p.state='제압 요청';p.request='경비 출동';}renderWin();},
  prisonStep:()=>{if(S.paused||S.negoPause)return;COLONY.prisoners.forEach(p=>{if(p.state==='제압 요청'){p.state='수용 중';p.hp=Math.max(1,p.hp-10);p.request=null;}if(p.request==='급식'&&COLONY.food){COLONY.food--;p.hunger=Math.max(0,p.hunger-30);p.request=null;}if(p.request==='처치'&&p.policy){if(p.policy>1&&COLONY.medicine)COLONY.medicine--;p.hp=Math.min(100,p.hp+10);p.request=null;}});renderWin();},
  prisonEscape:()=>{if(S.paused||S.negoPause)return;const p=COLONY.prisoners.find(p=>p.mobile&&p.state==='수용 중');if(!p)return;p.state='탈출 중';p.request=null;if(COLONY.autoPause)S.paused=true;toast({kind:'crisis',title:'포로 탈출',text:p.name+' · 제압·재포획을 지시하세요.',go:()=>openWin('colony',{tab:'포로'})});renderAll();}
});
Object.assign(INPUT,{
  compostAllowManure:el=>LG.compost.allowManure=el.checked,compostAllowRotten:el=>LG.compost.allowRotten=el.checked,compostPriority:el=>LG.compost.priority=+el.value,
  prisonGuard:el=>COLONY.guard=el.value,prisonGuarding:el=>{COLONY.guarding=el.checked;renderWin();},prisonEnclosed:el=>{COLONY.enclosed=el.checked;renderWin();},prisonPause:el=>COLONY.autoPause=el.checked,
  prisonCare:el=>{COLONY.prisoners.find(p=>p.id===+el.dataset.id).policy=+el.value;renderWin();}
});
const hygieneRanch=winRanch;
winRanch=function(){const r=hygieneRanch();r.body=`<div class="row"><span class="chip">위생 ${hygieneLevel()}</span>${lgButton('open','배설물·위생 관리','data-w="colony" data-tab="동물 위생"')}</div>`+r.body;return r;};
