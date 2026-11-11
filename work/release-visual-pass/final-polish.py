from pathlib import Path
p=Path('Assets/_Project/Scripts/UI/GameMenuController.cs');s=p.read_text(encoding='utf-8-sig').replace('Mathf.CeilToInt(10 / scale)','Mathf.CeilToInt(12 / scale)');p.write_text(s,encoding='utf-8')
p=Path('Assets/_Project/Scripts/UI/BuildScreen.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('if (open) placement?.CancelPlacement();','''if (open)
            {
                placement?.CancelPlacement();
                foreach (var page in tabPages) page.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
            }''');s=s.replace('// 줄이 많으면 칸 높이를 줄여 콘솔 안에 맞춘다(최대 52).\n                    // 12칸을 넘으면 4열(가구 5차로 휴게 13칸, 2026-10-06) — 5줄이면 이름과 상태 글자가 겹친다.','// Preserve readable cards; additional rows scroll instead of shrinking.');p.write_text(s,encoding='utf-8')
