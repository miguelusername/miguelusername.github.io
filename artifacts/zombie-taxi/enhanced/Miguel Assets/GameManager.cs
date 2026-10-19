using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Gameover UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Sound")]
    [SerializeField] TextMeshProUGUI musicVolumeText;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] AudioSource music;

    [Header("Upgrades")]
    [SerializeField] public TextMeshProUGUI UpgradeCounterText;

    private float score = 0f;
    private float highScore = 0f;
    private const string HighScoreKey = "HighScore";

    float defaultMusicVolume;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        defaultMusicVolume = music.volume;

        highScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);

        if (scoreText != null)
            scoreText.text = "Score: " + score.ToString("F0");

        if (highScoreText != null)
            highScoreText.text = "High Score: " + highScore.ToString("F0");
    }

    public void Restart()
    {
        SceneManager.LoadScene("Miguel Testing");
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void UpdateScore(float addScore)
    {
        score += addScore;

        if(scoreText != null)
            scoreText.text = "Score: " + score.ToString("F0");

        if(score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetFloat(HighScoreKey, highScore);
            PlayerPrefs.Save();

            if(highScoreText != null)
                highScoreText.text = "High Score: " + highScore.ToString("F0");
        }
    }
    public void OnMusicSliderChange()
    {
        float amount = musicVolumeSlider.value;

        musicVolumeText.text = ((int)amount).ToString();

        float percent = amount / 100;

        music.volume = defaultMusicVolume * percent;
    }
}
