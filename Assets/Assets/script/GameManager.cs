using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq; 
using UnityEngine.SceneManagement; // เพิ่มอันนี้เพื่อใช้คำสั่งเปลี่ยน Scene

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
    public string homeSceneName = "Home"; // ชื่อ Scene หน้า Home ของคุณ (แก้ให้ตรงด้วยนะครับ)

    public string currentMission;
    private bool isExecuting = false;
    private List<GameObject> spawnedDolls = new List<GameObject>();
    private Dictionary<GameObject, Vector3> dollTargetPositions = new Dictionary<GameObject, Vector3>();

    private string protectedPrefix = ""; 
    private string lastValidText = "";   

    private float startTime;                
    private int commandCounter = 0;         
    private List<ErrorDetail> sessionErrors = new List<ErrorDetail>();

    private struct CommandLog
    {
        public string commandText;
        public bool isCorrect;
    }
    private List<CommandLog> commandHistory = new List<CommandLog>();

    public GameObject clawimage;

    void Start()
    {
        startTime = Time.time;
        commandCounter = 0;
        sessionErrors = new List<ErrorDetail>();
        commandHistory.Clear(); 

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (emptyClawPanel) emptyClawPanel.SetActive(false);
        if (codeResultPanel) codeResultPanel.SetActive(false);
        if (errorPanel) errorPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false); 

        if (clawimage) clawimage.SetActive(false);

        RandomizeMission();

        if (closeErrorButton) closeErrorButton.onClick.AddListener(CloseErrorPanel);

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
        }
        else EndTutorial();
    }

    void GoToPage1() { PlayButtonSound(); if(tutorialPage1) tutorialPage1.SetActive(true); if(tutorialPage2) tutorialPage2.SetActive(false); }
    void GoToPage2() { PlayButtonSound(); if(tutorialPage1) tutorialPage1.SetActive(false); if(tutorialPage2) tutorialPage2.SetActive(true); }
    void EndTutorial() { PlayButtonSound(); if (tutorialPanel) tutorialPanel.SetActive(false); if (startPanel) startPanel.SetActive(true); }

    void Update()
    {
        // ==========================================
        // 3. ระบบคีย์ลัด กด Command/Ctrl + B กลับหน้า Home
        // ==========================================
        if ((Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) || 
             Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && 
             Input.GetKeyDown(KeyCode.B))
        {
            SceneManager.LoadScene(homeSceneName);
        }

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !isExecuting)
        {
            bool isTutorialActive = tutorialPanel != null && tutorialPanel.activeSelf;
            if (codeInputField.isFocused && !errorPanel.activeSelf && !isTutorialActive) 
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
        // ป้องกันบักการเว้นวรรคและ Tag สี
        if (currentText.Length < protectedPrefix.Length || !currentText.StartsWith(protectedPrefix))
        {
            codeInputField.SetTextWithoutNotify(lastValidText);
            codeInputField.caretPosition = lastValidText.Length;
        }
        else lastValidText = currentText;
    }

    public void OnClickStartGame() 
    { 
        PlayButtonSound(); 
        if (startPanel) startPanel.SetActive(false); 
        startTime = Time.time; 
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
            
            // คืนค่าขนาดรูปต้นฉบับก่อนคำนวณ
            img.SetNativeSize(); 
            
            RectTransform rect = img.GetComponent<RectTransform>();
            
            // --- ตั้งค่าขอบเขตสูงสุดที่ต้องการ ---
            float maxWidth = 190f;  // ความกว้างสูงสุดที่ยอมให้ขยายได้
            float maxHeight = 210f; // ความสูงสูงสุดที่ยอมให้ขยายได้ (ตั้งให้มากกว่ากว้างนิดนึง แครอทจะได้ดูไม่หดเกินไป)
            
            // คำนวณอัตราส่วนว่าต้องย่อ/ขยายเท่าไหร่ถึงจะพอดี
            float widthRatio = maxWidth / rect.sizeDelta.x;
            float heightRatio = maxHeight / rect.sizeDelta.y;
            
            // เลือกใช้สัดส่วนที่ "น้อยกว่า" เพื่อรับประกันว่ารูปจะไม่ทะลุกรอบทั้งแกน X และ Y
            float finalRatio = Mathf.Min(widthRatio, heightRatio);
            
            // บังคับใช้ขนาดใหม่
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * finalRatio, rect.sizeDelta.y * finalRatio);
        }

        Doll d = newDoll.GetComponent<Doll>();
        if (d) d.dollName = allDollNames[index];
        newDoll.name = allDollNames[index];
        spawnedDolls.Add(newDoll);
    }    IEnumerator AnimateDollsFalling()
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

    public void OnRunCode()
    {
        if (isExecuting) return;
        commandCounter++; 
        StartCoroutine(ExecuteLastLineOnly());
    }

    IEnumerator ExecuteLastLineOnly()
    {
        isExecuting = true;
        
        // กรองเอาบรรทัดสุดท้ายแบบไม่เอาช่องว่างมาวิเคราะห์
        string[] lines = codeInputField.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) { isExecuting = false; yield break; }

        string rawLine = lines[lines.Length - 1].Trim();
        int currentLineNum = lines.Length;

        if (string.IsNullOrEmpty(rawLine) && lines.Length > 1) 
        {
            rawLine = lines[lines.Length - 2].Trim();
            currentLineNum--;
        }

        // ลบ Tags สีเก่า (ถ้าผู้เล่นแอบพิมพ์เข้ามา) เพื่อเอามาวิเคราะห์
        string cleanRawLine = rawLine;

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

                LockAndColorLastLine(cleanRawLine, true); // พิมพ์ถูกเปลี่ยนเป็นสีเขียว
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

                LockAndColorLastLine(cleanRawLine, true); // พิมพ์ถูกเปลี่ยนเป็นสีเขียว
                if (cmdName == "MoveDown") yield return claw.MoveDown();
                else if (cmdName == "Grip") { yield return claw.GripAndUp(); CheckResult(); }
            }
        }
        else
        {
            LockAndColorLastLine(cleanRawLine, false); 
        }
    }

    // ==========================================
    // 2. ฟังก์ชันระบายสีคำสั่งที่เพิ่งพิมพ์เสร็จ (สีเขียว/แดง)
    // ==========================================
    void LockAndColorLastLine(string rawLine, bool isCorrect)
    {
        // เลือกสี (เขียวสำหรับถูก, แดงสำหรับผิด)
        string colorHex = isCorrect ? "#4CAF50" : "#F44336"; 
        
        // ถ้ามีความยาวคำสั่งให้ทำการหาและแทนที่ด้วย Tag สี
        if (!string.IsNullOrEmpty(rawLine))
        {
            int lastIdx = codeInputField.text.LastIndexOf(rawLine);
            if (lastIdx >= 0)
            {
                string coloredStr = $"<color={colorHex}>{rawLine}</color>";
                string newText = codeInputField.text.Remove(lastIdx, rawLine.Length).Insert(lastIdx, coloredStr);
                
                // ขึ้นบรรทัดใหม่เตรียมให้พิมพ์คำสั่งถัดไป
                if (!newText.EndsWith("\n")) newText += "\n";
                codeInputField.SetTextWithoutNotify(newText);
            }
        }
        else
        {
             if (!codeInputField.text.EndsWith("\n")) codeInputField.SetTextWithoutNotify(codeInputField.text + "\n");
        }
        
        // ล็อคข้อความกลายเป็นอดีต
        protectedPrefix = codeInputField.text;
        lastValidText = protectedPrefix;
        codeInputField.caretPosition = codeInputField.text.Length;
        isExecuting = false;
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
        }

        if(errorTitleText) errorTitleText.text = title;
        if(errorDescText) errorDescText.text = desc;
    }

    void CloseErrorPanel() 
    { 
        PlayButtonSound(); 
        if (errorPanel) errorPanel.SetActive(false);
        if (codeInputField) {
            codeInputField.ActivateInputField(); 
            codeInputField.caretPosition = codeInputField.text.Length;
            codeInputField.selectionAnchorPosition = codeInputField.text.Length;
            codeInputField.selectionFocusPosition = codeInputField.text.Length;
        }
        if (clawimage) clawimage.SetActive(true); 
    }

    void CheckResult()
    {
        string caught = claw.GetCaughtDollName();
        int dataIndex = -1;
        for (int i = 0; i < allDollNames.Length; i++) if (allDollNames[i] == caught) dataIndex = i;

        bool isWin = (caught == currentMission);
        bool isEmptyClaw = string.IsNullOrEmpty(caught); 
        
        if (DataManager.Instance != null)
        {
            int duration = Mathf.RoundToInt(Time.time - startTime);
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
        if (clawimage) clawimage.SetActive(false);
    }

  public void ShowCodeResultPanel()
    {
        PlayButtonSound();

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (emptyClawPanel) emptyClawPanel.SetActive(false);

        if (totalCommandText) totalCommandText.text = commandHistory.Count.ToString();

        string strAll = "";
        string strCorrect = "";
        string strIncorrect = "";

        foreach (var log in commandHistory)
        {
            if (log.isCorrect)
            {
                strAll += $"{log.commandText}\t\t<color=#4CAF50>ใช้ถูก</color>\n";
                strCorrect += $"{log.commandText}\n";
            }
            else
            {
                strAll += $"{log.commandText}\t\t<color=#F44336>ใช้ผิด</color>\n";
                strIncorrect += $"{log.commandText}\n";
            }
        }

        if (allCommandsText) allCommandsText.text = strAll;
        if (correctCommandsText) correctCommandsText.text = strCorrect;
        if (incorrectCommandsText) incorrectCommandsText.text = strIncorrect;

        if (codeResultPanel) 
        {
            // --- เพิ่มคำสั่งนี้เข้าไป 1 บรรทัดครับ ---
            codeResultPanel.transform.SetAsLastSibling(); 
            // ------------------------------------
            
            codeResultPanel.SetActive(true);
        }
    }

    public void OnClickReplay()
    {
        PlayButtonSound(); 
        startTime = Time.time;
        commandCounter = 0;
        sessionErrors.Clear();
        commandHistory.Clear(); 

        RandomizeMission();
        if (codeResultPanel) codeResultPanel.SetActive(false);
        
        if (codeInputField) 
        {
            codeInputField.SetTextWithoutNotify("");
            protectedPrefix = ""; 
            lastValidText = "";
        }
        if (startPanel) startPanel.SetActive(true);
    }
}
