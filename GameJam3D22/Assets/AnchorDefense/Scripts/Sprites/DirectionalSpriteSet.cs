using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Directional Sprite Set", fileName = "DirectionalSpriteSet")]
    public sealed class DirectionalSpriteSet : ScriptableObject
    {
        public const int DirectionCount = 8;

        [Tooltip("Order: North, North-East, East, South-East, South, South-West, West, North-West")]
        [SerializeField] private Sprite[] sprites = new Sprite[DirectionCount];

        public void Configure(Sprite[] directionSprites)
        {
            sprites = new Sprite[DirectionCount];
            if (directionSprites == null)
            {
                return;
            }

            int count = Mathf.Min(DirectionCount, directionSprites.Length);
            for (int i = 0; i < count; i++)
            {
                sprites[i] = directionSprites[i];
            }
        }

        public Sprite GetSprite(int directionIndex)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return null;
            }

            int wrappedIndex = (directionIndex % DirectionCount + DirectionCount) % DirectionCount;
            if (wrappedIndex < sprites.Length && sprites[wrappedIndex] != null)
            {
                return sprites[wrappedIndex];
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    return sprites[i];
                }
            }

            return null;
        }
    }
}
