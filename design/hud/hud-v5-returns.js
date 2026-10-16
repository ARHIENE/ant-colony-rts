'use strict';
const RETURN = {clock:0,distance:125,coefficient:.08,trip:null,exchange:'',invalid:false};
EXP.seized.forEach(g=>g.owner='황충');
const returnDuration=()=>84+RETURN.distance*RETURN.coefficient;
const returnDate=day=>`${Math.floor(day/336)+1}년 ${Math.floor(day%336/28)+1}월 ${Math.floor(day%28)+1}일`;
function returnDeposit(g){g.location='본거지 창고 주변 바닥';LG.floor.push({name:g.name,qty:1,fresh:null,quality:g.quality,durability:g.durability,location:g.location});}
function returnAdvance(days){if(S.paused||S.negoPause)return;RETURN.clock+=days;const t=RETURN.trip;if(t&&t.state==='귀환 중'&&RETURN.clock>=t.arrival){t.state='귀환 완료';t.goods.forEach(returnDeposit);const c=byId('hz');c.away=false;c.hp=t.hp;c.health.wounds=structuredClone(t.wounds);c.injuries=c.health.wounds.map(w=>[w.part,w.kind]);c.job='귀환 · 치료 대기';toast({kind:'done',title:'황충 안전 귀환',text:'부상을 유지한 채 도착했습니다. 합의한 장비도 도착했습니다.',go:()=>openWin('commander',{id:'hz',tab:'건강'})});}COLONY.prisoners.forEach(p=>{if(p.returnAt!=null&&RETURN.clock>=p.returnAt){p.state='귀환 완료';delete p.returnAt;}});memorialExpire();}
function returnPanel(){const t=RETURN.trip;return `<section class="box well"><h3>황충 · ${t?t.state:'상대에게 잡힌 우리 장수'}</h3><p>석방 시 부상 유지 · 공격·재포획 없이 안전 귀환</p>${t?`<p>예정일 ${returnDate(t.arrival)} · 남은 ${Math.max(0,t.arrival-RETURN.clock).toFixed(1)}일</p><progress max="${t.duration}" value="${Math.min(t.duration,RETURN.clock-t.started)}"></progress><p>동반 반환 장비 ${t.goods.length}개 · ${t.state==='귀환 중'?'장수와 함께 이동 중':'창고 주변 바닥 도착'}</p>`:`<p>현재 ${returnDate(RETURN.clock)} · 합의 시 귀환 예정 ${returnDate(RETURN.clock+returnDuration())}</p><p>기본 3개월(84일) + 거리 ${RETURN.distance} × 예시 계수 ${RETURN.coefficient} = ${returnDuration().toFixed(1)}일</p>${careSelect('상호 교환할 포로','returnExchange',RETURN.exchange,[['','금화 몸값'],...COLONY.prisoners.filter(p=>p.state==='수용 중').map(p=>[p.id,p.name])])}`}</section><details><summary>귀환 시간 예시</summary><label class="row">거점 간 실제 거리(시안) <input type="number" min="0" step="0.1" aria-label="귀환 거리" data-input="returnDistance" value="${RETURN.distance}" ${t?'disabled':''}></label><p class="dim">거리 계수는 미정이며 구간별 고정 시간 없이 비례합니다. 1배속 실제 5분 = 게임 1개월. 일시정지·협상 중에는 멈춥니다.</p>${lgButton('returnStep','게임 1개월 진행(시안)','',S.paused||!!S.negoPause)}${lgButton('returnInvalid','석방 대상 상태 변경(시안)','',!!t)}</details>`;}
const returnSeizedPanel=seizedPanel;
seizedPanel=function(){return returnSeizedPanel().replace('· 안전 귀환 중(최소 3개월 + 거리)',RETURN.trip?'· '+RETURN.trip.state:'').replace('협상으로 받은 물품은 즉시 도착하고 받을 창고가 없으면 창고 주변 바닥에 놓입니다.','일반 장비 거래만 즉시 도착합니다. 장수 석방과 함께 합의한 장비는 귀환 완료 때 도착합니다.')+returnPanel();};
function returnInvalidDialog(){S.negoPause={paused:S.paused,speed:S.speed};confirmBox('무효가 된 협상','상황 변화로 이 제안을 더는 진행할 수 없습니다. (대사 예시)','확인',()=>{RETURN.invalid=false;EXP.ransom=false;renderAll();});const d=$('#confirm'),cancel=d.querySelector('[value="cancel"]');cancel.hidden=true;const end=d.onclose;d.onclose=()=>{cancel.hidden=false;S.negoPause=null;end();renderClock();};}
Object.assign(ACT,{
  expNegotiate:()=>{
    if(RETURN.invalid){returnInvalidDialog();return;}
    const goods=EXP.seized.filter(g=>g.selected&&g.location==='압수 보관'),ransom=EXP.ransom&&!EXP.returned;
    const exchange=ransom&&RETURN.exchange?COLONY.prisoners.find(p=>p.id===+RETURN.exchange&&p.state==='수용 중'):null;
    if(ransom&&RETURN.exchange&&!exchange){RETURN.invalid=true;returnInvalidDialog();return;}
    const cost=goods.length*30+(ransom&&!exchange?80:0),version=EXP.offerVersion;
    if(!goods.length&&!ransom)return;
    S.negoPause={paused:S.paused,speed:S.speed};renderClock();
    confirmBox('반환 협상',`<p>장비 ${goods.length}개 · ${ransom?exchange?esc(exchange.name)+'와 상호 교환':'장수 몸값 포함':'일반 장비 거래'}</p><p>금화 ${cost}(예시)</p>${ransom?`<p>귀환 예정 ${returnDate(RETURN.clock+returnDuration())} · ${returnDuration().toFixed(1)}일<br>반환 장비는 귀환 완료 때 함께 도착합니다.</p>`:'<p>장비만 즉시 교환합니다.</p>'}`,'상대 수락(시안)',()=>{
      if(version!==EXP.offerVersion||RETURN.invalid||exchange&&exchange.state!=='수용 중'){RETURN.invalid=true;feedback('상황이 바뀌었습니다. 협상을 다시 열어 무효를 확인하세요.');return;}
      if(EXP.coins<cost||goods.some(g=>g.location!=='압수 보관')||ransom&&RETURN.trip)return;
      EXP.coins-=cost;
      if(ransom){const c=byId('hz'),duration=returnDuration();RETURN.trip={state:'귀환 중',started:RETURN.clock,arrival:RETURN.clock+duration,duration,goods,hp:c.hp,wounds:structuredClone(c.health.wounds)};goods.forEach(g=>g.location='황충과 안전 귀환 중');EXP.returned=true;EXP.ransom=false;if(exchange){exchange.state='안전 귀환 중';exchange.returnAt=RETURN.clock+duration;exchange.request=null;}}
      else goods.forEach(returnDeposit);renderAll();
    },false,EXP.coins<cost);
    const d=$('#confirm'),end=d.onclose;d.onclose=()=>{end();S.negoPause=null;renderClock();};
  },
  returnStep:()=>{returnAdvance(28);renderAll();},returnInvalid:()=>{RETURN.invalid=true;EXP.ransom=true;renderWin();}
});
Object.assign(INPUT,{returnDistance:el=>{if(!RETURN.trip)RETURN.distance=Math.max(0,Number(el.value)||0);renderWin();},returnExchange:el=>{RETURN.exchange=el.value;EXP.ransom=true;renderWin();}});

