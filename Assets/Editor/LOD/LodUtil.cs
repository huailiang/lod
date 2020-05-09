using UnityEditor;
using UnityEngine;

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
    
}
