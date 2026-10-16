const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const { pathToFileURL } = require('node:url');
const path = require('node:path');
(async()=>{
 const browser=await chromium.launch();
 try {
 const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 await page.goto(pathToFileURL(path.join(__dirname,'hud-v5.html')).href);
 await page.evaluate(()=>{S.paused=true;});
 let checks=0;const ok=(v,m)=>{assert.ok(v,m);checks++;};
 const click=s=>page.locator(s).click();
 await click('#menu [data-w=logistics]');
 ok(await page.locator('#winTitle').textContent()==='물류·식품 관리','new menu opens');
 const total=await page.evaluate(()=>LG.stores[0].batches.reduce((n,b)=>n+b.qty,0));
 await page.locator('[data-input=lgAllow][data-name="날것"]').uncheck();
 ok(await page.evaluate(()=>LG.floor.some(b=>b.name==='날것'&&b.qty===12&&b.fresh===25)),'disallow preserves quantity and freshness on floor');
 await click('[data-act=lgEmpty]');
 ok(await page.evaluate(()=>LG.stores[0].batches.length===0&&LG.floor.reduce((n,b)=>n+b.qty,0))===total,'empty conserves inventory');
 await page.evaluate(()=>LG.stores.forEach(s=>Object.keys(s.allow).forEach(k=>s.allow[k]=false)));
 await click('[data-act=lgHaul]');ok(await page.evaluate(()=>LG.floor.length)===2,'no receiver stays on floor');
 await page.evaluate(()=>LG.stores[0].allow.判材=true); // unrelated key must not accept the existing item
 await click('[data-act=lgHaul]');ok(await page.evaluate(()=>LG.floor.length)===2,'acceptance exact item');
 await page.evaluate(()=>{LG.stores[0].allow.판재=true;});await click('[data-act=lgHaul]');
 ok(await page.evaluate(()=>LG.stores[0].batches[0].qty)===8,'allowed ground stock hauled');
 await page.evaluate(()=>{LG.stores[1].allow.판재=true;LG.stores[1].priority=9;});await click('[data-act=lgHaul]');
 ok(await page.evaluate(()=>LG.stores[0].batches[0].qty)===8,'higher priority never relocates stored stock');
 await click('[data-act=lgStore][data-i="2"]');const fresh=await page.evaluate(()=>LG.stores[2].batches[0].fresh);
 await click('[data-act=lgSpoil]');ok(await page.evaluate(()=>LG.stores[2].batches[0].fresh)===fresh,'freezer prevents decay');
 await click('[data-act=lgPower]');await click('[data-act=lgCold]');ok(await page.evaluate(()=>LG.stores[2].cold)===.75,'outage gradual warming');
 await click('[data-act=lgPower]');await click('[data-act=lgCold]');ok(await page.evaluate(()=>LG.stores[2].cold)===1,'power recools');
 await click('[data-act=tab][data-n="식단"]');await page.selectOption('[data-input=lgDiet]','관우');await page.locator('[data-input=lgDietAllow][data-name="날것"]').uncheck();
 ok(await page.evaluate(()=>!LG.diets.관우.날것&&LG.diets.공통.날것),'individual diet independent');
 await click('[data-act=lgCopy]');await page.selectOption('[data-input=lgDiet]','유비');await click('[data-act=lgPaste]');
 ok(await page.evaluate(()=>!LG.diets.유비.날것),'diet copy paste');
 await click('[data-act=lgResetDiet]');ok(await page.evaluate(()=>!LG.diets.유비),'reset inheritance');
 await click('[data-act=tab][data-n="건설·작업"]');await click('[data-act=lgSelectAll]');await click('[data-act=lgBulk][data-mode=pause]');
 ok(await page.evaluate(()=>LG.jobs.every(j=>j.paused&&j.reserved===j.delivered)),'pause releases undelivered reservations');
 await click('[data-act=lgCancel][data-id="1"]');ok(await page.evaluate(()=>LG.jobs.length===3&&LG.jobs.every(j=>j.paused)),'individual cancel after bulk');
 await click('[data-act=lgBulk][data-mode=resume]');await page.selectOption('[data-input=lgBulkPriority]','9');
 ok(await page.evaluate(()=>LG.jobs.every(j=>j.priority===9&&!j.paused)),'bulk priority resume');
 await click('[data-act=lgAdvance][data-id="2"]');ok(await page.evaluate(()=>LG.jobs[0].delivered===30&&LG.jobs[0].phase==='시공 대기'),'delivery before build');
 await click('[data-act=lgAdvance][data-id="2"]');ok(await page.evaluate(()=>LG.jobs[0].phase.includes('소비 완료')),'start consumes');
 await click('[data-act=tab][data-n="퇴비·비료"]');await click('[data-act=lgCompostAdvance]');await click('[data-act=lgCompostOut]');
 ok(await page.evaluate(()=>LG.compost.progress===0&&LG.floor.some(b=>b.name==='썩은 식품')),'compost withdrawal resets progress');
 await page.evaluate(()=>openWin('duty'));await page.evaluate(()=>{C[0].prio.repair=9;renderWin();});await click('[data-act=duty][data-id=gw][data-k=repair]');
 ok(await page.evaluate(()=>C[0].prio.repair)===0,'priority 9 wraps to forbidden');
 await page.locator('[data-act=duty][data-id=gw][data-k=repair]').click({button:'right'});ok(await page.evaluate(()=>C[0].prio.repair)===9,'right click wraps to 9');
 await page.evaluate(()=>{openWin('production',{id:'cook'});});await click('[data-act=addOrder][data-i="0"]');await click('[data-act=lgProductionPause]');
 ok(await page.evaluate(()=>{S.paused=false;const p=S.orders.cook[0].prog;tick();S.paused=true;return S.orders.cook[0].prog===p;}),'production pause preserves progress');
 await page.evaluate(()=>{S.orders.cook[0].prog=.5;renderWin();});await click('[data-act=delOrder]');ok(await page.locator('#confirm').evaluate(e=>e.open),'started production confirms loss');await click('#confirm [value=cancel]');ok(await page.evaluate(()=>S.orders.cook.length)===1,'declining cancel preserves order');
 await page.evaluate(()=>{S.buildCat=2;S.buildItem='冷';openWin('build');});ok(await page.locator('[data-act=buildItem][data-n="냉동고"]').count()===1,'cold storage build catalog');
 for(const width of [1440,1280,1000,720]){await page.setViewportSize({width,height:900});for(const tab of ['창고','식단','건설·작업','퇴비·비료']){await page.evaluate(t=>openWin('logistics',{tab:t}),tab);ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`${width} ${tab} page width`);ok(await page.locator('.win-body').evaluate(e=>e.scrollWidth<=e.clientWidth+1),`${width} ${tab} panel width`);}}
 ok(!errors.length,errors.join('\n'));console.log(`PASS ${checks} logistics browser checks`);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
