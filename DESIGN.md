---
name: Ant Colony RTS
description: In-game HUD for a single-player ant-colony RTS; a dark chitin console that frames the nest and never competes with it.
colors:
  plate: "#1b1712"
  plate-raised: "#231e17"
  plate-hover: "#2e271e"
  well: "#120f0b"
  edge-shadow: "#080605"
  bevel-light: "rgba(255,236,200,0.09)"
  console-rim: "#5a4c3b"
  rule: "#3a3127"
  rule-strong: "#4a3f32"
  ink: "#efe7da"
  ink-soft: "#ddd2c1"
  ink-muted: "#b8ac9a"
  ink-dim: "#968976"
  accent: "#f2a93b"
  accent-hover: "#ffb84d"
  accent-rim: "#ffc56b"
  accent-ink: "#1a1206"
  troop-green: "#6cc46a"
  troop-amber: "#e0b43a"
  danger: "#e8574a"
  danger-ink: "#ffb3aa"
  crisis-surface: "#2a1512"
  crisis-rim: "#5c2620"
  crisis-control: "#3a1a16"
  warn-surface: "#3b2a12"
  done-surface: "#1d3020"
  quality-blue: "#86a9e6"
  ant-ink: "#d8ccb8"
  resource-food: "#e5bd6b"
  resource-soil: "#c49a74"
  resource-special: "#b9a2f2"
  passion-flame: "#e0913a"
  faction-hobak: "#c47b62"
  faction-cheong: "#6fb59c"
  faction-heuk: "#9a94b8"
  faction-jeok: "#b87a8e"
  crisis-icon: "#4a1d18"
  crisis-control-rim: "#6b2c25"
  crisis-title: "#ffd0ca"
  done-rim: "#3d6a3f"
  done-ink: "#a9e0a6"
  warn-ink: "#f5c77a"
  timer-warn: "#8a6a33"
  accent-hover-rim: "#ffd08a"
  table-head: "#16120e"
  row-rule: "#211b15"
  row-hover: "#1d1813"
  selected-surface: "#2a2219"
  scrim: "rgba(12,10,8,0.62)"
typography:
  hero:
    fontFamily: "Noto Sans KR, sans-serif"
    fontSize: "28px"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "-0.01em"
  display:
    fontFamily: "Noto Sans KR, sans-serif"
    fontSize: "19px"
    fontWeight: 700
    lineHeight: 1.4
    letterSpacing: "-0.01em"
  screen-title:
    fontFamily: "Noto Sans KR, sans-serif"
    fontSize: "15px"
    fontWeight: 700
    lineHeight: 1.4
    letterSpacing: "-0.01em"
  menu-item:
    fontFamily: "Noto Sans KR, sans-serif"
    fontSize: "14px"
    fontWeight: 500
    lineHeight: 1.4
  title:
    fontFamily: "Noto Sans KR, sans-serif"
    fontSize: "13px"
    fontWeight: 700
    lineHeight: 1.4
  body:
    fontFamily: "Noto Sans KR, sans-serif"
    fontSize: "13px"
    fontWeight: 400
    lineHeight: 1.4
  label:
    fontFamily: "Noto Sans KR, sans-serif"
    fontSize: "12px"
    fontWeight: 400
    lineHeight: 1.4
  caption:
    fontFamily: "Noto Sans KR, sans-serif"
    fontSize: "11px"
    fontWeight: 400
    lineHeight: 1.4
  numeral-xl:
    fontFamily: "Barlow Semi Condensed, sans-serif"
    fontSize: "30px"
    fontWeight: 700
    lineHeight: 1
    fontFeature: "tnum"
  numeral-lg:
    fontFamily: "Barlow Semi Condensed, sans-serif"
    fontSize: "20px"
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: "0.01em"
    fontFeature: "tnum"
  numeral-md:
    fontFamily: "Barlow Semi Condensed, sans-serif"
    fontSize: "17px"
    fontWeight: 600
    lineHeight: 1.4
    letterSpacing: "0.01em"
    fontFeature: "tnum"
  numeral-sm:
    fontFamily: "Barlow Semi Condensed, sans-serif"
    fontSize: "15px"
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: "0.01em"
    fontFeature: "tnum"
  numeral-cooldown:
    fontFamily: "Barlow Semi Condensed, sans-serif"
    fontSize: "22px"
    fontWeight: 700
    lineHeight: 1
    fontFeature: "tnum"
  hotkey:
    fontFamily: "Barlow Semi Condensed, sans-serif"
    fontSize: "11px"
    fontWeight: 700
    lineHeight: 1
rounded:
  pip: "1px"
  sm: "2px"
  md: "3px"
  lg: "4px"
  shoulder: "6px"
  round: "50%"
spacing:
  xxs: "2px"
  xs: "4px"
  sm: "6px"
  md: "8px"
  lg: "10px"
  xl: "12px"
  2xl: "16px"
  3xl: "20px"
