using UnityEditor;
using UnityEngine;

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

            // 위로 이동
            GUI.enabled = i > 0;

            if (GUILayout.Button("▲", GUILayout.Width(30)))
            {
                SwapSteps(i, i - 1);
                GUI.enabled = true;
                break;
            }

            // 아래로 이동
            GUI.enabled = i < scenarioData.steps.Count - 1;

            if (GUILayout.Button("▼", GUILayout.Width(30)))
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

            case ScenarioStepType.CharacterAnimation:
                step.character =
                    (CharacterType)EditorGUILayout.EnumPopup(
                        "Character",
                        step.character
                    );

                step.animationTrigger =
                    EditorGUILayout.TextField(
                        "Animation Trigger",
                        step.animationTrigger
                    );
                break;

            case ScenarioStepType.StandingShow:
                step.standingSprite =
                    (Sprite)EditorGUILayout.ObjectField(
                        "Standing Sprite",
                        step.standingSprite,
                        typeof(Sprite),
                        false
                    );

                step.standingPosition =
                    (StandingPosition)EditorGUILayout.EnumPopup(
                        "Position",
                        step.standingPosition
                    );
                break;

            case ScenarioStepType.StandingHide:
                EditorGUILayout.HelpBox(
                    "현재 표시 중인 스탠딩을 숨깁니다.",
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
}