using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // ผูกปุ่มใน Inspector แล้วเลือกเมธอดนี้
    public void LoadSceneByName(string sceneName)
    {
        // --- ส่วนที่เพิ่ม: เริ่ม Session ใหม่ ---
        if (DataManager.Instance != null)
        {
            // ส่งชื่อ "Random" ไปก่อน เพราะเดี๋ยวเกมจะสุ่มตุ๊กตาข้างในอีกที
            DataManager.Instance.StartGameSession("Random Mission");
        }
        // ----------------------------------

        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByIndex(int index)
    {
        // --- ส่วนที่เพิ่ม: เผื่อเพื่อนใช้แบบ Index ---
        if (DataManager.Instance != null)
        {
            DataManager.Instance.StartGameSession("Random Mission");
        }
        // ----------------------------------
        
        SceneManager.LoadScene(index);
    }

    public void ReloadCurrent()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}