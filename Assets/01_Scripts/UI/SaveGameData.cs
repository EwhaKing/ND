using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveGameData
{
    public string scenarioId;            // 저장 시점의 시나리오 이름 (참고용)

    // 1. 진행 위치 정보
    public GameFlowStep currentProgressStep;
    public int scenarioStepIndex;        // 대화 씬에서 저장한 경우, 이어서 시작할 Step 번호

    // 로드할 때 진입해야 할 Unity 씬 이름 (예: "ChatScene", "FindScene" 등)
    public string targetSceneName;

    // 2. 단서 및 진행 상태
    // 이제 clueName 이 아니라 ClueData.clueID 를 저장합니다.
    // (예전 세이브에는 clueName 이 들어 있으므로, 복원할 때 ID/이름 둘 다 검색합니다)
    public List<string> acquiredClues = new List<string>();
    public JudgmentVerdict finalVerdict;

    // 3. UI 및 슬롯 관리용 정보
    public string saveTime;
    public bool hasData = false;
}