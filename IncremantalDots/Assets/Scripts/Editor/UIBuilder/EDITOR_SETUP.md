# DeadWalls UI Importer V2 Editor Setup

## Open

`Window > DeadWalls > UI Importer`

You can also right-click a valid export folder in Project view and use:

`Assets > DeadWalls > Load UI Export Folder`

## Export Folder Format

```text
Assets/UIExports/<projectName>/
  manifest.json
  ui.json
  sprites/ optional
```

Minimum `manifest.json`:

```json
{
  "projectName": "building_detail_panel",
  "sprites": {},
  "borders": {}
}
```

## Workflow

1. Ask Codex for a DeadWalls UI export.
2. Put the export under `Assets/UIExports/<projectName>/`.
3. Open the importer and load the export folder.
4. Fix validation errors if any.
5. Review preview, sprite slots, hierarchy, and integration report.
6. Assign missing sprites manually or rely on Auto-Find from existing `Assets/Sprites/UI` assets.
7. Click `Import Prefab`.

Generated outputs:

```text
Assets/Prefabs/UI/Generated/
Assets/Sprites/UI/Generated/<projectName>/
```

## Runtime Binding Policy

V2 does not use automatic field binding and does not generate controller scripts.
The importer only reports paths for buttons, text, inputs, sliders, and toggles. Runtime behavior should be wired manually or by Codex in normal DeadWalls MonoBehaviour scripts.

## Common Issues

### Missing ui.json

The folder is not a valid export. Add `ui.json` beside `manifest.json`.

### Invalid projectName

Use lowercase snake_case only, for example `building_detail_panel`.

### Missing sprite assignment

This is a warning. The prefab can still import. Assign a Sprite in the slot field or add a matching entry/file later.

### Sprite path rejected

Manifest sprite values must be relative paths under the optional `sprites/` folder. Do not use absolute paths, empty path segments, `.` or `..`.

### Controller fields are not filled

Expected. Use the integration report as a wiring checklist for the runtime UI controller.
