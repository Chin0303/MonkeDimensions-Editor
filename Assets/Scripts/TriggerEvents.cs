using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerEvents : MonoBehaviour
{
    public enum Events
    {
        TeleportPlayer,
        ToggleActiveState,
    }

    [System.Serializable]
    public class AssociatedComponent
    {
        [SerializeField]
        public GameObject gameObjectField;
        // incase i ever gonna add trigger animations for example i can easily add it that way
    }

    [SerializeField, HideInInspector]
    public Events selectedEvent;

    [SerializeField, HideInInspector]
    public AssociatedComponent associatedComponent;
}