components:
  button:
    backgroundColor: "{colors.plate-raised}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    height: "28px"
    padding: "0 7px"
  button-hover:
    backgroundColor: "{colors.plate-hover}"
  speed-toggle:
    textColor: "{colors.ink-muted}"
    typography: "{typography.numeral-md}"
    rounded: "{rounded.md}"
    width: "30px"
    height: "24px"
  speed-toggle-active:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-ink}"
  command-slot:
    backgroundColor: "{colors.plate-raised}"
    textColor: "{colors.ink}"
    typography: "{typography.caption}"
    rounded: "{rounded.md}"
    width: "60px"
    height: "52px"
  command-slot-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-ink}"
    rounded: "{rounded.md}"
    width: "60px"
    height: "52px"
  command-slot-primary-hover:
    backgroundColor: "{colors.accent-hover}"
  command-slot-empty:
    backgroundColor: "{colors.well}"
    rounded: "{rounded.md}"
    width: "60px"
    height: "52px"
  command-slot-cooldown:
    textColor: "{colors.ink-muted}"
    typography: "{typography.numeral-cooldown}"
  alert-row:
    backgroundColor: "{colors.plate}"
    textColor: "{colors.ink}"
    typography: "{typography.title}"
    rounded: "{rounded.md}"
    padding: "6px 4px 6px 8px"
    width: "318px"
  alert-row-crisis:
    backgroundColor: "{colors.crisis-surface}"
    textColor: "{colors.danger-ink}"
  troop-bar:
    backgroundColor: "{colors.well}"
    rounded: "{rounded.sm}"
    height: "12px"
  meter:
    backgroundColor: "{colors.well}"
    rounded: "{rounded.sm}"
    height: "3px"
  status-tag-injury:
    backgroundColor: "{colors.crisis-surface}"
    textColor: "{colors.danger-ink}"
    typography: "{typography.label}"
    rounded: "{rounded.sm}"
    padding: "1px 7px"
  minimap:
    backgroundColor: "{colors.well}"
    rounded: "{rounded.sm}"
    width: "164px"
    height: "164px"
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-ink}"
    rounded: "{rounded.md}"
    height: "38px"
    padding: "0 14px"
  button-primary-hover:
    backgroundColor: "{colors.accent-hover}"
  button-danger:
    backgroundColor: "{colors.crisis-control}"
    textColor: "{colors.danger-ink}"
    rounded: "{rounded.md}"
    height: "36px"
  button-danger-hover:
    backgroundColor: "{colors.crisis-icon}"
  menu-item:
    backgroundColor: "{colors.plate-raised}"
    textColor: "{colors.ink}"
    typography: "{typography.menu-item}"
    rounded: "{rounded.md}"
    height: "44px"
    padding: "0 10px 0 12px"
  menu-item-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-ink}"
    rounded: "{rounded.md}"
    height: "72px"
    padding: "0 12px 0 14px"
  title-tab:
    backgroundColor: "{colors.plate}"
    textColor: "{colors.ink}"
    typography: "{typography.screen-title}"
    rounded: "{rounded.shoulder}"
    height: "27px"
    padding: "0 14px"
  data-table-header:
    backgroundColor: "{colors.table-head}"
    textColor: "{colors.ink-muted}"
    typography: "{typography.label}"
    height: "30px"
    padding: "0 10px"
  data-table-row:
    textColor: "{colors.ink-soft}"
    typography: "{typography.label}"
    height: "36px"
    padding: "0 10px"
  data-table-row-hover:
    backgroundColor: "{colors.row-hover}"
  data-table-row-selected:
    backgroundColor: "{colors.selected-surface}"
  option-card:
    backgroundColor: "{colors.plate-raised}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    height: "100px"
    padding: "10px 12px"
  option-card-selected:
    backgroundColor: "{colors.selected-surface}"
  radio-indicator:
    backgroundColor: "{colors.well}"
    rounded: "{rounded.round}"
    size: "14px"
  tab-category:
    textColor: "{colors.ink-muted}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    width: "60px"
    height: "26px"
  tab-category-selected:
    backgroundColor: "{colors.plate-hover}"
    textColor: "{colors.ink}"
  tab-segment:
    textColor: "{colors.ink-muted}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    height: "28px"
  tab-segment-selected:
    backgroundColor: "{colors.plate-raised}"
    textColor: "{colors.ink}"
  range-track:
    backgroundColor: "{colors.well}"
    rounded: "{rounded.sm}"
    height: "6px"
  range-thumb:
    backgroundColor: "{colors.plate-raised}"
    rounded: "{rounded.sm}"
    width: "12px"
    height: "16px"
  build-slot:
    backgroundColor: "{colors.plate-raised}"
    textColor: "{colors.ink}"
    typography: "{typography.caption}"
    rounded: "{rounded.md}"
    width: "60px"
    height: "52px"
  faction-badge:
    backgroundColor: "{colors.faction-hobak}"
    textColor: "{colors.accent-ink}"
    typography: "{typography.title}"
    rounded: "{rounded.sm}"
    size: "30px"
  site-marker:
    backgroundColor: "{colors.faction-heuk}"
    textColor: "{colors.accent-ink}"
    typography: "{typography.caption}"
    size: "26px"
  site-marker-own:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-ink}"
    size: "26px"
  difficulty-pip:
    backgroundColor: "{colors.well}"
    rounded: "{rounded.pip}"
    width: "14px"
    height: "8px"
  difficulty-pip-on:
    backgroundColor: "{colors.ink-muted}"
  state-chip-war:
    backgroundColor: "{colors.crisis-surface}"
    textColor: "{colors.danger-ink}"
    typography: "{typography.label}"
    rounded: "{rounded.sm}"
    height: "20px"
    padding: "0 7px 0 5px"
  state-chip-peace:
    backgroundColor: "{colors.well}"
    textColor: "{colors.ink-muted}"
    rounded: "{rounded.sm}"
    height: "20px"
  placeholder-label:
    backgroundColor: "{colors.plate}"
    textColor: "{colors.ink-muted}"
    typography: "{typography.caption}"
    rounded: "{rounded.sm}"
    padding: "1px 6px"
