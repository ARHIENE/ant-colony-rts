from pathlib import Path
p=Path('Assets/_Project/Scripts/Map/MapGenerator.cs');s=p.read_text(encoding='utf-8-sig');p.write_text(s.rstrip()+'\n',encoding='utf-8')
