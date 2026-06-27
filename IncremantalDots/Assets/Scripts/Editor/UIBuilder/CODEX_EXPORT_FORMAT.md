# Codex UI Export Format

Codex should produce an export folder, not a ZIP, for the V2 importer.

```text
Assets/UIExports/<projectName>/
  manifest.json
  ui.json
  sprites/ optional
```

`manifest.json` requires:

- `projectName`: unique snake_case name.
- `sprites`: object mapping element path to a file under `sprites/`.
- `borders`: optional object mapping sprite file path to 9-slice borders.

`ui.json` requires:

- `canvas`
- `root`
- supported element types only: `Panel`, `Text`, `Image`, `Button`, `ScrollView`, `InputField`, `Slider`, `Toggle`, `Table`, `TabGroup`.

Codex should keep runtime behavior out of the export. Button actions may be described in `button.onClick`, but actual code wiring belongs to DeadWalls runtime scripts.
