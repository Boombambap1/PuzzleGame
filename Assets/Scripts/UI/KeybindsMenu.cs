using UnityEngine;
using UnityEngine.UI;

public class KeybindsMenu : MonoBehaviour
{
    [System.Serializable]
    public class KeyUI
    {
        public KeyCode key;
        public Image image;
    }

    [SerializeField] private KeyUI[] keys;

    [SerializeField] private Color normalColor = new Color32(202, 202, 202, 255);
    [SerializeField] private Color pressedColor = new Color32(135, 135, 135, 255);

    void Update()
    {
        foreach (var keyUI in keys)
        {
            keyUI.image.color = Input.GetKey(keyUI.key) ? pressedColor : normalColor;
        }
    }
}