---

# Design System: Ant Colony RTS

## Overview

**Creative North Star: "The Chitin Console"**

The HUD is a piece of equipment bolted to the bottom of the nest, not a set of windows floating over it. A single dark, warm-brown console runs the full width of the bottom edge, stepped so its centre plate stands 28px taller than its wings; a thin 40px strip caps the top edge; alerts stack down the right. Everything between is the game world. The console is built from bevelled plates and sunken wells the way a physical instrument panel is built from pressed metal, which is also exactly how it ships: every surface is a 9-slice sprite in Unity uGUI, every number is TextMeshPro, every bar and cooldown is an Image fill.

Density is high and deliberate, in the tradition of StarCraft II's command console and RimWorld's colonist readouts. Two reading speeds coexist: condensed tabular numerals and fixed state colours carry combat reads in under a second (troops, cooldown seconds, alerts), while quieter Korean sans labels and dim reason lines carry the paused, management read (why mood is 34, where the defence bonus comes from). The system rejects glass, glow and neon sci-fi chrome, and it rejects web-dashboard cards floating over the play field; depth comes only from bevels and insets.

One warm amber does all the pointing. It marks the current game speed, the single primary command, the objective flag and progress, and doubles as the warning state; everything else is brown-on-brown with bright ink.

The same hardware scales up into mode windows (commanders, pause and save, new game, diplomacy, trade) and full-screen modes (world map, main menu). A mode window is a larger plate with the console's bright top rim and a raised title tab riding on its top-left edge, like a drawer pulled out of the console; the world stays visible behind it under a dim scrim or around its edges. Inside, the density of the console carries over: 36px data rows, keyboard-shaped grids, condensed numerals, and every control labelled with its key.

**Key Characteristics:**
- Full-bleed bottom console: 180px wings, 208px centre (23% of a 900px screen), plus a 40px top strip.
- Warm near-black chitin plates with a 1px light bevel on top and a dark edge below; sunken wells for data.
- One amber accent; fixed, non-negotiable state colours for troops, crisis, warning and completion.
- Condensed tabular numerals for every changing number; Korean sans for every word.
- Every command shows its hotkey in the slot's top-left corner; the command card is shaped like the keyboard.
- 2px-stroke line icons, 1-6px radii, square slots; circles only for radio indicators and map glyphs.
- Mode windows share one header: the console-rim top edge plus a raised title tab.
- Rival factions are identified by a colour and a letter together, never colour alone.

## Colors

Warm, low-chroma browns carry every surface; bright ink carries text; a small fixed set of saturated hues carries state, and only state.

### Primary
- **Forager Amber** (accent): the single pointing colour. Active speed toggle, the one primary command slot (B, build), the one primary button of a window, the objective flag, the upkeep-billing progress bar, the selected-row outline, active-tab underline, sort indicator and text selection. On the world map it also fills the player's own lair and absorbed sites. At rest it carries a paler **Amber Rim** (accent-rim) border; on hover it lifts to **Lit Amber** (accent-hover) with the palest **Amber Hover Rim** (accent-hover-rim). Text on amber is always **Amber Ink** (accent-ink), 9.3:1, including the hotkey inside an active speed toggle.
- **Selected Surface** (selected-surface): the warm fill behind a selected table row or a checked option card, always paired with an amber 1px outline.

