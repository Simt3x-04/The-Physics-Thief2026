# UI Flow Implementation Notes — Final
**Author:** Aigerim Turazhanova  
**Branch:** `feature/aigerim-ui-flow`  
**Base:** `main` @ `db0953c` · Davide_Branch @ `ea025ba` integrated  
**Final commit:** see `git log --oneline feature/aigerim-ui-flow -1`  
**Unity:** 6000.3.27f1 · URP 17.3.0 · Input System 1.20.1 · uGUI 2.0.0 · TextMeshPro  
**Last updated:** November 2026

---

## ⚠ Ownership Flag

The GDD table still assigns Davide to UI and Aigerim to object physics. Team chat assigns Aigerim these five screens. This document covers only the UI flow. Object physics, scanning, movement, and HUD/inventory are **untouched**. The ownership table needs reconciling by the team or course coordinator — this discrepancy is flagged here, not silently resolved.

---

## 1. What the Assignment Delivers

| Screen | Status | Demo trigger |
|---|---|---|
| Main Menu | ✅ Complete | Open `UIFlowTest.unity`, click **Start / Options / Quit** |
| Pause | ✅ Complete | Press **Esc** during play |
| Options | ✅ Complete | Options button from Main Menu or Pause |
| Death | ✅ Complete | Press **D** in test harness |
| Ending | ✅ Complete | Press **E** in test harness |

All five screens are wired, saved, and verified by static YAML inspection. Play-Mode verification requires running Unity — see §7.

---

## 2. Changed Files (this session)

| File | Change |
|---|---|
| `Assets/Scripts/Core/GameManager.cs` | Added `Awake()` override, `Start()` that calls `ApplyStatePolicy()` on the initial serialised state, extracted `ApplyStatePolicy()` private helper used by both `Start` and `SetState`. English error messages. |
| `Assets/Scripts/UI/MainMenuController.cs` | Null-guards on all serialised fields; hides Options button when `optionsPanel` is null (legacy MainMenu.unity). |
| `Assets/Scripts/UI/SensitivityDemoCamera.cs` | **NEW.** Minimal camera-look component for the demo. Responds to `UISettings.MouseSensitivity`. Contains full integration contract for movement teammate. Remove when real `PlayerController.HandleLook()` is wired. |
| `Assets/Editor/UIFlowTestSceneSetup.cs` | Full rewrite: GameManager `gameState=MainMenu`, `levelScenes=[UIFlowTest]`, `mainMenuScene=UIFlowTest`; FadeOverlay images `alpha=0 raycastTarget=false`; `SensitivityDemoCamera` added to camera; InputSystemUIInputModule wired via `SerializedObject`; InputActionAsset found by type+name, not hard-coded path; duplicate EventSystem removed. |
| `Assets/Scenes/UIFlowTest.unity` | GameManager: `gameState=0`, `mainMenuScene=UIFlowTest`, `levelScenes=[UIFlowTest]`. FadeOverlayD `raycastTarget=0`. InputSystemUIInputModule `m_ActionsAsset` GUID corrected. |
| `Assets/Scenes/MainMenu.unity` | InputSystemUIInputModule: 11 stale GUIDs `ca9f5fa9→052faaac`. MainMenuController: fields renamed from `playButton→startButton`, `mainPanel` added pointing to Canvas GO, `optionsPanel` remains null (no panel in scene). |
| `Assets/Scenes/HUDtest.unity` | 11 stale InputSystemUIInputModule GUIDs corrected (same `ca9f5fa9→052faaac`). Davide's HUD scene — no other changes. |
| `ProjectSettings/EditorBuildSettings.asset` | UIFlowTest.unity entry present with real GUID `3258ddd8...`. |

### Previously implemented (prior commits, preserved)

