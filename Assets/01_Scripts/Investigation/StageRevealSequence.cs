using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// StageRevealSequence
///
/// 담당:
/// - 조사 스테이지 프리팹 안에서 "버튼 클릭 → 대사 출력 → (대사 종료 후) 이미지 교체 + 확대 → 단서 오브젝트 활성화"
///   같은 스테이지 전용 연출을 인스펙터 설정만으로 실행
/// - 대사(이름/스탠딩/내용), 확대 대상/배율/위치/시간, 이미지 교체, 활성화/비활성화할 오브젝트를
///   모두 인스펙터에서 지정할 수 있어 같은 스크립트를 여러 연출에 재사용 가능
/// - 대사가 끝난 뒤에 이미지 교체와 확대가 시작되고, 확대가 끝나면 연결된 오브젝트를 켜고/끔
/// - 세이브 로드 등으로 연출 결과가 이미 반영되어야 하는 경우, 연출 없이 최종 상태로 바로 시작
///
/// 연출 순서:
/// 1. 트리거 버튼 클릭 (버튼 중복 클릭 방지)
/// 2. 대사 출력 (대사가 없으면 건너뜀)
/// 3. 대사 종료 후 이미지 교체 + 확대 시작
/// 4. 확대 종료 후 활성화/비활성화 처리 및 종료 이벤트 호출
///
/// 사용 위치:
/// - 연출이 필요한 조사 스테이지 프리팹의 아무 오브젝트에 부착 (한 프리팹에 여러 개 부착 가능)
///   (시작부터 활성 상태인 오브젝트에 붙일 것)
///
/// 연결:
/// - PointClickDialogueManager.ShowTexts()로 대사를 출력하고, onDialogueClosedCallback으로 종료를 감지
/// - GameProgressManager.HasClue()로 이미 획득한 단서가 있는지 확인 (복원용)
/// - InvestigationManager / ClueInteract / InvestigationSceneController는 수정하지 않음
///
/// 주의:
/// - 활성화할 단서 오브젝트는 프리팹에서 비활성 상태로 저장해 둘 것
///   (InvestigationManager는 비활성 오브젝트도 수집하므로 조사율 계산에는 정상 반영됨)
/// - 이 연출 하나는 한 번만 실행됨 (실행 후 트리거 버튼 비활성화)
/// - 트리거 버튼의 Transition을 None으로 두지 않으면 비활성화 시 색이 흐려질 수 있음
///
/// TODO:
/// - 연출 중 다른 오브젝트 클릭을 막는 별도 차단막이 필요한지 확인
/// - 확대 상태를 되돌리는 기능이 필요하면 별도 옵션으로 추가
/// </summary>
public class StageRevealSequence : MonoBehaviour
{
    [Header("트리거")]
    [SerializeField] private Button triggerButton;

    [Header("대사")]
    [SerializeField] private DialogueEntry[] dialogue;

