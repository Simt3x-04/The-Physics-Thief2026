# The Physics Thief — Game Design Document

**First-person 3D physics action-puzzle adventure · Videogame Design and Programming**
**Version 2.0 · Draft for the 02/11/2026 delivery**

---

## Contents

1. [Overview and Vision](#1-overview-and-vision)
2. [Gameplay and Core Loop](#2-gameplay-and-core-loop)
3. [Mechanics and Systems](#3-mechanics-and-systems)
4. [Enemies and AI](#4-enemies-and-ai)
5. [Story and Narrative](#5-story-and-narrative)
6. [World and Level Design](#6-world-and-level-design)
7. [Objects Catalogue](#7-objects-catalogue)
8. [Interface (UI/UX)](#8-interface-uiux)
9. [Art and Audio](#9-art-and-audio)
10. [Production](#10-production)

---

## 1. Overview and Vision

**Core pitch.** *"What if you could steal the laws of physics?"* Players control Gino, an eccentric middle-school student who dreams of becoming a super-scientist. With a stolen holographic briefcase scanner, Gino scans an object, copies one of its physical attributes (mass, velocity, temperature, magnetism or hardness) and pastes it onto another object to cross hazards, solve puzzles and defeat enemies.

| Element | Specification |
|---|---|
| Genre | First-person 3D action-puzzle adventure |
| Target runtime | ~20 minutes (single demo) |
| Engine and physics | Unity 3D with NVIDIA PhysX |
| Platform and input | PC, keyboard and mouse |
| Visual style | Low-poly 3D assets (Synty/Kenney) with MS-Paint meme textures |
| Target audience | Casual-to-core PC players aged 12+ who enjoy physics and absurd humor; no prior experience needed |
| Reference games | Portal (first-person puzzle pacing), Scribblenauts (properties and creativity), Half-Life 2 gravity gun (physics as a tool), Zelda: Tears of the Kingdom (emergent solutions) |

**Design pillars.**

1. **Creative physics:** the player's own ideas are the main source of fun.
2. **Readable rules:** every value, threshold and timer is shown on screen.
3. **Absurd humor:** a realistic physics simulation wearing crude, funny visuals.
4. **Short and polished:** a tight 20-minute path with no filler.

**Non-goals.** No multiplayer; no crafting; no procedural levels; no open-world exploration (the path is strictly linear); no pasting properties on Gino himself; no save system beyond checkpoints (Start always begins at the tutorial).

---

## 2. Gameplay and Core Loop

### 2.1 Player goal

Cross the daydream from the tutorial to the final arena and defeat the boss, the Crazy Machine. Along the way the player unlocks attributes and briefcase slots, solves physics puzzles and defeats enemies.

### 2.2 Win and loss conditions

- **Win:** defeating the Crazy Machine ends the demo. The world crumbles, an alarm rings and Gino wakes up in his bedroom (simple end screen).
- **Loss:** Gino's HP reaches 0. He respawns with full HP at the checkpoint at the end of the previous area (the transition corridor). The current area is reset (enemies and puzzles); unlocked attributes, slots and collected modules are kept.
- There is no timer and no failure state other than death; stuck puzzles are solved with the reset control (see [3.8](#38-object-rules)).

### 2.3 Core loop

**Moment to moment:** Observe → Scan → Copy → Paste → Watch the physics react → Adapt. In a fight the loop is the same, under pressure: dodge → scan a weapon or the enemy → copy → paste → hit.

**Per area:** enter → learn or unlock a mechanic → puzzles and encounters that use it → checkpoint corridor.

Tension comes from limited slots, short property timers and enemy fire; the reward is seeing a creative solution actually work.

### 2.4 Game moment (first five minutes) [proposal]

Gino stands in front of a gate held shut by a pressure plate. A huge boulder (600 kg) sits nearby. Gino aims the scanner at it, holds right click and presses **1** to copy its Mass, then aims at a sheet of paper (0.1 kg) and left clicks. The paper turns heavy, drops like a stone and slams the pressure plate; the gate opens while a timer bar over the paper counts down. This is the intended "wow" moment and teaches the whole scan–copy–paste cycle in a few seconds.

### 2.5 Puzzle philosophy

The path through the game is linear, but every puzzle is designed so that at least one solution is guaranteed. Further solutions are allowed to emerge from consistent physics rules. The team tests that every puzzle is solvable, and every encounter has a reset control.

---

## 3. Mechanics and Systems

### 3.1 Scan, copy and paste

1. Hold **right click** to open the scanner: editable objects are highlighted and the attributes of the aimed object are shown.
2. While scanning, press **1–5** to copy the corresponding attribute (1 Mass, 2 Velocity, 3 Temperature, 4 Magnetism, 5 Hardness) into the first free slot. Locked attributes are greyed out.
3. Select one or more slots pressing **1–5**, aim at a target and **left click** to paste. The slot is emptied.
4. The pasted value lasts for a limited time and then reverts to its baseline.

Invalid interactions show a brief explanation without changing the target. Gino cannot paste on himself.

### 3.2 The five physical attributes

| Attribute | Range | Available from | Thresholds and effects |
|---|---|---|---|
| Mass | 0.1 – 1000 kg | Start | ≤ 1 kg: ultra-light (easily carried).<br>≥ 200 kg: grounds light flyers and charging enemies.<br>≥ 500 kg: crushes buttons, squashes enemies, breaks weak floors. |
| Velocity | 0 – 50 m/s | Start | ≥ 20 m/s: turns an object into a ballistic projectile; when pasted, the object moves in the direction Gino is looking. |
| Temperature | 0 – 500 °C | Laboratory | ≤ 0: freezes water into ice walkways and immobilizes enemies.<br>≥ 70: superheats metal (contact damage).<br>≥ 100: evaporates water and ignites wood.<br>≥ 500: melts heavy metal barriers. |
| Magnetism | 0 – 2 T | Laboratory | 0: none. 1: moderate. 2: strongly attracts metallic objects and strips enemy shields and helmets. |
| Hardness | 1 – 10 | Start | ≤ 2: squishy and fragile (shatters, absorbs impacts).<br>≥ 8: diamond-rigid (bulletproof cover, stable bridge). |

Magnetism is unlocked in the laboratory. The Math Exam mini-boss cannot be trivialized: its arena contains no high-temperature sources, so the intended solution stays Velocity.

### 3.3 Stacking and independent timers

Several different attributes can be active on one object at the same time (for example 0 °C and 1000 kg on a box). Each has its own countdown; when one expires, only that attribute reverts. Pasting the same attribute again replaces its value and restarts its duration; values never add up.

### 3.4 Inventory

The briefcase uses fixed slots: a copied attribute goes into the first free slot, and if no slot is free the copy is refused. Using an attribute leaves its slot empty (gaps are allowed). Pressing **X** trashes the active slot. Copying stores the value at that moment and leaves the source unchanged.

### 3.5 Briefcase progression

| Moment | Slots | Attributes |
|---|---|---|
| Game start | 2 | Mass, Velocity, Hardness |
| End of the tutorial | 3 | — |
| Leaving the laboratory | 5 | + Temperature and Magnetism (unlocked inside the lab) |

*Overclock Modules* (collectibles that extend property duration from 3 s up to 7 s) are a nice-to-have. Base durations must be playtested early, because 3 s may be too short for some puzzles.

### 3.6 Health, apples and melee

Gino has an HP bar (starting values in [section 4.2](#42-roster-starting-values-to-be-tuned-in-playtests), to be tuned in playtests). Apples picked from trees (**E**) restore HP. A weak, short-range melee swing (**V**) pushes back enemies and breaks weak containers.

### 3.7 Controls

| Action | Binding | Function |
|---|---|---|
| Move and aim | WASD and mouse | First-person navigation |
| Jump | Space | Small jump (limited reach, to encourage physics solutions) |
| Scan | Hold right click | Show attributes of the aimed object; 1–5 copy |
| Paste | Left click | Apply the active slot (1–5 for multi-paste + press) |
| Select slot | Scroll wheel / 1–5 | Change the active slot |
| Trash slot | X | Discard the active property |
| Melee | V | Short-range briefcase swing |
| Interact | E | Pick apples, open doors, use switches |
| Reset encounter | R (hold) | Reset the props of the current encounter |
| Pause | Esc | Pause menu |

### 3.8 Object rules

- Fixed structures stay anchored unless designed to become movable.
- When a property expires, only that attribute reverts: objects keep their position, destroyed or melted barriers stay destroyed, unlocked gates stay unlocked. Mechanisms that need a continuous condition (such as pressure plates) stop working. Temporary transformations (frozen water, evaporated pools) revert.
- **Warning:** in the last second the timer bar flashes red, the object flickers and a ticking sound plays.
- Encounters with essential movable or destructible props have a clearly marked reset control that restores props and mechanisms. Objects that fall out of bounds respawn at their starting position.
- A few objects are indestructible and immovable by design, for example the asteroid, the only source of maximum magnetism, which appears only in the final fight.

### 3.9 Physics safety rules

The game is a fantasy dream, so extreme values are handled with dream logic instead of strict realism:

- **Hard caps** on every pasted value: mass 1000 kg, velocity 50 m/s.
- **Continuous collision detection** on pasted and fast objects, plus a velocity limit on rigidbodies, to avoid tunneling.
- **Dream overload:** an object carrying both Mass ≥ 500 kg and Velocity ≥ 40 m/s vanishes in a harmless cartoon puff after its first collision. This avoids unstable simulations; the boss's final phase relies on it (the block hits the core, then puffs away).
- **Impact damage** is capped per hit; heavy objects hurt Gino with fixed damage and never kill him instantly.

---

## 4. Enemies and AI

### 4.1 Common behavior

All enemies are projections of Gino's pet peeves (vegetables, bugs, math tests) and use a simple state machine:

**Idle/Patrol → Alert** (player in detection range, short telegraph) **→ Attack → Cooldown/Stunned → Dead.**

Every attack has a visible telegraph of about 0.5–1 s so the player has time to scan and paste. If the player respawns, the enemies respawn too, always in the same way.

### 4.2 Roster (starting values, to be tuned in playtests)

| Enemy | Behavior | HP | Detection | Attack and damage | Physics counter |
|---|---|---|---|---|---|
| Angry Carrot | Burrowed; charges at 7 m/s, then is stunned 2 s if it hits an obstacle | 30 | 12 m | Contact, 15 | ≥ 200 kg grounds it; high temperature ignites it |
| Corn Soldier | Guards a spot, fires 3-kernel burst every 2 s behind a leaf shield | 40 | 18 m | Kernel, 5 each | Magnetism strips helmet and shield; fast blocks break through |
| Spear Beetle | Patrols narrow corridors; thrusts when Gino is close | 50 | 8 m cone | Spear, 20 | Hardness 1 makes the shell shatter |
| Hornet / Mosquito | Hovers 5 m up and fires venom darts every 2.5 s | 20 | 20 m | Dart, 10 | ≥ 200 kg stalls its wings and it crashes |
| Math Exam (mini-boss) | Floating demonic book firing grade projectiles | 100 | Arena | Grade 5: 10; grade 4: 25 | Copy Velocity from a fast object and paste it on a grade while facing the book to reflect it (each reflected hit removes 20 HP) |
| Crazy Machine (final boss) | 3-phase heavy security robot | Per phase | Arena | Projectiles and slams | See [4.3](#43-boss-design) |

Gino starts with **100 HP**; each apple restores **25**.

### 4.3 Boss design

**Math Exam.** Phase 1 (100–50 HP): a grade 5 every 3 s. Phase 2 (below 50 HP): grade 4 mixed in and faster fire. Only reflected grades damage it, and the arena has no high-temperature sources.

**Crazy Machine.** Fought in the final arena, where small enemies spawn periodically (max 2 at a time) to provide apples and physics targets.

1. **Phase 1:** redirect its projectiles into the glowing joints (with Velocity, or with Magnetism from the asteroid on metal projectiles).
2. **Phase 2:** paste 0 °C on its coolant pipes so the radiator cannot dissipate heat; the core overheats and a vulnerability window opens.
3. **Phase 3:** launch a block with maximum Mass and maximum Velocity into the core.

---

## 5. Story and Narrative

**Premise.** On a stressful morning before school, Gino snoozes his alarm clock and falls into a vivid daydream where he meets his deepest nightmares and must use his imagination to escape.

**Real-world metaphors.** The briefcase scanner is his school backpack; the vegetable and bug enemies are his uneaten dinner carrots and his fear of insects; the Math Exam is the terrifying test scheduled that morning.

**Climax.** Defeating the Crazy Machine makes the world crumble as a 7:30 AM alarm rings, and Gino wakes up in his bedroom.

**Delivery.** The story is told through the environment and two static screens: a short opening screen (alarm clock, snooze) that fades into the tutorial, and the final wake-up screen showing his open math textbook, a plate of carrots, a bug net and his school bag. Gino is mute in the first version; vocalizations and text-to-speech quotes are a stretch goal.

---

## 6. World and Level Design

The areas follow the level sketch (L0 to L4) after the merges suggested by the team. The exact setting of each area will be decided from the available asset packs.

| Area | Sketch | Focus | Encounters | Unlocks and checkpoint |
|---|---|---|---|---|
| 1. Tutorial zone | L0 + L1 | Movement, jump, eating an apple, scanner, first Mass and Velocity puzzles | 2 Spear Beetles, 1 Angry Carrot | Slot 3 at the end; checkpoint |
| 2. Math Exam clearing | L2 | Practice with all five attributes; no high-temperature sources | Hornets, Math Exam mini-boss | Checkpoint after the boss |
| 3. Laboratory | L1.1 | Unlock Temperature and Magnetism; Temperature puzzle (freeze water into an ice bridge, burn brambles); Slot Expansion puzzle | Corn Soldiers (need Magnetism), Angry Carrot | Slots 4–5 when leaving; checkpoint |
| 4. Puzzle passage (cuttable) | L3 / L3.1 | Jump and soft-landing puzzle using low Hardness; waterfall | Hornets, Angry Carrots | Checkpoint |
| 5. Security arena | L4 | All five attributes; final boss | Crazy Machine and spawned minions | Ends the demo with the wake-up screen |

---

## 7. Objects Catalogue

Starting values, to be tuned in Unity (for example as ScriptableObjects). Ambient temperature is 20 °C; Magnetism 0 means non-magnetic; "Metal" marks objects attracted by magnetism.

| Object | Mass (kg) | Velocity (m/s) | Temp (°C) | Magnetism (T) | Hardness | Metal |
|---|---|---|---|---|---|---|
| Tree | 800 | 0 | 20 | 0 | 5 | – |
| Rock (boulder) | 1000 / 600 / 300 | 0 | 10–20 | 0 | 7 | – |
| Paper | 0.1 | 0 | 20 | 0 | 1 | – |
| Water (channel) | 100 | 1.5–5 | 10 | 0 | 1 | – |
| Train | 1000 | 30 | 20 | 0 | 9 | Y |
| Plants | 2 | 0 | 20 | 0 | 1 | – |
| Magnet | 2 / 10 | 0 | 20 | 1 | 6 | Y |
| Asteroid (indestructible) | 1000 | 0 | 20 | 2 | 8 | Y |
| Pen | 0.1 | 0 | 20 | 0 | 3 | – |
| Pencil | 0.1 | 0 | 20 | 0 | 2 | – |
| Rubber (eraser) | 0.1 | 0 | 20 | 0 | 1 | – |
| Bag | 2 | 0 | 20 | 0 | 2 | – |
| Mattress | 10 | 0 | 20 | 0 | 1 | – |
| Couch | 80 | 0 | 20 | 0 | 2 | – |
| Glass | 0.3 | 0 | 20 | 0 | 2 | – |
| Metal gates | 600 | 0 | 20 | 0 | 8 | Y |
| House | 1000 | 0 | 20 | 0 | 7 | – |
| Fridge | 120 | 0 | 4 | 0.3 | 6 | Y |
| Ice cream | 0.3 | 0 | 0 | 0 | 1 | – |
| Fork, spoon, knife | 0.1 | 0 | 20 | 0 | 6 | Y |
| Chair | 7 | 0 | 20 | 0 | 3 | – |
| Table | 25 | 0 | 20 | 0 | 4 | – |
| Glass cups (lab) | 0.3 | 0 | 20 | 0 | 2 | – |
| Brick | 3 | 0 | 20 | 0 | 6 | – |
| Screws | 0.1 | 0 | 20 | 0 | 7 | Y |
| Wrench | 1.2 | 0 | 20 | 0 | 8 | Y |
| Lava | 500 | 0.5 | 700 | 0 | 1 | – |
| Leaves | 0.1 | 1 | 20 | 0 | 1 | – |
| Plume (feather) | 0.1 | 1 | 20 | 0 | 1 | – |
| Toolbox and tools | 10 | 0 | 20 | 0 | 6 | Y |
| Anvil | 100 | 0 | 20 | 0 | 9 | Y |
| Boiling water (lab) | 5 | 0 | 100 | 0 | 1 | – |
| Stove | 60 | 0 | 250 | 0 | 7 | Y |
| Oven | 70 | 0 | 220 | 0 | 7 | Y |
| Diamonds | 0.5 | 0 | 20 | 0 | 10 | – |
| Pillow | 0.5 | 0 | 20 | 0 | 1 | – |
| Closet | 90 | 0 | 20 | 0 | 4 | – |
| Towel | 0.4 | 0 | 20 | 0 | 1 | – |
| Tissues | 0.1 | 0 | 20 | 0 | 1 | – |
| Toothbrush | 0.1 | 0 | 20 | 0 | 2 | – |
| Campfire | 5 | 0 | 300 | 0 | 1 | – |
| Apple | 0.2 | 0 | 15 | 0 | 2 | – |
| Crate (added) | 20 | 0 | 20 | 0 | 4 | – |
| Wooden brambles (added) | 10 | 0 | 20 | 0 | 2 | – |

---

## 8. Interface (UI/UX)

### 8.1 HUD

- Crosshair and health bar.
- Five briefcase slots showing attribute icon and value, with the active slot highlighted and empty slots greyed out.
- Scanner panel with the five attributes of the aimed object (original values shown apart from temporarily modified ones).
- **Property timers:** above every object with a pasted property, an icon and a bar that shrinks with the remaining time (red and flashing in the last second).
- Prompts for interaction and reset, and short messages for invalid actions.

### 8.2 Menus and screens

- **Main menu:** Start, Options, Quit.
- **Pause menu:** Resume, Options.
- **Options:** master volume and mouse sensitivity.
- **Death screen:** a static MS-Paint image of Gino with spiral eyes, the text *"Nightmare lost…"* and two buttons, *Snooze and retry* (respawn at the checkpoint) and *Main menu*. Simple fade, no complex animation.
- **Ending screen:** a static image of Gino waking up in his bedroom, a *"Thanks for playing"* message and a button back to the main menu.

---

## 9. Art and Audio

**Art.** High contrast between a realistic PhysX simulation and crude, humorous visuals: low-poly packs (Synty/Kenney) with MS-Paint style textures made by the team.

**Audio** *(low priority, to be defined later).* Placeholder royalty-free sound effects first. Planned later: distorted impact sounds, cues for scan, copy, paste and expiry, a comedic soundtrack with kazoos and out-of-tune recorders, and Gino's squeaks, grunts and text-to-speech quotes.

---

## 10. Production

### 10.1 Technology

- Unity 3D (version 6000.3.27f1; Unity 6 LTS suggested) with the built-in PhysX physics.
- Packages: Input System, Cinemachine (first-person camera), TextMeshPro, ProBuilder (blockouts).
- Version control: Git with a GitHub remote and a Unity-specific `.gitignore`.
- Assets: Synty/Kenney low-poly packs (licenses to be checked for educational use) and original MS-Paint textures; royalty-free audio.

### 10.2 Milestones

| Date | Milestone | Planned content |
|---|---|---|
| 02/11/2026 | Design document | This document |
| 06/12/2026 | Prototype | Area 1 playable: first-person controller, scan/copy/paste with Mass and Velocity, 2 slots, one enemy type, main and pause menus |
| 10/01/2027 | Beta | Areas 1–3 and 5 playable end to end, all five attributes, both bosses, HUD, death and ending screens |
| TBD | Final version | Area 4 if time allows, polish, audio, balancing |

### 10.3 Scope tiers

- **Must have:** first-person controller, the five attributes, scan/copy/paste, slot progression, three or more enemy types, both bosses, HUD, menus, death and ending screens.
- **Should have:** Area 4, apples and melee, reset control, stacking and multi-paste.
- **Nice to have:** Overclock Modules, audio and Gino's voice, extra objects, extra solutions per puzzle.

### 10.4 Job subdivision

| Area | Owner |
|---|---|
| First-Person Character Controls | Davide Lasagni |
| Attributes and physics of objects | Aigerim turazhanova |
| Enemy AI and bosses | Marcostefano Debernardi |
| Level design and art integration | Lucia Xiong Xiao |
| UI and menus | Davide Palazzi |
| Audio (later) | Davide Lasagni, TBD |
| Playtesting and balancing | Everyone |

### 10.5 Main risks

- **Unstable physics at extreme values:** hard caps, continuous collision detection, dream overload (see [3.9](#39-physics-safety-rules)).
- **Property duration too short (3 s):** test in the first prototype.
- **Scope too big for 20 minutes:** keep Area 4 cuttable and build one area at a time.
- **Unsolvable states:** reset control and out-of-bounds respawn in every encounter.
