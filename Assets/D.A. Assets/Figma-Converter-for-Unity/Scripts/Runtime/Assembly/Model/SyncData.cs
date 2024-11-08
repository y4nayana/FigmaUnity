﻿using DA_Assets.Extensions;
using DA_Assets.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class SyncData : IHaveId
    {
        [SerializeField] string id;
        public string Id { get => id; set => id = value; }

        [SerializeField] FNames names;
        public FNames Names { get => names; set => names = value; }

        [Space]

        [SerializeField] List<FcuTag> tags = new List<FcuTag>();
        public List<FcuTag> Tags { get => tags; set => tags = value; }

        [SerializeField] FcuImageType fcuImageType;
        public FcuImageType FcuImageType { get => fcuImageType; set => fcuImageType = value; }

        [SerializeField] ButtonComponent buttonComponent;
        public ButtonComponent ButtonComponent { get => buttonComponent; set => buttonComponent = value; }

        [SerializeField] List<int> childIndexes = new List<int>();
        public List<int> ChildIndexes { get => childIndexes; set => childIndexes = value; }

        [Space]

        [SerializeField] MonoBehaviour fcu;
        public MonoBehaviour FigmaConverterUnity { get => fcu; set => fcu = value; }

        [SerializeField] GameObject gameObject;
        public GameObject GameObject { get => gameObject; set => gameObject = value; }

        [Space]

        [SerializeField] int hash;
        public int Hash { get => hash; set => hash = value; }

        [SerializeField, HideInInspector] List<FcuHierarchy> hierarchy = new List<FcuHierarchy>();
        public List<FcuHierarchy> Hierarchy { get => hierarchy; set => hierarchy = value; }

#pragma warning disable IDE0052
        [SerializeField] string nameHierarchy;
#pragma warning restore IDE0052
        public void DisplayNameHierarchyInField() => nameHierarchy = this.NameHierarchy;

        public string NameHierarchy
        {
            get
            {
                if (hierarchy.IsEmpty())
                    return null;

                string h = string.Join(FcuConfig.HierarchyDelimiter.ToString(), hierarchy.Select(x => x.Name));
                return h;
            }
        }

        [SerializeField] GameObject rootFrameGO;
        [SerializeField] SyncData rootFrameSD;
        public SyncData RootFrame
        {
            get
            {
                if (rootFrameGO == null)
                {
                    return rootFrameSD;
                }

                SyncHelper sh = rootFrameGO.GetComponent<SyncHelper>();

                if (sh == null || sh.Data == null)
                {
                    return rootFrameSD;
                }
                else
                {
                    return sh.Data;
                }
            }
            set
            {
                if (value?.GameObject != null)
                {
                    rootFrameGO = value.GameObject;
                }

                if (rootFrameGO == null)
                {
                    rootFrameSD = value;
                    return;
                }

                SyncHelper sh = rootFrameGO.GetComponent<SyncHelper>();

                if (sh != null)
                {
                    sh.Data = value;
                }
                else
                {
                    rootFrameSD = value;
                }
            }
        }

        [Clear] public string UitkType { get; set; }

        [Clear] public string HashData { get; set; }

        [Clear] public XmlElement XmlElement { get; set; }

        [Clear] public FObject Parent { get; set; }

        [Clear] public string HashDataTree { get; set; }

        [SerializeField, Clear] int parentIndex;
        public int ParentIndex { get => parentIndex; set => parentIndex = value; }

        [SerializeField, Clear] UguiTransformData uguiTransformData;
        public UguiTransformData UguiTransformData { get => uguiTransformData; set => uguiTransformData = value; }

#if NOVA_UI_EXISTS
        [SerializeField, Clear] NovaTransformData novaTransformData;
        public NovaTransformData NovaTransformData { get => novaTransformData; set => novaTransformData = value; }
#endif
        [SerializeField, Clear] FGraphic graphic;
        public FGraphic Graphic { get => graphic; set => graphic = value; }

#if UNITY_2021_3_OR_NEWER
        public UnityEngine.UIElements.UIDocument UIDocument { get; set; }
#endif

        [SerializeField, Clear] GameObject rectGameObject;
        public GameObject RectGameObject { get => rectGameObject; set => rectGameObject = value; }

        [SerializeField, Clear] Vector2Int spriteSize;
        public Vector2Int SpriteSize { get => spriteSize; set => spriteSize = value; }

        [SerializeField, Clear] Vector2 size;
        public Vector2 Size { get => size; set => size = value; }

        [SerializeField, Clear] Vector2 position;
        public Vector2 Position { get => position; set => position = value; }

        [SerializeField, Clear] string spritePath;
        public string SpritePath { get => spritePath; set => spritePath = value; }

        [SerializeField, Clear] string link;
        public string Link { get => link; set => link = value; }

        [SerializeField, Clear] string tagReason;
        public string TagReason { get => tagReason; set => tagReason = value; }

        [SerializeField, Clear] string downloadableReason;
        public string DownloadableReason { get => downloadableReason; set => downloadableReason = value; }

        [SerializeField, Clear] string generativeReason;
        public string GenerativeReason { get => generativeReason; set => generativeReason = value; }

        [SerializeField, Clear] int downloadAttempsCount;
        public int DownloadAttempsCount { get => downloadAttempsCount; set => downloadAttempsCount = value; }

        [SerializeField, Clear] float angle;
        public float Angle { get => angle; set => angle = value; }

        [SerializeField, Clear] bool isDuplicate;
        public bool IsDuplicate { get => isDuplicate; set => isDuplicate = value; }

        [SerializeField, Clear] bool isMutual;
        public bool IsMutual { get => isMutual; set => isMutual = value; }

        [SerializeField, Clear] bool isEmpty;
        public bool IsEmpty { get => isEmpty; set => isEmpty = value; }

        [SerializeField, Clear] bool needDownload;
        public bool NeedDownload { get => needDownload; set => needDownload = value; }

        [SerializeField, Clear] bool needGenerate;
        public bool NeedGenerate { get => needGenerate; set => needGenerate = value; }

        [SerializeField, Clear] bool forceImage;
        public bool ForceImage { get => forceImage; set => forceImage = value; }

        [SerializeField, Clear] bool isOverlappedByStroke;
        public bool IsOverlappedByStroke { get => isOverlappedByStroke; set => isOverlappedByStroke = value; }

        [SerializeField, Clear] bool forceContainer;
        public bool ForceContainer { get => forceContainer; set => forceContainer = value; }

        [SerializeField, Clear] bool isInsideDownloadable;
        public bool InsideDownloadable { get => isInsideDownloadable; set => isInsideDownloadable = value; }

        [SerializeField, Clear] bool hasFontAsset;
        public bool HasFontAsset { get => hasFontAsset; set => hasFontAsset = value; }

        [SerializeField, Clear] ImageFormat imageFormat;
        public ImageFormat ImageFormat { get => imageFormat; set => imageFormat = value; }
    }

    [Serializable]
    public struct FcuHierarchy
    {
        public int Index;
        public string Name;
        public string Guid;
    }
}