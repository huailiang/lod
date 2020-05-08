using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XEditor
{
    public class RoleLodWindow : EditorWindow
    {

        [MenuItem(@"Tools/RoleLOD")]
        static void AnimExportTool()
        {
            var win = GetWindow(typeof(RoleLodWindow));
            string iconPat = "Assets/Editor/LOD/ico.png";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPat);
            win.titleContent = new GUIContent(" role lod preview window", icon);
            win.Show();
        }

        private int m_SelectedLODSlider = -1;
        private int m_SelectedLOD = -1;
        private int m_NumberOfLODs = 3;
        private float m_cameraPercent = 0.8f;

        private List<LODAsset> m_LODs;

        private int activeLOD
        {
            get { return m_SelectedLOD; }
        }

        private void OnEnable()
        {
            if (m_LODs == null)
            {
                m_LODs = new List<LODAsset>();
                for (int i = 0; i < m_NumberOfLODs; i++)
                {
                    LODAsset oDAsset = new LODAsset();
                    oDAsset.screenRelativeHeight = (m_NumberOfLODs - i) * 0.25f;
                    m_LODs.Add(oDAsset);
                }
            }
        }

        private void OnGUI()
        {
            var initiallyEnabled = GUI.enabled;
            if (m_SelectedLOD >= m_NumberOfLODs)
            {
                m_SelectedLOD = m_NumberOfLODs - 1;
            }
            GUILayout.BeginVertical();
            GUILayout.Space(28);

            var sliderBarPosition = GUILayoutUtility.GetRect(0, LODGUI.kSliderBarHeight, GUILayout.ExpandWidth(true));
            var lods = LODGUI.CreateLODInfos(m_NumberOfLODs, sliderBarPosition,
                  i => string.Format("LOD {0}", i),
                  i => m_LODs[i].screenRelativeHeight);

            DrawLODLevelSlider(sliderBarPosition, lods);
            GUILayout.Space(LODGUI.kSliderBarBottomMargin);

            if (QualitySettings.lodBias != 1.0f)
                EditorGUILayout.HelpBox(string.Format("Active LOD bias is {0:0.0#}. Distances are adjusted accordingly.", QualitySettings.lodBias), MessageType.Warning);

            GUILayout.Space(8);
            GUILayout.Label(m_SelectedLOD < 0 ? "Culled" : "LOD " + m_SelectedLOD, LODGUI.selectStyle);
            GUILayout.Space(8);
            if (m_SelectedLOD >= 0)
            {
                m_LODs[m_SelectedLOD].GUI();
            }
            GUILayout.EndVertical();
        }


        private readonly int m_LODSliderId = "LODSliderIDHash".GetHashCode();
        private readonly int m_CameraSliderId = "LODCameraIDHash".GetHashCode();
        private void DrawLODLevelSlider(Rect sliderPosition, List<LODGUI.LODInfo> lods)
        {
            int sliderId = GUIUtility.GetControlID(m_LODSliderId, FocusType.Passive);
            int camerId = GUIUtility.GetControlID(m_CameraSliderId, FocusType.Passive);
            Event evt = Event.current;
            switch (evt.GetTypeForControl(sliderId))
            {
                case EventType.Repaint:
                    {
                        LODGUI.DrawLODSlider(sliderPosition, lods, activeLOD);
                        break;
                    }
                case EventType.MouseDown:
                    {
                        if (evt.button == 1 && sliderPosition.Contains(evt.mousePosition)) // right click 
                        {
                            float cameraPercent = LODGUI.GetCameraPercent(evt.mousePosition, sliderPosition);
                            var pm = new GenericMenu();
                            if (lods.Count >= 8)
                            {
                                pm.AddDisabledItem(EditorGUIUtility.TrTextContent("Insert Before"));
                            }
                            else
                            {
                                pm.AddItem(EditorGUIUtility.TrTextContent("Insert Before"), false,
                                    new LODAction(lods, cameraPercent, evt.mousePosition, m_LODs, InsertLod).InsertLOD);
                            }

                            // culled region
                            bool disabledRegion = !(lods.Count > 0 && lods[lods.Count - 1].RawScreenPercent < cameraPercent);

                            if (disabledRegion)
                                pm.AddDisabledItem(EditorGUIUtility.TrTextContent("Delete"));
                            else
                                pm.AddItem(EditorGUIUtility.TrTextContent("Delete"), false,
                                    new LODAction(lods, cameraPercent, evt.mousePosition, m_LODs, DeletedLOD).DeleteLOD);
                            pm.ShowAsContext();

                            bool selected = false;
                            foreach (var lod in lods)
                            {
                                if (lod.m_RangePosition.Contains(evt.mousePosition))
                                {
                                    m_SelectedLOD = lod.LODLevel;
                                    selected = true;
                                    break;
                                }
                            }
                            if (!selected) m_SelectedLOD = -1;
                            evt.Use();
                            break;
                        }

                        // edge buttons overflow by 5 pixels
                        var barPosition = sliderPosition;
                        barPosition.x -= 5;
                        barPosition.width += 10;

                        if (barPosition.Contains(evt.mousePosition))
                        {
                            evt.Use();
                            GUIUtility.hotControl = sliderId;
                            bool clickedButton = false;
                            lods.OrderByDescending(x => x.LODLevel);
                            foreach (var lod in lods)
                            {
                                if (lod.m_ButtonPosition.Contains(evt.mousePosition))
                                {
                                    m_SelectedLODSlider = lod.LODLevel;
                                    clickedButton = true;
                                    m_cameraPercent = lod.RawScreenPercent + 0.001f;
                                    // Bias by 0.1% so that there is no skipping when sliding
                                    BeginLODDrag();
                                    break;
                                }
                            }
                            if (!clickedButton)
                            {
                                foreach (var lod in lods)
                                {
                                    if (lod.m_RangePosition.Contains(evt.mousePosition))
                                    {
                                        m_SelectedLODSlider = -1;
                                        m_SelectedLOD = lod.LODLevel;
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == sliderId && m_SelectedLODSlider >= 0 && lods[m_SelectedLODSlider] != null)
                        {
                            evt.Use();
                            var cameraPercent = LODGUI.GetCameraPercent(evt.mousePosition, sliderPosition);
                            // Bias by 0.1% so that there is no skipping when sliding
                            LODGUI.SetSelectedLODLevelPercentage(cameraPercent - 0.001f, m_SelectedLODSlider, lods);
                            m_LODs[m_SelectedLODSlider].screenRelativeHeight = cameraPercent;
                            UpdateLODDrag();
                        }
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == sliderId)
                        {
                            GUIUtility.hotControl = 0;
                            m_SelectedLODSlider = -1;
                            EndLODDrag();
                            evt.Use();
                        }
                        break;
                    }
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        var lodLevel = -2;
                        foreach (var lod in lods)
                        {
                            if (lod.m_RangePosition.Contains(evt.mousePosition))
                            {
                                lodLevel = lod.LODLevel;
                                break;
                            }
                        }
                        if (lodLevel >= 0)
                        {
                            m_SelectedLOD = lodLevel;
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            if (DragAndDrop.objectReferences.Count() > 0)
                            {
                                if (evt.type == EventType.DragPerform)
                                {
                                    var selectedGameObjects = from go in DragAndDrop.objectReferences
                                                              where go as GameObject != null
                                                              select go as GameObject;
                                    if (selectedGameObjects.Count() > 0)
                                    {
                                        var go = selectedGameObjects.First();
                                        if (go != null)
                                        {
                                            m_LODs[m_SelectedLOD].Drop(go);
                                            UpdateBehavic();
                                            Debug.Log("drop lod: " + m_SelectedLOD + " " + go.name);
                                        }
                                    }
                                    DragAndDrop.AcceptDrag();
                                }
                                evt.Use();
                            }
                        }
                        break;
                    }
                case EventType.DragExited:
                    {
                        evt.Use();
                        break;
                    }
            }

            var cameraRect = LODGUI.CalcLODButton(sliderPosition, LODGUI.DelinearizeScreenPercentage(m_cameraPercent));
            var cameraIconRect = new Rect(cameraRect.center.x - 15, cameraRect.y - 25, 32, 32);
            var cameraLineRect = new Rect(cameraRect.center.x - 1, cameraRect.y, 2, cameraRect.height);
            var cameraPercentRect = new Rect(cameraIconRect.center.x - 5, cameraLineRect.yMax, 35, 20);

            switch (evt.GetTypeForControl(camerId))
            {
                case EventType.Repaint:
                    {
                        var colorCache = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(colorCache.r, colorCache.g, colorCache.b, 0.8f);
                        LODGUI.Styles.m_LODCameraLine.Draw(cameraLineRect, false, false, false, false);
                        GUI.backgroundColor = colorCache;
                        GUI.Label(cameraIconRect, LODGUI.Styles.m_CameraIcon, GUIStyle.none);
                        LODGUI.Styles.m_LODSliderText.Draw(cameraPercentRect, String.Format("{0:0}%", Mathf.Clamp01(m_cameraPercent) * 100.0f), false, false, false, false);
                        break;
                    }
                case EventType.MouseDown:
                    {
                        if (cameraIconRect.Contains(evt.mousePosition))
                        {
                            evt.Use();
                            var cameraPercent = LODGUI.GetCameraPercent(evt.mousePosition, sliderPosition);

                            UpdateSelectedLODFromCamera(lods, cameraPercent);
                            GUIUtility.hotControl = camerId;
                            BeginLODDrag();
                        }
                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == camerId)
                        {
                            evt.Use();
                            m_cameraPercent = LODGUI.GetCameraPercent(evt.mousePosition, sliderPosition);
                            UpdateSelectedLODFromCamera(lods, m_cameraPercent);
                            UpdateLODDrag();
                        }
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == camerId)
                        {
                            EndLODDrag();
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    }
            }
        }

        private void UpdateSelectedLODFromCamera(IEnumerable<LODGUI.LODInfo> lods, float cameraPercent)
        {
            bool find = false;
            foreach (var lod in lods)
            {
                if (cameraPercent > lod.RawScreenPercent)
                {
                    m_SelectedLOD = lod.LODLevel;
                    find = true;
                    break;
                }
            }
            if (!find)
            {
                m_SelectedLOD = -1;
            }
        }


        private void BeginLODDrag()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null)
                return;

            UpdateBehavic();
            SceneView.RepaintAll();
        }

        private void UpdateLODDrag()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null)
                return;

            UpdateBehavic();
            SceneView.RepaintAll();
        }

        private void EndLODDrag()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null)
                return;
            HierarchyProperty.ClearSceneObjectsFilter();
        }

        private void DeletedLOD()
        {
            m_SelectedLOD--;
            m_NumberOfLODs--;
        }

        private void InsertLod()
        {
            m_NumberOfLODs++;
        }

        private void UpdateBehavic()
        {
            if (m_SelectedLOD >= 0)
            {
                GameObject go = m_LODs[m_SelectedLOD]?.go;
                LodUtil.UpdateCamera(m_cameraPercent, go);
                for (int i = 0; i < m_LODs.Count; i++)
                {
                    if (m_LODs[i].go != null && m_LODs[i].go != go)
                    {
                        m_LODs[i].go.SetActive(false);
                    }
                }
                go.SetActive(true);
            }
            else
            {
                foreach (var it in m_LODs)
                {
                    if (it.go != null)
                    {
                        it.go.SetActive(false);
                    }
                }
            }
        }

    }
}