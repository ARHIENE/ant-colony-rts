// 실행: node design/hud/hud-v4.check.cjs (브라우저 없이 상태 전환만 검사)
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
function element(){
  return {innerHTML:'worker original',textContent:'',value:'',style:{},attrs:{},events:{},
    classList:{toggle(){}},setAttribute(k,v){this.attrs[k]=v;},
    addEventListener(k,v){this.events[k]=v;}};
}
const buttons = Array.from({length:4},()=>{
  const b=element(), label=element(), icon=element();
  b.querySelector=s=>s==='span'?label:icon; return b;
});
const elements = Object.fromEntries(['.console .center','.cmd2','.scene','preview-state','preview-feedback'].map(k=>[k,element()]));
const context=vm.createContext({document:{
  querySelector:s=>elements[s],getElementById:id=>elements[id],
  querySelectorAll:s=>s==='.cmd2 .c'?buttons:[]
}});
vm.runInContext(fs.readFileSync(__dirname+'/hud-v4.js','utf8'),context);
for(const key of ['none','worker','multi','building','combat']){
  vm.runInContext(`showState('${key}')`,context);
  assert.equal(elements['preview-state'].value,key);
  assert.ok(buttons.every(b=>b.querySelector('span').textContent));
}
vm.runInContext("showState('multi')",context); assert.equal(buttons[1].disabled,true);
vm.runInContext("showState('worker')",context);
assert.equal(elements['.console .center'].innerHTML,'worker original');
assert.ok(buttons.every(b=>!b.disabled));
for(const [location,state] of [['bedroom','building'],['battle','combat']]){
  vm.runInContext(`focusLocation('${location}')`,context);
  assert.equal(elements['preview-state'].value,state);
  assert.notEqual(elements['.scene'].attrs.viewBox,'0 0 1440 660');
  assert.ok(elements['preview-feedback'].textContent);
}
elements['preview-state'].value='none'; elements['preview-state'].events.change();
assert.equal(elements['.scene'].attrs.viewBox,'0 0 1440 660');
assert.equal(elements['preview-feedback'].textContent,'');
console.log('PASS: five selection states, command availability, worker restoration, alert focus and view reset');
