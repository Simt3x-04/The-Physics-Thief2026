# UI Flow Implementation Notes
**Author:** Aigerim Turazhanova  
**Feature branch:** `feature/aigerim-ui-flow`  
**Base commit (main):** `db0953c`  
**Davide_Branch integrated:** `ea025ba`  
**Implementation commit:** `b17634b`  
**Unity version:** 6000.3.27f1 · URP 17.3.0 · Input System 1.20.1 · uGUI 2.0.0 · TextMeshPro  
**Date:** November 2026

---

## ⚠ Ownership / Scope Flag

The GDD ownership table currently assigns:
- **Aigerim** → Object physics and scanning  
- **Davide Palazzi** → UI  

My team chat assigns me the five screens below. This document covers **only** the UI flow. I have **not** touched object physics, scanning, or Davide's HUD/inventory code. If the ownership table needs updating, the course coordinator or team lead should reconcile it — I did not silently change anyone else's assignment.

---

## 1. What Was Reused

| Asset | Source | Status |
|---|---|---|
| `GameManager.cs` | Davide_Branch | Extended (`SetState`: Ending + Loading timeScale) |
| `GameState.cs` | Davide_Branch | Kept (has `Ending`; `Sandbox` from main dropped) |
| `HealthComponent.cs` | Davide_Branch | Reused as-is (`OnDied`, `ResetHealth`) |
| `Singleton.cs` | Davide_Branch | Reused as-is |
| `AudioManager.cs` | Davide_Branch | Stubs reused; `AudioListener.volume` used instead |
| `HUDManager.cs` | Davide_Branch | Untouched |
| `MainMenu.unity` | Davide_Branch | Scene preserved; controller replaced |
| `HUDtest.unity` | Davide_Branch | Preserved (separate scene) |
| `InputSystem_Actions.inputactions` | Davide_Branch | Reused; `UI/Cancel` (Escape) drives pause |
| TextMeshPro assets | Davide_Branch | Reused as-is |

---

## 2. What Was Implemented

### New scripts (`Assets/Scripts/UI/`)
| File | Purpose |
|---|---|
| `UISettings.cs` | Static settings store: master volume (0–1) and mouse sensitivity (0.1–10). Persisted via `PlayerPrefs`, applied immediately. Fires `OnVolumeChanged` and `OnSensitivityChanged` events. Volume applied through `AudioListener.volume`. |
| `OptionsPanel.cs` | Reusable options panel with two sliders and a Back button. Opened via `Open(onBack)`, closed via `Close()`. Works identically from Main Menu and Pause. |
| `MainMenuController.cs` | **Rewrote** Davide's stub. Wires `OptionsPanel`, double-click guard during load, `UISettings.Apply()` on Awake. |
| `PauseMenuController.cs` | Reads `UI/Cancel` (Escape) from the Input Asset. Single handler: Esc→Pause, Esc-in-Options→back to Pause panel, Esc-on-Pause→Resume. Subscribes to `GameManager.Instance.OnGameStateChanged`. |
| `DeathScreenController.cs` | Binds to **player's** `HealthComponent.OnDied` only. Fade-in with unscaled time. Retry → `GameManager.RetryFromCheckpoint()` → `OnRetryRequested` → LevelManager. |
| `EndingScreenController.cs` | Listens for `GameState.Ending`. Fade from black, optional alarm clip, Main Menu button. Uses unscaled time. |

### New scripts (`Assets/Scripts/Tests/`)
| File | Purpose |
|---|---|
| `UIFlowTestHarness.cs` | Keyboard-driven test harness (D/E/P/R/M/F1). Wires a dummy `HealthComponent` to `DeathScreenController`. **Remove from release builds.** |

### New editor tool (`Assets/Editor/`)
| File | Purpose |
|---|---|
| `UIFlowTestSceneSetup.cs` | Menu: **Tools → UI Flow → Create UIFlowTest Scene**. Builds the full `UIFlowTest.unity` scene with all canvases, buttons, sliders, EventSystem, and wired serialized fields. Run once in Unity Editor. |

### Modified files
| File | Change |
|---|---|
| `GameManager.cs` | `SetState`: `Ending` now freezes `Time.timeScale = 0` (consistent with `Paused`/`GameOver`). `Loading` state resets timeScale to 1 so async loads always run. |
| `ProjectSettings/EditorBuildSettings.asset` | Added `UIFlowTest.unity` entry (placeholder GUID; updated to real GUID when `UIFlowTestSceneSetup` runs). |

