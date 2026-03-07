using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;

public class qrCode : MonoBehaviour
{
    public RawImage displayImage; // ตัวแสดงผลบน UI
    public GameObject loading;    // หน้าจอโหลด

    // 🛑 เปลี่ยนเป็น private เพื่อป้องกันไม่ให้ Unity Inspector ดึงค่า localhost เก่ามาทับ
    private string baseUrl = "https://clawcode.iappsilpakorn.com/qr?image=";

    // ฟังก์ชันนี้ให้ส่งแค่ "ชื่อไฟล์" เข้ามา (เช่น 1772857109_Vector 6.png)
    public void GenerateQR(string fileName)
    {
        if (loading != null) loading.SetActive(true);

        // นำ URL เซิร์ฟเวอร์จริง มาต่อกับ ชื่อไฟล์
        string fullUrl = baseUrl + fileName;

        // 🔍 ปริ้นเช็คใน Console เพื่อความชัวร์ ว่าได้ URL ที่ถูกต้อง
        Debug.Log("🎯 สแกน QR Code แล้วจะไปที่: " + fullUrl);

        // สร้างภาพ QR Code
        Texture2D qrTexture = GenerateQRCode(fullUrl, 512, 512);
        
        if (displayImage != null)
        {
            displayImage.texture = qrTexture;
        }

        if (loading != null) loading.SetActive(false);
    }

    private Texture2D GenerateQRCode(string text, int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width,
                Margin = 1 // ขอบขาวรอบๆ QR
            }
        };

        // สร้าง Color32 array จากข้อความ
        var colorMatrix = writer.Write(text);

        // สร้าง Texture2D เพื่อแสดงผล
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels32(colorMatrix);
        texture.Apply();

        return texture;
    }
}