#if UNITY_EDITOR
using SecretFour.Localization;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameTextCatalog))]
public class GameTextCatalogEditor : Editor
{
    private string search = "";
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("Edit every screen's wording here. Keep placeholders such as {0} and {1}, and matching rich-text tags, in each translation. Empty translations fall back to English.", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("latinFont"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("arabicFont"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("titleFont"));
        search = EditorGUILayout.TextField("Find text / key", search);
        var entries = serializedObject.FindProperty("entries");
        for (int i=0; i<entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            string key = entry.FindPropertyRelative("key").stringValue;
            string english = entry.FindPropertyRelative("english").stringValue;
            if (!string.IsNullOrEmpty(search) && key.IndexOf(search,System.StringComparison.OrdinalIgnoreCase)<0 && english.IndexOf(search,System.StringComparison.OrdinalIgnoreCase)<0) continue;
            entry.isExpanded=EditorGUILayout.Foldout(entry.isExpanded,key,true);
            if (!entry.isExpanded) continue;
            EditorGUI.indentLevel++;
            foreach(string language in new[]{"english","spanish","french","arabic"}) EditorGUILayout.PropertyField(entry.FindPropertyRelative(language));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