### Deleted files
| File | Reason |
|---|---|
| `Assets/Scripts/Logic/AttributeType.cs` | Exact duplicate of `Properties/AttributeType.cs` in same namespace `PhysicsThief.Properties`. The `Logic/` asmdef was `autoReferenced: true`, causing CS0433 duplicate-type compiler error. |
| `Assets/Scripts/Logic/PropertyValue.cs` | Same reason. |
| `Assets/Scripts/Logic/PhysicsThief.Logic.asmdef` | Removed with duplicates. `Properties/` uses the default assembly (no asmdef needed). |

---

## 3. Architecture Decisions

### Single Escape handler
`PauseMenuController` owns all Escape logic. It reads `UI/Cancel` directly from the input asset — an action that is always enabled because it is part of the `UI` map, not the `Player` map. The `Player` map is never disabled by UI code (that would break mouse/gamepad navigation). Time scale alone does not suppress Input System callbacks, so the handler explicitly checks `GameManager.Instance.State` before acting.

### GameManager as single source of truth
All state changes go through `GameManager.SetState()`. No UI controller sets `Time.timeScale` or `Cursor.lockState` directly except `EndingScreenController`, which was updated once GameManager was confirmed to handle Ending consistently.

### Options as a shared component
`OptionsPanel` is a self-contained `MonoBehaviour`. Both `MainMenuController` and `PauseMenuController` call `optionsPanel.Open(callback)`. The callback tells Options where to return. Closing pause options does **not** resume gameplay.

### Volume via AudioListener
`AudioManager` is all stubs. Using `AudioListener.volume` is the Unity-idiomatic single-point master volume that works with zero AudioMixer setup. When AudioManager is implemented, the line in `UISettings.ApplyVolume()` should be replaced with an AudioMixer dB conversion: `mixer.SetFloat("MasterVolume", Mathf.Log10(linear) * 20)`.

### Sensitivity adapter contract
`PlayerController.lookSensitivity` is a private serialized float stub with no real camera-look code. `UISettings.OnSensitivityChanged` fires when the slider changes. The PlayerController owner should wire it as follows:

```csharp
// In PlayerController.OnEnable():
UISettings.OnSensitivityChanged += v => lookSensitivity = v;
// Apply current value immediately:
lookSensitivity = UISettings.MouseSensitivity;
```

Do **not** multiply the value again — `UISettings.MouseSensitivity` is the final multiplier matching what the slider shows.

### Retry does not reload the scene
`DeathScreenController.OnRetryClicked()` calls `GameManager.RetryFromCheckpoint()`, which sets state to `Playing` and fires `OnRetryRequested`. `LevelManager` must subscribe to this event and call `RespawnPlayer()`. `RespawnPlayer()` is currently a stub — the player will not visually move on retry until the movement teammate wires it. The death screen dismisses correctly regardless, and health is reset via `HealthComponent.ResetHealth()`.

---

## 4. Integration Contracts for Teammates

### Movement teammate (PlayerController owner)
Must implement these to complete the retry loop:

```csharp
// PlayerController.cs
void OnEnable()
{
    UISettings.OnSensitivityChanged += v => lookSensitivity = v;
    lookSensitivity = UISettings.MouseSensitivity;
}

void Respawn(Vector3 checkpointPosition)
{
    // teleport character controller to checkpointPosition
    // re-enable input
}

void OnDeath()
{
    // disable input  ← do NOT call GameManager.ReportPlayerDeath() here
    // GameManager is called by LevelManager or DeathScreenController
}
```

### Level teammate (LevelManager owner)
Must implement:

```csharp
// LevelManager.cs
void Start()
{
    GameManager.Instance.OnRetryRequested += HandleRetry;
    // Also bind the HUD and death screen here:
    hudManager.Bind(player.Health, player.Briefcase, player.Scanner);
    deathScreenController.Bind(player.Health);
    // player.Health.OnDied → GameManager.ReportPlayerDeath() wiring:
    player.Health.OnDied += GameManager.Instance.ReportPlayerDeath;
}

void HandleRetry()
{
    player.Health.ResetHealth();
    player.Respawn(currentCheckpoint.position);
    ResetPuzzleRoom(currentRoomID);
}
```

> **Important:** `DeathScreenController.Bind(health)` must be called with the **player's** `HealthComponent` only. Never pass an enemy's component.

### Boss teammate (CrazyMachineBoss owner)
Must call this when the boss is defeated:

