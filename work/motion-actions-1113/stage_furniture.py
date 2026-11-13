from pathlib import Path
root=Path('F:/UnityProjects/ant-colony-rts')
s=(root/'AgentScripts/Life1112Models.cs').read_text(encoding='utf-8-sig')
s=s.replace('Life1112Models','Motion1113Models').replace('Assets/Resources/Life1112/Models','Assets/Resources/Furniture1113/Models').replace('art/life-assets-1112/models','art/meshy-1113/staged').replace('work/life-assets-1112','work/motion-actions-1113')
s=s.replace('var reports = new List<string>();','if (Application.isPlaying) throw new Exception("Edit mode required");\n        var reports = new List<string>();')
s=s.replace('importer.isReadable = false;', 'importer.isReadable = true; // Usage surfaces and tool contacts read source geometry.')
(root/'AgentScripts/Motion1113Models.cs').write_text(s,encoding='utf-8')
# Stage only confirmed distinct downloads. No Assets writes until Unity handoff.
import json, shutil, hashlib
manifest=json.loads((root/'work/motion-actions-1113/download-manifest.json').read_text(encoding='utf-8-sig'))
mapping={'woodland_single_bed':'SingleBed','woodland_table_benches':'DiningTable','woodland_workbench':'Workbench','woodland_low_bookshelf':'Bookshelf','woodland_chair':'Chair','woodland_bench':'Bench','wooden_cup':'Cup','wooden_bowl':'Bowl','open_book':'OpenBook','woodland_fence_straight':'RanchFence','wooden_ladle':'Ladle','cutting_board':'CuttingBoard','dough_lump':'Dough','medicine_bottle':'MedicineBottle','bandage_roll':'Bandage','seed_pouch':'SeedPouch','harvest_basket':'HarvestBasket','woodland_tall_spiked_palisade':'Palisade'}
reports=[]
for item in manifest:
    key=next((k for k in mapping if k in item['file']),None)
    if key is None: continue
    source=next(Path(item['extracted']).rglob('*.fbx'))
    folder=root/'art/meshy-1113/staged'/mapping[key]; folder.mkdir(parents=True,exist_ok=True)
    for src,dest in [(source,folder/(mapping[key]+'.fbx')),(source.with_suffix('.png'),folder/'BaseColor.png'),(source.with_name(source.stem+'_normal.png'),folder/'Normal.png')]:
        if not src.exists(): raise RuntimeError('Missing '+str(src))
        if not dest.exists() or hashlib.sha256(src.read_bytes()).digest()!=hashlib.sha256(dest.read_bytes()).digest(): shutil.copy2(src,dest)
    reports.append({'id':mapping[key],'source':str(source),'staged':str(folder),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
(root/'work/motion-actions-1113/staged-furniture.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('Staged',len(reports),'furniture models outside Assets; importer prepared, not executed.')
