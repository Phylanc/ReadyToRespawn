using UnityEngine;
using UnityEngine.SceneManagement;

public class EscToMenu : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
