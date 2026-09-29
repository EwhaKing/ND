using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Image 사용을 위해 추가

public class AnimationController : MonoBehaviour
{
    private Animator animator;
    private Image targetImage;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        targetImage = GetComponent<Image>();
    }

    /// <summary>
    /// 지정된 애니메이션 클립을 재생하고, 완료 시 콜백을 호출합니다.
    /// </summary>
    public void PlayClip(AnimationClip clip, Action onComplete)
    {
        if (clip == null)
        {
            Debug.LogError("재생할 AnimationClip이 없습니다.");
            onComplete?.Invoke();
            return;
        }

        // 1. 게임 오브젝트 및 Image 컴포넌트 활성화 보장
        gameObject.SetActive(true);
        if (targetImage != null)
        {
            targetImage.enabled = true;
        }

        // 2. Animator 상태 재생
        if (animator != null)
        {
            animator.Play(clip.name, 0, 0f);
        }

        // 3. 클립의 재생 시간 만큼 대기 후 완료 콜백 호출
        StartCoroutine(WaitRoutine(clip.length, onComplete));
    }

    private IEnumerator WaitRoutine(float duration, Action onComplete)
    {
        yield return new WaitForSecondsRealtime(duration);

        // 재생이 끝난 후 필요에 따라 비활성화 처리
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}