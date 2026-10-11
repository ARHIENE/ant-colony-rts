// 실행: node design/hud/hud-v5.check.cjs  (Playwright Chromium으로 HUD v5 시안을 실제 브라우저에서 확인)
// 주요 화면 전환·입력·확인/취소·비활성 상태·화면 폭별 잘림·콘솔 오류를 검사하고 스크린샷을 reports/hud-v5/에 남긴다.
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');
const { chromium } = require('playwright');

const url = 'file:///' + path.resolve(__dirname, 'hud-v5.html').replace(/\\/g, '/');
const out = path.resolve(__dirname, '../../reports/hud-v5');
fs.mkdirSync(out, { recursive: true });

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(url); await page.waitForTimeout(500);
  const shot = n => page.screenshot({ path: path.join(out, n + '.png') });
  const cmd = () => page.$$eval('#cmd .c', els => els.map(e => (e.classList.contains('nil') ? '·' : e.textContent.trim()) + (e.disabled ? '(off)' : '')));
  const click = async sel => { await page.click(sel); await page.waitForTimeout(80); };
  let checks = 0; const ok = (v, m) => { assert.ok(v, m); checks++; };

  // 1. 선택 상태별 3×3 명령 칸(항상 9칸), 징집소 바로가기 없음
  for (const k of ['none', 'commander', 'multi', 'deployed', 'building', 'animal']) {
    await click(`[data-act=preview][data-k=${k}]`);
    const c = await cmd(); ok(c.length === 9, `${k}: 9 slots`);
    ok(!c.some(x => x.startsWith('징집소')), `${k}: no conscription shortcut`);
    await shot('state-' + k);
  }
  await click('[data-act=preview][data-k=none]');
  ok((await cmd()).join() === '작업표,장수 관리,건설,연구,인구,외교,우선 지정,단계 5,빨간 경보', 'none card matches spec');
  await click('[data-act=preview][data-k=multi]'); ok((await cmd())[1] === '개별 선택 필요(off)', 'multi civilian rest needs individual');

  // 2. 상단 재료 세부·인구 납세 시간
  await click('#res [data-act=pop]'); ok(await page.isVisible('#pop'), 'materials popover opens');
  ok((await page.textContent('#pop')).includes('동물 수술 키트'), 'materials list includes medical stock');
  await page.keyboard.press('Escape'); ok(await page.isHidden('#pop'), 'Esc closes popover');
  ok(/다음 납세 \d+:\d\d/.test(await page.textContent('#res')), 'next tax time on top bar');

  // 3. 알림 위치 이동(알림 유지) + 선택
  const before = await page.getAttribute('#scene', 'viewBox');
  await click('.toast.crisis .go'); await page.waitForTimeout(300);
  ok(await page.getAttribute('#scene', 'viewBox') !== before && await page.isVisible('.toast.crisis'), 'toast location moves camera, toast stays');
  ok((await cmd()).includes('공격 이동'), 'battle toast selects deployed commander');

  // 4. 우선 지정·단계·빨간 경보
  await page.evaluate(() => { S.view = [0, 0, 1440, 660]; renderAll(); }); // 전장으로 옮긴 카메라를 되돌린다(지도 대상 클릭용)
  await click('[data-act=preview][data-k=none]'); await click('#cmd [data-act=prioLevel]'); ok((await cmd())[7] === '단계 6', 'priority level cycles');
  await click('#cmd [data-act=prioMode]'); await click('[data-act=prioTarget][data-id=rock]');
  ok(await page.$('#scene text.prio') !== null, 'target priority badge on map');
  await page.keyboard.press('Escape'); await click('#cmd [data-act=red]'); ok(await page.isVisible('.mode-chip.red'), 'red alert banner');
  await click('#cmd [data-act=red]');

  // 5. 작업표 15종: 클릭 올림·우클릭 내림·잠금
  await click('#cmd [data-act=open][data-w=duty]');
  ok((await page.$$('.duty th')).length === 16, 'duty table has 15 jobs');
  const cell = '.cell[data-id=gw][data-k=repair]'; const p0 = await page.getAttribute(cell, 'data-p');
  await click(cell); ok(await page.getAttribute(cell, 'data-p') === String((+p0 + 1) % 6), 'click raises priority');
  await page.click(cell, { button: 'right' }); await page.waitForTimeout(80); ok(await page.getAttribute(cell, 'data-p') === p0, 'right click lowers');
  ok(await page.$('.cell.locked[data-id=jb][data-k=art]') !== null, 'trait-locked cell');
  await shot('win-duty'); await page.keyboard.press('Escape'); await page.waitForTimeout(250); ok(await page.isHidden('#window .duty'), 'Esc closes window');

  // 6. 인구·세금: 슬라이더·숫자 입력·방침
  await click('#res .pop-btn'); const inc0 = await page.textContent('#taxLine');
  await page.fill('#taxNum', '20'); await page.press('#taxNum', 'Tab'); await page.waitForTimeout(100);
  ok(await page.inputValue('#taxRange') === '20' && (await page.textContent('#taxLine')) !== inc0, 'number input updates slider and income');
  await page.$eval('#taxRange', el => { el.value = '7'; el.dispatchEvent(new Event('input', { bubbles: true })); });
  ok(await page.waitForFunction(() => document.querySelector('#taxNum')?.value === '7', null, { timeout: 3000 }).then(() => true, () => false), 'slider updates number');
  await click('[data-act=focus][data-v="식량 중심"]'); ok(await page.getAttribute('[data-act=focus][data-v="식량 중심"]', 'aria-pressed') === 'true', 'tax focus');
  ok(await page.isDisabled('[data-act=policy][data-v=국민개병]'), 'locked policy disabled');
  await shot('win-population'); await page.keyboard.press('Escape');

  // 7. 건설: 준비 중 분류, 재료 부족 비활성, 개보수·재개발
  await click('[data-act=preview][data-k=none]'); await click('#cmd [data-act=open][data-w=build]');
  await click('[data-act=buildCat][data-i="5"]'); ok((await page.textContent('#window')).includes('준비 중'), 'unbuilt category marked');
  await click('[data-act=buildCat][data-i="10"]'); await click('[data-act=buildItem][data-n=성벽]'); await click('[data-act=buildMat][data-k=block]');
  ok(await page.isDisabled('#window [data-act=place]'), 'shortage disables placing');
  await click('[data-act=buildMat][data-k=pebble]'); await shot('win-build'); await page.keyboard.press('Escape');
  await click('[data-act=preview][data-k=building]'); await click('button[data-act=selB][data-id=hut1]');
  await click('#cmd [data-act=open][data-w=redevelop]'); ok((await page.textContent('#window')).includes('보상비'), 'redevelopment quote'); await shot('win-redevelop'); await page.keyboard.press('Escape');
  await click('button[data-act=selB][data-id=dorm]'); await click('#cmd [data-act=open][data-w=renovate]');
  await click('#window [data-act=renovate][data-k=leaf]'); ok((await page.textContent('#window')).includes('취소(새 재료 100% 반환)'), 'renovation cancel shown');
  await click('#window [data-act=renovateCancel]'); await page.keyboard.press('Escape');

  // 8. 생산 목록: 방식·수량·추가·삭제
  await click('button[data-act=selB][data-id=med]'); await click('#cmd [data-act=open][data-w=production]');
  await click('[data-act=mode][data-v=keep]'); await click('[data-act=amount][data-d="1"]');
  const n0 = (await page.$$('#window [data-act=delOrder]')).length; await click('#window [data-act=addOrder][data-i="1"]');
  ok((await page.$$('#window [data-act=delOrder]')).length === n0 + 1, 'order added'); await click('#window [data-act=delOrder]');
  ok((await page.$$('#window [data-act=delOrder]')).length === n0, 'order removed'); await shot('win-production'); await page.keyboard.press('Escape');

  // 9. 협상: 시간 정지·보류하고 닫기·무효
  await click('[data-act=preview][data-k=none]'); await click('#cmd [data-act=open][data-w=diplomacy]'); await click('[data-act=nego][data-id=cg]');
  ok((await page.textContent('#clock')).includes('협상 중 시간 정지') && await page.isDisabled('#clock [data-act=pause]'), 'negotiation stops time');
  ok(!/\d+%/.test(await page.textContent('.reaction')), 'no success probability shown');
  await click('#window [data-act=propose]'); await shot('win-negotiation');
  await page.keyboard.press('Escape'); ok(!(await page.textContent('#clock')).includes('협상 중'), 'time resumes after close');
  await click('#cmd [data-act=open][data-w=diplomacy]'); await click('[data-act=nego][data-id=jk]');
  ok(await page.$$eval('#window .win-foot .btn', b => b.length) === 1, 'invalid negotiation: confirm only'); await click('#window [data-act=negoInvalid]');

  // 10. 목장: 일괄 요청(제외 사유)·확인·취소, 이름 변경, 치료 시설 제한
  await click('[data-act=preview][data-k=building]'); await click('button[data-act=selB][data-id=pen]'); await click('#cmd [data-act=open][data-w=ranch]');
  for (const id of [3, 5, 12, 16]) await page.check(`[data-input=pick][data-id="${id}"]`);
  await page.waitForTimeout(80);
  ok((await page.textContent('#window')).includes('제외: 치료 시설 안 1'), 'bulk shows exclusion reason');
  await click('#window [data-act=bulk][data-k=slaughter]'); ok(await page.isVisible('#confirm'), 'slaughter confirm opens');
  ok((await page.textContent('#confirmBody')).includes('꿀이(진딧물) 1마리'), 'confirm lists names with counts (renamed: 이름(종))');
  await page.click('#confirm button[value=cancel]'); await page.waitForTimeout(80); ok(!(await page.textContent('#window')).includes('도축 대기'), 'cancel keeps state');
  await click('#window [data-act=bulk][data-k=slaughter]'); await page.click('#confirmOk'); await page.waitForTimeout(80);
  ok((await page.textContent('#window')).includes('도축 대기'), 'slaughter requested');
  await click('#window [data-act=bulkCancel]'); ok(!(await page.textContent('#window')).includes('도축 대기'), 'bulk cancel');
  await page.fill('[data-input=rename][data-id="5"]', '초록이'); await page.waitForTimeout(80); ok(await page.evaluate(() => A.find(a => a.id === 5).name === '초록이(진딧물)'), 'rename keeps 이름(종)');
  await page.fill('[data-input=rename][data-id="5"]', ''); await page.waitForTimeout(80); ok(await page.evaluate(() => A.find(a => a.id === 5).name === '진딧물'), 'empty name falls back to species');
  await page.selectOption('[data-input=flt][data-k=sp]', 'worm'); await page.waitForTimeout(80); ok(await page.$$eval('#window [data-input=pick]', e => e.length) === 2, 'species filter');
  await page.selectOption('[data-input=flt][data-k=sp]', 'all'); await page.selectOption('[data-input=sort]', 'hp'); await page.waitForTimeout(80);
  await shot('win-ranch'); await page.keyboard.press('Escape');
  await click('[data-act=selA][data-id="12"]');
  const c12 = await cmd(); ok(c12.includes('도축 요청(off)') && c12.includes('방생(off)'), 'clinic animal: no slaughter/release');

  // 11. 동물 의료: 고위험 집도의 지정, 키트 부족 비활성
  await click('#cmd [data-act=open][data-w=animal]'); await click('[data-act=tab][data-n=의료]');
  ok((await page.textContent('#window')).includes('수술 키트 사용 가능 0') && await page.isDisabled('#window [data-act=surgeon]'), 'kit shortage disables surgery');
  await shot('win-animal-medical');
  // 수술 흐름: 키트 확보(시안 조작) → 집도의 지정 → 수술 → 성공(회복) 또는 실패(후유증·선택 대기) 또는 사망
  await click('#window [data-act=demoStock]'); await click('#window [data-act=surgeon]'); await click('#window [data-act=surgeryStart]');
  ok(await page.evaluate(() => A.find(a => a.id === 12).clinic.phase === 'surgery'), 'surgery started');
  await page.evaluate(() => { S.speed = 5; }); await page.waitForTimeout(6500);
  const phase = await page.evaluate(() => { const a = A.find(x => x.id === 12); return a.dead ? 'dead' : a.clinic?.phase; });
  ok(['recovery', 'failed', 'dead', 'ready'].includes(phase), 'surgery resolved: ' + phase);
  if (phase === 'failed') ok((await page.textContent('#window')).includes('재수술'), 'failure offers manual re-surgery');
  await shot('win-animal-surgery-' + phase); await page.evaluate(() => { S.speed = 2; });
  await page.keyboard.press('Escape');
  await click('[data-act=selA][data-id="5"]'); await click('#cmd [data-act=open][data-w=animal]'); await click('[data-act=tab][data-n=의료]');
  await click('#window [data-act=dose]'); ok((await page.textContent('#window')).includes('예약 1'), 'dose reserves medicine');
  await page.keyboard.press('Escape');

  // 11-1. 리뷰 재현: 수술 실패 후 복귀해도 부상·체력·후유증 유지(완치는 성공 때만)
  ok(await page.evaluate(() => { const a = A.find(x => x.id === 5); a.clinic = { phase: 'failed', result: '부상 유지' }; a.injury = '중상'; a.hp = 25; a.seq = 1;
    ACT.returnPen({ id: '5' }); S.paused = false; for (let i = 0; i < 12 && a.clinic; i++) tick(); return !a.clinic && a.injury === '중상' && a.hp === 25 && a.seq === 1; }), 'return after failed surgery keeps injury');
  await page.evaluate(() => { S.sel = { kind: 'animal', id: 5 }; renderAll(); }); await click('#cmd [data-act=open][data-w=animal]'); await click('[data-act=tab][data-n=의료]');
  ok((await page.textContent('#window')).includes('집도의 지정'), 'returned animal still shows surgery (manual re-surgery)'); await page.keyboard.press('Escape');
  // 11-2. 리뷰 재현: 투약 연속 클릭 → 1개만 예약·소비, 일시정지 중에는 완료되지 않음
  ok(await page.evaluate(() => { const a = A.find(x => x.id === 8); a.injury = '경상'; a.hp = 80; a.medicated = 0; a.dosing = 0; mat('medicine').have = 3; reservedMap.medicine = 0;
    ACT.dose({ id: '8' }); ACT.dose({ id: '8' }); const once = reservedMap.medicine === 1;
    S.paused = true; for (let i = 0; i < 5; i++) tick(); const paused = mat('medicine').have === 3 && a.dosing > 0;
    S.paused = false; for (let i = 0; i < 4; i++) tick(); return once && paused && mat('medicine').have === 2 && reservedMap.medicine === 0 && a.medicated > 0; }), 'double dose click reserves/consumes once; paused time does not complete');

  // 12. 먹이통·도축대
  await click('[data-act=preview][data-k=building]'); await click('button[data-act=selB][data-id=feeder]'); await click('#cmd [data-act=open][data-w=feeder]');
  await click('[data-act=feedTarget][data-i="0"][data-d="-5"]'); await click('[data-act=feedTarget][data-i="0"][data-d="-5"]'); ok((await page.textContent('#feedback')).includes('바닥'), 'lower target spills'); await page.keyboard.press('Escape');
  await click('button[data-act=selB][data-id=butcher]'); await click('#cmd [data-act=open][data-w=butcher]');
  await click('[data-act=butcherPause]'); ok((await page.textContent('#window')).includes('재개'), 'butcher pause');
  await page.uncheck('[data-input=butcherAllow][data-k=worm]'); await page.waitForTimeout(80); ok(await page.evaluate(() => S.butcher.queue.every(e => e.sp !== 'worm')), 'disallowed species removed from queue');
  await shot('win-butcher'); await page.keyboard.press('Escape');

  // 13. 화면 폭별 잘림(가로 스크롤·넘침)
  for (const w of [1440, 1280, 1000, 720]) {
    await page.setViewportSize({ width: w, height: 860 }); await page.waitForTimeout(150);
    const over = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1);
    ok(!over, `no horizontal overflow at ${w}`);
    const clipped = await page.$$eval('#cmd .c span', els => els.filter(e => e.scrollWidth > e.clientWidth + 2).map(e => e.textContent));
    ok(clipped.length === 0, `command labels fit at ${w}: ${clipped}`);
    await shot('width-' + w);
  }
  await page.setViewportSize({ width: 1440, height: 900 });
  // 시간 흐름 중 오류 없음(포식 알림 등)
  await page.evaluate(() => { S.speed = 5; }); await page.waitForTimeout(9000);
  ok(errors.length === 0, 'console errors: ' + errors.join(' | '));
  ok((await page.textContent('#alerts')).includes('잡아먹혔습니다'), 'predation toast after actual damage');
  await shot('after-time');
  await browser.close();
  console.log(`PASS ${checks} HUD v5 browser checks · screenshots → reports/hud-v5/`);
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
