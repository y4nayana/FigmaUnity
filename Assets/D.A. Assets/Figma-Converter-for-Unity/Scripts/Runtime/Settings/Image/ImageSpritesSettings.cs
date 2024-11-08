using DA_Assets.DAI;
using DA_Assets.Logging;
using System;
using System.IO;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class ImageSpritesSettings : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        [SerializeField] ProceduralCondition proceduralCondition = ProceduralCondition.Sprite | ProceduralCondition.RectangleNoRoundedCorners;
        [SerializeProperty(nameof(proceduralCondition))]
        public ProceduralCondition ProceduralCondition { get => proceduralCondition; set => SetValue(ref proceduralCondition, value); }

       
        [Tooltip(@"The VectorGraphics 2.0.0 asset does not support displaying SVG images with effects.

I also noticed that it doesn’t render objects that use an image as a fill in Figma.

By default, the asset will download objects that meet these conditions as PNG sprites and use the UI.Image component to draw them.  

You can disable these options, but these components may not display correctly in Unity.")]
        [SerializeField] SvgCondition svgCondition = SvgCondition.ImageOrVideo | SvgCondition.AnyEffect;
        [SerializeProperty(nameof(svgCondition))]
        public SvgCondition SvgCondition { get => svgCondition; set => SetValue(ref svgCondition, value); }

        [SerializeField] ImageComponent imageComponent = ImageComponent.UnityImage;
        public ImageComponent ImageComponent
        {
            get => imageComponent;
            set
            {
                switch (value)
                {
                    case ImageComponent.SubcShape:
#if SUBC_SHAPES_EXISTS == false
                        DALogger.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.SubcShape)));
                        SetValue(ref imageComponent, ImageComponent.UnityImage);
                        return;
#else
                        break;
#endif
                    case ImageComponent.MPImage:
#if MPUIKIT_EXISTS == false
                        DALogger.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.MPImage)));
                        SetValue(ref imageComponent, ImageComponent.UnityImage);
                        return;
#else
                        break;
#endif
                    case ImageComponent.ProceduralImage:
#if JOSH_PUI_EXISTS == false
                        DALogger.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.ProceduralImage)));
                        SetValue(ref imageComponent, ImageComponent.UnityImage);
                        return;
#else
                        break;
#endif
                    case ImageComponent.RoundedImage:
#if PROCEDURAL_UI_ASSET_STORE_RELEASE == false
                        DALogger.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.RoundedImage)));
                        SetValue(ref imageComponent, ImageComponent.UnityImage);
                        return;
#else
                        break;
#endif
                    case ImageComponent.SvgImage:
#if VECTOR_GRAPHICS_EXISTS == false
                        DALogger.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.SvgImage)));
                        SetValue(ref imageComponent, ImageComponent.UnityImage);
                        return;
#else
                        break;
#endif
                }

                SetValue(ref imageComponent, value);
            }
        }


        [SerializeField] bool downloadMultipleFills = true;
        public bool DownloadMultipleFills { get => downloadMultipleFills; set => SetValue(ref downloadMultipleFills, value); }

        [SerializeField] bool downloadUnsupportedGradients = true;
        public bool DownloadUnsupportedGradients { get => downloadUnsupportedGradients; set => SetValue(ref downloadUnsupportedGradients, value); }

        [SerializeField] bool redownloadSprites = false;
        public bool RedownloadSprites { get => redownloadSprites; set => SetValue(ref redownloadSprites, value); }

        [SerializeField] string spritesPath = Path.Combine("Assets", "Sprites");
        public string SpritesPath { get => spritesPath; set => SetValue(ref spritesPath, value); }

        [SerializeField] ImageFormat imageFormat = ImageFormat.PNG;
        public ImageFormat ImageFormat
        {
            get => imageFormat;
            set
            {
                switch (imageFormat)
                {
                    case ImageFormat.SVG:
#if VECTOR_GRAPHICS_EXISTS == false
                        DALogger.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageFormat.SVG)));
                        SetValue(ref imageFormat, ImageFormat.PNG);
                        return;
