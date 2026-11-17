# Question 2 — Changes

> Completing question 1 may cause an unintended issue for the designer, once the player collides they will likely collide again immediately. Give the player 1 second of immunity after a collision with an enemy.

## Summary
- After an enemy hurts the player, the player is **immune to enemy damage for 1 second**. The duration is configurable on the Player as **Hurt Immunity Duration**.
- **Feedback:** the normal Hurt animation plays. Once it has finished, the player sprite **blinks until immunity ends**. The blink speed is configurable as **Immunity Blink Frequency**.
- **While immune, enemies don't collide with the player.** They pass through each other: no blocking, no damage, and no stomping. Death zones still kill.
- **The Player animator is not modified.**

## Why the player was hit twice
Both the player and each enemy have a single collider, so the repeat hit isn't caused by extra colliders. It happens because the contact ends and starts again:
- The Q1 knockback hop separates the player from the enemy.
- The patrolling enemy (about 3.5 units/s) catches up again.
- Unity fires a new `OnCollisionEnter2D`, which schedules a new `PlayerEnemyCollision` event, sometimes in the **same frame** as the first one.

## Design
- **Immunity is a timestamp on the player:** `immuneUntil = Time.time + hurtImmunityDuration`.
  - It uses the same clock as the template's `Simulation`, so it also pauses with the main menu (`Time.timeScale = 0`).
- **It starts at the moment damage is applied**, inside `PlayerEnemyCollision.HurtPlayer()`, and not in the later `PlayerHurt` event.
  - The template's event queue is a heap, not first-in-first-out, so a second collision event in the same frame can run before `PlayerHurt`.
  - Starting immunity synchronously guarantees that second event already sees the player as immune.
- **Death clears immunity**, as it already cancels the knockback, so nothing carries over into the respawn.
- **The blink waits for the Hurt animation.**
  - The `hurt` trigger is processed by the Animator after the frame's `Update`, so `PlayerController` first waits for the `Player-Hurt` state to start, then for it to end.
  - Only then does it toggle `SpriteRenderer.enabled` until immunity is over.
  - The sprite is always made visible again when the player is not immune, for example after death.
- **Enemies pass through the immune player.**
  - Each `EnemyController` calls `Physics2D.IgnoreCollision(enemyCollider, playerCollider, true)` while the player is immune. This stops contacts, so there are no `OnCollisionEnter2D` events and no damage or stomps.
  - The template moves characters with `Rigidbody2D.Cast` in `KinematicObject`, and **`Rigidbody2D.Cast` does not respect `IgnoreCollision`**. This was verified in the Editor: the cast still reported the ignored collider. So `KinematicObject` now skips cast hits for pairs that `Physics2D.GetIgnoreCollision` reports as ignored. This applies in both directions: the player moves through enemies and enemies move through the player.
  - **The pair stays ignored until they stop overlapping**, checked with `Collider2D.Distance(...).isOverlapped`. Casts that start inside a collider report it immediately (`queriesStartInColliders` is on), so re-enabling collision while overlapping would leave both characters stuck inside each other. After separating, the next touch hurts again.

```
Enemy side hit ─► PlayerEnemyCollision.HurtPlayer()
                    ├─ player.IsImmune ─────────► ignored (no damage)
                    ├─ Decrement → HP == 0 ─────► HealthIsZero → PlayerDeath (clears immunity)
                    └─ survived ─► StartHurtImmunity() ─► PlayerHurt (sound, "hurt" trigger, knockback)

While immune:  [Player-Hurt plays] ──(state ends)──► sprite blinks ──(immunity ends)──► sprite visible
```

