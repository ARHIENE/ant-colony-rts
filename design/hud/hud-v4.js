// HTML 시안용 고정 예시. 실제 게임 상태와 연결하지 않는다.
const states = {
  none: {title:'군체 현황', text:'장수나 건물을 선택하세요. 평시 장수는 작업표에 따라 일합니다.', stats:['대기 인력 <strong>46</strong>','추가 동원 가능 <strong>23</strong>','숙소 <strong>1명 부족</strong>'], commands:['작업표','장수 관리','징집소','건설']},
  worker: {commands:['우선','휴식','징집소','건설']},
  multi: {title:'장수 3명 선택', text:'공통으로 가능한 명령만 표시합니다. 개별 능력치는 장수를 한 명 선택해 확인합니다.', names:['관우 · 채집','유비 · 연구','손권 · 건설'], stats:['평시 <strong>3명</strong>','배정 인력 <strong>28</strong>'], commands:['우선',null,'징집소','선택 해제']},
  building: {title:'숙소', text:'침대 4개 · 배정 장수 5명', warning:'침대 1개 부족 — 장수 1명이 노숙합니다.', stats:['방 등급 <strong>보통</strong>','침대 <strong>4 / 5</strong>'], commands:['배정 보기','침대 건설','방 정보','선택 해제']},
  combat: {title:'조운 · 출전 중', text:'서쪽 성벽 방어 · 적 3마리 접근', stats:['병력 <strong>16 / 20</strong>','개인 체력 <strong>92 / 100</strong>','근접 <strong>12</strong>'], commands:['공격 이동','정지','귀환','강타']}
};
const center = document.querySelector('.console .center');
const workerMarkup = center.innerHTML;
const commandButtons = [...document.querySelectorAll('.cmd2 .c')];
const picker = document.getElementById('preview-state');
const scene = document.querySelector('.scene');
const feedback = document.getElementById('preview-feedback');
function showState(key){
  const state = states[key];
  if (!state) return;
  picker.value = key;
  center.innerHTML = key === 'worker' ? workerMarkup : `<div class="state-card"><h2>${state.title}</h2><p>${state.text}</p>${state.warning ? `<p class="warning">⚠ ${state.warning}</p>` : ''}${state.names ? `<div class="selection-list">${state.names.map(n=>`<span>${n}</span>`).join('')}</div>` : ''}<div class="summary">${state.stats.map(s=>`<span>${s}</span>`).join('')}</div></div>`;
  commandButtons.forEach((button,i)=>{
    button.disabled = !state.commands[i];
    button.querySelector('span').textContent = state.commands[i] || '개별 선택 필요';
    button.querySelector('svg').style.display = key === 'worker' ? '' : 'none';
    button.title = state.commands[i] || '장수마다 휴식 조건이 달라 개별 선택이 필요합니다.';
    button.classList.toggle('primary',state.commands[i] === '건설' || state.commands[i] === '침대 건설');
  });
  document.querySelector('.cmd2').setAttribute('aria-label','명령: '+(state.title || '관우 평시'));
  document.querySelectorAll('.cm').forEach(button=>{
    const name = button.title.split(' · ')[0];
    const selected = key === 'worker' && name === '관우' || key === 'combat' && name === '조운' || key === 'multi' && ['관우','유비','손권'].includes(name);
    button.classList.toggle('sel',selected); button.setAttribute('aria-pressed',String(selected));
  });
}
function focusLocation(kind){
  const bedroom = kind === 'bedroom';
  scene.setAttribute('viewBox',bedroom ? '280 80 720 330' : '100 300 720 330');
  showState(bedroom ? 'building' : 'combat');
  feedback.textContent = bedroom ? '숙소로 이동 · 침대가 1개 부족합니다.' : '서쪽 성벽으로 이동 · 조운 선택';
}
picker.addEventListener('change',()=>{scene.setAttribute('viewBox','0 0 1440 660');feedback.textContent='';showState(picker.value);});
document.querySelectorAll('[data-location]').forEach(button=>button.addEventListener('click',()=>focusLocation(button.dataset.location)));
document.querySelectorAll('.toast .x').forEach(button=>button.addEventListener('click',()=>button.closest('.toast').remove()));
document.querySelectorAll('.roster .arr').forEach((button,i)=>button.addEventListener('click',()=>document.querySelector('.rlist').scrollBy({left:i ? 180 : -180})));
commandButtons.forEach(button=>button.addEventListener('click',()=>{
  const label = button.querySelector('span').textContent;
  if(label === '선택 해제'){showState('none');feedback.textContent='선택을 해제했습니다.';return;}
  feedback.textContent = `${label} — 시안에서는 명령 선택까지만 표시합니다.`;
}));
showState('worker');
