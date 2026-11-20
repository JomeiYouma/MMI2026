// Bouton du pack Flat UI : un Button ordinaire (Transition « Sprite Swap ») dont le texte suit le dessus du bouton.
// Les quatre images du pack montrent le bouton plus ou moins enfoncé : le dessus descend de 0 à 4 px. Sans ce décalage,
// le texte resterait immobile pendant que le bouton s'enfonce sous lui.
// Image de repos : _3 ; survol : _4 (monte de 1 px) ; appui : _1 (descend de 3 px) ; désactivé : _2 (descend de 2 px).
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Flat UI/Flat Button")]
public class FlatButton : Button
{
    RectTransform label;
    Vector2 labelRest;
    bool hasRest;

    protected override void Awake()
    {
        base.Awake();
        CacheLabel();
    }

    void CacheLabel()
    {
        if (hasRest) return;
        var text = GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        label = text.rectTransform;
        labelRest = label.anchoredPosition;
        hasRest = true;
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        // En mode édition, ne rien déplacer : la position enregistrée dans la scène reste celle du repos
        if (!Application.isPlaying) return;
        CacheLabel();
        if (label == null) return;

        float px = state switch
        {
            SelectionState.Highlighted => 1f,
            SelectionState.Pressed => -3f,
            SelectionState.Disabled => -2f,
            _ => 0f,
        };
        label.anchoredPosition = labelRest + new Vector2(0f, px * PackPixel());
    }

    // Taille d'un pixel du pack en unités d'UI (4 avec le Canvas par défaut et les sprites à 25 pixels par unité)
    float PackPixel()
    {
        var img = image;
        if (img == null || img.sprite == null || img.canvas == null) return 4f;
        return img.canvas.referencePixelsPerUnit / (img.sprite.pixelsPerUnit * img.pixelsPerUnitMultiplier);
    }
}
