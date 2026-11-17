# Transcript summary — Test setup (before Question 1)

**Tool:** Claude Code (Claude Opus 5.5) inside VS Code. It controlled the open Unity Editor through the Unity MCP bridge.
**Ground rules:** a `CLAUDE.md` I wrote beforehand. It sets the workflow (understand → plan → implement → verify → report), Unity 6 API rules, code style, and "never hand-edit `.meta` / `.unity` / `.prefab` YAML".
**Mode:** plan mode. The agent could only read the project and propose a plan until I approved it.

My prompts are quoted exactly as typed, typos included. The agent's replies are summarised.

---

## 1. Kick-off prompt

> Read CLAUDE.md & README.md completely. Split it into Exercices/00md (context, rules, deliverables, evaluation, appendices).
> Then create one file per question (question-01, question-02, question-03, question-04), copying the text exactly, without summarizing.
> Then analyze the Unity project: folders, scenes, scripts and what they do, packages, render pipeline, input system, assemblies. Then give me: (1) a summary
> of the project; (2) dependencies between the exercises and the shared systems they need: the architecture
> you propose (assemblies, main interfaces, composition root, where the static facade fits) so later exercises don't need
> a rewrite; (4) ambiguities I must decide. Fill in 'Project facts' and 'Exercise map' in CLAUDE.md. Don't write any code
> yet.

### What the agent did (read-only)
- Found that `README.md` contains the whole statement: there is no PDF, and `Exercises/` was empty.
- Read every gameplay script, the prefabs, the scene wiring, `ProjectSettings` and the `Assets/Documentation` PDF.
- **Project facts it found:**
  - Unity 6000.3.25f1, **Built-in** render pipeline, **legacy Input Manager only** (no Input System package), Cinemachine 2.10.7.
  - A single scene, `SampleScene`, and **no asmdefs**.
  - The template's architecture: a static `Simulation` discrete-event scheduler (the "static facade") with pooled `Simulation.Event<T>` gameplay events, plus a shared `PlatformerModel` exposed by `GameController`.
- **Latent bugs it flagged early.** These turned out to be central to Q1:
  1. `HealthIsZero` always schedules `PlayerDeath`, whichever object's Health reached 0.
  2. `PlayerDeath` only runs if the player is *alive*, so a death caused by damage would never respawn.
  3. `PlayerSpawn` restores only +1 HP.
  4. `Health` reads `maxHP` only in `Awake`.
  5. The Enemy prefab has no `Health`.
- **Exercise dependencies:** Q2 builds on Q1's damage path. Q3 configures values introduced by Q1/Q2 (max HP, immunity time). Q4 needs Q1's amount-based damage and the `HealthIsZero` fix, and can take its damage value from Q3's game modes.
- **First proposed architecture:**
  - A `Platformer` asmdef plus EditMode/PlayMode test asmdefs.
  - Small pure C# classes for logic that can be tested.
  - `GameController` as the composition root.
  - `Simulation` kept as the event bus. Wrapping it would fight the template, and the README asks to follow the project's style.
- **Ambiguities raised, each with a recommendation:**
  - README "use this solution's style" vs CLAUDE.md's DI/MVP rules. Recommendation: project style wins.
  - Where X (per-enemy damage) lives.
  - Immunity scope.
  - How a designer selects a game mode.
  - Fire key.
  - Enemy health and stomp behaviour.
  - No RUM/telemetry, since nothing in the questions asks for it.

## 2. Plan review — I rejected part of the plan

The agent's plan included creating a `Platformer` assembly definition so automated tests could reference the game code. I rejected that step:

> Since the test is not asking for testing, skip creating a new assembly

The agent updated the plan:
- No new assemblies; all new code stays in Assembly-CSharp under the existing `Platformer.*` namespaces.
- No automated tests, since test asmdefs cannot reference Assembly-CSharp.
- Each exercise is verified by a clean compile, the Unity console, and a manual Play-mode checklist.
- This decision is recorded in `CLAUDE.md`, so its "write/run tests" workflow step is read that way.

I then approved the plan.

## 3. Result
- `Exercises/00-overview.md`: the README's non-question text, regrouped word for word under Context / Rules / Deliverables / Evaluation / Appendices, plus a clearly marked summary of the documentation PDF.
- `Exercises/question-01.md` … `question-04.md`: each question copied exactly. The agent checked them against README.md with `grep -F`.
- `CLAUDE.md`:
  - "Project facts" filled in: pipeline, input, UI, scene contents, physics, assemblies decision, animator parameters, conventions, latent bugs.
  - "Exercise map" and "Shared systems" filled in.
  - A list of open decisions to confirm before each question.
  - One stale line about a PDF corrected.
- No code, assets or Unity settings were changed in this phase.

## My takeaways for this phase
- I steered the scope: no extra assemblies or tests, because the test doesn't ask for them and time is limited.
- I asked for the analysis and the list of ambiguities *before* any code, so the bugs above were known before Q1 started.
