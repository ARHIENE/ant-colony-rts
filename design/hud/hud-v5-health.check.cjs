const assert = require('node:assert/strict');
const path = require('node:path');
const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage(); const errors = [];
    page.on('pageerror', e => errors.push(e.message));
    await page.goto('file:///' + path.resolve(__dirname, 'hud-v5.html').replace(/\\/g, '/'));
    const result = await page.evaluate(() => {
      S.paused = true;
      const a = live().find(a => !a.clinic), surgeon = surgeons()[0];
      a.seq = 0; const chance = chanceFor(a, surgeon), death = deathFor(a, surgeon);
      a.seq = 20;
      const noPenalty = chance === chanceFor(a, surgeon) && death === deathFor(a, surgeon);
      infectAnimal(a, 'contagious');
      const hidden = !symptomatic(a) && !infectious(a);
      tickAnimalDisease(a, .3);
      const exposed = symptomatic(a) && infectious(a);
      S.win = 'animal'; S.winArg = { id: a.id };
      ACT.diseaseDose({ id: String(a.id) });
      const reservedOnce = reservedMap.medicine;
      ACT.diseaseDose({ id: String(a.id) });
      const noDuplicate = reservedMap.medicine === reservedOnce;
      const elapsed = a.disease.elapsed; tick();
      const paused = a.disease.elapsed === elapsed;
      a.disease.medication = 1; tickAnimalDisease(a, .1);
      const notInstantCure = infectious(a);
      tickAnimalDisease(a, 4);
      const recovered = !a.disease;
      return { noPenalty, hidden, exposed, noDuplicate, paused, notInstantCure, recovered };
    });
    for (const [name, passed] of Object.entries(result)) assert.ok(passed, name);
    assert.deepEqual(errors, []);
    console.log('PASS 8 V5 health checks (no captures)');
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
