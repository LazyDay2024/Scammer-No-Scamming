using Unity.AI.Navigation;
using UnityEngine;

namespace ScammerNoScamming
{
    // Bakes a NavMesh from the scene's render meshes when play starts, so the scene file needs no baked asset.
    // Put one in any scene whose enemies should walk around obstacles.
    [RequireComponent(typeof(NavMeshSurface))]
    public class RuntimeNavMesh : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<NavMeshSurface>().BuildNavMesh();
        }
    }
}
