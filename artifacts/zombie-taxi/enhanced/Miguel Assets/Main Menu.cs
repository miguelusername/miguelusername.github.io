using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayZombieTaxi()
    {
        SceneManager.LoadScene("Miguel Testing");
    }

    public void PlayBolts()
    {
        SceneManager.LoadScene("Avery_HateYourBolts_City");

    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