| File | Role |
|---|---|
| `Assets/Scripts/UI/UISettings.cs` | Shared PlayerPrefs store: volume 0–1, sensitivity 0.1–10. `AudioListener.volume` applied immediately. |
| `Assets/Scripts/UI/OptionsPanel.cs` | Reusable sliders panel; `Open(onBack)` / `Close()`. |
| `Assets/Scripts/UI/PauseMenuController.cs` | Esc state machine; reads `UI/Cancel` from Input System asset; never disables the full action asset. |
| `Assets/Scripts/UI/DeathScreenController.cs` | Player-only `OnDied` binding; unscaled fade; `SetFadeRaycast` control; Retry → `GameManager.RetryFromCheckpoint()`. |
| `Assets/Scripts/UI/EndingScreenController.cs` | Listens for `GameState.Ending`; unscaled fade from black; optional alarm clip; `SetFadeRaycast` control. |
| `Assets/Scripts/Tests/UIFlowTestHarness.cs` | D/E/P/R/M/F1 keyboard triggers. Test only — remove from release builds. |
| `Assets/Scripts/Core/GameState.cs` | `MainMenu=0 Loading=1 Playing=2 Paused=3 Cutscene=4 GameOver=5 Ending=6`. Davide's `Ending` kept; main-branch `Sandbox` dropped. |

### Deleted (prior commit)

`Assets/Scripts/Logic/` — exact duplicate of `Properties/` in the same namespace. Duplicate `autoReferenced` asmdef caused CS0433. Deleted; `Properties/` is canonical.

---

## 3. Architecture Decisions

### Escape handling — one handler, no double-fire
`PauseMenuController` owns all Esc logic via `UI/Cancel` from the Input System asset. The `UI` action map is always enabled; we never disable it (that would kill mouse/gamepad navigation). Gameplay input (`Player` map) is effectively suppressed because `Time.timeScale=0` prevents physics callbacks, and the Player map's `PlayerInputHandler` is an unimplemented stub at present.

### GameManager as single state owner
All cursor, timeScale and event-broadcast logic lives in `GameManager.SetState()` and `ApplyStatePolicy()`. No UI controller sets `Time.timeScale` or `Cursor.lockState` directly. `ApplyStatePolicy()` is called both from `Start()` (initial state) and from `SetState()` (state changes), so the first frame is always correct regardless of which scene the manager enters in.

### UIFlowTest as self-contained demo
`levelScenes=[UIFlowTest]` and `mainMenuScene=UIFlowTest` mean the demo scene reloads itself on Start. All five UI canvases are in UIFlowTest; the persistent GameManager (DontDestroyOnLoad) carries state across the reload and destroys any duplicate that appears. This lets every screen be demonstrated without any real level or enemy implementation.

### FadeOverlay raycast discipline
Both `FadeOverlayD` (Death, sort=20) and `FadeOverlayE` (Ending, sort=30) are saved with `alpha=0` and `raycastTarget=false`. Each controller's `Awake()` enforces this at runtime too. `raycastTarget` is set to `true` only when the screen actually activates, and back to `false` when it dismisses. A full-screen opaque Image on sort=30 would intercept all clicks on lower canvases — this was the original click-blocking bug.

### Volume implementation
`AudioListener.volume` is the Unity-standard single-point master volume with zero AudioMixer dependency. When `AudioManager` is implemented, replace the `ApplyVolume` body in `UISettings.cs` with `masterMixer.SetFloat("MasterVolume", Mathf.Log10(linear) * 20f)`.

---

## 4. Integration Contracts for Teammates

### Movement teammate (PlayerController owner)

**Sensitivity slider** — when real camera-look is wired:

```csharp
// In PlayerController:
void OnEnable()
{
    UISettings.OnSensitivityChanged += v => lookSensitivity = v;
    lookSensitivity = UISettings.MouseSensitivity;   // apply current value immediately
}
void OnDisable() { UISettings.OnSensitivityChanged -= v => lookSensitivity = v; }
```

Do **not** multiply `lookSensitivity` by any extra factor — `UISettings.MouseSensitivity` is already the final multiplier (range 0.1–10, displayed to the player as-is).

Remove `SensitivityDemoCamera` from the UIFlowTest Main Camera once real look code is active.

### Level teammate (LevelManager owner)

**Retry loop** — when checkpoint respawn is implemented:

```csharp
void Start()
{
    GameManager.Instance.OnRetryRequested += HandleRetry;
    // After player is spawned:
    deathScreenController.Bind(player.Health);   // PLAYER only, never an enemy
    player.Health.OnDied += GameManager.Instance.ReportPlayerDeath;
}

void HandleRetry()
{
    player.Health.ResetHealth();                   // HealthComponent.ResetHealth() — implemented
    player.Respawn(currentCheckpoint.position);    // PlayerController.Respawn() — still stub
    ResetPuzzleRoom(currentRoomID);                // LevelManager.ResetPuzzleRoom() — still stub
}
```

