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
    public string[] allDollNames;        
    public Sprite[] allDollSprites;      
    public string[] allDollDescriptions; 

    [Header("Popups UI")]
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
    public AudioClip dollDropSound;     // (เพิ่มใหม่) เสียงตุ๊กตาร่วงลงพื้น (ถ้ามี)

    [Header("Drop Animation Settings")]
    public RectTransform dropBoxTarget;     
    public float dropZoneX = -350f;         
    public float spawnDropOffset = 800f; // (เพิ่มใหม่) ความสูงที่ตุ๊กตาจะซ่อนตัวก่อนร่วงลงมา

    public string currentMission;
    private bool isExecuting = false;
    private List<GameObject> spawnedDolls = new List<GameObject>();
    
    // (เพิ่มใหม่) เอาไว้จำตำแหน่งปลายทางที่ตุ๊กตาต้องตกลงมา
    private Dictionary<GameObject, Vector3> dollTargetPositions = new Dictionary<GameObject, Vector3>();

    private string protectedPrefix = ""; 
    private string lastValidText = "";   

    private float startTime;                
    private int commandCounter = 0;         
    private List<ErrorDetail> sessionErrors = new List<ErrorDetail>();

    public GameObject clawimage;

    void Start()
    {
        startTime = Time.time;
        commandCounter = 0;
        sessionErrors = new List<ErrorDetail>();

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
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
        else
        {
            EndTutorial();
        }
    }

    void GoToPage1()
    {
        PlayButtonSound(); 
        if(tutorialPage1) tutorialPage1.SetActive(true);
        if(tutorialPage2) tutorialPage2.SetActive(false);
    }

    void GoToPage2()
    {
        PlayButtonSound(); 
        if(tutorialPage1) tutorialPage1.SetActive(false);
        if(tutorialPage2) tutorialPage2.SetActive(true);
    }

    void EndTutorial()
    {
        PlayButtonSound(); 
        if (tutorialPanel) tutorialPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);
    }

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !isExecuting)
        {
            bool isTutorialActive = tutorialPanel != null && tutorialPanel.activeSelf;
            if (codeInputField.isFocused && !errorPanel.activeSelf && !isTutorialActive) 
            {
                OnRunCode();
            }
        }
        
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

        // --- (ของใหม่) เรียกใช้อนิเมชันตุ๊กตาร่วงลงมา ---
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
        dollTargetPositions.Clear(); // เคลียร์จุดเดิม

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
        
        // --- (ของใหม่) จำตำแหน่งปลายทาง แล้วจับเลื่อนขึ้นไปข้างบนเพื่อรอร่วง ---
        Vector3 targetPos = spot.position;
        dollTargetPositions.Add(newDoll, targetPos);
        newDoll.transform.position = targetPos + new Vector3(0, spawnDropOffset, 0); // เลื่อนขึ้นไป 800 pixel

        Image img = newDoll.GetComponent<Image>();
        if (img) { img.sprite = allDollSprites[index]; img.preserveAspect = true; }
        Doll d = newDoll.GetComponent<Doll>();
        if (d) d.dollName = allDollNames[index];
        newDoll.name = allDollNames[index];
        spawnedDolls.Add(newDoll);
    }

    // ==================================================
    // (ของใหม่) อนิเมชันสั่งให้ตุ๊กตาทั้งหมดร่วงลงมาพร้อมกัน
    // ==================================================
