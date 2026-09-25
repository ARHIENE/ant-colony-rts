# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

(HTML mockups only. The shipping target is a Unity desktop game; see Capabilities and Constraints.)

## Stack

Mockups: static HTML/CSS artboards on the Claude Design canvas. Shipping implementation: Unity uGUI (Canvas), so every mockup element must be buildable with uGUI images, TextMeshPro text, masks, fills and simple tweens.

## Users

PC players of a single-player ant-colony strategy game, at a desk in front of a 1080p–1440p monitor, mouse and keyboard. Sessions are long (one campaign is 10+ hours). Play splits roughly half and half: calm management with frequent pausing (RimWorld-style: checking commanders, mood, loyalty, research, economy) and fast tactical control (StarCraft-style: selecting commanders, hotkeys, attack-move, dodging boss telegraphs, firing skills on cooldown).

## Product Purpose

Ant Colony RTS (working title). The player runs an ant nest: produces worker ants as a quantity resource, assigns them to named commander ants who are the only directly controlled units, builds, researches, expands through a world map (expeditions, diplomacy, trade), and wins by launching a Great Migration airship. The in-game HUD must let the player read colony state and control commanders without leaving the game world.

## Positioning

Commanders are individual characters with RimWorld-depth inner lives (traits, passions, mood, loyalty, injuries, relationships) who also lead troops in real-time battles. Troop count is the commander's HP. That fusion of character simulation and RTS control is what a neighbouring RTS or colony sim cannot claim.

## Operating Context

- Local map is an isometric ant nest; the HUD overlays it.
- Game calendar: 1 month = 5 real minutes; game speed pause/1×/2×/3×.
- Non-blocking toasts only (crisis pinned until resolved, warning 10s, completion 6s, max 5).
- Hotkeys: A attack-move, S stop, Q weapon skill, W wing skill, E +1 troop, D −1 troop, R swap weapon, G commanders, K science, J diplomacy, L event log, M world map, P pause, B build, Z/C camera rotate, Esc menu.

## Capabilities and Constraints

- Resources: Food, Soil, Special (current / capacity). Worker ants: idle / assigned / reserved / total. Monthly food balance.
- Commander data: name, 1–3 traits, weapon (defines role: mandible/acid sprayer/shield/pheromone; wings = flight), troops vs command limit, attack/defense, 9 skills 0–20 with passion flames, mood 0–100 with reasons, loyalty 0–100 with reasons, injuries by body part, friends/rivals, 3 equipment slots.
- Skills: weapon skill (Q) and wing skill (W) with locked/ready/active/cooldown states.
- HUD must not cover much of the game world; information must be readable at a glance during combat.
- Must translate to Unity uGUI (no effects that need web-only rendering).
- Spec facts from Notion (confirmed by the user, provisional numbers): saving is refused while commanders move, work, carry, build or fight (the build shows the reason); new-game invasion difficulty 온화/보통/가혹 = crisis:luck 40:60 / 60:40 / 75:25; commander death modes 관대 (none) / 보통 (25% when downed again while seriously injured) / 가혹 (30% each time downed); transport slots 차량 commanders 4 + ants 40, 비행기 commanders 8 + ants 100; trade AI accepts when received value >= given value x (1.2 - favour/250); trade values Food/Soil 1, Special 10, equipment 40/80/160/320 by quality, captive commander = skill total x 5, site = difficulty x 300, treaties 100/150/300.

## Brand Commitments

- The previous dark + mint look and layout may be fully replaced; only the displayed information (above) must be kept.
- Standing preference (2026-09-25): the in-game HUD follows the category-standard RTS HUD, played straight. Craft bar: StarCraft II (bottom console: minimap, unit info, command card grid) and RimWorld (alert list, dense readable colonist info).
- Failure conditions named by the user: looks like a web/SaaS dashboard; covers too much of the game world; information not readable at a glance.

## Evidence on Hand

Spec in Notion (기획(스펙 문서)). No final art, logo, portraits or icon set exist yet; portraits and art are placeholders and must be labelled as such. Names used in mockups (관우, 장비, 여포) are placeholder commander names.

## Product Principles

1. The world is the screen; the HUD frames it and never competes with it.
2. Glanceable in combat, deep on pause: troop HP, cooldowns and alerts read in under a second; mood, loyalty and reasons are there when the player stops to look.
3. Commanders are characters, not units: their personal state is always one glance away.
4. Hotkey first: every command shows its key; nothing important is mouse-only.

## Accessibility & Inclusion

Text contrast at least WCAG AA; states distinguished by more than hue (icon, shape or label as well); support reduced motion.
