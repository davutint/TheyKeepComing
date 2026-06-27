# DeadWalls UI Importer V2 Architecture

## Purpose

DeadWalls UI Importer turns Codex-authored UI export folders into Unity uGUI/TMP prefabs.
The importer owns visual prefab creation only. Runtime controller logic, event wiring, and ECS/MonoBehaviour integration stay in normal DeadWalls scripts.

## Pipeline

```text
Assets/UIExports/<projectName>/
  ui.json
  manifest.json
  sprites/ optional
        |
        v
Window > DeadWalls > UI Importer
        |
        v
Strict validation -> preview -> optional sprite assignment -> prefab import
        |
        v
Assets/Prefabs/UI/Generated/<RootName>.prefab
Assets/Sprites/UI/Generated/<projectName>/ optional imported sprites
```

## Main Pieces

- `UIImporterWindow.cs`: UI Toolkit editor window, export-folder loading, preview, sprite slots, hierarchy, reports.
- `UIImporterExportFolder.cs`: strict manifest/ui validation and optional sprite-file import.
- `UIImporterJsonParser.cs`: lightweight recursive parser for `ui.json`.
- `UIImporterBuilder.cs`: hidden preview canvas and final prefab creation.
- `UIImporterElementFactory.cs`: uGUI/TMP element builders.
- `UIImporterAssetResolver.cs`: TMP font and existing sprite lookup.
- `UIImporterWindow.uss`: DeadWalls editor styling for the tool window.

`UIImporterZipHandler.cs` remains as legacy support code, but the V2 primary workflow is export-folder based.

## Validation Rules

- `manifest.json` and `ui.json` are required.
- `projectName` is required, snake_case, 3-64 chars.
- Unknown element types fail validation.
- Duplicate element paths fail validation.
- Sprite paths cannot be absolute and cannot contain `.` or `..` path segments.
- Sprite slots without manifest assignments are warnings, not blockers.
- Manifest mappings that do not point at declared sprite slots are warnings.

## Integration Boundary

The importer reports interactive paths and suggested field names. It does not generate scripts and does not auto-bind controller fields.
Codex or the developer wires runtime behavior in repo-specific controllers such as `HUDController`, `BuildingDetailUI`, or future UI controllers.
