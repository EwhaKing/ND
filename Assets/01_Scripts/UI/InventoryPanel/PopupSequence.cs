using System;
using System.Collections;
using UnityEngine;

// ClueDetailPopup의 popupPanel 오브젝트에 붙임
public class PopupSequence : MonoBehaviour
{
    [Header("배경 어둡게")]
    [SerializeField] CanvasGroup dim;
    [SerializeField, Range(0f, 1f)] float dimAlpha = 0.45f;
    [SerializeField] float dimFadeTime = 0.15f;

    [Header("창 프레임 (SpriteFrameAnimator)")]
    [SerializeField] SpriteFrameAnimator frame;

    [Header("내용 (아이콘, 이름, 설명, X 버튼)")]
    [SerializeField] CanvasGroup content;
    [SerializeField] float contentFadeTime = 0.25f;

    Coroutine routine;

    private void OnDisable()
    {
        Debug.Log("[PopupSequence] 꺼짐\n" + StackTraceUtility.ExtractStackTrace());
    }

    // ShowPopup()에서 아이콘/이름/설명을 채운 뒤 호출. 연출이 모두 끝나면 onOpened 실행
    public void Open(Action onOpened = null)
    {
        gameObject.SetActive(true);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(OpenRoutine(onOpened));
    }

    // 바로 사라짐 (ClueDetailPopup.ClosePopup()이 popupPanel을 끔)
    public void Close()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        gameObject.SetActive(false);
    }

    IEnumerator OpenRoutine(Action onOpened)
    {
        content.alpha = 0f;
        content.blocksRaycasts = false;   // 내용이 나타나기 전에는 클릭 막기
        dim.alpha = 0f;

        StartCoroutine(Fade(dim, 0f, dimAlpha, dimFadeTime));

        bool frameDone = false;
        frame.Play(() => frameDone = true);
        yield return new WaitUntil(() => frameDone);

        yield return Fade(content, 0f, 1f, contentFadeTime);
        content.blocksRaycasts = true;
        routine = null;

        onOpened?.Invoke();
    }

    IEnumerator Fade(CanvasGroup group, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        group.alpha = to;
    }
}