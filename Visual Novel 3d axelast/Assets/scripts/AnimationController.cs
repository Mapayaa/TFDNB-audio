using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;
    public string animationTriggerName = "Play";
    public string stopTriggerName = "Stop";
    public AudioClip animationSound;
    
    private AudioSource audioSource;
    private bool isPlaying = false;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
            
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Plays the animation
    /// </summary>
    public void PlayAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            animator.SetTrigger(animationTriggerName);
            isPlaying = true;
            
            if (animationSound != null && audioSource != null)
            {
                audioSource.clip = animationSound;
                audioSource.Play();
            }
            
            Debug.Log($"AnimationController {gameObject.name} playing animation");
        }
    }

    /// <summary>
    /// Stops the animation
    /// </summary>
    public void StopAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(stopTriggerName))
        {
            animator.SetTrigger(stopTriggerName);
            isPlaying = false;
            
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
                
            Debug.Log($"AnimationController {gameObject.name} stopping animation");
        }
    }

    /// <summary>
    /// Toggles animation play/stop
    /// </summary>
    public void ToggleAnimation()
    {
        if (isPlaying)
            StopAnimation();
        else
            PlayAnimation();
    }

    /// <summary>
    /// Sets animation speed
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
            animator.speed = speed;
    }
}
