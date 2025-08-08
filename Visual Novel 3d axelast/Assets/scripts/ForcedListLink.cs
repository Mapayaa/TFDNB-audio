using UnityEngine;

namespace VN3D.Shared
{
    public class ForcedListLink : MonoBehaviour
    {
        [Tooltip("Verwijzing naar de bijbehorende ClickList GameObject.")]
        public GameObject clickListReference;

        [Tooltip("Startindex van het bereik binnen de ClickList.")]
        public int fromIndex;

        [Tooltip("Eindindex van het bereik binnen de ClickList.")]
        public int toIndex;
    }
}
