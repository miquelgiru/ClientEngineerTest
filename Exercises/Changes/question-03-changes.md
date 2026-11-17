# Question 3 — Changes

> The game designer would like to create different game modes. These modes would require a different configuration of game values like player health and starting position. Implement the necessary changes to allow the game designer to be independent of a developer, add and run new game mode configurations. It is expected the designer cab be working inside unity but will not write any code.

## Summary
- **Game modes are ScriptableObject assets** (`GameMode`). The designer creates them from the Project window (**Create → Platformer → Game Mode**) and edits them in the Inspector. Every field has a tooltip.
- A game mode configures the player:

  | Group | Values |
  |---|---|
  | Start | Override Start Position (toggle), Start Position |
  | Health | Max HP |
  | Movement | Max Speed, Jump Take Off Speed, Gravity Modifier |
  | Knockback | Knockback Speed, Knockback Lift, Knockback Max Duration |
  | Hurt Immunity | Hurt Immunity Duration, Immunity Blink Frequency |
- **To run a mode**, select **GameController** in the Hierarchy, drag the asset into **Model → Game Mode**, and press Play.
- **Live tuning:** while playing in the Editor, edits to the active asset apply immediately. Max HP and start position apply at the next level start.
- **Three ready-made modes** are in `Assets/GameModes/`:

  | Mode | Max HP | Immunity | Start |
  |---|---|---|---|
  | **Default** | 3 | 1 s | Scene SpawnPoint |
  | **Hardcore** | 1 | 0.5 s | Scene SpawnPoint |
  | **Practice** | 10 | 2 s | (73.3, 3.6), mid-level, also used for respawns |

  All three share the same movement and knockback values: speed 3, jump 7, gravity 1, knockback 3 / 5 / 0.75. Default reproduces exactly how the game played before Q3.

## Design
The solution follows the template's architecture:
- `PlatformerModel`, which the template describes as holding "values … which modify gameplay" and shows in GameController's Inspector, now references the active mode.
- `GameController`, the scene's composition root, applies the mode when the level starts.
- `PlayerController` configures itself and its `Health`.

```
GameController ▸ Model ▸ Game Mode = [GameMode asset]      ◄ designer's choice
Start():  player.ApplyGameMode(mode)  →  ApplyGameModeTuning(mode)   (speed, jump, gravity, knockback, immunity)
                                       →  health.SetMaxHP(mode.MaxHP) (max HP + refill)
          if mode.OverrideStartPosition → move SpawnPoint there + Teleport player
                                           (PlayerSpawn already respawns at the SpawnPoint)
Editor, while playing:  mode.Changed (OnValidate)  →  player.ApplyGameModeTuning(mode)
```

**Decisions:**
- **`Health` stays generic.** Enemies use `Health` too, and will need their own Max HP in Q4. So `PlayerController` configures its own `Health` through the new `Health.SetMaxHP()`, and enemy Health is unaffected.
- **Tuning values stay runtime fields on `PlayerController`, filled in from the mode.** The Mod Assets `PlatformerSpeedPad` writes `player.maxSpeed` at runtime. So `maxSpeed` and `jumpTakeOffSpeed` remain public fields, keeping that API working, but are hidden from the Inspector. The game mode is now the only place a designer edits them.
- **The asset is read-only to game code.** `GameMode` fields are `[SerializeField] private` with read-only properties. Runtime code can't change the asset by accident, which would otherwise persist in the Editor.
- **Live tuning.** `GameMode.OnValidate()` raises a `Changed` event. Unity calls `OnValidate` only in the Editor when the asset is edited. `GameController` subscribes in `OnEnable` and unsubscribes in `OnDisable`, because the asset outlives Play mode.
- **Safe fallback.** If no mode is assigned, `GameController` logs a clear error and the player keeps its built-in defaults, so the game still runs.

## Files changed

