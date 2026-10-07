using UnityEngine;

public sealed class MenuCursorController :
    MonoBehaviour
{
    private void Start()
    {
        Time.timeScale = 1f;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;
    }
}