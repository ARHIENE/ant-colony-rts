/* V6: V5 상태·명령을 재사용하고 정보의 노출 위치만 바꾼다. */
(() => {
  const detail = document.querySelector('#v6-detail');
  const oldCenter = renderCenter;
  renderCenter = function () {
    oldCenter();
    const kind = kindOf(), panel = document.querySelector('.console');
    panel.hidden = kind === 'none';
    if (kind === 'none') { if (detail.open) detail.close(); return; }
    const c = selCommanders()[0];
    const b = kind === 'building' ? bld(S.sel.id) : null;
    const a = kind === 'animal' ? animal(S.sel.id) : null;
    const title = kind === 'multi' ? `장수 ${selCommanders().length}명` : b?.name || c?.name || a?.name || '동물';
    const status = b ? '시설 선택 · 관리 명령' : c ? c.job : '사육 · 건강 관리';
    document.querySelector('#v6-summary').innerHTML = `<div class="v6-avatar">${ic(b ? 'house' : a ? 'paw' : 'users',24)}</div><div class="v6-identity"><strong>${esc(title)}</strong><span>${esc(status)}</span>${c && kind !== 'multi' ? `<small>체력 ${c.hp} / 100 <span>기분 ${c.mood}</span></small>` : ''}</div><button class="btn" id="v6-details">상세 ↗</button><button class="btn" data-act="selNone" aria-label="선택 해제">×</button>`;
  };
  const oldCmd = renderCmd;
  renderCmd = function () { oldCmd(); document.querySelectorAll('#cmd .nil, #cmd .c').forEach(e => { if(e.classList.contains('nil') || e.textContent.trim()==='상세') e.remove(); }); };
  const oldOpen = openWin;
  openWin = function (...args) { if (detail.open) detail.close(); return oldOpen(...args); };
  document.addEventListener('click', e => {
    if (e.target.closest('#v6-details')) detail.showModal();
    if (e.target.closest('#v6-detail-close')) detail.close();
  });
  document.querySelector('#v6-map-toggle').onclick = e => {
    const body = document.querySelector('#v6-map-body'); body.hidden = !body.hidden;
    e.currentTarget.textContent = body.hidden ? '지도 펼치기' : '지도 접기';
    e.currentTarget.setAttribute('aria-expanded', String(!body.hidden));
  };
  document.querySelector('#v6-demo').onclick = e => {
    const show = document.body.classList.toggle('show-demo');
    e.currentTarget.setAttribute('aria-expanded', String(show));
  };
  select('none');
})();