### Scripts
| File | Change |
|---|---|
| [GameMode.cs](../../Assets/Scripts/Model/GameMode.cs) **(new)** | The `ScriptableObject`. `[CreateAssetMenu]` puts it under "Platformer/Game Mode". Fields are grouped by `[Header]`, with `[Tooltip]` and `[Min]`, and exposed through read-only properties. Has the `Changed` event raised from `OnValidate`. |
| [PlatformerModel.cs](../../Assets/Scripts/Model/PlatformerModel.cs) | New `public GameMode gameMode;`, the active mode, shown under GameController → Model. |
| [GameController.cs](../../Assets/Scripts/Mechanics/GameController.cs) | `Start()` → `ApplyGameMode()`. That applies the mode to the player and, if the start position is overridden, moves the SpawnPoint (keeping z) and teleports the player. `OnEnable` and `OnDisable` subscribe and unsubscribe `GameMode.Changed` for live tuning. Logs an error if no mode is assigned. |
| [PlayerController.cs](../../Assets/Scripts/Mechanics/PlayerController.cs) | New `ApplyGameMode(mode)`, which applies the tuning and calls `health.SetMaxHP`, and `ApplyGameModeTuning(mode)`, which sets speed, jump, gravity, knockback and immunity. The knockback and immunity fields are no longer serialized; they're runtime values from the mode. `maxSpeed` and `jumpTakeOffSpeed` are `[HideInInspector]`, still public. |
| [Health.cs](../../Assets/Scripts/Mechanics/Health.cs) | New `SetMaxHP(int)`: sets max HP (at least 1) and refills. |

### Assets and scene (created through the Unity API, not by editing YAML)
| Asset | Change |
|---|---|
| `Assets/GameModes/Default.asset`, `Hardcore.asset`, `Practice.asset` **(new)** | The three modes above. |
| [SampleScene.unity](../../Assets/Scenes/SampleScene.unity) | One line: GameController → Model → Game Mode = **Default**. |

**Practice start position.** I found it in the Editor by raycasting down from the collectible tokens, which lie along the level's path. (73.3, 3.6) is just above the ground (y = 3.0), about 10 units from the nearest enemy, with free space above.

**Leftover serialized values.** The Player's old serialized values for the moved fields are now ignored and do nothing; they're just ignored data left in the prefab and scene. This includes the scene override Knockback Lift = 5, which now lives in the Default mode.

## For the designer
1. **Create a mode:** Project window → right-click → **Create → Platformer → Game Mode**, or select `Default` and press Ctrl+D to duplicate it. Edit the values in the Inspector; hover a field to see its tooltip.
2. **Choose a start position (optional):** tick **Override Start Position** and type the world coordinates. A simple way to get them is to move any object in the scene where you want the player to start and copy its X/Y.
3. **Run it:** select **GameController** in the Hierarchy, drag your asset into **Model → Game Mode**, and press Play.
4. **Tune while playing:** edit the active asset during Play. Speed, jump, gravity, knockback and immunity update immediately and are **kept after you stop**, because it's an asset. Max HP and start position apply the next time you press Play.

## How to verify (Play mode)
1. **Default** (already assigned): the game plays exactly as before. You have 3 HP, the same speed and jump, and the same knockback.
2. **Hardcore:** one enemy hit kills the player.
3. **Practice:** the player starts mid-level at about x = 73 with 10 HP and 2 s of immunity. After dying, it **respawns at the same mid-level position**.
4. **Live tuning:** play with Default and set Max Speed to 6 on the asset; the player speeds up immediately. Change Max HP; nothing changes until the next Play. Afterwards, set Max Speed back to 3, since asset edits persist.
5. **Empty slot:** clear Game Mode and press Play. A clear error appears in the console, and the game still runs with the player's default settings.
6. **New mode:** create one through the Create menu, assign it, and press Play.

## Additional fix found while testing: enemies and tokens come back when the player respawns
**Problem.** After the player died and respawned, enemies killed during the previous life stayed dead. This is the original template's behaviour, not a regression:
- `EnemyDeath` disables the enemy's collider and its movement component (`AnimationController`). Disabling the movement component turns the body dynamic, so the dead enemy falls out of the level.
- `PlayerSpawn` only resets the player and the camera.

