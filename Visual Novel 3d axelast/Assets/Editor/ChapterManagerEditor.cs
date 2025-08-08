using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VN3D.Shared;

[CustomEditor(typeof(ChapterManager))]
public class ChapterManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ChapterManager manager = (ChapterManager)target;

        // Toon alle standaard velden van ChapterManager
        DrawDefaultInspector();

        if (GUILayout.Button("Vul lege nodeText met GameObject naam"))
        {
            int count = 0;
            void SetTextIfEmpty(GameObject go)
            {
                var sn = go.GetComponent<StoryNode>();
                if (sn != null && string.IsNullOrWhiteSpace(sn.nodeText))
                {
                    sn.nodeText = go.name;
                    EditorUtility.SetDirty(sn);
                    count++;
                }
            }
            foreach (var node in manager.sewList) SetTextIfEmpty(node);
            foreach (var cl in manager.clickLists)
                for (int i = 0; i < cl.transform.childCount; i++)
                    SetTextIfEmpty(cl.transform.GetChild(i).gameObject);
            foreach (var fl in manager.forcedLists)
                for (int i = 0; i < fl.transform.childCount; i++)
                    SetTextIfEmpty(fl.transform.GetChild(i).gameObject);
            foreach (var el in manager.endLists)
                for (int i = 0; i < el.transform.childCount; i++)
                    SetTextIfEmpty(el.transform.GetChild(i).gameObject);
            Debug.Log($"nodeText ingevuld voor {count} lege StoryNodes.");
        }

        EditorGUILayout.LabelField("ChapterManager", EditorStyles.boldLabel);

        // Chapter number invulbaar veld
        EditorGUI.BeginChangeCheck();
        int newChapterNumber = EditorGUILayout.IntField("Chapter Number", manager.chapterNumber);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(manager, "Change Chapter Number");
            manager.chapterNumber = newChapterNumber;
            string desiredName = $"Chapter {manager.chapterNumber}";
            if (manager.gameObject.name != desiredName)
                manager.gameObject.name = desiredName;
            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Synchroniseer lijsten met scene"))
        {
            Undo.RecordObject(manager, "Sync Chapter Lists");
            manager.SyncLists();
            string desiredName = $"Chapter {manager.chapterNumber}";
            if (manager.gameObject.name != desiredName)
                manager.gameObject.name = desiredName;
            EditorUtility.SetDirty(manager);
        }

        EditorGUILayout.Space();
        DrawSewList(manager);
        DrawClickList(manager);
        DrawForcedList(manager);
        DrawEndList(manager);
    }

    // --- SewList sectie ---
    private void DrawSewList(ChapterManager manager)
    {
        EditorGUILayout.LabelField("SewNodes", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        var list = manager.sewList;
        var lilaStyle = new GUIStyle(EditorStyles.label);
        lilaStyle.normal.textColor = new Color(0.7f, 0.4f, 1f); // lila

        if (list == null || list.Count == 0)
        {
            EditorGUILayout.LabelField("<leeg>");
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                GameObject node = list[i];
                EditorGUILayout.BeginHorizontal();
                string nodeName = node != null ? node.name : "";
                string labelText = $"element {i}" + (string.IsNullOrEmpty(nodeName) ? "" : $" ({nodeName})");
                GUIContent labelContent = new GUIContent(labelText, nodeName);
                EditorGUILayout.LabelField(labelContent, lilaStyle, GUILayout.Width(180));
                GUILayout.Space(20);
                if (GUILayout.Button("Details", GUILayout.Width(60)) && node != null)
                {
                    var storyNode = node.GetComponent<StoryNode>();
                    if (storyNode != null)
                        StoryNodePopupWindow.ShowWindow(storyNode);
                    else
                        EditorUtility.DisplayDialog("Geen StoryNode", "Deze sewnode heeft geen StoryNode component.", "OK");
                }
                EditorGUILayout.ObjectField(node, typeof(GameObject), true);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    Undo.RecordObject(manager, "Remove SewNode");
                    Undo.DestroyObjectImmediate(node);
                    manager.SyncLists();
                    EditorUtility.SetDirty(manager);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Add Node", GUILayout.Width(100)))
        {
            GameObject chapterGO = manager.gameObject;
            GameObject sewNodesRoot = chapterGO.transform.Find("SewNodes")?.gameObject;
            if (sewNodesRoot == null)
            {
                sewNodesRoot = new GameObject("SewNodes");
                sewNodesRoot.transform.SetParent(chapterGO.transform);
            }
            int chapterNumber = manager.chapterNumber;
            int sewIndex = sewNodesRoot.transform.childCount;
            string nodeName = $"h{chapterNumber}.s{sewIndex}";
            GameObject newNodeGO = new GameObject(nodeName);
            newNodeGO.transform.SetParent(sewNodesRoot.transform);
            var storyNode = newNodeGO.AddComponent<StoryNode>();
            storyNode.chapter = manager.chapterNumber;
            Undo.RegisterCreatedObjectUndo(newNodeGO, "Add SewNode");
            manager.SyncLists();
            EditorUtility.SetDirty(manager);
        }
        GUI.backgroundColor = prevBg;
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    // --- ClickList sectie ---
    private void DrawClickList(ChapterManager manager)
    {
        EditorGUILayout.LabelField("ClickLists", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        var list = manager.clickLists;
        var yellowStyle = new GUIStyle(EditorStyles.label);
        yellowStyle.normal.textColor = new Color(1f, 0.85f, 0f);

        if (list == null || list.Count == 0)
        {
            EditorGUILayout.LabelField("<leeg>");
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                GameObject clickList = list[i];
                string clickListName = clickList != null ? clickList.name : "";
                string labelText = $"ClickList {i}" + (string.IsNullOrEmpty(clickListName) ? "" : $": {clickListName}");
                EditorGUILayout.LabelField(labelText, yellowStyle, GUILayout.Width(250));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(clickList, typeof(GameObject), true);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    Undo.RecordObject(manager, "Remove ClickList");
                    Undo.DestroyObjectImmediate(clickList);
                    manager.SyncLists();
                    EditorUtility.SetDirty(manager);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                for (int c = 0; c < clickList.transform.childCount; c++)
                {
                    GameObject entry = clickList.transform.GetChild(c).gameObject;
                    EditorGUILayout.BeginHorizontal();
                    string entryName = entry != null ? entry.name : "";
                    string entryLabel = $"element {c}" + (string.IsNullOrEmpty(entryName) ? "" : $" ({entryName})");
                    GUIContent entryContent = new GUIContent(entryLabel, entryName);
                    EditorGUILayout.LabelField(entryContent, yellowStyle, GUILayout.Width(180));
                    GUILayout.Space(20);
                    if (GUILayout.Button("Details", GUILayout.Width(60)) && entry != null)
                    {
                        var storyNode = entry.GetComponent<StoryNode>();
                        if (storyNode != null)
                            StoryNodePopupWindow.ShowWindow(storyNode);
                        else
                            EditorUtility.DisplayDialog("Geen StoryNode", "Deze entry heeft geen StoryNode component.", "OK");
                    }
                    EditorGUILayout.ObjectField(entry, typeof(GameObject), true);
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        Undo.RecordObject(manager, "Remove ClickList Entry");
                        Undo.DestroyObjectImmediate(entry);
                        manager.SyncLists();
                        EditorUtility.SetDirty(manager);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                Color prevBgEntry = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.85f, 0f);
                if (GUILayout.Button("Add Entry", GUILayout.Width(100)))
                {
                    int entryIndex = clickList.transform.childCount;
                    string entryName = $"{clickList.name}.{entryIndex}";
                    GameObject newEntryGO = new GameObject(entryName);
                    newEntryGO.transform.SetParent(clickList.transform);
            var storyNode = newEntryGO.AddComponent<StoryNode>();
            storyNode.chapter = manager.chapterNumber;
            storyNode.nodeText = newEntryGO.name;
                    storyNode.nodeText = newEntryGO.name;
                    storyNode.meetellenAlsClick = false;
                    Undo.RegisterCreatedObjectUndo(newEntryGO, "Add ClickList Entry");
                    manager.SyncLists();
                    EditorUtility.SetDirty(manager);
                }
                GUI.backgroundColor = prevBgEntry;
                EditorGUI.indentLevel--;
            }
        }
        Color prevBgClickList = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.85f, 0f);
        if (GUILayout.Button("Add ClickList", GUILayout.Width(120)))
        {
            GameObject chapterGO = manager.gameObject;
            GameObject clickListsRoot = chapterGO.transform.Find("ClickLists")?.gameObject;
            if (clickListsRoot == null)
            {
                clickListsRoot = new GameObject("ClickLists");
                clickListsRoot.transform.SetParent(chapterGO.transform);
            }
            int chapterNumber = manager.chapterNumber;
            HashSet<int> usedIndices = new HashSet<int>();
            foreach (Transform child in clickListsRoot.transform)
            {
                if (child.name.StartsWith($"H{chapterNumber}.C"))
                {
                    string idxStr = child.name.Substring($"H{chapterNumber}.C".Length);
                    if (int.TryParse(idxStr, out int idx))
                        usedIndices.Add(idx);
                }
            }
            int clickListIndex = 0;
            while (usedIndices.Contains(clickListIndex))
                clickListIndex++;
            string clickListName = $"H{chapterNumber}.C{clickListIndex}";
            GameObject newClickListGO = new GameObject(clickListName);
            newClickListGO.transform.SetParent(clickListsRoot.transform);
            Undo.RegisterCreatedObjectUndo(newClickListGO, "Add ClickList");
            string entryName = $"{clickListName}.0";
            GameObject entryGO = new GameObject(entryName);
            entryGO.transform.SetParent(newClickListGO.transform);
            var storyNode = entryGO.AddComponent<StoryNode>();
            storyNode.chapter = manager.chapterNumber;
            Undo.RegisterCreatedObjectUndo(entryGO, "Add ClickList Entry");
            manager.SyncLists();
            EditorUtility.SetDirty(manager);
        }
        GUI.backgroundColor = prevBgClickList;
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    // --- ForcedList sectie ---
    private void DrawForcedList(ChapterManager manager)
    {
        EditorGUILayout.LabelField("ForcedLists", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        var list = manager.forcedLists;
        var redStyle = new GUIStyle(EditorStyles.label);
        redStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);

        if (list == null || list.Count == 0)
        {
            EditorGUILayout.LabelField("<leeg>");
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                GameObject forcedList = list[i];
                string forcedListName = forcedList != null ? forcedList.name : "";
                string labelText = $"ForcedList {i}" + (string.IsNullOrEmpty(forcedListName) ? "" : $": {forcedListName}");
                EditorGUILayout.LabelField(labelText, redStyle, GUILayout.Width(250));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Details", GUILayout.Width(60)) && forcedList != null)
                {
                    var forcedListLink = forcedList.GetComponent<ForcedListLink>();
                    if (forcedListLink != null)
                        ForcedListDetailWindow.ShowWindow(forcedListLink, manager);
                    else
                        EditorUtility.DisplayDialog("Geen ForcedListLink", "Deze ForcedList heeft geen ForcedListLink component.", "OK");
                }
                EditorGUILayout.ObjectField(forcedList, typeof(GameObject), true);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    Undo.RecordObject(manager, "Remove ForcedList");
                    Undo.DestroyObjectImmediate(forcedList);
                    manager.SyncLists();
                    EditorUtility.SetDirty(manager);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                for (int c = 0; c < forcedList.transform.childCount; c++)
                {
                    GameObject entry = forcedList.transform.GetChild(c).gameObject;
                    EditorGUILayout.BeginHorizontal();
                    string entryName = entry != null ? entry.name : "";
                    string entryLabel = $"element {c}" + (string.IsNullOrEmpty(entryName) ? "" : $" ({entryName})");
                    GUIContent entryContent = new GUIContent(entryLabel, entryName);
                    EditorGUILayout.LabelField(entryContent, redStyle, GUILayout.Width(180));
                    GUILayout.Space(20);
                    if (GUILayout.Button("Details", GUILayout.Width(60)) && entry != null)
                    {
                        var storyNode = entry.GetComponent<StoryNode>();
                        if (storyNode != null)
                            StoryNodePopupWindow.ShowWindow(storyNode);
                        else
                            EditorUtility.DisplayDialog("Geen StoryNode", "Deze entry heeft geen StoryNode component.", "OK");
                    }
                    EditorGUILayout.ObjectField(entry, typeof(GameObject), true);
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        Undo.RecordObject(manager, "Remove ForcedList Entry");
                        Undo.DestroyObjectImmediate(entry);
                        manager.SyncLists();
                        EditorUtility.SetDirty(manager);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                Color prevBgEntry = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("Add Entry", GUILayout.Width(100)))
                {
                    int entryIndex = forcedList.transform.childCount;
                    string entryName = $"{forcedList.name}.{entryIndex}";
                    GameObject newEntryGO = new GameObject(entryName);
                    newEntryGO.transform.SetParent(forcedList.transform);
                    var storyNode = newEntryGO.AddComponent<StoryNode>();
                    storyNode.chapter = manager.chapterNumber;
                    storyNode.meetellenAlsClick = false;
                    Undo.RegisterCreatedObjectUndo(newEntryGO, "Add ForcedList Entry");
                    manager.SyncLists();
                    EditorUtility.SetDirty(manager);
                }
                GUI.backgroundColor = prevBgEntry;
                EditorGUI.indentLevel--;
            }
        }
        Color prevBgForcedList = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("Add ForcedList", GUILayout.Width(120)))
        {
            GameObject chapterGO = manager.gameObject;
            GameObject forcedListsRoot = chapterGO.transform.Find("ForcedLists")?.gameObject;
            if (forcedListsRoot == null)
            {
                forcedListsRoot = new GameObject("ForcedLists");
                forcedListsRoot.transform.SetParent(chapterGO.transform);
            }
            System.Text.RegularExpressions.Regex forcedListRegex = new System.Text.RegularExpressions.Regex(@"^F(\d+)\b");
            HashSet<int> usedIndices = new HashSet<int>();
            foreach (Transform child in forcedListsRoot.transform)
            {
                var match = forcedListRegex.Match(child.name);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int idx))
                    usedIndices.Add(idx);
            }
            int forcedListIndex = 0;
            while (usedIndices.Contains(forcedListIndex))
                forcedListIndex++;
            string forcedListName = $"F{forcedListIndex}";
            GameObject newForcedListGO = new GameObject(forcedListName);
            newForcedListGO.transform.SetParent(forcedListsRoot.transform);
            var forcedListLink = newForcedListGO.AddComponent<ForcedListLink>();
            Undo.RegisterCreatedObjectUndo(newForcedListGO, "Add ForcedList");
            string entryName = $"{forcedListName}.0";
            GameObject entryGO = new GameObject(entryName);
            entryGO.transform.SetParent(newForcedListGO.transform);
            var storyNode = entryGO.AddComponent<StoryNode>();
            storyNode.chapter = manager.chapterNumber;
            Undo.RegisterCreatedObjectUndo(entryGO, "Add ForcedList Entry");
            manager.SyncLists();
            EditorUtility.SetDirty(manager);

            ForcedListDetailWindow.ShowWindow(forcedListLink, manager);
        }
        GUI.backgroundColor = prevBgForcedList;
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    // --- EndList sectie ---
    private void DrawEndList(ChapterManager manager)
    {
        EditorGUILayout.LabelField("EndLists", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        var list = manager.endLists;
        var blueStyle = new GUIStyle(EditorStyles.label);
        blueStyle.normal.textColor = new Color(0.3f, 0.5f, 1f);

        if (list == null || list.Count == 0)
        {
            EditorGUILayout.LabelField("<leeg>");
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                GameObject endList = list[i];
                string endListName = endList != null ? endList.name : "";
                string labelText = $"EndList {i}" + (string.IsNullOrEmpty(endListName) ? "" : $": {endListName})");
                EditorGUILayout.LabelField(labelText, blueStyle, GUILayout.Width(250));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Details", GUILayout.Width(60)) && endList != null)
                {
                    EndListDetailWindow.ShowWindow(endList, manager);
                }
                EditorGUILayout.ObjectField(endList, typeof(GameObject), true);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    Undo.RecordObject(manager, "Remove EndList");
                    Undo.DestroyObjectImmediate(endList);
                    manager.SyncLists();
                    EditorUtility.SetDirty(manager);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                for (int c = 0; c < endList.transform.childCount; c++)
                {
                    GameObject entry = endList.transform.GetChild(c).gameObject;
                    EditorGUILayout.BeginHorizontal();
                    string entryName = entry != null ? entry.name : "";
                    string entryLabel = $"element {c}" + (string.IsNullOrEmpty(entryName) ? "" : $" ({entryName})");
                    GUIContent entryContent = new GUIContent(entryLabel, entryName);
                    EditorGUILayout.LabelField(entryContent, blueStyle, GUILayout.Width(180));
                    GUILayout.Space(20);
                    if (GUILayout.Button("Details", GUILayout.Width(60)) && entry != null)
                    {
                        var storyNode = entry.GetComponent<StoryNode>();
                        if (storyNode != null)
                            StoryNodePopupWindow.ShowWindow(storyNode);
                        else
                            EditorUtility.DisplayDialog("Geen StoryNode", "Deze entry heeft geen StoryNode component.", "OK");
                    }
                    EditorGUILayout.ObjectField(entry, typeof(GameObject), true);
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        Undo.RecordObject(manager, "Remove EndList Entry");
                        Undo.DestroyObjectImmediate(entry);
                        manager.SyncLists();
                        EditorUtility.SetDirty(manager);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                Color prevBgEntry = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.5f, 1f);
                if (GUILayout.Button("Add Entry", GUILayout.Width(100)))
                {
                    int entryIndex = endList.transform.childCount;
                    string entryName = $"{endList.name}.{entryIndex}";
                    GameObject newEntryGO = new GameObject(entryName);
                    newEntryGO.transform.SetParent(endList.transform);
                    var storyNode = newEntryGO.AddComponent<StoryNode>();
                    storyNode.chapter = manager.chapterNumber;
                    storyNode.meetellenAlsClick = false;
                    Undo.RegisterCreatedObjectUndo(newEntryGO, "Add EndList Entry");
                    manager.SyncLists();
                    EditorUtility.SetDirty(manager);
                }
                GUI.backgroundColor = prevBgEntry;
                EditorGUI.indentLevel--;
            }
        }
        Color prevBgEndList = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.5f, 1f);
        if (GUILayout.Button("Add EndList", GUILayout.Width(120)))
        {
            GameObject chapterGO = manager.gameObject;
            GameObject endListsRoot = chapterGO.transform.Find("EndLists")?.gameObject;
            if (endListsRoot == null)
            {
                endListsRoot = new GameObject("EndLists");
                endListsRoot.transform.SetParent(chapterGO.transform);
            }
            System.Text.RegularExpressions.Regex endListRegex = new System.Text.RegularExpressions.Regex(@"^E(\d+)\b");
            HashSet<int> usedIndices = new HashSet<int>();
            foreach (Transform child in endListsRoot.transform)
            {
                var match = endListRegex.Match(child.name);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int idx))
                    usedIndices.Add(idx);
            }
            int endListIndex = 0;
            while (usedIndices.Contains(endListIndex))
                endListIndex++;
            string endListName = $"E{endListIndex}";
            GameObject newEndListGO = new GameObject(endListName);
            newEndListGO.transform.SetParent(endListsRoot.transform);
            Undo.RegisterCreatedObjectUndo(newEndListGO, "Add EndList");
            string entryName = $"{endListName}.0";
            GameObject entryGO = new GameObject(entryName);
            entryGO.transform.SetParent(newEndListGO.transform);
            var storyNode = entryGO.AddComponent<StoryNode>();
            storyNode.chapter = manager.chapterNumber;
            Undo.RegisterCreatedObjectUndo(entryGO, "Add EndList Entry");
            manager.SyncLists();
            EditorUtility.SetDirty(manager);
        }
        GUI.backgroundColor = prevBgEndList;
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
}

