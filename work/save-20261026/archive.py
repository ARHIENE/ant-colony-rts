import pathlib,subprocess,difflib,json,shutil
root=pathlib.Path('E:/Git/ant');out=root/'docs/patches/unfinished-20261026';out.mkdir(parents=True,exist_ok=True)
def git(w,*a): return subprocess.check_output(['git','-c','core.quotepath=false',*a],cwd=w,stderr=subprocess.DEVNULL)
for name in ['F1','F2']:
 w=pathlib.Path('E:/Git/ant-wt')/name
 parts=[git(w,'diff','--binary','HEAD','--','Assets/_Project/Scripts','AgentScripts','docs').decode('utf-8')]
 for p in git(w,'ls-files','--others','--exclude-standard','--','Assets/_Project/Scripts','AgentScripts','docs').decode('utf-8').splitlines():
  if not p.endswith(('.cs','.meta','.md')):continue
  text=(w/p).read_text(encoding='utf-8-sig');lines=text.splitlines(keepends=True)
  if text and not text.endswith('\n'):lines[-1]+='\n'
  parts.append('diff --git a/'+p+' b/'+p+'\nnew file mode 100644\n'+''.join(difflib.unified_diff([],lines,fromfile='/dev/null',tofile='b/'+p)))
 (out/(name+'-source-snapshot.patch')).write_text(''.join(parts),encoding='utf-8')
 hand=w/'docs'/('IMPL-'+name+'-HANDOFF.md')
 if hand.exists():shutil.copyfile(hand,out/hand.name)
 print(name,'base',git(w,'rev-parse','HEAD').decode().strip(),'patch bytes',(out/(name+'-source-snapshot.patch')).stat().st_size)
(out/'README.md').write_text('# 미통합 작업 보존 — 2026-10-26\n\nF1(D02/D03 평가·외부 조사)과 F2(D08 탐사)는 사본에서 코드/오프라인 컴파일을 마쳤으나 본 프로젝트 병합·UI·Unity 검증을 하지 않았다. 사용자 요청으로 전체 작업을 확인한 SAVE에서 소스 스냅샷을 보존한다.\n\n패치는 각 worktree HEAD 대비 전체 코드 차이와 신규 코드·문서를 담으므로 이미 본 프로젝트에 병합된 E1/E2 등의 기준 변경도 포함할 수 있다. **본 프로젝트에 바로 git apply하지 않는다.** 원본 worktree에서 인계 문서의 담당 파일을 기준으로 최신 develop과 대조·이식한다. 원본은 E:/Git/ant-wt/F1, F2에 그대로 있다. raw art·prefab·빌드·캐시·비밀 정보는 포함하지 않았다.\n',encoding='utf-8')
shutil.copyfile(pathlib.Path('E:/Git/ant-wt/F3/docs/MODEL-AUDIT-1025.md'),root/'docs/MODEL-AUDIT-1025.md')
