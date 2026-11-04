# ai-skills

PC 포맷 전(2026-11-04) Claude Code · Codex에서 쓰던 스킬·설정 백업.

## 구성
| 폴더 | 원래 위치 |
|---|---|
| `claude/skills/` | `~/.claude/skills/` |
| `claude/CLAUDE.md` | `~/.claude/CLAUDE.md` |
| `claude/settings.json` | `~/.claude/settings.json` |
| `claude/memory/` | `~/.claude/projects/E--Git/memory/` |
| `claude/plugins/*.json` | `~/.claude/plugins/` (설치 플러그인·마켓플레이스 목록) |
| `codex/skills/` | `~/.codex/skills/` (`~/.codex2/skills`는 이 폴더로의 symlink였음) |
| `codex/AGENTS.md` | `~/.codex/AGENTS.md` |
| `agents/skills/` | `~/.agents/skills/` |

## 복원 (Git Bash)
```sh
git clone https://github.com/ARHIENE/ai-skills.git && cd ai-skills
mkdir -p ~/.claude/projects/E--Git ~/.codex ~/.agents
cp -r claude/skills claude/CLAUDE.md claude/settings.json ~/.claude/
cp -r claude/memory ~/.claude/projects/E--Git/
cp -r codex/skills codex/AGENTS.md ~/.codex/
cp -r agents/skills ~/.agents/
```
플러그인은 `claude/plugins/installed_plugins.json`의 목록대로 Claude Code에서 `/plugin`으로 재설치
(claude-mem@thedotmack, codex@openai-codex, ponytail@ponytail, unity@claude-plugins-official).

## 제외 (보안)
`auth.json`, `config.toml`(MCP 토큰 포함 가능), 세션 기록·sqlite DB → 재설치 후 다시 로그인/설정.
