using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SaveLoadType
{
    Save,
    Load
}

public class SaveData
{
    public TMP_Text Date;
    public TMP_Text Chapter;
    public GameObject PlusImage;
    public Image SaveMainImage;
    public Button MainButton;
}

public class SaveLoad : MonoBehaviour
{
    SaveLoadType m_Type;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform gridParent;
    List<SaveData> m_data = new();

    private void Awake()
    {
        SetParameter();
    }

    private void SetParameter()
    {
        m_data.Clear();
        for (int i = 0; i < gridParent.childCount; i++)
        {
            SaveData data = new SaveData();
            var child = gridParent.GetChild(i);
            data.Date = child.Find("Date").GetComponent<TMP_Text>();
            data.Chapter = child.Find("Chapter").GetComponent<TMP_Text>();
            data.PlusImage = child.Find("Plus").gameObject; 
            data.SaveMainImage = child.Find("MainImage").GetComponent<Image>();
            data.MainButton = child.GetComponent<Button>();

            m_data.Add(data);
        }
    }

    public void Initalize(SaveLoadType type)
    {
        m_Type = type;
        titleText.text = type == SaveLoadType.Save ? "저장하기" : "불러오기";

        if (type == SaveLoadType.Save)
        {
            for (int i = 0; i < m_data.Count; i++)
            {

                m_data[i].MainButton.onClick.RemoveAllListeners();


                if (string.IsNullOrEmpty(PlayerPrefs.GetString($"#{i}_Date", "")))
                {
                    m_data[i].PlusImage.SetActive(true);
                    int index = i;
                    

                    m_data[index].MainButton.onClick.AddListener(() => Save(index));
                }
                else
                {
                    m_data[i].PlusImage.SetActive(false);
                    m_data[i].Date.text = PlayerPrefs.GetString($"#{i}_Date");

                }
            }
        }
    }

    public void Save(int value)
    {
        string date = System.DateTime.Now.ToString("yyyy.MM.dd HH:mm");

        PlayerPrefs.SetString($"#{value}_Date", date);

        Initalize(SaveLoadType.Save);
    }
}