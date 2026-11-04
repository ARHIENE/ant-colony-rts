from pathlib import Path
R=Path('E:/Git/ant/Assets/_Project/Scripts')
for name in ['Save/SaveSnapshot.cs','Save/SaveBuildings.cs']:
 p=R/name;s=p.read_text(encoding='utf-8-sig')
 s=s.replace('workPaused = nodes[i].WorkPaused,','workPaused = nodes[i].WorkPaused, workRequestOrder = nodes[i].WorkRequestOrder,')
 s=s.replace('workPaused = n != null && n.WorkPaused,','workPaused = n != null && n.WorkPaused, workRequestOrder = n != null ? n.WorkRequestOrder : 0,')
 for target,dto in [('nodes[node.index]','node'),('node','n'),('node','d')]:
  old=f'{target}.WorkPaused = {dto}.workPaused;'
  s=s.replace(old,old+f' {target}.WorkRequestOrder = {dto}.workRequestOrder;')
 p.write_text(s,encoding='utf-8')
p=R/'UI/GameMenuTasks.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('private Component confirmTask;', 'private int taskFilter;\n        private bool waitingOnly;\n        private Component confirmTask;')
s=s.replace('x.CurrentResourceNode == c);','x.CurrentResourceNode == c || x.CraftingWorkshop == c);')
s=s.replace(': c is Kitchen k && k.Current != null ?', ': c is Processor p && p.Current != null ? $"{p.Current.name} {p.Progress:P0}"\n            : c is Workshop w && w.Jobs.Count > 0 ? $"제작 대기 {w.Jobs.Count}건 · 진행 {w.Jobs[0].work:0}"\n            : c is Kitchen k && k.Current != null ?')
s=s.replace('(조리 {losing}건 재료 손실)','(진행 중 {losing}건 재료 손실)')
s=s.replace('content = V5Section(window, "작업 목록", 554, 104, 526, 544);\n            foreach (var c in taskPick.OrderByDescending(WorkPriorities.Level).ToList())', '''content = V5Section(window, "작업 목록", 554, 104, 526, 544);
            var kinds = new[] { "전체", "자연 작업", "건설", "생산" };
            MenuTheme.Button(content, "종류: " + kinds[taskFilter], () => { taskFilter = (taskFilter + 1) % kinds.Length; ShowTasks(taskPick); });
            MenuTheme.Button(content, waitingOnly ? "대기 중인 작업만 표시" : "진행·대기 모두 표시", () => { waitingOnly = !waitingOnly; ShowTasks(taskPick); });
            var visible = taskPick.Where(c => taskFilter == 0 || taskFilter == 1 && c is AntColony.World.ResourceNode
                || taskFilter == 2 && c is BuildingConstructionSite || taskFilter == 3 && (c is Kitchen || c is Processor || c is Workshop))
                .Where(c => !waitingOnly || !TaskCrew(c).Any(x => x.IsGatheringAnimation || x.IsBuildingAnimation || x.ServiceTarget == c || x.CraftingWorkshop == c));
            foreach (var c in visible.OrderByDescending(WorkPriorities.Yellow).ThenByDescending(WorkPriorities.Level).ThenBy(WorkPriorities.RequestedAt).ToList())''')
s=s.replace('var amounts = TaskAmounts(row);','''var amounts = TaskAmounts(row);
                MenuTheme.Button(content, "위치 → " + TaskName(row), () => { Resume(); AntColony.Core.SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(row.transform.position); });''')
p.write_text(s,encoding='utf-8')
p=R/'UI/GatherDesignation.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('if (node == null || !node.GatheringForbidden) continue;','if (node == null || !node.GatheringForbidden && !(node.NeedsDesignation && node.WorkRequested)) continue;')
s=s.replace('if (HudResponsiveLayout.ReserveWorldLabel(label)) GUI.Box(label, "채집 금지", markStyle);','''markStyle.normal.textColor = node.GatheringForbidden ? new Color(1f,.45f,.35f) : node.WorkPaused ? Color.gray : new Color(.95f,.85f,.3f);
                if (HudResponsiveLayout.ReserveWorldLabel(label)) GUI.Box(label, node.GatheringForbidden ? "채집 금지" : node.WorkPaused ? "작업 정지" : node.WorkName + " 지정", markStyle);''')
s=s.replace('GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;','''GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
            if (work)
            {
                var area = Rect.MinMaxRect(Mathf.Min(start.x, now.x), Mathf.Min(start.y, now.y), Mathf.Max(start.x, now.x), Mathf.Max(start.y, now.y));
                var selected = ResourceNode.Available.Where(n => n != null && n.NeedsDesignation && (cancelWork || n.WorkKind == workKind))
                    .Where(n => { var p = camera.WorldToScreenPoint(n.transform.position); return p.z > 0 && area.Contains(new Vector2(p.x,p.y)); }).ToArray();
                var valid = selected.Count(n => cancelWork ? n.WorkRequested : !n.GatheringForbidden && (!n.IsDepleted || n.IsRegrowing));
                GUI.Box(new Rect(rect.x, Mathf.Max(0,rect.y-28),240,24), $"대상 {valid}개 · 제외 {selected.Length-valid}개");
            }''')
p.write_text(s,encoding='utf-8')
