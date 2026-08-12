using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScenarioDataEditor
///
/// 담당:
/// - ScenarioData ScriptableObject를 Unity Inspector에서 편집하기 쉽게 보여주는 커스텀 에디터
/// - 시나리오 ID, Step 목록, Step 타입별 입력 필드를 직접 그림
/// - Step 추가, 삭제, 순서 이동 기능을 제공
/// - Choice Step의 선택지와 선택 후 ReactionStep을 Inspector에서 편집할 수 있도록 지원
/// - StandingShow Step에서 여러 스탠딩 캐릭터 정보를 배열 형태로 편집할 수 있도록 지원
///
/// 사용 위치:
/// - Editor 폴더 안에 위치
/// - ScenarioData 에셋을 선택했을 때 Unity Inspector에 커스텀 편집 UI를 표시
///
/// 연결:
/// - ScenarioData, ScenarioStep, ChoiceData, StandStep, ReactionStep 구조와 직접 연결
/// - ScenarioRunner가 실행할 시나리오 데이터를 에디터에서 구성하기 위한 도구 역할
///
/// TODO:
/// - Narration / CG / Command 타입 Step이 추가될 경우 Inspector 입력 필드 확장
/// - ChoiceActionType.LoadScene 대신 GameState 전환 방식 사용 여부 검토
/// - Step 수가 많아질 경우 접기/펼치기 Foldout 기능 추가 검토
/// - 데이터 변경 시 Undo.RecordObject 적용 검토
/// 
/// </summary>
[CustomEditor(typeof(ScenarioData))]
public class ScenarioDataEditor : Editor
{
    private ScenarioData scenarioData;

