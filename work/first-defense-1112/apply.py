from pathlib import Path
p=Path('Assets/_Project/Scripts/Buildings/FacilityVisual.cs');b=Path('work/first-defense-1112/before/FacilityVisual.cs');assert p.read_bytes()==b.read_bytes(),'Concurrent edit'
p.write_bytes(Path('work/first-defense-1112/draft/FacilityVisual.cs').read_bytes())
