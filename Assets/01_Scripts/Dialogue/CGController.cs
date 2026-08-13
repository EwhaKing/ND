using System;
using UnityEngine;
using UnityEngine.UI;

public class CGController : MonoBehaviour
{
    [SerializeField] private Image cgImage;

    private Action onClicked;

    private void Awake()
    {
        Hide();
    }

    public void Show(
        Sprite sprite,
        Action clickCallback)
    {
        if (sprite == null)
        {
            Debug.LogError(
                "not found CG Sprite."
            );

            return;
        }

        cgImage.sprite = sprite;
        cgImage.gameObject.SetActive(true);

        onClicked = clickCallback;
    }

    public void OnClick()
    {
        if (onClicked == null)
        {
            return;
        }

        Action callback = onClicked;
        onClicked = null;

        callback.Invoke();
    }

    public void Hide()
    {
        onClicked = null;

        if (cgImage == null)
        {
            return;
        }

        cgImage.sprite = null;
        cgImage.gameObject.SetActive(false);
    }
}