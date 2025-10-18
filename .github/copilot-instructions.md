# Copilot instructions for GodotAiPlatformer
# Copilot instructions for GodotAiPlatformer
# Copilot instructions for GodotAiPlatformer

Short, actionable guidance for AI coding agents working in this repository.

Project summary
- Godot 4.x platformer written in CSharp using Godot Mono. Engine configuration is in project.godot. Solution file is GodotAiPlatformer.sln and project file is GodotAiPlatformer.csproj.
- Core gameplay scripts are in the repository root: CharacterController.cs, InputController.cs, CameraFollow.cs, Explosion.cs
- Game type: 2D side-scrolling platformer with narrative elements and short-form (approx 15 minute) gameplay experience.

Artistic description
- Title: Adventures of Green Man
- Summary: A short, 15-minute platformer narrative that follows one day in the life of an imaginary moon-alien called Green Man. The experience is intentionally intimate and surreal: Green Man wakes up like a burned-out salary worker, performs a humorously mechanical morning routine, pulls on his hat and takes his briefcase, and slips out of an apartment to escape an angry spouse.
- World and beats: Outside, Green Man joins a long tunnel of near-identical alien figures moving like clockwork parts toward a distant black door. Every sign points to the door and reads "work." Along the route, giant suction-cup glass tubes hover above the line and intermittently pull aliens inside. A display shows an evaluation score; green scores let the alien ascend somewhere unknown, while red scores spit the alien back into the line. The art direction should support a slightly melancholic, absurdist mood with clockwork repetition and occasional uncanny visual flourishes.


Key architecture
- InputController centralizes input handling for keyboard and gamepad. Gameplay scripts read from it via signals or helper methods such as GetMoveDirection and IsActionJustPressed rather than calling Godot Input directly.
- CharacterController is the player controller. It expects an exported InputControllerPath to be wired in the scene and optionally an exported PackedScene named ExplosionScene for instancing effects. Movement and physics are handled in _PhysicsProcess and MoveAndSlide.
- CameraFollow follows a Node2D target and applies a velocity-based look-ahead and smoothing. Use exported TargetPath to wire the target.

Project conventions
- Export NodePath and PackedScene fields for any scene wiring required by code. Example: CharacterController.InputControllerPath must be set in the scene to avoid null input.
- Prefer InputController helpers over direct Input polling in gameplay code. Example helpers: GetMoveDirection, IsJumpPressed, IsActionJustPressed.
- When instancing a PackedScene, check the instance type before accessing node-specific properties. Example: if explosion is Node2D then set GlobalPosition and add it to the scene tree.
- Keep Godot node names and types stable (AnimatedSprite2D, Sprite2D, CharacterBody2D). If you rename nodes in code, update the scene in the Godot editor.

Build and debug workflow
- Fast edit-run-iterate in the Godot 4.x editor with Mono enabled. Wire exported fields in the Inspector and run scenes.
- Command-line compile only: run dotnet build GodotAiPlatformer.sln. This compiles CSharp code but does not run scenes. Requires a Mono SDK compatible with the Godot Mono runtime.
- Debugging: use Godot's built-in debugger and editor console. Watch for GD.Print and GD.PushWarning messages. CharacterController._Ready contains warning logs when wiring is missing.

Files to inspect when changing gameplay
- CharacterController.cs: movement, jump buffer and coyote time, animation state selection, drop-shadow raycast, explosion instancing
- InputController.cs: input mapping, gamepad handling, signals and helper poll methods (single source of truth for input)
- CameraFollow.cs: camera target wiring and look-ahead smoothing
- Explosion.cs and explosion.tscn: example pattern for instanced visual effects

Cautions
- Do not hand-edit scene resource UIDs or project.godot values in text; use the Godot editor to avoid corrupting scene resources
- If you change exported properties referenced by scenes, add defensive null checks and GD.PushWarning so issues surface in the editor console at runtime

Next steps I can take
- Add a short code example that shows safe instancing of ExplosionScene from CSharp
- Add macOS-specific setup steps for Godot and Mono if you provide your local tool versions

Ask which area you want expanded and I will iterate.
  - Serialized scene wiring (scene files and `project.godot`) should be edited in the Godot editor. Avoid manual edits to scene resource UIDs in text unless necessary.
