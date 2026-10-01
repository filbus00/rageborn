using System;
namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive }
    public struct Scene { public string name => ""; public string path => ""; public bool isLoaded => true; public bool IsValid() => true; public int buildIndex => 0; public GameObject[] GetRootGameObjects() => new GameObject[0]; public int rootCount => 0; public bool isDirty => false; public static bool operator ==(Scene a, Scene b) => true; public static bool operator !=(Scene a, Scene b) => false; public override bool Equals(object o) => true; public override int GetHashCode() => 0; }
    public static class SceneManager
    {
        public static Scene GetActiveScene() => default; public static bool SetActiveScene(Scene s) => true; public static int sceneCount => 1; public static int sceneCountInBuildSettings => 1; public static Scene GetSceneAt(int i) => default; public static Scene GetSceneByName(string n) => default; public static Scene GetSceneByBuildIndex(int i) => default;
        public static void LoadScene(string n) { } public static void LoadScene(int i) { } public static void LoadScene(string n, LoadSceneMode m) { }
        public static AsyncOperation LoadSceneAsync(string n) => null; public static AsyncOperation LoadSceneAsync(string n, LoadSceneMode m) => null; public static AsyncOperation LoadSceneAsync(int i) => null; public static AsyncOperation UnloadSceneAsync(string n) => null; public static AsyncOperation UnloadSceneAsync(Scene s) => null;
        public static Scene CreateScene(string n) => default; public static void MoveGameObjectToScene(GameObject g, Scene s) { }
        public static event Action<Scene, LoadSceneMode> sceneLoaded; public static event Action<Scene> sceneUnloaded; public static event Action<Scene, Scene> activeSceneChanged;
    }
    public static class SceneUtility { public static string GetScenePathByBuildIndex(int i) => ""; public static int GetBuildIndexByScenePath(string p) => 0; }
}
