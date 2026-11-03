# To Meteora: Character Setup Tool

A Unity editor workflow for adding new characters. You fill in one data asset, and the tool builds a playable, animated character prefab, registers it for selection, and stops bad input before anything is written to the project.

| | |
|---|---|
| **Unity** | 6000.6.7f1 |
| **Render pipeline** | URP 17.6, Linear colour space |
| **Input** | Legacy Input Manager |
| **Packages** | URP, uGUI with TextMeshPro, Test Framework (all built in) |
| **Characters** | Stylized3DMonster: Angelo = `Monster35_01`, Brick = `Monster33_01`, plus three more monsters for new characters |
| **Map** | Palmov Island, camping forest location |

## Getting started

1. Open the project folder in Unity Hub with Unity 6000.6.7f1.
2. Open `Assets/ToMeteora/Scenes/CharacterTest.unity` and press **Play**.
3. Pick **Angelo** or **Brick** in the top-left panel.
   - Move with **WASD** or the arrow keys.
   - Attack with **Space** or the **Attack** button.

   The panel at the bottom left shows health, speed, ability and cooldown.

The character starts at the **Player Start** object (`Gameplay/Player Start`), facing the way its green arrow points in the Scene view. Move or rotate it to change where play begins.

The camera is a fixed isometric view that follows the character. To see the whole map instead, untick **Follow Character** on `Gameplay/Main Camera`.

## Adding a character

Open **To Meteora > Character Setup**. The window lists every character on the left, shows the selected definition in the middle, and has a live 3D preview on the right.

1. Click **New Character...** and save the asset in `Data/Characters`.
2. Fill in the fields:

   | Field | Notes |
   |---|---|
   | ID | Lowercase and permanent, e.g. `hero_tiger`. Saves rely on it |
   | Display Name | Shown in the game |
   | Visual Prefab | A model prefab, e.g. `Stylized3DMonster/Monster19_FreeTrial/Prefab/Monster19_01` |
   | Visual Height | The model is scaled to this height and placed on the ground |
   | Visual Yaw | Rotates the model if it doesn't face forward. The monsters need 0 |
   | Idle Clip / Move Clip | Looping clips from the model's `Anim` folder |
   | Attack Clip | Optional. Without one, the attack plays a short punch effect |
   | Move Animation Speed | Raise it if the feet slide |
   | Custom Controller | Optional. Must have a float `Speed` and a trigger `Attack` |
   | Attack Effect | Optional particle prefab played on attack, e.g. `Prefabs/Effects/AttackBurst` |
   | Attack Effect Scale | Size multiplier for the effect |
   | Attack Effect Offset | Where it appears relative to the character (x right, y up, z forward) |
   | Attack Effect Follows Character | On: moves with the character. Off: stays where it was played |
   | Health, Move Speed, Cooldown, Ability | Gameplay values |

3. Check the preview. Drag to rotate it, and switch between Idle, Move and Attack to play the clips. The Attack tab also plays the attack effect, so you can tune its size and position.
4. Click **Validate**. Every problem is listed with what to change. If the clips aren't set to loop, **Make Clips Loop** fixes that.
5. Click **Create / Update**. The prefab is written to `Prefabs/Characters/` and the character appears in the game's selection panel.

**Create / Update All** rebuilds every character. It is safe to run at any time.

New definitions saved in `Data/Characters` are added to `Resources/CharacterRoster.asset` automatically. Once a definition passes validation, its prefab is built the next time it is saved.

## Abilities and enemies

Abilities are assets in `Data/Abilities`. **New Ability...** in the setup window creates one; pick the ability type, and optionally assign it to the selected character. The selected character's ability can also be edited directly in the window, under **Ability settings**. If several characters share the ability, the window tells you.

| Field | Notes |
|---|---|
| Damage | Damage dealt to each enemy inside the area |
| Range | How far in front of the character the area is centred. 0 means around the character |
| Radius | Enemies within this distance of the area centre are hit |
| Area Indicator | Optional particle prefab that shows the area, e.g. `Prefabs/Effects/AoeRing`. Its circle is sized to the radius |

