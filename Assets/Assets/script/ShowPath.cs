using UnityEngine;

public class ShowPath : MonoBehaviour
{
    void Start()
    {
        string path = Application.persistentDataPath;
        Debug.Log("📂 ROOT FOLDER: " + path);
        Debug.Log("📄 User Data: " + path + "/UserData.json");
        Debug.Log("📄 Doll Data: " + path + "/DollData.json");
        Debug.Log("📄 Error Types: " + path + "/ErrorTypeData.json");
        Debug.Log("📄 Error Details: " + path + "/ErrorDetails.json");
    }
}