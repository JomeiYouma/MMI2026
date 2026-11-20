// Toute image déposée dans un dossier « Icones » de CreaturesDuMod s'importe en Sprite aux pixels nets (icônes 16 px du mod) :
// prête pour une Image d'UI (Canvas) ou un SpriteRenderer, sans flou ni compression.
using UnityEditor;
using UnityEngine;

public class IconesDuMod : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/CreaturesDuMod/Icones/")) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 16;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.alphaIsTransparency = true;
    }
}
