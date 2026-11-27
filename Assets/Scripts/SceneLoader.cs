using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Scene to load, set in the Inspector
    [SerializeField]
    private string m_SceneName;

    // Called by the button's On Click event
    public void LoadScene()
    {
        SceneManager.LoadScene(m_SceneName);
    }
}