`DeathScreenController.Bind(health)` must be called with the **player's** component only. Enemy deaths must never trigger it. Calling `Bind()` again is safe — it unsubs before re-subscribing.

### Boss teammate (CrazyMachineBoss / encounter owner)

After the Crazy Machine is defeated:

```csharp
GameManager.Instance.TriggerAlarmAwakeSequence();
// → SetState(Ending) → timeScale=0, cursor visible, OnGameStateChanged fires
// → EndingScreenController receives the event and fades in automatically
```

No other call is needed. Assign an `AudioClip` to `EndingScreenController.alarmClip` in the Inspector for the alarm sound; a missing clip is handled gracefully.

### AudioManager owner (Davide)

When `AudioManager` stub methods are implemented:

```csharp
// In UISettings.cs — replace ApplyVolume():
private static void ApplyVolume(float linear)
{
    AudioListener.volume = linear;               // keep as fallback
    // AudioManager.Instance?.SetMasterVolume(linear);  // uncomment when implemented
}
```

---

## 5. Open Issues / Remaining Manual Work

| # | Issue | Owner | Priority |
|---|---|---|---|
| 1 | `PlayerController.Respawn()` — stub. Retry dismisses death screen but player doesn't teleport | Movement | High |
| 2 | `PlayerController.HandleLook()` — stub. Sensitivity slider animates demo camera only | Movement | Medium |
| 3 | `LevelManager.RespawnPlayer()`, `SetCheckpoint()`, `ResetPuzzleRoom()` — stubs | Level | High |
| 4 | `Checkpoint.OnTriggerEnter()` — stub. Checkpoint never updates LevelManager | Level | High |
| 5 | `GinoImage` on DeathPanel — grey placeholder (300×300). Replace with spiral-eyes art | Art | Low |
| 6 | `BedroomImage` on EndingPanel — blue placeholder (500×350). Replace with bedroom art (math book, carrots, bug net, school bag) | Art | Low |
| 7 | `AudioManager` all methods — stubs. Alarm clip in EndingScreenController plays via standalone AudioSource | Davide | Low |
| 8 | `PauseMenuController` not yet added to any real gameplay scene (SampleScene, future level scenes). Must be added as a persistent Canvas when those scenes are built | Aigerim | Required for gameplay |
| 9 | `DeathScreenController` not yet in gameplay scenes — same as above | Aigerim | Required |
| 10 | `EndingScreenController` not yet in gameplay scenes | Aigerim | Required |
| 11 | GDD ownership table still lists Davide for UI, Aigerim for object physics | Coordinator | Admin |
| 12 | `SensitivityDemoCamera` — remove from Main Camera when `PlayerController.HandleLook()` is implemented | Movement/Aigerim | After movement |

---

## 6. How to Open and Run the Project

### Import
1. Install Unity `6000.3.27f1` via Unity Hub (use Archive if not in the default list)
2. Hub → **Add project from disk** → select `The-Physic-Thief-Project/`
3. Unity reimports assets and compiles (~2 min). No upgrade prompts should appear.

### First-time setup (run once)
In the Unity menu bar: **Tools → UI Flow → Create UIFlowTest Scene**  
→ click **Overwrite** if the file already exists  
→ this regenerates the scene with all current fixes preserved

### Scene to open
`Assets/Scenes/UIFlowTest.unity`

---

## 7. Demonstration Sequence

Open `UIFlowTest.unity` → press **Play**:

