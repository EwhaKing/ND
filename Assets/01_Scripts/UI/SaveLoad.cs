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




    public void ClosePanel()
    {
        Destroy(gameObject);
    }

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
                    m_data[i].Date.gameObject.SetActive(false);
                    m_data[i].Chapter.gameObject.SetActive(false);
                    int index = i;
                    m_data[index].MainButton.onClick.AddListener(() => Save(index));
                }
                else
                {
                    m_data[i].PlusImage.SetActive(false);
                    LoadImages(i);
                }
            }
        }
        else if(type==SaveLoadType.Load)
        {
            for (int i = 0; i < m_data.Count; i++)
            {
                m_data[i].MainButton.onClick.RemoveAllListeners();

                m_data[i].PlusImage.SetActive(false);
                m_data[i].Date.gameObject.SetActive(false);
                m_data[i].Chapter.gameObject.SetActive(false);

                if (!string.IsNullOrEmpty(PlayerPrefs.GetString($"#{i}_Date", "")))
                {
                    m_data[i].MainButton.interactable=true;
                    LoadImages(i);
                }
                else
                {
                    m_data[i].MainButton.interactable=false;
                }
            }
        }
    }

    void LoadImages(int index)
    {
        m_data[index].Date.gameObject.SetActive(true);
        m_data[index].Chapter.gameObject.SetActive(true);
        m_data[index].Date.text = PlayerPrefs.GetString($"#{index}_Date");
        LoadSaveImage(index);
    }

    private void LoadSaveImage(int index)
    {
        string path = PlayerPrefs.GetString($"#{index}_ImagePath", "");

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }///수정해야할 부분

        byte[] bytes = System.IO.File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        tex.LoadImage(bytes);

        Sprite sprite = Sprite.Create(
            tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        m_data[index].SaveMainImage.gameObject.SetActive(true);
        m_data[index].SaveMainImage.sprite=sprite;

    }



    public void Save(int value)
    {
        string date = System.DateTime.Now.ToString("yyyy.MM.dd HH:mm");

        PlayerPrefs.SetString($"#{value}_Date", date);
        
        //PlayerPrefs.SetString($"#{value}_Scenario",InGame.Instance.SaveBranch());
        //InGame.Instance.Capture(value, ()=> Initalize(SaveLoadType.Save));
        Initalize(SaveLoadType.Save);
    }

}