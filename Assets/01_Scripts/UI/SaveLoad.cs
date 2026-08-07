using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SaveLoad
///
/// 담당:
/// - 저장/불러오기 패널의 UI를 관리
/// - SaveLoadType에 따라 패널을 저장 모드 또는 불러오기 모드로 초기화
/// - 저장 슬롯의 날짜, 챕터 텍스트, 썸네일 이미지, 버튼 상태를 설정
/// - 저장 시 현재 날짜를 PlayerPrefs에 저장
/// - 저장 슬롯에 연결된 캡처 이미지를 로컬 경로에서 불러와 슬롯 썸네일로 표시
///
/// 사용 위치:
/// - SaveLoad 패널 프리팹에 붙여 사용
/// - InGame에서 SaveLoad 프리팹을 생성한 뒤 Initalize()를 호출
///
/// 연결:
/// - InGame에서 저장/불러오기 모드로 패널을 생성
/// - PlayerPrefs를 통해 저장 날짜와 저장 이미지 경로를 읽고 씀
/// - 저장 이미지 파일은 Application.persistentDataPath/SaveImages 경로에서 불러옴
///
/// TODO:
/// - Initalize 오타를 Initialize로 수정 검토
/// - 저장 시 scenarioIndex, branchIndex 등 실제 게임 진행 데이터 저장 기능 연결 필요
/// - Load 모드에서 저장 데이터를 실제로 불러오는 기능 추가 필요
/// - 기존 저장 슬롯을 덮어쓸 수 있는지 정책 정리 필요
/// - 저장 슬롯에 챕터명/시나리오 정보를 표시하도록 Chapter 텍스트 저장 추가
/// - PlayerPrefs.Save() 호출 여부 검토
/// - 저장 이미지가 없을 때 기본 이미지/빈 슬롯 상태로 초기화하는 처리 추가
/// </summary>
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