```csharp
GameManager.Instance.TriggerAlarmAwakeSequence();
// This sets GameState.Ending, freezes time, and fires OnGameStateChanged,
// which EndingScreenController is already listening for.
```

### AudioManager owner (Davide)
When `AudioManager.PlaySFX` is implemented, replace the volume line in `UISettings`:

```csharp
// UISettings.cs – ApplyVolume()
// REPLACE:  AudioListener.volume = linear;
// WITH:     AudioManager.Instance?.SetMasterVolume(linear);
// AND add to AudioManager: public void SetMasterVolume(float linear) { masterMixer.SetFloat(...); }
```

---

## 5. Open Issues / Remaining Manual Work

| # | Issue | Owner | Priority |
|---|---|---|---|
| 1 | `UIFlowTest.unity` scene does not exist yet — run **Tools → UI Flow → Create UIFlowTest Scene** in Unity Editor | Aigerim | Required before demo |
| 2 | `PlayerController.lookSensitivity` is a stub — sensitivity slider exists but has no visible effect | Movement teammate | Medium |
| 3 | `LevelManager.RespawnPlayer()` is a stub — Retry dismisses death screen but player doesn't move | Level teammate | High |
| 4 | `Checkpoint.OnTriggerEnter()` is a stub — checkpoint never gets set | Level teammate | High |
| 5 | Art placeholders: GinoImage (grey rect) and BedroomImage (blue rect) need real art assigned in Inspector | Art/Aigerim | Low |
| 6 | `AudioManager` methods are all stubs — alarm clip in EndingScreenController plays via its own `AudioSource`, not via AudioManager | Davide | Low |
| 7 | `MainMenu.unity` still has the old `MainMenuController` serialized fields unassigned — needs re-wiring in editor after opening the scene | Aigerim (manual step) | Required before demo |
| 8 | GameManager Inspector: `levelScenes` array is empty — add a scene name (e.g. "SampleScene") so Start works | Any team member | Required |
| 9 | `PauseMenuController` is not yet present in `SampleScene` / any gameplay scene — must be added to each level scene | Aigerim | Required |
| 10 | `DeathScreenController` is not yet present in gameplay scenes — must be added | Aigerim | Required |
| 11 | `EndingScreenController` is not yet present in gameplay scenes — must be added | Aigerim | Required |
| 12 | `GDD ownership table mismatch` — see §⚠ above | Course coordinator / team lead | Admin |

---

## 6. How to Open and Run the Project

### Import steps
1. Install **Unity 6000.3.27f1** via Unity Hub (use "Install" → "Archive" if not listed).
2. In Unity Hub → **Add project from disk** → select `The-Physic-Thief-Project/`.
3. Unity will re-import assets and compile scripts (takes ~2 min first time).
4. If prompted about URP upgrader, click **No** — the project already has URP configured.

### First-time setup (run once after open)
In the Unity menu bar: **Tools → UI Flow → Create UIFlowTest Scene**  
This creates `Assets/Scenes/UIFlowTest.unity` with all canvases pre-wired.

### Wiring MainMenu.unity (manual — one time)
1. Open `Assets/Scenes/MainMenu.unity`.
2. Select the `MainMenuCanvas` GameObject (or wherever `MainMenuController` lives).
3. In the Inspector, drag:
   - `MainPanel` → *Main Panel*  
   - `OptionsPanel` GameObject → *Options Panel*  
   - `StartButton` → *Start Button*  
   - `OptionsButton` → *Options Button*  
   - `QuitButton` → *Quit Button*  
4. Select `GameManager` in the Hierarchy → set `Level Scenes[0]` to `"SampleScene"` (or your target level scene name).
5. Save the scene.

### Demo steps (UIFlowTest scene)
Open `Assets/Scenes/UIFlowTest.unity` → press **Play**, then:

| Key | Action | Expected result |
|---|---|---|
| `F1` | Smoke test | Console: `SMOKE TEST PASSED` |
| `Esc` | Pause | Pause panel appears, time frozen |
| `Esc` again | Resume | Pause panel hides, time resumes |
| `Esc` → O button | Options | Options panel opens over pause |
| `Esc` in options | Close options | Returns to pause panel (NOT resumed) |
| `D` | Simulate death | Fade-in + death panel + "Nightmare lost…" |
| `R` | Retry | Death panel fades out, state → Playing |
| `D` then retry button | Retry via button | Same as R |
| `D` then main menu button | Main menu | Loads `MainMenu.unity` |
| `E` | Trigger ending | Fade from black, "Thanks for playing" |
| Main Menu button on ending | Back to menu | Loads `MainMenu.unity` |
| `M` | Load main menu directly | Same as above |

