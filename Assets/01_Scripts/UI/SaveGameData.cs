using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveGameData
{
    // 1. 진행 위치 정보
    public GameFlowStep currentProgressStep;
    public int scenarioStepIndex;

    // 2. 단서 및 진행 상태 (단서 이름 리스트)
    public List<string> acquiredClues = new List<string>();
    public JudgmentVerdict finalVerdict;

    // 3. UI 및 슬롯 관리용 정보
    public string saveTime;
    public bool hasData = false;
}