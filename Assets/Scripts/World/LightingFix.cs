using UnityEngine;
using UnityEngine.Rendering;

public class LightingFix : MonoBehaviour
{
    void Start()
    {
        DynamicGI.UpdateEnvironment();
    }
}