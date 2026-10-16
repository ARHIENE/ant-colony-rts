'use strict';
// HTML 시안의 의료 상태. 시간·확률·수량은 검토용 예시이며 게임 밸런스 확정값이 아니다.
const CARE = { selected: 'jb', medicine: [8, 4, 2], beds: 2, floorBeds: 1, food: 8, clock: 0 };
const CARE_POLICY = ['치료 안 함', '약 없이 처치', '기초 약품까지', '일반 약품까지', '고급 약품까지'];
const MED_NAMES = ['기초 약품', '일반 약품', '고급 약품'];
C.forEach(c => c.health = { policy: 3, self: false, rest: false, place: '현장', rescue: false, feeding: false,
  wounds: c.injuries.map(([part, kind]) => ({ part, kind, pain: 15, bleed: 4, tended: false })),
  disease: null, surgery: null, surgeryPart: 0, surgeon: 'best', recovery: 0, dead: false });
byId('jb').health.wounds.push({ part: '앞다리', kind: '깊은 상처', pain: 30, bleed: 12, tended: false });
byId('jb').health.disease = { name: '상처 감염', severity: 24, immunity: 17, due: 0 };
const carePatient = () => byId(CARE.selected) || C[0];
const careBleed = h => h.wounds.reduce((n,w) => n + (w.tended ? 0 : w.bleed), 0);
const carePain = h => Math.min(100, h.wounds.reduce((n,w) => n + w.pain, 0));
const careMobile = h => !h.dead && !h.recovery && carePain(h) < 70 && h.surgery?.phase !== '수술 중' && (!h.missing || (careFunctions(h).이동 > 0 && careFunctions(h).의식 > 30));
const careMedic = h => (h.surgeon === 'best' ? C.filter(c => c.health!==h && !c.young && !c.away && !c.deployed && c.prio.nursing>0 && !c.health.dead && careMobile(c.health)).sort((a,b)=>b.skills.medicine-a.skills.medicine)[0] : C.find(c=>c.id===h.surgeon&&c.health!==h&&!c.away&&!c.deployed&&c.prio.nursing>0&&!c.health.dead&&careMobile(c.health)));
const careGrade = h => { for(let i=h.policy-2;i>=0;i--) if(CARE.medicine[i]>0) return i; return -1; };
function careSelect(label, key, value, options) {
  return `<label class="row">${label}<select aria-label="${label}" data-input="${key}">${options.map(([v,n])=>`<option value="${v}" ${String(value)===String(v)?'selected':''}>${esc(n)}</option>`).join('')}</select></label>`;
}
function careBody(c) {
  const h=c.health, op=h.surgery, bleed=careBleed(h), medic=careMedic(h), grade=careGrade(h);
  const patient=`data-id="${c.id}"`;
  return `<div class="row"><span class="chip ${bleed?'warn':''}">${h.dead?'사망':careMobile(h)?'이동 가능':'이동 불가'}</span><span class="chip">${h.place}</span><span class="chip">통증 ${carePain(h)}%</span><span class="chip">출혈 ${bleed}% / 시간</span></div>
    ${bleed?`<p class="warn-ink">출혈로 작업 중단 · 사망까지 약 ${Math.ceil(c.hp/bleed)}시간(예시). 구조보다 현장 지혈을 먼저 합니다.</p>`:''}
    <div class="grid2"><div class="box well"><h3>부위별 부상·기능</h3>${anatomyBody(h)}
    <h3>질병·면역</h3>${h.disease?`<p>${h.disease.name} · 진행 ${Math.round(h.disease.severity)}% · 면역 ${Math.round(h.disease.immunity)}%</p><p>다음 처치: ${h.disease.due<=CARE.clock?'지금':h.disease.due-CARE.clock+'시간 뒤'}</p>`:'<p>증상 없음</p>'}
    <p>의식 ${h.dead?0:Math.max(0,100-carePain(h))}% · 회복 휴식 ${h.recovery}시간</p></div>
    <div class="box well"><h3>치료 방침</h3>${careSelect('허용 치료','carePolicy',h.policy,CARE_POLICY.map((n,i)=>[i,n]))}
    <label class="row"><input type="checkbox" data-input="careSelf" ${h.self?'checked':''}>의식이 있으면 자가 처치 허용(품질 감소)</label>
    ${careSelect('병상 휴식 우선순위','careRest',h.restPriority,Array.from({length:10},(_,i)=>[i,i?i:'0 · 휴식 작업 금지']))}
    <p>${MED_NAMES.map((n,i)=>`${n} ${CARE.medicine[i]}`).join(' · ')}</p><p class="dim">일반 처치는 약이 없어도 가능. 투약·수술은 허용 약품 재고가 있어야 지시할 수 있습니다. 청결도 보정 없음.</p><p>${grade<0?'투약·수술 불가: 허용 약품 재고 없음.':''} ${!medic?'의료 장수 대기.':''} ${h.quality!=null?'최근 처치 품질 '+h.quality+'%':''}</p>
    <div class="row">${lgButton('careTend',h.tendRequested?'처치 취소':'처치 지시',patient,h.dead||h.policy===0)}${lgButton('careDose',h.doseGrade!=null?'투약 취소':'투약 지시',patient,h.dead||(h.doseGrade==null&&(grade<0||(!h.disease&&!h.wounds.length)||h.disease?.name==='식중독')))}${lgButton('careRescue',h.rescue?'구조 취소':'구조 지시',patient,h.dead||h.place!=='현장')}${lgButton('careFeed',h.feeding?'급식 취소':'급식 지시',patient,h.dead||careMobile(h)||!CARE.food)}</div>
    <p>처치: ${h.tendRequested?'대기':'없음'} · 투약: ${h.doseGrade!=null?MED_NAMES[h.doseGrade]+' 예약':'없음'} · 구조: ${h.rescue?'운반 대기':'없음'} · 급식: ${h.feeding?'의료 작업 대기':'없음'}</p>
    ${lgButton('careDeploy','직접 출전',patient,!careMobile(h)||!!bleed)}<p class="dim">구조는 운반 가능한 장수, 처치·투약·수술·환자 급식은 의료 작업입니다. 장수 환자는 치료 중에도 배고픔이 진행됩니다.</p></div></div>
    <div class="box well"><h3>수술 목록</h3>${careSurgeryControls(c)}${careSelect('집도의','careSurgeon',h.surgeon,[['best','가능한 최고 의료 장수'],...C.filter(x=>!x.young).map(x=>[x.id,`${x.name} · 의료 ${x.skills.medicine}`])])}
    <p>${op?`${op.part} · ${op.phase} · 진행 ${op.progress}% · ${op.work?`기여 의료 ${(op.skillWork/op.work).toFixed(1)}`:'시작 전'}`:'등록된 수술 없음'}${op?.result?' · '+op.result:''}</p>
    <div class="row">${lgButton('careQueue','수술 등록',patient,!!op||!!careProcedureReason(h))}${lgButton('careStart','수술 시작',patient,op?.phase!=='준비'||!!careOperationWait(h))}${lgButton('careCancel','수술 취소',patient,op?.phase!=='준비')}${lgButton('carePause',op?.phase==='중단'?'수술 재개':'수술 중단(집도의 이탈)',patient,!op||!['수술 중','중단'].includes(op.phase))}${lgButton('careRetry','재수술 검토',patient,op?.phase!=='실패')}</div>
    <p class="dim">병상·마취 필요. 준비 때 약품 예약, 시작 때 소비. 시작 후 취소 불가, 집도의 교대 시 실제 작업 기여도를 합산합니다. 실패는 악화·사망 가능, 자동 재시도 없음. 성공은 해당 부위만 치료하며 이후 회복 시간이 필요합니다.</p></div>`;
}
function carePanel() {
  const c=carePatient();
  return `<div class="row">${C.map(x=>lgButton('carePick',x.name,`data-id="${x.id}" aria-pressed="${x===c}"`)).join('')}</div>${careBody(c)}
    <details class="box well"><summary>시안 상태 진행</summary><p class="dim">아래 버튼은 의료 작업 1단계를 재현합니다. 게임 일시정지·협상 중에는 진행되지 않습니다. 병상 ${CARE.beds} / 바닥 치료 자리 ${CARE.floorBeds} · 환자 식사 ${CARE.food}</p>${lgButton('careStep','의료 작업·1시간 진행(시안)','',S.paused||!!S.negoPause)}${lgButton('careScenario','식중독 예시','data-kind="food"')}${lgButton('careScenario','전염병 예시','data-kind="disease"')}${lgButton('careScenario','행동 불가 예시','data-kind="down"')}${lgButton('careScar','흉터 예시')}${lgButton('careMissing','부위 상실 예시')}</details>`;
}
function careDeath(c,cause) {
  const h=c.health;h.dead=true;c.hp=0;c.job='사망';c.deployed=false;
  if(h.doseGrade!=null){CARE.medicine[h.doseGrade]++;delete h.doseGrade;}
  if(h.surgery?.phase==='준비'){const op=h.surgery;CARE.medicine[op.grade]++;if(op.material)CARE.materials++;if(op.prosthetic)CARE.prosthetics++;h.surgery=null;}
  h.tendRequested=h.rescue=h.feeding=false;
  c.locked=JOBS.map(([k])=>k);Object.keys(c.prio).forEach(k=>c.prio[k]=0);
  const gear=c.equip.filter(([,item])=>item).map(([,name])=>({name,qty:1,fresh:null,location:c.away?'원정 정착지 · 압수 보관':'사망 지점'}));
  if(c.away)EXP.tradeGoods.push(...gear);else LG.floor.push(...gear);
  c.equip=c.equip.map(([slot])=>[slot,null]);
  COLONY.graves.push({id:Math.max(...COLONY.graves.map(g=>g.id))+1,name:c.name,date:'현재 날짜(시안)',cause,body:c.away?null:'시신',buried:false,memorial:false,request:false});
  toast({kind:'crisis',title:c.name+' 사망',text:c.away?'원정 장비가 압수되었습니다.':'시신과 장비가 현장에 남았습니다.',go:()=>openWin('colony',{tab:'묘비·추모'})});
}
function careStep(random=Math.random) {
  if(S.paused||S.negoPause)return;
  CARE.clock++;
  for(const c of C) {
    const h=c.health;if(h.dead||c.away)continue;
    const m=careMedic(h), active=m&&!m.away&&careMobile(m.health)&&!m.health.dead;
    if(h.tendRequested&&h.policy>0&&(active||h.self&&careMobile(h))) {h.wounds.forEach(w=>{w.tended=true;w.nextTend=CARE.clock+6;});h.tendRequested=false;h.quality=Math.min(100,25+(active?m.skills.medicine:c.skills.medicine*.6)*3);if(h.disease)h.disease.due=CARE.clock+6;}
    if(h.doseGrade!=null&&active){if(h.disease){h.disease.due=CARE.clock+6;h.disease.treated=true;}h.wounds.forEach(w=>w.pain=Math.max(0,w.pain-3));delete h.doseGrade;}
    if(h.rescue&&!careBleed(h)&&(CARE.beds||CARE.floorBeds)){h.place=CARE.beds?'병상':'바닥 치료 자리';if(CARE.beds)CARE.beds--;else CARE.floorBeds--;h.rescue=false;}
    if(h.feeding&&active&&CARE.food){CARE.food--;c.need[0]=Math.max(0,c.need[0]-30);h.feeding=false;}
    c.need[0]=Math.min(100,c.need[0]+2);
    if(h.recovery)h.recovery--;
    const op=h.surgery;
    if(op?.phase==='수술 중'&&active){op.progress+=25;op.work+=25;op.skillWork+=25*m.skills.medicine;
      if(op.progress>=100){const success=clamp(.35+op.skillWork/op.work*.025+c.hp*.002,.1,.95);if(random()<success){careCompleteProcedure(h,op);c.injuries=h.wounds.map(w=>[w.part,w.kind]);h.recovery=6;h.surgery=null;}else{op.phase='실패';if(random()<.1){careDeath(c,'수술 중 사망');op.result='환자 사망';}else{if(op.wound)op.wound.pain=Math.min(100,op.wound.pain+15);else h.wounds.push({part:op.part,kind:'수술 손상',pain:15,bleed:4,tended:false});op.result='부상 악화';}}}}
    careAlerts(c);
    if(h.disease){h.disease.severity=clamp(h.disease.severity+(h.disease.treated?1:3),0,100);h.disease.immunity+=h.disease.treated?8:4;if(h.disease.immunity>=100)h.disease=null;}
  }
  renderAll();
}
Object.assign(ACT, {
  carePick:d=>{CARE.selected=d.id;renderWin();},
  careTend:d=>{const h=byId(d.id).health;if(h.policy&&!h.dead)h.tendRequested=!h.tendRequested;renderWin();},
  careDose:d=>{const h=byId(d.id).health,g=careGrade(h);if(h.doseGrade!=null){CARE.medicine[h.doseGrade]++;delete h.doseGrade;}else if(g>=0&&(h.disease||h.wounds.length)&&h.disease?.name!=='식중독'){CARE.medicine[g]--;h.doseGrade=g;}renderWin();},
  careRescue:d=>{const h=byId(d.id).health;h.rescue=!h.rescue;renderWin();},careFeed:d=>{const h=byId(d.id).health;h.feeding=!h.feeding;renderWin();},
  careDeploy:d=>{const c=byId(d.id);if(careMobile(c.health)&&!careBleed(c.health)){c.deployed=true;c.health.rest=false;feedback('직접 출전 지시');}renderAll();},
  careQueue:d=>{const h=byId(d.id).health,w=h.wounds[h.surgeryPart],g=careGrade(h);if(!w||h.surgery||g<0)return;confirmBox('수술 등록',`${esc(w.part)} 수술을 준비합니다. 실패 시 부상 악화·사망 위험이 있습니다.`, '등록',()=>{if(h.surgery||CARE.medicine[g]<1)return;CARE.medicine[g]--;h.surgery={part:w.part,wound:w,grade:g,phase:'준비',progress:0,work:0,skillWork:0};renderWin();},true);},
  careStart:d=>{const h=byId(d.id).health;if(h.surgery?.phase==='준비'&&h.place!=='현장')h.surgery.phase='수술 중';renderWin();},
  careCancel:d=>{const h=byId(d.id).health;if(h.surgery?.phase==='준비'){CARE.medicine[h.surgery.grade]++;h.surgery=null;}renderWin();},
  carePause:d=>{const op=byId(d.id).health.surgery;if(op&&['수술 중','중단'].includes(op.phase))op.phase=op.phase==='중단'?'수술 중':'중단';renderWin();},
  careRetry:d=>{const h=byId(d.id).health;if(h.surgery?.phase==='실패'&&!h.dead)h.surgery=null;renderWin();},careStep:()=>careStep(),
  careScenario:d=>{const h=carePatient().health;if(d.kind==='down')h.wounds.push({part:'가슴',kind:'중상',pain:75,bleed:10,tended:false});else h.disease={name:d.kind==='food'?'식중독':'전염성 질환',severity:10,immunity:0,due:CARE.clock};renderWin();}
});
Object.assign(INPUT, {
  carePolicy:el=>{carePatient().health.policy=+el.value;renderWin();},careSelf:el=>carePatient().health.self=el.checked,careRest:el=>carePatient().health.rest=el.checked,
  carePart:el=>carePatient().health.surgeryPart=+el.value,careSurgeon:el=>{carePatient().health.surgeon=el.value;renderWin();}
});
const careCommander=winCommander;
winCommander=function(arg){const r=careCommander(arg);if(S.tab.commander==='건강'){const c=byId(arg.id||(S.sel.ids||[])[0])||C[0];CARE.selected=c.id;r.body=tabs('commander',['개요','기분','특성','관계','장비','건강'])+careBody(c)+lgButton('open','의료 작업 관리','data-w="colony" data-tab="의료"');}return r;};
