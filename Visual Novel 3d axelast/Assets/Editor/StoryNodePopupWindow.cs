using UnityEngine;
using UnityEditor;

public class StoryNodePopupWindow : EditorWindow
{
    private static StoryNodePopupWindow currentWindow;

    private StoryNode node;
    private SerializedObject nodeSO;
    private string originalJson;
    private bool changesMade = false;

    public static void ShowWindow(StoryNode node)
    {
        if (node == null) return;
        if (currentWindow != null)
        {
            currentWindow.Close();
        }
        var window = ScriptableObject.CreateInstance<StoryNodePopupWindow>();
        currentWindow = window;
        window.node = node;
        window.nodeSO = new SerializedObject(node);
        window.originalJson = JsonUtility.ToJson(node);
        window.titleContent = new GUIContent("StoryNode Details");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 400);
        window.ShowUtility();
    }

    void OnGUI()
    {
        if (node == null || nodeSO == null)
        {
            EditorGUILayout.LabelField("Geen node geladen.");
            if (GUILayout.Button("OK")) this.Close();
            return;
        }

        nodeSO.Update();

        EditorGUILayout.LabelField("StoryNode Details", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(nodeSO.FindProperty("chapter"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("nodeText"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("background"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("meetellenAlsClick"));
        // EditorGUILayout.PropertyField(nodeSO.FindProperty("nextOnClick"));
        // EditorGUILayout.PropertyField(nodeSO.FindProperty("nextOnSew"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("backgroundSound"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("nodeRole"));

        // Contextuele uitleg over sew/click progressie
        string contextUitleg = GetNodeProgressionExplanation(node);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(contextUitleg, MessageType.Info);

        // Nieuwe checkboxes voor StoryNode Details
        EditorGUILayout.PropertyField(nodeSO.FindProperty("disableClick"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("disableSew"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("GameObject Activation", EditorStyles.boldLabel);

        // GameObject 1
        EditorGUILayout.PropertyField(nodeSO.FindProperty("object1.gameObject"), new GUIContent("GameObject 1"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("object1.activateOnStart"), new GUIContent("Activate on Start"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("object1.deactivateOnEnd"), new GUIContent("Deactivate on End"));

        EditorGUILayout.Space();

        // GameObject 2
        EditorGUILayout.PropertyField(nodeSO.FindProperty("object2.gameObject"), new GUIContent("GameObject 2"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("object2.activateOnStart"), new GUIContent("Activate on Start"));
        EditorGUILayout.PropertyField(nodeSO.FindProperty("object2.deactivateOnEnd"), new GUIContent("Deactivate on End"));

        nodeSO.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("OK"))
        {
            changesMade = true;
            this.Close();
        }
        if (GUILayout.Button("Cancel"))
        {
            // Revert changes
            if (!string.IsNullOrEmpty(originalJson))
                JsonUtility.FromJsonOverwrite(originalJson, node);
            this.Close();
        }
        EditorGUILayout.EndHorizontal();
    }

    void OnLostFocus()
    {
        if (!changesMade && !string.IsNullOrEmpty(originalJson) && node != null)
        {
            JsonUtility.FromJsonOverwrite(originalJson, node);
        }
    }

    void OnDestroy()
    {
        if (currentWindow == this)
            currentWindow = null;
    }

    // Context aware uitleg
    private string GetNodeProgressionExplanation(StoryNode node)
    {
        if (node == null) return "";

        string parentType = "";
        if (node.transform.parent != null)
        {
            string pname = node.transform.parent.name;
            if (pname == "SewNodes") parentType = "SewList";
            else if (pname.StartsWith("H") && pname.Contains(".C")) parentType = "ClickList";
            else if (pname.StartsWith("F")) parentType = "ForcedList";
            else if (pname.StartsWith("E")) parentType = "EndList";
        }

        bool disableClick = node.disableClick;
        bool disableSew = node.disableSew;

        string sewText = "";
        string clickText = "";

        switch (parentType)
        {
            case "SewList":
                sewText = disableSew ? "Sew (spatiebalk): Uitgeschakeld ('disableSew' aan)." : "Sew (spatiebalk): Gaat naar de volgende node.";
                clickText = "Click (muisklik): Heeft geen effect.";
                break;
            case "ClickList":
                sewText = disableSew
                    ? "Sew (spatiebalk): Uitgeschakeld ('disableSew' aan)."
                    : "Sew (spatiebalk): Verlaat de clicklist en gaat naar de volgende entry in de sewList.";
                clickText = disableClick
                    ? "Click (muisklik): Uitgeschakeld ('disableClick' aan)."
                    : "Click (muisklik): Gaat naar de volgende entry in de clicklist.";
                break;
            case "ForcedList":
                sewText = "Sew (spatiebalk): Gaat naar de volgende ForcedList entry.";
                clickText = "Click (muisklik): Wordt genegeerd.";
                break;
            case "EndList":
                sewText = "Sew (spatiebalk): Gaat naar de volgende EndList entry. Bij de laatste entry eindigt het spel.";
                clickText = "Click (muisklik): Gaat naar de volgende EndList entry. Bij de laatste entry eindigt het spel.";
                break;
            default:
                sewText = disableSew ? "Sew (spatiebalk): Uitgeschakeld ('disableSew' aan)." : "Sew (spatiebalk): Mogelijk afhankelijk van context.";
                clickText = disableClick ? "Click (muisklik): Uitgeschakeld ('disableClick' aan)." : "Click (muisklik): Mogelijk afhankelijk van context.";
                break;
        }

        string waarschuwing = (disableClick && disableSew)
            ? "\nLet op: Zowel 'disableClick' als 'disableSew' staan aan. De speler kan niet verder vanaf deze node."
            : "";

        return $"{sewText}\n{clickText}{waarschuwing}";
    }
}
