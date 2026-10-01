using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public static class Time
    {
        public static float deltaTime => 0.016f; public static float unscaledDeltaTime => 0.016f; public static float fixedDeltaTime { get; set; } = 0.02f;
        public static float time => 0; public static float unscaledTime => 0; public static float realtimeSinceStartup => 0; public static double timeAsDouble => 0; public static double unscaledTimeAsDouble => 0;
        public static float timeSinceLevelLoad => 0; public static float timeScale { get; set; } = 1; public static int frameCount => 0; public static float captureDeltaTime { get; set; } public static float smoothDeltaTime => 0.016f; public static float maximumDeltaTime { get; set; }
        public static float fixedTime => 0; public static float fixedUnscaledTime => 0;
    }
    public enum LogType { Error, Assert, Warning, Log, Exception }
    public static class Debug
    {
        public static void Log(object m) { } public static void Log(object m, Object c) { }
        public static void LogWarning(object m) { } public static void LogWarning(object m, Object c) { }
        public static void LogError(object m) { } public static void LogError(object m, Object c) { }
        public static void LogException(Exception e) { } public static void LogException(Exception e, Object c) { }
        public static void LogFormat(string f, params object[] a) { } public static void LogWarningFormat(string f, params object[] a) { } public static void LogErrorFormat(string f, params object[] a) { }
        public static void Assert(bool c) { } public static void Assert(bool c, object m) { }
        public static void DrawLine(Vector3 a, Vector3 b) { } public static void DrawLine(Vector3 a, Vector3 b, Color c) { } public static void DrawLine(Vector3 a, Vector3 b, Color c, float d) { } public static void DrawRay(Vector3 a, Vector3 b, Color c) { }
        public static bool isDebugBuild => true;
    }
    public enum RuntimePlatform { OSXEditor, OSXPlayer, WindowsPlayer, WindowsEditor, IPhonePlayer, Android, LinuxPlayer, LinuxEditor }
    public enum SystemLanguage { English }
    public static class Application
    {
        public static string persistentDataPath => "/tmp"; public static string dataPath => "/tmp"; public static string temporaryCachePath => "/tmp"; public static string streamingAssetsPath => "/tmp";
        public static bool isPlaying => false; public static bool isEditor => true; public static bool isFocused => true; public static bool isMobilePlatform => false; public static bool isBatchMode => false;
        public static int targetFrameRate { get; set; } public static RuntimePlatform platform => RuntimePlatform.LinuxEditor; public static string version => "0"; public static string productName => "x"; public static string identifier => "x"; public static string unityVersion => "6000";
        public static bool runInBackground { get; set; }
        public static void Quit() { } public static void OpenURL(string u) { }
        public static event Action<bool> focusChanged; public static event Action quitting; public static event Action<string, string, LogType> logMessageReceived; public static event Action lowMemory;
        public static SystemLanguage systemLanguage => SystemLanguage.English;
    }
    public enum ScreenOrientation { Portrait = 1, PortraitUpsideDown = 2, LandscapeLeft = 3, LandscapeRight = 4, AutoRotation = 5 }
    public struct Resolution { public int width, height; public int refreshRate; }
    public static class Screen
    {
        public static int width => 1170; public static int height => 2532; public static float dpi => 460; public static Rect safeArea => new Rect(0, 0, 1170, 2532);
        public static ScreenOrientation orientation { get; set; } public static bool autorotateToPortrait { get; set; } public static bool autorotateToPortraitUpsideDown { get; set; } public static bool autorotateToLandscapeLeft { get; set; } public static bool autorotateToLandscapeRight { get; set; }
        public static int sleepTimeout { get; set; } public static Resolution currentResolution => default; public static bool fullScreen { get; set; }
        public static void SetResolution(int w, int h, bool f) { }
    }
    public static class SleepTimeout { public const int NeverSleep = -1; public const int SystemSetting = -2; }
    public static class QualitySettings { public static ColorSpace activeColorSpace => ColorSpace.Linear; public static int vSyncCount { get; set; } public static int antiAliasing { get; set; } public static int GetQualityLevel() => 0; }
    public static class SystemInfo { public static string deviceModel => "x"; public static int systemMemorySize => 4096; public static string operatingSystem => "x"; public static int processorCount => 4; public static string deviceUniqueIdentifier => "x"; public static int graphicsMemorySize => 1; }
    public class ResourceRequest : AsyncOperation { public Object asset => null; }
    public static class Resources
    {
        public static T Load<T>(string p) where T : Object => null; public static Object Load(string p) => null; public static Object Load(string p, Type t) => null;
        public static T[] LoadAll<T>(string p) where T : Object => new T[0]; public static Object[] LoadAll(string p) => new Object[0]; public static Object[] LoadAll(string p, Type t) => new Object[0];
        public static ResourceRequest LoadAsync<T>(string p) where T : Object => null;
        public static AsyncOperation UnloadUnusedAssets() => null; public static void UnloadAsset(Object o) { }
        public static T GetBuiltinResource<T>(string p) where T : Object => null;
        public static T[] FindObjectsOfTypeAll<T>() where T : Object => new T[0];
    }
    public class TextAsset : Object { public TextAsset() { } public TextAsset(string t) { } public string text => ""; public byte[] bytes => new byte[0]; }
    public static class PlayerPrefs { public static int GetInt(string k, int d = 0) => d; public static void SetInt(string k, int v) { } public static float GetFloat(string k, float d = 0) => d; public static void SetFloat(string k, float v) { } public static string GetString(string k, string d = "") => d; public static void SetString(string k, string v) { } public static bool HasKey(string k) => false; public static void DeleteKey(string k) { } public static void Save() { } }
