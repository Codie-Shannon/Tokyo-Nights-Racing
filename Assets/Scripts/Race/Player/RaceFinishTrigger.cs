using UnityEngine;

public class RaceFinishTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (RaceManager.Instance != null)
            RaceManager.Instance.HitFinishTrigger();
    }
}