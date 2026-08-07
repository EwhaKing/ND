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
/// - Awake에서 InGame.Instance를 할당하는 Singleton 초기화 추가 필요
/// - SetNoneCaptureActive() 구현 필요
/// - 저장 시 시나리오/분기 데이터도 함께 저장하도록 SaveLoad와 연결 필요
/// - 캡처 후 생성된 Texture2D 메모리 해제 처리 검토
/// - GameFlowManager / ChapterManager가 생기면 scenarioIndex, branchIndex 관리 위치 재검토
/// </summary>
public class InGame : MonoBehaviour
{
    [Header("## UI")]
    [SerializeField] private Transform mainCanvas;

    public static InGame Instance = null;
    public int scenarioIndex = 0;
    public int branchIndex = 0;

    public string SaveBranch(){
        return $"{scenarioIndex}#{branchIndex}";
    }
    public void Onsave()
    {
        SetSaveLoadPanel(SaveLoadType.Save);
    }

    public void OnLoad(){
        SetSaveLoadPanel(SaveLoadType.Load);
    }

    [SerializeField] private GameObject saveLoadPrefab; 

    private void SetSaveLoadPanel(SaveLoadType type)
    {

    var go = Instantiate(saveLoadPrefab, mainCanvas); 
    var script = go.GetComponent<SaveLoad>();
    script.Initalize(type);
    }

    public void Capture(int index, Action action = null){
        StartCoroutine(CaptureUI(index, action));
    }

    IEnumerator CaptureUI(int index, Action action = null){
        //NoneCaptureSave.Clear();
        SetNoneCaptureActive(false);

        yield return new WaitForEndOfFrame();

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        SetNoneCaptureActive(true);
        SaveCapturedImage(index, tex);
        action?.Invoke();
    }

    void SaveCapturedImage(int index, Texture2D tex){
        byte[] png = tex.EncodeToPNG();
        string dir = Application.persistentDataPath + "/SaveImages";
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        string path = $"{dir}/save_{index}.png";
        System.IO.File.WriteAllBytes(path, png);

        PlayerPrefs.SetString($"#{index}_ImagePath",path);
    }

    void SetNoneCaptureActive(bool active)//캡처할때 UI안나오게 하는것
    {

    }

}
