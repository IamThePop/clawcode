using UnityEngine;
using UnityEditor; // This namespace only works inside the Unity Editor

public class EditorMenu : MonoBehaviour
{
    // This adds a menu item at the top of Unity called "Claw Code"
    [MenuItem("Claw Code/Open Save File Folder")]
    static void OpenFolderPath()
    {
        // This opens the file explorer directly to the persistentDataPath
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
    
    [MenuItem("Claw Code/Clear All Data")]
    static void ClearAllData()
    {
        // A helper to delete everything if you want to reset for testing
        string path = Application.persistentDataPath;
        string[] files = System.IO.Directory.GetFiles(path);
        
        foreach (string file in files)
        {
            System.IO.File.Delete(file);
        }
        Debug.Log("All database files have been deleted from: " + path);
    }
}