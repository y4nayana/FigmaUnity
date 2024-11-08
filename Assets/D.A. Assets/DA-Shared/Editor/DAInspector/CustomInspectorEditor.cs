using UnityEditor;
using UnityEngine;

namespace DA_Assets.DAI
{
    [CustomEditor(typeof(CustomInspector<>), editorForChildClasses: true), CanEditMultipleObjects]
    internal class CustomInspectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Reinit"))
            {
                var customInspector = target as ICustomInspector;

                if (customInspector != null)
                {
                    customInspector.Init();
                }
            }
        }
    }
}
