#!/bin/bash
# 사용: run_checks.sh Check1 Check2 ... (AgentScripts/<이름>.cs, 각각 Play 재시작 후 순차 실행) 긴 검사는 CHECK_POLLS=180(10초 단위).
cd /e/Git/ant
SP=$(mktemp -d)  # 임시 setcheck.cs 위치
unity cmd editor_focus >/dev/null 2>&1
for name in "$@"; do
  unity cmd editor_stop >/dev/null 2>&1
  sleep 3
  unity cmd editor_play >/dev/null 2>&1
  printf 'UnityEditor.SessionState.SetString("AntColony.CheckFile", "AgentScripts/%s.cs"); UnityEditor.SessionState.SetString("AntColony.CheckResult", ""); return Application.isPlaying ? "ok" : "notplaying";\n' "$name" > "$SP/setcheck.cs"
  ok=""
  for i in $(seq 1 30); do
    out=$(unity cmd eval_file --file "$SP/setcheck.cs" --timeout 20 --result-only 2>&1)
    if echo "$out" | grep -q '"ok"'; then ok=1; break; fi
    sleep 5
  done
  if [ -z "$ok" ]; then echo "$name: SETUP FAIL $out"; continue; fi
  start=$(date +%s)
  run=$(unity cmd eval_file --file AgentScripts/RunChecks.cs --timeout 30 --result-only 2>&1)
  # 검사 스크립트 컴파일 실패는 결과 파일을 남기지 않으므로 TIMEOUT으로 보이지 않게 바로 출력한다.
  if echo "$run" | grep -q '"success": false\|Severity\|Exception'; then echo "$name: EVAL FAIL $(echo "$run" | head -c 600)"; continue; fi
  res="TIMEOUT"
  for i in $(seq 1 ${CHECK_POLLS:-60}); do
    if [ -f Temp/check-result.txt ] && [ $(stat -c %Y Temp/check-result.txt) -gt $start ]; then res=$(head -c 600 Temp/check-result.txt); break; fi
    sleep 10
  done
  echo "$name: $res"
done
unity cmd editor_stop >/dev/null 2>&1
