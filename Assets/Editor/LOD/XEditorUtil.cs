using UnityEditor;
using UnityEngine;

namespace XEditor
{
    
    public class XEditorUtil
    {
        [System.NonSerialized]
        private static GUIStyle _labelStyle = null;


        public static GUIStyle titleLableStyle
        {
            get
            {
                if (_labelStyle == null)
                    _labelStyle = new GUIStyle(EditorStyles.label);
                _labelStyle.fontStyle = FontStyle.Bold;
                _labelStyle.fontSize = 22;
                return _labelStyle;
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