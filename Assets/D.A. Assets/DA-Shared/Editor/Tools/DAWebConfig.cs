using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.Tools
{
    public class DAWebConfig
    {
        public static WebConfig WebConfig => _webConfig;
        private static WebConfig _webConfig = default;

        internal static bool IsWebConfigAvailable => _configAvailable;
        private static bool _configAvailable = false;

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
#endif
        private static void OnScriptsReload()
        {
            if (Application.isPlaying)
                return;

            _ = GetWebConfig();
        }

        private static async Task GetWebConfig()
        {
            await Task.Delay(100);
            //TODO: ThreadAbortException: Thread was being aborted.
            try
            {
                Thread t = new Thread(() =>
                {
                    string url = "https://da-assets.github.io/site/files/webConfig.json";
                    string json = new WebClient().DownloadString(url);
                    _webConfig = JsonUtility.FromJson<WebConfig>(json);
                });

                t.Start();
            }
            catch (WebException ex)
            {
                Debug.LogException(ex);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _configAvailable = true;
            }
        }
    }

    [Serializable]
    public struct WebConfig
    {
        [SerializeField] List<Asset> assets;
        public List<Asset> Assets => assets;
    }

    [Serializable]
    public struct Asset
    {
        [SerializeField] string name;
        [SerializeField] AssetType assetType;
        [SerializeField] int oldVersionDaysCount;
        [SerializeField] List<AssetVersion> versions;

        public string Name => name;
        public AssetType Type => assetType;
        public int OldVersionDaysCount => oldVersionDaysCount;
        public List<AssetVersion> Versions => versions;
    }

    [Serializable]
    public struct AssetVersion
    {
        [SerializeField] string version;
        [SerializeField] VersionType versionType;
        [SerializeField] string releaseDate;
        [SerializeField] string description;

        public string Version => version;
        public VersionType VersionType => versionType;
        public string ReleaseDate => releaseDate;
        public string Description => description;
    }

    public enum AssetType
    {
        fcu = 1,
        dab = 2,
        uitk = 3,
        dal = 4,
    }

    public enum VersionType
    {
        stable = 0,
        beta = 1,
        buggy = 2
    }
}
