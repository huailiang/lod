using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace XEditor
{

    [CustomEditor(typeof(LodData))]
    public class LodDataEditor : Editor
    {
        private LodData odData;
        private string search;
        private GUIContent icon;

        public LodData Data
        {
            get { return odData; }
        }

        internal const string dataPat = "Assets/Editor/LOD/LodData.asset";

        [MenuItem("Assets/LodData")]
        static void CreateFaceData()
        {
            if (!File.Exists(dataPat))
            {
                LodData od = ScriptableObject.CreateInstance<LodData>();
                AssetDatabase.CreateAsset(od, dataPat);
                AssetDatabase.ImportAsset(dataPat);
                AssetDatabase.Refresh();
            }
        }

        private void OnEnable()
        {
            odData = target as LodData;
            string iconPat = "Assets/Editor/LOD/ico.png";
            var ico = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPat);
            icon = new GUIContent(ico);
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(icon, LODGUI.Styles.LODRendererButton, GUILayout.MaxWidth(32), GUILayout.MaxHeight(32));
            GUILayout.Label("Lod Data", XEditorUtil.titleLableStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            LodNode action = null;
            search = GuiSearch(search);
            if (Data.nodes != null)
            {
                for (int i = 0; i < Data.nodes.Length; i++)
                {
                    var node = Data.nodes[i];
                    if (node.Match(search))
                    {
                        var op = node.GUI();
                        if (op == LodOP.DELETE)
                        {
                            Data.nodes = XEditorUtil.Remv<LodNode>(Data.nodes, i);
                            break;
                        }
                        else if (op == LodOP.DETAIL)
                        {
                            action = node;
                        }
                    }
                }
            }
            GuiButtons();
            GUILayout.EndVertical();
            if (action != null) OpenLodWin(action);
        }

        private string GuiSearch(string value, params GUILayoutOption[] options)
        {
            MethodInfo info = typeof(EditorGUILayout).GetMethod("ToolbarSearchField",
                BindingFlags.NonPublic | BindingFlags.Static, null,
                new System.Type[] { typeof(string), typeof(GUILayoutOption[]) }, null);
            if (info != null)
            {
                value = (string)info.Invoke(null, new object[] { value, options });
            }
            return value;
        }

        private void GuiButtons()
        {
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                XEditorUtil.Add<LodNode>(ref odData.nodes, new LodNode("role"));
            }
            if (GUILayout.Button("Save"))
            {
                odData.Save();
            }
            GUILayout.EndHorizontal();
        }

        private void OpenLodWin(LodNode lod)
        {
            if (LodUtil.MakeLodEnv())
            {
                var win = LodWindow.LodShow();
                win.LoadRole(lod);
                win.UpdateInfo();
            }
        }
    }

}