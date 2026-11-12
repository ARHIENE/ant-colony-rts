from pathlib import Path
root=Path('Assets/_Project/Scripts');dest=Path('work/commander-only-1112/pending')
def patch(rel,changes):
 p=root/rel;s=p.read_text(encoding='utf-8-sig')
 for a,b in changes:
  assert a in s,(rel,a);s=s.replace(a,b)
 q=dest/rel;q.parent.mkdir(parents=True,exist_ok=True);q.write_text(s,encoding='utf-8')
patch('UI/CommanderOverhead.cs', [('                if (c.IsDeployed) Bar(new Rect(x + 2 * scale, y + line + detail + 5 * scale, width - 4 * scale, 3 * scale), c.CommandLimit > 0 ? c.TroopHealth / c.CommandLimit : 0, MenuTheme.Accent);',''),('(hurt || c.IsDeployed ? 9 * scale : 0)','(hurt ? 5 * scale : 0)')])
patch('UI/CommandCard.cs', [('            Add(deployedGrid, 8, "Partial Recall", () => "절반 해산", () => { if (Commander != null) Commander.RecallTroops((Commander.TroopCount + 1) / 2); },\n                () => "현재 병력 절반을 반납하고 나머지는 소집을 유지합니다.", () => Commander != null && Commander.CanReceiveOrders && !Commander.IsAwayFromHome && Commander.TroopCount > 0);',''),('소집 상태와 배정 병력은 유지합니다. 병력을 돌려보내려면 귀환하세요.','소집 상태는 유지합니다. 작업에 복귀하려면 귀환하세요.'),('징집소로 돌아가 생존 병력을 반납하고 자율 작업을 재개합니다.','소집 지점으로 돌아가 자율 작업을 재개합니다.'),('지금은 징집소로 귀환할 수 없습니다.','지금은 소집 지점으로 귀환할 수 없습니다.')])
patch('Units/CommanderDeploymentMeal.cs',[('RationPortion * (troopCount + 1)','RationPortion'),('부대 식량','소집 식량')])
print('Prepared 3 pending files; Assets untouched.')
