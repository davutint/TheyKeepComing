# Repository Guidelines

## Project Structure & Module Organization

Unity 6 DOTS/ECS project for `DeadWalls`, a zombie tower-defense game. Core code lives in `Assets/Scripts`.

- `Assets/Scripts/ECS/Components`: pure `IComponentData` structs.
- `Assets/Scripts/ECS/Authoring`: MonoBehaviour authoring components and Bakers.
- `Assets/Scripts/ECS/Systems`: resources, waves, population, arrows, animation.
- `Assets/Scripts/ECS/Physics`: custom 2D circle physics and spatial hash.
- `Assets/Scripts/MonoBehaviour`: Unity managers and UI controllers.
- `Assets/Scripts/Editor`: editor tools and analyzers.
- `Assets/Scenes`, `Assets/Prefabs`, `Assets/Materials`, `Assets/UI`, `Assets/ScriptableObject`: Unity content.
- `Assets/Docs`: GDD and roadmap.

Do not track `Library`, `Temp`, `Obj`, `Logs`, `Build`, or IDE files.

## Build, Test, and Development Commands

Use Unity `6000.3.10f1`.

- Open: Unity Hub -> `IncremantalDots`.
- Edit code: open `IncremantalDots.sln` after Unity regenerates files.
- Run EditMode tests:
  `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -quit -logFile Logs/EditModeTests.log`
- Run PlayMode tests:
  `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -quit -logFile Logs/PlayModeTests.log`

No build script is checked in. Use Unity Build Settings.

## Coding Style & Naming Conventions

Use namespace `DeadWalls`. ECS systems should be `partial struct` implementations of `ISystem`; keep jobs Burst-compatible with `[BurstCompile]`. If an `ISystem` accesses static fields, remove `[BurstCompile]` from system methods but keep Burst on compatible jobs.

Components hold data only. Systems own behavior and one responsibility. Prefer `IJobEntity` for parallel ECS work. Use English identifiers and Turkish comments when comments are needed.

## Documentation Rules

When adding a system or folder, add specific architecture and editor setup docs there, for example `RESOURCE_TICK_SYSTEM_ARCHITECTURE.md` and `RESOURCE_TICK_SYSTEM_EDITOR_SETUP.md`. Update docs when extending a module.

## Testing Guidelines

Unity Test Framework is installed. Add EditMode tests under `Assets/Tests/EditMode` and PlayMode tests under `Assets/Tests/PlayMode`. Name tests after the system, for example `ResourceTickSystemTests`. Cover ECS logic, resource math, state transitions, and editor regressions.

## Commit & Pull Request Guidelines

Recent local commit titles use short summaries such as `Map Create System Added` or `M1.7 Worker Assignment UI + Building Detail Panel`. If the owner explicitly asks for commit text, mention the affected feature and changed systems/assets.

Do not create pull requests, issues, releases, remote branches, or any GitHub-facing artifact. Do not push, publish, upload, sync, or send repository files to GitHub or any remote service.

## Agent-Specific Instructions

Always communicate with the repository owner in Turkish. This is a standing rule for all future work in this repository.

### Mandatory Unity MCP Availability

Unity MCP is a hard prerequisite for every Unity project task in this repository.

- Before inspecting or changing source code, assets, scenes, prefabs, tests, project settings, or project documentation, verify that Unity MCP responds and that its active instance targets `IncremantalDots`.
- Filesystem tools may be used for scoped text inspection and edits only while the Unity MCP connection is healthy. Unity MCP remains mandatory for Unity Editor state, asset refresh/import, compilation, Console, scene/prefab, Play Mode, and test verification.
- If Unity MCP does not respond, disconnects outside an expected domain reload, or cannot target the correct instance, stop immediately. Do not continue through a shell or filesystem fallback.
- Ask the repository owner to restart or reconnect Unity MCP. Resume work only after the owner confirms readiness and MCP connectivity is verified again.
- A short disconnect caused by a Unity MCP-triggered domain reload or test run may be retried. If it does not reconnect promptly, stop and ask the repository owner to restart or reconnect it.

GitHub and remote operations are forbidden unless the owner explicitly changes this rule in a later message. Forbidden actions include but are not limited to:

- `git push`
- creating or updating pull requests
- creating issues, releases, tags, or remote branches
- uploading files to GitHub
- publishing project files to any external service
- using GitHub CLI or web actions that modify remote state

Read-only local git inspection is acceptable when needed, for example `git status`, `git diff`, or `git log`. Do not perform destructive git operations such as reset, checkout, clean, rebase, or branch deletion unless the owner explicitly requests that exact operation.

After adding or editing Unity scripts, do not manually compile scripts through external commands. Unity compiles scripts automatically. Do not run extra compile commands just to check whether a script compiles. If verification is needed, prefer explaining that Unity will compile after the editor refreshes. Run Unity tests or editor commands only when the owner explicitly asks or when the task cannot be completed responsibly without them.

Do not treat this file as limited to 400 words. Keep it clear and useful; completeness is more important than hitting a word count.