## Files changed
| File | Change |
|---|---|
| [PlayerController.cs](../../Assets/Scripts/Mechanics/PlayerController.cs) | New **Hurt Immunity** header with two fields. `hurtImmunityDuration = 1f` (`[Min(0)]`) is how long the player ignores enemy damage. `immunityBlinkFrequency = 10f` (`[Min(0)]`) is blinks per second, and 0 disables blinking. Adds `IsImmune` (`Time.time < immuneUntil`), `StartHurtImmunity()` and `ClearHurtImmunity()`. Adds `UpdateImmunityBlink()`, called from `Update()`: it waits for the `Player-Hurt` state (cached `Animator.StringToHash`) to start, then to end, then blinks the sprite until immunity ends. |
| [PlayerEnemyCollision.cs](../../Assets/Scripts/Gameplay/PlayerEnemyCollision.cs) | `HurtPlayer()` does nothing while `player.IsImmune`. This is the safety net for collision events already queued in the same frame. After a hit the player survives, it calls `player.StartHurtImmunity()` before scheduling `PlayerHurt`. The stomp branch is unchanged. |
| [EnemyController.cs](../../Assets/Scripts/Mechanics/EnemyController.cs) | New `UpdatePlayerCollision()`, called at the start of `Update()`. While `model.player.IsImmune`, or while still overlapping after immunity, it ignores collisions with the player via `Physics2D.IgnoreCollision`. It calls Unity only when that state changes, and skips dead enemies, whose collider `EnemyDeath` has disabled. Reads the player from `PlatformerModel`, like the Gameplay events do. |
| [KinematicObject.cs](../../Assets/Scripts/Mechanics/KinematicObject.cs) | Caches its own `Collider2D` in `OnEnable`. `PerformMovement` skips cast hits whose pair is ignored (`Physics2D.GetIgnoreCollision`), because `Rigidbody2D.Cast` doesn't respect `IgnoreCollision`. With no ignored pairs, movement behaves exactly as before. |
| [PlayerDeath.cs](../../Assets/Scripts/Gameplay/PlayerDeath.cs) | Calls `player.ClearHurtImmunity()` next to `CancelKnockback()`. |

### Scene
| Asset | Change |
|---|---|
| [SampleScene.unity](../../Assets/Scenes/SampleScene.unity) | Saved in the Editor during Play testing. Two overrides on the scene's Player (Player Controller): **Knockback Lift = 5** (code/prefab default 3) is tuning chosen after testing, giving a higher knockback arc with about 1 s of airtime. **Hurt Immunity Duration = 1** has the same value as the default. The rest of the diff is Unity 6 re-serialization of the original 2020-format scene, including an embedded lighting settings object, and doesn't change behaviour. |

No prefabs or animator controllers changed. The new fields on the Player prefab take their defaults from the script: Hurt Immunity Duration 1 and Immunity Blink Frequency 10.

`ProjectSettings/SceneTemplateSettings.json` was generated by Unity when the scene was saved. It isn't related to gameplay.

## Approach that was tried and reverted
A first version made the Hurt animation **loop for the whole immunity**:
- `PlayerHurt.anim` set to Loop Time.
- A new `immune` animator Bool.
- Player-Hurt → Idle waiting for `immune == false`.

After Play testing, the Hurt animation didn't fit as an immunity indicator. Those animator changes were reverted, and the asset files are identical to the Question 1 commit. They were replaced with the sprite blink described above.

## For the designer
Select the Player and use **Player Controller → Hurt Immunity**:
- **Hurt Immunity Duration**: seconds of immunity after a hit (default 1).
- **Immunity Blink Frequency**: blinks per second after the Hurt animation (default 10, 0 = no blink).

## How to verify (Play mode)
1. Get hit by an enemy: the Hurt animation plays once. The sprite blinks from when it ends until immunity ends, then becomes solid again.
2. Walk into an enemy during immunity: you pass through it, and it passes through you, with no damage. Watch `currentHP` on the Player with the Inspector in Debug mode.
3. Jump onto an enemy during immunity: you fall through it, with no stomp.
4. Stay inside an enemy when immunity ends: you are not stuck and can walk out. Once separated, touching it again hurts.
5. After immunity, a new contact damages and blocks normally again.
6. Third hit: Hurt, then Death, then the normal respawn with full HP. The sprite is visible after the respawn.
7. Fall into a pit during immunity: the player still dies and respawns normally.
8. Set Hurt Immunity Duration to 2: about 1.1 s of blinking after the Hurt animation, and about 2 s of passing through enemies.
9. Stomp an enemy, then get hurt by another one: no errors in the console. The dead enemy is skipped.

## Known limitations
- **Short blink at the defaults:** the Hurt clip lasts 0.92 s and immunity defaults to 1 s, so the blink only shows for about 0.08 s, a single flicker. It becomes clearly visible with a longer Hurt Immunity Duration.
- **Overlap at the end of immunity:** an enemy the player is still overlapping when immunity ends doesn't hurt or block them until they separate. This is intentional, to avoid getting stuck.
- **No stomping while immune:** this follows from "enemies should not collide with the player".
- **Per-enemy polling:** each enemy checks the player's immunity every frame. That's a bool comparison for 16 enemies, with Unity only called when the state changes.
- **Hard-coded state name:** the blink relies on the animator state name `Player-Hurt`. Renaming that state disables the blink, but immunity is unaffected.
- **No automated tests:** the project has no assembly definitions, as decided at setup. Changes were checked by a clean compile, a clean console, read-backs of the serialized fields and the restored animator, and the manual Play-mode steps above.
