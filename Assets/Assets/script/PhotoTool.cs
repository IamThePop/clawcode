using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class UploadResponse
{
    public bool success;
    public string message;
    public string file_name; 
    public string url;
}

public class PhotoTool : MonoBehaviour
{
    public qrCode qrCode;
    public GameObject DownloadPopup;
    public string API;

    // Rect region to take screenshot
    public int startX;
    public int startY;
    public int width;
    public int height;

    [SerializeField] public Texture2D tex2d_photo;
    Rect photoRegion;

    public delegate void PhotoTaken(Texture2D _photoTaken);
    public static event PhotoTaken OnPhotoTaken;

    [Header("setting")]
    public bool useCanvas;
    public Image uiImage;
    public Canvas canvas;
    public bool saveImage;

    void Start()
    {
        if (useCanvas)
        {
            RectTransform rect = uiImage.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            Vector3 bl = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
            Vector3 tr = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);

            int startX = Mathf.RoundToInt(bl.x);
            int startY = Mathf.RoundToInt(bl.y);
            int width = Mathf.RoundToInt(tr.x - bl.x);
            int height = Mathf.RoundToInt(tr.y - bl.y);

            startY = Screen.height - startY - height;

            Debug.Log($"UI to Screen: X:{startX} Y:{startY} W:{width} H:{height}");
            Init(startX, startY, width, height);
        }
    }

    public void Init()
    {
        tex2d_photo = new Texture2D(width, height, TextureFormat.RGB24, false);
        photoRegion = new Rect(startX, Screen.height - height - startY, width, height);
        Debug.LogWarning("PhotoUtility init with width:" + width);
    }

    public void Init(int _startX, int _startY, int _width, int _height)
    {
        startX = _startX;
        startY = _startY;
        width = _width;
        height = _height;

        photoRegion = new Rect(startX, Screen.height - height - startY, width, height);
        Debug.LogWarning("PhotoUtility init with width:" + _width);
    }

    public void TakePhoto()
    {
        StartCoroutine(E_TakePhoto());
    }

    IEnumerator E_TakePhoto()
    {
        #region Hide Debug Region If Needed
        if (debugOnGUI)
        {
            debugOnGUI = false;
            shouldTurnOnDebugOnGUI = true;
        }
        else
        {
            shouldTurnOnDebugOnGUI = false;
        }
        #endregion

        // รอจนกว่าจะสิ้นสุดการเรนเดอร์เฟรมนี้ (สำคัญมากสำหรับการแคปจอ)
        yield return new WaitForEndOfFrame();

        // สร้าง Texture ใหม่ (กันเหนียวเคลียร์ของเก่าทิ้งถ้ามี เพื่อไม่ให้เมมโมรี่รั่ว)
        if (tex2d_photo != null)
        {
            Destroy(tex2d_photo); 
        }

        tex2d_photo = new Texture2D(width, height, TextureFormat.RGB24, false);
        photoRegion = new Rect(startX, Screen.height - height - startY, width, height);

        // ดึงพิกเซลจากหน้าจอมาใส่รูป
        tex2d_photo.ReadPixels(photoRegion, 0, 0);
        tex2d_photo.Apply();

        if (OnPhotoTaken != null)
            OnPhotoTaken(tex2d_photo);

        Debug.Log("✅ Photo taken successfully!");

        if (saveImage)
        {
            byte[] bytes = tex2d_photo.EncodeToPNG();
            string filename = "photo_" + System.Guid.NewGuid().ToString() + ".png";
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log("💾 Saved: " + path);
        }

        #region Turn On Debug Region If Needed
        if (shouldTurnOnDebugOnGUI)
        {
            debugOnGUI = true;
        }
        #endregion
    }

    #region Debug Region to take photo
    public bool debugOnGUI;
    bool shouldTurnOnDebugOnGUI;
    public Texture debugRegionTex;
    public Color debugRegionColor = Color.white;

    void OnGUI()
    {
#if UNITY_EDITOR 
        // Leave bool debugOnGUI as is
#elif UNITY_STANDALONE
        debugOnGUI = false;
#endif
        if (debugOnGUI)
        {
            photoRegion = new Rect(startX, startY, width, height);
            GUI.color = debugRegionColor;
            GUI.DrawTexture(photoRegion, debugRegionTex);
        }
    }
    #endregion

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            TakePhoto();
        }
    }

    // ฟังก์ชันนี้ผูกกับปุ่ม UI
    public void UploadImage()
    {
        StartCoroutine(TakephotoWait());
    }

    IEnumerator TakephotoWait()
    {
        // 🛑 [จุดที่แก้ไข] เปลี่ยนมารอให้ E_TakePhoto ทำงานเสร็จ 100% แทนการใช้ WaitForSeconds(1f)
        yield return StartCoroutine(E_TakePhoto());

        // พอมั่นใจว่ารูปถ่ายเสร็จแล้วค่อยสั่งอัปโหลด
        if (tex2d_photo != null)
        {
            UploadImageToServer(tex2d_photo);
        }
        else
        {
            Debug.LogError("❌ รูปภาพเป็นค่าว่าง ไม่สามารถอัปโหลดได้!");
        }
    }

    public void UploadImageToServer(Texture2D texture)
    {
        StartCoroutine(UploadRoutine(texture));
    }

    IEnumerator UploadRoutine(Texture2D texture)
    {
        Debug.Log("📤 เริ่มอัปโหลดรูปภาพ...");
        byte[] imageBytes = texture.EncodeToPNG();
        WWWForm form = new WWWForm();
        form.AddBinaryData("image_file", imageBytes, "screenshot.png", "image/png");

        using (UnityWebRequest www = UnityWebRequest.Post(API, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResult = www.downloadHandler.text;
                UploadResponse responseData = JsonUtility.FromJson<UploadResponse>(jsonResult);

                if (responseData.success)
                {
                    string extractedFileName = responseData.file_name;
                    
                    if(DownloadPopup != null)
                    {
                        DownloadPopup.SetActive(true);
                        DownloadPopup.transform.SetAsLastSibling();
                    }

                    Debug.Log("🎯 อัปโหลดสำเร็จ! ชื่อไฟล์ที่ได้คือ: " + extractedFileName);
                    
                    if(qrCode != null)
                    {
                        qrCode.GenerateQR(extractedFileName);
                    }
                }
            }
            else
            {
                Debug.LogError("❌ Upload Failed: " + www.error);
            }
        }
    }
}