using UnityEngine;
using UnityEngine.SceneManagement;

public class Start : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    [Tooltip("Nama scene tujuan yang akan dibuka")]
    [SerializeField] private string targetSceneName;

    /// <summary>
    /// Berpindah ke scene yang sudah ditentukan di Inspector (targetSceneName).
    /// </summary>
    public void ChangeScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("Target Scene Name belum diisi di Inspector!");
        }
    }

    /// <summary>
    /// Berpindah ke scene sesuai nama yang diisi langsung pada OnClick event button.
    /// </summary>
    public void ChangeSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Nama scene kosong!");
        }
    }

    /// <summary>
    /// Keluar dari game (berfungsi saat sudah di-build, dan menghentikan Play Mode jika di dalam Unity Editor).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Keluar dari game...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
