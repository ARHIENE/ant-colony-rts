#!/bin/bash
# 사용: run_checks.sh Check1 Check2 ... (AgentScripts/<이름>.cs, 각각 Play 재시작 후 순차 실행)
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
  unity cmd eval_file --file AgentScripts/RunChecks.cs --timeout 30 >/dev/null 2>&1
  res="TIMEOUT"
  for i in $(seq 1 60); do
    if [ -f Temp/check-result.txt ] && [ $(stat -c %Y Temp/check-result.txt) -gt $start ]; then res=$(head -c 600 Temp/check-result.txt); break; fi
    sleep 10
  done
  echo "$name: $res"
done
unity cmd editor_stop >/dev/null 2>&1
