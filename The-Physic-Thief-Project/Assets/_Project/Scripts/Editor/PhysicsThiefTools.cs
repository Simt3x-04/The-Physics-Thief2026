using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PhysicsThief.EditorTools
{
    /// <summary>
    /// Level-design helper, under the menu  Tools > Physics Thief.
    /// Select props in the Hierarchy and swap each one for the prefab with the same name prefixed by "PO_"
    /// (e.g. Barrel01 -> PO_Barrel01), so that it can be scanned. Position, rotation, scale and parent are kept.
    /// </summary>
    public static class PhysicsThiefTools
    {
        private const string Menu = "Tools/Physics Thief/";
        private const string LogTag = "[Physics Thief Tools] ";

        [MenuItem(Menu + "Replace Selected Props With Physical Versions", false, 20)]
        public static void ReplaceSelectedWithPhysical()
        {
            GameObject[] selection = Selection.gameObjects;
            if (selection.Length == 0)
            {
                Dialog("Replace props", "Select one or more props (e.g. Barrel01, Rock02) in the Hierarchy first.");
                return;
            }

            int replaced = 0;
            var skipped = new List<string>();
            var created = new List<Object>();

            Undo.SetCurrentGroupName("Replace props with physical versions");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject go in selection)
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                {
                    skipped.Add(go.name + " (not the root of a prefab)");
                    continue;
                }
                if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go)
                {
                    skipped.Add(go.name + " (inside another prefab: unpack that one first)");
                    continue;
                }

                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                string physicalName = "PO_" + (source != null ? source.name : go.name);
                GameObject physical = LoadPrefabByName(physicalName);
                if (physical == null)
                {
                    skipped.Add(go.name + " (no " + physicalName + " prefab)");
                    continue;
                }

                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(physical, go.scene);
                Undo.RegisterCreatedObjectUndo(replacement, "Replace with physical version");

                Transform t = replacement.transform;
                t.SetParent(go.transform.parent, false);
                t.SetPositionAndRotation(go.transform.position, go.transform.rotation);
                t.localScale = go.transform.localScale;
                t.SetSiblingIndex(go.transform.GetSiblingIndex());
                replacement.name = go.name;

                created.Add(replacement);
                Undo.DestroyObjectImmediate(go);
                replaced++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            if (created.Count > 0) Selection.objects = created.ToArray();
            if (replaced > 0) EditorSceneManager.MarkAllScenesDirty();

            string text = replaced + " prop(s) replaced.";
            if (skipped.Count > 0) text += "\nSkipped:\n  - " + string.Join("\n  - ", skipped);
            Debug.Log(LogTag + text);
            Dialog("Replace props", text);
        }

        private static GameObject LoadPrefabByName(string name)
        {
            string path = FindAssetPath(name + " t:Prefab", p => Path.GetFileNameWithoutExtension(p) == name);
            return path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
        }

        private static string FindAssetPath(string filter, Func<string, bool> accept)
        {
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (accept == null || accept(path)) return path;
            }
            return null;
        }

        private static void Dialog(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
