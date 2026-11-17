# Polishing — Changes

Gameplay polish requested after the four questions were committed. Each section is a small, self-contained change.

## 1. No movement during knockback
**Problem.** The player could steer during the last part of the knockback arc.
- Player input was already ignored while knocked back.
- But **Knockback Max Duration** (0.75 s) ended the knockback on a timer.
- With Knockback Lift 5 the arc lasts about 1 s (2 × 5 / 9.81), so control came back mid-air. It also came back during any longer fall, for example after being knocked off a ledge.

**Fix.** The knockback now lasts **until the player lands**, and the player can't move during it. Knockback Max Duration becomes a pure **safety limit**, so control still comes back if the player never lands (for example, stuck on a steep slope).

| File | Change |
|---|---|
| [GameMode.cs](../../Assets/Scripts/Model/GameMode.cs) | New tooltip explaining the safety-limit meaning. Default **3 s** (was 0.75). |
| [PlayerController.cs](../../Assets/Scripts/Mechanics/PlayerController.cs) | Fallback value 3 s, and the `Knockback()` doc comment is updated. The logic was already "ends on landing or at the limit". |
| `Assets/GameModes/Default`, `Hardcore`, `Practice` | Knockback Max Duration 0.75 → **3**, set through the Unity API (one line per asset). |

**Verify (Play mode):**
1. Get hit and hold left or right during the whole hop. The player only moves again after landing.
2. Get knocked off a ledge. There's no control until the player reaches the ground.
