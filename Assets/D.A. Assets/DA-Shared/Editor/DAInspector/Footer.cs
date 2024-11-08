using DA_Assets.Constants;
using UnityEngine;

namespace DA_Assets.DAI
{
    public class Footer
    {
        public static DAInspector gui => BlackInspector.Instance.Inspector;

        public static void DrawFooter()
        {
            gui.Space30();

            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Flexible = true,
                Body = () =>
                {
                    if (gui.LinkButton(new GUIContent($"made by\n{DAConstants.Publisher}"), false))
                    {
                        Application.OpenURL(DAConstants.SiteLink);
                    }
                }
            });
        }
    }
}
