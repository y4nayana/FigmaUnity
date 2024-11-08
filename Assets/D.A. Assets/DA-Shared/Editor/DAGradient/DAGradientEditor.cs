using DA_Assets.DAI;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.DAG
{
    [CustomEditor(typeof(DAGradient)), CanEditMultipleObjects]
    public class DAGradientEditor : DAEditor<DAGradientEditor, DAGradient, BlackInspector>
    {
        public override void OnInspectorGUI()
        {
            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Vertical,
                Style = gui.ColoredStyle.Background,
                Body = () =>
                {
                    DrawAssetLogo();

                    gui.SerializedPropertyField<DAGradient>(serializedObject, x => x.Gradient, false);
                    gui.Space5();
                    gui.SerializedPropertyField<DAGradient>(serializedObject, x => x.BlendMode);
                    gui.Space5();
                    gui.SerializedPropertyField<DAGradient>(serializedObject, x => x.Intensity);
                    gui.Space5();
                    gui.SerializedPropertyField<DAGradient>(serializedObject, x => x.Angle);

                    Footer.DrawFooter();
                }
            });
        }

        private void DrawAssetLogo()
        {
            GUILayout.BeginVertical(gui.Data.Resources.DAGradientLogo, gui.ColoredStyle.Logo);
            gui.Space60();
            GUILayout.EndVertical();
        }
    }
}