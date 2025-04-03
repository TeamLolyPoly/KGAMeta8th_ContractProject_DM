using System.Collections;
using Michsky.UI.Heat;
using UnityEngine;

public class AudioTest : MonoBehaviour
{
    public AudioSource audioSource;
    public ButtonManager playButton;

    public IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance.IsInitialized);
        Initialize();
    }

    public void Initialize()
    {
        playButton.onClick.AddListener(TestPlay);
    }

    public void TestPlay()
    {
        AudioClip testClip = DataManager.Instance.GetTrack("Afro Dancehall").TrackAudio;
        audioSource.clip = testClip;
        audioSource.Play();
    }
}
