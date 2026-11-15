# Editor smoke checks

Run only in an empty, disposable Unity project. These checks create `Assets/Checks`, `Assets/Demo`, a small test PNG and project settings. A repeated run replaces `Assets/Checks`.

1. Import the release `.unitypackage` into the empty project.
2. Copy `FolderIconChecks.cs` into `Assets/Editor`.
3. Launch the project with Unity's `-batchmode -quit -executeMethod FolderIconChecks.Run` arguments.
4. Launch again with `-executeMethod FolderIconChecks.Persistence` to check persistence across processes.

`CHECKS-PASS.txt` records 78 successful assertions. `PERSISTENCE-PASS.txt` records the restart check. Exceptions fail the run. The checks cover rules, GUID overrides, rename/move, bulk edits, Undo/Redo, reset, custom unreadable PNG import, transparency, caching and Editor-only assembly targeting. Visual inspection of the Project window and customization window is also required.