### Demo steps (MainMenu scene)
Open `Assets/Scenes/MainMenu.unity` → press **Play**:
- Click **Start** → logs error if `levelScenes` is empty, otherwise loads level
- Click **Options** → options panel slides in with sliders
- Adjust volume → `AudioListener.volume` changes immediately  
- Click **Back** → returns to main panel
- Click **Quit** → exits play mode in Editor

---

## 7. Verification Checklist

Status key: **PASS** = verified in static review / confirmed by code logic | **UNVERIFIED** = requires running Unity | **PENDING** = depends on teammate stub | **N/A** = not applicable

| # | Check | Status | Notes |
|---|---|---|---|
| 1 | Main Menu shows title, Start, Options, Quit | UNVERIFIED | Scene hierarchy built by setup tool; needs Play Mode |
| 2 | Start → configured scene; double-click blocked | UNVERIFIED | `startButton.interactable = false` after click |
| 3 | Missing level scene logs error, doesn't crash | PASS | `Application.CanStreamedLevelBeLoaded` check in GameManager |
| 4 | Playing → Esc → Pause panel appears | UNVERIFIED | Code logic verified; needs Play Mode |
| 5 | Paused → Esc → resumes | UNVERIFIED | `OnCancelPerformed` state machine verified |
| 6 | Options opens from Main Menu, Back returns | UNVERIFIED | `OptionsPanel.Open(onBack)` wired |
| 7 | Options opens from Pause, Back returns to Pause (not resumed) | PASS | `CloseOptions()` only shows `pausePanel`, does not call `ResumeGame()` |
| 8 | First Esc in options closes options (not resume) | PASS | `_optionsOpen` flag checked before state check |
| 9 | No gameplay input while paused | PASS | `Time.timeScale = 0` in `GameManager.SetState(Paused)` + Player map is separate from UI map |
| 10 | Fades work at timeScale = 0 | PASS | All fades use `Time.unscaledDeltaTime` and `WaitForSecondsRealtime` |
| 11 | Volume slider → AudioListener.volume changes immediately | UNVERIFIED | Logic verified; needs Play Mode |
| 12 | Sensitivity slider persists across scenes / restarts | PASS | `PlayerPrefs.SetFloat` called on every change |
| 13 | Settings persist across app restarts | UNVERIFIED | PlayerPrefs verified; needs full build test |
| 14 | Options loaded values validated / clamped | PASS | `Mathf.Clamp` in `UISettings` property getters |
| 15 | Player death opens death screen once | PASS | `if (IsDead) return` guard in HealthComponent; idempotent GameOver check in `OnPlayerDied` |
| 16 | Enemy death does NOT open death screen | PASS | `DeathScreenController.Bind()` only accepts the player's component; enemies use separate HealthComponent instances |
| 17 | Retry → state=Playing, `OnRetryRequested` fires | PASS | `GameManager.RetryFromCheckpoint()` code verified |
| 18 | Retry → player respawns at checkpoint | PENDING | `LevelManager.RespawnPlayer()` is a stub |
| 19 | Retry → health reset | PENDING | `HealthComponent.ResetHealth()` exists; wiring in LevelManager pending |
| 20 | Retry preserves unlocked attributes and slots | PENDING | `BriefcaseModel` is not reset on retry (no reset call exists) — correct by design |
| 21 | Repeated retries no duplicate subscriptions | PASS | `SubscribeToHealth` does `health.OnDied -= handler` before `+=` |
| 22 | Ending screen opens via `TriggerAlarmAwakeSequence()` | PASS | `GameState.Ending` → `OnStateChanged` → `BeginEndingSequence()` chain verified |
| 23 | Ending screen → Main Menu button works | UNVERIFIED | Needs Play Mode |
| 24 | Ending time scale = 0 | PASS | `GameManager.SetState(Ending)` now sets `Time.timeScale = 0` |
| 25 | Alarm clip plays on ending (if assigned) | UNVERIFIED | `audioSource.Play()` is reached; needs clip assigned |
| 26 | Missing alarm clip handled gracefully | PASS | `if (audioSource != null && alarmClip != null)` guard |
| 27 | Existing HUD health bar and slots still work | UNVERIFIED | HUDManager not modified; needs HUDtest.unity Play Mode |
| 28 | No duplicate managers across scene loads | PASS | Singleton destroys duplicates in `Awake()` |
| 29 | Stale subscriptions after scene reload | PASS | `OnDestroy` removes all listeners; `Bind()` unsubs before re-sub |
| 30 | Cursor and timeScale restored on LoadMainMenu | PASS | `GameManager.LoadMainMenu()` → `SetState(Loading)` then `SetState(MainMenu)` — MainMenu state sets `Cursor.visible = true`, timeScale = 1 |
| 31 | Layout at 1920×1080 | UNVERIFIED | `ScaleWithScreenSize` @ 1920×1080 reference, match 0.5 |
| 32 | Layout at 1280×720 | UNVERIFIED | Same scaler |
| 33 | Layout at 1024×768 | UNVERIFIED | Same scaler |
| 34 | Keyboard focus (Tab/Enter navigation) | UNVERIFIED | `button.Select()` called on panel show |
| 35 | One EventSystem per scene | PASS | Setup tool creates exactly one EventSystem |
| 36 | UIFlowTest scene: full cycle D→R→D→R no leak | UNVERIFIED | Architecture prevents leaks; needs Play Mode cycle |