#if NET5_0_OR_GREATER
    // Unity's rules, close enough for the tests: public fields and [SerializeField] ones, never properties or
    // [NonSerialized]; enums as numbers; unknown keys ignored; after reading, no null strings, lists, arrays or
    // [Serializable] classes (Unity always fills them).
    public static class JsonUtility
    {
        static System.Text.Json.JsonSerializerOptions Options(bool pretty)
        {
            var resolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(info =>
            {
                if (info.Kind != System.Text.Json.Serialization.Metadata.JsonTypeInfoKind.Object) return;
                for (var i = info.Properties.Count - 1; i >= 0; i--)
                {
                    var p = info.Properties[i];
                    if (p.AttributeProvider is System.Reflection.PropertyInfo) { info.Properties.RemoveAt(i); continue; }
                    if (p.AttributeProvider is System.Reflection.FieldInfo f && (f.IsNotSerialized || f.IsStatic)) info.Properties.RemoveAt(i);
                }
                foreach (var f in info.Type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
                {
                    if (!f.IsDefined(typeof(SerializeField), true)) continue;
                    var field = f;
                    var p = info.CreateJsonPropertyInfo(f.FieldType, f.Name);
                    p.Get = o => field.GetValue(o);
                    p.Set = (o, v) => field.SetValue(o, v);
                    info.Properties.Add(p);
                }
            });
            return new System.Text.Json.JsonSerializerOptions { IncludeFields = true, WriteIndented = pretty, TypeInfoResolver = resolver, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };
        }
        public static string ToJson(object o) => ToJson(o, false);
        public static string ToJson(object o, bool prettyPrint) => o == null ? "" : System.Text.Json.JsonSerializer.Serialize(o, o.GetType(), Options(prettyPrint));
        public static T FromJson<T>(string j) => (T)FromJson(j, typeof(T));
        public static object FromJson(string j, Type t)
        {
            if (string.IsNullOrEmpty(j)) return null;
            var o = System.Text.Json.JsonSerializer.Deserialize(j, t, Options(false));
            Fill(o, 0);
            return o;
        }
        public static void FromJsonOverwrite(string j, object o)
        {
            var read = FromJson(j, o.GetType());
            foreach (var f in o.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                f.SetValue(o, f.GetValue(read));
        }
        static void Fill(object o, int depth)
        {
            if (o == null || depth > 10) return;
            foreach (var f in o.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (f.IsNotSerialized || (!f.IsPublic && !f.IsDefined(typeof(SerializeField), true))) continue;
                var t = f.FieldType; var v = f.GetValue(o);
                if (v == null)
                {
                    if (t == typeof(string)) v = "";
                    else if (t.IsArray) v = Array.CreateInstance(t.GetElementType(), 0);
                    else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>)) v = Activator.CreateInstance(t);
                    else if (t.IsClass && t.IsSerializable && !typeof(Object).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null) v = Activator.CreateInstance(t);
                    f.SetValue(o, v);
                }
                if (v is System.Collections.IList list) { foreach (var e in list) if (e != null && !(e is string) && e.GetType().IsClass) Fill(e, depth + 1); }
                else if (v != null && t.IsClass && t != typeof(string) && t.IsSerializable) Fill(v, depth + 1);
            }
        }
    }
#else
    public static class JsonUtility { public static string ToJson(object o) => "{}"; public static string ToJson(object o, bool prettyPrint) => "{}"; public static T FromJson<T>(string j) => default; public static object FromJson(string j, Type t) => null; public static void FromJsonOverwrite(string j, object o) { } }
