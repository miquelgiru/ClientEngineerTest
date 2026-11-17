# CLAUDE.md — Unity Test Project

## Context
- This is a timed Unity test (~4h, 4 exercises) for a **Unity Developer + Real User Monitoring (RUM)** role.
- Correctness and a clean, working project beat cleverness.
- Unity version: **6000.3.25f1** (Unity 6.3). Do NOT upgrade packages or the editor version.
- IDE: VS Code. Unity Editor stays open; you control it through the Unity MCP tools.
- The 4 exercises are done **in this same project and likely build on each other**. Every exercise must leave the project compiling and the previous exercises still working.
- Statements: the original is `README.md` (repo root). Split copies live in `/Exercises/`: `00-overview.md` (context, rules, deliverables, evaluation, appendices) and `question-01.md` … `question-04.md`. Treat them as read-only. Re-read the statement before marking an exercise done.

<!-- Fill these in during the first 10 minutes of the test -->
## Project facts (fill in at start)
- Project: Unity's **2D Platformer Microgame** (v2.1.0). The test questions are gameplay features — **no RUM/telemetry is requested**.
- Render pipeline: **Built-in** (no SRP asset) + Post Processing v2. Camera: **Cinemachine 2.10.7** (`CinemachineVirtualCamera.m_Follow/m_LookAt`).
- Input handling: **Legacy Input Manager only** (`activeInputHandler: 0`, Input System package NOT installed). Use `Input.GetAxis` / `Input.GetButtonDown` like `PlayerController`. Axes: Horizontal, Vertical, Jump, Fire1-3, Submit, Cancel, Menu. (README: the InputSystem warning is harmless.)
- UI: **uGUI** (TMP imported in `Assets/TextMesh Pro`). `UI Canvas` prefab = `MainUIController` with panels Main/Settings/Controls/Credits; `MetaGameController` toggles it with "Menu" and sets `Time.timeScale = 0`.
- Main scenes: `Assets/Scenes/SampleScene.unity` (only scene in build). Contains GameController object (GameController + TokenController + MetaGameController), 1 Player prefab instance, 16 Enemy prefab instances (+ PatrolPaths), 100 Tokens, 4 DeathZones, 1 VictoryZone, SpawnPoint (Transform), CM vcam1, UI Canvas.
- Physics: 2D. Player and enemies are kinematic `KinematicObject`s (custom `Rigidbody2D.Cast` movement), both on unnamed layer 13 (Enemy prefab is tagged "Player"). 2D collision matrix fully enabled.
- Assemblies: **none** — all code in Assembly-CSharp / Assembly-CSharp-Editor. **Decision: do not add asmdefs → no automated tests.** Workflow step 5 means: clean compile + console check + manual Play-mode checklist for the user.
- Animator params — Player: `hurt`, `dead`, `victory`, `grounded`, `velocityX/Y`. Enemy: `hurt`, `death`, `grounded`, `velocityX/Y`.
- Existing code conventions observed (README: "use the style or approach this solution uses" → **project style wins over the generic patterns below**):
  - Namespaces `Platformer.{Core,Mechanics,Gameplay,Model,UI,View}` (use these, not `TestProject.*`).
  - `Platformer.Core.Simulation` = **static facade**: discrete-event scheduler (`Schedule<T>(delay)`, pooled `Simulation.Event<T>`, static `OnExecute` hooks) + model registry (`Simulation.GetModel<PlatformerModel>()`). `GameController` ticks it and exposes the shared `PlatformerModel` in the Inspector.
  - **Mechanics** MonoBehaviours detect collisions/input and `Schedule<...>()` events; **Gameplay** event classes hold the rules (modify model, post events); **Model** is data only; **View** is visual only.
  - Public fields + XML-doc comments, `GetComponent` in `Awake`. New code: `[SerializeField] private` + read-only properties, keep existing public APIs.
  - No DI container / MVP / SO event channels — not needed for these questions. `GameController` is the composition root.
