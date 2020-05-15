using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XEditor
{

    public class LODAction
    {
        private readonly float m_Percentage;
        private readonly List<LODGUI.LODInfo> m_LODs;
        private readonly Vector2 m_ClickedPosition;
        private readonly List<LODAsset> m_LODsProperty;

        public delegate void Callback();
        private readonly Callback m_Callback;

        public LODAction(List<LODGUI.LODInfo> lods, float percentage, Vector2 clickedPosition, List<LODAsset> propLODs, Callback callback)
        {
            m_LODs = lods;
            m_Percentage = percentage;
            m_ClickedPosition = clickedPosition;
            m_LODsProperty = propLODs;
            m_Callback = callback;
        }

        public void InsertLOD()
        {
            int insertIndex = -1;
            float screenHeight = 0.1f;
            foreach (var lod in m_LODs)
            {
                if (m_Percentage > lod.RawScreenPercent)
                {
                    insertIndex = lod.LODLevel;
                    screenHeight = lod.ScreenPercent;
                    break;
                }
            }

            LODAsset asset = new LODAsset();
            asset.screenPercentage = Mathf.Max(0.1f, screenHeight - 0.1f);
            if (insertIndex < 0)
            {
                m_LODsProperty.Add(asset);
                insertIndex = m_LODs.Count;
            }
            else
            {
                m_LODsProperty.Insert(insertIndex, asset);
            }

            asset.screenPercentage = m_Percentage;
            m_Callback?.Invoke();
        }

        public void DeleteLOD()
        {
            if (m_LODs.Count <= 0) return;
            
            foreach (var lod in m_LODs)
            {
                string name = string.Format("lod", lod.LODLevel);
                if (lod.m_RangePosition.Contains(m_ClickedPosition) &&
                    EditorUtility.DisplayDialog("Delete LOD", "Are you sure you wish to delete this LOD?", "Yes", "No"))
                {
                    m_LODsProperty.RemoveAt(lod.LODLevel);
                    m_LODs.Remove(lod);
                    m_Callback?.Invoke();
                    break;
                }
            }
        }
    }
}