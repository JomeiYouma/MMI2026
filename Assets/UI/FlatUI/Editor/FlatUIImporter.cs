// Importe les sprites du pack « Complete UI Essential Pack » (Crusenho, CC BY 4.0) déposés dans Assets/UI/FlatUI/Sprites :
// Sprite aux pixels nets, sans compression ni mipmaps, 25 pixels par unité (un pixel du pack = 4 unités d'UI avec le
// Canvas par défaut à 100), et bordures 9-slice pour que boutons, cadres et bannières s'étirent sans déformer leur contour.
// Menu Tools > Flat UI > Réimporter les sprites si les réglages n'ont pas été appliqués.
using UnityEditor;
using UnityEngine;

public class FlatUIImporter : AssetPostprocessor
{
    public const string Folder = "Assets/UI/FlatUI/Sprites/";
    public const float PixelsPerUnit = 25f;

    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = PixelsPerUnit;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.alphaIsTransparency = true;
        imp.spriteBorder = Border(System.IO.Path.GetFileNameWithoutExtension(assetPath));
    }

    // Bordures en pixels du pack, dans l'ordre de Unity : gauche, bas, droite, haut.
    static Vector4 Border(string name)
    {
        // Bouton carré : contour 1 px, bande orange jusqu'à 6 px en bas, jusqu'à 4 px vides en haut selon l'enfoncement
        if (name.StartsWith("UI_Flat_Button01a")) return new Vector4(2, 7, 2, 5);
        // Bouton arrondi : coins de 4 px en plus
        if (name.StartsWith("UI_Flat_Button02a")) return new Vector4(4, 7, 4, 8);
        if (name.StartsWith("UI_Flat_Frame0")) return new Vector4(6, 6, 6, 6);
        // Bannière : les rubans des deux bouts restent entiers, seul le centre s'allonge
        if (name.StartsWith("UI_Flat_Banner")) return new Vector4(9, 5, 9, 5);
        if (name.StartsWith("UI_Flat_InputField")) return new Vector4(4, 4, 4, 4);
        // Crochets de sélection : un coin de 10 px à chaque angle
        if (name.StartsWith("UI_Flat_Select")) return new Vector4(10, 10, 10, 10);
        return Vector4.zero;
    }

    [MenuItem("Tools/Flat UI/Réimporter les sprites")]
    static void ReimportAll()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/') }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }
}
