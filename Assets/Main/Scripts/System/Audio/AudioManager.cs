using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    public AudioSource currentClip;

    public void PlayClip(AudioClip clip)
    {
        if (currentClip != null)
        {
            currentClip.Stop();
        }

        currentClip = GetComponent<AudioSource>();
        currentClip.clip = clip;
        currentClip.Play();
    }
}