#else
                        break;
#endif
                }

                SetValue(ref imageFormat, value);
            }
        }

        [SerializeField] float imageScale = 4.0f;
        public float ImageScale
        {
            get
            {
                if (imageFormat == ImageFormat.SVG && imageScale != 1f)
                {
                    DALogger.Log(FcuLocKey.log_svg_scale_1.Localize());
                    imageScale = 1f;
                }

                return imageScale;
            }
            set => SetValue(ref imageScale, value);
        }

        [SerializeField] PreserveRatioMode preserveRatioMode = PreserveRatioMode.None;
        public PreserveRatioMode PreserveRatioMode { get => preserveRatioMode; set => SetValue(ref preserveRatioMode, value); }

        [SerializeField] float pixelsPerUnit = 100;
        public float PixelsPerUnit { get => pixelsPerUnit; set => SetValue(ref pixelsPerUnit, value); }

        [SerializeField] bool useImageLinearMaterial = true;
        public bool UseImageLinearMaterial
        { 
            get => useImageLinearMaterial; 
            set => SetValue(ref useImageLinearMaterial, value); 
        }
    }

    [Serializable]
    [FcuPropertyHeader(FcuLocKey.label_texture_importer_settings, FcuLocKey.tooltip_texture_importer_settings)]
    public class TextureImporterSettings : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        [SerializeField] bool crunchedCompression = false;
        [FcuInspectorProperty(ComponentType.Toggle, FcuLocKey.label_crunched_compression, FcuLocKey.tooltip_crunched_compression)]
        public bool CrunchedCompression { get => crunchedCompression; set => SetValue(ref crunchedCompression, value); }

        [SerializeField] int compressionQuality = 100;
        [FcuInspectorProperty(ComponentType.SliderField, FcuLocKey.label_compression_quality, FcuLocKey.tooltip_compression_quality, minValue: 0f, maxValue: 100f)]
        public int CompressionQuality { get => compressionQuality; set => SetValue(ref compressionQuality, value); }

        [SerializeField] bool isReadable = true;
        [FcuInspectorProperty(ComponentType.Toggle, FcuLocKey.label_is_readable, FcuLocKey.tooltip_is_readable)]
        public bool IsReadable { get => isReadable; set => SetValue(ref isReadable, value); }

        [SerializeField] bool mipmapEnabled = false;
        [FcuInspectorProperty(ComponentType.Toggle, FcuLocKey.label_mipmap_enabled, FcuLocKey.tooltip_mipmap_enabled)]
        public bool MipmapEnabled { get => mipmapEnabled; set => SetValue(ref mipmapEnabled, value); }

#if UNITY_EDITOR
        [SerializeField] UnityEditor.TextureImporterType textureType = UnityEditor.TextureImporterType.Sprite;
        [FcuInspectorProperty(ComponentType.EnumField, FcuLocKey.label_texture_type, FcuLocKey.tooltip_texture_type)]
        public UnityEditor.TextureImporterType TextureType { get => textureType; set => SetValue(ref textureType, value); }

        [SerializeField] UnityEditor.TextureImporterCompression textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
        [FcuInspectorProperty(ComponentType.EnumField, FcuLocKey.label_texture_compression, FcuLocKey.tooltip_texture_compression)]
        public UnityEditor.TextureImporterCompression TextureCompression { get => textureCompression; set => SetValue(ref textureCompression, value); }

        [SerializeField] UnityEditor.SpriteImportMode spriteImportMode = UnityEditor.SpriteImportMode.Single;
        [FcuInspectorProperty(ComponentType.EnumField, FcuLocKey.label_sprite_import_mode, FcuLocKey.tooltip_sprite_import_mode)]
        public UnityEditor.SpriteImportMode SpriteImportMode { get => spriteImportMode; set => SetValue(ref spriteImportMode, value); }
#endif
    }
}