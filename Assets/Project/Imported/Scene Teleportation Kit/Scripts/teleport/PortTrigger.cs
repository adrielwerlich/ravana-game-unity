using UnityEngine;

namespace Scene_Teleportation_Kit.Scripts.teleport
{
    public class PortTrigger : MonoBehaviour
    {
        [SerializeField] private Transform destination;

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.name.Contains("Player"))
            {
                transform.position = destination.position;
            }
        }
    }
}