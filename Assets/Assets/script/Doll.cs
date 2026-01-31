using UnityEngine;

public class Doll : MonoBehaviour
{
    public string dollName;
    // ตัวช่วยดึงค่าตำแหน่งแบบ UI
    public RectTransform rectTransform { get { return GetComponent<RectTransform>(); } }
}