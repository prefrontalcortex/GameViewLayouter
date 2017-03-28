using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace Klak
{
    class GameViewLayouter : EditorWindow
    {
        #region Editor resources

        static GUIContent _textViewCount = new GUIContent("Number of Screens");

        static GUIContent[] _viewLabels = {
            new GUIContent("Screen 1"), new GUIContent("Screen 2"),
            new GUIContent("Screen 3"), new GUIContent("Screen 4"),
            new GUIContent("Screen 5"), new GUIContent("Screen 6"),
            new GUIContent("Screen 7"), new GUIContent("Screen 8"),
        };

        static GUIContent[] _optionLabels = {
            new GUIContent("None"),
            new GUIContent("Display 1"), new GUIContent("Display 2"),
            new GUIContent("Display 3"), new GUIContent("Display 4"),
            new GUIContent("Display 5"), new GUIContent("Display 6"),
            new GUIContent("Display 7"), new GUIContent("Display 8"),
        };

        static int[] _optionValues = { -1, 0, 1, 2, 3, 4, 5, 6, 7 };

        #endregion

        #region Private variables

        [SerializeField] GameViewLayoutTable _table;

        #endregion

        #region UI methods

        [MenuItem("Window/Game View Layouter/Edit Layout")]
        static void OpenWindow()
        {
            EditorWindow.GetWindow<GameViewLayouter>("Layouter").Show();
        }

        [MenuItem("Window/Game View Layouter/Close All Game Views %#w")]
        static void CloseAllGameViews()
        {
            CloseAllViews();
        }

        void OnGUI()
        {
            var serializedTable = new UnityEditor.SerializedObject(_table);
            
            // Get all screens
            var displayInfo = MonitorHelper.GetDisplays();

            EditorGUILayout.BeginVertical();

            // Screen num box
            var displayInfoAvailable = displayInfo != null && displayInfo.Count > 0;
            
            var viewCountProperty = serializedTable.FindProperty("viewCount");
            if (!displayInfoAvailable)
                EditorGUILayout.PropertyField(viewCountProperty, _textViewCount);
            else
                viewCountProperty.intValue = displayInfo.Count;

            int viewCount = viewCountProperty.intValue;
            
            // View-display table
            var viewTable = serializedTable.FindProperty("viewTable");
            for (var i = 0; i < viewCount; i++)
            {
                EditorGUILayout.IntPopup(
                    viewTable.GetArrayElementAtIndex(i),
                    _optionLabels, _optionValues, _viewLabels[i]
                );
                
                // display monitor info below dropdown
                if(displayInfoAvailable) { 
                    EditorGUILayout.LabelField(displayInfo[i].MonitorArea.ToString(), EditorStyles.miniLabel);
                }
            }
            for(var i = viewCount; i < viewTable.arraySize; i++)
            {
                viewTable.GetArrayElementAtIndex(i).intValue = -1;
            }

            EditorGUILayout.Space();

            // Function buttons
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Layout")) LayoutViews();
            EditorGUILayout.Space();
            if (GUILayout.Button("Close All")) CloseAllViews();
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            serializedTable.ApplyModifiedProperties();
        }

        #endregion

        #region Private properties and methods

        // Retrieve the hidden GameView type.
        static Type GameViewType {
            get { return System.Type.GetType("UnityEditor.GameView,UnityEditor"); }
        }

        // Change the target display of a game view.
        static void ChangeTargetDisplay(EditorWindow view, int displayIndex)
        {
            var serializedObject = new SerializedObject(view);
            var targetDisplay = serializedObject.FindProperty("m_TargetDisplay");
            targetDisplay.intValue = displayIndex;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        // Close all the game views.
        static void CloseAllViews()
        {
            foreach (EditorWindow view in Resources.FindObjectsOfTypeAll(GameViewType))
            {
                view.Close();
            }
        }
                    
        // Send a game view to a given screen.
        static void SendViewToScreen(EditorWindow view, int screenIndex)
        {
            const int kMenuHeight = 22;

            var firstDisplayInfo = MonitorHelper.GetDisplay(0);
            var displayInfo = MonitorHelper.GetDisplay(screenIndex);
            var monitorArea = displayInfo.MonitorArea;
            //monitorArea.left = (int) (monitorArea.left / firstDisplayInfo.scaleFactor);
            //monitorArea.right = (int)(monitorArea.right / firstDisplayInfo.scaleFactor);
            //monitorArea.top = (int)(monitorArea.top / firstDisplayInfo.scaleFactor);
            //monitorArea.bottom = (int)(monitorArea.bottom / firstDisplayInfo.scaleFactor);

            Debug.Log(displayInfo.scaleFactor + " ,  " + firstDisplayInfo.scaleFactor);

            float relativeScaleFactor = displayInfo.scaleFactor / firstDisplayInfo.scaleFactor;

            // we need to get the scaled rect based on the main screen scale factor and the target screen scale factor.
            
            var origin = new Vector2(monitorArea.left, -kMenuHeight);
            var size = new Vector2(monitorArea.right - monitorArea.left, monitorArea.bottom - monitorArea.top + kMenuHeight);
            size.x *= relativeScaleFactor;
            size.y *= relativeScaleFactor;

            // not sure why multiple sets are necessary, but otherwise the menu height offset does not work correctly.
            view.position = new Rect(origin, size);
            view.minSize = view.maxSize = size;
            view.position = new Rect(origin, size);
            view.minSize = view.maxSize = size;
            view.position = new Rect(origin, size);
        }

        // Instantiate and layout game views based on the setting.
        void LayoutViews()
        {
            CloseAllViews();

            var views = MonitorHelper.GetDisplays();
            int viewCount = 0;
            if (views == null || views.Count < 1) viewCount = 7;
            else viewCount = views.Count;

            for (var i = 0; i < viewCount; i++)
            {
                if (_table.viewTable[i] == -1) continue; // "None", display no screen

                var view = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);
                view.Show();

                ChangeTargetDisplay(view, _table.viewTable[i]);
                SendViewToScreen(view, i);
            }
        }

        #endregion
    }
}
