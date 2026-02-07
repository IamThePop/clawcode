using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX Settings")]
    public AudioSource sfxSource;      
    
    [Header("UI Sounds (เพิ่มใหม่)")]
    public AudioClip clickClip;        // ลากไฟล์เสียงคลิกปุ่ม มาใส่ตรงนี้

    [Header("BGM Settings")]
    public AudioSource bgmSource;      
    public AudioClip bgmClip;          

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        PlayBGM();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // ฟังก์ชันใหม่: สั่งเล่นเสียงคลิกปุ่ม
    public void PlayClickSound()
    {
        if (clickClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clickClip);
        }
    }
    
    public void PlayBGM()
    {
        if (bgmSource != null && bgmClip != null)
        {
            if (bgmSource.clip != bgmClip || !bgmSource.isPlaying)
            {
                bgmSource.clip = bgmClip;
                bgmSource.loop = true; 
                bgmSource.Play();
            }
        }
    }
    
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }
}