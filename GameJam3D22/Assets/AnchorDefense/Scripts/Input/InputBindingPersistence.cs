using UnityEngine;
using UnityEngine.InputSystem;

namespace AnchorDefense
{
    public static class InputBindingPersistence
    {
        private const string PlayerPrefsKey = "AnchorDefense.InputBindings";

        public static void Load(InputActionAsset asset)
        {
            if (asset == null || !PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                return;
            }

            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                asset.LoadBindingOverridesFromJson(json);
            }
        }

        public static void Save(InputActionAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            PlayerPrefs.SetString(PlayerPrefsKey, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public static void Reset(InputActionAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
