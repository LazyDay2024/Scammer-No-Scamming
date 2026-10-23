using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ScammerNoScamming
{
    public static class SceneLoader
    {
        // Loads a scene by asset path. In a build the scene must be in Build Settings; in the editor
        // it also works for scenes that are not, such as the sandboxes.
        public static void Load(string path)
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(path);
#endif
        }
    }
}
