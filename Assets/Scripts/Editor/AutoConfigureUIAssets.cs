using UnityEditor;
using UnityEngine;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Ensures that UI images dragged or copied into the project are 
    /// properly configured as Sprites so Resources.Load<Sprite> can find them.
    /// </summary>
    public static class AutoConfigureUIAssets
    {
        [InitializeOnLoadMethod]
        public static void ConfigureAssets()
        {
            ConfigureAsSprite("Assets/Resources/UI/GameLogo.png");
            ConfigureAsSprite("Assets/Resources/UI/PremiumBackground.png");
        }

        private static void ConfigureAsSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                Debug.Log($"[AutoConfigureUIAssets] Automatically configured {path} as a Sprite (2D and UI).");
            }
        }
    }
}
