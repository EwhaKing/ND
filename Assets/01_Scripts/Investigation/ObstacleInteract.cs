using UnityEngine;

/// <summary>
/// 단서를 가리고 있는 장애물 (옷장 속 옷, 참고서, 수납상자, 베개 등)
/// 클릭하면 장애물이 사라지고, 가려져 있던 단서를 조사할 수 있게 된다.
/// </summary>
public class ObstacleInteract : MonoBehaviour
{
    [Tooltip("이 장애물이 가리고 있는 단서 오브젝트 (ClueInteract가 붙은 오브젝트)")]
    [SerializeField] private GameObject hiddenClueObject;

    private void Start()
    {
        // 세이브를 불러왔을 때 이미 얻은 단서라면 장애물은 치워진 상태로 시작
        ClueInteract clue = hiddenClueObject != null
            ? hiddenClueObject.GetComponent<ClueInteract>()
            : null;

        bool alreadyFound =
            clue != null &&
            clue.clueData != null &&
            GameProgressManager.Instance != null &&
            GameProgressManager.Instance.HasClue(clue.clueData.clueID);

        if (alreadyFound)
        {
            Remove();
        }
        else if (hiddenClueObject != null)
        {
            hiddenClueObject.SetActive(false);   // 처음엔 단서를 숨겨둔다
        }
    }

    // 장애물 Button의 OnClick에 연결
    public void OnClickObstacle()
    {
        if (ClueDetailPopup.Instance != null && ClueDetailPopup.Instance.IsBusy) return;
        Remove();
    }

    private void Remove()
    {
        if (hiddenClueObject != null) hiddenClueObject.SetActive(true);
        gameObject.SetActive(false);
    }
}
