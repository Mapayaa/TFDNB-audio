using UnityEngine;
using System.Collections.Generic; // Required for List<>
using System.Linq; // Required for LINQ methods like Any()

public class StoryNode : MonoBehaviour
{
    public enum NodeRole { User, Machine, Extra }

    [Tooltip("Welke rol is actief op deze node?")]
    public NodeRole nodeRole = NodeRole.User;

    [Tooltip("Moet een click op deze node meetellen voor de click/penalty teller?")]
    public bool meetellenAlsClick = true;

    [Header("StoryNode Details")]
    [Tooltip("Als dit aan staat, is click op deze node uitgeschakeld.")]
    public bool disableClick = false;

    [Tooltip("Als dit aan staat, is sew op deze node uitgeschakeld.")]
    public bool disableSew = false;

    [Tooltip("The chapter this node belongs to.")]
    public int chapter = 1;
    [TextArea(3, 10)]
    public string nodeText;
    public Sprite background;
    public AudioClip backgroundSound;
    public StoryNode nextOnSew;
    public StoryNode nextOnClick;

    [System.Serializable]
    public class ActivationData
    {
        public GameObject gameObject;
        public bool activateOnStart;
        public bool deactivateOnEnd;
    }

    [Header("GameObject Activation")]
    public ActivationData object1 = new ActivationData();
    public ActivationData object2 = new ActivationData();

    public void ActivateGameObjects()
    {
        if (object1.gameObject != null && object1.activateOnStart)
        {
            object1.gameObject.SetActive(true);
        }
        if (object2.gameObject != null && object2.activateOnStart)
        {
            object2.gameObject.SetActive(true);
        }
    }

    public void DeactivateGameObjects()
    {
        if (object1.gameObject != null && object1.deactivateOnEnd)
        {
            object1.gameObject.SetActive(false);
        }
        if (object2.gameObject != null && object2.deactivateOnEnd)
        {
            object2.gameObject.SetActive(false);
        }
    }

}
