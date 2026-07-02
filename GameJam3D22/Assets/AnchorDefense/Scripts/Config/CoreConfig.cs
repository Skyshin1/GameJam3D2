using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Core Config", fileName = "CoreConfig")]
    public sealed class CoreConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f)] public float MaxHealth { get; private set; } = 100f;
        [field: SerializeField, Min(0.1f)] public float Radius { get; private set; } = 2f;
        [field: SerializeField] public Color BaseColor { get; private set; } = new Color(0.12f, 0.48f, 1f);
        [field: SerializeField] public Color EmissionColor { get; private set; } = new Color(0.05f, 0.25f, 1f);
    }
}
