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

                // 기존 원본 키 규칙($"#{i}_Date") 유지
                if (string.IsNullOrEmpty(PlayerPrefs.GetString($"#{i}_Date", "")))
                {
                    m_data[i].PlusImage.SetActive(true);
                    m_data[i].Date.gameObject.SetActive(false);
                    m_data[i].Chapter.gameObject.SetActive(false);
                }
                else
                {
                    m_data[i].PlusImage.SetActive(false);
                    
                    // [기존 원본 로직 복원] 날짜 표시 및 이미지 로드
                    m_data[i].Date.gameObject.SetActive(true);
                    m_data[i].Date.text = PlayerPrefs.GetString($"#{i}_Date", "");

                    // [신규 추가] 챕터 텍스트 표시
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

                // 기존 원본 키 규칙($"#{i}_Date") 유지
                if (!string.IsNullOrEmpty(PlayerPrefs.GetString($"#{i}_Date", "")))
                {
                    m_data[i].MainButton.interactable = true;
                    m_data[i].MainButton.onClick.AddListener(() => Load(index));

                    // [기존 원본 로직 복원] 날짜 표시
                    m_data[i].Date.gameObject.SetActive(true);
                    m_data[i].Date.text = PlayerPrefs.GetString($"#{i}_Date", "");

                    // [신규 추가] 챕터 텍스트 표시
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

        byte[] bytes = File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        tex.LoadImage(bytes);

        // 이전 스프라이트/텍스처가 있다면 메모리 누수 방지를 위해 덮어쓰기 전 할당 관리 검토가 권장됩니다.
        Sprite sprite = Sprite.Create(
            tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

        m_data[index].SaveMainImage.gameObject.SetActive(true);
        m_data[index].SaveMainImage.sprite = sprite;
    }

    public void Save(int value)
    {
        string date = DateTime.Now.ToString("yyyy.MM.dd HH:mm");
        PlayerPrefs.SetString($"#{value}_Date", date);

        string chapterName = "프롤로그"; // 기본값
        ScenarioRunner runner = UnityEngine.Object.FindAnyObjectByType<ScenarioRunner>();
        if (runner != null && runner.CurrentScenarioData != null)
        {
            // ScenarioData에 적힌 ID 또는 이름 활용 (예: "챕터 1", "C1P" 등)
            chapterName = runner.CurrentScenarioData.scenarioId; 
        }

        // [추가] PlayerPrefs에 챕터 텍스트 저장
        string formattedChapter = GetFormattedChapterName(chapterName);
        PlayerPrefs.SetString($"#{value}_Chapter", formattedChapter);

        //캡쳐 및 UI 갱신
        if (InGame.Instance != null)
        {
            PlayerPrefs.SetString($"#{value}_Scenario", InGame.Instance.SaveBranch());
            InGame.Instance.Capture(value, () => Initialize(SaveLoadType.Save));
        }
        else
        {
            Debug.LogWarning("InGame.Instance가 null 상태입니다. UI만 갱신합니다.");
            Initialize(SaveLoadType.Save);
        }

        // 1. GameProgressManager로부터 현재 게임 세이브 데이터 생성
        if (GameProgressManager.Instance != null)
        {
            SaveGameData gameData = GameProgressManager.Instance.CreateSaveData();
            
            // 2. JSON 문자열로 변환
            string jsonText = JsonUtility.ToJson(gameData);
            
            // 3. PlayerPrefs에 저장 (슬롯 인덱스 활용, 예: "SaveData_Slot_0")
            PlayerPrefs.SetString("SaveData_Slot_" + value, jsonText);
            PlayerPrefs.Save();
            
            Debug.Log($"{value}번 슬롯에 게임 진행 데이터(JSON) 저장 완료!");
        }
    }
    public void Load(int value)
    {
        // 슬롯 키 이름
        string key = "SaveData_Slot_" + value;

        if (!PlayerPrefs.HasKey(key))
        {
            Debug.LogWarning($"{value}번 슬롯에 저장된 데이터가 없습니다.");
            return;
        }

        // 1. JSON 불러오기
        string jsonText = PlayerPrefs.GetString(key);
        SaveGameData loadedData = JsonUtility.FromJson<SaveGameData>(jsonText);

        // 2. 게임 상태 복원 (GameProgressManager가 내부에서 ScenarioRunner 대화 위치까지 복원함)
        if (loadedData != null && GameProgressManager.Instance != null)
        {
            
            // 현재 씬 이름 확인
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // [추가] 로비 씬에서 로드했을 경우 -> ChatScene으로 이동 후 데이터 적용
            if (currentSceneName == "LobbyScene")
            {
                // 실제 게임 플레이 씬 이름(예: "ChatScene")을 정확히 적어주세요.
                GameProgressManager.Instance.LoadGameAndChangeScene(loadedData, "ChatScene");
            }
            // [기존 코드 그대로] 게임 씬(ChatScene) 내부에서 로드했을 경우 -> 즉시 복원
            else
            {
                GameProgressManager.Instance.ApplySaveData(loadedData);
                Debug.Log($"{value}번 슬롯 데이터 불러오기 성공!");
            }
        }

        // 3. 세이브/로드 UI 팝업 창 닫기 (창이 꺼지면서 복원된 대화가 바로 보여짐)
        this.gameObject.SetActive(false);
    }

    private string GetFormattedChapterName(string scenarioId)
    {
        if (string.IsNullOrEmpty(scenarioId)) return "프롤로그";

        if (scenarioId.Contains("C1")) return "챕터 1";
        if (scenarioId.Contains("C2")) return "챕터 2";
        if (scenarioId.Contains("P")) return "프롤로그";

        return scenarioId; // 매칭되는 게 없으면 기본 값 반환
    }
    
}