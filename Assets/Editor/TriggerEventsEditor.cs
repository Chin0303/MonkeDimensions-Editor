using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TriggerEvents))]
public class TriggerEventsEditor : Editor
{
    private SerializedProperty selectedEvent;
    private SerializedProperty associatedComponent;

    private void OnEnable()
    {
        selectedEvent = serializedObject.FindProperty("selectedEvent");
        associatedComponent = serializedObject.FindProperty("associatedComponent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();
        EditorGUILayout.LabelField("When player collides, event triggers.", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Event Selection", EditorStyles.boldLabel);
        selectedEvent.enumValueIndex = EditorGUILayout.Popup("Select Event", selectedEvent.enumValueIndex, GetEventNames());

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Associated Component", EditorStyles.boldLabel);

        switch ((TriggerEvents.Events)selectedEvent.enumValueIndex)
        {
            case TriggerEvents.Events.TeleportPlayer:
                EditorGUILayout.PropertyField(associatedComponent.FindPropertyRelative("gameObjectField"), new GUIContent("Teleport Location"));
                break;

            case TriggerEvents.Events.ToggleActiveState:
                EditorGUILayout.PropertyField(associatedComponent.FindPropertyRelative("gameObjectField"), new GUIContent("Object To Toggle"));
                break;

            default:
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private string[] GetEventNames()
    {
        return System.Enum.GetNames(typeof(TriggerEvents.Events));
    }
}