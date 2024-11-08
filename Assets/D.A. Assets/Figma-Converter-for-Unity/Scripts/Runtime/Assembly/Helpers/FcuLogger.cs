using DA_Assets.Logging;
using System.Collections;

namespace DA_Assets.FCU
{
    public class FcuLogger
    {
        public static void Debug(object log, FcuLogType logType = FcuLogType.Default)
        {
            switch (logType)
            {
                case FcuLogType.Default:
                    if (!FcuDebugSettings.Settings.HasFlag(FcuDebugSettingsFlags.LogDefault))
                        return;
                    break;
                case FcuLogType.SetTag:
                    if (!FcuDebugSettings.Settings.HasFlag(FcuDebugSettingsFlags.LogSetTag))
                        return;
                    break;
                case FcuLogType.IsDownloadable:
                    if (!FcuDebugSettings.Settings.HasFlag(FcuDebugSettingsFlags.LogIsDownloadable))
                        return;
                    break;
                case FcuLogType.Transform:
                    if (!FcuDebugSettings.Settings.HasFlag(FcuDebugSettingsFlags.LogTransform))
                        return;
                    break;
                case FcuLogType.GameObjectDrawer:
                    if (!FcuDebugSettings.Settings.HasFlag(FcuDebugSettingsFlags.LogGameObjectDrawer))
                        return;
                    break;
                case FcuLogType.HashGenerator:
                    if (!FcuDebugSettings.Settings.HasFlag(FcuDebugSettingsFlags.LogHashGenerator))
                        return;
                    break;
                case FcuLogType.ComponentDrawer:
                    if (!FcuDebugSettings.Settings.HasFlag(FcuDebugSettingsFlags.LogComponentDrawer))
                        return;
                    break;
                case FcuLogType.Error:
                    UnityEngine.Debug.LogError(log);
                    return;
            }


            UnityEngine.Debug.Log(log);
        }
        public static bool WriteLogBeforeApiTimeout(ref int requestCount, ref int remainingTime, string log)
        {
            if (requestCount != 0 && requestCount % FcuConfig.Instance.ApiRequestsCountLimit == 0)
            {
                if (remainingTime > 0)
                {
                    DALogger.Log(log);
                    remainingTime -= 10;
                }
                else if (remainingTime == 0)
                {
                    DALogger.Log(log);
                }
            }

            return remainingTime > 0;
        }

        public static bool WriteLogBeforeEqual(ICollection list1, ICollection list2, FcuLocKey locKey, int count1, int count2, ref int tempCount)
        {
            if (list1.Count != list2.Count)
            {
                if (tempCount != list1.Count)
                {
                    tempCount = list1.Count;
                    DALogger.Log(locKey.Localize(count1, count2));
                }

                return true;
            }

            if (tempCount != list2.Count)
            {
                DALogger.Log(locKey.Localize(count1, count2));
            }

            return false;
        }

        public static bool WriteLogBeforeEqual(ref int count1, ref int count2, string log, ref int tempCount)
        {
            if (count1 != count2)
            {
                if (tempCount != count1)
                {
                    tempCount = count1;
                    DALogger.Log(log);
                }

                return true;
            }

            if (tempCount != count2)
            {
                DALogger.Log(log);
            }

            return false;
        }

        public static bool WriteLogBeforeEqual(ICollection list1, ICollection list2, FcuLocKey locKey, ref int tempCount)
        {
            if (list1.Count != list2.Count)
            {
                if (tempCount != list1.Count)
                {
                    tempCount = list1.Count;
                    DALogger.Log(locKey.Localize(list1.Count, list2.Count));
                }

                return true;
            }

            if (tempCount != list2.Count)
            {
                DALogger.Log(locKey.Localize(list1.Count, list2.Count));
            }

            return false;
        }
    }
}
