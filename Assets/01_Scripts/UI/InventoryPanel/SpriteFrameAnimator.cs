using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] Sprite[] frames;   //
    [SerializeField] float fps = 15f;   //속도조절

    Image image;
    Coroutine routine;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    // 나타날 때: 첫 프레임 → 마지막 프레임
    public void Play(Action onComplete = null) => Run(false, onComplete);

    // 사라질 때: 마지막 프레임 → 첫 프레임
    public void PlayReverse(Action onComplete = null) => Run(true, onComplete);

    void Run(bool reverse, Action onComplete)
    {
        if (frames == null || frames.Length == 0) { onComplete?.Invoke(); return; }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine(reverse, onComplete));
    }

    IEnumerator PlayRoutine(bool reverse, Action onComplete)
    {
        float interval = 1f / fps;

        for (int n = 0; n < frames.Length; n++)
        {
            int i = reverse ? frames.Length - 1 - n : n;
            image.sprite = frames[i];
            yield return new WaitForSecondsRealtime(interval);
        }

        routine = null;
        onComplete?.Invoke();
    }
}