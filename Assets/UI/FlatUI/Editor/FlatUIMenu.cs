// Pose les éléments du pack Flat UI dans la scène : menu GameObject > UI > Flat UI (ou clic droit dans la Hiérarchie).
// Bouton : FlatButton en Sprite Swap sur les quatre images du pack, texte TextMeshPro qui suit l'enfoncement.
// Panneau : cadre gris étirable. Menu : panneau, bannière de titre et trois boutons rangés par un Vertical Layout Group.
// Crée au besoin un Canvas (1920×1080, mise à l'échelle avec l'écran) et un EventSystem adapté au nouvel Input System.
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class FlatUIMenu
{
    const string S = FlatUIImporter.Folder + "UI_Flat_";
    static readonly Color Ink = new Color32(0x2B, 0x26, 0x22, 0xFF);

    static Sprite Load(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(S + name + ".png");
        if (sprite == null) Debug.LogWarning($"Flat UI : sprite introuvable {S}{name}.png (menu Tools > Flat UI > Réimporter les sprites)");
        return sprite;
    }

    [MenuItem("GameObject/UI/Flat UI/Bouton", false, 2000)]
    static void CreateButton(MenuCommand cmd) => Finish(Button("Bouton", "Jouer", Parent(cmd)));

    [MenuItem("GameObject/UI/Flat UI/Bouton arrondi", false, 2001)]
    static void CreateRoundButton(MenuCommand cmd) => Finish(Button("Bouton arrondi", "Jouer", Parent(cmd), round: true));

    [MenuItem("GameObject/UI/Flat UI/Panneau", false, 2002)]
    static void CreatePanel(MenuCommand cmd) => Finish(Panel("Panneau", Parent(cmd), new Vector2(480, 360)));

    [MenuItem("GameObject/UI/Flat UI/Menu (panneau, titre, 3 boutons)", false, 2003)]
    static void CreateMenu(MenuCommand cmd)
    {
        var panel = Panel("Menu", Parent(cmd), new Vector2(480, 520));
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(56, 56, 40, 48);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var banner = NewUI("Titre", panel);
        banner.sizeDelta = new Vector2(0, 80);
        var bannerImg = banner.gameObject.AddComponent<Image>();
        bannerImg.sprite = Load("Banner01a");
        bannerImg.type = Image.Type.Sliced;
        var title = Text(banner, "Mon jeu", 40);
        title.rectTransform.offsetMin = new Vector2(40, 20);
        title.rectTransform.offsetMax = new Vector2(-40, -20);
        banner.gameObject.AddComponent<LayoutElement>().minHeight = 80;

        foreach (var label in new[] { "Jouer", "Options", "Quitter" })
            Button("Bouton " + label, label, panel);

        Finish(panel);
    }

    static RectTransform Button(string name, string label, Transform parent, bool round = false)
    {
        var id = round ? "Button02a_" : "Button01a_";
        var rt = NewUI(name, parent);
        rt.sizeDelta = new Vector2(320, 96);

        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Load(id + "3");
        img.type = Image.Type.Sliced;

        var button = rt.gameObject.AddComponent<FlatButton>();
        button.transition = Selectable.Transition.SpriteSwap;
        button.targetGraphic = img;
        button.spriteState = new SpriteState
        {
            highlightedSprite = Load(id + "4"),
            pressedSprite = Load(id + "1"),
            selectedSprite = Load(id + "3"),
            disabledSprite = Load(id + "2"),
        };

        // Le texte occupe le dessus du bouton : la bande orange du bas (6 px du pack) et le contour restent libres
        var text = Text(rt, label, 36);
        text.rectTransform.offsetMin = new Vector2(16, 28);
        text.rectTransform.offsetMax = new Vector2(-16, -8);
        return rt;
    }

    static RectTransform Panel(string name, Transform parent, Vector2 size)
    {
        var rt = NewUI(name, parent);
        rt.sizeDelta = size;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Load("Frame01a");
        img.type = Image.Type.Sliced;
        return rt;
    }

    static TextMeshProUGUI Text(RectTransform parent, string value, float size)
    {
        var rt = NewUI("Texte", parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = size;
        tmp.color = Ink;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    // Parent : l'objet choisi s'il est sous un Canvas, sinon le premier Canvas de la scène, sinon un nouveau
    static Transform Parent(MenuCommand cmd)
    {
        var selected = cmd.context as GameObject;
        if (selected != null && selected.GetComponentInParent<Canvas>() != null) return selected.transform;

        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(go, "Créer Canvas");
        }

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(es, "Créer EventSystem");
        }
        return canvas.transform;
    }

    static void Finish(RectTransform rt)
    {
        Undo.RegisterCreatedObjectUndo(rt.gameObject, "Créer " + rt.name);
        Selection.activeGameObject = rt.gameObject;
    }
}
