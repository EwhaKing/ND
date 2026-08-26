using UnityEngine;
using UnityEngine.UI;

public class InvestigationSceneController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField]
    private InvestigationManager investigationManager;

    [Header("Scene")]
    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Transform stageRoot;

    [Header("Stage Data")]
    [SerializeField]
    private InvestigationStageData chapter1RoofData;

    [SerializeField]
    private InvestigationStageData chapter1GroundData;

    private GameObject currentStageObject;

    private void Start()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogError(
                "GameProgressManager가 없습니다."
            );
            return;
        }

        InvestigationStageData data =
            GetStageData();

        if (data == null)
        {
            Debug.LogError(
                $"현재 단계에 맞는 조사 데이터가 없습니다: " +
                $"{GameProgressManager.Instance.CurrentStep}"
            );
            return;
        }

        LoadStage(data);
    }

    private InvestigationStageData GetStageData()
    {
        switch (GameProgressManager.Instance.CurrentStep)
        {
            case GameFlowStep.InvestigationRoof:
                return chapter1RoofData;

            case GameFlowStep.InvestigationGround:
                return chapter1GroundData;

            default:
                return null;
        }
    }

    private void LoadStage(
        InvestigationStageData data)
    {
        // 배경 교체
        if (backgroundImage != null)
        {
            backgroundImage.sprite =
                data.backgroundSprite;
        }

        // 기존 조사 Stage 제거
        if (currentStageObject != null)
        {
            Destroy(currentStageObject);
        }

        // 새로운 조사 Stage 생성
        currentStageObject = Instantiate(
            data.stagePrefab,
            stageRoot
        );

        // 위치 초기화
        RectTransform rect =
            currentStageObject.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        // InvestigationManager에게
        // 새 조사 오브젝트 목록 등록
        investigationManager.InitializeStage(
            currentStageObject.transform
        );
    }
}