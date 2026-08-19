using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JudgmentUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Button")]
    [SerializeField] private Button andButton;
    [SerializeField] private Button endButton;

    private JudgmentFlowManager manager;


    public void Initialize(JudgmentFlowManager flowManager)
    {
        manager = flowManager;

        andButton.onClick.AddListener(OnClickAND);
        endButton.onClick.AddListener(OnClickEND);
    }


    /// <summary>
    /// 심판 UI 표시
    /// </summary>
    public void Show(bool isFinal)
    {
        gameObject.SetActive(true);

        if (isFinal)
        {
            titleText.text = "최종 심판";

            descriptionText.text =
                "모든 진실을 확인했습니다.\n" +
                "이 영혼의 결말을 결정하십시오.";
        }
        else
        {
            titleText.text = "심판";

            descriptionText.text =
                "아직 밝혀지지 않은 진실이 남아 있습니다.\n" +
                "그럼에도 지금 심판하시겠습니까?";
        }
    }


    public void Hide()
    {
        gameObject.SetActive(false);
    }


    private void OnClickAND()
    {
        manager.SelectVerdict(JudgmentVerdict.AND);
    }


    private void OnClickEND()
    {
        manager.SelectVerdict(JudgmentVerdict.END);
    }
}
