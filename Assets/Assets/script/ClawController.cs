using System.Collections;
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

        // ขยับแกน X
        while (Mathf.Abs(clawRect.anchoredPosition.x - targetX) > 1f)
        {
            float newX = Mathf.MoveTowards(clawRect.anchoredPosition.x, targetX, moveSpeed * Time.deltaTime);
            clawRect.anchoredPosition = new Vector2(newX, clawRect.anchoredPosition.y);
            yield return null;
        }
        
        // ล็อคตำแหน่งปลายทางให้เป๊ะ
        clawRect.anchoredPosition = new Vector2(targetX, clawRect.anchoredPosition.y);
    }

    public IEnumerator MoveDown()
    {
        if (clawRect == null) yield break;
        
        // *** จำค่า X ปัจจุบันไว้แน่นๆ ***
        float fixedX = clawRect.anchoredPosition.x;
        
        // เลื่อนลง (บังคับ X ให้เท่ากับ fixedX ตลอดเวลา)
        while (Mathf.Abs(clawRect.anchoredPosition.y - bottomY) > 1f)
        {
            float newY = Mathf.MoveTowards(clawRect.anchoredPosition.y, bottomY, moveSpeed * Time.deltaTime);
            clawRect.anchoredPosition = new Vector2(fixedX, newY); // ใช้ fixedX ห้ามเปลี่ยน
            yield return null;
        }
        
        clawRect.anchoredPosition = new Vector2(fixedX, bottomY);
    }

    public IEnumerator GripAndUp()
    {
        CheckForDoll();
        yield return new WaitForSeconds(0.5f); 

        // ดึงขึ้น (จำค่า X ไว้อีกที)
        float fixedX = clawRect.anchoredPosition.x;

        while (Mathf.Abs(clawRect.anchoredPosition.y - topY) > 1f)
        {
            float newY = Mathf.MoveTowards(clawRect.anchoredPosition.y, topY, moveSpeed * Time.deltaTime);
            clawRect.anchoredPosition = new Vector2(fixedX, newY); // ใช้ fixedX
            yield return null;
        }
        
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
            // รีเซ็ตตำแหน่งให้ห้อยตรงกลาง (X=0)
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
}