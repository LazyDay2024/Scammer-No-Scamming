using UnityEngine;

namespace ScammerNoScamming
{
    // A label that keeps its text inside a fixed sign area and is hidden by walls.
    [RequireComponent(typeof(TextMesh))]
    public class WorldLabel : MonoBehaviour
    {
        public float width = 2f, height = 1f, maxSize = .1f;
        TextMesh mesh;
        public TextMesh Mesh => mesh != null ? mesh : (mesh = GetComponent<TextMesh>());

        void Awake() => WorldText.Apply(Mesh);

        public string Text
        {
            get => Mesh.text;
            set
            {
                if (width > 0f && height > 0f) WorldText.Fit(Mesh, value, width, height, maxSize);
                else Mesh.text = value;
                WorldText.Apply(Mesh);
            }
        }
    }
}