// ==================================================
    // อนิเมชันสั่งให้ตุ๊กตาร่วงลงมาแบบกระจายๆ (ไม่พร้อมกัน)
    // ==================================================
    IEnumerator AnimateDollsFalling()
    {
        // เล่นเสียงหล่นตอนเริ่มเทตุ๊กตา
        if (uiAudioSource && dollDropSound) uiAudioSource.PlayOneShot(dollDropSound);

        // วนลูปสั่งให้ตุ๊กตาแต่ละตัวแยกกันร่วง
        foreach (var kvp in dollTargetPositions)
        {
            if (kvp.Key != null)
            {
                // สุ่มเวลาหน่วง (Delay) ตั้งแต่ 0 ถึง 0.6 วินาที (ปรับให้ร่วงช้า/เร็วตามใจชอบได้)
                float randomDelay = Random.Range(0f, 0.6f); 
                
                // สั่งให้ตุ๊กตาตัวนั้นแยกไปรันอนิเมชันของตัวเอง
                StartCoroutine(DropSingleDoll(kvp.Key, kvp.Value, randomDelay));
            }
        }
        
        yield return null;
    }

    // ฟังก์ชันใหม่: จัดการตุ๊กตาแต่ละตัวให้ร่วงตามเวลาที่สุ่มได้
    IEnumerator DropSingleDoll(GameObject doll, Vector3 targetPos, float delay)
    {
        // 1. รอเวลา Delay ที่สุ่มมา
        yield return new WaitForSeconds(delay);

        float duration = 0.8f; // ความเร็วในการตกลงมาของแต่ละตัว (วินาที)
        float t = 0;
        Vector3 startPos = targetPos + new Vector3(0, spawnDropOffset, 0);

        // 2. เริ่มร่วงลงมาพร้อมเอฟเฟกต์เด้งดึ๋ง
        while (t < 1f)
        {
            // ป้องกันบัคเผื่อตุ๊กตาโดนลบทิ้งไปก่อน
            if (doll == null) yield break; 

            t += Time.deltaTime / duration;
            float ease = BounceEaseOut(t); // ดึงสมการเด้งพื้น

            doll.transform.position = Vector3.LerpUnclamped(startPos, targetPos, ease);
            yield return null;
        }

        // 3. จับให้ตรงล็อกเป๊ะๆ ตอนจบ
        if (doll != null) doll.transform.position = targetPos;
    }

    // สมการคณิตศาสตร์ ทำให้ดูเด้งสมจริงตอนกระทบพื้น (อันเดิม)
    float BounceEaseOut(float t)
    {
        if (t < 1f / 2.75f) { return 7.5625f * t * t; }
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
                LockAndPrepareNewLine(); yield break; 
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
            else cmdName = commandFull; 

            string[] validCommands = { "GoLeft", "GoRight", "MoveLeft", "MoveRight", "MoveDown", "Grip" };

            bool caseMatch = validCommands.Any(x => x.Equals(cmdName, System.StringComparison.OrdinalIgnoreCase));
            bool exactMatch = validCommands.Contains(cmdName);

            if (caseMatch && !exactMatch)
            {
                string correctCmd = validCommands.First(x => x.Equals(cmdName, System.StringComparison.OrdinalIgnoreCase));
                ShowError(2, "CaseSensitiveError", rawLine, correctCmd + "(...);", currentLineNum);
                LockAndPrepareNewLine(); yield break; 
            }

            if (!exactMatch)
            {
                  string guess = "GoLeft";
                  if (cmdName.Contains("Right")) guess = "GoRight";
                  if (cmdName.Contains("Down")) guess = "MoveDown";
                  if (cmdName.Contains("Grip")) guess = "Grip";
                  ShowError(1, "Misspell", rawLine, guess + "(...);", currentLineNum);
                  LockAndPrepareNewLine(); yield break; 
            }

            if (!hasOpenParen || !hasCloseParen)
            {
                if (!hasOpenParen) ShowError(3, "MissingParentheses", rawLine, cmdName + "();", currentLineNum);
                else ShowError(4, "UnclosedParenthesis", rawLine, cmdName + "();", currentLineNum);
                LockAndPrepareNewLine(); yield break; 
            }

            bool needNumber = (cmdName == "GoLeft" || cmdName == "GoRight" || cmdName == "MoveLeft" || cmdName == "MoveRight");
            bool noNumber = (cmdName == "MoveDown" || cmdName == "Grip");

            if (needNumber)
            {
                if (string.IsNullOrEmpty(paramContent))
                {
                    ShowError(5, "MissingParameter", rawLine, cmdName + "(1);", currentLineNum);
                    LockAndPrepareNewLine(); yield break; 
                }

                if (!int.TryParse(paramContent, out int val))
                {
                    if (paramContent.Contains(",")) ShowError(6, "ExtraParameter", rawLine, cmdName + "(1);", currentLineNum);
                    else ShowError(7, "InvalidParameter", rawLine, cmdName + "(1);", currentLineNum);
                    LockAndPrepareNewLine(); yield break; 
                }

                if (val < 0 || val > 10)
                {
                    ShowError(7, "InvalidParameter", rawLine, "ใส่เลข 1-10", currentLineNum);
                    LockAndPrepareNewLine(); yield break; 
                }

                if (uiAudioSource && codeCorrectSound) uiAudioSource.PlayOneShot(codeCorrectSound);

                if (cmdName.Contains("Left")) yield return claw.MoveHorizontal(-val);
                else yield return claw.MoveHorizontal(val);
            }
            else if (noNumber)
            {
                if (!string.IsNullOrEmpty(paramContent))
                {
                    ShowError(6, "ExtraParameter", rawLine, cmdName + "();", currentLineNum);
                    LockAndPrepareNewLine(); yield break; 
                }

                if (uiAudioSource && codeCorrectSound) uiAudioSource.PlayOneShot(codeCorrectSound);

                if (cmdName == "MoveDown") yield return claw.MoveDown();
                else if (cmdName == "Grip") { yield return claw.GripAndUp(); CheckResult(); }
            }
        }

        LockAndPrepareNewLine(); 
    }

    void LockAndPrepareNewLine()
    {
        if (!codeInputField.text.EndsWith("\n")) 
            codeInputField.SetTextWithoutNotify(codeInputField.text + "\n");
        
        protectedPrefix = codeInputField.text;
        lastValidText = protectedPrefix;
        codeInputField.caretPosition = codeInputField.text.Length;
        isExecuting = false;
    }

    void ShowError(int id, string codeType, string userCode, string suggest, int lineNum = 0)
    {
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
            case 7: title = "InvalidParameter (ใส่พารามิเตอร์ผิดรูปแบบ)"; desc = "ในภาษาโปรแกรม พารามิเตอร์คือค่าตัวเลขที่ต้องการรับ จาก Error คุณใส่จำนวนตัวเลขที่ต้องการขยับตัวคีบเกิน ซึ่งในกรณีนี้ จะใส่ค่ามากสุดได้ไม่เกิน 10"; break;
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
        else ShowLosePanel(dataIndex);
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

    public void OnClickReplay()
    {
        PlayButtonSound(); 
        startTime = Time.time;
        commandCounter = 0;
        sessionErrors.Clear();

        RandomizeMission();
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (errorPanel) errorPanel.SetActive(false);
        if (codeInputField) 
        {
            codeInputField.SetTextWithoutNotify("");
            protectedPrefix = ""; 
            lastValidText = "";
        }
        if (startPanel) startPanel.SetActive(true);
    }
}