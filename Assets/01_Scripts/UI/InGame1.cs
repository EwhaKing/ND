using UnityEngine;

public class InGame1 : MonoBehaviour
{
    [Header("## UI")]
    [SerializeField] private Transform mainCanvas;

    // Update is called once per frame
    void Update()
    {
        
    }



    public void Onsave()
    {
        SetSaveLoadPanel(SaveLoadType.Save);
    }

    public void OnLoad(){
        SetSaveLoadPanel(SaveLoadType.Load);
    }

    private void SetSaveLoadPanel(SaveLoadType type){
        var go = Instantiate(Resources.Load<GameObject>("UI/Save&Load"), mainCanvas);
        var script = go.GetComponent<SaveLoad>();
        script.Initalize(type);
    }


}
