using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XEditor
{
    public class LODGUI
    {
        public static readonly Color[] kLODColors =
        {
            new Color(0.4831376f, 0.6211768f, 0.0219608f, 1.0f),
            new Color(0.2792160f, 0.4078432f, 0.5835296f, 1.0f),
            new Color(0.2070592f, 0.5333336f, 0.6556864f, 1.0f),
            new Color(0.5333336f, 0.1600000f, 0.0282352f, 1.0f),
            new Color(0.3827448f, 0.2886272f, 0.5239216f, 1.0f),
            new Color(0.8000000f, 0.4423528f, 0.0000000f, 1.0f),
            new Color(0.4486272f, 0.4078432f, 0.0501960f, 1.0f),
            new Color(0.7749016f, 0.6368624f, 0.0250984f, 1.0f)
        };

        public static readonly Color kCulledLODColor = new Color(.4f, 0f, 0f, 1f);

        public const int kSceneLabelHalfWidth = 100;
        public const int kSceneLabelHeight = 45;
        public const int kSceneHeaderOffset = 40;

        public const int kSliderBarTopMargin = 18;
        public const int kSliderBarHeight = 30;
        public const int kSliderBarBottomMargin = 16;

        public const int kRenderersButtonHeight = 60;
        public const int kButtonPadding = 2;
        public const int kDeleteButtonSize = 20;

        public const int kSelectedLODRangePadding = 3;
        public const int kRenderAreaForegroundPadding = 3;

        public class GUIStyles
        {
            public readonly GUIStyle LODSliderBG = "LODSliderBG";
            public readonly GUIStyle LODSliderRange = "LODSliderRange";
            public readonly GUIStyle LODSliderRangeSelected = "LODSliderRangeSelected";
            public readonly GUIStyle LODSliderText = "LODSliderText";
            public readonly GUIStyle LODSliderTextSelected = "LODSliderTextSelected";
            public readonly GUIStyle LODStandardButton = "Button";
            public readonly GUIStyle LODRendererButton = "LODRendererButton";
            public readonly GUIStyle LODRendererAddButton = "LODRendererAddButton";
            public readonly GUIStyle LODRendererRemove = "LODRendererRemove";
            public readonly GUIStyle LODBlackBox = "LODBlackBox";
            public readonly GUIStyle LODCameraLine = "LODCameraLine";

            public readonly GUIStyle LODSceneText = "LODSceneText";
            public readonly GUIStyle LODRenderersText = "LODRenderersText";
            public readonly GUIStyle LODLevelNotifyText = "LODLevelNotifyText";
            public readonly GUIContent CameraIcon = EditorGUIUtility.IconContent("Camera Icon");

            public readonly GUIContent RecalculateBounds = EditorGUIUtility.TrTextContent("Recalculate Bounds", "Recalculate bounds to encapsulate all child renderers.");
            public readonly GUIContent RecalculateBoundsDisabled = EditorGUIUtility.TrTextContent("Recalculate Bounds", "Bounds are already up-to-date.");
            public readonly GUIContent RendersTitle = EditorGUIUtility.TrTextContent("Renderers:");
          }

        private static GUIStyles s_Styles;

        public static GUIStyles Styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new GUIStyles();
                return s_Styles;
            }
        }

        private static GUIStyle _totalStyle, _selectStyle;

        public static GUIStyle totalStyle
        {
            get
            {
                if (_totalStyle == null)
                {
                    _totalStyle = new GUIStyle(GUI.skin.label);
                    _totalStyle.fontStyle = FontStyle.Bold;
                    _totalStyle.normal.textColor = Color.green;
                }
                return _totalStyle;
            }
        }

        public static GUIStyle selectStyle
        {
            get
            {
                if(_selectStyle==null)
                {
                    _selectStyle = new GUIStyle(GUI.skin.label);
                    _selectStyle.fontSize = 22;
                    _selectStyle.normal.textColor = Color.red;
                    _selectStyle.fontStyle = FontStyle.Bold;
                }
                return _selectStyle;
            }
        }

        public static float DelinearizeScreenPercentage(float percentage)
        {
            if (Mathf.Approximately(0.0f, percentage))
                return 0.0f;

            return Mathf.Sqrt(percentage);
        }

        public static float LinearizeScreenPercentage(float percentage)
        {
            return percentage * percentage;
        }


        public class LODInfo
        {
            public Rect m_ButtonPosition;
            public Rect m_RangePosition;

            public LODInfo(int lodLevel, string name, float screenPercentage)
            {
                LODLevel = lodLevel;
                LODName = name;
                RawScreenPercent = screenPercentage;
            }

            public int LODLevel { get; private set; }
            public string LODName { get; private set; }
            public float RawScreenPercent { get; set; }

            public float ScreenPercent
            {
                get { return DelinearizeScreenPercentage(RawScreenPercent); }
                set { RawScreenPercent = LinearizeScreenPercentage(value); }
            }
        }


        public static Rect CalcLODButton(Rect totalRect, float percentage)
        {
            return new Rect(totalRect.x + (Mathf.Round(totalRect.width * (1.0f - percentage))) - 5, totalRect.y, 10, totalRect.height);
        }

        public static Rect GetCulledBox(Rect totalRect, float previousLODPercentage)
        {
            var r = CalcLODRange(totalRect, previousLODPercentage, 0.0f);
            r.height -= 2;
            r.width -= 1;
            r.center += new Vector2(0f, 1.0f);
            return r;
        }

        public static List<LODInfo> CreateLODInfos(int numLODs, Rect area, Func<int, string> nameGen, Func<int, float> heightGen)
        {
            var lods = new List<LODInfo>();
            for (int i = 0; i < numLODs; ++i)
            {
                var lodInfo = new LODInfo(i, nameGen(i), heightGen(i));
                lodInfo.m_ButtonPosition = CalcLODButton(area, lodInfo.ScreenPercent);
                var previousPercentage = i == 0 ? 1.0f : lods[i - 1].ScreenPercent;
                lodInfo.m_RangePosition = CalcLODRange(area, previousPercentage, lodInfo.ScreenPercent);
                lods.Add(lodInfo);
            }
            return lods;
        }

        public static float GetCameraPercent(Vector2 position, Rect sliderRect)
        {
            var percentage = Mathf.Clamp(1.0f - (position.x - sliderRect.x) / sliderRect.width, 0.01f, 1.0f);
            percentage = LODGUI.LinearizeScreenPercentage(percentage);
            return percentage;
        }

        public static void SetSelectedLODLevelPercentage(float newScreenPercentage, int lod, List<LODInfo> lods)
        {
            // Find the lower detail lod... clamp value to stop overlapping slider
            var minimum = 0.0f;
            var lowerLOD = lods.FirstOrDefault(x => x.LODLevel == lods[lod].LODLevel + 1);
            if (lowerLOD != null)
                minimum = lowerLOD.RawScreenPercent;

            // Find the higher detail lod... clamp value to stop overlapping slider
            var maximum = 1.0f;
            var higherLOD = lods.FirstOrDefault(x => x.LODLevel == lods[lod].LODLevel - 1);
            if (higherLOD != null)
                maximum = higherLOD.RawScreenPercent;

            maximum = Mathf.Clamp01(maximum);
            minimum = Mathf.Clamp01(minimum);
            
            lods[lod].RawScreenPercent = Mathf.Clamp(newScreenPercentage, minimum, maximum);
        }

        public static void DrawLODSlider(Rect area, IList<LODInfo> lods, int selectedLevel)
        {
            Styles.LODSliderBG.Draw(area, GUIContent.none, false, false, false, false);
            for (int i = 0; i < lods.Count; i++)
            {
                var lod = lods[i];
                DrawLODRange(lod, i == 0 ? 1.0f : lods[i - 1].RawScreenPercent, i == selectedLevel);
            }

            // Draw the last range (culled)
            DrawCulledRange(area, lods.Count > 0 ? lods[lods.Count - 1].RawScreenPercent : 1.0f);
        }

        private static Rect CalcLODRange(Rect totalRect, float startPercent, float endPercent)
        {
            var startX = Mathf.Round(totalRect.width * (1.0f - startPercent));
            var endX = Mathf.Round(totalRect.width * (1.0f - endPercent));
            return new Rect(totalRect.x + startX, totalRect.y, endX - startX, totalRect.height);
        }

        private static void DrawLODRange(LODInfo currentLOD, float previousLODPercentage, bool isSelected)
        {
            var tempColor = GUI.backgroundColor;
            var startPercentageString = string.Format("{0}\n{1:0}%", currentLOD.LODName, previousLODPercentage * 100);
            if (isSelected)
            {
                var foreground = currentLOD.m_RangePosition;
                foreground.width -= kSelectedLODRangePadding * 2;
                foreground.height -= kSelectedLODRangePadding * 2;
                foreground.center += new Vector2(kSelectedLODRangePadding, kSelectedLODRangePadding);
                Styles.LODSliderRangeSelected.Draw(currentLOD.m_RangePosition, GUIContent.none, false, false, false, false);
                GUI.backgroundColor = kLODColors[currentLOD.LODLevel];
                if (foreground.width > 0)
                    Styles.LODSliderRange.Draw(foreground, GUIContent.none, false, false, false, false);
                Styles.LODSliderText.Draw(currentLOD.m_RangePosition, startPercentageString, false, false, false, false);
            }
            else
            {
                GUI.backgroundColor = kLODColors[currentLOD.LODLevel];
                GUI.backgroundColor *= 0.6f;
                Styles.LODSliderRange.Draw(currentLOD.m_RangePosition, GUIContent.none, false, false, false, false);
                Styles.LODSliderText.Draw(currentLOD.m_RangePosition, startPercentageString, false, false, false, false);
            }
            GUI.backgroundColor = tempColor;
        }

        private static void DrawCulledRange(Rect totalRect, float previousLODPercentage)
        {
            if (Mathf.Approximately(previousLODPercentage, 0.0f)) return;

            var r = GetCulledBox(totalRect, DelinearizeScreenPercentage(previousLODPercentage));
            // Draw the range of a lod level on the slider
            var tempColor = GUI.color;
            GUI.color = kCulledLODColor;
            Styles.LODSliderRange.Draw(r, GUIContent.none, false, false, false, false);
            GUI.color = tempColor;
            // Draw some details for the current marker
            var startPercentageString = string.Format("Culled\n{0:0}%", previousLODPercentage * 100);
            Styles.LODSliderText.Draw(r, startPercentageString, false, false, false, false);
        }
    }
}
