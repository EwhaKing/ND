using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    private Image fadeImage;
    [SerializeField] private float Duration = 0.5f;
    private Coroutine coroutine;

    private void Awake()
    {
        fadeImage = GetComponentInChildren<Image>(true);
    }

    // 화면이 걷히는 효과 (투명해짐: 1 -> 0)
    public void FadeIn(float duration = -1f, float completeDelay=0f, Action onComplete = null)
    {
        StartFade(1.0f, 0.0f, duration, completeDelay, onComplete);
    }

    // 화면이 어두워지는 효과 (불투명해짐: 0 -> 1)
    public void FadeOut(float duration = -1f, float completeDelay=0f, Action onComplete = null)
    {
        StartFade(0f, 1f, duration, completeDelay, onComplete);
    }


    private void StartFade(float from, float to, float duration,float completeDelay, Action onComplete)
    {
        if(fadeImage.gameObject.activeSelf==false)
            fadeImage.gameObject.SetActive(true);
        if (coroutine != null)
            StopCoroutine(coroutine);


        coroutine = StartCoroutine(FadeRoutine(from, to, duration < 0 ? Duration : duration, completeDelay, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration,float completeDelay, Action onComplete)
    {
        float time = 0f;
        SetAlpha(from);

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, time / duration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(to);
        if(completeDelay>0f)
            yield return new WaitForSeconds(completeDelay);
        coroutine = null;
        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
}