public class EndListDetailWindow : EditorWindow
{
    private GameObject endListGO;
    private ChapterManager chapterManager;
    private int selectedClickListIndex = 0;
    private int fromIndex = 0;
    private int toIndex = 0;
    private string[] clickListNames;

    public static void ShowWindow(GameObject endListGO, ChapterManager chapterManager)
    {
        var window = ScriptableObject.CreateInstance<EndListDetailWindow>();
        window.endListGO = endListGO;
        window.chapterManager = chapterManager;
        window.titleContent = new GUIContent("EndList Details");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 180);
        window.Init();
        window.ShowUtility();
    }

    private void Init()
    {
        selectedClickListIndex = 0;
        if (chapterManager != null && chapterManager.clickLists != null)
        {
            clickListNames = new string[chapterManager.clickLists.Count];
            for (int i = 0; i < chapterManager.clickLists.Count; i++)
            {
                clickListNames[i] = chapterManager.clickLists[i] != null ? chapterManager.clickLists[i].name : $"ClickList {i}";
                // Probeer huidige clickList te selecteren uit naam
                if (endListGO != null && endListGO.name.Contains(clickListNames[i]))
                    selectedClickListIndex = i;
            }
        }
        else
        {
            clickListNames = new string[0];
        }

        fromIndex = 0;
        toIndex = 0;
        if (endListGO != null)
        {
            var so = new SerializedObject(endListGO);
            // Probeer bereik uit naam te halen
            string name = endListGO.name;
            int bracketStart = name.IndexOf('[');
            int bracketEnd = name.IndexOf(']');
            if (bracketStart >= 0 && bracketEnd > bracketStart)
            {
                string inside = name.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
                var parts = inside.Split(' ');
                if (parts.Length == 2)
                {
                    string[] range = parts[1].Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int from) && int.TryParse(range[1], out int to))
                    {
                        fromIndex = from;
                        toIndex = to;
                    }
                }
            }
        }
    }

    void OnGUI()
    {
        if (endListGO == null || chapterManager == null)
        {
            EditorGUILayout.LabelField("Geen EndList of ChapterManager gevonden.");
            if (GUILayout.Button("OK")) this.Close();
            return;
        }

        EditorGUILayout.LabelField("EndList Details", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Koppel aan ClickList:");
        int newSelected = EditorGUILayout.Popup(selectedClickListIndex, clickListNames);
        if (newSelected != selectedClickListIndex)
        {
            selectedClickListIndex = newSelected;
        }

        fromIndex = EditorGUILayout.IntField("From Index", fromIndex);
        toIndex = EditorGUILayout.IntField("To Index", toIndex);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Opslaan"))
        {
            // Pas naam van endListGO aan
            string baseName = endListGO.name;
            string endListIndex = baseName;
            if (baseName.Contains(" "))
                endListIndex = baseName.Split(' ')[0];
            string clickListName = clickListNames != null && selectedClickListIndex < clickListNames.Length
                ? clickListNames[selectedClickListIndex]
                : "None";
            string newListName = $"{endListIndex} [{clickListName} {fromIndex}-{toIndex}]";
            endListGO.name = newListName;
            EditorUtility.SetDirty(endListGO);

            // Hernoem alle child entries
            for (int i = 0; i < endListGO.transform.childCount; i++)
            {
                var entry = endListGO.transform.GetChild(i).gameObject;
                entry.name = $"{newListName}.{i}";
                EditorUtility.SetDirty(entry);
            }

            this.Close();
        }
        if (GUILayout.Button("Annuleren"))
        {
            this.Close();
        }
        EditorGUILayout.EndHorizontal();
    }
}

