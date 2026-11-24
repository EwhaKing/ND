using System;
using System.Collections;
using UnityEngine;

// 단서 팝업 연출 순서 (GIF 기준)
// 1) 배경이 어두워짐  2) 프레임이 글리치처럼 나타남  3) 내용(아이콘/이름/설명/X)이 fade in
// ClueDetailPopup의 popupPanel 오브젝트에 붙이세요.
public class PopupSequence : MonoBehaviour
{
    [Header("배경 어둡게 (검정 Image + CanvasGroup)")]
    [SerializeField] CanvasGroup dim;
    [SerializeField, Range(0f, 1f)] float dimAlpha = 0.45f;
    [SerializeField] float dimFadeTime = 0.15f;

    [Header("창 프레임 (SpriteFrameAnimator)")]
    [SerializeField] SpriteFrameAnimator frame;

    [Header("내용 (아이콘, 이름, 설명, X 버튼을 묶은 CanvasGroup)")]
    [SerializeField] CanvasGroup content;
    [SerializeField] float contentFadeTime = 0.25f;

    Coroutine routine;

    // ShowPopup()에서 아이콘/이름/설명을 채운 뒤 호출. 연출이 모두 끝나면 onOpened 실행
    public void Open(Action onOpened = null)
    {
        gameObject.SetActive(true);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(OpenRoutine(onOpened));
    }

    // 바로 사라짐 (ClueDetailPopup.ClosePopup()이 popupPanel을 끄므로 보통은 직접 부를 일 없음)
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