- Latent bugs the exercises will hit (fix as part of Q1 unless noted):
  1. `HealthIsZero` **always** schedules `PlayerDeath`, whichever entity's Health hit 0 (breaks Q4 enemy health).
  2. `PlayerDeath` only acts `if (player.health.IsAlive)` → a death caused by damage reaching 0 does nothing (no anim, no respawn).
  3. `PlayerSpawn` restores only +1 HP (`Increment()`).
  4. `Health` sets `currentHP = maxHP` only in `Awake` → runtime maxHP changes (Q3) have no effect.
  5. Enemy prefab has **no Health** (stomp = instant kill); Player prefab `maxHP: 1`.
  6. `SpawnPoint` component exists but is unused; `model.spawnPoint` is a plain Transform.

<!-- Fill this in after the kickoff analysis; keep it updated -->
## Exercise map
| # | Goal (one line) | Depends on | Systems/files touched | Status |
|---|-----------------|------------|-----------------------|--------|
| 1 | Player uses Health; enemy contact deals per-enemy X damage | — | `Health` (`Decrement(int)`, `CurrentHP`, `ResetToMax`), `EnemyController` (`contactDamage`/`ContactDamage`), `PlayerEnemyCollision` (Precondition + `HurtPlayer`), `HealthIsZero` (player only → `PlayerDeath`), `PlayerDeath` (Precondition !IsAlive), `PlayerEnteredDeathZone` (`health.Die()`), `PlayerSpawn` (`ResetToMax`), new `PlayerHurt` event (→ `PlayerController.Knockback()`, `FacingDirection`), Player prefab maxHP 3 | done (awaiting Play check) |
| 2 | 1 s player immunity after an enemy hit | 1 | `PlayerController` (`hurtImmunityDuration` = 1, `immunityBlinkFrequency` = 10, `IsImmune`, `StartHurtImmunity`, `ClearHurtImmunity`, `UpdateImmunityBlink`), `PlayerEnemyCollision.HurtPlayer` (immunity check + start), `PlayerDeath` (clear), `EnemyController.UpdatePlayerCollision` (`Physics2D.IgnoreCollision` with the player while immune, kept until separated), `KinematicObject` (movement cast skips pairs ignored via `Physics2D.GetIgnoreCollision`). Animator unchanged (restored to Q1). | done (awaiting Play check) |
| 3 | Designer-made game modes (player HP, start position, …) created and run without code | 1 (maxHP), 2 (immunity duration) | new `GameMode` SO (`Assets/Scripts/Model/GameMode.cs`, Create > Platformer > Game Mode), `PlatformerModel.gameMode` (slot in GameController > Model), `GameController` (applies at Start + Editor live tuning via `GameMode.Changed`), `PlayerController.ApplyGameMode/ApplyGameModeTuning`, `Health.SetMaxHP`, assets `Assets/GameModes/{Default,Hardcore,Practice}`, scene: Default assigned | done (awaiting Play check) |
| 4 | Gun: keypress fires a bullet in facing direction; bullet deals player-specific Y damage to enemies | 1 (damage amounts, `HealthIsZero` routing), 3 (gun chosen per mode) | new `Gun` SO (`Assets/Scripts/Model/Gun.cs`, Create > Platformer > Gun), `PlayerGun` (Fire1, `ObjectPool<Bullet>`, gun model child, ammo refill on respawn), `Bullet` (+ `Assets/Prefabs/Bullet.prefab`), `BulletHitEnemy` event, `EnemyController.IsAlive/PlayHurt`, `PlayerEnemyCollision` (stomp = `Die()`), `GameMode.gun`, `PlayerController` (equips mode gun), Enemy prefab Health 3, Player prefab PlayerGun + "Gun" child, `Assets/Weapons/{Pistol.asset, Sprites/}` | done (awaiting Play check) |

