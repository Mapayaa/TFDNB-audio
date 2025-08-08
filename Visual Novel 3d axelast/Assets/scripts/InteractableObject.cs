using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Interactable Settings")]
    public bool isActive = false;
    public GameObject visualIndicator;
    public AudioClip activationSound;
    public AudioClip deactivationSound;
    
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Activates this interactable object
    /// </summary>
    public void Activate()
    {
        isActive = true;
        
        if (visualIndicator != null)
            visualIndicator.SetActive(true);
            
        if (activationSound != null && audioSource != null)
        {
            audioSource.clip = activationSound;
            audioSource.Play();
        }
        
        Debug.Log($"InteractableObject {gameObject.name} activated");
    }

    /// <summary>
    /// Deactivates this interactable object
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        
        if (visualIndicator != null)
            visualIndicator.SetActive(false);
            
        if (deactivationSound != null && audioSource != null)
        {
            audioSource.clip = deactivationSound;
            audioSource.Play();
        }
        
        Debug.Log($"InteractableObject {gameObject.name} deactivated");
    }

    /// <summary>
    /// Toggles the active state
    /// </summary>
    public void Toggle()
    {
        if (isActive)
            Deactivate();
        else
            Activate();
    }
}
