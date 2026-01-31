using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DataManager : MonoBehaviour
{
    // --- CONNECT TO API ---
    public APIManager apiManager; // Drag APIManager here in Inspector

    [System.Serializable]
    public class ErrorListWrapper
    {
    public List<ErrorDetail> errorDetails;
    }
    // --- USER DATA ---
    [System.Serializable]
    public class UserDataList
    {
        public List<UserData> users = new List<UserData>();
    }
    public UserDataList userList = new UserDataList();
    private string filePath;

    // --- DOLL DATA ---
    [System.Serializable]
    public class DollDataList
    {
        public List<DollData> dolls = new List<DollData>();
    }
    public DollDataList dollList = new DollDataList();
    private string dollFilePath;

    // --- ERROR TYPE DATA ---
    [System.Serializable]
    public class ErrorTypeList
    {
        public List<ErrorTypeData> errorTypes = new List<ErrorTypeData>();
    }
    public ErrorTypeList errorTypeList = new ErrorTypeList();
    private string errorTypeFilePath;

    // --- ERROR DETAILS DATA ---
    [System.Serializable]
    public class ErrorDetailList
    {
        public List<ErrorDetail> errorDetails = new List<ErrorDetail>();
    }
    public ErrorDetailList errorDetailList = new ErrorDetailList();
    private string errorDetailFilePath;
    
    public static DataManager Instance;
    public List<ErrorDetail> errorDetails;
    private void Awake()
    {
        // 1. Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
            return;
        }

        // 2. Define Paths
        filePath = Application.persistentDataPath + "/UserData.json";
        dollFilePath = Application.persistentDataPath + "/DollData.json";
        errorTypeFilePath = Application.persistentDataPath + "/ErrorTypeData.json";
        errorDetailFilePath = Application.persistentDataPath + "/ErrorDetails.json";

        // 3. Load/Create Error Types
        if (!File.Exists(errorTypeFilePath)) CreateDefaultErrorTypes();
        else LoadErrorTypes();

        // 4. Load/Create Dolls
        if (!File.Exists(dollFilePath)) CreateDefaultDollData();
        else LoadDollData();

        // 5. Load Error Details
        if (File.Exists(errorDetailFilePath))
        {
            string json = File.ReadAllText(errorDetailFilePath);
            errorDetailList = JsonUtility.FromJson<ErrorDetailList>(json);
        }
        else
        {
            errorDetailList = new ErrorDetailList();
        }

        // 6. Load User Data
        LoadUserData(); 
    }

    public int currentLogID; 

    // Function called by SceneLoader
    public void StartGameSession(string selectedDoll)
    {
        int newLogID = 1;
        if (userList.users.Count > 0)
        {
            newLogID = userList.users[userList.users.Count - 1].log_id + 1;
        }
        currentLogID = newLogID;
        Debug.Log($"<color=yellow>Session Started! Assigned Log ID: {currentLogID}</color>");
    }

    // --- MAIN SAVE FUNCTION ---
    public void AddNewUserLog(string dollName, int commands, int errors, int time, bool isSuccess, List<ErrorDetail> sessionErrors)
    {
        // 1. Prepare User Data
        UserData newUser = new UserData();
        
        // Use the ID we calculated at Start, or calculate fresh if needed
        int newLogID = currentLogID; 
        if(newLogID == 0) // Safety check
        {
             if (userList.users.Count > 0) newLogID = userList.users[userList.users.Count - 1].log_id + 1;
             else newLogID = 1;
        }

        newUser.log_id = newLogID;
        newUser.target_doll = dollName;
        newUser.command_count = commands;
        newUser.error_count = errors;
        newUser.play_time = time;
        newUser.result = isSuccess;
        newUser.created_at = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 2. Save to Local List
        userList.users.Add(newUser);
        SaveUserData();
        
        // 3. Process Error Details
        foreach (ErrorDetail err in sessionErrors)
        {
            int newDetailID = 1;
            if (errorDetailList.errorDetails.Count > 0)
            {
                newDetailID = errorDetailList.errorDetails[errorDetailList.errorDetails.Count - 1].detail_id + 1;
            }

            err.detail_id = newDetailID;
            err.log_id = newLogID; 

            errorDetailList.errorDetails.Add(err);
            errorDetails.Add(err);
        }
        SaveErrorDetails();

        Debug.Log($"<color=green>Saved User ID: {newLogID} to Local File.</color>");

        // 4. SEND TO LARAVEL (API)
        if (apiManager != null)
        {
            // 1. Create the wrapper instance
ErrorListWrapper wrapper = new ErrorListWrapper();

// 2. Assign your list to the wrapper's field
wrapper.errorDetails = this.errorDetails;

// 3. Serialize the wrapper
string jsonerrorToSend = JsonUtility.ToJson(wrapper);

Debug.Log(jsonerrorToSend); 
// Now it will show: {"errorDetails":[{"detail_id":1,...}, ...]}
            
            string jsonToSend = JsonUtility.ToJson(newUser);
            apiManager.UploadUserData(jsonToSend,jsonerrorToSend);
        }
        else
        {
            Debug.LogWarning("APIManager is missing! Data saved locally but NOT sent to server.");
        }
    }

    private void SaveUserData()
    {
        string json = JsonUtility.ToJson(userList, true);
        File.WriteAllText(filePath, json);
    }

    public void LoadUserData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            userList = JsonUtility.FromJson<UserDataList>(json);
        }
        else
        {
            userList = new UserDataList();
        }
    }

    // --- DOLL FUNCTIONS ---
    private void CreateDefaultDollData()
    {
        Debug.Log("Creating Default Doll Database...");
        
        // ID, Name, Description, ImageFileName
        AddDoll(1, "Cutie Cat",   "แมวเหมียวน่ารักแสนซน", "CutieCat.png");
        AddDoll(2, "My Puppy",    "น้องหมาตัวน้อยสุดคิ้ว", "My Puppy.png");
        AddDoll(3, "Honey Bear",  "เจ้าหมีตัวกลมหน้างุ้ง", "HoneyBear.png");
        AddDoll(4, "Ocean Frog",  "กบน้อยหัวใจดวงโโต",   "OceanFrog.png");
        AddDoll(5, "Pinky Piglet","น้องลูกหมูแสนดี",     "PinkyPiglet.png");
        AddDoll(6, "Eh Carrot",   "แครอทนี้ มายังไงเนี่ย", "EhCarrot.png");
        
        SaveDollData(); // This writes the "DollData.json" file
    }

    private void AddDoll(int id, string name, string desc, string path)
    {
        DollData newDoll = new DollData();
        newDoll.doll_id = id;
        newDoll.doll_name = name;
        newDoll.description = desc;
        newDoll.image_path = path;
        dollList.dolls.Add(newDoll);
    }

    private void SaveDollData()
    {
        string json = JsonUtility.ToJson(dollList, true);
        File.WriteAllText(dollFilePath, json);
    }

    private void LoadDollData()
    {
        if (File.Exists(dollFilePath))
        {
            string json = File.ReadAllText(dollFilePath);
            dollList = JsonUtility.FromJson<DollDataList>(json);
        }
    }

    // --- ERROR TYPE FUNCTIONS ---
    private void CreateDefaultErrorTypes()
    {
        AddErrorType(1, "Misspell", "คุณสะกดชื่อฟังก์ชันผิด", "ในภาษาโปรแกรม การสะกดคำสั่งผิดแม้แต่ตัวเดียวจะทำให้คอมพิวเตอร์ไม่เข้าใจ");
        AddErrorType(2, "CaseSensitiveError", "ตัวพิมพ์เล็ก-ใหญ่ผิด", "ในภาษาโปรแกรม ตัวพิมพ์เล็ก-ใหญ่ มีผลต่อความถูกต้อง");
        AddErrorType(3, "MissingParentheses", "ลืมวงเล็บ ()", "วงเล็บมีไว้สำหรับรับค่าพารามิเตอร์");
        AddErrorType(4, "UnclosedParenthesis", "ลืมปิดวงเล็บ", "อย่าลืมปิดวงเล็บและใส่ ; ทุกครั้ง");
        AddErrorType(5, "MissingParameter", "ลืมใส่ตัวเลข", "คำสั่งนี้ต้องการตัวเลขในวงเล็บ");
        AddErrorType(6, "ExtraParameter", "ใส่ตัวเลขเกิน", "คำสั่งนี้ไม่ต้องการตัวเลข หรือใส่เกินจำนวน");
        AddErrorType(7, "InvalidParameter", "ตัวเลขผิดช่วง", "ใส่ค่าได้ไม่เกิน 10");
        AddErrorType(8, "MissingSemicolon", "ลืม ;", "ต้องปิดท้ายคำสั่งด้วย ; เสมอ");
        SaveErrorTypes();
    }

    private void AddErrorType(int id, string code, string desc, string msg)
    {
        ErrorTypeData newError = new ErrorTypeData();
        newError.error_id = id;
        newError.error_code = code;
        newError.description = desc;
        newError.error_message = msg;
        errorTypeList.errorTypes.Add(newError);
    }

    private void SaveErrorTypes()
    {
        string json = JsonUtility.ToJson(errorTypeList, true);
        File.WriteAllText(errorTypeFilePath, json);
    }

    private void LoadErrorTypes()
    {
        if (File.Exists(errorTypeFilePath))
        {
            string json = File.ReadAllText(errorTypeFilePath);
            errorTypeList = JsonUtility.FromJson<ErrorTypeList>(json);
        }
    }

    private void SaveErrorDetails()
    {
        string json = JsonUtility.ToJson(errorDetailList, true);
        File.WriteAllText(errorDetailFilePath, json);
    }
}