using System.Collections;
using System; // เพิ่มอันนี้เพื่อใช้ Action callback
using UnityEngine;
using UnityEngine.UI;

public class ClawController : MonoBehaviour
{
    [Header("UI Settings")]
    public RectTransform clawRect;

    [Header("Movement (Pixel Units)")]
    public float moveSpeed = 500f; 
    public float stepSize = 100f;  

    [Header("Boundaries")]
    public float leftLimit = -420f;
    public float rightLimit = 420f;
    public float topY = 400f;    
    public float bottomY = -400f; 

    [Header("Grip Settings")]
    public float gripDistance = 120f; 

    [Header("Audio Settings (เสียงกลไก)")]
    public AudioSource clawAudioSource; 
    public AudioClip moveSound;         
    public AudioClip gripSound;         
    
    private Transform caughtDoll = null;

    void Awake()
    {
        if (clawRect == null) clawRect = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (clawRect != null) clawRect.anchoredPosition = new Vector2(0, topY);
    }

    public IEnumerator MoveHorizontal(float steps)
    {
        if (clawRect == null) yield break;

        float targetX = clawRect.anchoredPosition.x + (steps * stepSize);
        targetX = Mathf.Clamp(targetX, leftLimit, rightLimit);

        if (clawAudioSource && moveSound) 
        {
            clawAudioSource.clip = moveSound;
            clawAudioSource.loop = true; 
            clawAudioSource.Play();
        }

        while (Mathf.Abs(clawRect.anchoredPosition.x - targetX) > 1f)
        {
            float newX = Mathf.MoveTowards(clawRect.anchoredPosition.x, targetX, moveSpeed * Time.deltaTime);
            clawRect.anchoredPosition = new Vector2(newX, clawRect.anchoredPosition.y);
            yield return null;
        }
        
        if (clawAudioSource) clawAudioSource.Stop();
        clawRect.anchoredPosition = new Vector2(targetX, clawRect.anchoredPosition.y);
    }

    public IEnumerator MoveDown()
    {
        if (clawRect == null) yield break;
        
        float fixedX = clawRect.anchoredPosition.x;
        
        if (clawAudioSource && moveSound) 
        {
            clawAudioSource.clip = moveSound;
            clawAudioSource.loop = true;
            clawAudioSource.Play();
        }

        while (Mathf.Abs(clawRect.anchoredPosition.y - bottomY) > 1f)
        {
            float newY = Mathf.MoveTowards(clawRect.anchoredPosition.y, bottomY, moveSpeed * Time.deltaTime);
            clawRect.anchoredPosition = new Vector2(fixedX, newY); 
            yield return null;
        }
        
        if (clawAudioSource) clawAudioSource.Stop();
        clawRect.anchoredPosition = new Vector2(fixedX, bottomY);
    }

    public IEnumerator GripAndUp()
    {
        if (clawAudioSource && gripSound) clawAudioSource.PlayOneShot(gripSound);

        CheckForDoll();
        yield return new WaitForSeconds(0.5f); 

        float fixedX = clawRect.anchoredPosition.x;

        if (clawAudioSource && moveSound) 
        {
            clawAudioSource.clip = moveSound;
            clawAudioSource.loop = true;
            clawAudioSource.Play();
        }

        while (Mathf.Abs(clawRect.anchoredPosition.y - topY) > 1f)
        {
            float newY = Mathf.MoveTowards(clawRect.anchoredPosition.y, topY, moveSpeed * Time.deltaTime);
            clawRect.anchoredPosition = new Vector2(fixedX, newY); 
            yield return null;
        }
        
        if (clawAudioSource) clawAudioSource.Stop();
        clawRect.anchoredPosition = new Vector2(fixedX, topY);
    }

    void CheckForDoll()
    {
        Doll[] allDolls = FindObjectsOfType<Doll>();
        float closestDist = Mathf.Infinity;
        Doll target = null;

        foreach (Doll d in allDolls)
        {
            RectTransform dollRect = d.GetComponent<RectTransform>();
            if (dollRect == null) continue;

            float dist = Vector2.Distance(clawRect.anchoredPosition, dollRect.anchoredPosition);
            
            if (dist < gripDistance && dist < closestDist)
            {
                closestDist = dist;
                target = d;
            }
        }

        if (target != null)
        {
            caughtDoll = target.transform;
            caughtDoll.SetParent(this.transform); 
            caughtDoll.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120); 
        }
    }

    public void ReleaseDoll()
    {
        if (caughtDoll != null)
        {
            Destroy(caughtDoll.gameObject); 
            caughtDoll = null;
        }
    }

    public string GetCaughtDollName()
    {
        if (caughtDoll != null)
        {
            Doll d = caughtDoll.GetComponent<Doll>();
            if (d != null) return d.dollName;
        }
        return "";
    }

    // ==========================================
    // ของใหม่: แอนิเมชันตอนคีบถูกตัว แล้วเอาไปหย่อนลงกล่อง
    // ==========================================
    public IEnumerator DropWinDollSequence(float dropZoneX, RectTransform targetBox, Action onComplete)
    {
        // 1. เลื่อนตัวคีบไปทางซ้าย (จุดที่จะปล่อย)
        if (clawAudioSource && moveSound) 
        {
            clawAudioSource.clip = moveSound;
            clawAudioSource.loop = true;
            clawAudioSource.Play();
        }

        while (Mathf.Abs(clawRect.anchoredPosition.x - dropZoneX) > 1f)
        {
            float newX = Mathf.MoveTowards(clawRect.anchoredPosition.x, dropZoneX, moveSpeed * Time.deltaTime);
            clawRect.anchoredPosition = new Vector2(newX, clawRect.anchoredPosition.y);
            yield return null;
        }

        if (clawAudioSource) clawAudioSource.Stop();
        
        // เสียงอ้ากรงเล็บ (ใช้เสียง grip ซ้ำได้)
        if (clawAudioSource && gripSound) clawAudioSource.PlayOneShot(gripSound);

        if (caughtDoll != null)
        {
            // 2. ปล่อยตุ๊กตาให้หลุดจากตัวคีบ
            Transform droppingDoll = caughtDoll;
            caughtDoll = null;
            
            // ย้ายตุ๊กตาไปอยู่ชั้นนอกสุดของ UI จะได้ไม่โดนบัง
            droppingDoll.SetParent(targetBox.parent); 
            droppingDoll.SetAsLastSibling(); // ให้อยู่หน้าสุด

            RectTransform dollRect = droppingDoll.GetComponent<RectTransform>();
            Vector3 startPos = dollRect.position;
            Vector3 endPos = targetBox.position;

            // 3. อนิเมชันเด้งเข้ากล่อง + ขยายขนาดนิดนึงแบบในรูป
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 2f; // ความเร็วตอนเด้งเข้ากล่อง (ปรับได้)
                
                // เลื่อนตำแหน่ง
                dollRect.position = Vector3.Lerp(startPos, endPos, t);
                
                // ขยายขนาด (เด้งขึ้นมาใหญ่แล้วหดลงไปในกล่อง)
                float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * 1.5f; // ทำให้มันเด้งใหญ่ขึ้นกลางทาง
                dollRect.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);

                yield return null;
            }

            // จบอนิเมชัน เซ็ตขนาดให้พอดี
            dollRect.localScale = Vector3.one * 1.2f; // ใหญ่กว่าปกตินิดนึงตอนอยู่ในกล่อง
        }

        yield return new WaitForSeconds(0.5f); // รอแปปนึงก่อนขึ้นป้าย Win
        onComplete?.Invoke(); // สั่งให้ GameManager โชว์ป้าย Win
    }
}