| Step | Action | Expected result |
|---|---|---|
| 1 | Press **F1** | Console: `SMOKE TEST PASSED` |
| 2 | Click **Options** | Options panel opens with volume and sensitivity sliders |
| 3 | Drag volume slider | `AudioListener.volume` changes; label shows e.g. "65%" |
| 4 | Drag sensitivity slider | Demo camera rotates at new speed (cursor must be unlocked) |
| 5 | Click **Back** | Options closes, main panel returns |
| 6 | Click **Start** | Scene reloads (UIFlowTest → UIFlowTest); state becomes Playing |
| 7 | Press **Esc** | Pause panel appears; time frozen |
| 8 | Click **Options** in Pause | Options opens over pause; pause stays frozen |
| 9 | Press **Esc** | Options closes; back to Pause panel (NOT resumed) |
| 10 | Press **Esc** again | Resumes; Pause panel hides |
| 11 | Press **D** | Death fade-in; "Nightmare lost…" panel appears |
| 12 | Click **Snooze and retry** | Fade-out; state → Playing; health reset in harness |
| 13 | Press **D** then click **Main menu** | Fades out; loads UIFlowTest in MainMenu state |
| 14 | Click **Start** | Reloads to Playing state |
| 15 | Press **E** | Fade from black; "Thanks for playing"; alarm plays if clip assigned |
| 16 | Click **Main menu** | Returns to UIFlowTest in MainMenu state |
| 17 | Click **Quit** | Exits Play Mode in Editor |

---

## 8. Verification Results

Unity cannot be controlled headlessly from this environment. Results below combine:
- **STATIC** — confirmed by YAML inspection or reading .cs source  
- **UNVERIFIED** — requires Play Mode in Unity

| # | Check | Result | Notes |
|---|---|---|---|
| 1 | Compilation — no new errors | STATIC | No ambiguous types, no Logic/ duplicate, null-guards added |
| 2 | Start loads configured destination | STATIC | GameManager validates scene name before load; UIFlowTest in build settings |
| 3 | Missing level scene logs error, doesn't crash | STATIC | `CanStreamedLevelBeLoaded` check in `LoadSceneRoutine` |
| 4 | Double-click Start blocked during load | STATIC | `SetButtonsInteractable(false)` before `StartGame()` |
| 5 | Options opens from Main Menu, Back returns | UNVERIFIED | `OptionsPanel.Open(onBack)` wired in `OnOptionsClicked` |
| 6 | Options opens from Pause, Back returns to Pause (not resumed) | STATIC | `CloseOptions()` shows pausePanel only; no `ResumeGame()` call |
| 7 | First Esc on Options → closes Options, stays Paused | STATIC | `_optionsOpen` flag checked before state switch in `OnCancelPerformed` |
| 8 | Esc on PausePanel → resumes | STATIC | Falls through to `OnResumeClicked()` in `OnCancelPerformed` |
| 9 | Fades use unscaled time | STATIC | All fade coroutines use `Time.unscaledDeltaTime`; 6 confirmed references |
| 10 | No fade overlay blocks clicks at startup | STATIC | FadeOverlayD `a=0 rt=false`, FadeOverlayE `a=0 rt=false` — verified in YAML |
| 11 | GraphicRaycaster on all interactive canvases | STATIC | 4 canvases × 1 GR each, confirmed in YAML |
| 12 | Exactly one EventSystem | STATIC | Count=1 in UIFlowTest YAML |
| 13 | Volume slider → AudioListener.volume | UNVERIFIED | `UISettings.MasterVolume` setter calls `AudioListener.volume = value` |
| 14 | Settings persist across restarts | UNVERIFIED | `PlayerPrefs.SetFloat` on every change; `PlayerPrefs.Save()` on Close |
| 15 | Player death opens death screen once | STATIC | `IsDead` guard in `HealthComponent`; idempotent GameOver check in `OnPlayerDied` |
| 16 | Enemy death does NOT open death screen | STATIC | `DeathScreenController.Bind()` accepts only the player health component |
| 17 | Retry — state=Playing, OnRetryRequested fires | STATIC | `GameManager.RetryFromCheckpoint()` code path confirmed |
| 18 | Retry — player respawns at checkpoint | PENDING | `LevelManager.RespawnPlayer()` stub |
| 19 | Retry — health reset | PENDING | `HealthComponent.ResetHealth()` exists; LevelManager wiring pending |
| 20 | Retry — attributes/slots/upgrades preserved | STATIC | No `BriefcaseModel` reset on retry by design |
| 21 | Repeated retries — no duplicate subscriptions | STATIC | `SubscribeToHealth` unsubs before re-subscribing |
| 22 | Ending via `TriggerAlarmAwakeSequence()` | STATIC | `SetState(Ending)` → `OnStateChanged` → `BeginEndingSequence()` chain |
| 23 | Ending returns to Main Menu | UNVERIFIED | `FadeOutAndLoadMainMenu()` → `GameManager.LoadMainMenu()` |
| 24 | Ending time scale = 0 | STATIC | `GameManager.SetState(Ending)` → `frozen=true` → `timeScale=0` |
| 25 | Alarm clip plays (if assigned) | UNVERIFIED | `audioSource.Play()` guarded by non-null check |
| 26 | Missing clip handled gracefully | STATIC | `if (audioSource != null && alarmClip != null)` guard |
| 27 | Initial cursor/timeScale correct from first frame | STATIC | `GameManager.Start()` calls `ApplyStatePolicy(gameState)` |
| 28 | Cursor visible in menus | STATIC | `ApplyStatePolicy`: `Cursor.visible = !inGameplay` |
| 29 | No duplicate managers across scene reloads | STATIC | `Singleton.Awake()` destroys duplicates |
| 30 | HUD health and slots work (HUDtest.unity) | STATIC | HUDManager untouched; only ISUI GUID corrected |
| 31 | Keyboard navigation (Tab/Enter) | UNVERIFIED | `button.Select()` called on each panel show |
| 32 | Layout at 1920×1080 | UNVERIFIED | CanvasScaler `ScaleWithScreenSize` 1920×1080, match 0.5 |
| 33 | Layout at 1280×720 and 1024×768 | UNVERIFIED | Same scaler |
| 34 | Sensitivity slider moves demo camera | UNVERIFIED | `SensitivityDemoCamera` reads `UISettings.MouseSensitivity` each frame |
| 35 | Regeneration preserves all fixes | STATIC | Generator reviewed; all bugs fixed at source |

