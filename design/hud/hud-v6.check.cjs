const assert=require('node:assert/strict');
const {chromium}=require('playwright');
(async()=>{
  const browser=await chromium.launch();
  try {
    const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    await page.goto('file:///'+require('node:path').resolve(__dirname,'hud-v6.html').replaceAll('\\','/'));
    assert.equal(await page.locator('.console').isVisible(),false);
    await page.click('[data-v6=manage]');
    for(const tab of ['자원','생산','장비·도구','수리','매장지·배관','음주']){
      await page.click(`[data-v6=tab][data-tab="${tab}"]`);
      assert.ok(await page.locator('#v6-management').evaluate(e=>e.scrollWidth<=e.clientWidth+1));
    }
    await page.click('[data-tab="자원"]');await page.fill('#v6-search','송로');
    assert.equal(await page.locator('.v6-catalog article').count(),1);
    await page.click('[data-tab="생산"]');
    await page.selectOption('[data-v6-input=mode]','계속 생산');
    assert.equal(await page.locator('[data-v6-input=quantity]').count(),0);
    await page.click('[data-v6=addOrder]');await page.click('[data-v6=advanceOrder]');
    await page.click('[data-v6=deleteOrder]');assert.match(await page.locator('.v6-orders').innerText(),/미완성품 보존/);
    await page.click('[data-tab="장비·도구"]');
    await page.selectOption('[data-v6-tool="곡괭이"]','청동');
    await page.click('[data-act=gearOff][data-i="0"]');
    assert.match(await page.locator('#v6-management').innerText(),/관우 주변 바닥/);
    await page.click('[data-tab="자원"]');await page.click('[data-tab="장비·도구"]');
    assert.equal(await page.locator('[data-v6-tool="곡괭이"]').inputValue(),'청동');
    await page.click('[data-tab="수리"]');await page.click('[data-v6=selectRepairs]');await page.click('[data-v6=assignRepairs]');
    await page.click('[data-v6=repairStep][data-id="1"]');await page.click('[data-v6=repairPause][data-id="1"]');
    const paused=await page.locator('.v6-orders article').first().innerText();
    await page.click('[data-v6=repairStep][data-id="1"]');assert.equal(await page.locator('.v6-orders article').first().innerText(),paused);
    await page.selectOption('[data-v6-repair="1"]','jg');assert.equal(await page.locator('.v6-change').count(),1);
    await page.getByRole('button',{name:'견적 확인 후 변경'}).click();
    assert.equal(await page.locator('[data-v6-repair="1"]').inputValue(),'jg');
    await page.click('[data-v6=cancelRepair][data-id="1"]');
    assert.match(await page.locator('.v6-orders article').first().innerText(),/취소/);
    await page.click('[data-tab="매장지·배관"]');assert.equal(await page.locator('.v6-catalog article').count(),3);
    await page.click('[data-v6=survey]');assert.equal(await page.locator('.v6-catalog article').count(),4);
    await page.click('[data-v6=overlay]');assert.equal(await page.locator('#v6-deposits').count(),1);
    await page.click('[data-tab="음주"]');assert.equal(await page.locator('[data-v6-drink=allow]:disabled').count(),1);
    await page.click('[data-v6=dismiss]');
    await page.click('#v6-map-toggle');assert.equal(await page.locator('#v6-map-body').isVisible(),false);
    await page.click('#v6-map-toggle');
    await page.click('#v6-demo');
    for(const viewport of [{width:1440,height:900},{width:1280,height:720},{width:960,height:720}]){
      await page.setViewportSize(viewport);
      for(const kind of ['none','commander','multi','deployed','building','animal']){
        await page.click(`[data-act=preview][data-k=${kind}]`);
        if(kind==='none'){assert.equal(await page.locator('.console').isVisible(),false);continue;}
        const box=await page.locator('.console').boundingBox();assert.ok(box.height<=163,`${kind}: card height`);
        assert.ok(await page.locator('.console').evaluate(e=>e.scrollHeight<=e.clientHeight+1),`${kind}: card overflow ${viewport.width}`);
        await page.click('#v6-details');assert.equal(await page.locator('#v6-detail').evaluate(e=>e.open),true);
        await page.click('#v6-detail-close');
      }
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
    }
    assert.deepEqual(errors,[]);console.log('V6: layout, selection, details, catalog, production, repair, deposits, drinking checks passed.');
  } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
