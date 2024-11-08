using DA_Assets.Constants;
using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Singleton;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/FCU Config")]
    public class FcuConfig : AssetConfig<FcuConfig>
    {
        [SerializeField] List<TagConfig> tags;
        public List<TagConfig> TagConfigs => tags;

        [Header("File names")]
        [SerializeField] string webLogFileName;
        public string WebLogFileName => webLogFileName;

        [Header("Formats")]
        [SerializeField] string dateTimeFormat1;
        public string DateTimeFormat1 => dateTimeFormat1;

        [SerializeField] string dateTimeFormat2;
        public string DateTimeFormat2 => dateTimeFormat2;

        [SerializeField] string dateTimeFormat3;
        public string DateTimeFormat3 => dateTimeFormat3;

        [Header("GameObject names")]
        [SerializeField] string canvasGameObjectName;
        public string CanvasGameObjectName => canvasGameObjectName;
        [SerializeField] string i2LocGameObjectName;
        public string I2LocGameObjectName => i2LocGameObjectName;

        [Header("Values")]
        [SerializeField] int recentProjectsLimit = 20;
        public int RecentProjectsLimit => recentProjectsLimit;

        [SerializeField] int figmaSessionsLimit = 10;
        public int FigmaSessionsLimit => figmaSessionsLimit;

        [SerializeField] int logFilesLimit = 50;
        public int LogFilesLimit => logFilesLimit;

        [SerializeField] int maxRenderSize = 4096;
        public int MaxRenderSize => maxRenderSize;

        [SerializeField] int renderUpscaleFactor = 2;
        public int RenderUpscaleFactor => renderUpscaleFactor;

        [SerializeField] string blurredObjectTag = "UIBlur";
        public string BlurredObjectTag => blurredObjectTag;

        [SerializeField] string blurCameraTag = "BackgroundBlur";
        public string BlurCameraTag => blurCameraTag;

        [SerializeField] char realTagSeparator = '-';
        public char RealTagSeparator => realTagSeparator;

        [Header("Api")]
        [SerializeField] int apiRequestsCountLimit = 2;
        public int ApiRequestsCountLimit => apiRequestsCountLimit;

        [SerializeField] int apiTimeoutSec = 5;
        public int ApiTimeoutSec => apiTimeoutSec;

        [SerializeField] int chunkSizeGetNodes;
        public int ChunkSizeGetNodes => chunkSizeGetNodes;

        [SerializeField] int frameListDepth = 2;
        public int FrameListDepth => frameListDepth;

        [SerializeField] string gFontsApiKey;
        public string GoogleFontsApiKey { get => gFontsApiKey; set => gFontsApiKey = value; }

        [Header("Other")]
        [SerializeField] Sprite whiteSprite32px;
        public Sprite WhiteSprite32px => whiteSprite32px;

        [SerializeField] Sprite missingImageTexture128px;
        public Sprite MissingImageTexture128px => missingImageTexture128px;

        [SerializeField] TextAsset baseClass;
        public TextAsset BaseClass => baseClass;

        [SerializeField] Material imageLinearMaterial;
        public Material ImageLinearMaterial => imageLinearMaterial;

        [SerializeField] VectorMaterials vectorMaterials;
        public VectorMaterials VectorMaterials => vectorMaterials;

        /////////////////////////////////////////////////////////////////////////////////
        //CONSTANTS//////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////

        public const string ProductName = "Figma Converter for Unity";
        public const string ProductNameShort = "FCU";
        public const string DestroyChilds = "Destroy childs";
        public const string SetFcuToSyncHelpers = "Set current FCU to SyncHelpers";
        public const string CompareTwoObjects = "Compare two selected objects";
        public const string DestroyLastImported = "Destroy last imported frames";
        public const string DestroySyncHelpers = "Destroy SyncHelpers";
        public const string CreatePrefabs = "Create Prefabs";
        public const string UpdatePrefabs = "Update Prefabs";
        public const string Create = "Create";
        public const string OptimizeSyncHelpers = "Optimize SyncHelpers";

        public static char HierarchyDelimiter { get; } = '/';
        public static string PARENT_ID { get; } = "603951929:602259738";
        public static char AsterisksChar { get; } = '•';
        public static string DefaultLocalizationCulture { get; } = "en-US";

        public static string RATEME_PREFS_KEY { get; } = "DONT_SHOW_RATEME";
        public static string RECENT_PROJECTS_PREFS_KEY { get; } = "recentProjectsPrefsKey";
        public static string FIGMA_SESSIONS_PREFS_KEY { get; } = "FigmaSessions";

        public static string ClientId => "LaB1ONuPoY7QCdfshDbQbT";
        public static string ClientSecret => "E9PblceydtAyE7Onhg5FHLmnvingDp";
        public static string RedirectUri => "http://localhost:1923/";
        public static string AuthUrl => "https://www.figma.com/api/oauth/token?client_id={0}&client_secret={1}&redirect_uri={2}&code={3}&grant_type=authorization_code";
        public static string OAuthUrl => "https://www.figma.com/oauth?client_id={0}&redirect_uri={1}&scope=file_read&state={2}&response_type=code";

        private static string logPath;
        public static string LogPath
        {
            get
            {
                if (logPath.IsEmpty())
                    logPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");

                logPath.CreateFolderIfNotExists();

                return logPath;
            }
        }

        private static string cachePath;
        public static string CachePath
        {
            get
            {
                if (cachePath.IsEmpty())
                {
                    string tempFolder = Path.GetTempPath();
                    cachePath = Path.Combine(tempFolder, "FcuCache");
                }

                cachePath.CreateFolderIfNotExists();

                return cachePath;
            }
        }
    }
}