### Secondary (state colours)
- **Troop Green** (troop-green): healthy troop fill, positive food balance, friendly minimap blips, the completion state. The portrait frame of the selected commander uses a darkened green rim (#3d6a3f) so selection reads as "ours".
- **Troop Amber** (troop-amber): the middle band of the troop fill as troops fall; declared in the build as the green-to-red midpoint.
- **Crisis Red** (danger): enemy blips and the red end of the troop ramp. Red text is always the lighter **Crisis Ink** (danger-ink, 10.1:1 on the crisis surface), never raw red on dark.
- **Warning** reuses Forager Amber (the build aliases `--warn` to the accent): mood warnings, the warning alert icon. It is told apart from "primary" by its icon, its tinted well (warn-surface) and its word label, never by hue alone. Warning text on a warning tint uses **Warning Ink** (warn-ink, 8.7:1).

**Crisis and success tint set.** Toasts, notices, war chips, destructive buttons and placement feedback all draw from one fixed family:
- Crisis: **Crisis Surface** (crisis-surface) fill, **Crisis Rim** (crisis-rim) border, **Crisis Icon Tile** (crisis-icon) behind the icon and as the destructive button's hover, **Crisis Control** (crisis-control) fill with **Crisis Control Rim** (crisis-control-rim) for destructive buttons and jump buttons, **Crisis Title** (crisis-title, 12.5:1) for the headline, Crisis Ink for the rest.
- Success: **Completion Well** (done-surface) fill, **Completion Rim** (done-rim) border (also the selected-portrait rim and the completion timer strip), **Completion Ink** (done-ink, 9.3:1) for text on the tint.
- Timers: the warning toast's drain strip is **Warning Timer** (timer-warn); the completion toast's is Completion Rim. Warning Timer also borders the selected site label on the world map.

### Tertiary (identity colours)
- **Resource hues**: Honeydew Gold (resource-food), Tilled Clay (resource-soil), Spore Violet (resource-special) stroke only the resource icons in the top strip; numbers beside them stay ink.
- **Pale Chitin** (ant-ink): the worker-ant icon and the "common" equipment quality pip.
- **Quality Blue** (quality-blue): the "fine" equipment quality pip. (The build's variable is named `--loyal`, but its only rendered use is item quality.)
- **Passion Flame** (passion-flame): the flame marks on skills (filled = major passion, outline = minor). A second, deeper orange so passion never reads as selection.
- **Faction colours**: four rival civilisations, each a muted mid-tone that holds Amber Ink letters at 5.5:1 or better. **Pumpkin Clay** (faction-hobak, 호박, letter 호), **Leaf Jade** (faction-cheong, 청엽, 청), **Obsidian Lilac** (faction-heuk, 흑요, 흑), **Red Sand Rose** (faction-jeok, 적사, 적). Unaffiliated wild sites use Muted Ink with the letter 야; the player's own side uses Forager Amber (lair house) or Pale Chitin (trade emblem, letter 우), and on the map an absorbed site is an amber triangle lettered 나. Faction colour fills emblems, map markers and the leader-portrait halo, and nothing else.

### Neutral
- **Chitin Plate** (plate): the base of every console panel, the top strip, alert rows and the objective card.
- **Raised Plate** (plate-raised): buttons and command slots sitting on a plate; **Worn Plate** (plate-hover) on pointer hover.
- **Burrow Well** (well): sunken data areas: minimap, portrait, troop bar track, meters, speed group, empty command slots.
- **Edge Shadow** (edge-shadow): the 1px dark outer edge of every plate and well.
- **Bevel Light** (bevel-light): the 1px inset highlight on the top of every raised surface.
- **Console Rim** (console-rim): the brighter 1px top edge along the console's whole silhouette.
- **Rule** / **Strong Rule** (rule, rule-strong): dividers inside the stat row and skill row, button borders, hover borders.
- **Bone Ink** (ink, 14.5:1 on plate), **Soft Ink** (ink-soft, trait and friend names, table cells), **Muted Ink** (ink-muted, 8.0:1, labels and hotkeys), **Dim Ink** (ink-dim, 5.2:1, reasons, capacities, captions).
- **Table Head** (table-head), **Row Rule** (row-rule), **Row Hover** (row-hover): the dense data table's header band, hairline row dividers and pointer-hover fill.
- **Scrim** (scrim): the dim layer between the world and a modal mode window (commanders, pause); new game uses it at 0.5.

### Named Rules
**The One Amber Rule.** Only one command slot on the card, and one button in a window footer or action row, is ever filled amber: the primary action. Selection and progress may use amber as a line or a fill (the active speed, the pressed filter chip); no second filled amber control appears in the same group.

**The Letter-Badge Rule.** A faction colour never appears without its letter. Emblems, map markers and legend swatches all carry the faction's first syllable in bold Amber Ink; map markers add shape for site type.

**The Ink-Not-Hue Rule.** State colours tint icons, surfaces, fills and borders. Text in a state colour uses its light ink variant (Crisis Ink, Troop Green, Forager Amber at 8-9:1), and every state also carries a word or icon.

**The Fixed Ramp Rule.** Troop fill runs Troop Green, Troop Amber, Crisis Red, always in that order, always meaning troop count. Nothing else borrows the ramp.

## Typography

**Display Font:** Noto Sans KR (with sans-serif)
**Body Font:** Noto Sans KR (with sans-serif)
**Label/Mono Font:** Barlow Semi Condensed, tabular numerals (with sans-serif)

**Character:** A plain, sturdy Korean sans for every word, paired with a condensed, squared-off numeral face that packs large numbers into narrow slots and keeps columns from jittering as they tick. Words are quiet; numbers are loud.

### Hierarchy
- **Hero** (700, 28px, 1.2, -0.01em): the game title on the main menu; appears nowhere else.
- **Display** (700, 19px, -0.01em): the selected commander's or leader's name, a site name, and the title of a large mode window (commanders, new game, pause); the one large word per view.
- **Screen title** (700, 15px, -0.01em): the title tab of a full-screen mode (diplomacy, trade) and the mode name in its top strip.
- **Menu item** (500, 14px): main-menu rows and window-footer buttons.
- **Title** (700, 13px): alert titles, objective title, calendar date.
- **Body** (400, 13px, 1.4): trait tags, general text; the base size for the whole HUD.
- **Label** (400, 12px): field labels (공격, 병력, 기분), menu button text, alert descriptions, skill names; usually Muted Ink.
- **Caption** (400, 11px): reason lines under stats, command slot labels, placeholder captions; usually Dim Ink.
- **Numeral extra large** (700, 30px, 1): one headline figure per view, such as a faction's favour score.
- **Numeral large** (600, 20px, 1.1): stat values (attack, defence, mood, loyalty), difficulty, travel time, trade totals (22px).
- **Numeral medium** (600, 17-18px): resource amounts in the top strip and the troop count.
- **Numeral small** (600, 15px): skill levels.
- **Numeral cooldown** (700, 22px): seconds remaining, centred over a cooling command slot with a 1px dark text shadow.
- **Hotkey** (700, 11px, line-height 1): every key label, Muted Ink by default, Amber Ink on an amber control. Standalone key legends (main menu footer) set the key as a keycap: a 20px-tall well, 2px radius.

### Named Rules
**The Numbers Get the Condensed Face Rule.** Any value that changes during play is set in Barlow Semi Condensed with tabular figures; capacities and limits after a slash drop to 13px, weight 500, Dim Ink.

**The Hotkey Always Shows Rule.** Every control that has a key prints it, in the hotkey style, inside the control. On command and build slots it sits in the top-left corner; on menu buttons, tabs and primary buttons it trails the label; a back button shows both keys (J / Esc).

## Layout

The reference artboard is 1440x900, fixed; the shipping canvas targets 1080p to 1440p through a uGUI Canvas Scaler scaled from that reference. The HUD occupies the screen edges only.

- **Top strip:** full width, 40px, 8px side padding, three clusters spaced apart: screen menu buttons (left), calendar with upkeep countdown, food balance and speed toggles (centre), resources and worker-ant breakdown (right), a 1px Rule separator between resources and ants.
- **Objective:** 280px card, 8px from the left, 8px below the strip.
- **Alerts:** a 318px column, 8px from the right, 8px below the strip, rows 4px apart, newest-important first; crisis rows are pinned.
- **Bottom console:** a three-column grid of 236px wing, flexible centre, 372px wing, bottom-aligned. Wings are 180px tall with 8px padding; the centre plate is 208px tall, overlaps each wing by 6px, and holds portrait (132px column), commander info, stats and skills with 10-12px padding.
- **Command card:** 5 columns x 3 rows of 60x52px slots, 5px column gap, 4px row gap, centred in the right wing; rows follow the keyboard (QWERT / ASDFG / ZXCVB).
- **Minimap:** 164px square well plus a 44px column of three 48px-tall side buttons, 4px apart.

**Mode windows and full-screen modes** reuse the 40px top strip and the 8px edge inset.
- **Modal windows** sit over the scrimmed world: commanders 1344x676px (list 740px | detail), new game 1120x664px (rows 56px head / body / 64px footer, body split form | 312px summary), pause 280px menu beside a 540px save list.
- **Full-screen modes** (diplomacy, trade) fill the screen below the strip from 74px down, 8px from every edge, the title tab occupying 48-74px. Diplomacy splits 372px list | detail; trade splits partner header / two equal columns / bottom verdict bar. Their top strip switches to a three-column grid: back button with keys and mode title left, calendar and speed centre, resources right.
- **World map** keeps the strip and alert column, adds a 340px expeditions column on the left, a legend under the alerts, and a 128px site-info shelf along the bottom from x=356px with the console's 6px shoulder on its top-left.
- **Main menu** is a 440px left plate, full height, padded 88px top and 48px sides, with the world visible to its right.
- **Build mode** keeps the HUD console: the centre plate becomes a commander picker, the right wing becomes the build card, and a 34px instruction bar sits top centre under the strip.

Spacing is a tight 2/4/6/8/10/12px rhythm; 4px separates siblings in a group, 8px separates groups and insets from screen edges, 10-12px pads the centre plate and window panels. 16px and 20px appear only as the inner padding of large mode windows (new game form and summary), and 28-32px only as the main menu's section breaks. Inside the HUD, spacing never exceeds 12px.

### Named Rules
**The World Is the Screen Rule.** HUD chrome stays within the 40px top strip, the edge columns and a bottom console no taller than 208px (about 23% of screen height). Panels never float in the middle of the play field.

**The Keyboard-Shaped Card Rule.** Command and build slots map to their physical key rows. A command without a key does not go on the card; an unused key keeps an empty well so the grid never reflows.

## Elevation & Depth

Depth is entirely bevel and inset, never cast shadow. Raised surfaces (plates, buttons, slots) carry a 1px light inset line on top and a darker line underneath, framed by a 1px Edge Shadow border; sunken surfaces (wells) carry an inner top shadow. This is a pressed-metal instrument panel, and it maps one-to-one onto 9-slice sprites in uGUI.

### Shadow Vocabulary
- **Plate bevel** (`box-shadow: inset 0 1px 0 rgba(255,236,200,0.09), inset 0 -1px 0 rgba(0,0,0,0.35)`): every plate. Bake into the plate 9-slice sprite.
- **Control bevel** (`box-shadow: inset 0 1px 0 rgba(255,236,200,0.09)`): buttons and command slots. Bake into the button sprite.
- **Well inset** (`box-shadow: inset 0 1px 2px rgba(0,0,0,0.5)`): minimap, portrait, bars, meters, speed group. Bake into the well sprite.
- **Cooldown shade** (`conic-gradient(rgba(8,6,5,0.72) 0 N%, transparent N% 100%)`): the cooldown sweep over a slot. In uGUI: a dark Image, Fill Method Radial 360, fill amount = remaining fraction.
- **Seconds legibility** (`text-shadow: 0 1px 2px rgba(8,6,5,0.9)`): cooldown seconds only. In TextMeshPro: an underlay.
- **Selected ring** (`box-shadow: inset 0 1px 0 <bevel>, inset 0 0 0 1px rgba(242,169,59,0.25)` with an amber 1px border): a selected list row or save slot on Worn Plate. Option cards use a full-strength inset amber ring.
- **Emblem bevel** (`box-shadow: inset 0 1px 0 rgba(255,236,200,0.22), inset 0 -1px 0 rgba(0,0,0,0.3)`): faction letter badges; a stronger highlight because the fill is mid-tone. Bake into the badge sprite and tint with the faction colour.
- **Scrim**: a flat Scrim-coloured full-screen Image under modal windows; no blur.

The **mode-window header** is built from depth, not colour: the window plate carries the Console Rim along its top edge, and a raised title tab (6px top corners, no bottom border, a 2px plate-coloured strip covering the seam) sits on that edge at the left, so tab and window read as one pressed piece. In uGUI: a tab 9-slice with its rim baked, overlapping the window plate by 1px.

### Named Rules
**The No-Float Rule.** No outer drop shadows, blurs, glows or translucent glass. A surface is either raised by a bevel or sunk into a well. The single ring in the system is the 3px Crisis Red halo around an enemy blip on the minimap.

## Shapes

Hard-edged and machined. Radii stay small: 1px for pips and minimap blips, 2px for wells, bars, meters, icon tiles and tags, 3px for buttons, slots, alert rows and the objective card, 4px for the speed group. The one larger radius is the 6px shoulder where the console steps up: the wings round only their inner top corner and the centre plate rounds both top corners, so the silhouette reads as one stepped piece of hardware sitting on the bottom edge. The console has no bottom or outer side borders; it runs off the screen.

Mode-window title tabs reuse the 6px shoulder on their top corners; a commanders window attached to its tab drops its own top-left radius to 0. Circles appear only where a circle is the meaning: the 14px radio indicator in option cards (a 6px amber dot when checked), settlement markers and the selection ring on the world map, and the planet itself.

World-map site markers are 26px glyphs with a 2px Edge Shadow outline: shape encodes site type (circle = settlement, diamond = boss nest, triangle = resource site, square = trading post, house = own lair), fill and letter encode owner. Faction emblems are squares with a 2px radius at 24, 30, 36 or 40px.

Dashed 1px borders mean "absent or not yet available": an empty equipment slot, a locked skill or building, a disabled action, an empty save slot, an unmet faction, a hidden agenda, placeholder art. Icons are 24-unit line glyphs at a 2-2.4px stroke with round caps, drawn at 14-20px.

## Components

### Buttons
Compact, tactile controls pressed into the plate.
- **Shape:** gently squared (3px).
- **Default:** Raised Plate fill, 1px Rule border, control bevel, Bone Ink text; 28px tall in the top strip with 7px side padding, 6px gap to the trailing hotkey.
- **Hover / Focus:** hover shifts to Worn Plate with a Strong Rule border (150ms); press scales to 0.97 (120ms, strong ease-out); keyboard focus draws a 2px Forager Amber outline 1px outside.
- **Disabled:** Dim Ink text, no press scale.
- **Ghost:** used for toast close buttons and inactive speed toggles; transparent fill and border, no bevel.
- **Primary:** Forager Amber with Amber Rim, Amber Ink at weight 700, hotkey at 70% Amber Ink; 38-44px tall in window footers and the world-map send button. Hover to Lit Amber with Amber Hover Rim. A disabled primary drops back to Raised Plate with Dim Ink.
- **Danger:** Crisis Control fill, Crisis Control Rim border, Crisis Ink text (declare war); hover to Crisis Icon Tile.
- **Disabled action:** transparent with a dashed border and a one-line reason in Dim Ink caption underneath.
- **Main-menu rows:** 44px, icon / label / chevron grid, Menu item type; the continue row is a 72px amber primary with a second line of save metadata.

### Speed Toggles
A segmented group inside a 4px-radius well with 2px padding: pause (with its P key) and 1x/2x/3x in the numeral face. The active speed fills Forager Amber with Amber Ink; the others are ghost buttons in Muted Ink.

### Command Card (signature)
The StarCraft II command card, played straight.
- **Slot:** 60x52px, 3px radius, Raised Plate, icon (18-20px) over an 11px caption, hotkey in the top-left corner (3px/4px inset).
- **Primary:** exactly one slot, filled Forager Amber with an Amber Rim border and Amber Ink content; hover to Lit Amber.
- **Cooldown:** a dark radial sweep covers the elapsed-remaining fraction, the icon and caption drop to 30% opacity, and the remaining seconds sit centred in the 22px cooldown numeral. uGUI: Image Filled Radial 360 plus a TMP label.
- **Locked:** dashed border, transparent, a lock icon and the skill name in Dim Ink; the reason lives in the tooltip.
- **Empty:** a flat Burrow Well with an Edge Shadow border, no bevel, not interactive.

### Build Card (signature)
The command card's second mode, in the same right wing.
- **Category tabs:** five 60x26px raised tabs across the top, keys 1-5 (생산, 자원, 연구, 방어, 특수). The selected tab turns Worn Plate with a Strong Rule border and a 2px amber underline inset 6px from each side.
- **Grid:** 5x2 of 60x52px slots on the QWERT / ASDFG rows. Each slot shows its key top-left, the building name, and its cost as resource icons with 12px numerals. The building currently being placed is the one amber slot; locked slots are dashed with a one-line reason; unused keys stay empty wells.
- **Unlock popover:** hovering a locked slot opens a 276px plate above it with a 10px pointer, a condition list (met in Troop Green, unmet in Crisis Ink) and the cost line.
- **Placement:** a 34px instruction bar top centre names the building, cost and assigned commander with Esc/R keys; in the world, valid spots get a Completion-tinted label and blocked spots a Crisis-tinted label with the reason.

### Mode-Window Header
- **Frame:** window plate with a 1px Console Rim top edge and a 3-4px radius.
- **Title tab:** a raised plate on the top-left edge, 6px top corners, 27px tall with a 15px Screen title (full-screen modes) or 34px with a 19px Display title (commanders); an optional Dim Ink crumb or count follows the title.
- **Under the tab:** a 52-56px header row holds count, search (a 30px well that gains an amber border on focus) and the close button with its keys.

### Data Table
Dense, sortable, RimWorld-grade.
- **Header:** 30px Table Head band, Label type in Muted Ink, numeric columns right-aligned. The sorted column's label turns Bone Ink with a 10px amber chevron (3px stroke) showing direction.
- **Rows:** 36px, Row Rule hairlines, Soft Ink cells, names in 13px bold Bone Ink, numbers in the condensed face with inline 5px mini bars. Hover fills Row Hover; the selected row fills Selected Surface with a 1px amber outline.
- **Filter chips above:** 28px raised chips with counts; the pressed chip goes amber.
- The build-mode commander picker uses the same 36px row as a button list, with an amber outline and "선택됨" label for the chosen row.

### Option Cards (radio)
Round-radio choice cards for setup screens.
- **Card:** 100px tall, 3px radius, Raised Plate with the control bevel, in a three-column grid with 8px gaps. Top line: a 15px bold name with a 20px numeral and unit; then a Muted Ink description; a footer line in Dim Ink.
- **Indicator:** a 14px round well in the top-right corner.
- **Checked:** amber border plus a 1px inset amber ring, Selected Surface fill, amber dot in the indicator. Hover Worn Plate; press scales 0.97; keyboard focus draws the amber outline.
- **Recommended:** a small 11px outlined tag beside the name.

### Tabs
- **Category tabs** (build card): separate raised buttons, selected one gets Worn Plate and a 2px amber underline; keys 1-5.
- **Segmented tabs** (trade inventory): ghost tabs inside a 4px-radius well with 2px padding; the selected tab becomes Raised Plate with the bevel and an inset 2px amber underline (`inset 0 -2px 0`); counts in Dim Ink numerals.

### Range Slider Rows
- **Row:** label | slider | value, the value as a 15px condensed numeral right-aligned in a 64px column with a Dim unit.
- **Track:** 6px well, 2px radius, inset shadow; the chosen part fills Muted Ink (neutral quantity, not a state).
- **Thumb:** 12x16px raised plate, 2px radius, Strong Rule border. uGUI: Slider with a well background, a filled Image and a 9-slice handle.

### Faction Emblem
- A square badge (2px radius, Edge Shadow border, emblem bevel) filled with the faction colour and lettered in bold Amber Ink: 24px in lists, 30px in the diplomacy list, 36-40px in headers.
- Leader portraits carry the faction colour only as a thin inner halo (50% opacity) inside the well.

### State Chips
20px, 2px radius, 12px/500 text with a leading icon. Peace: well fill, Rule border, Muted Ink. War or fighting: Crisis tint. Returning or complete: Completion tint. Warning: warn-surface with Warning Ink.

### Favour Bar
A centred-zero bar for -100 to +100: 6-8px well, a 1px Strong Rule tick at 0, Troop Green grows right, Crisis Red grows left; the value beside it in the condensed face, green, Crisis Ink or Muted Ink for zero. A reasons list below itemises each contribution with signed numerals.

### World-Map Site Markers
- **Marker:** 26px glyph, 2px Edge Shadow outline, press scale 0.97, amber focus outline. Shape = site type (circle settlement, diamond boss nest, triangle resource, square trading post). Fill + bold 11px letter = owner (faction colour, Muted Ink 야 for wild, amber house or amber lettered triangle for the player's own).
- **Selected:** a 40px amber ring (2px, with a 45% inner ring) around the marker, and its label bordered in Warning Timer.
- **Labels:** 12px Soft Ink on a Chitin Plate tag with an Edge Shadow border, 2px radius.
- **Routes:** dashed Soft Ink lines for expeditions, dashed Crisis Red arrows for enemy movement; a legend plate explains shape, owner and route groups.

### Difficulty Pips
Segmented rectangles with a 1px radius: 14x8px, five in a row, 3px apart; lit pips fill Muted Ink with a Strong Rule border, unlit pips are wells with a Rule border. Always followed by the numeral "3 / 5" so the value never rests on the pips alone. Inline frequency pips on setup cards use a smaller 10x6px step.

### Placeholder Labels
Every stand-in (portrait, logo, planet render, leader name) is labelled in words: 11px Muted Ink on a Chitin Plate tag with a Strong Rule border, 2px radius, padding 1px 6px, reading "(임시)" or "(자리 표시)". Large placeholder areas use a dashed Strong Rule border.

### Alert Rows
RimWorld's alert column rendered as plate rows.
- **Shape:** 318px wide, 3px radius, plate bevel; 22px icon tile (2px radius), title, one-line ellipsised description, and a trailing action or close button.
- **Crisis:** Crisis Surface fill with Crisis Rim border, a Crisis Icon Tile behind a Crisis Ink icon, a Crisis Title headline, a "고정" pin label, an optional countdown in 15px Crisis Ink numerals, and a Crisis Control jump-to-location button with its Space key. Crisis rows do not time out. The same tint set builds inline blocking notices (e.g. "cannot save during combat") inside windows.
- **Warning / Completion:** plain plate rows with a tinted icon tile (warn-surface with amber, done-surface with green), a count badge for merged alerts, and a 2px timer strip along the bottom edge that drains as the row expires.
- **Motion:** enters from 24px right with opacity 0 over 250ms on the strong ease-out; under reduced motion, opacity only over 200ms.

### Commander Readout
- **Identity row:** Display name, role in Muted Ink, traits in Soft Ink separated by dim dots, friend and rival (rival in Crisis Ink), and injury as a Crisis-tinted tag with an icon.
- **Troop bar:** a 12px well with a Troop Green fill and ten 1px dark ticks at 10% intervals; count and command limit sit above it right-aligned. uGUI: Image Filled Horizontal plus a tick overlay sprite.
- **Stat row:** four equal cells split by Rule lines, each a 12px label, a 20px numeral and an 11px Dim reason line. A cell in a warning state colours its numeral and adds a word label beside the field name.
- **Skill row:** nine equal cells, name left and level right, passion flame after the name; major skills bold.
- **Portrait and equipment:** a 132x128px well with a green selection rim (placeholder art is labelled as such), three 40px equipment slots below, each with a 6px quality pip in the bottom-right.

### Meters
3px-tall well tracks with a 2px radius and a solid fill; used for the upkeep countdown and small gauges. uGUI: Image Filled Horizontal.

### Minimap
A 164px square well, 2px radius, terrain as soft blobs, friendly units as square Troop Green blips, enemies as Crisis Red blips with a halo, and the camera view as a 1px Bone Ink frame at 85% opacity. Three side buttons (Space, Tab, Z/C) show their key above the icon.

## Do's and Don'ts

### Do:
- **Do** build every surface from three sprites: raised plate, raised control, sunken well, each a 9-slice with its bevel baked in.
- **Do** set every changing number in Barlow Semi Condensed with tabular figures, and every word in Noto Sans KR.
- **Do** print the hotkey inside every control that has one; on the command card, in the top-left corner.
- **Do** keep the command card a 5x3 keyboard-shaped grid of 60x52px slots, with empty wells for unused keys.
- **Do** show cooldowns as a dark radial fill with the remaining seconds centred at 22px, the icon dimmed to 30%.
- **Do** pair every state colour with an icon, word or shape, and use the light ink variants for coloured text.
- **Do** keep all motion under 250ms on `cubic-bezier(0.23,1,0.32,1)`, and fall back to opacity-only when reduced motion is requested. Windows rise 8px as they fade in (200-220ms), side panels slide 16-24px, popovers 4px.
- **Do** open every mode window with the Console Rim top edge and a raised title tab.
- **Do** print a faction's letter on every faction-coloured badge or marker, and encode site type by marker shape.
- **Do** label every placeholder in words ("(임시)", "(자리 표시)").
- **Do** set the hotkey inside an active amber control in Amber Ink.

### Don't:
- **Don't** cast outer drop shadows, glows, blurs or translucent glass panels; depth is bevel and inset only.
- **Don't** fill more than one command slot with amber, or give amber fill to any control other than the active selection or the primary action.
- **Don't** float cards or windows over the play field; HUD stays on the top strip, the edge columns and the bottom console.
- **Don't** raise the bottom console above 208px or let the wings exceed 180px.
- **Don't** use radii above 4px except the 6px console shoulder and title tabs, and circles only where the circle carries meaning (radio indicator, settlement marker, map selection ring).
- **Don't** use a faction colour for anything but that faction's badge, marker or portrait halo.
- **Don't** set a key label below 11px.
- **Don't** set coloured text in raw Crisis Red; use Crisis Ink.
