using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.Tools;
using System;
using System.Linq;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]  
    internal class HeaderSection : MonoBehaviourLinkerEditor<FcuEditor, FigmaConverterUnity, BlackInspector>
    {
        private void DrawStar()
        {
            GUILayout.Box(gui.Data.Resources.ImgStar, gui.ColoredStyle.ImgStar);
        }

        private void RateMeUI()
        {
            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Body = () =>
                {
                    gui.FlexibleSpace();

                    for (int i = 0; i < 5; i++)
                    {
                        DrawStar();

                        if (i != 5)
                        {
                            gui.Space5();
                        }
                    }

                    gui.FlexibleSpace();
                }
            });

            gui.Space15();

            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Vertical,
                Style = gui.ColoredStyle.BoxPanel,
                Body = () =>
                {
                    int dc = GetFirstVersionDaysCount();
                    gui.Label12px(new GUIContent(FcuLocKey.label_rateme_desc.Localize(dc)), GUILayout.ExpandWidth(true));

                    gui.Space5();
                    if (gui.OutlineButton("Don't show", "null", true))
                    {
                        DontShowRateMe_OnClick();
                    }

                    gui.Space5();
                    if (gui.OutlineButton("Open Asset Store", "null", true))
                    {
                        int packageId;

                        if (monoBeh.IsUGUI())
                        {
                            packageId = 198134;
                        }
                        else
                        {
                            packageId = 272042;
                        }

                        Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/" + packageId + "#reviews");

                        DontShowRateMe_OnClick();
                    }
                }
            });
        }

        private void DontShowRateMe_OnClick()
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetInt(FcuConfig.RATEME_PREFS_KEY, 1);
#endif
        }

        public void Draw()
        {
            gui.TopProgressBar(monoBeh.RequestSender.PbarProgress);

            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Body = () =>
                {
                    gui.Space(18);
                    GUILayout.BeginVertical(gui.Data.Resources.FcuLogo, gui.ColoredStyle.Logo);
                    gui.Space30();
                    GUILayout.EndVertical();
                    gui.Space(18);
                }
            });

            if (monoBeh.AssetTools.NeedShowRateMe)
            {
                RateMeUI();
                gui.Space(25);
            }

            UpdateChecker.DrawVersionLine(AssetType.fcu, FcuConfig.Instance.ProductVersion);
#if FCU_EXISTS && FCU_UITK_EXT_EXISTS
            UpdateChecker.DrawVersionLine(AssetType.uitk, FuitkConfig.Instance.ProductVersion);
#endif
            DrawImportInfoLine();
            DrawCurrentProjectName();
        }

        private static int GetFirstVersionDaysCount()
        {
            try
            {
                Asset assetInfo = DAWebConfig.WebConfig.Assets.FirstOrDefault(x => x.Type == AssetType.fcu);
                AssetVersion firstVersion = assetInfo.Versions.First();

                DateTime firstDt = DateTime.ParseExact(firstVersion.ReleaseDate, "MMM d, yyyy", new System.Globalization.CultureInfo("en-US"));

                int dc = (int)Mathf.Abs((float)(DateTime.Now - firstDt).TotalDays);

                return dc;
            }
            catch
            {
                return -1;
            }
        }
        public void DrawSmallHeader()
        {
            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Vertical,
                Body = () =>
                {
                    DrawImportInfoLine();
                    DrawCurrentProjectName();
                }
            });
        }

        private void DrawImportInfoLine()
        {
            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Body = () =>
                {
                    gui.FlexibleSpace();

                    gui.Label10px(new GUIContent($"{Mathf.Round(monoBeh.RequestSender.PbarBytes / 1024)} kB", FcuLocKey.label_kilobytes.Localize()));
                    gui.Space5();
                    gui.Label10px(new GUIContent("—"));

                    string userId = monoBeh.Authorizer.CurrentSession.User.Id.SubstringSafe(10);
                    string userName = monoBeh.Authorizer.CurrentSession.User.Name;

                    if (string.IsNullOrWhiteSpace(userName) == false)
                    {
                        gui.Space5();
                        gui.Label10px(new GUIContent(userName, FcuLocKey.label_user_name.Localize()));
                        gui.Space5();
                        gui.Label10px(new GUIContent("—"));
                    }
                    else if (string.IsNullOrWhiteSpace(userId) == false)
                    {
                        gui.Space5();
                        gui.Label10px(new GUIContent(userId, FcuLocKey.tooltip_user_id.Localize()));
                        gui.Space5();
                        gui.Label10px(new GUIContent("—"));
                    }

                    gui.Space5();
                    gui.Label10px(new GUIContent(monoBeh.Guid, FcuLocKey.tooltip_asset_instance_id.Localize()));
                }
            });
        }

        private void DrawCurrentProjectName()
        {
            string currentProjectName = monoBeh.CurrentProject.ProjectName;

            if (currentProjectName != null)
            {
                gui.DrawGroup(new Group
                {
                    GroupType = GroupType.Horizontal,
                    Body = () =>
                    {
                        gui.FlexibleSpace();
                        gui.Label10px(currentProjectName);
                    }
                });
            }
        }
    }
}