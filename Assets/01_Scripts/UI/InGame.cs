using System;                  
using System.Collections;      
using System.Collections.Generic; 
using UnityEngine;

/// <summary>
/// InGame
///
/// 담당:
/// - 인게임 씬에서 공통 UI 기능을 관리
/// - 현재 시나리오 번호와 분기 번호를 저장하고, 저장/불러오기 패널을 생성
/// - SaveLoad 패널을 Save 또는 Load 모드로 열 수 있도록 처리
/// - 저장 슬롯에 사용할 화면 캡처 이미지를 생성하고 로컬 경로를 PlayerPrefs에 저장
///
/// 사용 위치:
/// - 인게임 씬의 전체 UI를 관리하는 오브젝트에 부착
/// - 저장/불러오기 버튼에서 Onsave(), OnLoad()를 호출
///
/// 연결:
/// - SaveLoad 프리팹을 생성하고 SaveLoad.Initalize()를 호출
/// - SaveBranch()를 통해 현재 scenarioIndex와 branchIndex 정보를 저장용 문자열로 반환
/// - 저장 이미지 경로를 PlayerPrefs에 기록하여 SaveLoad에서 불러올 수 있도록 함
///
/// TODO:
/// - 저장 시 시나리오/분기 데이터도 함께 저장하도록 SaveLoad와 연결 필요
/// - GameFlowManager / ChapterManager가 생기면 scenarioIndex, branchIndex 관리 위치 재검토
/// </summary>
public class InGame : MonoBehaviour
{
    [Header("## UI")]
    [SerializeField] private Transform mainCanvas;
    [SerializeField] private GameObject[] noneCaptureUIS;

    List<GameObject> NoneCaptureSave = new();

    [SerializeField] private GameObject saveLoadPrefab; 


    public static InGame Instance = null;
    public int scenarioIndex = 0;
    public int branchIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string SaveBranch()
    {
        return $"{scenarioIndex}#{branchIndex}";
    }


    private void SetSaveLoadPanel(SaveLoadType type)
    {

    var go = Instantiate(saveLoadPrefab, mainCanvas); 
    var script = go.GetComponent<SaveLoad>();
    script.Initalize(type);
    }


    public void Capture(int index, Action action = null)
    {
        StartCoroutine(CaptureUI(index, action));
    }

    IEnumerator CaptureUI(int index, Action action = null)
    {
        SetNoneCaptureAlpha(0f);

        yield return new WaitForEndOfFrame();

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        SetNoneCaptureAlpha(1f);

        SaveCapturedImage(index, tex);
        Destroy(tex);

        action?.Invoke();
    }

    void SetNoneCaptureAlpha(float alpha)
    {
        int layer = LayerMask.NameToLayer("NoneCapture");
        
        Transform[] allTransforms = mainCanvas.GetComponentsInChildren<Transform>(true);

        foreach (Transform T in allTransforms)
        {
            if (T.gameObject.layer == layer)
            {
                CanvasGroup group = T.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = T.gameObject.AddComponent<CanvasGroup>();
                }

                group.alpha = alpha;                           
                group.blocksRaycasts = (alpha > 0f);           
                group.interactable = (alpha > 0f);             
            }
        }
    }



    void SaveCapturedImage(int index, Texture2D tex)
    {
        byte[] png = tex.EncodeToPNG();

        string dir = Application.persistentDataPath + "/SaveImages";
        if(!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        string path = $"{dir}/save_{index}.png";
        System.IO.File.WriteAllBytes(path, png);

        PlayerPrefs.SetString($"#{index}_ImagePath",path);
    }

    public void Onsave()
    {
        SetSaveLoadPanel(SaveLoadType.Save);
    }

    public void OnLoad(){
        SetSaveLoadPanel(SaveLoadType.Load);
    }


}
