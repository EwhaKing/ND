using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StandingController : MonoBehaviour
{
    //[SerializeField] private GameObject dim;
    [SerializeField] private Image[] images;

    private readonly List<StandStep> currentStands = new List<StandStep>();

    public void SetSprite(StandStep[] stands)
    {
        //dim.SetActive(true);
        currentStands.Clear();

        for (int i = 0; i < images.Length; i++)
        {
            bool hasStand = i < stands.Length && stands[i].sprite != null;

            images[i].gameObject.SetActive(hasStand);

            if (!hasStand)
            {
                continue;
            }

            currentStands.Add(stands[i]);

            images[i].sprite = stands[i].sprite;
            images[i].rectTransform.sizeDelta = new Vector2(650f, 1100f);
            images[i].color = Color.white;

            if (stands.Length > 1)
            {
                images[i].rectTransform.localPosition =
                    i == 0 ? new Vector2(-450f, -50f) : new Vector2(450f, -50f);
            }
            else
            {
                images[i].rectTransform.localPosition = Vector2.zero;
            }
        }
    }

    public void SetColor(string speaker)
    {
        int count = Mathf.Min(
            currentStands.Count,
            images.Length
        );

        if (count == 0)
        {
            return;
        }

        bool isNarration =
            string.IsNullOrWhiteSpace(speaker);

        for (int i = 0; i < count; i++)
        {
            if (images[i] == null ||
                !images[i].gameObject.activeSelf)
            {
                continue;
            }

            if (isNarration)
            {
                images[i].color = Color.gray;
                continue;
            }

            bool isCurrentSpeaker =
                currentStands[i].standName == speaker;

            images[i].color =
                isCurrentSpeaker
                    ? Color.white
                    : Color.gray;
        }
    }

    public void Hide()
    {
        //dim.SetActive(false);

        foreach (Image image in images)
        {
            image.gameObject.SetActive(false);
            image.color = Color.white;
        }

        currentStands.Clear();
    }
}