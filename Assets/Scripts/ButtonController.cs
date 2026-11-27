using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Icon button with a label: each instance sets its own text and icon sprite in the Inspector.
public class ButtonController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_Title;

    [SerializeField]
    private Image m_Icon;

    [SerializeField]
    private Sprite m_Sprite;

    [SerializeField]
    private string m_Text = "Button";

    [SerializeField]
    private Color m_Color = Color.magenta;

    void Start()
    {
        Apply();
    }

#if UNITY_EDITOR
    // Keeps the edit-mode preview in sync with the Inspector. Deferred because resizing a
    // RectTransform inside OnValidate makes Unity log SendMessage warnings.
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) Apply();
        };
    }
#endif

    void Apply()
    {
        if (m_Title != null) m_Title.text = m_Text;
        if (m_Icon != null)
        {
            m_Icon.sprite = m_Sprite;
            // Native size keeps every icon pixel-perfect (one pack pixel = 4 UI units), whatever its dimensions
            if (m_Sprite != null) m_Icon.SetNativeSize();
        }
    }

    // Called by the button's On Click event (wired in the StylishButton prefab)
    public void ChangeIconColor()
    {
        if (m_Icon != null) m_Icon.color = m_Color;
    }
}