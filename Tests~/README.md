# Validation

Copy `FolderIconChecks.cs` to `Assets/Editor/` in a disposable Unity project containing the package. Copy `Settings-1.1.json` to the project root. Run `FolderIconChecks.Run` via `-executeMethod`.

The checks create test folders and change package settings. Do not run in a production project.

127 assertions cover aliases, customization, Undo/Redo, GUID persistence, custom PNGs, caching, artwork, native icon bounds and automatic color migration. The migration fixture includes the original 1.1 defaults. Customized alias groups, manual colors and disabled rules must survive updates.

Run `FolderIconChecks.Persistence` after restarting the Editor to verify the saved customization. `FolderIconChecks.Export` creates the release unitypackage from `Assets/Blumgames/FolderIcon`.
