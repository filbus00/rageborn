using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine
{
    [Flags] public enum HideFlags { None = 0, HideInHierarchy = 1, HideInInspector = 2, DontSaveInEditor = 4, NotEditable = 8, DontSaveInBuild = 16, DontUnloadUnusedAsset = 32, DontSave = 52, HideAndDontSave = 61 }
    public enum FindObjectsSortMode { None, InstanceID }
    public enum FindObjectsInactive { Exclude, Include }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum Space { World, Self }
    public enum SendMessageOptions { RequireReceiver, DontRequireReceiver }

    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public int GetInstanceID() => 0;
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static void DestroyImmediate(Object o) { }
        public static void DestroyImmediate(Object o, bool allowAssets) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T Instantiate<T>(T o) where T : Object => o;
        public static T Instantiate<T>(T o, Transform parent) where T : Object => o;
        public static T Instantiate<T>(T o, Transform parent, bool worldStays) where T : Object => o;
        public static T Instantiate<T>(T o, Vector3 p, Quaternion r) where T : Object => o;
        public static T Instantiate<T>(T o, Vector3 p, Quaternion r, Transform parent) where T : Object => o;
        public static Object Instantiate(Object o) => o;
        public static T FindAnyObjectByType<T>() where T : Object => null;
        public static T FindAnyObjectByType<T>(FindObjectsInactive i) where T : Object => null;
        public static T FindFirstObjectByType<T>() where T : Object => null;
        public static T FindFirstObjectByType<T>(FindObjectsInactive i) where T : Object => null;
        public static T[] FindObjectsByType<T>(FindObjectsSortMode m) where T : Object => new T[0];
        public static T[] FindObjectsByType<T>(FindObjectsInactive i) where T : Object => new T[0];
        public static T[] FindObjectsByType<T>() where T : Object => new T[0];
        public static T[] FindObjectsByType<T>(FindObjectsInactive i, FindObjectsSortMode m) where T : Object => new T[0];
        public static Object FindAnyObjectByType(Type t) => null;
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);
        public override bool Equals(object o) => ReferenceEquals(this, o);
        public override int GetHashCode() => base.GetHashCode();
    }

    public class Component : Object
    {
        public GameObject gameObject => null;
        public Transform transform => null;
        public string tag { get; set; }
        public T GetComponent<T>() => default;
        public Component GetComponent(Type t) => null;
        public bool TryGetComponent<T>(out T c) { c = default; return false; }
        public T GetComponentInChildren<T>() => default;
        public T GetComponentInChildren<T>(bool inactive) => default;
        public T GetComponentInParent<T>() => default;
        public T GetComponentInParent<T>(bool inactive) => default;
        public T[] GetComponents<T>() => new T[0];
        public void GetComponents<T>(List<T> into) { }
        public T[] GetComponentsInChildren<T>() => new T[0];
        public T[] GetComponentsInChildren<T>(bool inactive) => new T[0];
        public void GetComponentsInChildren<T>(bool inactive, List<T> into) { }
        public void GetComponentsInChildren<T>(List<T> into) { }
        public T[] GetComponentsInParent<T>() => new T[0];
        public bool CompareTag(string t) => false;
        public void SendMessage(string m) { }
        public void SendMessage(string m, object v, SendMessageOptions o) { }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled => enabled;
    }

    public class Coroutine { }
    public class YieldInstruction { }
    public class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) { } }
    public class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float s) { } public override bool keepWaiting => false; }
    public class WaitForEndOfFrame : YieldInstruction { }
    public class WaitForFixedUpdate : YieldInstruction { }
    public abstract class CustomYieldInstruction : IEnumerator { public abstract bool keepWaiting { get; } public object Current => null; public bool MoveNext() => keepWaiting; public void Reset() { } }
    public class WaitUntil : CustomYieldInstruction { public WaitUntil(Func<bool> f) { } public override bool keepWaiting => false; }
    public class WaitWhile : CustomYieldInstruction { public WaitWhile(Func<bool> f) { } public override bool keepWaiting => false; }
    public class AsyncOperation : YieldInstruction { public bool isDone => true; public float progress => 1; public bool allowSceneActivation { get; set; } public event Action<AsyncOperation> completed; }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator e) => null;
        public void StopCoroutine(Coroutine c) { }
        public void StopCoroutine(IEnumerator e) { }
        public void StopAllCoroutines() { }
        public void Invoke(string m, float t) { }
        public void CancelInvoke() { }
        public bool useGUILayout { get; set; }
        public static void print(object o) { }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
        public static ScriptableObject CreateInstance(Type t) => (ScriptableObject)Activator.CreateInstance(t);
    }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform => null;
        public GameObject gameObject => this;
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf => true;
        public bool activeInHierarchy => true;
        public bool isStatic { get; set; }
        public SceneManagement.Scene scene => default;
        public void SetActive(bool v) { }
        public T AddComponent<T>() where T : Component => null;
        public Component AddComponent(Type t) => null;
        public T GetComponent<T>() => default;
        public Component GetComponent(Type t) => null;
        public bool TryGetComponent<T>(out T c) { c = default; return false; }
        public T GetComponentInChildren<T>() => default;
        public T GetComponentInChildren<T>(bool inactive) => default;
        public T GetComponentInParent<T>() => default;
        public T[] GetComponents<T>() => new T[0];
        public T[] GetComponentsInChildren<T>() => new T[0];
        public T[] GetComponentsInChildren<T>(bool inactive) => new T[0];
        public void GetComponentsInChildren<T>(bool inactive, List<T> into) { }
        public bool CompareTag(string t) => false;
        public static GameObject Find(string n) => null;
        public static GameObject FindWithTag(string t) => null;
        public static GameObject[] FindGameObjectsWithTag(string t) => new GameObject[0];
        public static GameObject CreatePrimitive(PrimitiveType t) => null;
        public void SendMessage(string m) { }
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale => localScale;
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 up { get; set; }
        public Vector3 right { get; set; }
        public Vector3 forward { get; set; }
        public Transform parent { get; set; }
        public Transform root => this;
        public int childCount => 0;
        public Matrix4x4 localToWorldMatrix => default;
        public Matrix4x4 worldToLocalMatrix => default;
        public bool hasChanged { get; set; }
        public void SetParent(Transform p) { }
        public void SetParent(Transform p, bool worldStays) { }
        public Transform GetChild(int i) => null;
        public Transform Find(string n) => null;
        public bool IsChildOf(Transform t) => false;
        public void Rotate(float x, float y, float z) { }
        public void Rotate(Vector3 e) { }
        public void Rotate(Vector3 e, Space s) { }
        public void Rotate(Vector3 axis, float angle) { }
        public void RotateAround(Vector3 p, Vector3 axis, float a) { }
        public void Translate(Vector3 v) { }
        public void Translate(Vector3 v, Space s) { }
        public void LookAt(Transform t) { }
        public void LookAt(Vector3 p) { }
        public void LookAt(Vector3 p, Vector3 up) { }
        public Vector3 TransformPoint(Vector3 p) => p;
        public Vector3 InverseTransformPoint(Vector3 p) => p;
        public Vector3 TransformDirection(Vector3 p) => p;
        public Vector3 InverseTransformDirection(Vector3 p) => p;
        public Vector3 TransformVector(Vector3 p) => p;
        public void SetSiblingIndex(int i) { }
        public int GetSiblingIndex() => 0;
        public void SetAsLastSibling() { }
        public void SetAsFirstSibling() { }
        public void DetachChildren() { }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { }
        public void SetLocalPositionAndRotation(Vector3 p, Quaternion r) { }
        public void GetPositionAndRotation(out Vector3 p, out Quaternion r) { p = default; r = default; }
        public IEnumerator GetEnumerator() { yield break; }
    }

    public sealed class RectTransform : Transform
    {
        public enum Axis { Horizontal, Vertical }
        public enum Edge { Left, Right, Top, Bottom }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector3 anchoredPosition3D { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect => default;
        public void SetSizeWithCurrentAnchors(Axis a, float s) { }
        public void SetInsetAndSizeFromParentEdge(Edge e, float i, float s) { }
        public void GetWorldCorners(Vector3[] c) { }
        public void GetLocalCorners(Vector3[] c) { }
        public void ForceUpdateRectTransforms() { }
    }

    // Attributes
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeReference : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class PropertyAttribute : Attribute { }
    public sealed class TooltipAttribute : PropertyAttribute { public TooltipAttribute(string t) { } }
    public sealed class HeaderAttribute : PropertyAttribute { public HeaderAttribute(string t) { } }
    public sealed class SpaceAttribute : PropertyAttribute { public SpaceAttribute() { } public SpaceAttribute(float h) { } }
    public sealed class MinAttribute : PropertyAttribute { public MinAttribute(float m) { } }
    public sealed class RangeAttribute : PropertyAttribute { public RangeAttribute(float a, float b) { } }
    public sealed class TextAreaAttribute : PropertyAttribute { public TextAreaAttribute() { } public TextAreaAttribute(int a, int b) { } }
    public sealed class MultilineAttribute : PropertyAttribute { public MultilineAttribute() { } public MultilineAttribute(int a) { } }
    public sealed class HideInInspector : Attribute { }
    public sealed class ColorUsageAttribute : PropertyAttribute { public ColorUsageAttribute(bool a) { } public ColorUsageAttribute(bool a, bool hdr) { } }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class RequireComponent : Attribute { public RequireComponent(Type a) { } public RequireComponent(Type a, Type b) { } public RequireComponent(Type a, Type b, Type c) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class ExecuteAlways : Attribute { }
    public sealed class ExecuteInEditMode : Attribute { }
    public sealed class AddComponentMenu : Attribute { public AddComponentMenu(string m) { } public AddComponentMenu(string m, int o) { } }
    public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) { } }
    public sealed class CreateAssetMenuAttribute : Attribute { public string fileName { get; set; } public string menuName { get; set; } public int order { get; set; } }
    public sealed class ContextMenu : Attribute { public ContextMenu(string m) { } }
    public sealed class SelectionBaseAttribute : Attribute { }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute() { } public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    namespace Serialization { [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)] public sealed class FormerlySerializedAsAttribute : Attribute { public FormerlySerializedAsAttribute(string n) { } } }
}
