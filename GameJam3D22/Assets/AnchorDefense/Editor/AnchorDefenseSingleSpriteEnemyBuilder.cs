using UnityEditor;
using UnityEngine;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseSingleSpriteEnemyBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string PlaceholderSetPath = Root + "/Configs/Directional/EnemyDirections.asset";

        [MenuItem("Tools/Anchor Defense/Convert Gameplay Enemies To Single Sprites")]
        public static void BuildAll()
        {
            DirectionalSpriteSet placeholderSet = AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>(PlaceholderSetPath);
            Sprite placeholder = placeholderSet != null ? placeholderSet.GetSprite(0) : null;
            PatchEnemyPrefab(Root + "/Prefabs/Gameplay/Enemy.prefab", placeholder);
            PatchEnemyPrefab(Root + "/Prefabs/Gameplay/Enemy_Ranged.prefab", placeholder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Gameplay enemy prefabs now use single-sprite billboard visuals.");
        }

        private static void PatchEnemyPrefab(string path, Sprite placeholder)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            EnemyController controller = root.GetComponent<EnemyController>();
            SingleSpriteBillboardVisual existingVisual = root.GetComponentInChildren<SingleSpriteBillboardVisual>(true);
            if (controller == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            if (existingVisual == null)
            {
                Transform oldVisual = root.transform.Find("VisualRoot");
                if (oldVisual != null)
                {
                    Object.DestroyImmediate(oldVisual.gameObject);
                }

                GameObject visualObject = new GameObject(
                    "VisualRoot", typeof(SpriteRenderer), typeof(SingleSpriteBillboardVisual));
                visualObject.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = visualObject.GetComponent<SpriteRenderer>();
                renderer.sprite = placeholder;
                renderer.sortingOrder = 8;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                existingVisual = visualObject.GetComponent<SingleSpriteBillboardVisual>();
                existingVisual.Configure(renderer, true);
            }

            controller.ConfigureDirectionalVisual(null);
            controller.ConfigureSingleSpriteVisual(existingVisual);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
