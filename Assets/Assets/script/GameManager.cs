using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq; 
using UnityEngine.SceneManagement; 

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
    
    // ✨ [อัปเดตใหม่] ตัวหนังสือสำหรับแสดงเวลาตอนเล่นเกม (บนหน้าต่าง Mission)
    [Header("Timer UI")]
    public TextMeshProUGUI timerTextUI; 

    [Header("Doll Database")]
    public string[] allDollNames;        
    public Sprite[] allDollSprites;      
    public string[] allDollDescriptions; 

    [Header("Popups UI (ผลลัพธ์การคีบ)")]
    public GameObject startPanel;       
    public Image startDollImage;
    public TextMeshProUGUI startDollName;
    
    public GameObject winPanel;
    public Image winDollImage;
    public TextMeshProUGUI winDollNameText;
    public TextMeshProUGUI winDollDetailText;
    
    public GameObject losePanel;
    public Image loseDollImage;
    public Sprite defaultLoseSprite;

    [Header("Empty Claw UI (คีบว่าว)")]
    public GameObject emptyClawPanel;   

    [Header("Code Result UI (หน้าสรุปผลโค้ด)")]
    public GameObject codeResultPanel;
    public TextMeshProUGUI totalCommandText;      
    public TextMeshProUGUI allCommandsText;       
    public TextMeshProUGUI correctCommandsText;   
    public TextMeshProUGUI incorrectCommandsText; 

    // ✨ [อัปเดตใหม่] ตัวแปรสำหรับแสดงข้อมูลเพิ่มเติมในหน้า Result Popup
    [Header("Result Stats UI")]
    public TextMeshProUGUI resultTimeText;       // แสดงเวลาที่ใช้ไปทั้งหมด
    public TextMeshProUGUI correctPercentText;   // แสดงตัวเลข % ที่พิมพ์ถูก
    public TextMeshProUGUI incorrectPercentText; // แสดงตัวเลข % ที่พิมพ์ผิด

    [Header("Tutorial UI")]
    public GameObject tutorialPanel;    
    public GameObject tutorialPage1;    
    public GameObject tutorialPage2;    
    public Button tutNextBtn1;          
    public Button tutBackBtn;           
    public Button tutFinishBtn;         
    public Button tutCloseXBtn;         

    [Header("Error Popup UI")]
    public GameObject errorPanel;           
    public TextMeshProUGUI errorTitleText;  
    public TextMeshProUGUI errorUserCodeText; 
    public TextMeshProUGUI errorSuggestionText; 
    public TextMeshProUGUI errorDescText;   
    public Button closeErrorButton;         

    [Header("Empty Input Popup UI")]
    public GameObject emptyInputPanel;
    public Button closeEmptyInputButton;

    [Header("MovedDown Error Popup UI")]
    public GameObject movedDownErrorPanel;
    public Button closeMovedDownErrorButton;

    // ✨ [อัปเดตใหม่] ระบบ Advance Mode
    [Header("Advance Mode UI")]
    public GameObject advanceModePanel;     // พาเนลที่จะเด้งขึ้นมา
    public Button advanceModeButton;        // ปุ่มสำหรับกดเปิด/ปิด

    [Header("Audio Settings (เสียงระบบ)")]
    public AudioSource uiAudioSource;       
    public AudioClip buttonClickSound;      
    public AudioClip winSound;              
    public AudioClip loseSound;             
    public AudioClip codeCorrectSound;      
    public AudioClip codeErrorSound;        
    public AudioClip dollDropSound;     

    [Header("Drop Animation Settings")]
    public RectTransform dropBoxTarget;     
    public float dropZoneX = -350f;         
    public float spawnDropOffset = 800f; 

    [Header("Scene Settings")]
    public string homeSceneName = "Home"; 

    public string currentMission;
    private bool isExecuting = false;
    private List<GameObject> spawnedDolls = new List<GameObject>();
    private Dictionary<GameObject, Vector3> dollTargetPositions = new Dictionary<GameObject, Vector3>();

    private string protectedPrefix = ""; 
    private string lastValidText = "";   

    // ✨ [อัปเดตใหม่] ระบบจัดการเวลา
    private float startTime;                
    private float elapsedGameTime = 0f;     
    private bool isTimerRunning = false;    

    private int commandCounter = 0;         
    private List<ErrorDetail> sessionErrors = new List<ErrorDetail>();

    // ตัวแปรเช็คสถานะการทำงานของที่คีบ
    private bool hasGripped = false;
    private bool hasMovedDown = false; 

    private struct CommandLog
    {
        public string commandText;
        public bool isCorrect;
    }
    private List<CommandLog> commandHistory = new List<CommandLog>();

    public GameObject clawimage;

    // ---------------------------------------------------------
    // ฟังก์ชัน Start: ทำงานครั้งแรกสุดเมื่อเปิดฉาก (Scene) นี้ขึ้นมา
    // ---------------------------------------------------------
    void Start()
    {
        startTime = Time.time;
        commandCounter = 0;
        elapsedGameTime = 0f;
        isTimerRunning = false; 

        sessionErrors = new List<ErrorDetail>();
        commandHistory.Clear(); 
        
        hasGripped = false; 
        hasMovedDown = false; 

        if (codeInputField) codeInputField.interactable = true; 
        if (timerTextUI) timerTextUI.text = "00:00"; // รีเซ็ตตัวหนังสือเวลาเริ่มต้น

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (emptyClawPanel) emptyClawPanel.SetActive(false);
        if (codeResultPanel) codeResultPanel.SetActive(false);
        if (errorPanel) errorPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false); 
        if (emptyInputPanel) emptyInputPanel.SetActive(false); 
        if (movedDownErrorPanel) movedDownErrorPanel.SetActive(false); 
        if (clawimage) clawimage.SetActive(false);

        // ✨ [อัปเดตใหม่] ตั้งค่าเริ่มต้นให้ Advance Mode
        if (advanceModePanel) advanceModePanel.SetActive(false); // ซ่อน Panel ไว้ตอนเริ่มเกม
        if (advanceModeButton) advanceModeButton.onClick.AddListener(ToggleAdvanceMode); // ผูกปุ่มเข้ากับฟังก์ชัน

        RandomizeMission(); 

        if (closeErrorButton) closeErrorButton.onClick.AddListener(CloseErrorPanel);
        if (closeEmptyInputButton) closeEmptyInputButton.onClick.AddListener(CloseEmptyInputPanel);
        if (closeMovedDownErrorButton) closeMovedDownErrorButton.onClick.AddListener(CloseMovedDownErrorPanel); 

        if(codeInputField) 
        {
            codeInputField.ActivateInputField();
            codeInputField.onValueChanged.AddListener(ValidateInputPreservation);
            codeInputField.onValidateInput += ValidateCharInput;
        }

        SetupTutorial(); 
    }

    public void PlayButtonSound()
    {
        if (uiAudioSource && buttonClickSound) uiAudioSource.PlayOneShot(buttonClickSound);
    }

    // ✨ [อัปเดตใหม่] ฟังก์ชันสำหรับเปิด/ปิด Advance Mode Panel
    public void ToggleAdvanceMode()
    {
        PlayButtonSound();
        if (advanceModePanel != null)
        {
            // เช็คสถานะปัจจุบันว่าเปิดหรือปิดอยู่ แล้วสั่งให้เป็นค่าตรงข้าม
            bool isActive = advanceModePanel.activeSelf;
            advanceModePanel.SetActive(!isActive);
        }
    }

    // ---------------------------------------------------------
    // หมวด Tutorial: จัดการหน้าต่างสอนเล่นเมื่อเข้าเกมครั้งแรก
    // ---------------------------------------------------------
    void SetupTutorial()
    {
        if(tutNextBtn1) tutNextBtn1.onClick.AddListener(GoToPage2);
        if(tutBackBtn) tutBackBtn.onClick.AddListener(GoToPage1);
        if(tutFinishBtn) tutFinishBtn.onClick.AddListener(EndTutorial);
        if(tutCloseXBtn) tutCloseXBtn.onClick.AddListener(EndTutorial);

        if (tutorialPanel)
        {
            tutorialPanel.SetActive(true);
            if(tutorialPage1) tutorialPage1.SetActive(true);
            if(tutorialPage2) tutorialPage2.SetActive(false);
            if (clawimage) clawimage.SetActive(false);
            
            if (codeInputField) codeInputField.interactable = false; 
        }
        else EndTutorial();
    }
    void GoToPage1() { PlayButtonSound(); if(tutorialPage1) tutorialPage1.SetActive(true); if(tutorialPage2) tutorialPage2.SetActive(false); }
    void GoToPage2() { PlayButtonSound(); if(tutorialPage1) tutorialPage1.SetActive(false); if(tutorialPage2) tutorialPage2.SetActive(true); }
    
    void EndTutorial() 
    { 
        PlayButtonSound(); 
        if (tutorialPanel) tutorialPanel.SetActive(false); 
        if (startPanel) startPanel.SetActive(true); 
    }

    // ---------------------------------------------------------
    // ฟังก์ชัน Update: ทำงานทุกๆ เฟรม ใช้ดักจับการกดคีย์บอร์ดของผู้เล่น
    // ---------------------------------------------------------
    void Update()
    {
        // ✨ [อัปเดตใหม่] ระบบจับเวลาแบบ Real-time เดินเฉพาะตอนเกมเล่นอยู่
        if (isTimerRunning)
        {
            elapsedGameTime = Time.time - startTime;
            if (timerTextUI != null)
            {
                int minutes = Mathf.FloorToInt(elapsedGameTime / 60F);
                int seconds = Mathf.FloorToInt(elapsedGameTime - minutes * 60);
                timerTextUI.text = string.Format("{0:00}:{1:00}", minutes, seconds); // แสดงผลแบบ นาที:วินาที
            }
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && 
             Input.GetKeyDown(KeyCode.B))
        {
            SceneManager.LoadScene(homeSceneName);
        }

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !isExecuting)
        {
            bool isTutorialActive = tutorialPanel != null && tutorialPanel.activeSelf;
            if (codeInputField.isFocused && !errorPanel.activeSelf && (emptyInputPanel == null || !emptyInputPanel.activeSelf) && (movedDownErrorPanel == null || !movedDownErrorPanel.activeSelf) && !isTutorialActive) 
                OnRunCode();
        }
        
        if (codeInputField.isFocused)
        {
            if (codeInputField.caretPosition < protectedPrefix.Length) codeInputField.caretPosition = codeInputField.text.Length;
            if (codeInputField.selectionAnchorPosition < protectedPrefix.Length || codeInputField.selectionFocusPosition < protectedPrefix.Length)
            {
                codeInputField.selectionAnchorPosition = codeInputField.text.Length;
                codeInputField.selectionFocusPosition = codeInputField.text.Length;
            }
        }
    }

    char ValidateCharInput(string text, int charIndex, char addedChar) { if (charIndex < protectedPrefix.Length) return '\0'; return addedChar; }
    
    void ValidateInputPreservation(string currentText)
    {
        if (currentText.Length < protectedPrefix.Length || !currentText.StartsWith(protectedPrefix))
        {
            codeInputField.SetTextWithoutNotify(lastValidText);
            codeInputField.caretPosition = lastValidText.Length;
        }
        else lastValidText = currentText;
    }

    // ---------------------------------------------------------
    // ระบบเตรียมตัวเริ่มเกม และจัดการตุ๊กตา
    // ---------------------------------------------------------
    public void OnClickStartGame() 
    { 
        PlayButtonSound(); 
        if (startPanel) startPanel.SetActive(false); 
        
        if (codeInputField) { codeInputField.interactable = true; codeInputField.ActivateInputField(); }

        // ✨ [อัปเดตใหม่] เริ่มนับเวลาเมื่อกดเริ่มเกม
        startTime = Time.time; 
        isTimerRunning = true; 

        hasGripped = false; 
        hasMovedDown = false; 
        if (clawimage) clawimage.SetActive(true);
        StartCoroutine(AnimateDollsFalling());
    }
    
    public void RandomizeMission() 
    {
        if (allDollNames.Length == 0) return;
        int missionIndex = Random.Range(0, allDollNames.Length);
        currentMission = allDollNames[missionIndex];
        if (missionImageUI) { missionImageUI.sprite = allDollSprites[missionIndex]; missionImageUI.preserveAspect = true; }
        if (missionTextUI) missionTextUI.text = currentMission;
        if (startDollImage) { startDollImage.sprite = allDollSprites[missionIndex]; startDollImage.preserveAspect = true; }
        if (startDollName) startDollName.text = currentMission;
        SpawnDolls(missionIndex);
    }
    
    void SpawnDolls(int missionIndex) 
    { 
        foreach (GameObject d in spawnedDolls) Destroy(d);
        spawnedDolls.Clear();
        dollTargetPositions.Clear(); 

        List<Transform> availableTop = new List<Transform>(topRowSpots);
        List<Transform> availableBottom = new List<Transform>(bottomRowSpots);
        List<Transform> allRemainingSpots = new List<Transform>();

        if (availableTop.Count > 0) {
            int randPos = Random.Range(0, availableTop.Count);
            CreateDollUI(missionIndex, availableTop[randPos]);
            availableTop.RemoveAt(randPos); 
        }

        allRemainingSpots.AddRange(availableTop);
        allRemainingSpots.AddRange(availableBottom);

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
        Vector3 targetPos = spot.position;
        dollTargetPositions.Add(newDoll, targetPos);
        newDoll.transform.position = targetPos + new Vector3(0, spawnDropOffset, 0); 

        Image img = newDoll.GetComponent<Image>();
        if (img) 
        { 
            img.sprite = allDollSprites[index]; 
            img.SetNativeSize(); 
            
            RectTransform rect = img.GetComponent<RectTransform>();
            float maxWidth = 190f;  
            float maxHeight = 210f; 
            
            float widthRatio = maxWidth / rect.sizeDelta.x;
            float heightRatio = maxHeight / rect.sizeDelta.y;
            float finalRatio = Mathf.Min(widthRatio, heightRatio);
            
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * finalRatio, rect.sizeDelta.y * finalRatio);
        }

        Doll d = newDoll.GetComponent<Doll>();
        if (d) d.dollName = allDollNames[index];
        newDoll.name = allDollNames[index];
        spawnedDolls.Add(newDoll);
    }    

    // ---------------------------------------------------------
    // แอนิเมชันตอนตุ๊กตาร่วงลงมาพร้อมกัน
    // ---------------------------------------------------------
    IEnumerator AnimateDollsFalling()
    {
        if (uiAudioSource && dollDropSound) uiAudioSource.PlayOneShot(dollDropSound);
        foreach (var kvp in dollTargetPositions)
        {
            if (kvp.Key != null)
            {
                float randomDelay = Random.Range(0f, 0.6f); 
                StartCoroutine(DropSingleDoll(kvp.Key, kvp.Value, randomDelay));
            }
        }
        yield return null;
    }

    IEnumerator DropSingleDoll(GameObject doll, Vector3 targetPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        float duration = 0.8f; 
        float t = 0;
        Vector3 startPos = targetPos + new Vector3(0, spawnDropOffset, 0);

        while (t < 1f)
        {
            if (doll == null) yield break; 
            t += Time.deltaTime / duration;
            float ease = BounceEaseOut(t); 
            doll.transform.position = Vector3.LerpUnclamped(startPos, targetPos, ease);
            yield return null;
        }
        if (doll != null) doll.transform.position = targetPos;
        if (uiAudioSource && dollDropSound) uiAudioSource.PlayOneShot(dollDropSound, 0.5f);
    }

    float BounceEaseOut(float t)
    {
        if (t < 1f / 2.75f) return 7.5625f * t * t;
        else if (t < 2f / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + 0.75f; }
        else if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + 0.9375f; }
        else { t -= 2.625f / 2.75f; return 7.5625f * t * t + 0.984375f; }
    }

    // ---------------------------------------------------------
    // ระบบตรวจสอบคำสั่งโค้ด (หัวใจหลักของเกม)
    // ---------------------------------------------------------
    public void OnRunCode()
    {
        if (isExecuting) return;
        commandCounter++; 
        StartCoroutine(ExecuteLastLineOnly());
    }

    IEnumerator ExecuteLastLineOnly()
    {
        isExecuting = true;
        
        string[] lines = codeInputField.text.Split(new char[] { '\n' });
        if (lines.Length == 0) { isExecuting = false; yield break; }

        string rawLine = lines[lines.Length - 1].Trim();
        int currentLineNum = lines.Length;

        if (string.IsNullOrEmpty(rawLine) && lines.Length > 1) 
        {
            rawLine = lines[lines.Length - 2].Trim();
            currentLineNum--;
        }

        string cleanRawLine = rawLine;

        if (string.IsNullOrEmpty(cleanRawLine))
        {
            if (uiAudioSource && codeErrorSound) uiAudioSource.PlayOneShot(codeErrorSound);
            if (emptyInputPanel) emptyInputPanel.SetActive(true); 
            if (clawimage) clawimage.SetActive(false); 
            
            if (codeInputField) codeInputField.interactable = false;

            LockAndColorLastLine("", false); 
            yield break;
        }

        if (hasGripped)
        {
            ShowError(9, "AlreadyGripped", cleanRawLine, "เริ่มเกมใหม่เพื่อคีบอีกครั้ง", currentLineNum);
            LockAndColorLastLine(cleanRawLine, false);
            yield break;
        }

        if (!string.IsNullOrEmpty(cleanRawLine))
        {
            if (!cleanRawLine.EndsWith(";")) 
            {
                ShowError(8, "MissingSemicolon", cleanRawLine, cleanRawLine + ";", currentLineNum);
                LockAndColorLastLine(cleanRawLine, false); yield break; 
            }

            string commandFull = cleanRawLine.Substring(0, cleanRawLine.Length - 1).Trim(); 
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
            else cmdName = commandFull; 

            string[] validCommands = { "GoLeft", "GoRight", "MoveLeft", "MoveRight", "MoveDown", "Grip" };

            bool caseMatch = validCommands.Any(x => x.Equals(cmdName, System.StringComparison.OrdinalIgnoreCase));
            bool exactMatch = validCommands.Contains(cmdName);

            if (caseMatch && !exactMatch)
            {
                string correctCmd = validCommands.First(x => x.Equals(cmdName, System.StringComparison.OrdinalIgnoreCase));
                ShowError(2, "CaseSensitiveError", cleanRawLine, correctCmd + "(...);", currentLineNum);
                LockAndColorLastLine(cleanRawLine, false); yield break; 
            }

            if (!exactMatch)
            {
                  string guess = "GoLeft";
                  if (cmdName.Contains("Right")) guess = "GoRight";
                  if (cmdName.Contains("Down")) guess = "MoveDown";
                  if (cmdName.Contains("Grip")) guess = "Grip";
                  ShowError(1, "Misspell", cleanRawLine, guess + "(...);", currentLineNum);
                  LockAndColorLastLine(cleanRawLine, false); yield break; 
            }

            if (!hasOpenParen || !hasCloseParen)
            {
                if (!hasOpenParen) ShowError(3, "MissingParentheses", cleanRawLine, cmdName + "();", currentLineNum);
                else ShowError(4, "UnclosedParenthesis", cleanRawLine, cmdName + "();", currentLineNum);
                LockAndColorLastLine(cleanRawLine, false); yield break; 
            }

            bool needNumber = (cmdName == "GoLeft" || cmdName == "GoRight" || cmdName == "MoveLeft" || cmdName == "MoveRight");
            bool noNumber = (cmdName == "MoveDown" || cmdName == "Grip");

            if (needNumber && hasMovedDown)
            {
                commandHistory.Add(new CommandLog { commandText = cleanRawLine, isCorrect = false });
                if (uiAudioSource && codeErrorSound) uiAudioSource.PlayOneShot(codeErrorSound);

                ErrorDetail newErr = new ErrorDetail();
                newErr.line_number = currentLineNum;
                newErr.command_text = cleanRawLine;
                newErr.error_type = "AlreadyMovedDown"; 
                sessionErrors.Add(newErr);

                if (movedDownErrorPanel) movedDownErrorPanel.SetActive(true);
                if (clawimage) clawimage.SetActive(false); 
                
                if (codeInputField) codeInputField.interactable = false; 

                LockAndColorLastLine(cleanRawLine, false);
                yield break;
            }

            if (needNumber)
            {
                if (string.IsNullOrEmpty(paramContent))
                {
                    ShowError(5, "MissingParameter", cleanRawLine, cmdName + "(1);", currentLineNum);
                    LockAndColorLastLine(cleanRawLine, false); yield break; 
                }

                if (!int.TryParse(paramContent, out int val))
                {
                    if (paramContent.Contains(",")) ShowError(6, "ExtraParameter", cleanRawLine, cmdName + "(1);", currentLineNum);
                    else ShowError(7, "InvalidParameter", cleanRawLine, cmdName + "(1);", currentLineNum);
                    LockAndColorLastLine(cleanRawLine, false); yield break; 
                }

                if (val < 0)
                {
                    ShowError(7, "InvalidParameter", cleanRawLine, "ใส่ค่าติดลบไม่ได้", currentLineNum);
                    LockAndColorLastLine(cleanRawLine, false); yield break; 
                }

                commandHistory.Add(new CommandLog { commandText = cleanRawLine, isCorrect = true });
                if (uiAudioSource && codeCorrectSound) uiAudioSource.PlayOneShot(codeCorrectSound);

                LockAndColorLastLine(cleanRawLine, true); 
                if (cmdName.Contains("Left")) yield return claw.MoveHorizontal(-val);
                else yield return claw.MoveHorizontal(val);
            }
            else if (noNumber)
            {
                if (!string.IsNullOrEmpty(paramContent))
                {
                    ShowError(6, "ExtraParameter", cleanRawLine, cmdName + "();", currentLineNum);
                    LockAndColorLastLine(cleanRawLine, false); yield break; 
                }

                commandHistory.Add(new CommandLog { commandText = cleanRawLine, isCorrect = true });
                if (uiAudioSource && codeCorrectSound) uiAudioSource.PlayOneShot(codeCorrectSound);

                LockAndColorLastLine(cleanRawLine, true); 
                
                if (cmdName == "MoveDown") 
                {
                    hasMovedDown = true; 
                    yield return claw.MoveDown();
                }
                else if (cmdName == "Grip") 
                { 
                    hasGripped = true; 
                    yield return claw.GripAndUp(); 
                    CheckResult(); 
                }
            }
        }
        else
        {
            LockAndColorLastLine(cleanRawLine, false); 
        }
    }

    void LockAndColorLastLine(string rawLine, bool isCorrect)
    {
        string colorHex = isCorrect ? "#4CAF50" : "#F44336"; 
        
        if (!string.IsNullOrEmpty(rawLine))
        {
            int lastIdx = codeInputField.text.LastIndexOf(rawLine);
            if (lastIdx >= 0)
            {
                string coloredStr = $"<color={colorHex}>{rawLine}</color>";
                string newText = codeInputField.text.Remove(lastIdx, rawLine.Length).Insert(lastIdx, coloredStr);
                
                if (!newText.EndsWith("\n")) newText += "\n";
                codeInputField.SetTextWithoutNotify(newText);
            }
        }
        else
        {
             if (!codeInputField.text.EndsWith("\n")) codeInputField.SetTextWithoutNotify(codeInputField.text + "\n");
        }
        
        protectedPrefix = codeInputField.text;
        lastValidText = protectedPrefix;
        codeInputField.caretPosition = codeInputField.text.Length;
        isExecuting = false;
    }

    // ---------------------------------------------------------
    // ระบบแสดง Popups แจ้งเตือนต่างๆ และตรวจสอบผลแพ้ชนะ
    // ---------------------------------------------------------
    void CloseEmptyInputPanel() 
    { 
        PlayButtonSound(); 
        if (emptyInputPanel) emptyInputPanel.SetActive(false);
        if (codeInputField) {
            codeInputField.interactable = true; 
            codeInputField.ActivateInputField(); 
            codeInputField.caretPosition = codeInputField.text.Length;
            codeInputField.selectionAnchorPosition = codeInputField.text.Length;
            codeInputField.selectionFocusPosition = codeInputField.text.Length;
        }
        if (clawimage) clawimage.SetActive(true); 
    }

    void CloseMovedDownErrorPanel() 
    { 
        PlayButtonSound(); 
        if (movedDownErrorPanel) movedDownErrorPanel.SetActive(false);
        if (codeInputField) {
            codeInputField.interactable = true; 
            codeInputField.ActivateInputField(); 
            codeInputField.caretPosition = codeInputField.text.Length;
            codeInputField.selectionAnchorPosition = codeInputField.text.Length;
            codeInputField.selectionFocusPosition = codeInputField.text.Length;
        }
        if (clawimage) clawimage.SetActive(true); 
    }

    void ShowError(int id, string codeType, string userCode, string suggest, int lineNum = 0)
    {
        commandHistory.Add(new CommandLog { commandText = userCode, isCorrect = false });

        if (uiAudioSource && codeErrorSound) uiAudioSource.PlayOneShot(codeErrorSound);

        ErrorDetail newErr = new ErrorDetail();
        newErr.line_number = lineNum;
        newErr.command_text = userCode;
        newErr.error_type = codeType; 
        sessionErrors.Add(newErr);

        if (errorPanel == null) return;

        errorPanel.SetActive(true);
        if (clawimage) clawimage.SetActive(false); 
        
        if (codeInputField) codeInputField.interactable = false;
        
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
            case 7: title = "InvalidParameter (ใส่พารามิเตอร์ผิดรูปแบบ)"; desc = "ในภาษาโปรแกรม พารามิเตอร์คือค่าตัวเลขที่ต้องการรับ ห้ามใส่ค่าติดลบ"; break;
            case 8: title = "MissingSemicolon (ลืม ;)"; desc = "ในภาษาโปรแกรม ต้องมีการปิดท้ายในคำสั่ง โดยใช้เครื่องหมาย ; (semicolon)"; break;
            case 9: title = "Already Gripped (สั่งคีบไปแล้ว)"; desc = "คุณได้สั่ง Grip(); เพื่อคีบไปแล้ว ไม่สามารถขยับหรือคีบซ้ำได้อีกในรอบนี้ กรุณาเริ่มเกมใหม่ครับ"; break;
        }

        if(errorTitleText) errorTitleText.text = title;
        if(errorDescText) errorDescText.text = desc;
    }

    void CloseErrorPanel() 
    { 
        PlayButtonSound(); 
        if (errorPanel) errorPanel.SetActive(false);
        if (codeInputField) {
            codeInputField.interactable = true; 
            codeInputField.ActivateInputField(); 
            codeInputField.caretPosition = codeInputField.text.Length;
            codeInputField.selectionAnchorPosition = codeInputField.text.Length;
            codeInputField.selectionFocusPosition = codeInputField.text.Length;
        }
        if (clawimage) clawimage.SetActive(true); 
    }

    void CheckResult()
    {
        // ✨ [อัปเดตใหม่] หยุดเวลาทันทีที่ระบบคีบทำงานเสร็จและกำลังประมวลผล
        isTimerRunning = false; 

        string caught = claw.GetCaughtDollName();
        int dataIndex = -1;
        for (int i = 0; i < allDollNames.Length; i++) if (allDollNames[i] == caught) dataIndex = i;

        bool isWin = (caught == currentMission);
        bool isEmptyClaw = string.IsNullOrEmpty(caught); 
        
        if (DataManager.Instance != null)
        {
            int duration = Mathf.RoundToInt(elapsedGameTime);
            DataManager.Instance.AddNewUserLog(currentMission, commandCounter, sessionErrors.Count, duration, isWin, sessionErrors);
        }

        if (isWin)
        {
            if (dropBoxTarget != null)
            {
                StartCoroutine(claw.DropWinDollSequence(dropZoneX, dropBoxTarget, () => {
                    ShowWinPanel(dataIndex);
                }));
            }
            else ShowWinPanel(dataIndex); 
        }
        else if (isEmptyClaw)
        {
            ShowEmptyClawPanel();
        }
        else 
        {
            ShowLosePanel(dataIndex);
        }
    }

    void ShowWinPanel(int dataIndex)
    {
        if (uiAudioSource && winSound) uiAudioSource.PlayOneShot(winSound);
        if (winPanel)
        {
            winPanel.SetActive(true);
            if (codeInputField) codeInputField.interactable = false; 

            if (dataIndex != -1 && winDollImage) { winDollImage.sprite = allDollSprites[dataIndex]; winDollImage.preserveAspect = true; }
            if (winDollNameText) winDollNameText.text = currentMission;
            if (winDollDetailText && dataIndex < allDollDescriptions.Length) winDollDetailText.text = allDollDescriptions[dataIndex];
        }
        if (clawimage) clawimage.SetActive(false);
    }

    void ShowLosePanel(int dataIndex)
    {
        if (uiAudioSource && loseSound) uiAudioSource.PlayOneShot(loseSound);
        if (losePanel)
        {
            losePanel.SetActive(true);
            if (codeInputField) codeInputField.interactable = false; 

            if (loseDollImage) {
                if (dataIndex != -1) { loseDollImage.sprite = allDollSprites[dataIndex]; loseDollImage.preserveAspect = true; }
                else loseDollImage.sprite = defaultLoseSprite;
            }
        }
        if (clawimage) clawimage.SetActive(false);
    }

    void ShowEmptyClawPanel()
    {
        if (uiAudioSource && loseSound) uiAudioSource.PlayOneShot(loseSound); 
        if (emptyClawPanel) emptyClawPanel.SetActive(true);
        if (codeInputField) codeInputField.interactable = false; 
        
        if (clawimage) clawimage.SetActive(false);
    }

    // ---------------------------------------------------------
    // หน้าสรุปคำสั่ง (Code Result) และจบเกม
    // ---------------------------------------------------------
    public void ShowCodeResultPanel()
    {
        PlayButtonSound();

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (emptyClawPanel) emptyClawPanel.SetActive(false);
        if (codeInputField) codeInputField.interactable = false; 

        if (totalCommandText) totalCommandText.text = commandHistory.Count.ToString();

        // ✨ [อัปเดตใหม่] โชว์เวลาที่ใช้ไปในหน้า Result
        if (resultTimeText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedGameTime / 60F);
            int seconds = Mathf.FloorToInt(elapsedGameTime - minutes * 60);
            resultTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // ✨ [อัปเดตใหม่] คำนวณเปอร์เซ็นต์ ถูก/ผิด
        int totalCmd = commandHistory.Count;
        int correctCmd = 0;
        int incorrectCmd = 0;

        foreach (var log in commandHistory)
        {
            if (log.isCorrect) correctCmd++;
            else incorrectCmd++;
        }

        if (totalCmd > 0)
        {
            float correctPct = ((float)correctCmd / totalCmd) * 100f;
            float incorrectPct = ((float)incorrectCmd / totalCmd) * 100f;

            // ปัดเศษให้เป็นจำนวนเต็ม และเติมเครื่องหมาย %
            if (correctPercentText) correctPercentText.text = Mathf.RoundToInt(correctPct).ToString() + "%";
            if (incorrectPercentText) incorrectPercentText.text = Mathf.RoundToInt(incorrectPct).ToString() + "%";
        }
        else
        {
            // กันเคสที่ผู้เล่นไม่ได้พิมพ์อะไรเลยแล้วระบบจบ
            if (correctPercentText) correctPercentText.text = "0%";
            if (incorrectPercentText) incorrectPercentText.text = "0%";
        }

        string strAll = "";
        string strCorrect = "";
        string strIncorrect = "";

        // ✨ [อัปเดตใหม่] เปลี่ยนสีข้อความแบบคลุมทั้งบรรทัด (คำสั่ง + คำแปล)
        foreach (var log in commandHistory)
        {
            if (log.isCorrect)
            {
                // เอา Tag สีมาครอบตั้งแต่ต้นจนจบประโยค
                strAll += $"<color=#4CAF50>{log.commandText}\t\tใช้ถูก</color>\n";
                strCorrect += $"<color=#4CAF50>{log.commandText}</color>\n";
            }
            else
            {
                // เอา Tag สีมาครอบตั้งแต่ต้นจนจบประโยค
                strAll += $"<color=#F44336>{log.commandText}\t\tใช้ผิด</color>\n";
                strIncorrect += $"<color=#F44336>{log.commandText}</color>\n";
            }
        }

        if (allCommandsText) allCommandsText.text = strAll;
        if (correctCommandsText) correctCommandsText.text = strCorrect;
        if (incorrectCommandsText) incorrectCommandsText.text = strIncorrect;

        if (codeResultPanel) 
        {
            codeResultPanel.transform.SetAsLastSibling(); 
            codeResultPanel.SetActive(true);
        }
    }

    public void OnClickReplay()
    {
        PlayButtonSound(); 
        startTime = Time.time;
        elapsedGameTime = 0f; // รีเซ็ตเวลาที่นับไว้
        commandCounter = 0;
        sessionErrors.Clear();
        commandHistory.Clear(); 
        
        hasGripped = false; 
        hasMovedDown = false; 

        if (timerTextUI) timerTextUI.text = "00:00"; // รีเซ็ตตัวเลขบนจอ

        RandomizeMission(); 
        if (codeResultPanel) codeResultPanel.SetActive(false);
        
        if (codeInputField) 
        {
            codeInputField.SetTextWithoutNotify("");
            protectedPrefix = ""; 
            lastValidText = "";
            codeInputField.interactable = true; 
        }
        if (startPanel) startPanel.SetActive(true);
    }
}