The demo map has dummy enemies (`Prefabs/Enemies/DummyEnemy`):
- Every enemy inside the area flashes when hit and gets darker as it loses health.
- At zero health an enemy shrinks away and comes back after `Respawn Seconds`.
- The HUD shows how many enemies the last attack hit.

## How it works

- **Data lives in the definition.** Each character is a `CharacterDefinition` ScriptableObject. The generated prefab only points to it, so changing a stat or a clip doesn't need a rebuild.
- **One shared animator.** `Animations/CharacterBase.controller` has Idle and Move states, switched by `Speed`, and an Attack state on a trigger. Each character swaps in its own clips at runtime through an `AnimatorOverrideController`, so a new character never needs a new controller. Root motion is off; movement comes from Move Speed.
- **The Animator goes where the bones are.** The tool reads the bone paths stored in the clips and places the Animator on the object they start from. A clip made for a different rig is rejected during validation.
- **Abilities.** A character that reuses an existing ability needs no code. A new ability means a new `AbilityDefinition` and `AbilityBehaviour` pair (engineering work); after that, the character goes through the same tool.

## What the tool checks and guarantees

**Validation runs before anything is written.** It checks:
- the ID format, and duplicate IDs across the project
- a missing visual, or one with no mesh
- an empty name
- zero or negative stats or height
- a missing ability
- missing or non-looping clips
- clips that don't match the model
- a custom controller without the required parameters

**Failures leave nothing behind.** If a build fails part-way through, the new prefab is removed and the roster is left as it was.

**Re-running is safe.** The same prefab file is updated in place, so its GUID and references don't change. A re-run with no data changes produces identical files.

**Renaming an ID** moves the prefab and keeps its GUID.

**A deleted model is recovered from.** If a character's model is deleted from the project, the next build recreates the prefab cleanly and logs a warning.

**What the tool owns on a generated prefab:**
- the link to the definition, and the ability component
- the `Visual` child's position, rotation and scale (measured from the real mesh, so the feet touch the ground), and its Animator settings
- the `CharacterController`, which gives gravity and collision

Anything else you add to the prefab is left alone.

## Tests

Close the Editor, then run from the project folder:

```bash
powershell -ExecutionPolicy Bypass -File run-tests.ps1
```

The same tests are under **Window > General > Test Runner**. Results are written to `TestResults/`.

Last run: **EditMode 6/6, PlayMode 2/2, no compiler warnings.**

| Test | Checks |
|---|---|
| `ValidDefinition_CreatesPrefab_AndRegistersIt` | Valid creation |
| `RunningSetupTwice_CreatesNoDuplicates_AndKeepsReferences` | Repeated setup: one prefab, same GUID, unchanged file |
| `IntentionalUpdate_ToSameDefinition_StaysValid` | Updating a definition you own stays valid |
| `MissingVisual_IsRejected_AndNothingIsCreated` | Missing visual reference |
| `TwoDefinitionsWithSameId_AreRejected_AndNothingIsCreated` | Two definitions with the same ID |
| `AddingBrick_DoesNotChangeAngelo` | Building Brick leaves Angelo's files and roster entry unchanged |
| `Character_CanBeSelected_AndSpawned` (Angelo, Brick) | Play-mode smoke check: select, spawn, values, cooldown |

Movement, animation, collisions, the setup window and the HUD were checked by hand in the Editor.