    [Header("확대 연출")]
    [SerializeField] private bool useZoom = true;
    [Tooltip("확대할 UI 오브젝트 (Rect Transform)")]
    [SerializeField] private RectTransform zoomTarget;
    [SerializeField] private float zoomScale = 2f;
    [Tooltip("확대가 끝났을 때의 Anchored Position (Rect Transform 기준)")]
    [SerializeField] private Vector2 zoomPosition = Vector2.zero;
    [SerializeField] private float zoomDuration = 0.6f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("이미지 교체 (선택)")]
    [SerializeField] private Image swapTargetImage;
    [SerializeField] private Sprite swapSprite;

    [Header("연출 종료 후 처리")]
    [Tooltip("연출이 끝난 뒤 켤 오브젝트")]
    [SerializeField] private GameObject[] activateObjects;
    [Tooltip("연출이 끝난 뒤 끌 오브젝트")]
    [SerializeField] private GameObject[] deactivateObjects;

    [Tooltip("연출이 끝난 뒤 Raycast Target을 끌 Graphic")]
    [SerializeField] private Graphic[] disableRaycastOnFinish;

    [Header("세이브/로드 복원 (선택)")]
    [Tooltip("이 중 하나라도 이미 획득한 상태면 연출 없이 최종 상태로 시작 (예: 이 연출로 열리는 단서들)")]
    [SerializeField] private ClueData[] restoreIfAnyOwned;

    [Header("이벤트 (효과음 등 추가 처리용)")]
    public UnityEvent onSequenceStarted;  // 트리거 클릭 직후 (대사 시작 전)
    public UnityEvent onSequenceFinished; // 모든 연출이 끝난 뒤 (복원 시에는 호출되지 않는다)

    private bool hasPlayed = false;
    private System.Action closedCallback;

    private void Awake()
    {
        closedCallback = OnDialogueClosed;
    }

    private void Start()
    {
        if (triggerButton == null)
        {
            triggerButton = GetComponent<Button>();
        }

        if (ShouldRestore())
        {
            ApplyFinalStateImmediately();
            return;
        }

        if (triggerButton != null)
        {
            triggerButton.onClick.AddListener(OnTriggerClicked);
        }
        else
        {
            Debug.LogWarning($"[StageRevealSequence] {name}: 트리거 버튼이 연결되지 않았습니다.", this);
        }
    }

    private void OnDestroy()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.RemoveListener(OnTriggerClicked);
        }

        // 이 연출이 대사 종료 콜백으로 남아 있다면 제거 (씬 전환/파괴 중 호출 방지)
        if (PointClickDialogueManager.Instance != null &&
            PointClickDialogueManager.Instance.onDialogueClosedCallback == closedCallback)
        {
            PointClickDialogueManager.Instance.onDialogueClosedCallback = null;
        }
    }

    // 이미 획득한 연결 단서가 있다면 복원 대상
    private bool ShouldRestore()
    {
        if (restoreIfAnyOwned == null || restoreIfAnyOwned.Length == 0) return false;
        if (GameProgressManager.Instance == null) return false;

        foreach (ClueData clue in restoreIfAnyOwned)
        {
            if (clue != null && GameProgressManager.Instance.HasClue(clue.clueID))
            {
                return true;
            }
        }

        return false;
    }

    // 1단계: 트리거 클릭 → 대사 출력
    private void OnTriggerClicked()
    {
        if (hasPlayed) return;
        hasPlayed = true;

        // 연출 중 중복 클릭 방지
        if (triggerButton != null) triggerButton.interactable = false;

        onSequenceStarted?.Invoke();

        if (HasAnyDialogueText() && PointClickDialogueManager.Instance != null)
        {
            PointClickDialogueManager.Instance.onDialogueClosedCallback = closedCallback;
            PointClickDialogueManager.Instance.ShowTexts(dialogue, false);
        }
        else
        {
            // 대사가 없으면 바로 다음 단계로
            StartZoomThenFinish();
        }
    }

    // 2단계: 대사 종료 → 이미지 교체 + 확대 시작
    private void OnDialogueClosed()
    {
        StartZoomThenFinish();
    }

    private void StartZoomThenFinish()
    {
        ApplySwapSprite();

        if (useZoom && zoomTarget != null)
        {
            StartCoroutine(ZoomRoutine());
        }
        else
        {
            FinishSequence();
        }
    }

    private bool HasAnyDialogueText()
    {
        if (dialogue == null) return false;

        foreach (DialogueEntry entry in dialogue)
        {
            if (!string.IsNullOrEmpty(entry.text)) return true;
        }

        return false;
    }

    // 3단계: 확대 연출
    private IEnumerator ZoomRoutine()
    {
        Vector3 startScale = zoomTarget.localScale;
        Vector3 endScale = Vector3.one * zoomScale;
        Vector2 startPos = zoomTarget.anchoredPosition;

        if (zoomDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < zoomDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = zoomCurve.Evaluate(Mathf.Clamp01(elapsed / zoomDuration));

                zoomTarget.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
                zoomTarget.anchoredPosition = Vector2.LerpUnclamped(startPos, zoomPosition, t);

                yield return null;
            }
        }

        // 보간 오차 없이 최종값으로 확정
        zoomTarget.localScale = endScale;
        zoomTarget.anchoredPosition = zoomPosition;

        FinishSequence();
    }

    // 4단계: 마무리 (오브젝트 활성화/비활성화, 종료 이벤트)
    private void FinishSequence()
    {
        ApplyObjectStates();
        onSequenceFinished?.Invoke();
    }

    // 세이브 로드 복원용 (연출 제거)
    private void ApplyFinalStateImmediately()
    {
        hasPlayed = true;

        if (triggerButton != null) triggerButton.interactable = false;

        ApplySwapSprite();

        if (useZoom && zoomTarget != null)
        {
            zoomTarget.localScale = Vector3.one * zoomScale;
            zoomTarget.anchoredPosition = zoomPosition;
        }

        ApplyObjectStates();
    }

    private void ApplySwapSprite()
    {
        if (swapTargetImage != null && swapSprite != null)
        {
            swapTargetImage.sprite = swapSprite;
        }
    }

    private void ApplyObjectStates()
    {
        if (activateObjects != null)
        {
            foreach (GameObject obj in activateObjects)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        if (deactivateObjects != null)
        {
            foreach (GameObject obj in deactivateObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (disableRaycastOnFinish != null)
        {
            foreach (Graphic graphic in disableRaycastOnFinish)
            {
                if (graphic != null) graphic.raycastTarget = false;
            }
        }
    }
}