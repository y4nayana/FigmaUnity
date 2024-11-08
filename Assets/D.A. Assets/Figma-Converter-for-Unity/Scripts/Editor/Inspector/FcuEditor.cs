using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#pragma warning disable IDE0003
#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    [CustomEditor(typeof(FigmaConverterUnity)), CanEditMultipleObjects]
    internal class FcuEditor : DAEditor<FcuEditor, FigmaConverterUnity, BlackInspector>
    {
        public override void OnShow()
        {
            base.OnShow();

            monoBeh.DelegateHolder.SetSpriteRects = SpriteEditorUtility.SetSpriteRects;
            monoBeh.DelegateHolder.ShowDifferenceChecker = ShowDifferenceChecker;
            monoBeh.DelegateHolder.UpdateScrollContent = this.FrameList.UpdateScrollContent;
            monoBeh.DelegateHolder.UpdateScrollContent();
            monoBeh.DelegateHolder.GetGameViewSize = GameViewUtils.GetGameViewSize;
            monoBeh.DelegateHolder.SetGameViewSize = GameViewUtils.SetGameViewSize;

            _ = monoBeh.Authorizer.TryRestoreSession();
        }

        private void ShowDifferenceChecker(PreImportInput data, Action<PreImportOutput> callback)
        {
            this.DifferenceCheckerWindow.SetData(data, callback);
            this.DifferenceCheckerWindow.Show();
        }

        public void DrawBaseOnInspectorGUI()
        {
            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Vertical,
                Body = () => base.OnInspectorGUI()
            });
        }

        public override void OnInspectorGUI()
        {
            if (monoBeh.Settings.MainSettings.WindowMode)
            {
                DrawWindowedGUI();
            }
            else
            {
                DrawGUI(gui.ColoredStyle.Background);
            }
        }

        public void DrawWindowedGUI()
        {
            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Vertical,
                Style = gui.ColoredStyle.Background,
                Body = () =>
                {
                    gui.TopProgressBar(monoBeh.RequestSender.PbarProgress);

                    gui.DrawGroup(new Group
                    {
                        GroupType = GroupType.Horizontal,
                        Body = () =>
                        {
                            this.Header.DrawSmallHeader();

                            gui.Space15();

                            if (gui.SquareButton30x30(new GUIContent(gui.Data.Resources.IconOpen, FcuLocKey.tooltip_open_fcu_window.Localize())))
                            {
                                this.SettingsWindow.Show();
                            }

                            gui.Space5();

                            if (gui.SquareButton30x30(new GUIContent(gui.Data.Resources.IconExpandWindow, FcuLocKey.tooltip_change_window_mode.Localize())))
                            {
                                if (monoBeh.Settings.MainSettings.WindowMode)
                                {
                                    monoBeh.Settings.MainSettings.WindowMode = false;
                                }
                            }
                        }
                    });
                }
            });
        }

        public void DrawGUI(GUIStyle customStyle)
        {
            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Vertical,
                Style = customStyle,
                Body = () =>
                {
                    this.Header.Draw();

                    gui.Space15();

                    gui.DrawGroup(new Group
                    {
                        GroupType = GroupType.Horizontal,
                        Body = () =>
                        {
                            monoBeh.Settings.MainSettings.ProjectUrl = gui.BigTextField(monoBeh.Settings.MainSettings.ProjectUrl, null);

                            gui.Space5();

                            Group gr = new Group();

                            if (monoBeh.Settings.MainSettings.WindowMode)
                            {
                                gr.Style = gui.ColoredStyle.Group5Buttons;
                            }
                            else
                            {
                                gr.Style = gui.ColoredStyle.Group6Buttons;
                            }

                            gr.GroupType = GroupType.Horizontal;
                            gr.Body = () =>
                            {
                                if (gui.SquareButton30x30(new GUIContent(gui.Data.Resources.ImgViewRecent, FcuLocKey.tooltip_recent_projects.Localize())))
                                {
                                    ShowRecentProjectsPopup_OnClick();
                                }

                                gui.Space5();
                                if (gui.SquareButton30x30(new GUIContent(gui.Data.Resources.IconDownload, FcuLocKey.tooltip_download_project.Localize())))
                                {
                                    if (monoBeh.Authorizer.IsAuthed() == false)
                                    {
                                        DALogger.Log(FcuLocKey.log_not_authorized.Localize());
                                    }
                                    else if (monoBeh.Settings.MainSettings.ProjectUrl.IsEmpty())
                                    {
                                        DALogger.Log(FcuLocKey.log_incorrent_project_url.Localize());
                                    }
                                    else
                                    {
                                        monoBeh.EventHandlers.DownloadProject_OnClick();
                                    }
                                }

                                gui.Space5();
                                if (gui.SquareButton30x30(new GUIContent(gui.Data.Resources.IconImport, FcuLocKey.tooltip_import_frames.Localize())))
                                {
                                    monoBeh.EventHandlers.ImportSelectedFrames_OnClick();
                                }

                                gui.Space5();
                                if (gui.SquareButton30x30(new GUIContent(gui.Data.Resources.IconStop, FcuLocKey.tooltip_stop_import.Localize())))
                                {
                                    monoBeh.EventHandlers.StopImport_OnClick();
                                }

                                if (monoBeh.Settings.MainSettings.WindowMode == false)
                                {
                                    gui.Space5();
                                    if (gui.SquareButton30x30(new GUIContent(gui.Data.Resources.IconSettings, FcuLocKey.tooltip_open_settings_window.Localize())))
                                    {
                                        this.SettingsWindow.Show();
                                    }
                                }

                                gui.Space5();
                                if (gui.SquareButton30x30(new GUIContent(gui.Data.Resources.IconExpandWindow, FcuLocKey.tooltip_change_window_mode.Localize())))
                                {
                                    if (monoBeh.Settings.MainSettings.WindowMode)
                                    {
                                        monoBeh.Settings.MainSettings.WindowMode = false;
                                        this.SettingsWindow.CreateTabs();
                                    }
                                    else
                                    {
                                        monoBeh.Settings.MainSettings.WindowMode = true;
                                        this.SettingsWindow.Show();
                                    }
                                }
                            };

                            gui.DrawGroup(gr);
                        }
                    });

                    gui.Space5();

                    if (!monoBeh.InspectorDrawer.SelectableDocument.IsProjectEmpty())
                    {
                        this.FrameList.Draw();
                    }

                    Footer.DrawFooter();
                }
            });
        }

        private void ShowRecentProjectsPopup_OnClick()
        {
            List<RecentProject> recentProjects = monoBeh.ProjectCacher.GetRecentProjects();

            List<GUIContent> options = new List<GUIContent>();

            if (recentProjects.IsEmpty())
            {
                options.Add(new GUIContent(FcuLocKey.label_no_recent_projects.Localize()));
            }
            else
            {
                foreach (RecentProject project in recentProjects)
                {
                    options.Add(new GUIContent(project.Name));
                }
            }

            EditorUtility.DisplayCustomMenu(new Rect(11, 150, 0, 0), options.ToArray(), -1, (userData, ops, selected) =>
            {
                RecentProject recentProject = recentProjects[selected];
                monoBeh.Settings.MainSettings.ProjectUrl = recentProject.Url;
                monoBeh.EventHandlers.DownloadProject_OnClick();
            }, null);
        }

        internal DifferenceCheckerWindow DifferenceCheckerWindow =>
            DifferenceCheckerWindow.GetInstance(this, monoBeh, new Vector2(900, 600), false);

        internal FcuSettingsWindow SettingsWindow =>
            FcuSettingsWindow.GetInstance(this, monoBeh, new Vector2(800, 600), false);

        [SerializeField] HeaderSection headerSection;
        internal HeaderSection Header => monoBeh.Link(ref headerSection, this);

        [SerializeField] FramesSection frameListSection;
        internal FramesSection FrameList => monoBeh.Link(ref frameListSection, this);
    }

    public enum HamburgerMenuId
    {
        MainSettingsKey,
        UnityTextSettingsKey,
        TextMeshSettingsKey,
        PuiSettingsKey,
        MPUIKitSettingsKey,
        FrameListKey,
        AssetsConfigKey,
        AssetToolsKey,
        DebugToolsKey,
        RemoveUnusedSpritesKey,
        ImportEventsKey,
        UnityImageSettingsKey,
        Shapes2DSettingsKey,
        TMFontsConverterKey
    }
}