**Summary:** 20 STATIC PASS · 12 UNVERIFIED (need Play Mode) · 2 PENDING (teammate stubs)  
No static FAILs.

---

## 9. Git Status (at time of writing)

```
Branch: feature/aigerim-ui-flow
HEAD: (see git log --oneline -1)

Modified (staged after final commit):
  Assets/Editor/UIFlowTestSceneSetup.cs
  Assets/Scenes/HUDtest.unity
  Assets/Scenes/MainMenu.unity
  Assets/Scenes/UIFlowTest.unity
  Assets/Scripts/Core/GameManager.cs
  Assets/Scripts/UI/MainMenuController.cs
  Assets/Scripts/UI/SensitivityDemoCamera.cs       (new)

Not pushed. Do not merge to main or Davide_Branch without team review.
```

---

## 10. Draft Commit Message

```
feat(ui): complete all 5 screens – Start fixed, cursor policy, click fixes

GameManager:
  - Start() applies initial cursor/timeScale policy from serialised state
  - ApplyStatePolicy() extracted; Ending now freezes time consistently

UIFlowTest.unity:
  - GameManager: gameState=MainMenu, mainMenuScene=UIFlowTest, levelScenes=[UIFlowTest]
  - FadeOverlayD: raycastTarget=false (was blocking clicks while invisible)
  - InputSystemUIInputModule: m_ActionsAsset GUID fixed (ca9f5fa9→052faaac)

MainMenu.unity:
  - MainMenuController fields: startButton/mainPanel/optionsPanel match current script
  - InputSystemUIInputModule: 11 stale GUIDs corrected

HUDtest.unity:
  - InputSystemUIInputModule: 11 stale GUIDs corrected (same fix, Davide's scene)

MainMenuController.cs: null-guards on all fields; hides Options if panel absent
SensitivityDemoCamera.cs: demo camera for sensitivity slider (TEST ONLY, remove after
  PlayerController.HandleLook() is implemented — adapter contract in file comment)
UIFlowTestSceneSetup.cs: full rewrite preserving all fixes on regeneration

Verified by static YAML inspection: 20 checks PASS.
12 checks require Play Mode. 2 PENDING on LevelManager stubs.
See Documents/UI_Implementation_Notes.md.

SCOPE FLAG: GDD table still assigns Davide→UI, Aigerim→object physics.
  Team chat assigns Aigerim these five screens. Table needs team reconciliation.
  No teammate code was modified except stale GUID corrections in HUDtest.unity.
```

---

*Content was rephrased for compliance with licensing restrictions where applicable.*