#endif
    public static class GUIUtility { public static int hotControl { get; set; } }
    public static class Gizmos { public static Color color { get; set; } public static void DrawLine(Vector3 a, Vector3 b) { } public static void DrawWireSphere(Vector3 c, float r) { } public static void DrawWireCube(Vector3 c, Vector3 s) { } public static void DrawSphere(Vector3 c, float r) { } public static void DrawCube(Vector3 c, Vector3 s) { } public static Matrix4x4 matrix { get; set; } }

    // Rendering
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum TextureFormat { Alpha8 = 1, RGB24 = 3, RGBA32 = 4, ARGB32 = 5, RGBAHalf = 17, RGBAFloat = 20, R8 = 63 }
    public enum SpriteMeshType { FullRect, Tight }
    public enum SpriteAlignment { Center, TopLeft, TopCenter, TopRight, LeftCenter, RightCenter, BottomLeft, BottomCenter, BottomRight, Custom }
    public enum SpriteDrawMode { Simple, Sliced, Tiled }
    public enum SpriteMaskInteraction { None, VisibleInsideMask, VisibleOutsideMask }
    public enum SpriteSortPoint { Center, Pivot }
    public enum RenderTextureFormat { ARGB32, Depth, ARGBHalf, Default = 7, DefaultHDR = 9, ARGBFloat = 11 }
    public enum RenderTextureReadWrite { Default, Linear, sRGB }
    public enum CameraClearFlags { Skybox = 1, Color = 2, SolidColor = 2, Depth = 3, Nothing = 4 }
    public enum TransparencySortMode { Default, Perspective, Orthographic, CustomAxis }
    public enum LightType { Spot, Directional, Point, Area, Rectangle = 3, Disc = 4 }
    public enum LightShadows { None, Hard, Soft }
    public enum AnisotropicFiltering { Disable, Enable, ForceEnable }
    public enum ColorSpace { Uninitialized = -1, Gamma, Linear }

    public class Shader : Object { public static int PropertyToID(string n) => 0; public static Shader Find(string n) => null; public static void SetGlobalFloat(string n, float v) { } public static void SetGlobalFloat(int n, float v) { } public static void SetGlobalColor(int n, Color c) { } }
    public class Texture : Object { public int width { get; set; } public int height { get; set; } public FilterMode filterMode { get; set; } public TextureWrapMode wrapMode { get; set; } public int anisoLevel { get; set; } public bool isReadable => true; public int mipmapCount => 1; }
    public sealed class Texture2D : Texture
    {
        public Texture2D(int w, int h) { width = w; height = h; } public Texture2D(int w, int h, TextureFormat f, bool mip) { width = w; height = h; } public Texture2D(int w, int h, TextureFormat f, bool mip, bool linear) { width = w; height = h; }
        public TextureFormat format => TextureFormat.RGBA32;
        public static Texture2D whiteTexture => new Texture2D(4, 4); public static Texture2D blackTexture => new Texture2D(4, 4);
        public void SetPixel(int x, int y, Color c) { } public Color GetPixel(int x, int y) => default; public Color GetPixelBilinear(float u, float v) => default;
        public void SetPixels(Color[] c) { } public void SetPixels(int x, int y, int w, int h, Color[] c) { } public void SetPixels32(Color32[] c) { } public void SetPixels32(int x, int y, int w, int h, Color32[] c) { }
        public Color[] GetPixels() => new Color[width * height]; public Color[] GetPixels(int x, int y, int w, int h) => new Color[w * h]; public Color32[] GetPixels32() => new Color32[width * height];
        public void Apply() { } public void Apply(bool mip) { } public void Apply(bool mip, bool makeNoLongerReadable) { }
        public bool Reinitialize(int w, int h) => true; public bool Reinitialize(int w, int h, TextureFormat f, bool m) => true;
        public void ReadPixels(Rect r, int x, int y) { } public void ReadPixels(Rect r, int x, int y, bool m) { }
        public byte[] GetRawTextureData() => new byte[0]; public Unity.Collections.NativeArray<T> GetRawTextureData<T>() where T : struct => default;
        public void LoadRawTextureData(byte[] d) { } public bool LoadImage(byte[] d) => true; public bool LoadImage(byte[] d, bool m) => true; public byte[] EncodeToPNG() => new byte[0];
    }
    public class RenderTexture : Texture
    {
        public RenderTexture(int w, int h, int d) { } public RenderTexture(RenderTextureDescriptor d) { } public RenderTexture(int w, int h, int d, RenderTextureFormat f) { } public RenderTexture(int w, int h, int d, RenderTextureFormat f, RenderTextureReadWrite rw) { }
        public static RenderTexture active { get; set; } public static RenderTexture GetTemporary(int w, int h, int d) => null; public static RenderTexture GetTemporary(int w, int h, int d, RenderTextureFormat f) => null; public static RenderTexture GetTemporary(int w, int h, int d, RenderTextureFormat f, RenderTextureReadWrite rw) => null; public static void ReleaseTemporary(RenderTexture t) { }
        public bool Create() => true; public void Release() { } public int antiAliasing { get; set; } public int depth { get; set; }
    }
    public struct RenderTextureDescriptor { public RenderTextureDescriptor(int w, int h) { width = w; height = h; colorFormat = default; depthBufferBits = 0; msaaSamples = 1; sRGB = false; graphicsFormat = default; useMipMap = false; } public RenderTextureDescriptor(int w, int h, RenderTextureFormat f, int d) : this(w, h) { colorFormat = f; depthBufferBits = d; } public int width; public int height; public RenderTextureFormat colorFormat; public int depthBufferBits; public int msaaSamples; public bool sRGB; public Experimental.Rendering.GraphicsFormat graphicsFormat; public bool useMipMap; }
    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 p) => new Sprite(); public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) => new Sprite();
        public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude) => new Sprite(); public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude, SpriteMeshType m) => new Sprite();
        public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude, SpriteMeshType m, Vector4 border) => new Sprite();
        public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude, SpriteMeshType m, Vector4 border, bool physics) => new Sprite();
        public Texture2D texture => null; public Rect rect => default; public Rect textureRect => default; public Vector2 pivot => default; public float pixelsPerUnit => 100; public Bounds bounds => default; public Vector4 border => default;
        public Vector2[] vertices => new Vector2[0]; public ushort[] triangles => new ushort[0]; public Vector2[] uv => new Vector2[0];
    }
    public class Material : Object
    {
        public Material(Shader s) { } public Material(Material m) { }
        public Shader shader { get; set; } public Color color { get; set; } public Texture mainTexture { get; set; } public int renderQueue { get; set; }
        public void SetColor(string n, Color c) { } public void SetColor(int n, Color c) { } public void SetFloat(string n, float v) { } public void SetFloat(int n, float v) { } public void SetTexture(string n, Texture t) { } public void SetTexture(int n, Texture t) { } public void SetInt(string n, int v) { } public void SetVector(string n, Vector4 v) { }
        public Color GetColor(string n) => default; public float GetFloat(string n) => 0; public bool HasProperty(string n) => false; public bool HasProperty(int n) => false;
        public void EnableKeyword(string k) { } public void DisableKeyword(string k) { }
    }
    public sealed class MaterialPropertyBlock
    {
        public void SetColor(string n, Color c) { } public void SetColor(int n, Color c) { } public void SetFloat(string n, float v) { } public void SetFloat(int n, float v) { } public void SetTexture(int n, Texture t) { } public void SetTexture(string n, Texture t) { } public void SetVector(int n, Vector4 v) { } public void Clear() { }
    }
    public class Mesh : Object
    {
        public Vector3[] vertices { get; set; } public int[] triangles { get; set; } public Vector2[] uv { get; set; } public Vector3[] normals { get; set; } public Color[] colors { get; set; } public Bounds bounds { get; set; } public int vertexCount => 0; public int subMeshCount { get; set; }
        public void RecalculateBounds() { } public void RecalculateNormals() { } public void Clear() { } public void SetVertices(List<Vector3> v) { } public void SetTriangles(int[] t, int s) { } public void SetUVs(int c, List<Vector2> u) { }
        public void MarkDynamic() { } public bool isReadable => true; public void UploadMeshData(bool b) { } public int[] GetTriangles(int s) => new int[0];
    }
    public class Renderer : Component
    {
        public bool enabled { get; set; } public Material material { get; set; } public Material sharedMaterial { get; set; } public Material[] materials { get; set; } public Material[] sharedMaterials { get; set; }
        public int sortingOrder { get; set; } public string sortingLayerName { get; set; } public int sortingLayerID { get; set; } public Bounds bounds => default; public bool isVisible => true;
        public void SetPropertyBlock(MaterialPropertyBlock b) { } public void GetPropertyBlock(MaterialPropertyBlock b) { } public bool HasPropertyBlock() => false;
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; } public bool receiveShadows { get; set; } public int renderingLayerMask { get; set; }
    }
    public sealed class SpriteRenderer : Renderer
    {
        public Sprite sprite { get; set; } public Color color { get; set; } public bool flipX { get; set; } public bool flipY { get; set; } public SpriteDrawMode drawMode { get; set; } public Vector2 size { get; set; }
        public SpriteMaskInteraction maskInteraction { get; set; } public SpriteSortPoint spriteSortPoint { get; set; }
    }
    public sealed class MeshRenderer : Renderer { }
    public sealed class SkinnedMeshRenderer : Renderer { public Mesh sharedMesh { get; set; } public Transform[] bones { get; set; } public Transform rootBone { get; set; } public void BakeMesh(Mesh m) { } public void BakeMesh(Mesh m, bool useScale) { } public bool updateWhenOffscreen { get; set; } }
    public sealed class MeshFilter : Component { public Mesh mesh { get; set; } public Mesh sharedMesh { get; set; } }
    public sealed class LineRenderer : Renderer { public int positionCount { get; set; } public float startWidth { get; set; } public float endWidth { get; set; } public Color startColor { get; set; } public Color endColor { get; set; } public bool useWorldSpace { get; set; } public void SetPosition(int i, Vector3 p) { } public void SetPositions(Vector3[] p) { } public float widthMultiplier { get; set; } }
    public sealed class TrailRenderer : Renderer { }
    public sealed class SpriteMask : Renderer { public Sprite sprite { get; set; } }
    public sealed class TextMesh : Component { public string text { get; set; } public Color color { get; set; } public int fontSize { get; set; } public float characterSize { get; set; } public TextAnchor anchor { get; set; } public TextAlignment alignment { get; set; } public Font font { get; set; } public FontStyle fontStyle { get; set; } public float offsetZ { get; set; } public float lineSpacing { get; set; } public float tabSize { get; set; } public bool richText { get; set; } }
    public enum TextAlignment { Left, Center, Right }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public sealed class Font : Object { public Font() { } public Font(string n) { } public static Font CreateDynamicFontFromOSFont(string n, int s) => new Font(); public static Font CreateDynamicFontFromOSFont(string[] n, int s) => new Font(); public static string[] GetOSInstalledFontNames() => new string[0]; public Material material { get; set; } public bool dynamic => true; public int fontSize => 16; }
    public sealed class Camera : Behaviour
    {
        public static Camera main => null; public static Camera current => null; public static Camera[] allCameras => new Camera[0];
        public float orthographicSize { get; set; } public bool orthographic { get; set; } public float aspect { get; set; } public Color backgroundColor { get; set; } public CameraClearFlags clearFlags { get; set; }
        public float nearClipPlane { get; set; } public float farClipPlane { get; set; } public float fieldOfView { get; set; } public RenderTexture targetTexture { get; set; } public Rect rect { get; set; } public Rect pixelRect { get; set; } public int pixelWidth => 0; public int pixelHeight => 0; public int cullingMask { get; set; } public float depth { get; set; }
        public TransparencySortMode transparencySortMode { get; set; } public Vector3 transparencySortAxis { get; set; }
        public Vector3 WorldToScreenPoint(Vector3 p) => p; public Vector3 ScreenToWorldPoint(Vector3 p) => p; public Vector3 WorldToViewportPoint(Vector3 p) => p; public Vector3 ViewportToWorldPoint(Vector3 p) => p; public Vector3 ScreenToViewportPoint(Vector3 p) => p; public Vector3 ViewportToScreenPoint(Vector3 p) => p; public Ray ScreenPointToRay(Vector3 p) => default;
        public void Render() { } public bool allowHDR { get; set; } public bool allowMSAA { get; set; } public Matrix4x4 projectionMatrix { get; set; } public Matrix4x4 worldToCameraMatrix { get; set; } public void ResetProjectionMatrix() { } public void ResetAspect() { }
        public Rendering.OpaqueSortMode opaqueSortMode { get; set; } public SceneManagement.Scene scene { get; set; } public int targetDisplay { get; set; }
    }
    public struct Ray { public Vector3 origin; public Vector3 direction; public Ray(Vector3 o, Vector3 d) { origin = o; direction = d; } public Vector3 GetPoint(float d) => origin + direction * d; }
    public sealed class Light : Behaviour { public LightType type { get; set; } public Color color { get; set; } public float intensity { get; set; } public float range { get; set; } public float spotAngle { get; set; } public LightShadows shadows { get; set; } public int cullingMask { get; set; } public float bounceIntensity { get; set; } }
    public static class RenderSettings { public static Color ambientLight { get; set; } public static Rendering.AmbientMode ambientMode { get; set; } public static Color ambientSkyColor { get; set; } }
    public sealed class Canvas : Behaviour { public RenderMode renderMode { get; set; } public int sortingOrder { get; set; } public bool overrideSorting { get; set; } public Camera worldCamera { get; set; } public float scaleFactor { get; set; } public float planeDistance { get; set; } public Canvas rootCanvas => this; public bool pixelPerfect { get; set; } public string sortingLayerName { get; set; } public int sortingLayerID { get; set; } public static void ForceUpdateCanvases() { } public float referencePixelsPerUnit { get; set; } public AdditionalCanvasShaderChannels additionalShaderChannels { get; set; } }
    [Flags] public enum AdditionalCanvasShaderChannels { None = 0, TexCoord1 = 1, TexCoord2 = 2, TexCoord3 = 4, Normal = 8, Tangent = 16 }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public sealed class CanvasGroup : Behaviour { public float alpha { get; set; } public bool interactable { get; set; } public bool blocksRaycasts { get; set; } public bool ignoreParentGroups { get; set; } }
    public sealed class CanvasRenderer : Component { public void SetAlpha(float a) { } public bool cullTransparentMesh { get; set; } }
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform r, Vector2 s, Camera c, out Vector2 l) { l = s; return true; }
        public static bool RectangleContainsScreenPoint(RectTransform r, Vector2 s) => true; public static bool RectangleContainsScreenPoint(RectTransform r, Vector2 s, Camera c) => true;
        public static Vector2 WorldToScreenPoint(Camera c, Vector3 w) => w;
        public static bool ScreenPointToWorldPointInRectangle(RectTransform r, Vector2 s, Camera c, out Vector3 w) { w = s; return true; }
    }
    public enum SortingLayerStub { }
    public struct SortingLayer { public static int NameToID(string n) => 0; public static SortingLayer[] layers => new SortingLayer[0]; public string name => ""; public int id => 0; public static int GetLayerValueFromName(string n) => 0; public static bool IsValid(int id) => true; }
    public struct LayerMask { public int value { get; set; } public static int NameToLayer(string n) => 0; public static string LayerToName(int l) => ""; public static int GetMask(params string[] n) => 0; public static implicit operator int(LayerMask m) => m.value; public static implicit operator LayerMask(int v) => new LayerMask { value = v }; }

    // Animation
    public class Motion : Object { }
    public sealed class AnimationClip : Motion { public float length => 1; public float frameRate { get; set; } public bool isLooping => false; public bool legacy { get; set; } public bool humanMotion => true; public WrapMode wrapMode { get; set; } public void SampleAnimation(GameObject g, float t) { } public Bounds localBounds { get; set; } public bool empty => false; public void SetCurve(string p, Type t, string n, AnimationCurve c) { } public void ClearCurves() { } public void EnsureQuaternionContinuity() { } public AnimationEvent[] events { get; set; } }
    public sealed class AnimationEvent { }
    public enum WrapMode { Default = 0, Once = 1, Clamp = 1, Loop = 2, PingPong = 4, ClampForever = 8 }
    public class AnimationCurve { public AnimationCurve() { } public AnimationCurve(params Keyframe[] k) { } public float Evaluate(float t) => 0; public Keyframe[] keys { get; set; } public static AnimationCurve Linear(float a, float b, float c, float d) => new AnimationCurve(); public static AnimationCurve EaseInOut(float a, float b, float c, float d) => new AnimationCurve(); public int AddKey(float t, float v) => 0; public int length => 0; }
    public struct Keyframe { public Keyframe(float t, float v) { time = t; value = v; } public float time; public float value; }
    public class Avatar : Object { public bool isValid => true; public bool isHuman => true; }
    public class RuntimeAnimatorController : Object { public AnimationClip[] animationClips => new AnimationClip[0]; }
    public enum HumanBodyBones { Hips, LeftUpperLeg, RightUpperLeg, LeftLowerLeg, RightLowerLeg, LeftFoot, RightFoot, Spine, Chest, Neck, Head, LeftShoulder, RightShoulder, LeftUpperArm, RightUpperArm, LeftLowerArm, RightLowerArm, LeftHand, RightHand, LeftToes, RightToes, LeftEye, RightEye, Jaw, LeftThumbProximal, LeftThumbIntermediate, LeftThumbDistal, LeftIndexProximal, LeftIndexIntermediate, LeftIndexDistal, LeftMiddleProximal, LeftMiddleIntermediate, LeftMiddleDistal, LeftRingProximal, LeftRingIntermediate, LeftRingDistal, LeftLittleProximal, LeftLittleIntermediate, LeftLittleDistal, RightThumbProximal, RightThumbIntermediate, RightThumbDistal, RightIndexProximal, RightIndexIntermediate, RightIndexDistal, RightMiddleProximal, RightMiddleIntermediate, RightMiddleDistal, RightRingProximal, RightRingIntermediate, RightRingDistal, RightLittleProximal, RightLittleIntermediate, RightLittleDistal, UpperChest, LastBone }
    public sealed class Animator : Behaviour
    {
        public Avatar avatar { get; set; } public RuntimeAnimatorController runtimeAnimatorController { get; set; } public bool applyRootMotion { get; set; } public bool isHuman => true; public float speed { get; set; } public AnimatorCullingMode cullingMode { get; set; } public AnimatorUpdateMode updateMode { get; set; }
        public Transform GetBoneTransform(HumanBodyBones b) => null; public void Rebind() { } public void Update(float dt) { } public void Play(string s) { } public void SetFloat(string n, float v) { } public void SetBool(string n, bool v) { } public void SetTrigger(string n) { }
        public Playables.PlayableGraph playableGraph => default; public Vector3 deltaPosition => default; public Quaternion deltaRotation => default; public bool hasBoundPlayables => false; public void WriteDefaultValues() { }
    }
    public enum AnimatorCullingMode { AlwaysAnimate, CullUpdateTransforms, CullCompletely }
    public enum AnimatorUpdateMode { Normal, AnimatePhysics, UnscaledTime, Fixed = 1 }

    // Audio
    public enum AudioRolloffMode { Logarithmic, Linear, Custom }
    public enum AudioDataLoadState { Unloaded, Loading, Loaded, Failed }
    public sealed class AudioClip : Object
    {
        public static AudioClip Create(string n, int len, int ch, int freq, bool stream) => new AudioClip(); public static AudioClip Create(string n, int len, int ch, int freq, bool stream, PCMReaderCallback r) => new AudioClip();
        public delegate void PCMReaderCallback(float[] data);
        public bool SetData(float[] d, int o) => true; public bool GetData(float[] d, int o) => true; public float length => 0; public int samples => 0; public int channels => 1; public int frequency => 44100; public AudioDataLoadState loadState => AudioDataLoadState.Loaded;
    }
    public sealed class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; } public float volume { get; set; } public float pitch { get; set; } public bool loop { get; set; } public bool playOnAwake { get; set; } public float spatialBlend { get; set; } public int priority { get; set; } public bool isPlaying => false; public float time { get; set; } public int timeSamples { get; set; } public bool mute { get; set; } public bool ignoreListenerPause { get; set; } public float panStereo { get; set; } public bool bypassEffects { get; set; } public bool ignoreListenerVolume { get; set; }
        public Audio.AudioMixerGroup outputAudioMixerGroup { get; set; }
        public void Play() { } public void PlayDelayed(float d) { } public void Stop() { } public void Pause() { } public void UnPause() { } public void PlayOneShot(AudioClip c) { } public void PlayOneShot(AudioClip c, float v) { } public void PlayScheduled(double t) { }
    }
    public sealed class AudioListener : Behaviour { public static float volume { get; set; } public static bool pause { get; set; } public static void GetOutputData(float[] s, int c) { } }
    public enum AudioSpeakerMode { Mono = 1, Stereo = 2 }
    public struct AudioConfiguration { public int sampleRate; public int dspBufferSize; public AudioSpeakerMode speakerMode; public int numVirtualVoices; public int numRealVoices; }
    public sealed class AudioSettings { public static int outputSampleRate => 48000; public static double dspTime => 0; public static AudioConfiguration GetConfiguration() => default; public static bool Reset(AudioConfiguration c) => true; public static AudioSpeakerMode driverCapabilities => AudioSpeakerMode.Stereo; public static void GetDSPBufferSize(out int l, out int n) { l = 512; n = 4; } public static event AudioConfigurationChangeHandler OnAudioConfigurationChanged; public delegate void AudioConfigurationChangeHandler(bool d); }

    // 2D physics
    public enum RigidbodyType2D { Dynamic, Kinematic, Static }
    public enum RigidbodyInterpolation2D { None, Interpolate, Extrapolate }
    public enum CollisionDetectionMode2D { Discrete, Continuous }
    public enum RigidbodyConstraints2D { None = 0, FreezePositionX = 1, FreezePositionY = 2, FreezeRotation = 4, FreezePosition = 3, FreezeAll = 7 }
    public enum CompositeColliderGeometryType { Outlines, Polygons }
    public sealed class Rigidbody2D : Component
    {
        public Vector2 position { get; set; } public float rotation { get; set; } public Vector2 velocity { get; set; } public Vector2 linearVelocity { get; set; } public float angularVelocity { get; set; } public float gravityScale { get; set; } public float mass { get; set; } public float drag { get; set; } public float linearDamping { get; set; } public float angularDamping { get; set; }
        public bool freezeRotation { get; set; } public RigidbodyType2D bodyType { get; set; } public bool isKinematic { get; set; } public bool simulated { get; set; } public RigidbodyInterpolation2D interpolation { get; set; } public CollisionDetectionMode2D collisionDetectionMode { get; set; } public RigidbodyConstraints2D constraints { get; set; } public bool useFullKinematicContacts { get; set; }
        public void MovePosition(Vector2 p) { } public void MoveRotation(float a) { } public void AddForce(Vector2 f) { } public void AddForce(Vector2 f, ForceMode2D m) { } public void Sleep() { } public void WakeUp() { }
        public int Cast(Vector2 d, RaycastHit2D[] r, float dist) => 0; public int Cast(Vector2 d, ContactFilter2D f, RaycastHit2D[] r, float dist) => 0;
    }
    public enum ForceMode2D { Force, Impulse }
    public class Collider2D : Behaviour
    {
        public bool isTrigger { get; set; } public Vector2 offset { get; set; } public Bounds bounds => default; public Rigidbody2D attachedRigidbody => null; public bool usedByComposite { get; set; } public CompositeOperation compositeOperation { get; set; } public bool usedByEffector { get; set; } public PhysicsMaterial2D sharedMaterial { get; set; } public float density { get; set; }
        public bool OverlapPoint(Vector2 p) => false; public bool IsTouching(Collider2D c) => false; public int Cast(Vector2 d, RaycastHit2D[] r, float dist) => 0; public Vector2 ClosestPoint(Vector2 p) => p; public ColliderDistance2D Distance(Collider2D o) => default;
        public enum CompositeOperation { None, Merge, Intersect, Difference, Flip }
        public LayerMask excludeLayers { get; set; } public LayerMask includeLayers { get; set; }
    }
    public class Collider : Component { public bool enabled { get; set; } public bool isTrigger { get; set; } public Bounds bounds => default; }
    public sealed class BoxCollider : Collider { } public sealed class SphereCollider : Collider { } public sealed class CapsuleCollider : Collider { } public sealed class MeshCollider : Collider { }
    public struct ColliderDistance2D { public float distance; public bool isOverlapped; public Vector2 normal; public Vector2 pointA; public Vector2 pointB; public bool isValid; }
    public sealed class PhysicsMaterial2D : Object { public float friction { get; set; } public float bounciness { get; set; } }
    public sealed class CircleCollider2D : Collider2D { public float radius { get; set; } }
    public sealed class BoxCollider2D : Collider2D { public Vector2 size { get; set; } public float edgeRadius { get; set; } }
    public sealed class CapsuleCollider2D : Collider2D { public Vector2 size { get; set; } public CapsuleDirection2D direction { get; set; } }
    public enum CapsuleDirection2D { Vertical, Horizontal }
    public sealed class PolygonCollider2D : Collider2D { public Vector2[] points { get; set; } public int pathCount { get; set; } public void SetPath(int i, Vector2[] p) { } public void SetPath(int i, List<Vector2> p) { } }
    public sealed class EdgeCollider2D : Collider2D { public Vector2[] points { get; set; } }
    public sealed class CompositeCollider2D : Collider2D { public enum GeometryType { Outlines, Polygons } public GeometryType geometryType { get; set; } public GenerationType generationType { get; set; } public void GenerateGeometry() { } public enum GenerationType { Synchronous, Manual } public int pathCount => 0; public float vertexDistance { get; set; } }
    public struct RaycastHit2D { public Vector2 point; public Vector2 normal; public float distance; public float fraction; public Collider2D collider => null; public Transform transform => null; public Rigidbody2D rigidbody => null; public static implicit operator bool(RaycastHit2D h) => false; }
    public struct ContactFilter2D { public bool useTriggers; public LayerMask layerMask; public bool useLayerMask; public void SetLayerMask(LayerMask m) { } public ContactFilter2D NoFilter() => this; }
    public static class Physics2D
    {
        public static RaycastHit2D Raycast(Vector2 o, Vector2 d) => default; public static RaycastHit2D Raycast(Vector2 o, Vector2 d, float dist) => default; public static RaycastHit2D Raycast(Vector2 o, Vector2 d, float dist, int mask) => default;
        public static RaycastHit2D Linecast(Vector2 a, Vector2 b) => default; public static RaycastHit2D Linecast(Vector2 a, Vector2 b, int mask) => default;
        public static Collider2D OverlapPoint(Vector2 p) => null; public static Collider2D OverlapPoint(Vector2 p, int mask) => null; public static Collider2D OverlapCircle(Vector2 p, float r) => null; public static Collider2D OverlapCircle(Vector2 p, float r, int mask) => null; public static Collider2D[] OverlapCircleAll(Vector2 p, float r) => new Collider2D[0]; public static Collider2D[] OverlapCircleAll(Vector2 p, float r, int mask) => new Collider2D[0];
        public static int OverlapCircleNonAlloc(Vector2 p, float r, Collider2D[] res) => 0; public static int OverlapCircleNonAlloc(Vector2 p, float r, Collider2D[] res, int mask) => 0;
        public static RaycastHit2D CircleCast(Vector2 o, float r, Vector2 d, float dist) => default; public static RaycastHit2D CircleCast(Vector2 o, float r, Vector2 d, float dist, int mask) => default;
        public static void SyncTransforms() { } public static bool autoSyncTransforms { get; set; } public static void IgnoreLayerCollision(int a, int b, bool i) { } public static bool GetIgnoreLayerCollision(int a, int b) => false; public static Vector2 gravity { get; set; } public static bool queriesHitTriggers { get; set; } public static void IgnoreCollision(Collider2D a, Collider2D b) { } public static void IgnoreCollision(Collider2D a, Collider2D b, bool i) { }
        public static bool Simulate(float s) => true; public static SimulationMode2D simulationMode { get; set; }
    }
    public enum SimulationMode2D { FixedUpdate, Update, Script }
    public struct Collision2D { public Collider2D collider => null; public GameObject gameObject => null; public Transform transform => null; }
}
namespace UnityEngine.Audio { public class AudioMixerGroup : Object { } public class AudioMixer : Object { } }
namespace UnityEngine.Playables { public struct PlayableGraph { public static PlayableGraph Create() => default; public static PlayableGraph Create(string n) => default; public void Destroy() { } public void Evaluate() { } public void Evaluate(float dt) { } public bool IsValid() => true; public void Play() { } public void Stop() { } public void SetTimeUpdateMode(DirectorUpdateMode m) { } } public enum DirectorUpdateMode { DSPClock, GameTime, UnscaledGameTime, Manual } public interface IPlayable { } public struct Playable { } }
namespace Unity.Collections { public struct NativeArray<T> : IDisposable where T : struct { public int Length => 0; public T this[int i] { get => default; set { } } public void Dispose() { } public T[] ToArray() => new T[0]; } public enum Allocator { Invalid, None, Temp, TempJob, Persistent } }
namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum OpaqueSortMode { Default, FrontToBack, NoDistanceSort }
    public enum AmbientMode { Skybox, Trilight, Flat = 3, Custom = 4 }
    public enum CompareFunction { Disabled, Never, Less, Equal, LessEqual, Greater, NotEqual, GreaterEqual, Always }
    public class SortingGroup : Behaviour { public string sortingLayerName { get; set; } public int sortingOrder { get; set; } public int sortingLayerID { get; set; } public bool sortAtRoot { get; set; } }
    public class RenderPipelineAsset : ScriptableObject { public virtual Material default2DMaterial => null; public virtual Material defaultMaterial => null; }
    public static class GraphicsSettings { public static RenderPipelineAsset defaultRenderPipeline { get; set; } public static RenderPipelineAsset currentRenderPipeline => null; public static TransparencySortMode transparencySortMode { get; set; } public static Vector3 transparencySortAxis { get; set; } public static RenderPipelineAsset renderPipelineAsset { get; set; } }
    public class Volume : Behaviour { public bool isGlobal { get; set; } public float weight { get; set; } public float priority { get; set; } public VolumeProfile profile { get; set; } public VolumeProfile sharedProfile { get; set; } }
    public class VolumeProfile : ScriptableObject { public bool TryGet<T>(out T c) where T : VolumeComponent { c = null; return false; } public T Add<T>(bool o = false) where T : VolumeComponent => null; }
    public class VolumeComponent : ScriptableObject { public bool active { get; set; } }
    public class VolumeParameter<T> { public T value { get; set; } public bool overrideState { get; set; } public void Override(T v) { } }
    public class ClampedFloatParameter : VolumeParameter<float> { public ClampedFloatParameter(float v, float a, float b, bool o = false) { } }
    public class MinFloatParameter : VolumeParameter<float> { public MinFloatParameter(float v, float a, bool o = false) { } }
    public class ColorParameter : VolumeParameter<Color> { public ColorParameter(Color c, bool o = false) { } }
    public class BoolParameter : VolumeParameter<bool> { public BoolParameter(bool c, bool o = false) { } }
}

namespace UnityEngine.Experimental.Rendering { public enum GraphicsFormat { None = 0, R8G8B8A8_SRGB = 8, R8G8B8A8_UNorm = 4, R16G16B16A16_SFloat = 48, R32G32B32A32_SFloat = 52 } }
