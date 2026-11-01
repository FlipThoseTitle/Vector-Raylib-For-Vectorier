# Vector-Raylib
Port of the game Vector to the Raylib framework

This was made with debugging the game in mind.


To enter a specific level, launch the app with -level LEVEL NAME argument.
To enter in hunter mode, add -huntermode.

For example: -level TECHPARK_BONUS_05 -huntermode

## Vectorier

Vectorier starts the game with `-project PROJECT_NAME -level LEVEL_NAME [-huntermode]`.
The published game sits in `Data/Game/<runtime>/` (only the executable and the raylib library) and reads the
project `Data/Projects/PROJECT_NAME/`, which uses Vectorier's layout: `xmlroot` instead of `xml` and trick icons in
`icons/tricks`. Without `-project`, the `Resources` folder next to the executable is used as before.

## Publishing

VS Code tasks (Terminal › Run Task): `Publish Windows (win-x64)`, `Publish Linux (linux-x64)`,
`Publish macOS Apple Silicon (osx-arm64)`, `Publish macOS Intel (osx-x64)` and `Publish All`.
Each publishes a self-contained Release build to `publish/<runtime>/`. Copy that folder's content to
`Data/Game/<runtime>/` of Vectorier.

Text uses Arial from the system (Linux: Liberation Sans / Arimo when Arial is not installed; an `arial.ttf` next to
the executable also works).

## Controls

- Esc - Pause the game
- F11 - Fullscreen
- Q - Change Anti-Aliasing
- F - Next frame (only when paused)
- S - Slow Mode
- A - Shows areas
- P - Shows platforms
- T - Shows triggers
- N - Model nodes: Off, All, Important (COM in blue, DetectorH / DetectorV in red, Camera in yellow)
- E - Shows model edges
- D - Shows model detectors
- B - Shows model Bounding box and Collision box if it has
- I - Shows the moves information
- G - Shows a cross line through the screen center
- M - Mutes / unmutes the music
- R - Restart the level
- H - Hides the debug UI