﻿using System.Linq;

namespace DA_Assets.FCU
{
    public class FcuDebugSettings
    {
        private const string FCU_DEBUG_PREFS_KEY = "FCU_DEBUG_FLAGS";
        private static FcuDebugSettingsFlags flags;

        static FcuDebugSettings()
        {
            FcuDebugSettingsFlags[] debugFlags = new FcuDebugSettingsFlags[]
            {
                FcuDebugSettingsFlags.LogDefault,
                FcuDebugSettingsFlags.LogSetTag,
                FcuDebugSettingsFlags.LogIsDownloadable,
                FcuDebugSettingsFlags.LogTransform,
                FcuDebugSettingsFlags.LogGameObjectDrawer,
                FcuDebugSettingsFlags.LogComponentDrawer,
                FcuDebugSettingsFlags.LogHashGenerator
            };

#if UNITY_EDITOR
            flags = (FcuDebugSettingsFlags)UnityEditor.EditorPrefs.GetInt(FCU_DEBUG_PREFS_KEY, (int)debugFlags.Aggregate((acc, flag) => acc | flag));
#endif
        }

        public static FcuDebugSettingsFlags Settings
        {
            get
            {
                return flags;
            }
            set
            {
                if (flags != value)
                {
                    flags = value;
#if UNITY_EDITOR
                    UnityEditor.EditorPrefs.SetInt(FCU_DEBUG_PREFS_KEY, (int)flags);
#endif
                }
            }
        }
    }
}