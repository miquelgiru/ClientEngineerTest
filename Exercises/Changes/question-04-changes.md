# Question 4 — Changes

> The game designer would like to add a player "gun" weapon. On a keypress, the Player should fire a "bullet" in whatever direction they are facing. The player can have unlimited bullets and fire as often as they like. If the bullet collides with an enemy, it should do Y damage, where Y is set specific to the player.

## Summary
- **Firing:** press **Fire1** (Left Ctrl, left mouse button, or joystick button 0) and the player fires a bullet in the direction it's facing. A small gun is drawn on that side.
- **Gun assets:** guns are ScriptableObjects (`Gun`, created with **Create → Platformer → Gun**). Each configures:
  - **Model:** gun sprite and hold offset, which is also where bullets are fired from.
  - **Bullets:** bullet sprite, speed and lifetime.
  - **Ammo:** **Unlimited Bullets** (on by default) or a **Bullet Limit**.
  - **Fire Rate:** 0 = no limit, so the player can fire as often as they like.
  - **Damage:** this is **Y**.
- **Choosing the gun:** each **Game Mode** picks the gun the player carries (**Weapon → Gun**). Leaving it empty means no gun. All three existing modes use the new **Pistol**, so by default the player has a gun with unlimited bullets.
- **Hitting enemies:** a bullet that hits an enemy deals the gun's damage to the enemy's new **Health (3 HP)**.
  - A hit the enemy survives plays its hurt animation and sound.
  - The last one kills it.
  - **Stomping still kills instantly.**
- **Object pool:** bullets come from an **object pool** (`UnityEngine.Pool.ObjectPool<Bullet>`). They're reused instead of instantiated and destroyed on every shot.
- **Ammo refill:** limited ammo refills when the player respawns.

## Design
The solution follows the template's architecture:
- **Mechanics** components detect input and collisions.
- A **Gameplay** event applies the rule (damage → hurt or death).
- **Model** ScriptableObjects hold the designer data.

```
GameMode.Gun ──(level start / Editor live tuning)──► PlayerController.ApplyGameModeTuning ──► PlayerGun.Equip(gun)  (null = no gun)

PlayerGun.Update: Fire1 + can fire ──► bulletPool.Get().Launch(origin, facing, gun)
Bullet.OnTriggerEnter2D:  enemy ──► Schedule<BulletHitEnemy>(enemy, gun.Damage) ──► enemy Health.Decrement(Y)
                                                                              ├─ survives ─► enemy.PlayHurt()  (hurt anim + ouch)
                                                                              └─ HP == 0 ─► EnemyDeath (existing)
                          enemy / level geometry ──► pool.Release(bullet)
                          lifetime over          ──► pool.Release(bullet)
PlayerSpawn.OnExecute ──► PlayerGun refills ammo (same hook the enemies and tokens use)
```

**"Y is set specific to the player."** Y is the damage of the gun the player carries. That gun comes from the active Game Mode, or from the `PlayerGun` component when no mode is assigned.

## Decisions (mine)
| Topic | Decision |
|---|---|
| Enemy health | Enemies get **Health with Max HP 3**. Stomping still kills instantly. |
| Fire key | **Fire1**, the existing Input Manager button, so no ProjectSettings change. |
| Where the gun is chosen | **Per Game Mode** (empty = no gun). |
| Limited ammo | **Refills on respawn**, consistent with enemies and tokens resetting. |

## Implementation details
- **Pool:** `PlayerGun` creates the pool in `Awake`. Bullets are instantiated under a runtime **"Player Bullets"** object. Get and release toggle `SetActive`. `collectionCheck` is on, with default capacity 10 and max size 50. The pool is cleared in `OnDestroy`.
- **Releasing bullets:** a bullet goes back to the pool on a solid hit (enemy or level) or when its lifetime ends. An `isActive` guard prevents a double release, for example two hits in the same physics step, which the pool would reject.
- **Bullet physics:**
  - The body is Dynamic with gravity 0, Continuous collision and Interpolate, and the collider is a small **trigger**. Triggers fire against the kinematic enemies and the static level.
  - Characters move with casts that ignore triggers, so bullets never block anyone's movement.
  - Bullets ignore other triggers (tokens, zones, other bullets) and the player who fired them.
