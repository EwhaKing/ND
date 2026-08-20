using UnityEngine;

public class JudgmentTest : MonoBehaviour
{
    [SerializeField]
    private JudgmentFlowManager judgmentFlowManager;

    private void Start()
    {
        judgmentFlowManager.StartJudgmentFlow();
    }

    private void Update()
    {
        // 논파 완료를 임시로 테스트
        if (Input.GetKeyDown(KeyCode.N))
        {
            judgmentFlowManager.OnDigDeeperFinished();
        }
    }
}