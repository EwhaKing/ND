using UnityEngine;

public class MiniGameManager : MonoBehaviour
{
    [SerializeField] private CorrectSpot[] spots;  // 관리할 2개의 CorrectSpot 등록
    [SerializeField] private GameObject conclusionUI;  // 결론창 UI Panel
    public static MiniGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

    }
private void Start()
    {
        if (conclusionUI != null)
        {
            conclusionUI.SetActive(false);
        }
    }

    
    /// <summary>
    /// MiniGameDialogue에서 대화가 끝난 후 혹은 정답을 맞춘 후 호출하도록 설정
    /// </summary>
    public void CheckAllSolved()
    {
        Debug.Log("CheckAllSolved 실행");
        if (spots == null || spots.Length == 0) return;

        // 모든 CorrectSpot이 해결(IsSolved == true)되었는지 검사
        foreach (var spot in spots)
        {
            if (!spot.IsSolved)
            {
                return; // 아직 안 풀린 문제가 있다면 중단
            }
        }

        // 2개 문제 모두 해결 시 결론창 활성화
        Debug.Log("문제 모두 해결");
        ShowConclusionUI();
    }

    private void ShowConclusionUI()
    {
        Debug.Log("결론창 출력");
        if (conclusionUI != null)
        {
            conclusionUI.SetActive(true);
        }
    }
}