// 위로는 일시적인 기분 요인 하나. 같은 고인 또는 여러 묘비 방문으로 중첩하지 않는다.
const MEMORIAL={visitor:'gw',grave:1};
function memorialVisit(){const c=byId(MEMORIAL.visitor),g=COLONY.graves.find(g=>g.id===MEMORIAL.grave);if(S.paused||S.negoPause)return;if(S.red||c.deployed||c.away||c.health.dead||c.health.rescue||c.health.tendRequested){COLONY.mourning='전투·구조·치료 중에는 추모를 미룹니다.';renderWin();return;}if(!g?.memorial){COLONY.mourning='방문할 묘비가 없습니다.';renderWin();return;}const relation=c.rel.find(([name])=>name===g.name),value=Number(relation?.[1].match(/[−-]?\d+/)?.[0]?.replace('−','-')||0);if(value<=0){COLONY.mourning='관계가 없는 장수는 여가 방문을 하지 않습니다.';renderWin();return;}if(!c.memorialUntil){const amount=Math.min(100-c.mood,value>=40?4:2);c.memorialMood=amount;c.mood=clamp(c.mood+amount,0,100);c.moodWhy.push(['추모의 위로',amount]);c.memorialUntil=RETURN.clock+1;}COLONY.mourning=`${c.name}이 ${g.name}을 추모했습니다. 위로 +${c.memorialMood} · 남은 ${((c.memorialUntil-RETURN.clock)*24).toFixed(1)}시간(시안)`;renderAll();}
function memorialExpire(){C.forEach(c=>{if(c.memorialUntil&&RETURN.clock>=c.memorialUntil){c.mood=clamp(c.mood-c.memorialMood,0,100);c.moodWhy=c.moodWhy.filter(([n])=>n!=='추모의 위로');delete c.memorialUntil;delete c.memorialMood;}});}
const fullMemorialPanel=memorialPanel;
memorialPanel=function(){return fullMemorialPanel()+`<details><summary>관계에 따른 추모 예시</summary>${careSelect('방문 장수','memorialVisitor',MEMORIAL.visitor,C.filter(c=>!c.health.dead).map(c=>[c.id,c.name]))}${careSelect('방문 묘비','memorialGrave',MEMORIAL.grave,COLONY.graves.map(g=>[g.id,g.name]))}<p>친한 장수는 여가에 방문합니다. 위로는 장수 상세의 기분에 표시되며 중첩하지 않습니다.</p>${lgButton('memorialFriend','친구 관계 예시 설정')}${lgButton('graveVisit','여가 방문 상황 진행(시안)','',S.paused||!!S.negoPause)}${lgButton('memorialTime','위로 시간 경과(시안)','',S.paused||!!S.negoPause)}</details>`;};
Object.assign(ACT,{graveVisit:memorialVisit,memorialTime:()=>{returnAdvance(1);renderAll();},memorialFriend:()=>{const c=byId(MEMORIAL.visitor),g=COLONY.graves.find(g=>g.id===MEMORIAL.grave);c.rel=c.rel.filter(([n])=>n!==g.name);c.rel.push([g.name,'친구 52']);renderWin();}});
Object.assign(INPUT,{memorialVisitor:el=>MEMORIAL.visitor=el.value,memorialGrave:el=>MEMORIAL.grave=+el.value});

const managementColony=winColony;
winColony=function(){const r=managementColony();r.body=`<nav class="row" aria-label="생활 관리">${lgButton('open','농사 관리','data-w="farm"')}${lgButton('open','설비 연결','data-w="utilities"')}${lgButton('open','장비 보관함','data-w="equipment"')}</nav>`+r.body;return r;};
const managementBuild=winBuild;
winBuild=function(){const r=managementBuild();const cat=BUILD[S.buildCat]?.[0],destination=cat==='식량'?'farm':['전력','배관','자동화'].includes(cat)?'utilities':cat==='작업'?'science':null;if(destination)r.body=`<div class="row">${lgButton('open','관련 관리 화면','data-w="'+destination+'"')}</div>`+r.body;return r;};
for(const [name,items] of [['자동화',[['신호선',2,0],['저장량 센서',12,0],['AND 논리 장치',10,0],['OR 논리 장치',10,0],['NOT 논리 장치',10,0]]],['배관',[['배관',2,0],['펌프',20,1],['밸브',8,0],['액체 탱크',30,1]]]])BUILD.find(([n])=>n===name)[1]=items;
renderAll();
