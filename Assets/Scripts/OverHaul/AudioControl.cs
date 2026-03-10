using UnityEngine;

namespace NewArch
{

/// <summary>
/// Plays one-shot sound effects for game events.
/// Called by AnimationControlV2 at the appropriate animation moments.
/// Attach to the same GameObject as AnimationControlV2.
/// Part of the View layer (MVC).
/// </summary>
public class AudioControlV2 : MonoBehaviour
{
    [Header("Sound Effects")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private AudioClip slideSound;
    [SerializeField] private AudioClip fallLandSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip respawnSound;
    [SerializeField] private AudioClip winSound;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayWalkSound()    => PlayClip(walkSound);
    public void PlaySlideSound()   => PlayClip(slideSound);
    public void PlayLandSound()    => PlayClip(fallLandSound);
    public void PlayDeathSound()   => PlayClip(deathSound);
    public void PlayRespawnSound() => PlayClip(respawnSound);
    public void PlayWinSound()     => PlayClip(winSound);

    private void PlayClip(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip, masterVolume);
    }
}

} // namespace NewArch