To rebuild every character from the command line (for CI; it exits with code 1 on any error):

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -projectPath . -executeMethod ToMeteora.Tools.CharacterBuilder.CreateOrUpdateAllFromCommandLine
```

## Demo scene

`Scenes/CharacterTest.unity` is generated by `Scripts/Editor/Demo/DemoMapBuilder.cs` around the camping forest location prefab. The asset pack's own scene is untouched.

The builder:
- **Colliders:** replaces the pack's box colliders with mesh colliders that follow the real terrain. Trees, rocks, tents, logs and the bridge block movement; grass and mushrooms don't.
- **River:** puts the river on Unity's Water layer, so characters stop at the bank.
- **Sizing:** sizes the camera, fog of war, play area and spawn point from the ground.

**Lighting is kept cheap:**
- one sun with soft shadows and a single shadow cascade
- gradient ambient light
- no extra lights
- post-processing limited to URP's final pass (tonemapping, colour grading, vignette)

To regenerate the scene, close the Editor and run:

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod ToMeteora.Tools.DemoMapBuilder.Build
```

> The fog of war reads the camera depth texture, so `Settings/URP-Renderer.asset` must keep **Copy Depth Mode** set to **After Opaques**.

## Project layout

All project code and content is under `Assets/ToMeteora`:

| Folder | Contents |
|---|---|
| `Scripts/Runtime` | Character, animation, abilities, roster, demo driver, HUD, camera, fog of war |
| `Scripts/Editor` | Validator, builder, setup window, preview, demo scene builder |
| `Scripts/Tests` | EditMode and PlayMode tests |
| `Data` | Character and ability definitions |
| `Animations` | Shared animator controller |
| `Prefabs/Characters` | Generated character prefabs (written by the tool) |
| `Prefabs/UI` | `GameHUD.prefab`, free to edit |
| `UI/Sprites` | Unity's placeholder sprites used by the HUD |
| `Materials`, `Shaders` | Sky and fog of war |
| `Settings` | URP assets and post-processing profile |
| `Resources` | `CharacterRoster.asset` (written by the tool) |
| `Scenes` | `CharacterTest.unity` |

The HUD uses TextMeshPro for all text. To restyle the character buttons, edit the hidden `CharacterButtonTemplate` inside `CharacterPanel/CharacterList`.

## Source control

- Always commit `.meta` files; they hold the GUIDs that keep references stable.
- Generated prefabs and the roster are committed. Output is deterministic, so a fresh clone works without running the tool first.

## Live demo

1. Beforehand, delete `Data/Characters/Brick.asset` and `Prefabs/Characters/hero_brick.prefab`.
2. Select Angelo, then **Validate** and **Create / Update**. Show the preview and its clips.
3. Add Brick: `hero_brick`, Brick, `Monster33_01`, height 3, `Monster33_Idle` / `Monster33_Walk_InPlace`, 140 / 4 / 4. Then **Create / Update**.
4. Run **Create / Update All**. There are no duplicates and no file changes.
5. Show the invalid cases: clear the visual, duplicate Angelo's asset to get a duplicate ID, and turn off looping on a clip.
6. Run `run-tests.ps1`.
7. Play the scene and switch between both characters.
8. Walk through one AI-assisted change from `EVIDENCE_AND_LOGS.txt`.

## Known limitations

- The monster pack has no attack animation. The attack plays the particle effect with a short punch on the model.
- Enemies are training dummies: they don't move or fight back.
- Obstacles use their full mesh, so fir trees block at the width of their lowest branches.
- The fog of war is visual only.
- The roster loads from `Resources`. A production project would likely use Addressables.
- Deleting a definition doesn't delete its prefab; the roster entry is cleaned up on the next build.
- Not profiled on mobile hardware. The monster textures would need compression budgets for devices.

## Documents

- [`EVIDENCE_AND_LOGS.txt`](EVIDENCE_AND_LOGS.txt): AI usage, defects found and fixed, timing log
- [`DESIGN_AND_ROLLOUT.txt`](DESIGN_AND_ROLLOUT.txt): bottlenecks, design boundary, production checks, six-week pilot, risks

Everything in this README is implemented. Addressables, mobile profiling, save compatibility, PvP authority and the six-week rollout are proposals, described in `DESIGN_AND_ROLLOUT.txt`.
