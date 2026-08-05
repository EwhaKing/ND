using System;                  
using System.Collections;      
using System.Collections.Generic; 
using UnityEngine;

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
