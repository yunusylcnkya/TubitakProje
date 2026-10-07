using System.Collections;
using UnityEngine;

public sealed class BootController :
    MonoBehaviour
{
    private IEnumerator Start()
    {
        // AppRoot ve diğer kalıcı sistemlerin
        // Awake işlemlerinin tamamlanması için
        // bir frame bekliyoruz.
        yield return null;

        if (SceneLoader.Instance == null)
        {
            Debug.LogError(
                "BOOT | SceneLoader bulunamadı.",
                this
            );

            yield break;
        }

        SceneLoader.Instance.LoadScene(
            SceneNames.MainMenu
        );
    }
}