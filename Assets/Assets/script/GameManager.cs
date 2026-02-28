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

    [Header("Empty Input Popup UI")]
    public GameObject emptyInputPanel;
    public Button closeEmptyInputButton;

    // ✨ [เพิ่มใหม่] หน้าต่างแจ้งเตือนเฉพาะกิจ: เลื่อนลงแล้วห้ามขยับซ้ายขวา
    [Header("MovedDown Error Popup UI")]
    public GameObject movedDownErrorPanel;
    public Button closeMovedDownErrorButton;

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

    private float startTime;                
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
    // ใช้สำหรับเคลียร์ค่าเก่า ปิดหน้าต่าง UI ทั้งหมด และสุ่มตุ๊กตาเตรียมเริ่มเกม
    // ---------------------------------------------------------
    void Start()
    {
        startTime = Time.time;
        commandCounter = 0;
        sessionErrors = new List<ErrorDetail>();
        commandHistory.Clear(); 
        
        hasGripped = false; // รีเซ็ตสถานะให้ยังไม่ได้คีบ
        hasMovedDown = false; // รีเซ็ตสถานะให้ยังไม่ได้เลื่อนลง

        // เปิดให้พิมพ์ช่อง InputField ได้ปกติเมื่อเริ่มเกม
        if (codeInputField) codeInputField.interactable = true; 

        // ปิดหน้าต่าง Popups ต่างๆ เพื่อความเรียบร้อยก่อนเริ่มเกม
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (emptyClawPanel) emptyClawPanel.SetActive(false);
        if (codeResultPanel) codeResultPanel.SetActive(false);
        if (errorPanel) errorPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false); 
        if (emptyInputPanel) emptyInputPanel.SetActive(false); 
        if (movedDownErrorPanel) movedDownErrorPanel.SetActive(false); 
        if (clawimage) clawimage.SetActive(false);

        // สุ่มหาว่ารอบนี้ต้องคีบตัวอะไร
        RandomizeMission(); 

        // ผูกปุ่มกดปิดหน้าต่างเข้ากับฟังก์ชัน
        if (closeErrorButton) closeErrorButton.onClick.AddListener(CloseErrorPanel);
        if (closeEmptyInputButton) closeEmptyInputButton.onClick.AddListener(CloseEmptyInputPanel);
        if (closeMovedDownErrorButton) closeMovedDownErrorButton.onClick.AddListener(CloseMovedDownErrorPanel); 

        // เปิดใช้งานช่องพิมพ์และผูกระบบป้องกันการโกงลบข้อความ
        if(codeInputField) 
        {
            codeInputField.ActivateInputField();
            codeInputField.onValueChanged.AddListener(ValidateInputPreservation);
            codeInputField.onValidateInput += ValidateCharInput;
        }

        // โชว์หน้าจอสอนเล่น
        SetupTutorial(); 
    }

    // ---------------------------------------------------------
    // ฟังก์ชัน PlayButtonSound: ใช้เรียกเล่นเสียงเวลากดปุ่มในเกม
    // ---------------------------------------------------------
    public void PlayButtonSound()
    {
        if (uiAudioSource && buttonClickSound) uiAudioSource.PlayOneShot(buttonClickSound);
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
            
            // ล็อคไม่ให้พิมพ์ตอนหน้าสอนเล่นเด้งอยู่
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
        // 1. เช็คคีย์ลัด Ctrl + B เพื่อกดกลับหน้า Home
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && 
             Input.GetKeyDown(KeyCode.B))
        {
            SceneManager.LoadScene(homeSceneName);
        }

        // 2. เช็คว่าผู้เล่นกดปุ่ม Enter เพื่อรันคำสั่งหรือไม่ 
        // (โดยต้องเช็คว่าไม่มี Popup อื่นๆ เด้งขวางอยู่ ถึงจะยอมให้รันโค้ด)
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !isExecuting)
        {
            bool isTutorialActive = tutorialPanel != null && tutorialPanel.activeSelf;
            if (codeInputField.isFocused && !errorPanel.activeSelf && (emptyInputPanel == null || !emptyInputPanel.activeSelf) && (movedDownErrorPanel == null || !movedDownErrorPanel.activeSelf) && !isTutorialActive) 
                OnRunCode();
        }
        
        // 3. ระบบบังคับไม่ให้ผู้เล่นเอาเมาส์ไปคลิกลบคำสั่งเก่าที่พิมพ์ผ่านไปแล้ว
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

    // ---------------------------------------------------------
    // หมวดป้องกันข้อความ: เช็คว่าตัวอักษรที่พิมพ์/ลบ อยู่ในโซนห้ามแก้หรือไม่
    // ---------------------------------------------------------
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
    // ทำงานเมื่อกดปุ่ม "Start Game" ให้ตุ๊กตาร่วงลงมาและเริ่มนับเวลา
    public void OnClickStartGame() 
    { 
        PlayButtonSound(); 
        if (startPanel) startPanel.SetActive(false); 
        
        // ปลดล็อคให้พิมพ์ได้ตอนเกมเริ่ม
        if (codeInputField) { codeInputField.interactable = true; codeInputField.ActivateInputField(); }

        startTime = Time.time; 
        hasGripped = false; 
        hasMovedDown = false; 
        if (clawimage) clawimage.SetActive(true);
        StartCoroutine(AnimateDollsFalling());
    }
    
    // สุ่มว่ารอบนี้ผู้เล่นต้องคีบตัวไหนเป็นเป้าหมาย
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
    
    // จัดตำแหน่งเกิดของตุ๊กตา โดยบังคับให้ตัวเป้าหมายเกิดด้านบนเสมอ เพื่อไม่ให้โดนตัวอื่นบัง
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
    
    // สร้าง Object ตุ๊กตาบนหน้าจอ และควบคุมไม่ให้รูปบิดเบี้ยวหรือทะลุกรอบความกว้าง/สูงที่เรากำหนดไว้
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

    // สมการคำนวณการร่วงให้ดูเด้งดึ๋งเหมือนตกกระทบพื้น
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

    // ฟังก์ชันตรวจสอบโค้ดบรรทัดล่าสุดที่ผู้เล่นเพิ่งกด Enter
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

        // เช็คว่าเคาะเอนเทอร์เปล่าๆ ไม่ยอมพิมพ์โค้ด ให้โชว์หน้าต่างแจ้งเตือนและล็อคการพิมพ์
        if (string.IsNullOrEmpty(cleanRawLine))
        {
            if (uiAudioSource && codeErrorSound) uiAudioSource.PlayOneShot(codeErrorSound);
            if (emptyInputPanel) emptyInputPanel.SetActive(true); 
            if (clawimage) clawimage.SetActive(false); 
            
            if (codeInputField) codeInputField.interactable = false;

            LockAndColorLastLine("", false); 
            yield break;
        }

        // เช็คว่าถ้าสั่ง Grip ไปแล้ว จะไม่ยอมให้พิมพ์ขยับต่อได้อีก
        if (hasGripped)
        {
            ShowError(9, "AlreadyGripped", cleanRawLine, "เริ่มเกมใหม่เพื่อคีบอีกครั้ง", currentLineNum);
            LockAndColorLastLine(cleanRawLine, false);
            yield break;
        }

        if (!string.IsNullOrEmpty(cleanRawLine))
        {
            // ตรวจสอบเซมิโคลอน ปิดท้ายคำสั่ง
            if (!cleanRawLine.EndsWith(";")) 
            {
                ShowError(8, "MissingSemicolon", cleanRawLine, cleanRawLine + ";", currentLineNum);
                LockAndColorLastLine(cleanRawLine, false); yield break; 
            }

            // แยกวิเคราะห์ชื่อคำสั่ง กับ ตัวเลขในวงเล็บ (พารามิเตอร์)
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

            // ตรวจสอบความถูกต้องของคำสั่ง
            string[] validCommands = { "GoLeft", "GoRight", "MoveLeft", "MoveRight", "MoveDown", "Grip" };

            bool caseMatch = validCommands.Any(x => x.Equals(cmdName, System.StringComparison.OrdinalIgnoreCase));
            bool exactMatch = validCommands.Contains(cmdName);

            // พิมพ์ตัวพิมพ์เล็ก/ใหญ่ผิด (Case Sensitive)
            if (caseMatch && !exactMatch)
            {
                string correctCmd = validCommands.First(x => x.Equals(cmdName, System.StringComparison.OrdinalIgnoreCase));
                ShowError(2, "CaseSensitiveError", cleanRawLine, correctCmd + "(...);", currentLineNum);
                LockAndColorLastLine(cleanRawLine, false); yield break; 
            }

            // สะกดผิด ไม่ตรงกับคำสั่งไหนเลย
            if (!exactMatch)
            {
                  string guess = "GoLeft";
                  if (cmdName.Contains("Right")) guess = "GoRight";
                  if (cmdName.Contains("Down")) guess = "MoveDown";
                  if (cmdName.Contains("Grip")) guess = "Grip";
                  ShowError(1, "Misspell", cleanRawLine, guess + "(...);", currentLineNum);
                  LockAndColorLastLine(cleanRawLine, false); yield break; 
            }

            // ลืมใส่วงเล็บ หรือ ลืมปิดวงเล็บ
            if (!hasOpenParen || !hasCloseParen)
            {
                if (!hasOpenParen) ShowError(3, "MissingParentheses", cleanRawLine, cmdName + "();", currentLineNum);
                else ShowError(4, "UnclosedParenthesis", cleanRawLine, cmdName + "();", currentLineNum);
                LockAndColorLastLine(cleanRawLine, false); yield break; 
            }

            bool needNumber = (cmdName == "GoLeft" || cmdName == "GoRight" || cmdName == "MoveLeft" || cmdName == "MoveRight");
            bool noNumber = (cmdName == "MoveDown" || cmdName == "Grip");

            // เช็คว่าถ้าสั่ง MoveDown ไปแล้ว ห้ามใช้คำสั่งขยับซ้ายขวา (โชว์ Popup แบบแยก)
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
                
                if (codeInputField) codeInputField.interactable = false; // ล็อคการพิมพ์

                LockAndColorLastLine(cleanRawLine, false);
                yield break;
            }

            // กรณีคำสั่งที่ต้องระบุระยะทางในวงเล็บ (ขยับซ้าย/ขวา)
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

                // พิมพ์ถูก สั่งการที่คีบให้ขยับแนวนอน
                commandHistory.Add(new CommandLog { commandText = cleanRawLine, isCorrect = true });
                if (uiAudioSource && codeCorrectSound) uiAudioSource.PlayOneShot(codeCorrectSound);

                LockAndColorLastLine(cleanRawLine, true); 
                if (cmdName.Contains("Left")) yield return claw.MoveHorizontal(-val);
                else yield return claw.MoveHorizontal(val);
            }
            // กรณีคำสั่งที่ไม่ต้องระบุตัวเลข (ลงล่าง, คีบ)
            else if (noNumber)
            {
                if (!string.IsNullOrEmpty(paramContent))
                {
                    ShowError(6, "ExtraParameter", cleanRawLine, cmdName + "();", currentLineNum);
                    LockAndColorLastLine(cleanRawLine, false); yield break; 
                }

                // พิมพ์ถูก สั่งการที่คีบให้ขยับลงล่าง หรือคีบ
                commandHistory.Add(new CommandLog { commandText = cleanRawLine, isCorrect = true });
                if (uiAudioSource && codeCorrectSound) uiAudioSource.PlayOneShot(codeCorrectSound);

                LockAndColorLastLine(cleanRawLine, true); 
                
                if (cmdName == "MoveDown") 
                {
                    hasMovedDown = true; // ล็อคทันทีที่สั่ง MoveDown สำเร็จ ไม่ให้เลื่อนซ้ายขวาได้อีก
                    yield return claw.MoveDown();
                }
                else if (cmdName == "Grip") 
                { 
                    hasGripped = true; // ล็อคทันทีที่สั่ง Grip สำเร็จ ป้องกันการขยับซ้ำ
                    yield return claw.GripAndUp(); 
                    CheckResult(); // ตรวจผลลัพธ์ว่าคีบได้หรือไม่ได้
                }
            }
        }
        else
        {
            LockAndColorLastLine(cleanRawLine, false); 
        }
    }

    // ฟังก์ชันสำหรับใส่สีข้อความ (เขียว=ถูก, แดง=ผิด) และล็อคให้เป็นข้อความในอดีต ห้ามแก้ไข
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

    // ฟังก์ชันสำหรับปิดหน้าต่าง "ไม่ได้พิมพ์โค้ด" แล้วปลดล็อคให้พิมพ์ต่อ
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

    // ฟังก์ชันสำหรับปิดหน้าต่างเฉพาะกิจ "เลื่อนลงแล้วห้ามขยับซ้ายขวา" แล้วปลดล็อคให้พิมพ์ต่อ
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

    // ฟังก์ชันเรียกหน้าต่าง Error Popup ขึ้นมาโชว์ พร้อมแสดงคำอธิบายสาเหตุ และล็อคการพิมพ์
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

    // ปิดหน้าต่าง Error เพื่อกลับไปแก้ตัวพิมพ์โค้ดใหม่
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

    // ประมวลผลหลังจากคีบ (Grip) ว่าคีบโดนตัวไหน และตรงเป้าหมายหรือไม่
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

        // กรณีที่คีบโดนเป้าหมาย (ชนะ)
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
        // กรณีคีบไม่โดนอะไรเลย (ว่าว)
        else if (isEmptyClaw)
        {
            ShowEmptyClawPanel();
        }
        // กรณีคีบโดนตัวอื่น (แพ้)
        else 
        {
            ShowLosePanel(dataIndex);
        }
    }

    // ฟังก์ชันโชว์หน้าต่างชนะ คีบถูกตัว
    void ShowWinPanel(int dataIndex)
    {
        if (uiAudioSource && winSound) uiAudioSource.PlayOneShot(winSound);
        if (winPanel)
        {
            winPanel.SetActive(true);
            // ล็อคไม่ให้แอบพิมพ์โค้ดต่อตอนจบเกม
            if (codeInputField) codeInputField.interactable = false; 

            if (dataIndex != -1 && winDollImage) { winDollImage.sprite = allDollSprites[dataIndex]; winDollImage.preserveAspect = true; }
            if (winDollNameText) winDollNameText.text = currentMission;
            if (winDollDetailText && dataIndex < allDollDescriptions.Length) winDollDetailText.text = allDollDescriptions[dataIndex];
        }
        if (clawimage) clawimage.SetActive(false);
    }

    // ฟังก์ชันโชว์หน้าต่างแพ้ คีบผิดตัว
    void ShowLosePanel(int dataIndex)
    {
        if (uiAudioSource && loseSound) uiAudioSource.PlayOneShot(loseSound);
        if (losePanel)
        {
            losePanel.SetActive(true);
            // ล็อคไม่ให้แอบพิมพ์โค้ดต่อตอนจบเกม
            if (codeInputField) codeInputField.interactable = false; 

            if (loseDollImage) {
                if (dataIndex != -1) { loseDollImage.sprite = allDollSprites[dataIndex]; loseDollImage.preserveAspect = true; }
                else loseDollImage.sprite = defaultLoseSprite;
            }
        }
        if (clawimage) clawimage.SetActive(false);
    }

    // ฟังก์ชันโชว์หน้าต่างคีบไม่ได้อะไรเลย
    void ShowEmptyClawPanel()
    {
        if (uiAudioSource && loseSound) uiAudioSource.PlayOneShot(loseSound); 
        if (emptyClawPanel) emptyClawPanel.SetActive(true);
        // ล็อคไม่ให้แอบพิมพ์โค้ดต่อตอนจบเกม
        if (codeInputField) codeInputField.interactable = false; 
        
        if (clawimage) clawimage.SetActive(false);
    }

    // ---------------------------------------------------------
    // หน้าสรุปคำสั่ง (Code Result) และจบเกม
    // ---------------------------------------------------------
    // รวบรวมคำสั่งที่ผู้เล่นพิมพ์ทั้งหมดมาโชว์ในหน้า Result ตอนจบด่าน
    public void ShowCodeResultPanel()
    {
        PlayButtonSound();

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (emptyClawPanel) emptyClawPanel.SetActive(false);

        // กันเหนียว ล็อคไม่ให้พิมพ์ต่อเด็ดขาดในหน้าสรุปผล
        if (codeInputField) codeInputField.interactable = false; 

        if (totalCommandText) totalCommandText.text = commandHistory.Count.ToString();

        string strAll = "";
        string strCorrect = "";
        string strIncorrect = "";

        // วนลูปเพื่อแยกคำสั่งที่พิมพ์ถูก ออกจากคำสั่งที่พิมพ์ผิด
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
            // ดึงหน้าต่าง Result ให้ขึ้นมาโชว์ทับสิ่งอื่นทั้งหมด
            codeResultPanel.transform.SetAsLastSibling(); 
            codeResultPanel.SetActive(true);
        }
    }

    // ปุ่ม Replay เพื่อล้างค่าทั้งหมดแล้วเริ่มสุ่มภารกิจใหม่
    public void OnClickReplay()
    {
        PlayButtonSound(); 
        startTime = Time.time;
        commandCounter = 0;
        sessionErrors.Clear();
        commandHistory.Clear(); 
        
        hasGripped = false; // ปลดล็อคการคีบ
        hasMovedDown = false; // ปลดล็อคการเลื่อนลง

        RandomizeMission(); // สุ่มเป้าหมายใหม่
        if (codeResultPanel) codeResultPanel.SetActive(false);
        
        // เคลียร์ช่องพิมพ์ให้ว่างเปล่า
        if (codeInputField) 
        {
            codeInputField.SetTextWithoutNotify("");
            protectedPrefix = ""; 
            lastValidText = "";
            codeInputField.interactable = true; // เปิดให้กลับมาพิมพ์ได้เมื่อเริ่มเกมใหม่
        }
        if (startPanel) startPanel.SetActive(true);
    }
}