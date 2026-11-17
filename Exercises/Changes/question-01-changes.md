# Question 1 — Changes

> The game designer would like to enable the use of health on the Player so that the player can survive more than one collision with an enemy. […] Refactor the damage code to use the player health value and to cause X damage on a collision with an enemy that would cause damage, where X is specified on a per enemy basis.

## Summary
- Damage from enemies now goes through the player's existing `Health` component. The player starts with **3 HP**.
- Each enemy has a **Contact Damage** value: X, editable per enemy in the Inspector. It defaults to 1, and 0 makes the enemy harmless.
- A hit the player survives plays the ouch sound and the hurt animation, and knocks the player back in a small arc.
- The player dies only when their health reaches 0. They then respawn with full health.
- Stomping enemies and death zones work as before. A death zone still kills instantly, whatever the HP.

## Design
The solution keeps the template's architecture.
- **Mechanics** MonoBehaviours detect collisions and schedule events.
- **Gameplay** events (`Simulation.Event<T>`) hold the rules.
- Events read the shared `PlatformerModel` through `Simulation.GetModel`.
- The template's `Precondition()` override guards events against stale state.

**Health reaching 0 is now the only way the player can die:**

```
Enemy side hit ─► PlayerEnemyCollision ─► player.health.Decrement(enemy.ContactDamage)
                                            ├─ still alive ─► PlayerHurt (ouch sound + "hurt" anim + knockback)
                                            └─ HP == 0 ───► HealthIsZero ─► PlayerDeath ─► PlayerSpawn (+2 s, full HP)
DeathZone ─► PlayerEnteredDeathZone ─► player.health.Die() ─► HealthIsZero ─► PlayerDeath ─► …
Stomp (player above enemy) ─► unchanged
```

## Bugs in the original code fixed along the way
Damage had to go through `Health`, which exposed these existing problems:

| Problem in original code | Effect once damage uses Health | Fix |
|---|---|---|
| `HealthIsZero` always scheduled `PlayerDeath`, for any entity's Health | Any enemy given a Health component would kill the player when it died | Only the player's health schedules `PlayerDeath` |
| `PlayerDeath` only ran `if (player.health.IsAlive)` and killed the health itself | A death caused by damage reaching 0 did nothing: no death animation, no respawn | `PlayerDeath` runs when health **is** depleted (`Precondition`) |
| `PlayerSpawn` restored only +1 HP (`Increment()`) | With 3 max HP the player would respawn with 1 HP | `ResetToMax()` |
| `Health.Decrement()` scheduled `HealthIsZero` again when already at 0 | Death could be triggered twice | `Decrement` does nothing when already dead |
| `Health.maxHP` accepted 0 or negative values | The player would start "dead": no damage, no stomp, death zones do nothing, no respawn | `[Min(1)]` on `maxHP` |

## Files changed

### Scripts
| File | Change |
|---|---|
| [Health.cs](../../Assets/Scripts/Mechanics/Health.cs) | Added `Decrement(int amount)`: ignores amounts ≤ 0 and calls on an already-dead entity, and raises `HealthIsZero` only once. Added a `CurrentHP` getter and `ResetToMax()`. `Decrement()` is now `Decrement(1)`, and `Die()` is now `Decrement(currentHP)` instead of a loop. `[Min(1)]` on `maxHP`. The existing public API is unchanged. Typo fixes in comments. |
| [EnemyController.cs](../../Assets/Scripts/Mechanics/EnemyController.cs) | New `[SerializeField] private int contactDamage = 1` with `[Min(0)]` and a tooltip, exposed through the read-only property `ContactDamage`. This is **X, per enemy**. |
| [PlayerEnemyCollision.cs](../../Assets/Scripts/Gameplay/PlayerEnemyCollision.cs) | The non-stomp branch now calls `HurtPlayer()` (decrement by `enemy.ContactDamage`, then schedule `PlayerHurt` if the player survives) instead of `Schedule<PlayerDeath>()`. `Precondition()`: a dead player can't be hurt and can't stomp. The stomp branch is unchanged. |
| [PlayerHurt.cs](../../Assets/Scripts/Gameplay/PlayerHurt.cs) **(new)** | Gameplay event "player took damage but survived": plays `ouchAudio`, sets the existing `hurt` animator trigger, and calls `player.Knockback()`. `Precondition() => player.health.IsAlive`: when two enemy hits are processed in the same frame and the second one is lethal, the first hit's `PlayerHurt` runs after the death, and this skips it. |
| [HealthIsZero.cs](../../Assets/Scripts/Gameplay/HealthIsZero.cs) | Schedules `PlayerDeath` only when `health == model.player.health`. Enemy deaths stay the job of whatever damaged the enemy, as the stomp code already does. |
| [PlayerDeath.cs](../../Assets/Scripts/Gameplay/PlayerDeath.cs) | `Precondition() => !player.health.IsAlive`. Removed the `IsAlive` wrapper and the `Die()` call. Calls `player.CancelKnockback()`, so a body killed during a knockback doesn't keep sliding. The rest of the death sequence (camera unfollow, input off, sound, animation, respawn after 2 s) is unchanged. |
| [PlayerEnteredDeathZone.cs](../../Assets/Scripts/Gameplay/PlayerEnteredDeathZone.cs) | Calls `model.player.health.Die()` instead of scheduling `PlayerDeath`, so death zones use the same single death path. It is still an instant kill. |
| [PlayerSpawn.cs](../../Assets/Scripts/Gameplay/PlayerSpawn.cs) | `health.Increment()` → `health.ResetToMax()`. |
| [PlayerController.cs](../../Assets/Scripts/Mechanics/PlayerController.cs) | **Knockback** (see below). `FacingDirection` property (+1 right / −1 left). `collider2d`, `audioSource` and `health` turned from public fields into public properties with private setters, assigned in `Awake` and hidden from the Inspector. |

