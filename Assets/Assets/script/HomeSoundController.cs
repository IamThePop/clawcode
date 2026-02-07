using UnityEngine;

public class HomeSoundController : MonoBehaviour
{
    public AudioSource audioSource; // ตัวลำโพง
    public AudioClip clickSound;    // ไฟล์เสียงคลิก

    public void PlayClickSound()
    {
        // สั่งให้เล่นเสียง 1 ครั้ง
        if (audioSource && clickSound)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}