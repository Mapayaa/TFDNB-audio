using System.Collections.Generic;
using UnityEngine;

public class EndRestart : MonoBehaviour
{
    [Header("End/Restart UI")]
    public GameObject ActivateEndRestartCanvas;
    public List<GameObject> Deactivate;
    public List<GameObject> Activate;

    private void OnEnable()
    {
        if (ActivateEndRestartCanvas != null)
        {
            ActivateEndRestartCanvas.SetActive(true);
        }

        if (Deactivate != null)
        {
            foreach (var obj in Deactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    public void Restart()
    {
        if (Activate != null)
        {
            foreach (var obj in Activate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
    }
}
