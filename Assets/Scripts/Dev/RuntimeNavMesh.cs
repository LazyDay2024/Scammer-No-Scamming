using Unity.AI.Navigation;
using UnityEngine;

namespace ScammerNoScamming
{
    // Bakes a NavMesh from the scene's render meshes when play starts, so the scene file needs no baked asset.
    // Put one in any scene whose enemies should walk around obstacles.
    // Runs before other Awakes so enemies placed in the scene find the NavMesh when they look for it in theirs.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class RuntimeNavMesh : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<NavMeshSurface>().BuildNavMesh();
        }
    }
}
