using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScenarioData
///
/// 담당:
/// - 비주얼 노벨식 대화/연출 진행 순서를 ScriptableObject 데이터로 관리
/// - 하나의 시나리오 안에 Dialogue, Wait, StandingShow, StandingHide, Choice 등의 Step을 순서대로 저장
/// - 선택지 선택 이후 짧은 반응 대사, 스탠딩 변경, 대기 시간 같은 ReactionStep을 구성
///
/// 사용 위치:
/// - Unity 에디터에서 ScenarioData 에셋으로 생성하여 사용
/// - 각 챕터나 장면의 대화 흐름, 선택지, 스탠딩 연출 순서를 데이터로 관리할 때 사용
///
/// 연결:
/// - ScenarioRunner가 이 데이터를 읽어 실제 대화와 연출을 실행
/// - DialogueDatabase의 dialogueId와 연결되어 실제 대사 내용을 불러옴
/// - ChoiceController는 ChoiceData를 받아 선택지 버튼을 생성
/// - StandingController는 StandStep 또는 ReactionStep의 스프라이트 정보를 받아 스탠딩을 제어
///
/// TODO:
/// - Narration / CG / Command 타입 Step 추가 검토
/// - 선택지 결과가 ChapterManager, GameFlagManager에 전달되도록 확장
/// - 씬 이동 대신 GameState 전환 방식으로 LoadScene 로직 개선 검토
/// - 구글 스프레드시트 기반 시나리오 데이터와 현재 ScriptableObject 구조의 역할 분리 필요
/// 
/// - 한글 깨짐이 있는 Debug 문자열 정리 필요
/// </summary>


/// <summary>
/// 시나리오에서 실행할 Step의 종류를 정의
/// Dialogue는 대화 출력, Wait는 대기, StandingShow/Hide는 스탠딩 제어, Choice는 플레이어 선택지 출력을 의미
/// </summary>
public enum ScenarioStepType
{
    Dialogue,
    Wait,
    StandingShow,
    StandingHide,
    Choice
}

/// <summary>
/// 선택지를 눌렀을 때 실행할 행동 종류를 정의
/// 다음 Step으로 진행, 반응 연출 후 진행, 씬 이동 구분
/// </summary>
public enum ChoiceActionType
{
    NextStep,
    ReactionThenNext,
    LoadScene
}

/// <summary>
/// 선택지 선택 직후 실행되는 짧은 반응 Step의 종류를 정의
/// 대화 출력, 스탠딩 변경, 대기 연출을 처리
/// </summary>
public enum ReactionStepType
{
    Dialogue,
    StandingChange,
    Wait
}

/// <summary>
/// 시나리오를 구성하는 하나의 진행 단위
/// Step 타입에 따라 대화 ID, 대기 시간, 스탠딩 정보, 선택지 목록 중 필요한 데이터를 사용
/// </summary>
[Serializable]
public class ScenarioStep
{
    public ScenarioStepType stepType;

    [Header("Dialogue")]
    public string dialogueId;

    [Header("Wait")]
    [Min(0f)]
    public float waitSeconds = 1f;

    [Header("Standing")]
    public StandStep[] stands;

    [Header("Choice")]
    public List<ChoiceData> choices = new();
}

/// <summary>
/// 선택지 하나의 정보를 담는 데이터
/// 버튼에 표시될 문구와 선택 후 실행할 행동, 반응 Step, 이동할 씬 이름을 관리
/// </summary>
[Serializable]
public class ChoiceData
{
    public string choiceText;

    public ChoiceActionType actionType;

    [Tooltip("ReactionThenNext�� �� ������ ª�� ����")]
    public List<ReactionStep> reactionSteps =
        new List<ReactionStep>();

    [Tooltip("LoadScene�� �� �̵��� ��")]
    public string targetScene;
}

/// <summary>
/// 스탠딩 캐릭터 출력에 필요한 정보를 담는 데이터
/// speaker 이름과 연결되는 standName, 출력할 Sprite를 관리
/// </summary>
[Serializable]
public class StandStep
{
    [Tooltip("CSV�� speaker �̸��� ���ƾ� �մϴ�.")]
    public string standName;

    public Sprite sprite;
}

/// <summary>
/// 선택지 선택 직후 실행되는 짧은 반응 연출 데이터
/// 대화, 스탠딩 변경, 대기 시간 중 하나를 실행 가능
/// </summary>
[Serializable]
public class ReactionStep
{
    public ReactionStepType stepType;

    [Header("Dialogue")]
    public string dialogueId;

    [Header("Standing")]
    public string standName;
    public Sprite standingSprite;

    [Header("Wait")]
    [Min(0f)]
    public float waitSeconds = 0.5f;
}

[CreateAssetMenu(
    fileName = "ScenarioData",
    menuName = "Scenario/Scenario Data"
)]
public class ScenarioData : ScriptableObject
{
    public string scenarioId;

    public List<ScenarioStep> steps = new();
}
