using UnityEngine;

public class EffectController : MonoBehaviour
{
    [Header("Effect Settings")]
    public ParticleSystem particleEffect;
    public GameObject effectObject;
    public Light effectLight;
    public AudioClip effectStartSound;
    public AudioClip effectStopSound;
    
    private AudioSource audioSource;
    private bool isEffectActive = false;

    void Awake()
    {
        if (particleEffect == null)
            particleEffect = GetComponent<ParticleSystem>();
            
        if (effectLight == null)
            effectLight = GetComponent<Light>();
            
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Starts the visual effect
    /// </summary>
    public void StartEffect()
    {
        isEffectActive = true;
        
        // Start particle effect
        if (particleEffect != null)
            particleEffect.Play();
            
        // Activate effect object
        if (effectObject != null)
            effectObject.SetActive(true);
            
        // Turn on light
        if (effectLight != null)
            effectLight.enabled = true;
            
        // Play start sound
        if (effectStartSound != null && audioSource != null)
        {
            audioSource.clip = effectStartSound;
            audioSource.Play();
        }
        
        Debug.Log($"EffectController {gameObject.name} started effect");
    }

    /// <summary>
    /// Stops the visual effect
    /// </summary>
    public void StopEffect()
    {
        isEffectActive = false;
        
        // Stop particle effect
        if (particleEffect != null)
            particleEffect.Stop();
            
        // Deactivate effect object
        if (effectObject != null)
            effectObject.SetActive(false);
            
        // Turn off light
        if (effectLight != null)
            effectLight.enabled = false;
            
        // Play stop sound
        if (effectStopSound != null && audioSource != null)
        {
            audioSource.clip = effectStopSound;
            audioSource.Play();
        }
        
        Debug.Log($"EffectController {gameObject.name} stopped effect");
    }

    /// <summary>
    /// Toggles the effect on/off
    /// </summary>
    public void ToggleEffect()
    {
        if (isEffectActive)
            StopEffect();
        else
            StartEffect();
    }

    /// <summary>
    /// Sets the effect intensity (for lights and particles)
    /// </summary>
    public void SetEffectIntensity(float intensity)
    {
        if (effectLight != null)
            effectLight.intensity = intensity;
            
        if (particleEffect != null)
        {
            var main = particleEffect.main;
            main.startLifetime = intensity;
        }
    }
}