- **Hit height:** relative to the ground, the enemy hitbox is a circle from 0 to 0.45, and the player's pivot is at 0.41. The default hold offset (0.25, −0.15) fires bullets about 0.26 above the ground, through the middle of the enemy hitbox.
- **When you can't fire:** with no gun, while dead, **while the Hurt animation is playing**, during the 2 s respawn lock or at victory (`controlEnabled` false), while the main menu is open (`Time.timeScale` 0), during the fire-rate cooldown, or when out of ammo.
- **Blocking fire while hurt:** `PlayerController.IsHurtAnimationPlaying` is true while the Animator is in `Player-Hurt`. It's also true on the frame of the hit, before the Animator has entered that state, because immunity has already started and the Hurt state hasn't been seen yet. The same Hurt-state tracking drives the Q2 blink, so the player can shoot again exactly when the blinking starts.
- **Gun model:** the gun child is mirrored with the player's facing. It's hidden when there's no gun, when the player is dead, or while the player sprite is hidden by the Q2 immunity blink, so it blinks too.
- **`BulletHitEnemy` precondition:** `enemy.IsAlive`, because the enemy may already have been killed earlier in the same frame. An enemy without Health dies in one hit, like a stomp.
- **Live tuning:** gun values are read when firing, so editing the Gun asset during Play applies immediately. The exception is the ammo count (see Known limitations). Changing a mode's Gun slot during Play re-equips the player through the Q3 live-tuning hook.

## Files changed

### New scripts
| File | Purpose |
|---|---|
| [Gun.cs](../../Assets/Scripts/Model/Gun.cs) | `ScriptableObject` with `[CreateAssetMenu]`. Fields are `[SerializeField] private` with `[Header]`, `[Tooltip]` and `[Min]`, exposed through read-only properties. |
| [PlayerGun.cs](../../Assets/Scripts/Mechanics/PlayerGun.cs) | On the Player: Fire1 input, bullet pool, ammo, fire-rate cooldown, gun model, refill on respawn. Has `Equip(Gun)`, `CurrentGun` and `BulletsLeft`. |
| [Bullet.cs](../../Assets/Scripts/Mechanics/Bullet.cs) | Pooled bullet: `Launch`, lifetime, trigger handling, schedules `BulletHitEnemy`, releases itself to the pool. |
| [BulletHitEnemy.cs](../../Assets/Scripts/Gameplay/BulletHitEnemy.cs) | Gameplay event: damage → `PlayHurt()` or `EnemyDeath`. |

### Modified scripts
| File | Change |
|---|---|
| [EnemyController.cs](../../Assets/Scripts/Mechanics/EnemyController.cs) | `IsAlive` (`EnemyDeath` disables the collider) and `PlayHurt()` (`hurt` animator trigger plus `ouch` sound). The Animator is cached in `Awake`. |
| [PlayerEnemyCollision.cs](../../Assets/Scripts/Gameplay/PlayerEnemyCollision.cs) | The stomp branch calls `Health.Die()` before `EnemyDeath`, so a stomp kills whatever the enemy's HP. This replaces the template's 1-damage stomp and its "bounce higher if it survives" branch, which became unreachable. `Precondition()` also requires `enemy.IsAlive`: an enemy killed earlier in the same frame, for example by a bullet, can't be stomped again (`EnemyDeath` twice, double ouch) and can't hurt the player. |
| [GameMode.cs](../../Assets/Scripts/Model/GameMode.cs) | New **Weapon → Gun** field and `Gun` property. |
| [PlayerController.cs](../../Assets/Scripts/Mechanics/PlayerController.cs) | Caches `PlayerGun` in `Awake`. `ApplyGameModeTuning` equips the mode's gun when it differs from the current one. New `IsHurtAnimationPlaying`, used by `PlayerGun` to block firing while hurt. |

