using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 테스트용 LoadScene 스크립트
/// </summary>

public class MiniGameNext : MonoBehaviour
{
    public void LoadScene()
    {
        SceneManager.LoadScene("ChatScene");
    }   
}
