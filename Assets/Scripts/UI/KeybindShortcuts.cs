using UnityEngine;
using UnityEngine.UI;

public class KeybindShortcuts : MonoBehaviour
{
    public enum GameAction
    {
        ResetCamera,
        Undo,
        Redo,
        Pause,
        Restart,
        Quit
    }

    [System.Serializable]
    public class Keybind
    {
        public KeyCode key;
        public GameAction action;
    }

    [SerializeField] private Keybind[] keybinds;
    [SerializeField] private PauseMenu pauseMenu;

    void Update()
    {
        foreach (var bind in keybinds)
        {
            if (Input.GetKeyDown(bind.key))
            {
                Execute(bind.action);
            }
        }
    }

    void Execute(GameAction action)
    {
        switch (action)
        {
            case GameAction.ResetCamera:
                Debug.Log("Reset Camera");
                break;
            case GameAction.Undo:
                Debug.Log("Undo");
                break;
            case GameAction.Redo:
                Debug.Log("Redo");
                break;
            case GameAction.Pause:
                pauseMenu.TogglePause();
                break;
            case GameAction.Restart:
                pauseMenu.Restart();
                break;
            case GameAction.Quit:
                pauseMenu.QuitGame();
                break;
        }
    }
}
