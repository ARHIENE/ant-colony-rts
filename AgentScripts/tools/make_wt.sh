#!/bin/bash
# 사용: make_wt.sh NAME → E:/Git/ant-wt/NAME 에 현재 작업 트리 상태(미커밋 포함)를 복제하고 index에 올려 둔다.
# 나중에 Codex 변경분만: cd E:/Git/ant-wt/NAME && git add -A -N && git diff
set -e
cd /e/Git/ant
wt=/e/Git/ant-wt/$1
git worktree add --detach "$wt" HEAD >/dev/null
git ls-files -m -o --exclude-standard -- Assets/_Project/Scripts AgentScripts docs Assets/_Project/Data | while read -r f; do
  [ -f "$f" ] || continue; mkdir -p "$wt/$(dirname "$f")"; cp "$f" "$wt/$f"
done
git ls-files -d -- Assets/_Project/Scripts AgentScripts docs | while read -r f; do rm -f "$wt/$f"; done
cd "$wt" && git add -A >/dev/null
echo "$wt ready"
