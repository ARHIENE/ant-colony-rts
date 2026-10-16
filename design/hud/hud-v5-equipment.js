'use strict';
const GEAR = {selected:'gw',stock:[{id:1,slot:'무기',name:'산 분사기 · 정교'},{id:2,slot:'갑옷',name:'외골격 코팅 · 정교'},{id:3,slot:'장신구',name:'향기 주머니 · 보통'}],next:4};
const gearBlock=c=>c.health.dead?'사망한 장수':c.away?'원정 중 · 본거지 보관함 접근 불가':c.young?'어린 장수':!careMobile(c.health)?'치료 중 · 이동 불가':'';
function equipmentBody(c){return `<p role="status">${gearBlock(c)||'본거지 장비 보관함 사용 가능'}</p><div class="grid2"><section><h3>${c.name} · 착용 장비</h3><ul class="list">${c.equip.map(([slot,name],i)=>`<li><span class="grow"><b>${slot}</b><br>${name||'비어 있음'}</span>${lgButton('gearOff','해제',`data-id="${c.id}" data-i="${i}"`,!name||!!gearBlock(c))}</li>`).join('')}</ul></section><section><h3>보관함 · ${GEAR.stock.length}개</h3><ul class="list">${GEAR.stock.map(g=>`<li><span class="grow"><b>${g.name}</b><br>${g.slot}</span>${lgButton('gearEquip',c.equip.find(([s])=>s===g.slot)?.[1]?'교체':'장착',`data-id="${c.id}" data-item="${g.id}"`,!!gearBlock(c))}</li>`).join('')||'<li class="dim">보관 장비 없음 · 해제하면 이곳에 보관합니다.</li>'}</ul></section></div><p class="dim">같은 슬롯 장비를 교체하면 기존 장비는 보관함으로 돌아갑니다. 다른 장수의 착용 장비는 자동으로 빼앗지 않습니다.</p>`;}
function winEquipment(){const c=byId(GEAR.selected);return {title:'장비 보관함',sub:'장착·교체·해제',body:careSelect('장수','gearCommander',c.id,C.map(x=>[x.id,x.name]))+equipmentBody(c),foot:lgButton('close','닫기')};}
function gearSync(c){c.weapon=c.equip[0][1]?.split(' · ')[0]||'맨 큰턱';c.wings=!!c.equip[1][1]?.includes('비행 날개');}
Object.assign(ACT,{
  gearEquip:d=>{const c=byId(d.id),g=GEAR.stock.find(g=>g.id===+d.item);if(!g||gearBlock(c))return;const slot=c.equip.find(s=>s[0]===g.slot);if(!slot)return;GEAR.stock=GEAR.stock.filter(x=>x!==g);if(slot[1])GEAR.stock.push({id:GEAR.next++,slot:slot[0],name:slot[1]});slot[1]=g.name;gearSync(c);renderAll();},
  gearOff:d=>{const c=byId(d.id),slot=c.equip[+d.i];if(gearBlock(c)||!slot?.[1])return;GEAR.stock.push({id:GEAR.next++,slot:slot[0],name:slot[1]});slot[1]=null;gearSync(c);renderAll();}
});
INPUT.gearCommander=el=>{GEAR.selected=el.value;renderWin();};
const equipmentCommander=winCommander;
winCommander=function(arg){const r=equipmentCommander(arg);if(S.tab.commander==='장비'){const c=byId(arg.id||(S.sel.ids||[])[0])||C[0];r.body=tabs('commander',['개요','기분','특성','관계','장비','건강'])+equipmentBody(c);}return r;};
