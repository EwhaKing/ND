using UnityEngine;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    private const string PlayCountKey = "PlayCount";

    [Header("Progress")]
    [SerializeField] private int playCount=1;

    public int PlayCount => playCount;

    public bool CanSkip => playCount >= 2;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProgress();
    }

    private void LoadProgress()
    {
        playCount =
            PlayerPrefs.GetInt(
                PlayCountKey,
                1
            );
    }

    public void CompletePlaythrough()
    {
        playCount++;

        PlayerPrefs.SetInt(
            PlayCountKey,
            playCount
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"회차 증가: {playCount}회차"
        );
    }
    // 테스트용 Playcount감소
    public void DiscountPlay()
    {
        playCount--;
        PlayerPrefs.SetInt(
            PlayCountKey,
            playCount
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"회차 감소: {playCount}회차"
        );
    }
}