Shared systems used by several exercises (design these carefully the first time):
- **Health**: amount-based damage, `SetMax`/`ResetToMax`, death routed by owner (player → `PlayerDeath`, enemy → `EnemyDeath`). Keep existing API (`maxHP`, `IsAlive`, `Increment`, `Decrement`, `Die`).
- **Damage flow through Simulation events**: Mechanics detects → Gameplay event applies rules → Health.
- **`GameController`** = single entry point for configuration (Q3 game mode applied there; Q2 immunity duration and Q4 bullet damage can be overridden by the mode).

Open decisions (recommendations, confirm before each exercise):
- Q1 (decided): X = per-instance `contactDamage` on `EnemyController` (prefab default 1, 0 = harmless); Player maxHP 3; non-lethal hit = hurt anim + ouch sound + **knockback** (`PlayerController.Knockback()`: small gravity arc opposite to the facing direction, **no movement until landing**; `knockbackMaxDuration` (default 3 s) is only a safety limit if the player never lands; `jumpState = InFlight`; animator untouched); death zones kill regardless of HP; the only way the player dies is Health reaching 0.
- Q2 (decided): `hurtImmunityDuration` on the Player (default 1 s), started synchronously in `HurtPlayer` (event queue isn't FIFO); while immune the **player and enemies pass through each other** (no blocking, no contacts → no damage and no stomping; death zones still kill); the pair stays ignored until they stop overlapping (casts start-inside would otherwise get both stuck). Note: `Rigidbody2D.Cast` does NOT respect `Physics2D.IgnoreCollision` (verified in Editor), hence the `KinematicObject` check. Cleared on death. Feedback: **sprite blink after the Hurt animation has finished, until immunity ends** (SpriteRenderer toggled in `PlayerController`; waits for state `Player-Hurt` to start, then to end). The looping-Hurt approach was tried and reverted; **the Player animator is unchanged** from Q1.
- Q3 (decided): `GameMode` SO holds start position (override toggle + Vector2, also used for respawn by moving the SpawnPoint), max HP, movement (maxSpeed, jumpTakeOffSpeed, gravityModifier), knockback and immunity. Active mode = **Inspector slot** GameController > Model > Game Mode. **PlayerController configures Health** (`SetMaxHP`); enemies keep their own maxHP. Player tuning fields are runtime values filled from the mode (`maxSpeed`/`jumpTakeOffSpeed` stay public for the speed pad, hidden in Inspector). **Live tuning** in Editor (tuning only; HP/start at level start). No mode assigned → console error, player keeps defaults.
- Respawn fix (found in Q3 testing, template behaviour): on `PlayerSpawn`, **all enemies reset in place** via `PlayerSpawn.OnExecute` → `EnemyController.ResetToStart()` (collider + movement re-enabled, teleport to start, patrol restarted, Health refilled if present). **Tokens reset too**: `TokenController` keeps `allTokens` and on `PlayerSpawn.OnExecute` calls `ResetAllTokens()` → `TokenInstance.ResetToIdle()`. `GameController.Update()` (`Simulation.Tick()`) must never be removed — it drives every Simulation event. Killed enemies stop simulating + hide 20 units below their start (`UpdateDeathFall`); `ResetToStart` restores them. Start-position override snaps the camera (`PreviousStateIsValid = false`).
- Q4 (decided): `Gun` SO = gun sprite, hold offset (also bullet origin; fires ~0.26 above ground to hit the enemy's 0.45 circle hitbox), bullet sprite/speed/lifetime, unlimited bullets (default on) or limit, fire rate (0 = no limit), damage **Y**. Gun **chosen per Game Mode** (`GameMode.gun`, empty = no gun; `PlayerGun.gun` is the fallback without a mode); default modes use **Pistol**. **Fire1** (Left Ctrl / Mouse 0 / joystick 0); no firing while dead, **during the Hurt animation** (`PlayerController.IsHurtAnimationPlaying`), respawn lock, victory, or menu open (`timeScale` 0). Bullets: `UnityEngine.Pool.ObjectPool<Bullet>` in `PlayerGun`, trigger + dynamic gravity-0 body, released on enemy/level hit or lifetime. Enemies: **Health 3**, bullet hit → `BulletHitEnemy` (hurt anim + ouch, or `EnemyDeath`); **stomp still one-shots** (`Health.Die()`). Limited ammo **refills on respawn**. Placeholder sprites generated (no weapon art in the project).

## Workflow for every exercise
1. **Understand**: read the exercise statement, the Exercise map and the related existing scripts/scenes. Check what **later** exercises will need from this one (events to raise, interfaces to expose, data to keep) so we don't have to rewrite it — but don't implement future exercises. List assumptions and ambiguities; ask me before guessing on anything that changes the design.
2. **Plan**: give a short plan (files to create/modify, components, scene changes, which architecture pattern applies, impact on previous exercises). Wait for my OK on large plans.
3. **Implement** in small steps.
4. **Verify after every script change**:
   - Trigger a refresh/recompile, WAIT for compilation to finish, then read the Unity console.
   - Zero compile errors before continuing. Fix warnings you introduced.
   - If an MCP call fails right after a recompile (domain reload), wait a few seconds and retry once.
5. **Test**: write/run EditMode and PlayMode tests for the new logic, then run **all** existing tests to catch regressions in earlier exercises. Tell me exactly what to check in Play mode (I verify visuals myself).
6. **Report**: summarize what changed (files, scene objects, settings), any known limitations, and update the Exercise map status.
7. Remind me to commit and tag. Do not start the next exercise until I say so.

## Architecture patterns (use when possible)
Apply these where they make the code clearer and more testable. Don't over-engineer a 20-line exercise — if a pattern adds more ceremony than value, say so and keep it simple. Follow existing project architecture if it already has one.

### ScriptableObject patterns
- **Config/data assets**: tunables (speeds, thresholds, sampling rates, endpoints) live in ScriptableObjects, not hardcoded or duplicated across MonoBehaviours. Use `[CreateAssetMenu]`.
- **Event channels**: `VoidEventChannelSO`, `GameEventSO<T>`-style assets to decouple senders from listeners across scenes/prefabs. Listeners subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- **Runtime sets**: SO lists that objects register/unregister into (instead of `FindObjectsByType`).
- **Shared variables** (`FloatVariableSO`, etc.) only where they genuinely simplify wiring.
- Remember: SO state changed in Play mode persists in the Editor — reset runtime state in `OnEnable` or keep runtime state out of the asset.

### Dependency injection
- Depend on **interfaces**, not concrete classes (`IMetricsSink`, `ITimeProvider`, `IScoreService`...).
- **Plain C# services** get dependencies through the **constructor**. MonoBehaviours get them via an `Init(...)`/`Construct(...)` method or `[SerializeField]` references to SOs.
- Use a single **composition root** (e.g. `Bootstrapper`/`GameInstaller` MonoBehaviour in the first scene) that creates services and wires them up.
- If VContainer/Zenject is already in the project, use it. Otherwise use manual (pure) DI — do NOT add a DI package without asking.
- No new singletons / static service locators unless the existing project already relies on them.
- Wrap Unity statics that hurt testability (`Time`, `Application`, `Random`, network) behind small interfaces when the logic needs to be tested.

### MVP / MVC (UI and feature screens)
- **Model**: plain C# class (or SO for data) holding state and rules. No `UnityEngine.UI` references. Raises events on change.
- **View**: MonoBehaviour that only displays and forwards user input (`event Action OnClicked`). No game logic.
- **Presenter/Controller**: plain C# class that listens to the View and Model, updates the Model, and tells the View what to show. Created by the composition root; implements `IDisposable` to unsubscribe.
- Prefer **MVP** with a passive View (`IXxxView` interface) so the Presenter is unit-testable with a fake view.

## Unity rules
- Create scripts, prefabs, materials, ScriptableObjects and scene objects via Unity (MCP tools) so `.meta` files and GUIDs are generated correctly.
- NEVER hand-edit `.meta`, `.unity`, `.prefab` or `.asset` YAML files, and never delete/rename assets outside Unity (breaks references).
- Do not modify `ProjectSettings/`, `Packages/manifest.json` or existing package versions unless the exercise requires it — ask first.
- Do not modify code unrelated to the current exercise. If a change to code from a previous exercise or the original project is needed, say so in the plan first. Keep existing public APIs working.
- Understand the provided project before changing it: reuse its existing systems, managers and conventions instead of building parallel ones.
- Save the scene after scene changes. Mention every scene/prefab you modified.
- Prefer assigning references in the Inspector (`[SerializeField]`) or through the composition root over `Find`/`GetComponent` at runtime. If you can't wire an Inspector reference via MCP, tell me exactly what to drag where.

## Unity 6 API — use the current names
- Rigidbody: `linearVelocity`, `linearDamping`, `angularDamping` (NOT `velocity`, `drag`, `angularDrag`). Same for `Rigidbody2D`.
- `PhysicsMaterial` (NOT `PhysicMaterial`).
- `FindFirstObjectByType<T>()` / `FindAnyObjectByType<T>()` / `FindObjectsByType<T>(FindObjectsSortMode.None)` (NOT `FindObjectOfType` / `FindObjectsOfType`).
- Input: if the project uses the Input System package, use `UnityEngine.InputSystem` (InputActions / `Keyboard.current`), NOT `Input.GetKey` / `Input.GetAxis`.
- TextMeshPro ships inside the uGUI package; use `TMPro.TMP_Text` / `TextMeshProUGUI`, not legacy `UnityEngine.UI.Text`.
- Navigation: use the AI Navigation package (`NavMeshSurface`, `Unity.AI.Navigation`), not the legacy bake window.
- Async: `Awaitable` is available; coroutines are fine too. Don't add third-party async libraries.
- If unsure whether an API exists in this version, say so instead of guessing.

## Code style
- One MonoBehaviour/ScriptableObject per file; file name == class name.
- `[SerializeField] private` fields instead of public fields. Use `[Header]`, `[Tooltip]`, `[Range]` where helpful.
- Cache components in `Awake`. No `GetComponent`, `Find`, LINQ or allocations in `Update`/`FixedUpdate`.
- Physics in `FixedUpdate`, input reading in `Update`.
- Use events (C# `event`/`Action`, SO event channels, or `UnityEvent`) to decouple systems.
- Null-check serialized references with a clear `Debug.LogError` in `Awake`/`OnValidate`.
- Small, readable methods; comments only where intent isn't obvious. Readability matters — a human will grade this.
- Namespace: `TestProject.<Feature>` (e.g. `TestProject.Monitoring`), adapted to existing conventions if they differ.
- Use assembly definitions (`.asmdef`) per feature (e.g. `TestProject.Monitoring`, `TestProject.Monitoring.Tests`) so tests can reference code and compile times stay low.

## Tests
- Test Framework (NUnit): EditMode tests for pure logic, PlayMode tests for behaviour.
- Put tests under `Assets/Tests/EditMode` and `Assets/Tests/PlayMode` with proper `.asmdef` files referencing the code under test.
- Keep logic in plain C# classes (Models, Presenters, services, aggregators) so it's testable without scenes — this is the main reason for the DI/MVP rules above.
- Run the tests through the MCP/Test Runner after writing them and report pass/fail counts.

## Git
- Ignore Git actions

## Communication
- Be concise. Lead with what changed and what I need to check.
- If something will take long or is risky, say so and propose a simpler alternative — time is limited.

##  Evidence
- On asked, create a document with all the conversation between us during the development of the test.
