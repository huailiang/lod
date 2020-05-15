using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LodEditor
{

    public class LodUtil
    {

        public enum Direct
        {
            Forward,
            Back,
            Left,
            Right
        };

        private static Camera _camera;

        public static Camera camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = Camera.main;
                }
                return _camera;
            }
        }

        internal const string prefix = "Assets/Resources/LOD/";
        internal const string pref = "Assets/Resources/Prefabs/";
        internal static int rand;

        public static Bounds BuildBounds(GameObject go)
        {
            Renderer[] renders = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            int len = renders.Length;
            Bounds[] bounds = new Bounds[len];
            Vector3 center = Vector3.zero;
            for (int i = 0; i < len; i++)
            {
                center += renders[i].bounds.center;
                bounds[i] = renders[i].bounds;
            }
            center /= len;
            Bounds b = new Bounds(center, Vector3.zero);
            for (int i = 0; i < len; i++)
            {
                b.Encapsulate(bounds[i]);
            }
            b.center = b.center;
            return b;
        }

        public static GameObject CreateRole(LodNode node, int numLod, bool useFbx)
        {
            var go = GameObject.Find(node.prefab);
            if (go == null)
            {
                go = new GameObject(node.prefab);
                go.transform.position = Vector3.zero;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                for (int i = 0; i < numLod; i++)
                {
                    string name = node.prefab + "_LOD" + i;
                    string path = useFbx ? node.dir + "/" + name + ".fbx" : pref + node.prefab + ".prefab";
                    var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var child = GameObject.Instantiate(obj);
                    child.transform.parent = go.transform;
                    child.name = name;
                    child.transform.position = new Vector3(useFbx ? 8 * rand++ : i * 2, 0, 0);
                }
            }
            return go;
        }

        public static void UpdateCamera(float screenPercent, GameObject go, Direct direct)
        {
            if (go != null)
            {
                Selection.activeGameObject = go;
                Bounds b = BuildBounds(go);
                float height = b.extents.y;
                float h_fov = camera.fieldOfView / 2;
                float tan_v = Mathf.Tan(h_fov / Mathf.Rad2Deg);
                float dist = height / (tan_v * screenPercent);
                Vector3 dir = go.transform.forward;
                if (direct == Direct.Back)
                {
                    dir = new Vector3(-dir.x, dir.y, -dir.z);
                }
                else if (direct == Direct.Right)
                {
                    dir = go.transform.right;
                }
                else if (direct == Direct.Left)
                {
                    dir = go.transform.right;
                    dir = new Vector3(-dir.x, dir.y, -dir.z);
                }
                Vector3 camera_pos = b.center + dir.normalized * dist;
                camera.transform.position = camera_pos;
                camera.transform.LookAt(b.center);
            }
        }
        public static void ReLoad(List<LODAsset> lods, LodNode node)
        {
            var go = GameObject.Find(node.prefab);
            if (go) GameObject.DestroyImmediate(go);
            go = CreateRole(node, lods.Count, false);
            for (int i = 0; i < lods.Count; i++)
            {
                string name = node.prefab + "_LOD" + i;
                var tf = go.transform.Find(name);
                if (tf) lods[i].Drop(tf.gameObject);
                string pat = prefix + node.prefab;
                lods[i].LoadMesh(pat, i);
                if (tf) lods[i].Drop(tf.gameObject);
            }
            camera.transform.position = new Vector3(1.8f, 1, 3.0f);
        }

        public static bool CheckBoneValid(List<LODAsset> lods, out string msg)
        {
            msg = "";
            for (int i = lods.Count - 1; i > 0; i--)
            {
                int len = lods[0].renders.Length;
                for (int j = 0; j < len; j++)
                {
                    bool cont = ContainsBone(lods[i - 1].renders[j], lods[i].renders[j], out var error);
                    if (!cont)
                    {
                        msg = "lod " + i + " render: " + lods[i].renders[j].name + " bone: " + error;
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool ContainsBone(SkinnedMeshRenderer r1, SkinnedMeshRenderer r2, out string error)
        {
            var b1 = r1.bones.Select(x => x.name);
            var b2 = r2.bones.Select(x => x.name);
            bool ret = true;
            error = "";
            foreach (var b in b2)
            {
                if (!b1.Contains(b))
                {
                    ret = false;
                    error = b;
                    break;
                }
            }
            return ret;
        }

        public static void AttachCollider(GameObject go)
        {
            Bounds b = BuildBounds(go);
            b.center -= go.transform.position;
            Debug.Log("bounds: " + b + " " + b.size);
            var c = go.GetComponent<BoxCollider>();
            if (c == null)
            {
                c = go.AddComponent<BoxCollider>();
            }
            c.center = b.center;
            c.size = b.size;
        }

        public static Vector3 ScreenPos(Vector3 worldPos)
        {
            // 两种计算方式是等价的, UNITY NDC范围是[-1,1]
            Vector3 cam_p = camera.worldToCameraMatrix.MultiplyPoint(worldPos);
            Vector3 proj_p = camera.projectionMatrix.MultiplyPoint(cam_p);

            Vector3 cull_p = camera.cullingMatrix.MultiplyPoint(worldPos);
            Debug.Log("proj pos: " + proj_p + " cull pos: " + cull_p);
            return cull_p;
        }

        public static float ScreenPercent(Bounds b)
        {
            Vector3 p1 = b.center + new Vector3(0, b.extents.y, 0);
            Vector3 p2 = b.center - new Vector3(0, b.extents.y, 0);
            p1 = camera.cullingMatrix.MultiplyPoint(p1);
            p2 = camera.cullingMatrix.MultiplyPoint(p2);
            return Mathf.Abs(p1.y - p2.y) / 2;
        }

        public static bool MakeLodEnv()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                GraphicsSettings.renderPipelineAsset = null;
                var light = GameObject.FindObjectOfType<Light>();
                light.transform.localEulerAngles = new Vector3(100, 0, 0);
                return true;
            }
        }

        public static void Add<T>(ref T[] arr, T item)
        {
            if (arr != null)
            {
                T[] narr = new T[arr.Length + 1];
                for (int i = 0; i < arr.Length; i++)
                {
                    narr[i] = arr[i];
                }
                narr[arr.Length] = item;
                arr = narr;
            }
            else
            {
                arr = new T[1];
                arr[0] = item;
            }
        }

        public static T[] Remv<T>(T[] arr, int idx)
        {
            if (arr.Length > idx)
            {
                T[] narr = new T[arr.Length - 1];
                for (int i = 0; i < idx; i++)
                {
                    narr[i] = arr[i];
                }
                for (int i = idx + 1; i < arr.Length; i++)
                {
                    narr[i - 1] = arr[i];
                }
                return narr;
            }
            else
            {
                return arr;
            }
        }

    }
}