using UnityEngine;

public class LevelJsonTestRunner : MonoBehaviour
{
    [SerializeField] private LevelJsonLoader loader;
    [SerializeField] private TextAsset levelJson;

    void Start()
    {
        loader = FindObjectOfType<LevelJsonLoader>();

        if (loader == null)
        {
            Debug.LogError("[LevelJsonTestRunner] No LevelJsonLoader found in scene");
            return;
        }

        if (levelJson == null)
        {
            Debug.LogError("[LevelJsonTestRunner] No JSON TextAsset assigned");
            return;
        }

        loader.LoadLevelFromJson(levelJson.text);
    }
}
