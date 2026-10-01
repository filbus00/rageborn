using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)] public sealed class MenuItem : Attribute { public MenuItem(string p) { } public MenuItem(string p, bool v) { } public MenuItem(string p, bool v, int pr) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class InitializeOnLoadAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class InitializeOnLoadMethodAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CustomEditor : Attribute { public CustomEditor(Type t) { } }
    public static class EditorPrefs { public static bool GetBool(string k, bool d = false) => d; public static void SetBool(string k, bool v) { } public static int GetInt(string k, int d = 0) => d; public static void SetInt(string k, int v) { } public static string GetString(string k, string d = "") => d; public static void SetString(string k, string v) { } public static float GetFloat(string k, float d = 0) => d; public static void SetFloat(string k, float v) { } public static bool HasKey(string k) => false; public static void DeleteKey(string k) { } }
    public static class SessionState { public static bool GetBool(string k, bool d) => d; public static void SetBool(string k, bool v) { } }
    public enum PlayModeStateChange { EnteredEditMode, ExitingEditMode, EnteredPlayMode, ExitingPlayMode }
    public static class EditorApplication
    {
        public delegate void CallbackFunction();
        public static bool isPlaying { get; set; } public static bool isPaused { get; set; } public static bool isCompiling => false; public static bool isUpdating => false; public static bool isPlayingOrWillChangePlaymode => false; public static double timeSinceStartup => 0;
        public static CallbackFunction update; public static CallbackFunction delayCall; public static event Action<PlayModeStateChange> playModeStateChanged; public static event Action quitting;
        public static void Step() { } public static bool ExecuteMenuItem(string m) => true; public static void QueuePlayerLoopUpdate() { } public static void ExitPlaymode() { } public static void EnterPlaymode() { }
    }
    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object o) { } public static void RevealInFinder(string p) { } public static bool DisplayDialog(string t, string m, string ok) => true; public static bool DisplayDialog(string t, string m, string ok, string c) => true; public static int DisplayDialogComplex(string t, string m, string a, string b, string c) => 0;
        public static void DisplayProgressBar(string t, string i, float p) { } public static bool DisplayCancelableProgressBar(string t, string i, float p) => false; public static void ClearProgressBar() { } public static string SaveFilePanel(string a, string b, string c, string d) => ""; public static string OpenFolderPanel(string a, string b, string c) => ""; public static void CopySerialized(UnityEngine.Object a, UnityEngine.Object b) { } public static bool IsPersistent(UnityEngine.Object o) => true; public static void UnloadUnusedAssetsImmediate() { } public static void FocusProjectWindow() { }
    }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string p) where T : UnityEngine.Object => null; public static UnityEngine.Object LoadAssetAtPath(string p, Type t) => null; public static UnityEngine.Object LoadMainAssetAtPath(string p) => null;
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string p) => new UnityEngine.Object[0]; public static UnityEngine.Object[] LoadAllAssetRepresentationsAtPath(string p) => new UnityEngine.Object[0];
        public static void CreateAsset(UnityEngine.Object o, string p) { } public static void AddObjectToAsset(UnityEngine.Object o, string p) { } public static void AddObjectToAsset(UnityEngine.Object o, UnityEngine.Object a) { } public static void RemoveObjectFromAsset(UnityEngine.Object o) { }
        public static void SaveAssets() { } public static void SaveAssetIfDirty(UnityEngine.Object o) { } public static void Refresh() { } public static void Refresh(ImportAssetOptions o) { } public static void ImportAsset(string p) { } public static void ImportAsset(string p, ImportAssetOptions o) { }
        public static bool DeleteAsset(string p) => true; public static bool CopyAsset(string a, string b) => true; public static string MoveAsset(string a, string b) => ""; public static string RenameAsset(string a, string b) => ""; public static bool IsValidFolder(string p) => true; public static string CreateFolder(string a, string b) => ""; public static string[] GetSubFolders(string p) => new string[0];
        public static string GetAssetPath(UnityEngine.Object o) => ""; public static string AssetPathToGUID(string p) => ""; public static string GUIDToAssetPath(string g) => ""; public static string[] FindAssets(string f) => new string[0]; public static string[] FindAssets(string f, string[] folders) => new string[0];
        public static T GetBuiltinExtraResource<T>(string p) where T : UnityEngine.Object => null; public static bool Contains(UnityEngine.Object o) => true; public static void StartAssetEditing() { } public static void StopAssetEditing() { } public static bool IsMainAsset(UnityEngine.Object o) => true; public static bool IsSubAsset(UnityEngine.Object o) => false; public static string GenerateUniqueAssetPath(string p) => p; public static void ForceReserializeAssets(IEnumerable<string> p) { } public static string[] GetDependencies(string p) => new string[0]; public static void ReleaseCachedFileHandles() { }
    }
    [Flags] public enum ImportAssetOptions { Default = 0, ForceUpdate = 1, ForceSynchronousImport = 8, ImportRecursive = 256, DontDownloadFromCacheServer = 8192, ForceUncompressedImport = 16384 }
    public class AssetImporter : UnityEngine.Object
    {
        public static AssetImporter GetAtPath(string p) => null; public string assetPath => ""; public string userData { get; set; } public string assetBundleName { get; set; }
        public void SaveAndReimport() { } public void AddRemap(SourceAssetIdentifier i, UnityEngine.Object o) { } public bool RemoveRemap(SourceAssetIdentifier i) => true; public Dictionary<SourceAssetIdentifier, UnityEngine.Object> GetExternalObjectMap() => new Dictionary<SourceAssetIdentifier, UnityEngine.Object>();
        public struct SourceAssetIdentifier { public SourceAssetIdentifier(UnityEngine.Object o) { type = null; name = ""; } public SourceAssetIdentifier(Type t, string n) { type = t; name = n; } public Type type; public string name; }
    }
    public enum TextureImporterType { Default = 0, NormalMap = 1, GUI = 2, Sprite = 8, Cursor = 7, Cookie = 4, Lightmap = 6, SingleChannel = 10 }
    public enum TextureImporterCompression { Uncompressed, Compressed, CompressedHQ, CompressedLQ }
    public enum SpriteImportMode { None, Single, Multiple, Polygon }
    public enum TextureImporterNPOTScale { None, ToNearest, ToLarger, ToSmaller }
    public enum TextureImporterAlphaSource { None, FromInput, FromGrayScale }
    public enum TextureImporterMipFilter { BoxFilter, KaiserFilter }
    public sealed class TextureImporterSettings
    {
        public int spriteAlignment { get; set; } public Vector2 spritePivot { get; set; } public SpriteMeshType spriteMeshType { get; set; } public uint spriteExtrude { get; set; } public bool spriteGenerateFallbackPhysicsShape { get; set; } public float spritePixelsPerUnit { get; set; } public int spriteMode { get; set; } public Vector4 spriteBorder { get; set; } public FilterMode filterMode { get; set; } public bool readable { get; set; } public bool mipmapEnabled { get; set; } public bool alphaIsTransparency { get; set; } public TextureImporterType textureType { get; set; } public TextureWrapMode wrapMode { get; set; } public bool sRGBTexture { get; set; } public TextureImporterNPOTScale npotScale { get; set; } public TextureImporterAlphaSource alphaSource { get; set; }
    }
    public sealed class TextureImporterPlatformSettings { public string name { get; set; } public bool overridden { get; set; } public int maxTextureSize { get; set; } public TextureImporterFormat format { get; set; } public TextureImporterCompression textureCompression { get; set; } }
    public enum TextureImporterFormat { Automatic = -1, RGBA32 = 4, ARGB32 = 5, ASTC_4x4 = 48 }
    [Serializable] public struct SpriteMetaData { public string name; public Rect rect; public int alignment; public Vector2 pivot; public Vector4 border; }
    public sealed class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; } public SpriteImportMode spriteImportMode { get; set; } public float spritePixelsPerUnit { get; set; } public Vector2 spritePivot { get; set; } public Vector4 spriteBorder { get; set; } public FilterMode filterMode { get; set; } public TextureImporterCompression textureCompression { get; set; } public bool mipmapEnabled { get; set; } public bool alphaIsTransparency { get; set; } public bool isReadable { get; set; } public TextureWrapMode wrapMode { get; set; } public bool sRGBTexture { get; set; } public int maxTextureSize { get; set; } public TextureImporterNPOTScale npotScale { get; set; } public TextureImporterAlphaSource alphaSource { get; set; } public SpriteMetaData[] spritesheet { get; set; } public string spritePackingTag { get; set; } public bool crunchedCompression { get; set; } public int compressionQuality { get; set; } public int anisoLevel { get; set; }
        public void ReadTextureSettings(TextureImporterSettings s) { } public void SetTextureSettings(TextureImporterSettings s) { } public void GetSourceTextureWidthAndHeight(out int w, out int h) { w = h = 0; }
        public TextureImporterPlatformSettings GetDefaultPlatformTextureSettings() => new TextureImporterPlatformSettings(); public void SetPlatformTextureSettings(TextureImporterPlatformSettings s) { } public TextureImporterPlatformSettings GetPlatformTextureSettings(string p) => new TextureImporterPlatformSettings(); public void ClearPlatformTextureSettings(string p) { }
    }
    public enum ModelImporterAnimationType { None, Legacy, Generic, Human }
    public enum ModelImporterAvatarSetup { NoAvatar, CreateFromThisModel, CopyFromOther }
    public enum ModelImporterMaterialImportMode { None, ImportStandard, ImportViaMaterialDescription }
    public enum ModelImporterMaterialLocation { External, InPrefab }
    public enum ModelImporterAnimationCompression { Off, KeyframeReduction, KeyframeReductionAndCompression, Optimal }
    public enum ClipAnimationMaskType { CreateFromThisModel, CopyFromOther, None }
    public sealed class ModelImporterClipAnimation
    {
        public string name { get; set; } public string takeName { get; set; } public float firstFrame { get; set; } public float lastFrame { get; set; } public bool loopTime { get; set; } public bool loopPose { get; set; } public bool loop { get; set; } public float cycleOffset { get; set; }
        public bool lockRootRotation { get; set; } public bool lockRootHeightY { get; set; } public bool lockRootPositionXZ { get; set; } public bool keepOriginalOrientation { get; set; } public bool keepOriginalPositionY { get; set; } public bool keepOriginalPositionXZ { get; set; } public bool heightFromFeet { get; set; } public bool mirror { get; set; } public float rotationOffset { get; set; } public float heightOffset { get; set; } public WrapMode wrapMode { get; set; } public ClipAnimationMaskType maskType { get; set; } public AvatarMask maskSource { get; set; }
    }
    public sealed class TakeInfo { public string name; public string defaultClipName; public float startTime; public float stopTime; }
    public sealed class ModelImporter : AssetImporter
    {
        public ModelImporterAnimationType animationType { get; set; } public ModelImporterAvatarSetup avatarSetup { get; set; } public Avatar sourceAvatar { get; set; } public bool importAnimation { get; set; } public ModelImporterMaterialImportMode materialImportMode { get; set; } public ModelImporterMaterialLocation materialLocation { get; set; } public ModelImporterClipAnimation[] clipAnimations { get; set; } public ModelImporterClipAnimation[] defaultClipAnimations => new ModelImporterClipAnimation[0]; public TakeInfo[] importedTakeInfos => new TakeInfo[0]; public float globalScale { get; set; } public bool useFileScale { get; set; } public bool isReadable { get; set; } public bool importBlendShapes { get; set; } public bool importCameras { get; set; } public bool importLights { get; set; } public ModelImporterAnimationCompression animationCompression { get; set; } public bool optimizeGameObjects { get; set; } public HumanDescription humanDescription { get; set; } public bool bakeAxisConversion { get; set; } public string motionNodeName { get; set; }
        public bool ExtractTextures(string folder) => true; public void SearchAndRemapMaterials(ModelImporterMaterialName a, ModelImporterMaterialSearch b) { }
    }
    public enum ModelImporterMaterialName { BasedOnTextureName, BasedOnMaterialName }
    public enum ModelImporterMaterialSearch { Local, RecursiveUp, Everywhere }
    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAsset(GameObject g, string p) => null; public static GameObject SaveAsPrefabAsset(GameObject g, string p, out bool ok) { ok = true; return null; } public static UnityEngine.Object InstantiatePrefab(UnityEngine.Object p) => null; public static UnityEngine.Object InstantiatePrefab(UnityEngine.Object p, Transform parent) => null; public static GameObject LoadPrefabContents(string p) => null; public static void UnloadPrefabContents(GameObject g) { } public static GameObject SaveAsPrefabAssetAndConnect(GameObject g, string p, InteractionMode m) => null; public static void RecordPrefabInstancePropertyModifications(UnityEngine.Object o) { } public static bool IsPartOfPrefabAsset(UnityEngine.Object o) => false;
    }
    public enum InteractionMode { AutomatedAction, UserAction }
    public static class Selection { public static UnityEngine.Object activeObject { get; set; } public static GameObject activeGameObject { get; set; } public static UnityEngine.Object[] objects { get; set; } public static T[] GetFiltered<T>(SelectionMode m) => new T[0]; public static UnityEngine.Object[] GetFiltered(Type t, SelectionMode m) => new UnityEngine.Object[0]; }
    [Flags] public enum SelectionMode { Unfiltered = 0, TopLevel = 1, Deep = 2, ExcludePrefab = 4, Editable = 8, Assets = 16, DeepAssets = 32 }
    public static class Undo { public static void RecordObject(UnityEngine.Object o, string n) { } public static void RegisterCreatedObjectUndo(UnityEngine.Object o, string n) { } public static void DestroyObjectImmediate(UnityEngine.Object o) { } }
    public enum SerializedPropertyType { Generic = -1, Integer = 0, Boolean = 1, Float = 2, String = 3, Color = 4, ObjectReference = 5, Enum = 7, Vector2 = 8, Vector3 = 9, ArraySize = 13 }
    public class SerializedProperty
    {
        public int intValue { get; set; } public long longValue { get; set; } public float floatValue { get; set; } public double doubleValue { get; set; } public bool boolValue { get; set; } public string stringValue { get; set; } public Color colorValue { get; set; } public UnityEngine.Object objectReferenceValue { get; set; } public uint uintValue { get; set; } public int enumValueIndex { get; set; } public int enumValueFlag { get; set; } public string[] enumNames => new string[0]; public Vector2 vector2Value { get; set; } public Vector3 vector3Value { get; set; } public Vector2Int vector2IntValue { get; set; } public Rect rectValue { get; set; } public int arraySize { get; set; } public bool isArray => false; public string name => ""; public string propertyPath => ""; public SerializedPropertyType propertyType => default; public object managedReferenceValue { get; set; }
        public SerializedProperty GetArrayElementAtIndex(int i) => new SerializedProperty(); public SerializedProperty FindPropertyRelative(string n) => new SerializedProperty(); public void InsertArrayElementAtIndex(int i) { } public void DeleteArrayElementAtIndex(int i) { } public void ClearArray() { } public bool Next(bool c) => false; public bool NextVisible(bool c) => false; public SerializedProperty Copy() => this;
    }
    public class SerializedObject : IDisposable
    {
        public SerializedObject(UnityEngine.Object o) { } public SerializedObject(UnityEngine.Object[] o) { } public UnityEngine.Object targetObject => null;
        public SerializedProperty FindProperty(string n) => new SerializedProperty(); public SerializedProperty GetIterator() => new SerializedProperty(); public bool ApplyModifiedProperties() => true; public bool ApplyModifiedPropertiesWithoutUndo() => true; public void Update() { } public void UpdateIfRequiredOrScript() { } public void Dispose() { }
    }
    public sealed class EditorBuildSettingsScene { public EditorBuildSettingsScene() { } public EditorBuildSettingsScene(string p, bool e) { path = p; enabled = e; } public string path { get; set; } public bool enabled { get; set; } public GUID guid { get; set; } }
    public struct GUID { public static GUID Generate() => default; public bool Empty() => true; public override string ToString() => ""; }
    public class SceneAsset : UnityEngine.Object { }
    public static class Menu { public static void SetChecked(string p, bool c) { } public static bool GetChecked(string p) => false; }
    public static class AssemblyReloadEvents { public delegate void AssemblyReloadCallback(); public static event AssemblyReloadCallback beforeAssemblyReload; public static event AssemblyReloadCallback afterAssemblyReload; }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } = new EditorBuildSettingsScene[0]; }
    public enum BuildTarget { StandaloneOSX = 2, StandaloneWindows = 5, iOS = 9, Android = 13, StandaloneWindows64 = 19, StandaloneLinux64 = 24 }
    public enum BuildTargetGroup { Unknown = 0, Standalone = 1, iOS = 4, Android = 7 }
    [Flags] public enum BuildOptions { None = 0, Development = 1, AutoRunPlayer = 4, ShowBuiltPlayer = 8, AllowDebugging = 512, ConnectWithProfiler = 256, SymlinkSources = 1024, AcceptExternalModificationsToPlayer = 32, CleanBuildCache = 1 << 22 }
    public struct BuildPlayerOptions { public string[] scenes { get; set; } public string locationPathName { get; set; } public BuildTarget target { get; set; } public BuildTargetGroup targetGroup { get; set; } public BuildOptions options { get; set; } }
    public static class BuildPipeline { public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o) => null; public static bool isBuildingPlayer => false; }
    public enum ScriptingImplementation { Mono2x, IL2CPP }
    public enum UIOrientation { Portrait, PortraitUpsideDown, LandscapeRight, LandscapeLeft, AutoRotation }
    public enum iOSSdkVersion { DeviceSDK, SimulatorSDK }
    public enum iOSTargetDevice { iPhoneOnly, iPadOnly, iPhoneAndiPad }
    public enum IconKind { Any = -1, Application = 0, Settings = 1, Notification = 2, Spotlight = 3, Store = 4 }
    public static class PlayerSettings
    {
        public static string companyName { get; set; } public static string productName { get; set; } public static string bundleVersion { get; set; } public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; } public static bool allowedAutorotateToPortraitUpsideDown { get; set; } public static bool allowedAutorotateToLandscapeLeft { get; set; } public static bool allowedAutorotateToLandscapeRight { get; set; } public static ColorSpace colorSpace { get; set; } public static bool runInBackground { get; set; }
        public static void SetApplicationIdentifier(BuildTargetGroup g, string id) { } public static void SetApplicationIdentifier(Build.NamedBuildTarget t, string id) { } public static string GetApplicationIdentifier(Build.NamedBuildTarget t) => "";
        public static void SetScriptingBackend(BuildTargetGroup g, ScriptingImplementation s) { } public static void SetScriptingBackend(Build.NamedBuildTarget t, ScriptingImplementation s) { }
        public static void SetIcons(BuildTargetGroup g, Texture2D[] i, IconKind k) { } public static void SetIcons(Build.NamedBuildTarget t, Texture2D[] i, IconKind k) { } public static void SetIcons(Build.NamedBuildTarget t, Texture2D[] i) { } public static int[] GetIconSizes(Build.NamedBuildTarget t, IconKind k) => new int[0]; public static int[] GetIconSizesForTargetGroup(BuildTargetGroup g) => new int[0];
        public static class iOS { public static iOSSdkVersion sdkVersion { get; set; } public static string targetOSVersionString { get; set; } public static iOSTargetDevice targetDevice { get; set; } public static bool hideHomeButton { get; set; } public static bool appleEnableAutomaticSigning { get; set; } public static string appleDeveloperTeamID { get; set; } public static string buildNumber { get; set; } public static bool requiresFullScreen { get; set; } }
    }
    public static class AnimationUtility { public static EditorCurveBinding[] GetCurveBindings(AnimationClip c) => new EditorCurveBinding[0]; public static AnimationCurve GetEditorCurve(AnimationClip c, EditorCurveBinding b) => null; public static void SetEditorCurve(AnimationClip c, EditorCurveBinding b, AnimationCurve a) { } public static AnimationClipSettings GetAnimationClipSettings(AnimationClip c) => new AnimationClipSettings(); public static void SetAnimationClipSettings(AnimationClip c, AnimationClipSettings s) { } }
    public sealed class AnimationClipSettings { public bool loopTime { get; set; } public float startTime { get; set; } public float stopTime { get; set; } }
    public struct EditorCurveBinding { public string path; public Type type; public string propertyName; public static EditorCurveBinding FloatCurve(string p, Type t, string n) => default; }
    public class Editor : ScriptableObject { public UnityEngine.Object target => null; public virtual void OnInspectorGUI() { } public bool DrawDefaultInspector() => true; }
    public class EditorWindow : ScriptableObject { public static T GetWindow<T>() where T : EditorWindow => null; public void Show() { } public void Close() { } public void Repaint() { } public GUIContent titleContent { get; set; } }
    public class AssetPostprocessor { public string assetPath => ""; public AssetImporter assetImporter => null; }
    public class AvatarMask : UnityEngine.Object { }
    public struct HumanDescription { public HumanBone[] human; public SkeletonBone[] skeleton; }
    public struct HumanBone { public string boneName; public string humanName; }
    public struct SkeletonBone { public string name; public Vector3 position; public Quaternion rotation; public Vector3 scale; }
    public static class FileUtil { public static bool DeleteFileOrDirectory(string p) => true; public static void CopyFileOrDirectory(string a, string b) { } }
    public static class EditorGUIUtility { public static T Load<T>(string p) where T : UnityEngine.Object => null; }
}
namespace UnityEngine { public class GUIContent { public GUIContent() { } public GUIContent(string t) { } } public class AvatarMask : Object { } }
namespace UnityEditor.Build { public readonly struct NamedBuildTarget { public static readonly NamedBuildTarget iOS = default; public static readonly NamedBuildTarget Unknown = default; public static readonly NamedBuildTarget Standalone = default; public static readonly NamedBuildTarget Android = default; public static NamedBuildTarget FromBuildTargetGroup(BuildTargetGroup g) => default; public string TargetName => ""; } }
namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
    public struct BuildSummary { public BuildResult result; public ulong totalSize; public TimeSpan totalTime; public int totalErrors; public int totalWarnings; public string outputPath; }
    public class BuildReport : UnityEngine.Object { public BuildSummary summary => default; public BuildStep[] steps => new BuildStep[0]; }
    public struct BuildStep { public string name; public BuildStepMessage[] messages; }
    public struct BuildStepMessage { public LogType type; public string content; }
}
namespace UnityEditor.SceneManagement
{
    using UnityEngine.SceneManagement;
    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading }
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static Scene OpenScene(string p) => default; public static Scene OpenScene(string p, OpenSceneMode m) => default; public static Scene NewScene(NewSceneSetup s) => default; public static Scene NewScene(NewSceneSetup s, NewSceneMode m) => default; public static Scene NewPreviewScene() => default; public static bool ClosePreviewScene(Scene s) => true; public static bool CloseScene(Scene s, bool r) => true;
        public static bool SaveScene(Scene s) => true; public static bool SaveScene(Scene s, string p) => true; public static bool SaveScene(Scene s, string p, bool copy) => true; public static bool SaveOpenScenes() => true; public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true; public static bool MarkSceneDirty(Scene s) => true; public static bool MarkAllScenesDirty() => true; public static Scene GetActiveScene() => default;
        public static bool SaveModifiedScenesIfUserWantsTo(Scene[] s) => true;
    }
}
namespace UnityEditor.U2D.Sprites
{
    public interface ISpriteEditorDataProvider { void InitSpriteEditorDataProvider(); SpriteRect[] GetSpriteRects(); void SetSpriteRects(SpriteRect[] r); void Apply(); T GetDataProvider<T>() where T : class; }
    public interface ISpriteNameFileIdDataProvider { IEnumerable<SpriteNameFileIdPair> GetNameFileIdPairs(); void SetNameFileIdPairs(IEnumerable<SpriteNameFileIdPair> p); }
    public class SpriteNameFileIdPair { public SpriteNameFileIdPair(string n, GUID g) { } public string name { get; set; } public GUID fileId { get; set; } }
    public class SpriteRect { public string name { get; set; } public Rect rect { get; set; } public SpriteAlignment alignment { get; set; } public Vector2 pivot { get; set; } public Vector4 border { get; set; } public GUID spriteID { get; set; } }
    public class SpriteDataProviderFactories { public void Init() { } public ISpriteEditorDataProvider GetSpriteEditorDataProviderFromObject(UnityEngine.Object o) => null; }
}
namespace UnityEditor.TestTools.TestRunner.Api
{
    public enum TestMode { EditMode = 1, PlayMode = 2 }
    public enum TestStatus { Skipped, Passed, Failed, Inconclusive }
    public class Filter { public TestMode testMode; public string[] testNames; public string[] groupNames; public string[] assemblyNames; }
    public class ExecutionSettings { public ExecutionSettings(params Filter[] f) { } public bool runSynchronously { get; set; } }
    public interface ITestAdaptor { string Name { get; } string FullName { get; } bool HasChildren { get; } IEnumerable<ITestAdaptor> Children { get; } bool IsSuite { get; } }
    public interface ITestResultAdaptor { ITestAdaptor Test { get; } string Name { get; } string FullName { get; } TestStatus TestStatus { get; } string ResultState { get; } string Message { get; } string StackTrace { get; } int PassCount { get; } int FailCount { get; } int SkipCount { get; } int InconclusiveCount { get; } bool HasChildren { get; } IEnumerable<ITestResultAdaptor> Children { get; } double Duration { get; } string Output { get; } }
    public interface ICallbacks { void RunStarted(ITestAdaptor t); void RunFinished(ITestResultAdaptor r); void TestStarted(ITestAdaptor t); void TestFinished(ITestResultAdaptor r); }
    public class TestRunnerApi : ScriptableObject { public string Execute(ExecutionSettings s) => ""; public void RegisterCallbacks<T>(T c, int p = 0) where T : ICallbacks { } public void UnregisterCallbacks<T>(T c) where T : ICallbacks { } }
}
