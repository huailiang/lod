using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace XEditor
{
    /*
     * 这里生成的mesh的时候， 对骨骼的matrix进行排序和重定向
     * 避免运行时需要再次绑定骨骼，以节省不必要的开销， 特别是skinedmeshRender绑定骨骼特别多的情况
     */

    public class MeshExport
    {

        private static SkinnedMeshRenderer[] parts;

        private static Mesh CopyMesh(SkinnedMeshRenderer renderer)
        {
            var mesh = Object.Instantiate<Mesh>(renderer.sharedMesh);
            mesh.name = renderer.sharedMesh.name;
            return mesh;
        }

        class Bone
        {
            public string name;
            public int flag;
            public Transform tf;
        }

        public static void SortBone(LODAsset[] lods)
        {
            for (int i = 0; i < lods[0].renders.Length; i++)
            {
                List<Bone> list = new List<Bone>();
                var render = lods[0].renders[i];
                for (int j = 0; j < render.bones.Length; j++)
                {
                    list.Add(new Bone() { name = render.bones[j].name, flag = -1, tf = render.bones[j] });
                }
                int g_idx = 0;
                for (int j = lods.Length - 1; j >= 0; j--)
                {
                    render = lods[j].renders[i];
                    foreach (var b in render.bones)
                    {
                        var bone = list.Find(x => x.name == b.name);
                        if (bone.flag < 0) bone.flag = g_idx++;
                    }
                }

                Matrix4x4[] obind = render.sharedMesh.bindposes;
                Matrix4x4[] nbind = new Matrix4x4[obind.Length];
                for (int j = 0; j < obind.Length; j++)
                {
                    nbind[list[j].flag] = obind[j];
                }
                var weights = render.sharedMesh.boneWeights;
                var nweights = new BoneWeight[weights.Length];
                for (int j = 0; j < weights.Length; j++)
                {
                    int flag = list[j].flag;
                    nweights[j] = weights[j];
                    nweights[flag].boneIndex0 = weights[j].boneIndex0;
                    nweights[flag].boneIndex1 = weights[j].boneIndex1;
                    nweights[flag].boneIndex2 = weights[j].boneIndex2;
                    nweights[flag].boneIndex3 = weights[j].boneIndex3;
                }
                render.sharedMesh.bindposes = nbind;
                list.Sort((x, y) => x.flag.CompareTo(y.flag));
                render.bones = list.Select(x => x.tf).ToArray();
                lods[0].boneInfo = null;
            }
        }

        public static void Export(LODAsset[] lods)
        {
            parts = lods[0].go.GetComponentsInChildren<SkinnedMeshRenderer>();
            string name = lods[0].go.name;
            name = name.Substring(0, name.LastIndexOf("_LOD"));
            // lod0 export directly
            for (int i = 0; i < parts.Length; i++)
            {
                var mesh = CopyMesh(parts[i]);
                Save(name, 0, mesh);
            }
            // reculcate mesh weights & bindpose
            for (int i = 1; i < lods.Length; i++)
            {
                var ps = lods[i].go.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var render in ps)
                {
                    if (Recalcute(render, out var mesh))
                        Save(name, i, mesh);
                }
            }
            AssetDatabase.Refresh();
        }


        private static bool Recalcute(SkinnedMeshRenderer renderer, out Mesh mesh)
        {
            string name = renderer.name;
            int part = -1;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].name == name)
                {
                    part = i;
                }
            }
            if (part >= 0)
            {
                Dictionary<int, int> map = new Dictionary<int, int>();
                mesh = CopyMesh(renderer);
                for (int i = 0; i < renderer.bones.Length; i++)
                {
                    string bone = renderer.bones[i].name;
                    int idx = IndexBone(part, bone);
                    if (idx >= 0)
                    {
                        map.Add(i, idx);
                        mesh.bindposes[i] = parts[part].sharedMesh.bindposes[idx];
                    }
                }
                //  ReculMeshWeights(map, mesh);
                return true;
            }
            mesh = null;
            return false;
        }


        // error occur if boneindex overange
        private static void ReculMeshWeights(Dictionary<int, int> map, Mesh mesh)
        {
            var weights = mesh.boneWeights;
            BoneWeight[] boneWeights = new BoneWeight[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                boneWeights[i] = weights[i];
                boneWeights[i].boneIndex0 = map[weights[i].boneIndex0];
                boneWeights[i].boneIndex1 = map[weights[i].boneIndex1];
                boneWeights[i].boneIndex2 = map[weights[i].boneIndex2];
                boneWeights[i].boneIndex3 = map[weights[i].boneIndex3];
            }
            mesh.boneWeights = boneWeights;
            mesh.RecalculateBounds();
        }

        private static int IndexBone(int idx, string bone)
        {
            var bones = parts[idx].bones;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].name == bone)
                {
                    return i;
                }
            }
            return -1;
        }


        private static void Save(string name, int level, Mesh mesh)
        {
            string part = mesh.name;
            string dir = "Assets/" + name + "/lod" + level;
            string file = part + ".mesh";
            if(Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var path = Path.Combine(dir, file);
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.ImportAsset(path);
        }

    }

}