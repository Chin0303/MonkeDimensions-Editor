using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AddonsData
{
    public List<GameObject> SlipperyObjects = new List<GameObject>();

    [Tooltip("Points For The Terminal To Be When The Player Is Close To It")]
    public List<GameObject> ExtraTerminals = new List<GameObject>();
}

public class DimensionDescriptor : MonoBehaviour
{
    public string Name;
    public string Author;
    public string Description;

    public GameObject SpawnPosition;
    public GameObject TerminalPosition;
    public AddonsData Addons = new AddonsData();

    public List<string> GetAssociatedComponents(TriggerEvents.Events eventType)
    {
        List<string> associatedComponents = new List<string>();

        TriggerEvents[] triggerEventsArray = GetComponentsInChildren<TriggerEvents>();
        foreach (TriggerEvents triggerEvents in triggerEventsArray)
        {
            if (triggerEvents.selectedEvent == eventType && triggerEvents.associatedComponent != null && triggerEvents.associatedComponent.gameObjectField != null)
            {
                string componentName = triggerEvents.associatedComponent.gameObjectField.name;
                associatedComponents.Add(componentName);
            }
        }

        return associatedComponents;
    }
}