### Knockback (PlayerController)
- `Knockback()` pushes the player **opposite to the facing direction** in a small arc. It sets a constant horizontal speed and an initial upward speed, and gravity bends the path into an arch. The sprite keeps facing the same way.
- While knocked back, player input is ignored. The knockback ends on landing or after `knockbackMaxDuration`, so a knockback off a ledge returns control in mid-air.
- `jumpState` is set to `InFlight`, and pending jump input is cleared so it can't cut the arc short.
- `IsGrounded` is only refreshed in `FixedUpdate`. The knockback therefore waits until the player has actually left the ground before it treats "grounded" as a landing, and the normal `InFlight → Landed` check waits until the knockback ends.
- `CancelKnockback()` stops an active knockback. `PlayerDeath` calls it, so death stops the player as before, and a long `knockbackMaxDuration` can't carry over into the respawn.
- The animator is untouched: the existing `hurt` trigger is enough.
- Settings appear on the Player under the **Knockback** header in the Inspector:

| Setting | Default | Meaning |
|---|---|---|
| Knockback Speed | 3 | Horizontal speed, opposite to the facing direction |
| Knockback Lift | 3 | Initial upward speed (height of the arc) |
| Knockback Max Duration | 0.75 s | Longest time input is overridden |

### Assets
| Asset | Change |
|---|---|
| [Player.prefab](../../Assets/Prefabs/Player.prefab) | `Health.maxHP` 1 → **3**, set through the Unity API. The scene's Player instance has no override, so it picks this up. The prefab's git diff also contains Unity 6 re-serialization noise (format upgrade from 2020.3), which changes no behaviour. |
| Scene | Not modified. All 16 enemies use the default Contact Damage (1). |

No packages, project settings or animator controllers were changed.

## For the designer
- **Player health:** select the Player in the scene (or the Player prefab) and edit **Health → Max HP**.
- **Damage per enemy (X):** select any enemy and edit **Enemy Controller → Contact Damage**. Editing the Enemy prefab changes the default for all of them.
- **Knockback feel:** Player → **Knockback** section.

## How to verify (Play mode)
1. Select the Player and set the Inspector to Debug mode to watch `currentHP`.
2. Walk into an enemy from the side: ouch sound, hurt animation and a small backward hop, and HP goes 3 → 2.
3. Take 3 hits: death animation, then a respawn at the spawn point after about 2 s with 3 HP.
4. Stomp an enemy: it dies and the player bounces, with no HP lost.
5. Fall into a pit (death zone): instant death at any HP, then a respawn with full HP.
6. Set one enemy's Contact Damage to 3: one side hit kills the player. Set it to 0: that enemy is harmless.

## Known limitations
- **Repeated hits:** the player can still be hit several times in quick succession. Question 2 adds the post-hit immunity for this.
- **Knockback speed:** the default knockback speed (3) is slower than a patrolling enemy (about 3.5 units/s), so an enemy walking toward the player can catch up during the hop. It can be tuned on the Player.
- **Pre-existing, not fixed:** `EnablePlayerInput` (2 s after a respawn) re-enables control even if the player died again within those 2 s.
- **Knockback direction:** it is opposite to the *facing* direction, as specified. If an enemy hits the player from behind, the knockback pushes the player toward that enemy.
- **No automated tests:** the project has no assembly definitions, and the test doesn't ask for tests. Changes were checked by a clean compile (0 errors, 0 warnings) and the manual Play-mode steps above.
