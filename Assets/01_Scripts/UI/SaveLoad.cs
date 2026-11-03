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
/// - InGame에서 SaveLoad 프리팹을 생성한 뒤 Initialize()를 호출
///
/// 연결:
/// - InGame에서 저장/불러오기 모드로 패널을 생성
/// - PlayerPrefs를 통해 저장 날짜와 저장 이미지 경로를 읽고 씀
/// - 저장 이미지 파일은 Application.persistentDataPath/SaveImages 경로에서 불러옴
/// </summary>
public enum SaveLoadType
{
    Save,
    Load
}

public class SaveSlotUI
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

    // SaveData -> SaveSlotUI로 수정되었습니다.
    List<SaveSlotUI> m_data = new();

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
            SaveSlotUI data = new SaveSlotUI();
            var child = gridParent.GetChild(i);
            data.Date = child.Find("Date").GetComponent<TMP_Text>();
            data.Chapter = child.Find("Chapter").GetComponent<TMP_Text>();
            data.PlusImage = child.Find("Plus").gameObject; 
            data.SaveMainImage = child.Find("MainImage").GetComponent<Image>();
            data.MainButton = child.GetComponent<Button>();

            m_data.Add(data);
        }
    }

    public void Initialize(SaveLoadType type)
    {
        m_Type = type;
        titleText.text = type == SaveLoadType.Save ? "저장하기" : "불러오기";

        if (type == SaveLoadType.Save)
        {
            for (int i = 0; i < m_data.Count; i++)
            {
                int index = i;
                m_data[i].MainButton.onClick.RemoveAllListeners();
                m_data[i].MainButton.onClick.AddListener(() => Save(index));


                if (string.IsNullOrEmpty(PlayerPrefs.GetString($"#{i}_Date", "")))
                {
                    m_data[i].PlusImage.SetActive(true);
                    m_data[i].Date.gameObject.SetActive(false);
                    m_data[i].Chapter.gameObject.SetActive(false);
                }
                else
                {
                    m_data[i].PlusImage.SetActive(false);
                    
                    //  날짜 표시 및 이미지 로드
                    m_data[i].Date.gameObject.SetActive(true);
                    m_data[i].Date.text = PlayerPrefs.GetString($"#{i}_Date", "");

                    // 챕터 텍스트 표시
                    if (m_data[i].Chapter != null)
                    {
                        m_data[i].Chapter.gameObject.SetActive(true);
                        m_data[i].Chapter.text = PlayerPrefs.GetString($"#{i}_Chapter", "프롤로그");
                    }

                    LoadImages(i);
                }
            }
        }
        else if (type == SaveLoadType.Load)
        {
            for (int i = 0; i < m_data.Count; i++)
            {
                int index = i;
                m_data[i].MainButton.onClick.RemoveAllListeners();

                m_data[i].PlusImage.SetActive(false);

                if (!string.IsNullOrEmpty(PlayerPrefs.GetString($"#{i}_Date", "")))
                {
                    m_data[i].MainButton.interactable = true;
                    m_data[i].MainButton.onClick.AddListener(() => Load(index));

                    //날짜 표시
                    m_data[i].Date.gameObject.SetActive(true);
                    m_data[i].Date.text = PlayerPrefs.GetString($"#{i}_Date", "");

                    //챕터 텍스트 표시
                    if (m_data[i].Chapter != null)
                    {
                        m_data[i].Chapter.gameObject.SetActive(true);
                        m_data[i].Chapter.text = PlayerPrefs.GetString($"#{i}_Chapter", "프롤로그");
                    }

                    LoadImages(i);
                }
                else
                {
                    m_data[i].Date.gameObject.SetActive(false);
                    if (m_data[i].Chapter != null) m_data[i].Chapter.gameObject.SetActive(false);
                    m_data[i].MainButton.interactable = false;
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

    void LoadSaveImage(int index)
    {
        string path = PlayerPrefs.GetString($"#{index}_ImagePath", "");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            m_data[index].SaveMainImage.gameObject.SetActive(false);
            return;
        }

        // 이전에 만든 썸네일 텍스처 정리 (메모리 누수 방지)
        Sprite oldSprite = m_data[index].SaveMainImage.sprite;
        if (oldSprite != null && oldSprite.name == "SaveThumbnail") // 프리팹 기본 스프라이트(에셋)는 건드리지 않음
        {
            Destroy(m_data[index].SaveMainImage.sprite.texture);
            Destroy(m_data[index].SaveMainImage.sprite);
        }

        byte[] bytes = File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        tex.LoadImage(bytes);

        Sprite sprite = Sprite.Create(
            tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

        m_data[index].SaveMainImage.gameObject.SetActive(true);
        sprite.name = "SaveThumbnail";
        m_data[index].SaveMainImage.sprite = sprite;
    }

    public void Save(int value)
    {
        string date = DateTime.Now.ToString("yyyy.MM.dd HH:mm");
        PlayerPrefs.SetString($"#{value}_Date", date);

        // 챕터 문구는 현재 진행 단계(currentStep)로 결정한다.
        // (조사 씬에는 ScenarioRunner 가 없어서 시나리오 이름을 알 수 없다)
        string chapterLabel = GameProgressManager.Instance != null
            ? GameProgressManager.Instance.GetChapterLabel()
            : "프롤로그";
        PlayerPrefs.SetString($"#{value}_Chapter", chapterLabel);

        // 게임 진행 데이터(JSON) 저장 - 화면 캡처보다 먼저 확정해서 저장한다.
        if (GameProgressManager.Instance != null)
        {
            SaveGameData gameData = GameProgressManager.Instance.CreateSaveData();
            PlayerPrefs.SetString("SaveData_Slot_" + value, JsonUtility.ToJson(gameData));
        }
        else
        {
            Debug.LogWarning("GameProgressManager.Instance가 null 입니다. 진행 데이터는 저장되지 않습니다.");
        }

        PlayerPrefs.Save();

        // 캡쳐 및 UI 갱신
        if (InGame.Instance != null)
        {
            InGame.Instance.Capture(value, () => Initialize(SaveLoadType.Save));
        }
        else
        {
            Debug.LogWarning("InGame.Instance가 null 상태입니다. UI만 갱신합니다.");
            Initialize(SaveLoadType.Save);
        }
    }

    public void Load(int value)
    {
        string key = "SaveData_Slot_" + value;

        if (!PlayerPrefs.HasKey(key))
        {
            Debug.LogWarning($"{value}번 슬롯에 저장된 데이터가 없습니다.");
            return;
        }

        if (GameProgressManager.Instance == null)
        {
            Debug.LogError("GameProgressManager.Instance가 null 입니다. 불러올 수 없습니다.");
            return;
        }

        SaveGameData loadedData = JsonUtility.FromJson<SaveGameData>(PlayerPrefs.GetString(key));
        if (loadedData == null)
        {
            Debug.LogWarning($"{value}번 슬롯 데이터를 읽지 못했습니다.");
            return;
        }

        // 이동할 씬 결정 (저장된 씬 이름이 없으면 진행 단계로 추론)
        string destinationScene = loadedData.targetSceneName;
        if (string.IsNullOrEmpty(destinationScene))
        {
            destinationScene = GameProgressManager.Instance.GetTargetSceneByStep(loadedData.currentProgressStep);
        }

        // 같은 씬이어도 항상 씬을 다시 로드한다.
        // (FindScene 옥상 -> 땅처럼 씬 이름은 같고 진행 단계만 다른 경우,
        //  씬을 다시 시작해야 조사 컨트롤러가 새 단계를 읽는다)
        GameProgressManager.Instance.LoadGameAndChangeScene(loadedData, destinationScene);

        Destroy(gameObject);
    }
}