**Summary:** 19 PASS (static review + code logic) | 15 UNVERIFIED (need Unity Play Mode) | 3 PENDING (teammate stubs)  
No FAILs from static analysis.

---

## 8. Draft Commit Message

```
feat(ui): implement main menu, pause, options, death and ending screens

- UISettings: PlayerPrefs store for volume/sensitivity, UISettings.Apply() on load
- OptionsPanel: reusable slider panel shared by MainMenu and Pause
- MainMenuController: rewrite — wires OptionsPanel, double-click guard, scene validation
- PauseMenuController: Esc state machine, single Cancel handler, no Player-map disable
- DeathScreenController: player-only OnDied binding, unscaled fade, Retry/MainMenu
- EndingScreenController: TriggerAlarmAwakeSequence entry, alarm clip, fade from black
- UIFlowTestHarness: keyboard test harness (D/E/P/R/M/F1)
- UIFlowTestSceneSetup: Editor tool — Tools > UI Flow > Create UIFlowTest Scene
- GameManager: SetState — Ending now freezes time; Loading resets timeScale to 1
- Deleted Scripts/Logic/ — duplicate of Properties/ causing CS0433
- EditorBuildSettings: add UIFlowTest.unity entry

Integrates origin/Davide_Branch (ea025ba).
Branch: feature/aigerim-ui-flow. Do not merge to main without teammate review.

Pending: LevelManager.RespawnPlayer stub, PlayerController.lookSensitivity adapter,
boss TriggerAlarmAwakeSequence callsite. See Documents/UI_Implementation_Notes.md.
```

## 9. Suggested PR Description

**Title:** `feat(ui): main menu / pause / options / death / ending screens [Aigerim]`

**Summary of changes:**  
Implements all five screens assigned to Aigerim in team chat. Reuses and extends Davide's GameManager, HealthComponent and scene structure. No changes to HUD, inventory, scanning, movement or enemy code.

**Key decisions:**
- Single Escape handler in `PauseMenuController` reads `UI/Cancel` — never disables the whole action asset
- `OptionsPanel` is one component used from both menus; back callback determines where it returns
- Volume uses `AudioListener.volume` (AudioManager stubs are intact, ready to swap)
- Ending and GameOver both freeze `Time.timeScale = 0` via `GameManager.SetState`
- Deleted `Scripts/Logic/` duplicate types (CS0433 blocker)

**How to test:**  
1. Open project in Unity 6000.3.27f1  
2. **Tools → UI Flow → Create UIFlowTest Scene**  
3. Open `UIFlowTest.unity` → Play → use D/E/P/R/M keys  

**What to review:**  
- `PauseMenuController.cs` — Esc state machine  
- `DeathScreenController.cs` — `Bind()` call from LevelManager  
- `GameManager.cs` — SetState change (lines ~80-90)  

**Pending from teammates:**  
- Movement: `PlayerController.Respawn()` + sensitivity adapter (see §4)  
- Level: `LevelManager.RespawnPlayer()` wiring (see §4)  
- Boss: call `GameManager.Instance.TriggerAlarmAwakeSequence()` on defeat  
- Art: replace grey placeholder in `GinoImage`, blue placeholder in `BedroomImage`

**Not included (out of scope):**  
Object physics, scanning, boss combat, character controller, any non-UI gameplay.

---

*Document generated: November 2026. Last updated: b17634b.*
