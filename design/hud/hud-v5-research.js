'use strict';
// 연구량은 기획 잠정값. 항목별 비용·선행 관계는 화면 검토용 예시다.
const SCI = {
  era: 0, selected: 'farm', active: null, paused: false, stock: 12,
  labs: [{name:'서쪽 연구소',era:0,by:'yb'},{name:'동쪽 연구소',era:0,by:''}],
  topics: [
    {id:'farm',name:'균류 농법',era:0,field:'생물',effect:'밭에서 균류 재배',need:[],cost:2},
    {id:'fish',name:'낚시',era:0,field:'생물',effect:'물가 낚시 해금',need:[],cost:2},
    {id:'medicine',name:'의료 기술',era:1,field:'생물',effect:'의료 시설·처치 발전',need:['farm'],cost:3},
    {id:'vehicle',name:'차량 수송',era:2,field:'기계',effect:'수송 차량',need:[],cost:4},
    {id:'signal',name:'자동화',era:3,field:'기계',effect:'센서·신호선',need:[],cost:5},
    {id:'fusion',name:'핵융합',era:4,field:'기계',effect:'핵융합 발전',need:[],cost:6},
    {id:'rocket',name:'로켓 이론',era:5,field:'기계',effect:'탈출 로켓 연구',need:[],cost:8},
    {id:'hull',name:'선체 구조',era:5,field:'기계',effect:'로켓 선체 건조',need:['rocket'],cost:8},
    {id:'cocoon',name:'동면 고치',era:5,field:'생물',effect:'장수·병력 탑승 공간',need:['rocket'],cost:8},
    {id:'engine',name:'추진기관',era:5,field:'기계',effect:'로켓 추진기관 건조',need:['rocket'],cost:8,blueprint:true}
  ], blueprint:0
};
const SCI_ERAS = ['소굴','증기','석유','전기','원자','미래'];
const SCI_WORK = [200,400,800,1500,2800,5000];
SCI.topics.forEach(t=>{t.progress=0;t.paid=false;});
const sciTopic = id => SCI.topics.find(t=>t.id===id);
const sciDone = t => t.progress>=SCI_WORK[t.era];
const sciWorker = c => c&&!c.young&&!c.away&&!c.deployed&&!c.health.dead&&!c.locked.includes('research')&&c.prio.research>0&&careMobile(c.health);
function sciBlock(t) {
  if(sciDone(t))return '연구 완료';
  if(t.need.some(id=>!sciDone(sciTopic(id))))return '선행 연구 필요';
  if(!SCI.labs.some(l=>l.era>=t.era))return `${SCI_ERAS[t.era]} 시대 연구소 필요`;
  if(!t.paid&&SCI.stock<t.cost)return '연구 재료 부족';
  if(!t.paid&&t.blueprint&&!SCI.blueprint)return '월드맵 설계도 필요';
  return '';
}
function winScience() {
  const t=sciTopic(SCI.selected), workers=SCI.labs.filter(l=>l.era>=t.era&&sciWorker(byId(l.by)));
  return {title:'과학·연구',sub:'한 번에 하나 · 완료 후 다음 연구는 직접 선택',body:`
    <div class="tabs" role="tablist">${SCI_ERAS.map((n,i)=>`<button role="tab" aria-selected="${SCI.era===i}" data-act="sciEra" data-i="${i}">${i+1}. ${n}</button>`).join('')}</div>
    <div class="grid2"><section><h3>${SCI_ERAS[SCI.era]} 시대</h3><ul class="list">${SCI.topics.filter(x=>x.era===SCI.era).map(x=>`<li><button class="btn grow" data-act="sciPick" data-id="${x.id}" aria-pressed="${x===t}">${x.name}</button><span>${sciDone(x)?'완료':SCI.active===x.id?'연구 중':sciBlock(x)||'선택 가능'}</span></li>`).join('')}</ul></section>
    <section class="box well"><h3>${t.name} <span class="dim">${t.field}</span></h3><p>${t.effect}</p><p>선행: ${t.need.map(id=>sciTopic(id).name).join(' · ')||'없음(시안)'}</p><p>연구량 ${Math.round(t.progress)} / ${SCI_WORK[t.era]} · 재료 ${t.cost}${t.paid?' 소비 완료':''}${t.blueprint?' · 설계도 1':''}</p><progress max="${SCI_WORK[t.era]}" value="${t.progress}"></progress><p role="status">${sciBlock(t)||(!workers.length?'담당 장수 없음 · 연구소 배정 필요':SCI.paused?'연구 일시정지':`참여 가능 연구소 ${workers.length}곳`)}</p><div class="row">${lgButton('sciStart',SCI.active&&SCI.active!==t.id?'연구 전환':'연구 지시','',!!sciBlock(t))}${lgButton('sciPause',SCI.paused?'연구 재개':'연구 정지','',!SCI.active)}</div></section></div>
    <h3>연구소 배정</h3>${SCI.labs.map((l,i)=>`<div class="row"><b>${l.name} · ${SCI_ERAS[l.era]}</b><label>담당 장수 <select aria-label="${l.name} 담당" data-input="sciAssign" data-i="${i}"><option value="">미지정</option>${C.filter(c=>!c.young).map(c=>`<option value="${c.id}" ${l.by===c.id?'selected':''} ${!sciWorker(c)?'disabled':''}>${c.name} · 연구 ${c.skills.research}${!sciWorker(c)?' · 참여 불가':''}</option>`).join('')}</select></label><span>${l.era<t.era?'등급 부족':!sciWorker(byId(l.by))?'담당자 대기':'참여 가능'}</span></div>`).join('')}
    <details><summary>시안 상태 진행</summary><p>재료 ${SCI.stock} · 설계도 ${SCI.blueprint}. 비용·선행 관계·속도는 예시이며 전체 연구 항목 목록은 미정입니다.</p><div class="row">${lgButton('sciStep','연구 작업 진행(시안)','',S.paused||!!S.negoPause||!SCI.active||SCI.paused)}${lgButton('sciLab','선택 시대 연구소 준비(시안)')}${lgButton('sciStock','재료·설계도 확보(시안)')}</div></details>`,foot:lgButton('close','닫기')};
}
Object.assign(ACT,{
  sciEra:d=>{SCI.era=+d.i;SCI.selected=SCI.topics.find(t=>t.era===SCI.era).id;renderWin();},sciPick:d=>{SCI.selected=d.id;renderWin();},
  sciStart:()=>{const t=sciTopic(SCI.selected);if(sciBlock(t))return;if(!t.paid){SCI.stock-=t.cost;if(t.blueprint)SCI.blueprint--;t.paid=true;}SCI.active=t.id;SCI.paused=false;renderWin();},
  sciPause:()=>{SCI.paused=!SCI.paused;renderWin();},
  sciStep:()=>{if(S.paused||S.negoPause||SCI.paused||!SCI.active)return;const t=sciTopic(SCI.active);const work=SCI.labs.filter(l=>l.era>=t.era&&sciWorker(byId(l.by))).reduce((n,l)=>n+10+byId(l.by).skills.research*2,0);t.progress=Math.min(SCI_WORK[t.era],t.progress+work);if(sciDone(t)){SCI.active=null;toast({kind:'done',title:t.name+' 연구 완료',text:'다음 연구를 직접 선택하세요.',go:()=>openWin('science')});}renderAll();},
  sciLab:()=>{SCI.labs[0].era=SCI.era;renderWin();},sciStock:()=>{SCI.stock+=20;SCI.blueprint++;renderWin();}
});
INPUT.sciAssign=el=>{const i=+el.dataset.i;if(el.value&&!sciWorker(byId(el.value)))return;SCI.labs.forEach((l,j)=>{if(i!==j&&l.by===el.value)l.by='';});SCI.labs[i].by=el.value;renderWin();};