    private void OnEnable()
    {
        scenarioData = (ScenarioData)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        scenarioData.scenarioId =
            EditorGUILayout.TextField(
                "Scenario ID",
                scenarioData.scenarioId
            );

        EditorGUILayout.Space(10);

        for (int i = 0; i < scenarioData.steps.Count; i++)
        {
            ScenarioStep step = scenarioData.steps[i];

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"Step {i + 1}",
                EditorStyles.boldLabel
            );

           
            GUI.enabled = i > 0;

            if (GUILayout.Button("up", GUILayout.Width(30)))
            {
                SwapSteps(i, i - 1);
                GUI.enabled = true;
                break;
            }

          
            GUI.enabled = i < scenarioData.steps.Count - 1;

            if (GUILayout.Button("down", GUILayout.Width(40)))
            {
                SwapSteps(i, i + 1);
                GUI.enabled = true;
                break;
            }

            GUI.enabled = true;

            if (GUILayout.Button("Remove", GUILayout.Width(50)))
            {
                scenarioData.steps.RemoveAt(i);
                EditorUtility.SetDirty(scenarioData);
                break;
            }

            EditorGUILayout.EndHorizontal();

            step.stepType =
                (ScenarioStepType)EditorGUILayout.EnumPopup(
                    "Step Type",
                    step.stepType
                );

            EditorGUILayout.Space(4);

            DrawStepFields(step);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        if (GUILayout.Button("Add Step"))
        {
            scenarioData.steps.Add(new ScenarioStep());
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(scenarioData);
        }

        serializedObject.ApplyModifiedProperties();
    }
    private void DrawStepFields(ScenarioStep step)
    {
        switch (step.stepType)
        {
            case ScenarioStepType.Dialogue:
                step.dialogueId =
                    EditorGUILayout.TextField(
                        "Dialogue ID",
                        step.dialogueId
                    );
                break;

            case ScenarioStepType.Wait:
                step.waitSeconds =
                    EditorGUILayout.FloatField(
                        "Wait Seconds",
                        step.waitSeconds
                    );

                step.waitSeconds =
                    Mathf.Max(0f, step.waitSeconds);
                break;

            case ScenarioStepType.StandingShow:
                DrawStandingFields(step);
                break;

            case ScenarioStepType.StandingHide:
                EditorGUILayout.HelpBox(
                    "Standing Hide.",
                    MessageType.Info
                );
                break;

            case ScenarioStepType.Choice:
                DrawChoices(step);
                break;

            case ScenarioStepType.CGShow:
                step.cgSprite =
                    (Sprite)EditorGUILayout.ObjectField(
                        "CG Sprite",
                        step.cgSprite,
                        typeof(Sprite),
                        false
                    );
                break;


            case ScenarioStepType.CGHide:
                EditorGUILayout.HelpBox(
                    "현재 표시 중인 CG를 제거합니다.",
                    MessageType.Info
                );
                break;
        }
    }
    private void SwapSteps(int firstIndex, int secondIndex)
    {
        if (firstIndex < 0 ||
            secondIndex < 0 ||
            firstIndex >= scenarioData.steps.Count ||
            secondIndex >= scenarioData.steps.Count)
        {
            return;
        }

        ScenarioStep temporaryStep =
            scenarioData.steps[firstIndex];

        scenarioData.steps[firstIndex] =
            scenarioData.steps[secondIndex];

        scenarioData.steps[secondIndex] =
            temporaryStep;

        EditorUtility.SetDirty(scenarioData);
        serializedObject.Update();
    }
    private void DrawChoices(ScenarioStep step)
    {
        EditorGUILayout.LabelField(
            "Choices",
            EditorStyles.boldLabel
        );

        for (int i = 0; i < step.choices.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            ChoiceData choice = step.choices[i];

            choice.choiceText =
                EditorGUILayout.TextField(
                "Button Text",
                choice.choiceText
                );

            choice.actionType = (ChoiceActionType)EditorGUILayout.EnumPopup(
                "Action",
                choice.actionType
                );

        switch (choice.actionType)
{
            case ChoiceActionType.NextStep:
                EditorGUILayout.HelpBox(
                "Next Step.",
                MessageType.Info
                );
            break;

            case ChoiceActionType.ReactionThenNext:
            DrawReactionSteps(choice);
            break;

            case ChoiceActionType.LoadScene:
                choice.targetScene =
                EditorGUILayout.TextField(
                "Target Scene",
                choice.targetScene
                );
            break;
}

            if (GUILayout.Button("Del"))
            {
                step.choices.RemoveAt(i);
                break;
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add"))
        {
            step.choices.Add(new ChoiceData());
        }

    }
    private void DrawStandingFields(ScenarioStep step)
    {
        EditorGUILayout.LabelField(
            "Standing Characters",
            EditorStyles.boldLabel
        );

        if (step.stands == null)
        {
            step.stands = new StandStep[0];
        }

        int newSize = EditorGUILayout.IntField(
            "Size",
            step.stands.Length
        );

        newSize = Mathf.Max(0, newSize);

        if (newSize != step.stands.Length)
        {
            System.Array.Resize(
                ref step.stands,
                newSize
            );

            for (int i = 0; i < step.stands.Length; i++)
            {
                if (step.stands[i] == null)
                {
                    step.stands[i] = new StandStep();
                }
            }
        }

        for (int i = 0; i < step.stands.Length; i++)
        {
            if (step.stands[i] == null)
            {
                step.stands[i] = new StandStep();
            }

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField(
                $"Standing {i + 1}",
                EditorStyles.boldLabel
            );

            step.stands[i].standName =
                EditorGUILayout.TextField(
                    "Stand Name",
                    step.stands[i].standName
                );

            /*step.stands[i].sprite =
                (Sprite)EditorGUILayout.ObjectField(
                    "Sprite",
                    step.stands[i].sprite,
                    typeof(Sprite),
                    false
                );*/

            EditorGUILayout.EndVertical();
        }
    }
    private void DrawReactionSteps(ChoiceData choice)
    {
        if (choice.reactionSteps == null)
        {
            choice.reactionSteps =
                new List<ReactionStep>();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Reaction Steps",
            EditorStyles.boldLabel
        );

        for (int i = 0;
             i < choice.reactionSteps.Count;
             i++)
        {
            ReactionStep reaction =
                choice.reactionSteps[i];

            if (reaction == null)
            {
                reaction = new ReactionStep();
                choice.reactionSteps[i] = reaction;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"Reaction {i + 1}",
                EditorStyles.boldLabel
            );

            if (GUILayout.Button("up", GUILayout.Width(30)) &&
                i > 0)
            {
                SwapReactionSteps(
                    choice.reactionSteps,
                    i,
                    i - 1
                );

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            if (GUILayout.Button("down", GUILayout.Width(30)) &&
                i < choice.reactionSteps.Count - 1)
            {
                SwapReactionSteps(
                    choice.reactionSteps,
                    i,
                    i + 1
                );

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            if (GUILayout.Button("remove", GUILayout.Width(50)))
            {
                choice.reactionSteps.RemoveAt(i);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            reaction.stepType =
                (ReactionStepType)EditorGUILayout.EnumPopup(
                    "Reaction Type",
                    reaction.stepType
                );

            DrawReactionFields(reaction);

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Reaction Add"))
        {
            choice.reactionSteps.Add(
                new ReactionStep()
            );
        }
    }
    private void DrawReactionFields(ReactionStep reaction)
    {
        switch (reaction.stepType)
        {
            case ReactionStepType.Dialogue:
                reaction.dialogueId =
                    EditorGUILayout.TextField(
                        "Dialogue ID",
                        reaction.dialogueId
                    );
                break;

            /*case ReactionStepType.StandingChange:
                reaction.standName =
                    EditorGUILayout.TextField(
                        "Stand Name",
                        reaction.standName
                    );

                reaction.standingSprite =
                    (Sprite)EditorGUILayout.ObjectField(
                        "Standing Sprite",
                        reaction.standingSprite,
                        typeof(Sprite),
                        false
                    );
                break;*/

            case ReactionStepType.Wait:
                reaction.waitSeconds =
                    EditorGUILayout.FloatField(
                        "Wait Seconds",
                        reaction.waitSeconds
                    );

                reaction.waitSeconds =
                    Mathf.Max(0f, reaction.waitSeconds);
                break;
        }
    }
    private void SwapReactionSteps(
        List<ReactionStep> steps,
        int firstIndex, int secondIndex)
    {
        if (steps == null ||
            firstIndex < 0 ||
            secondIndex < 0 ||
            firstIndex >= steps.Count ||
            secondIndex >= steps.Count)
        {
            return;
        }

        ReactionStep temporary =
            steps[firstIndex];

        steps[firstIndex] =
            steps[secondIndex];

        steps[secondIndex] =
            temporary;

        EditorUtility.SetDirty(scenarioData);
    }
}