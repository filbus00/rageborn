# Compile check without Unity

For cloud sessions that have no Unity editor: compiles `Scripts/Runtime`, `Scripts/Editor` and `Tests/EditMode` with
the .NET SDK against stand-ins of the Unity API, then runs the EditMode tests with a small NUnit stand-in.

```
Tools/CompileCheck/run.sh             # everything
Tools/CompileCheck/run.sh Legendary   # only tests whose name contains "Legendary"
```

It installs `dotnet-sdk-8.0` from the Ubuntu archive when `dotnet` is missing (NuGet and Microsoft's own download
hosts may be blocked; the archive is not, and nothing here needs NuGet).

- **Compile:** C# 9 and .NET Standard 2.1, as Unity 6 uses, with `UNITY_EDITOR`, `UNITY_IOS` and
  `DEVELOPMENT_BUILD` defined. A missing Unity type or member shows as an error: add it to `Stubs/` with Unity's real
  signature (check the Unity docs; a stub more generous than Unity hides real errors).
- **Stubs (`Stubs/`):** the Unity API the project uses. The math types (`Vector2`, `Mathf`, `Color32`,
  `Quaternion`, `RectInt` and so on) and `JsonUtility` (on the test build) behave like Unity's, so pure code runs.
  Everything else is an empty shell: `GameObject`, components, tilemaps and the editor API do nothing.
- **Tests:** `NUnit/` covers the slice of NUnit the tests use; `Runner/` runs them. Tests that need real
  GameObjects fail here (on 2026-10-01: the five `NavGridBakerTests`, which build tilemaps) and must be run in Unity.
- **Not checked:** anything Unity does at runtime: scenes, serialization of assets, rendering, input. It finds compile
  errors and wrong rules in pure code, nothing else.
