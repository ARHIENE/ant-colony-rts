from pathlib import Path
root=Path('Assets/_Project/Scripts')
p=root/'UI/BuildScreen.cs';s=p.read_text(encoding='utf-8-sig').replace('label.text = "<b>" + entries[i].name + "</b>" +','label.text = entries[i].name +').replace('buttons[i].GetComponent<Image>().color = locked != null ? MenuTheme.Well : Color.white;','buttons[i].GetComponent<Image>().color = locked != null ? new Color(.72f, .72f, .72f, 1f) : Color.white;');p.write_text(s,encoding='utf-8')
p=root/'UI/CommanderOverhead.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('if (!HudResponsiveLayout.ReserveWorldLabel(new Rect(x, y, width, height))) continue;', '''bool reserved = false;
                float originalY = y;
                for (int lane = 0; lane < 3; lane++)
                {
                    y = originalY - lane * (height + 3 * scale);
                    if (y < 4) break;
                    if (HudResponsiveLayout.ReserveWorldLabel(new Rect(x, y, width, height))) { reserved = true; break; }
                }
                if (!reserved) continue;
                if (y != originalY)
                {
                    var tint = GUI.color; GUI.color = MenuTheme.Muted;
                    GUI.DrawTexture(new Rect(p.x, y + height, 1, originalY - y), Texture2D.whiteTexture);
                    GUI.color = tint;
                }''');p.write_text(s,encoding='utf-8')
p=root/'Map/MapGenerator.cs';s=p.read_text(encoding='utf-8-sig').replace('rng.NextDouble() > .63','rng.NextDouble() > .78').replace('distance > flatRadius * .9f','distance > flatRadius * .8f').replace('id == "Bush" ? 1.1f : .7f','id == "Bush" ? 2.2f : 1.05f');p.write_text(s,encoding='utf-8')
p=root/'Map/SeasonVisuals.cs';s=p.read_text(encoding='utf-8-sig').replace('fall.Set(25, new Color(1f, .9f, .95f, .9f), .4f','fall.Set(5, new Color(1f, .9f, .95f, .5f), .16f').replace('fall.Set(35, new Color(.95f, .5f, .15f, .95f), .3f','fall.Set(8, new Color(.95f, .5f, .15f, .65f), .18f');p.write_text(s,encoding='utf-8')