### Assets (created through the Unity API, not by editing YAML)
| Asset | Content |
|---|---|
| `Assets/Weapons/Sprites/Pistol.png`, `Bullet.png` **(new)** | Placeholder art generated in the Editor (no weapon art exists in the project): a 28×16 px grey pistol and a 10×10 px yellow round. Imported as Sprite at 100 PPU (the player's PPU) with Point filtering. |
| `Assets/Weapons/Pistol.asset` **(new)** | Gun: those sprites, hold offset (0.25, −0.15), speed 12, lifetime 1 s, unlimited bullets (limit 10 if switched off), fire rate 0, damage 1. |
| `Assets/Prefabs/Bullet.prefab` **(new)** | SpriteRenderer (player's sorting layer, order 6), Rigidbody2D (Dynamic, gravity 0, Continuous, Interpolate, rotation frozen), CircleCollider2D (trigger, r = 0.05), `Bullet`. |
| `Assets/Prefabs/Player.prefab` | Added `PlayerGun` (gun = Pistol, Bullet prefab, gun renderer) and a child **"Gun"** with a SpriteRenderer at sorting order 6. |
| `Assets/Prefabs/Enemy.prefab` | Added **`Health` (Max HP 3)**, inherited by all 16 scene enemies. The diff also has Unity 6 re-serialization of the old prefab. |
| `Assets/GameModes/Default`, `Hardcore`, `Practice` | Weapon → Gun = **Pistol** (one line each). |

The scene is unchanged: the Player and enemies get the new components from their prefabs.

## For the designer
1. **Create a gun:** **Create → Platformer → Gun**, or duplicate `Assets/Weapons/Pistol`. Set the sprites, ammo, fire rate and damage; every field has a tooltip.
2. **Give it to a mode:** select a Game Mode asset and set **Weapon → Gun**. Leave it empty for a mode without a gun.
3. **Tune while playing:** edits to the Gun asset during Play apply immediately and persist after Play, because it's an asset.
4. **Enemy toughness:** select an enemy, or the Enemy prefab, and change **Health → Max HP**.

## How to verify (Play mode)
1. **Fire:** press Fire1 (Left Ctrl or click). A yellow bullet flies in the facing direction. Turn around: the gun moves to the other side and bullets fly left.
2. **Enemy hits:** the enemy plays its hurt animation and sound. The **third** bullet kills it, and it falls like a stomped enemy.
3. **Bullet ends:** bullets disappear on walls and ground, or after about 1 s. Spam fire for a while: no console errors. In the Hierarchy, the **Player Bullets** object reuses inactive bullets instead of growing.
4. **Stomp:** still kills in one hit.
5. **Respawn:** die and respawn. Enemies come back with full HP.
6. **Limited ammo:** **before pressing Play**, untick **Unlimited Bullets** on `Pistol` and set **Bullet Limit** to 3. After 3 shots the gun stops firing, and it refills after a respawn.
7. **Fire rate and damage:** set **Fire Rate** to 2 and you get at most 2 shots per second. Set **Damage** to 3 and one bullet kills.
8. **No gun:** clear **Weapon → Gun** on the active Game Mode before Play. No gun is drawn and Fire1 does nothing.
9. **Menu:** open it and click. No bullet is fired.
10. **While hurt:** get hit by an enemy and press Fire1 during the Hurt animation. Nothing fires. Once the blinking starts, firing works again.
11. **Restore Pistol** to its defaults after testing, because asset edits persist.

## Known limitations
- **No HUD:** there's no ammo counter (not requested). `PlayerGun.BulletsLeft` is visible in the Debug Inspector.
- **Ammo limit and live tuning:** the ammo count is filled from **Bullet Limit** only when ammo is refilled (level start, equip, respawn). Changing the limit, or unticking Unlimited Bullets, *during* Play only takes effect after the next respawn. Found in review; not fixed by decision.
- **Bullet interpolation (unverified):** the Bullet prefab uses Interpolate. A reused bullet is reactivated where it last disappeared and then teleported to the gun, so it might show for one frame between the two positions. If you see that flicker, set the prefab's Interpolate to None.
- **No effects:** no shooting sound or muzzle effect.
- **Horizontal only:** bullets fly horizontally from the hold offset, so they can pass over enemies standing on lower ground.
- **New modes start without a gun:** a mode created from the Create menu has no gun until one is assigned. Duplicating `Default` keeps the Pistol.
- **No automated tests:** the project has no assembly definitions, as decided at setup. Changes were checked by a clean compile and console, read-backs of every asset and prefab value, git diff review, and the manual Play-mode steps above.
