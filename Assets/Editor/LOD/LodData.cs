using UnityEngine;
using UnityEditor;
using System.IO;

namespace LodEditor
{

    public enum ExMesh
    {
        UV1 = 0x01,
        UV2 = 0x02,
        UV3 = 0x04,
        UV4 = 0x08,
        NORMAL = 0x10,
        TANGENT = 0x20,
        COLOR = 0x40,
    }

    public enum LodOP
    {
        NONE,
        DELETE,
        DETAIL
    }

    [System.Serializable]
    public class LodNode
    {
        public string dir, prefab, comment;
        public float[] levels;
        public ExMesh[] format;

        private bool folder;
        private Object obj;

        public LodNode(string pref)
        {
            prefab = pref;
            comment = pref;
            folder = true;
            levels = new float[] { 0.75f, 0.5f, 0.25f };
            format = defaultFmt;
        }

        public LodNode(string pref, float[] les)
        {
            prefab = pref;
            levels = les;
            format = defaultFmt;
        }

        public string desc
        {
            get { return !string.IsNullOrEmpty(comment) ? comment : prefab; }
        }
        
        public ExMesh Format(int level)
        {
            return format[level];
        }

        private ExMesh[] defaultFmt
        {
            get
            {
                return new ExMesh[] { ExMesh.UV1 | ExMesh.NORMAL | ExMesh.COLOR,
                ExMesh.UV1 | ExMesh.NORMAL, ExMesh.UV1 };
            }
        }

        public bool Match(string search)
        {
            if (string.IsNullOrEmpty(search))
                return true;
            return (comment != null && comment.Contains(search)) ||
                (prefab != null && prefab.Contains(search));
        }

        

        public LodOP GUI()
        {
            LodOP lodOP = LodOP.NONE;
            EditorGUILayout.BeginHorizontal();
            folder = EditorGUILayout.Foldout(folder, desc);
            if (GUILayout.Button("X", GUILayout.MaxWidth(36)))
            {
                lodOP= LodOP.DELETE;
            }
            EditorGUILayout.EndHorizontal();
            if (folder)
            {
                obj = EditorGUILayout.ObjectField("fbx", obj, typeof(GameObject), false);
                if (obj == null)
                {
                    var path = dir +"/"+ prefab + "_LOD0.fbx";
                    obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
                else
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    int idx = path.LastIndexOf("/");
                    dir = path.Substring(0, idx);
                    int idx2 = path.LastIndexOf("_LOD");
                    if (idx2 > 0)
                        prefab = path.Substring(idx + 1, idx2 - idx - 1);
                    else
                        EditorGUILayout.HelpBox("fbx is not match lod rules ", MessageType.Error);
                }
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(prefab);
                if (GUILayout.Button("Add", GUILayout.MaxWidth(80)))
                {
                    LodUtil.Add<float>(ref levels, levels[levels.Length - 1] / 2.0f);
                    LodUtil.Add<ExMesh>(ref format, ExMesh.UV1);
                }
                if (GUILayout.Button("Detail", GUILayout.MaxWidth(80)))
                {
                    lodOP = LodOP.DETAIL;
                }
                EditorGUILayout.EndHorizontal();
                comment = EditorGUILayout.TextField("备注: ", comment);
                bool sort = true;
                for (int i = 0; i < levels.Length; i++)
                {
                    bool del = false;
                    EditorGUILayout.BeginHorizontal();
                    levels[i] = EditorGUILayout.FloatField(" - lod" + i, levels[i]);
                    if (format == null || format.Length <= i) format = defaultFmt;
                    format[i] = (ExMesh)EditorGUILayout.EnumFlagsField(format[i], GUILayout.MaxWidth(80));
                    if (GUILayout.Button("X", GUILayout.MaxWidth(36)))
                    {
                        del = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (levels[i] <= 0 || levels[i] >= 1)
                    {
                        EditorGUILayout.HelpBox("lod range [0-1]", MessageType.Error);
                    }
                    if (del)
                    {
                        levels = LodUtil.Remv<float>(levels, i);
                        format = LodUtil.Remv<ExMesh>(format, i);
                        break;
                    }
                    if (i > 0 && levels[i] >= levels[i - 1]) sort = false;
                }
                if (!sort)
                {
                    EditorGUILayout.HelpBox("lod sort invalid", MessageType.Error);
                }
            }
            return lodOP;
        }
    }


    public class LodData : ScriptableObject
    {
        public LodNode[] nodes;

        public bool AddorUpdate(string prefab, float[] levels)
        {
            bool find = false;
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].prefab == prefab)
                    {
                        find = true;
                        nodes[i].levels = levels;
                        break;
                    }
                }
            }
            if (!find)
            {
                LodUtil.Add<LodNode>(ref nodes, new LodNode(prefab, levels));
            }
            return find;
        }

        public void Save()
        {
            Debug.Log("lod data save");
            GenerateBytes();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void GenerateBytes()
        {
            string path =  "Assets/Resources/conf.bytes";
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(fs);
            int cnt = nodes.Length;
            writer.Write(cnt);
            for (int i = 0; i < cnt; i++)
            {
                LodNode node = nodes[i];
                writer.Write(node.prefab);
                writer.Write(node.levels.Length);
                for (int j = 0; j < node.levels.Length; j++)
                {
                    writer.Write(node.levels[j]);
                }
            }
            writer.Flush();
            writer.Close();
            fs.Close();

            AssetDatabase.ImportAsset(path);
        }
    }
}