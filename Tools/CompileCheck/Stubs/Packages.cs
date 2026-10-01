using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class GridLayout : Behaviour
    {
        public enum CellLayout { Rectangle, Hexagon, Isometric, IsometricZAsY }
        public enum CellSwizzle { XYZ, XZY, YXZ, YZX, ZXY, ZYX }
        public Vector3 cellSize => default; public Vector3 cellGap => default; public CellLayout cellLayout => default; public CellSwizzle cellSwizzle => default;
        public Vector3Int WorldToCell(Vector3 p) => default; public Vector3 CellToWorld(Vector3Int c) => default; public Vector3 GetCellCenterWorld(Vector3Int c) => default; public Vector3 CellToLocal(Vector3Int c) => default; public Vector3 LocalToWorld(Vector3 l) => l; public Vector3 WorldToLocal(Vector3 w) => w;
    }
    public sealed class Grid : GridLayout { public new Vector3 cellSize { get; set; } public new Vector3 cellGap { get; set; } public new CellLayout cellLayout { get; set; } public new CellSwizzle cellSwizzle { get; set; } }
}
namespace UnityEngine.Tilemaps
{
    public enum TileFlags { None = 0, LockColor = 1, LockTransform = 2, InstantiateGameObjectRuntimeOnly = 4, LockAll = 3 }
    public abstract class TileBase : ScriptableObject { public virtual void GetTileData(Vector3Int p, ITilemap t, ref TileData d) { } public virtual void RefreshTile(Vector3Int p, ITilemap t) { } }
    public class ITilemap { }
    public struct TileData { public Sprite sprite { get; set; } public Color color { get; set; } public Matrix4x4 transform { get; set; } public GameObject gameObject { get; set; } public TileFlags flags { get; set; } public Tile.ColliderType colliderType { get; set; } }
    public class Tile : TileBase
    {
        public enum ColliderType { None, Sprite, Grid }
        public Sprite sprite { get; set; } public Color color { get; set; } public Matrix4x4 transform { get; set; } public GameObject gameObject { get; set; } public TileFlags flags { get; set; } public ColliderType colliderType { get; set; }
    }
    public sealed class Tilemap : GridLayout
    {
        public enum Orientation { XY, XZ, YX, YZ, ZX, ZY, Custom }
        public BoundsInt cellBounds => default; public Vector3Int origin { get; set; } public Vector3Int size { get; set; } public Color color { get; set; } public Grid layoutGrid => null; public Vector3 tileAnchor { get; set; } public Orientation orientation { get; set; } public Bounds localBounds => default;
        public TileBase GetTile(Vector3Int p) => null; public T GetTile<T>(Vector3Int p) where T : TileBase => null; public bool HasTile(Vector3Int p) => false; public void SetTile(Vector3Int p, TileBase t) { } public void SetTiles(Vector3Int[] p, TileBase[] t) { } public void SetTilesBlock(BoundsInt b, TileBase[] t) { } public TileBase[] GetTilesBlock(BoundsInt b) => new TileBase[0];
        public void ClearAllTiles() { } public void CompressBounds() { } public void RefreshAllTiles() { } public void RefreshTile(Vector3Int p) { } public void ResizeBounds() { }
        public void SetColor(Vector3Int p, Color c) { } public Color GetColor(Vector3Int p) => Color.white; public void SetTileFlags(Vector3Int p, TileFlags f) { } public void RemoveTileFlags(Vector3Int p, TileFlags f) { } public void AddTileFlags(Vector3Int p, TileFlags f) { } public TileFlags GetTileFlags(Vector3Int p) => default;
        public void SetTransformMatrix(Vector3Int p, Matrix4x4 m) { } public Matrix4x4 GetTransformMatrix(Vector3Int p) => default; public Sprite GetSprite(Vector3Int p) => null; public int GetUsedTilesCount() => 0;
        public Tile.ColliderType GetColliderType(Vector3Int p) => default; public void SetColliderType(Vector3Int p, Tile.ColliderType t) { }
        public void BoxFill(Vector3Int p, TileBase t, int a, int b, int c, int d) { } public void FloodFill(Vector3Int p, TileBase t) { }
    }
    public sealed class TilemapRenderer : Renderer { public enum Mode { Chunk, Individual, SRPBatch } public enum SortOrder { BottomLeft, BottomRight, TopLeft, TopRight } public Mode mode { get; set; } public SortOrder sortOrder { get; set; } public Vector3Int chunkSize { get; set; } }
    public sealed class TilemapCollider2D : Collider2D { public bool useDelaunayMesh { get; set; } public uint maximumTileChangeCount { get; set; } public void ProcessTilemapChanges() { } public bool hasTilemapChanges => false; }
}
namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { public virtual bool IsActive() => true; protected virtual void Awake() { } protected virtual void OnEnable() { } protected virtual void OnDisable() { } protected virtual void Start() { } protected virtual void OnDestroy() { } protected virtual void OnRectTransformDimensionsChange() { } protected virtual void OnValidate() { } }
    public class BaseEventData { public BaseEventData(EventSystem e) { } public void Use() { } public bool used => false; public GameObject selectedObject { get; set; } }
    public enum PointerEventDataInputButton { Left, Right, Middle }
    public class PointerEventData : BaseEventData
    {
        public enum InputButton { Left, Right, Middle }
        public PointerEventData(EventSystem e) : base(e) { }
        public Vector2 position { get; set; } public Vector2 delta { get; set; } public Vector2 pressPosition { get; set; } public int pointerId { get; set; } public InputButton button { get; set; } public int clickCount { get; set; } public bool dragging { get; set; } public GameObject pointerPress { get; set; } public GameObject pointerDrag { get; set; } public GameObject pointerEnter { get; set; } public RaycastResult pointerCurrentRaycast { get; set; } public RaycastResult pointerPressRaycast { get; set; } public Camera pressEventCamera => null; public Camera enterEventCamera => null; public bool eligibleForClick { get; set; } public float clickTime { get; set; } public Vector2 scrollDelta { get; set; }
    }
    public struct RaycastResult { public GameObject gameObject { get; set; } public BaseRaycaster module; public float distance; public int index; public int depth; public int sortingLayer; public int sortingOrder; public Vector3 worldPosition; public Vector3 worldNormal; public Vector2 screenPosition; public bool isValid => gameObject != null; public void Clear() { } }
    public abstract class BaseRaycaster : UIBehaviour { public abstract void Raycast(PointerEventData e, List<RaycastResult> r); public abstract Camera eventCamera { get; } }
    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; } public GameObject currentSelectedGameObject => null; public GameObject firstSelectedGameObject { get; set; } public bool sendNavigationEvents { get; set; } public int pixelDragThreshold { get; set; } public bool alreadySelecting => false; public BaseInputModule currentInputModule => null;
        public void RaycastAll(PointerEventData e, List<RaycastResult> r) { } public bool IsPointerOverGameObject() => false; public bool IsPointerOverGameObject(int id) => false; public void SetSelectedGameObject(GameObject g) { } public void SetSelectedGameObject(GameObject g, BaseEventData d) { } public void UpdateModules() { }
    }
    public abstract class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData e); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData e); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData e); }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData e); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData e); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData e); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData e); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData e); }
    public interface IInitializePotentialDragHandler : IEventSystemHandler { void OnInitializePotentialDrag(PointerEventData e); }
    public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData e); }
    public static class ExecuteEvents { public delegate void EventFunction<T>(T h, BaseEventData d); public static bool Execute<T>(GameObject t, BaseEventData d, EventFunction<T> f) where T : IEventSystemHandler => false; public static GameObject ExecuteHierarchy<T>(GameObject r, BaseEventData d, EventFunction<T> f) where T : IEventSystemHandler => null; public static EventFunction<IBeginDragHandler> beginDragHandler => null; public static EventFunction<IDragHandler> dragHandler => null; public static EventFunction<IEndDragHandler> endDragHandler => null; public static EventFunction<IInitializePotentialDragHandler> initializePotentialDrag => null; public static EventFunction<IScrollHandler> scrollHandler => null; public static GameObject GetEventHandler<T>(GameObject r) where T : IEventSystemHandler => null; }
}
namespace UnityEngine.Events
{
    public delegate void UnityAction(); public delegate void UnityAction<T0>(T0 a);
    public abstract class UnityEventBase { public void RemoveAllListeners() { } public int GetPersistentEventCount() => 0; }
    public class UnityEvent : UnityEventBase { public void AddListener(UnityAction a) { } public void RemoveListener(UnityAction a) { } public void Invoke() { } }
    public class UnityEvent<T0> : UnityEventBase { public void AddListener(UnityAction<T0> a) { } public void RemoveListener(UnityAction<T0> a) { } public void Invoke(T0 a) { } }
}
namespace UnityEngine.UI
{
    using UnityEngine.EventSystems; using UnityEngine.Events;
    public interface ILayoutElement { } public interface ILayoutGroup { } public interface ILayoutController { } public interface ICanvasElement { } public interface IClippable { } public interface IMaskable { } public interface IMaterialModifier { } public interface IMeshModifier { }
    public class VertexHelper { public int currentVertCount => 0; public void Clear() { } public void AddVert(Vector3 p, Color32 c, Vector4 uv) { } public void AddVert(UIVertex v) { } public void AddTriangle(int a, int b, int c) { } public void PopulateUIVertex(ref UIVertex v, int i) { } public void SetUIVertex(UIVertex v, int i) { } }
    public struct UIVertex { public Vector3 position; public Color32 color; public Vector4 uv0; public static UIVertex simpleVert => default; }
    public abstract class Graphic : UIBehaviour { public Color color { get; set; } public bool raycastTarget { get; set; } public Material material { get; set; } public virtual Material materialForRendering => null; public RectTransform rectTransform => null; public Canvas canvas => null; public CanvasRenderer canvasRenderer => null; public virtual Texture mainTexture => null; public static Material defaultGraphicMaterial => null; public Vector4 raycastPadding { get; set; } public virtual void SetAllDirty() { } public virtual void SetVerticesDirty() { } public virtual void SetMaterialDirty() { } public virtual void SetLayoutDirty() { } protected virtual void OnPopulateMesh(VertexHelper vh) { } public void CrossFadeAlpha(float a, float d, bool i) { } public void CrossFadeColor(Color c, float d, bool i, bool a) { } public virtual void Rebuild(CanvasUpdate u) { } }
    public enum CanvasUpdate { Prelayout, Layout, PostLayout, PreRender, LatePreRender, MaxUpdateValue }
    public abstract class MaskableGraphic : Graphic { public bool maskable { get; set; } public virtual void RecalculateMasking() { } public virtual void RecalculateClipping() { } }
    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public enum Origin360 { Bottom, Right, Top, Left }
        public enum Origin180 { Bottom, Left, Top, Right }
        public enum Origin90 { BottomLeft, TopLeft, TopRight, BottomRight }
        public enum OriginHorizontal { Left, Right }
        public enum OriginVertical { Bottom, Top }
        public Sprite sprite { get; set; } public Sprite overrideSprite { get; set; } public Type type { get; set; } public FillMethod fillMethod { get; set; } public float fillAmount { get; set; } public bool fillClockwise { get; set; } public int fillOrigin { get; set; } public bool preserveAspect { get; set; } public bool fillCenter { get; set; } public float pixelsPerUnitMultiplier { get; set; } public bool useSpriteMesh { get; set; } public float alphaHitTestMinimumThreshold { get; set; }
        public void SetNativeSize() { }
    }
    public class RawImage : MaskableGraphic { public Texture texture { get; set; } public Rect uvRect { get; set; } public void SetNativeSize() { } }
    public class Text : MaskableGraphic
    {
        public string text { get; set; } public Font font { get; set; } public int fontSize { get; set; } public FontStyle fontStyle { get; set; } public TextAnchor alignment { get; set; } public bool supportRichText { get; set; } public bool resizeTextForBestFit { get; set; } public int resizeTextMinSize { get; set; } public int resizeTextMaxSize { get; set; } public HorizontalWrapMode horizontalOverflow { get; set; } public VerticalWrapMode verticalOverflow { get; set; } public float lineSpacing { get; set; } public bool alignByGeometry { get; set; }
        public float preferredWidth => 0; public float preferredHeight => 0; public TextGenerator cachedTextGenerator => null; public TextGenerationSettings GetGenerationSettings(Vector2 e) => default;
    }
    public class TextGenerator { public float GetPreferredWidth(string s, TextGenerationSettings t) => 0; public float GetPreferredHeight(string s, TextGenerationSettings t) => 0; public int lineCount => 0; }
    public struct TextGenerationSettings { }
    [Serializable] public struct ColorBlock { public Color normalColor { get; set; } public Color highlightedColor { get; set; } public Color pressedColor { get; set; } public Color selectedColor { get; set; } public Color disabledColor { get; set; } public float colorMultiplier { get; set; } public float fadeDuration { get; set; } public static ColorBlock defaultColorBlock => default; }
    public struct Navigation { public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 } public Mode mode { get; set; } public static Navigation defaultNavigation => default; }
    public class Selectable : UIBehaviour
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
        public bool interactable { get; set; } public Transition transition { get; set; } public ColorBlock colors { get; set; } public Graphic targetGraphic { get; set; } public Navigation navigation { get; set; } public Image image { get; set; } public void Select() { } public virtual bool IsInteractable() => interactable;
        public virtual void OnPointerDown(PointerEventData e) { } public virtual void OnPointerUp(PointerEventData e) { } public virtual void OnPointerEnter(PointerEventData e) { } public virtual void OnPointerExit(PointerEventData e) { }
    }
    public class Button : Selectable, IPointerClickHandler { public class ButtonClickedEvent : UnityEvent { } public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent(); public virtual void OnPointerClick(PointerEventData e) { } }
    public class Toggle : Selectable { public class ToggleEvent : UnityEvent<bool> { } public bool isOn { get; set; } public ToggleEvent onValueChanged { get; set; } = new ToggleEvent(); public Graphic graphic { get; set; } public ToggleGroup group { get; set; } public void SetIsOnWithoutNotify(bool v) { } }
    public class ToggleGroup : UIBehaviour { public bool allowSwitchOff { get; set; } }
    public class Slider : Selectable { public class SliderEvent : UnityEvent<float> { } public float value { get; set; } public float minValue { get; set; } public float maxValue { get; set; } public bool wholeNumbers { get; set; } public SliderEvent onValueChanged { get; set; } = new SliderEvent(); public RectTransform fillRect { get; set; } public RectTransform handleRect { get; set; } public void SetValueWithoutNotify(float v) { } }
    public class Scrollbar : Selectable { public float value { get; set; } public float size { get; set; } }
    public class InputField : Selectable { public string text { get; set; } }
    public class Dropdown : Selectable { }
    public class ScrollRect : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler, IScrollHandler
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
        public class ScrollRectEvent : UnityEvent<Vector2> { }
        public RectTransform content { get; set; } public RectTransform viewport { get; set; } public bool horizontal { get; set; } public bool vertical { get; set; } public MovementType movementType { get; set; } public float elasticity { get; set; } public bool inertia { get; set; } public float decelerationRate { get; set; } public float scrollSensitivity { get; set; } public Scrollbar horizontalScrollbar { get; set; } public Scrollbar verticalScrollbar { get; set; } public float verticalNormalizedPosition { get; set; } public float horizontalNormalizedPosition { get; set; } public Vector2 normalizedPosition { get; set; } public Vector2 velocity { get; set; } public ScrollRectEvent onValueChanged { get; set; } = new ScrollRectEvent(); public ScrollbarVisibility verticalScrollbarVisibility { get; set; } public ScrollbarVisibility horizontalScrollbarVisibility { get; set; }
        public virtual void OnBeginDrag(PointerEventData e) { } public virtual void OnDrag(PointerEventData e) { } public virtual void OnEndDrag(PointerEventData e) { } public virtual void OnInitializePotentialDrag(PointerEventData e) { } public virtual void OnScroll(PointerEventData e) { } public void StopMovement() { }
    }
    public class Mask : UIBehaviour { public bool showMaskGraphic { get; set; } public Graphic graphic => null; }
    public class RectMask2D : UIBehaviour { public Vector4 padding { get; set; } public Vector2Int softness { get; set; } }
    public abstract class BaseMeshEffect : UIBehaviour { public abstract void ModifyMesh(VertexHelper vh); }
    public class Shadow : BaseMeshEffect { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } public bool useGraphicAlpha { get; set; } public override void ModifyMesh(VertexHelper vh) { } }
    public class Outline : Shadow { }
    public class LayoutElement : UIBehaviour { public bool ignoreLayout { get; set; } public float minWidth { get; set; } public float minHeight { get; set; } public float preferredWidth { get; set; } public float preferredHeight { get; set; } public float flexibleWidth { get; set; } public float flexibleHeight { get; set; } public int layoutPriority { get; set; } }
    public abstract class LayoutGroup : UIBehaviour { public RectOffset padding { get; set; } = new RectOffset(); public TextAnchor childAlignment { get; set; } public virtual void CalculateLayoutInputHorizontal() { } public virtual void SetLayoutHorizontal() { } }
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup { public float spacing { get; set; } public bool childForceExpandWidth { get; set; } public bool childForceExpandHeight { get; set; } public bool childControlWidth { get; set; } public bool childControlHeight { get; set; } public bool childScaleWidth { get; set; } public bool childScaleHeight { get; set; } public bool reverseArrangement { get; set; } }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class GridLayoutGroup : LayoutGroup { public enum Corner { UpperLeft, UpperRight, LowerLeft, LowerRight } public enum Axis { Horizontal, Vertical } public enum Constraint { Flexible, FixedColumnCount, FixedRowCount } public Vector2 cellSize { get; set; } public Vector2 spacing { get; set; } public Corner startCorner { get; set; } public Axis startAxis { get; set; } public Constraint constraint { get; set; } public int constraintCount { get; set; } }
    public class ContentSizeFitter : UIBehaviour { public enum FitMode { Unconstrained, MinSize, PreferredSize } public FitMode horizontalFit { get; set; } public FitMode verticalFit { get; set; } }
    public class AspectRatioFitter : UIBehaviour { public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent } public AspectMode aspectMode { get; set; } public float aspectRatio { get; set; } }
    public class CanvasScaler : UIBehaviour { public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize } public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink } public ScaleMode uiScaleMode { get; set; } public Vector2 referenceResolution { get; set; } public float matchWidthOrHeight { get; set; } public ScreenMatchMode screenMatchMode { get; set; } public float scaleFactor { get; set; } public float referencePixelsPerUnit { get; set; } }
    public class GraphicRaycaster : BaseRaycaster { public enum BlockingObjects { None, TwoD, ThreeD, All } public bool ignoreReversedGraphics { get; set; } public BlockingObjects blockingObjects { get; set; } public override void Raycast(PointerEventData e, List<RaycastResult> r) { } public override Camera eventCamera => null; }
    public static class LayoutRebuilder { public static void ForceRebuildLayoutImmediate(RectTransform r) { } public static void MarkLayoutForRebuild(RectTransform r) { } }
    public static class LayoutUtility { public static float GetPreferredHeight(RectTransform r) => 0; public static float GetPreferredWidth(RectTransform r) => 0; }
}
namespace UnityEngine
{
    [Serializable] public class RectOffset { public RectOffset() { } public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; } public int left { get; set; } public int right { get; set; } public int top { get; set; } public int bottom { get; set; } public int horizontal => left + right; public int vertical => top + bottom; }
}
namespace UnityEngine.InputSystem
{
    public class InputDevice { public bool added => true; public int deviceId => 0; }
    public class Pointer : InputDevice { public Controls.Vector2Control position => null; }
    public class Mouse : Pointer { public static Mouse current => null; public Controls.ButtonControl leftButton => null; public Controls.ButtonControl rightButton => null; public Controls.Vector2Control scroll => null; }
    public class Touchscreen : Pointer { public static Touchscreen current => null; }
    public class Keyboard : InputDevice { public static Keyboard current => null; public Controls.KeyControl this[Key k] => null; public Controls.KeyControl spaceKey => null; public Controls.KeyControl escapeKey => null; public Controls.KeyControl leftShiftKey => null; public Controls.KeyControl wKey => null; public Controls.KeyControl aKey => null; public Controls.KeyControl sKey => null; public Controls.KeyControl dKey => null; }
    public enum Key { None, Space, Enter, Tab, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, Escape, F1, F2, F3, F4, F5, Digit1, Digit2, Digit3, LeftShift }
    public static class InputSystem { public static void Update() { } public static TDevice AddDevice<TDevice>() where TDevice : InputDevice => null; public static void RemoveDevice(InputDevice d) { } public static InputSettings settings { get; set; } }
    public class InputSettings : ScriptableObject { }
    public enum TouchPhase { None, Began, Moved, Ended, Canceled, Stationary }
}
namespace UnityEngine.InputSystem.Controls
{
    public class InputControl { } public class InputControl<T> : InputControl where T : struct { public T ReadValue() => default; }
    public class ButtonControl : InputControl<float> { public bool isPressed => false; public bool wasPressedThisFrame => false; public bool wasReleasedThisFrame => false; }
    public class KeyControl : ButtonControl { }
    public class Vector2Control : InputControl<Vector2> { }
}
namespace UnityEngine.InputSystem.EnhancedTouch
{
    using System.Collections.ObjectModel;
    public static class EnhancedTouchSupport { public static void Enable() { } public static void Disable() { } public static bool enabled => true; }
    public static class TouchSimulation { public static void Enable() { } public static void Disable() { } public static object instance => null; }
    public class Finger { public int index => 0; public bool isActive => false; public Vector2 screenPosition => default; public Touch currentTouch => default; public Touchscreen screen => null; }
    public struct Touch
    {
        public static ReadOnlyArray<Touch> activeTouches => default; public static ReadOnlyArray<Finger> activeFingers => default; public static ReadOnlyArray<Finger> fingers => default;
        public Finger finger => null; public TouchPhase phase => default; public Vector2 screenPosition => default; public Vector2 startScreenPosition => default; public Vector2 delta => default; public int touchId => 0; public bool began => false; public bool ended => false; public bool isInProgress => false; public bool valid => true; public double time => 0; public double startTime => 0; public int tapCount => 0; public bool isTap => false;
        public static event Action<Finger> onFingerDown; public static event Action<Finger> onFingerUp; public static event Action<Finger> onFingerMove;
    }
    public struct ReadOnlyArray<T> : IEnumerable<T> { public int Count => 0; public T this[int i] => default; public IEnumerator<T> GetEnumerator() { yield break; } System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { yield break; } }
}
namespace UnityEngine.InputSystem.UI { public class InputSystemUIInputModule : EventSystems.BaseInputModule { public void AssignDefaultActions() { } } }
namespace UnityEngine.Rendering.Universal
{
    public class Light2D : Behaviour
    {
        public enum LightType { Parametric = 0, Freeform = 1, Sprite = 2, Point = 3, Global = 4 }
        public LightType lightType { get; set; } public Color color { get; set; } public float intensity { get; set; } public float pointLightInnerRadius { get; set; } public float pointLightOuterRadius { get; set; } public float pointLightInnerAngle { get; set; } public float pointLightOuterAngle { get; set; } public float falloffIntensity { get; set; } public int blendStyleIndex { get; set; } public bool shadowsEnabled { get; set; } public float shadowIntensity { get; set; } public bool volumetricEnabled { get; set; } public float volumeIntensity { get; set; } public int[] targetSortingLayers { get; set; } public Sprite lightCookieSprite { get; set; } public bool overlapOperation { get; set; } public int lightOrder { get; set; }
        public void SetShapePath(Vector3[] p) { }
    }
    public class PixelPerfectCamera : MonoBehaviour
    {
        public enum CropFrame { None, Pillarbox, Letterbox, Windowbox, StretchFill }
        public enum GridSnapping { None, PixelSnapping, UpscaleRenderTexture }
        public enum PixelPerfectFilterMode { RetroAA, Point }
        public int assetsPPU { get; set; } public int refResolutionX { get; set; } public int refResolutionY { get; set; } public CropFrame cropFrame { get; set; } public GridSnapping gridSnapping { get; set; } public PixelPerfectFilterMode filterMode { get; set; } public int pixelRatio => 1; public float orthographicSize => 0; public Vector3 RoundToPixel(Vector3 p) => p; public float CorrectCinemachineOrthoSize(float s) => s; public bool requiresUpscalePass => true;
    }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public class UniversalAdditionalCameraData : MonoBehaviour { public bool renderPostProcessing { get; set; } public void SetRenderer(int i) { } public AntialiasingMode antialiasing { get; set; } public bool renderShadows { get; set; } public bool requiresDepthTexture { get; set; } public bool requiresColorTexture { get; set; } }
    public class UniversalRenderPipelineAsset : RenderPipelineAsset { public float renderScale { get; set; } public bool supportsHDR { get; set; } public int msaaSampleCount { get; set; } public ScriptableRendererData[] rendererDataList => new ScriptableRendererData[0]; public ScriptableRenderer scriptableRenderer => null; }
    public class ScriptableRendererData : ScriptableObject { }
    public class UniversalRendererData : ScriptableRendererData { }
    public class Renderer2DData : ScriptableRendererData { }
    public class ScriptableRenderer { }
}
