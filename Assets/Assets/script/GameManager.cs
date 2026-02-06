using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq; 

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField codeInputField; 
    public ClawController claw;           

    [Header("UI Spawn Settings")]
    public GameObject dollPrefabUI;     
    public Transform gameplayArea;      
    public Transform[] topRowSpots;     
    public Transform[] bottomRowSpots;  

    [Header("Mission Data")]
    public Image missionImageUI;
    public TextMeshProUGUI missionTextUI;
    
    [Header("Doll Database")]
    // IMPOTANT: These names must match DataManager names exactly!
    public string[] allDollNames;        
    public Sprite[] allDollSprites;      
    public string[] allDollDescriptions; 

    [Header("Popups UI")]
    public GameObject startPanel;       // The "Start Mission" Popup
    public Image startDollImage;
    public TextMeshProUGUI startDollName;
    
    public GameObject winPanel;
    public Image winDollImage;
    public TextMeshProUGUI winDollNameText;
    public TextMeshProUGUI winDollDetailText;
    
    public GameObject losePanel;
    public Image loseDollImage;
    public Sprite defaultLoseSprite;

    [Header("Tutorial UI")]
    public GameObject tutorialPanel;    // Panel ใหญ่สุด
    public GameObject tutorialPage1;    // หน้า 1
    public GameObject tutorialPage2;    // หน้า 2
    public Button tutNextBtn1;          // ปุ่ม Next ในหน้า 1
    public Button tutBackBtn;           // ปุ่ม Back ในหน้า 2
    public Button tutFinishBtn;         // ปุ่ม Next (เตรียมตัว) ในหน้า 2
    public Button tutCloseXBtn;         // ปุ่ม X มุมขวาบน

    [Header("Error Popup UI")]
    public GameObject errorPanel;           
    public TextMeshProUGUI errorTitleText;  
    public TextMeshProUGUI errorUserCodeText; 
    public TextMeshProUGUI errorSuggestionText; 
    public TextMeshProUGUI errorDescText;   
    public Button closeErrorButton;         

    public string currentMission;
    private bool isExecuting = false;
    private List<GameObject> spawnedDolls = new List<GameObject>();

    // History Lock Variables
    private string protectedPrefix = ""; 
    private string lastValidText = "";   

    // --- DATA COLLECTION VARIABLES ---
    private float startTime;                
    private int commandCounter = 0;         
    private List<ErrorDetail> sessionErrors = new List<ErrorDetail>();

    public GameObject clawimage;

    void Start()
    {
        // 1. Start Timer (Reset later when tutorial ends)
        startTime = Time.time;
        commandCounter = 0;
        sessionErrors = new List<ErrorDetail>();

        // 2. Hide Result Panels & Start Panel (Hide start panel first because tutorial comes first)
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (errorPanel) errorPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false); 

        // 3. Hide Claw initially (Tutorial is covering it)
        if (clawimage) clawimage.SetActive(false);

        // 4. Pick the Target Doll (Mission)
        RandomizeMission();

        if (closeErrorButton) closeErrorButton.onClick.AddListener(CloseErrorPanel);

        if(codeInputField) 
        {
            codeInputField.ActivateInputField();
            codeInputField.onValueChanged.AddListener(ValidateInputPreservation);
            codeInputField.onValidateInput += ValidateCharInput;
        }

        // 5. Start Tutorial
        SetupTutorial();
    }

    void SetupTutorial()
    {
        // เชื่อมปุ่มต่างๆ เข้ากับฟังก์ชัน
        if(tutNextBtn1) tutNextBtn1.onClick.AddListener(GoToPage2);
        if(tutBackBtn) tutBackBtn.onClick.AddListener(GoToPage1);
        if(tutFinishBtn) tutFinishBtn.onClick.AddListener(EndTutorial);
        if(tutCloseXBtn) tutCloseXBtn.onClick.AddListener(EndTutorial);

        // เปิด Tutorial หน้าแรก
        if (tutorialPanel)
        {
            tutorialPanel.SetActive(true);
            if(tutorialPage1) tutorialPage1.SetActive(true);
            if(tutorialPage2) tutorialPage2.SetActive(false);
            if (clawimage) clawimage.SetActive(false);
        }
        else
        {
            // ถ้าลืมใส่ TutorialPanel ให้ข้ามไปเริ่มเกมเลย
            EndTutorial();
        }
    }

    void GoToPage1()
    {
        if(tutorialPage1) tutorialPage1.SetActive(true);
        if(tutorialPage2) tutorialPage2.SetActive(false);
    }

    void GoToPage2()
    {
        if(tutorialPage1) tutorialPage1.SetActive(false);
        if(tutorialPage2) tutorialPage2.SetActive(true);
    }

    void EndTutorial()
    {
        if (tutorialPanel) tutorialPanel.SetActive(false);
        
        // เปิดหน้า Mission Start (เริ่มเกมจริง)
        if (startPanel) startPanel.SetActive(true);
    }

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !isExecuting)
        {
            // ห้ามรันถ้า Tutorial เปิดอยู่
            bool isTutorialActive = tutorialPanel != null && tutorialPanel.activeSelf;

            if (codeInputField.isFocused && !errorPanel.activeSelf && !isTutorialActive) 
            {
                OnRunCode();
            }
        }
        
        // Caret Guard
        if (codeInputField.isFocused)
        {
            if (codeInputField.caretPosition < protectedPrefix.Length)
                codeInputField.caretPosition = codeInputField.text.Length;
            
            if (codeInputField.selectionAnchorPosition < protectedPrefix.Length || 
                codeInputField.selectionFocusPosition < protectedPrefix.Length)
            {
                codeInputField.selectionAnchorPosition = codeInputField.text.Length;
                codeInputField.selectionFocusPosition = codeInputField.text.Length;
            }
        }
    }

    char ValidateCharInput(string text, int charIndex, char addedChar)
    {
        if (charIndex < protectedPrefix.Length) return '\0';
        return addedChar;
    }

    void ValidateInputPreservation(string currentText)
    {
        if (!currentText.StartsWith(protectedPrefix))
        {
            codeInputField.text = lastValidText;
            codeInputField.caretPosition = codeInputField.text.Length;
        }
        else
        {
            lastValidText = currentText;
        }
    }

    public void OnClickStartGame() 
    { 
        // Called when user clicks "OK" on the Start Popup
        if (startPanel) startPanel.SetActive(false); 
        
        // เริ่มจับเวลาจริงจังตรงนี้ (หลังกด Start Mission)
        startTime = Time.time; 
        
        if (clawimage) clawimage.SetActive(true);
    }
    
    public void RandomizeMission() 
    {
        if (allDollNames.Length == 0) return;

        // 1. Randomly pick a doll index
        int missionIndex = Random.Range(0, allDollNames.Length);
        
        // 2. Set the Current Mission Name
        currentMission = allDollNames[missionIndex];

        // 3. Update UI
        if (missionImageUI) { missionImageUI.sprite = allDollSprites[missionIndex]; missionImageUI.preserveAspect = true; }
        if (missionTextUI) missionTextUI.text = currentMission;
        
        // 4. Update the Start Popup
        if (startDollImage) { startDollImage.sprite = allDollSprites[missionIndex]; startDollImage.preserveAspect = true; }
        if (startDollName) startDollName.text = currentMission;
        
        // 5. Spawn dolls into the claw machine
        SpawnDolls(missionIndex);
    }
    
    void SpawnDolls(int missionIndex) 
    { 
        foreach (GameObject d in spawnedDolls) Destroy(d);
        spawnedDolls.Clear();

        List<Transform> availableTop = new List<Transform>(topRowSpots);
        List<Transform> availableBottom = new List<Transform>(bottomRowSpots);
        List<Transform> allRemainingSpots = new List<Transform>();

        // Ensure the mission doll is in the top row
        if (availableTop.Count > 0) {
            int randPos = Random.Range(0, availableTop.Count);
            CreateDollUI(missionIndex, availableTop[randPos]);
            availableTop.RemoveAt(randPos); 
        }

        allRemainingSpots.AddRange(availableTop);
        allRemainingSpots.AddRange(availableBottom);

        // Fill remaining spots
        foreach (Transform spot in allRemainingSpots) {
            int randomDollIndex;
            do { randomDollIndex = Random.Range(0, allDollNames.Length); } 
            while (randomDollIndex == missionIndex); 
            CreateDollUI(randomDollIndex, spot);
        }
    }
    
    void CreateDollUI(int index, Transform spot) 
    { 
        GameObject newDoll = Instantiate(dollPrefabUI, gameplayArea);
        newDoll.transform.position = spot.position;
        
        Image img = newDoll.GetComponent<Image>();
        if (img) { img.sprite = allDollSprites[index]; img.preserveAspect = true; }
        
        Doll d = newDoll.GetComponent<Doll>();
        if (d) d.dollName = allDollNames[index];
        
        newDoll.name = allDollNames[index];
        spawnedDolls.Add(newDoll);
    }

    public void OnRunCode()
    {
        if (isExecuting) return;
        commandCounter++; // Count the run command
        StartCoroutine(ExecuteLastLineOnly());
    }

    IEnumerator ExecuteLastLineOnly()
    {
        isExecuting = true;
        
        string[] lines = codeInputField.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) { isExecuting = false; yield break; }

        string lastLine = lines[lines.Length - 1]; 
        string rawLine = lastLine.Trim();
        int currentLineNum = lines.Length;

        if (string.IsNullOrEmpty(rawLine) && lines.Length > 1) 
        {
            rawLine = lines[lines.Length - 2].Trim();
            currentLineNum--;
        }

        if (!string.IsNullOrEmpty(rawLine))
        {
            if (!rawLine.EndsWith(";")) 
            {
                ShowError(8, "MissingSemicolon", rawLine, rawLine + ";", currentLineNum);
                isExecuting = false; yield break;
            }

            string commandFull = rawLine.Substring(0, rawLine.Length - 1).Trim(); 
            string cmdName = "";
            string paramContent = "";
            bool hasOpenParen = commandFull.Contains("(");
            bool hasCloseParen = commandFull.Contains(")");

            if (hasOpenParen)
            {
                cmdName = commandFull.Substring(0, commandFull.IndexOf("(")).Trim();
                if (hasCloseParen)
                {
                    int pFrom = commandFull.IndexOf("(") + 1;
                    int pTo = commandFull.LastIndexOf(")");
                    if (pTo > pFrom) paramContent = commandFull.Substring(pFrom, pTo - pFrom).Trim();
                }
            }
            else
            {
                cmdName = commandFull; 
            }

            string[] validCommands = { "GoLeft", "GoRight", "MoveLeft", "MoveRight", "MoveDown", "Grip" };

            // Check Case Sensitive
            bool caseMatch = validCommands.Any(x => x.Equals(cmdName, System.StringComparison.OrdinalIgnoreCase));
            bool exactMatch = validCommands.Contains(cmdName);

            if (caseMatch && !exactMatch)
            {
                string correctCmd = validCommands.First(x => x.Equals(cmdName, System.StringComparison.OrdinalIgnoreCase));
                ShowError(2, "CaseSensitiveError", rawLine, correctCmd + "(...);", currentLineNum);
                isExecuting = false; yield break;
            }

            // Check Misspell
            if (!exactMatch)
            {
                 // Simple guess logic
                  string guess = "GoLeft";
                  if (cmdName.Contains("Right")) guess = "GoRight";
                  if (cmdName.Contains("Down")) guess = "MoveDown";
                  if (cmdName.Contains("Grip")) guess = "Grip";
                  ShowError(1, "Misspell", rawLine, guess + "(...);", currentLineNum);
                  isExecuting = false; yield break;
            }

            // Check Parentheses
            if (!hasOpenParen || !hasCloseParen)
            {
                if (!hasOpenParen) ShowError(3, "MissingParentheses", rawLine, cmdName + "();", currentLineNum);
                else ShowError(4, "UnclosedParenthesis", rawLine, cmdName + "();", currentLineNum);
                isExecuting = false; yield break;
            }

            bool needNumber = (cmdName == "GoLeft" || cmdName == "GoRight" || cmdName == "MoveLeft" || cmdName == "MoveRight");
            bool noNumber = (cmdName == "MoveDown" || cmdName == "Grip");

            if (needNumber)
            {
                if (string.IsNullOrEmpty(paramContent))
                {
                    ShowError(5, "MissingParameter", rawLine, cmdName + "(1);", currentLineNum);
                    isExecuting = false; yield break;
                }

                if (!int.TryParse(paramContent, out int val))
                {
                    if (paramContent.Contains(",")) ShowError(6, "ExtraParameter", rawLine, cmdName + "(1);", currentLineNum);
                    else ShowError(7, "InvalidParameter", rawLine, cmdName + "(1);", currentLineNum);
                    isExecuting = false; yield break;
                }

                if (val < 0 || val > 10)
                {
                    ShowError(7, "InvalidParameter", rawLine, "ใส่เลข 1-10", currentLineNum);
                    isExecuting = false; yield break;
                }

                if (cmdName.Contains("Left")) yield return claw.MoveHorizontal(-val);
                else yield return claw.MoveHorizontal(val);
            }
            else if (noNumber)
            {
                if (!string.IsNullOrEmpty(paramContent))
                {
                    ShowError(6, "ExtraParameter", rawLine, cmdName + "();", currentLineNum);
                    isExecuting = false; yield break;
                }

                if (cmdName == "MoveDown") yield return claw.MoveDown();
                else if (cmdName == "Grip") { yield return claw.GripAndUp(); CheckResult(); }
            }
        }

        if (!codeInputField.text.EndsWith("\n")) codeInputField.text += "\n";
        protectedPrefix = codeInputField.text;
        lastValidText = protectedPrefix;
        codeInputField.caretPosition = codeInputField.text.Length;
        codeInputField.ActivateInputField(); 

        isExecuting = false;
    }

    void ShowError(int id, string codeType, string userCode, string suggest, int lineNum = 0)
    {
        // 1. Record Error to List
        ErrorDetail newErr = new ErrorDetail();
        newErr.line_number = lineNum;
        newErr.command_text = userCode;
        newErr.error_type = codeType; 
        sessionErrors.Add(newErr);

        // 2. Show UI
        if (errorPanel == null) return;

        errorPanel.SetActive(true);
        if (clawimage) clawimage.SetActive(false); // Hide claw
        
        if(errorUserCodeText) errorUserCodeText.text = userCode;
        if(errorSuggestionText) errorSuggestionText.text = suggest;

        string title = "";
        string desc = "";
        
        switch (id)
        {
            case 1: title = "Misspell (สะกดผิด)"; desc = "ในภาษาโปรแกรม ชื่อฟังก์ชันต้องตรงกับที่ประกาศไว้ ทุกตัวอักษร สะกดผิดเพียงตัวเดียวโปรแกรมจะไม่รู้จักคำสั่งนั้น"; break;
            case 2: title = "CaseSensitiveError"; desc = "ในภาษาโปรแกรม ตัวพิมพ์เล็ก-ใหญ่ มีผลต่อความถูกต้องของชื่อตัวแปร"; break;
            case 3: title = "MissingParentheses (ลืมวงเล็บ)"; desc = "ในภาษาโปรแกรม วงเล็บมีไว้สำหรับรับค่าพารามิเตอร์ ในกรณีนี้มีไว้สำหรับใส่ตัวเลข เพื่อขยับที่คีบตุ๊กตา"; break;
            case 4: title = "UnclosedParenthesis (ลืมปิดวงเล็บ)"; desc = "รายละเอียดเล็กน้อย ในภาษาโปรแกรมเป็นสิ่งสำคัญและที่สำคัญอย่าลืม ; (semicolon) ปิดท้ายทุกครั้งด้วยนะ"; break;
            case 5: title = "MissingParameter (ลืมใส่พารามิเตอร์)"; desc = "ในภาษาโปรแกรม พารามิเตอร์คือค่าตัวเลขที่ต้องการรับ กรณีนี้ต้องการให้ตัวคีบขยับ ซึ่งรับค่าโดยใส่จำนวนภายในวงเล็บ ()"; break;
            case 6: title = "ExtraParameter (ใส่พารามิเตอร์เกิน)"; desc = "ในภาษาโปรแกรม พารามิเตอร์คือค่าตัวเลขที่ต้องการรับ จาก Error คุณใส่จำนวนตัวเลขที่ต้องการขยับตัวคีบเกิน"; break;
            case 7: title = "InvalidParameter (ใส่พารามิเตอร์ผิดรูปแบบ)"; desc = "ในภาษาโปรแกรม พารามิเตอร์คือค่าตัวเลขที่ต้องการรับ จาก Error คุณใส่จำนวนตัวเลขที่ต้องการขยับตัวคีบเกิน ซึ่งในกรณีนี้ จะใส่ค่ามากสุดได้ไม่เกิน 10"; break;
            case 8: title = "MissingSemicolon (ลืม ;)"; desc = "ในภาษาโปรแกรม ต้องมีการปิดท้ายในคำสั่ง โดยใช้เครื่องหมาย ; (semicolon)"; break;
        }

        if(errorTitleText) errorTitleText.text = title;
        if(errorDescText) errorDescText.text = desc;
    }

    void CloseErrorPanel() 
    { 
        if (errorPanel) errorPanel.SetActive(false);
        if (codeInputField) {
            codeInputField.ActivateInputField(); 
            codeInputField.caretPosition = codeInputField.text.Length;
        }
        if (clawimage) clawimage.SetActive(true); // Show claw
    }

    // --- KEY FUNCTION: LINKS DATA TO DATABASE ---
    void CheckResult()
    {
        string caught = claw.GetCaughtDollName();
        int dataIndex = -1;
        for (int i = 0; i < allDollNames.Length; i++) if (allDollNames[i] == caught) dataIndex = i;

        bool isWin = (caught == currentMission);
        
        // SAVE TO DATABASE
        if (DataManager.Instance != null)
        {
            int duration = Mathf.RoundToInt(Time.time - startTime);

            DataManager.Instance.AddNewUserLog(
                currentMission,     // THIS IS THE TARGET DOLL NAME
                commandCounter,     
                sessionErrors.Count,
                duration,           
                isWin,              
                sessionErrors       
            );
        }

        if (isWin)
        {
            if (winPanel)
            {
                winPanel.SetActive(true);
                if (dataIndex != -1 && winDollImage) { winDollImage.sprite = allDollSprites[dataIndex]; winDollImage.preserveAspect = true; }
                if (winDollNameText) winDollNameText.text = currentMission;
                if (winDollDetailText && dataIndex < allDollDescriptions.Length) winDollDetailText.text = allDollDescriptions[dataIndex];
            }
            if (clawimage) clawimage.SetActive(false);
        }
        else
        {
            if (losePanel)
            {
                losePanel.SetActive(true);
                if (loseDollImage) {
                    if (dataIndex != -1) { loseDollImage.sprite = allDollSprites[dataIndex]; loseDollImage.preserveAspect = true; }
                    else loseDollImage.sprite = defaultLoseSprite;
                }
            }
            if (clawimage) clawimage.SetActive(false);
        }
    }

    public void OnClickReplay()
    {
        // Reset Data for new round
        startTime = Time.time;
        commandCounter = 0;
        sessionErrors.Clear();

        RandomizeMission();
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (errorPanel) errorPanel.SetActive(false);
        if (codeInputField) 
        {
            codeInputField.text = "";
            protectedPrefix = ""; 
            lastValidText = "";
        }
        if (startPanel) startPanel.SetActive(true);
    }
}