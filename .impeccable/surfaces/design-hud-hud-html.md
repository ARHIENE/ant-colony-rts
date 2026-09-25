---
version: 1
slug: "design-hud-hud-html"
primary_target: "design/hud/hud.html"
related_targets: []
---

Scope: in-game HUD of the local nest map, 1440x900 desktop mockup. Visitor mode: Operate.
Task: read colony and selected-commander state at a glance, issue commands by hotkey; long paused management and fast combat both happen here.
Constraints: Unity uGUI buildable; covers at most ~23% of the screen height; readable in combat.

## Direction contract

THESIS: One continuous StarCraft II style console along the bottom edge plus a thin top strip and a RimWorld style alert column; it refuses both neon glass sci-fi chrome and web dashboard cards floating over the world.
OWN-WORLD: Dark chitin console (warm near-black browns) built from 9-slice bevelled plates with a stepped silhouette, the centre plate taller than the wings. One amber accent for selection and primary action; fixed state colours: troop HP green to amber to red, crisis red, warning amber, cooldown dark sweep with white seconds. Condensed numerals, Korean sans labels, 2px stroke icons, 2-4px radii, square command slots with corner hotkeys.
STORY: The player sees food, soil, special and ants top right, time and speed top centre, alerts on the right edge, and the selected commander's troops, mood, loyalty, traits and skills in the console; then presses A, Q, E and so on from the command card.
FIRST VIEWPORT: Top strip 40px: menu buttons left, calendar and speed centre, resources right. Alerts: right edge rows below the strip. Objective: top left under the strip. Bottom console 180px wings / 208px centre (20-23% of screen height): minimap left 164px square with three side buttons, commander info centre (portrait, troop bar with 10% ticks, stat row, traits, skills), command card right as a 5x3 keyboard-shaped grid (QWERT / ASDFG / ZXCVB) of 60x52 slots, B build as the one amber primary.
FORM: Canon (standing exit, category standard RTS HUD), user-chosen; not on the ordered list; seed key 5afb167b. Craft bar: StarCraft II, RimWorld.
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
