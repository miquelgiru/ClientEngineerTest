# 00 — Overview (from README.md)

> Text below is copied verbatim from `README.md`, only regrouped under these headings. Original numbering of the Instructions list is kept. The questions themselves live in `question-01.md` … `question-04.md`.

## Context

# Unity Project Test
Unity Project Take Home Test

You have been provided with a copy of the 2D Platformer Microgame project, a small project offered by Unity as part of their learning program.  This will be the basis for the test, all questions pertain to adding or changing this project.  In this test we will present a series of feature requests from a theoretical game designer.

- The project is using Unity version `6000.3.25f1`

## Rules

1. Clone the repository to your local machine OR download the project as a zip file and establish your own local git repo
2. Read each question below and work through them.

7. We ask you to take no more than 4 hours on the test and to self report how long you worked on it.  Do not count environment setup (e.g. installing IDE, Unity, setting up `git`) as part of that 4 hours.
8. You may use AI tools, including coding agents.

- Please avoid adding 3rd party frameworks to the solution as much as you can.  If you feel one is necessary indicate why in your commit.  Please commit this as a separate commit from any of your code.
- Aim to complete the questions using the style or approach this solution uses
- Developer art is expected, do not waste time finding the perfect art

## Deliverables

3. Please commit each answer with comments on which question the commit addresses.  It is OK to go back and change previous answers if you jump around in the test, just note it in your commit.
4. You will not need to push your results.  Instead, when you are done use this git command to create a bundle with your results: git bundle create UnityTestResults.bundle --all
5. Note that deleting the "Library" folder before you create the bundle will significantly reduce the size
6. Email the bundle back to your contact.  If it is too large, try uploading to an online file storage provider like Google Drive and provide a link
8. In your submission, describe how you used them. You may optionally include relevant agent transcripts or prompts.

## Evaluation

8. We are looking for evidence of skilled agentic engineering: clear direction, critical evaluation of outputs, and ownership of the final solution.

- You do not have to complete all the questions.
- If you partially complete a question it is OK submit that with a comment indicating that it is WIP (work in progress)

## Appendices

### Other tips (verbatim)
- There is a harmless warning about InputSystem you can safely ignore.
- There is light project documentation found in the Assets/Documentation folder that will help you understand the project better

### Notes — NOT part of the original statement
Summary of `Assets/Documentation/PlatformerTemplateUserGuide.pdf`:
- **GameController** exposes the `PlatformerModel` (virtual camera, player, spawn point, jump modifier/deceleration); accessible anywhere via `Simulation.GetModel<PlatformerModel>()`.
- **TokenController** animates all tokens in one batch; **MetaGameController** switches between menu and gameplay; **UI Canvas** is a uGUI menu with placeholder tabs.
- **Enemies** are prefab instances placed in the scene; patrol path assigned on the EnemyController. **Zones**: DeathZone = instant death, VictoryZone = win.
- **Architecture**: *Mechanics* (Unity idioms, post events, update model) → *Gameplay* (events holding the game rules; modify model, post events; avoid Unity idioms) → *Model* (data + data methods only) → *Visual* (feedback; may subscribe to events, should not post events or modify model).
- **Customising behaviour**: edit an event's `Execute`, or subscribe to its static `OnExecute` delegate (fired after `Execute`).

### Question files
- [question-01.md](question-01.md) — Player health / per-enemy damage
- [question-02.md](question-02.md) — 1 second immunity after enemy collision
- [question-03.md](question-03.md) — Designer-configurable game modes
- [question-04.md](question-04.md) — Player gun and bullets