// Popup voor ForcedList details
public class ForcedListDetailWindow : EditorWindow
{
    private ForcedListLink forcedListLink;
    private ChapterManager chapterManager;
    private int selectedClickListIndex = 0;
    private int fromIndex = 0;
    private int toIndex = 0;
    private string[] clickListNames;

    public static void ShowWindow(ForcedListLink forcedListLink, ChapterManager chapterManager)
    {
        var window = ScriptableObject.CreateInstance<ForcedListDetailWindow>();
        window.forcedListLink = forcedListLink;
        window.chapterManager = chapterManager;
        window.titleContent = new GUIContent("ForcedList Details");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 180);
        window.Init();
        window.ShowUtility();
    }

    private void Init()
    {
        selectedClickListIndex = 0;
        if (chapterManager != null && chapterManager.clickLists != null)
        {
            clickListNames = new string[chapterManager.clickLists.Count];
            for (int i = 0; i < chapterManager.clickLists.Count; i++)
            {
                clickListNames[i] = chapterManager.clickLists[i] != null ? chapterManager.clickLists[i].name : $"ClickList {i}";
                if (forcedListLink != null && forcedListLink.clickListReference == chapterManager.clickLists[i])
                    selectedClickListIndex = i;
            }
        }
        else
        {
            clickListNames = new string[0];
        }

        fromIndex = 0;
        toIndex = 0;
        if (forcedListLink != null)
        {
            var so = new SerializedObject(forcedListLink);
            var fromProp = so.FindProperty("fromIndex");
            var toProp = so.FindProperty("toIndex");
            if (fromProp != null) fromIndex = fromProp.intValue;
            if (toProp != null) toIndex = toProp.intValue;
        }
    }

    private int GetIntField(ForcedListLink link, string field)
    {
        var so = new SerializedObject(link);
        var prop = so.FindProperty(field);
        return prop != null ? prop.intValue : 0;
    }

    private void SetIntField(ForcedListLink link, string field, int value)
    {
        var so = new SerializedObject(link);
        var prop = so.FindProperty(field);
        if (prop != null)
        {
            prop.intValue = value;
            so.ApplyModifiedProperties();
        }
    }

    void OnGUI()
    {
        if (forcedListLink == null || chapterManager == null)
        {
            EditorGUILayout.LabelField("Geen ForcedListLink of ChapterManager gevonden.");
            if (GUILayout.Button("OK")) this.Close();
            return;
        }

        EditorGUILayout.LabelField("ForcedList Details", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Koppel aan ClickList:");
        int newSelected = EditorGUILayout.Popup(selectedClickListIndex, clickListNames);
        if (newSelected != selectedClickListIndex)
        {
            selectedClickListIndex = newSelected;
            if (chapterManager.clickLists != null && selectedClickListIndex < chapterManager.clickLists.Count)
                forcedListLink.clickListReference = chapterManager.clickLists[selectedClickListIndex];
            EditorUtility.SetDirty(forcedListLink);
        }

        fromIndex = EditorGUILayout.IntField("From Index", fromIndex);
        toIndex = EditorGUILayout.IntField("To Index", toIndex);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Opslaan"))
        {
            if (chapterManager != null && chapterManager.clickLists != null && selectedClickListIndex < chapterManager.clickLists.Count)
                forcedListLink.clickListReference = chapterManager.clickLists[selectedClickListIndex];
            else
                forcedListLink.clickListReference = null;

            SetIntField(forcedListLink, "fromIndex", fromIndex);
            SetIntField(forcedListLink, "toIndex", toIndex);
            EditorUtility.SetDirty(forcedListLink);

            string baseName = forcedListLink.gameObject.name;
            string forcedListIndex = baseName;
            if (baseName.Contains(" "))
                forcedListIndex = baseName.Split(' ')[0];
            string clickListName = clickListNames != null && selectedClickListIndex < clickListNames.Length
                ? clickListNames[selectedClickListIndex]
                : "None";
            string newListName = $"{forcedListIndex} [{clickListName} {fromIndex}-{toIndex}]";
            forcedListLink.gameObject.name = newListName;

            // Hernoem alle child entries
            var go = forcedListLink.gameObject;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var entry = go.transform.GetChild(i).gameObject;
                entry.name = $"{newListName}.{i}";
                EditorUtility.SetDirty(entry);
            }

            this.Close();
        }
        if (GUILayout.Button("Annuleren"))
        {
            this.Close();
        }
        EditorGUILayout.EndHorizontal();
    }
}