**Decision (mine):** reset **in place**, keeping the template's respawn sequence (death animation, 2 s wait, respawn sound, input lock). At first only enemies were reset. After that, I asked for **collected tokens to respawn too**.

**Fix.** In [EnemyController.cs](../../Assets/Scripts/Mechanics/EnemyController.cs):
- Each enemy stores its start position in `Awake`.
- It subscribes to the template's built-in hook **`PlayerSpawn.OnExecute`** in `OnEnable` and unsubscribes in `OnDisable`. No new events, registries or singletons are needed.
- On respawn it calls the new public **`ResetToStart()`**:
  - Re-enables the collider and the movement component, which makes the body kinematic again; this happens before the teleport.
  - Teleports the enemy to its start position.
  - Restarts its patrol (`mover = null`, so it is recreated exactly like at level start).
  - Refills its `Health` if it has one. Enemies have none yet; Q4 adds it.
- **Every** enemy resets on respawn, alive or dead, so the level is back to its starting enemy layout.

**Tokens:**
- When a token is collected, [TokenController.cs](../../Assets/Scripts/Mechanics/TokenController.cs) plays its "collected" animation, then deactivates it and removes it from its animation list (`tokens[i] = null`).
- `TokenController` now keeps a copy of every token registered at level start (`allTokens`). It subscribes to the same `PlayerSpawn.OnExecute` hook, and on respawn `ResetAllTokens()` puts every token back in the list and calls the new **`TokenInstance.ResetToIdle()`** ([TokenInstance.cs](../../Assets/Scripts/Mechanics/TokenInstance.cs)).
- `ResetToIdle()` clears the collected flag, restarts the idle animation and reactivates the token.

**Verify:**
1. Stomp a few enemies and collect some tokens.
2. Die (enemy hits or a pit).
3. After the respawn, the stomped enemies are back at their original positions and patrolling, and the collected tokens are back and spinning.
4. Enemies can be stomped again and damage you again. Tokens can be collected again, with sound.

## Review fixes
After a final error review, I chose to fix two small issues:

1. **Camera at level start with a custom start position.**
   - **Issue:** the camera follows the player with damping (X/Y = 1). After the start teleport it could pan across the level.
   - **Cause:** the body's new position only reaches the transform on the next physics step, which may come after the camera's first update.
   - **Fix:** `GameController.MovePlayerToStart()` sets the player's transform as well as its body. It then sets `virtualCamera.PreviousStateIsValid = false` (Cinemachine 2.10 API), so the camera snaps to the start position.
2. **Killed enemies falling forever.**
   - **Issue:** `EnemyDeath` turns the body dynamic with no collider, so it kept falling and being simulated until the next respawn.
   - **Fix:** `EnemyController.UpdateDeathFall()` runs once the enemy is 20 units below its start position (`DeathFallDistance`), well out of view. It disables the `Rigidbody2D` simulation and hides the sprite.
   - **Restore:** `ResetToStart()` turns both back on before re-enabling the enemy.
   - **Why not deactivate the GameObject:** that would also unsubscribe the enemy from the respawn hook.

## Known limitations
- **Level-start values:** Max HP and start position apply only at level start, not during live tuning.
- **No swapping during Play:** swapping the asset in the Game Mode slot during Play isn't picked up. Swap between Play sessions.
- **Model-level jump values:** `jumpModifier` and `jumpDeceleration` on GameController → Model are global and not part of a game mode. They weren't requested, and each would be a one-field addition.
- **Mid-level start isn't a checkpoint:** a start position mid-level only changes where the player appears. Tokens and enemies before that point are still in the level.- **No automated tests:** the project has no assembly definitions, as decided at setup. Changes were checked by a clean compile and console, read-backs of all asset values and the scene assignment, a one-line scene diff, and the manual Play-mode steps above.
