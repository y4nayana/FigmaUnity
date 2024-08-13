using DA_Assets.FCU.Model;
using DA_Assets.Shared;
using UnityEngine;

#pragma warning disable IDE0003
#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    internal class UnityComponentsTab : ScriptableObjectBinder<FcuSettingsWindow, FigmaConverterUnity>
    {
        public void Draw()
        {
            gui.SectionHeader(FcuLocKey.label_import_components.Localize(), FcuLocKey.tooltip_import_components.Localize());
            gui.Space15();

            monoBeh.Settings.ComponentSettings.ImageComponent = gui.EnumField(
                new GUIContent(FcuLocKey.label_image_component.Localize(), FcuLocKey.tooltip_image_component.Localize()),
                monoBeh.Settings.ComponentSettings.ImageComponent, false);

            monoBeh.Settings.ComponentSettings.TextComponent = gui.EnumField(
                new GUIContent(FcuLocKey.label_text_component.Localize(), FcuLocKey.tooltip_text_component.Localize()),
                monoBeh.Settings.ComponentSettings.TextComponent, false);

            monoBeh.Settings.ComponentSettings.ShadowComponent = gui.EnumField(
                new GUIContent(FcuLocKey.label_shadow_type.Localize(), FcuLocKey.tooltip_shadow_type.Localize()),
                monoBeh.Settings.ComponentSettings.ShadowComponent, false);

            monoBeh.Settings.ComponentSettings.ButtonComponent = gui.EnumField(
                new GUIContent(FcuLocKey.label_button_type.Localize(), FcuLocKey.tooltip_button_type.Localize()),
                monoBeh.Settings.ComponentSettings.ButtonComponent, false, null);

            monoBeh.Settings.ComponentSettings.UseI2Localization = gui.Toggle(
                new GUIContent(FcuLocKey.label_use_i2localization.Localize(), FcuLocKey.tooltip_use_i2localization.Localize()),
                monoBeh.Settings.ComponentSettings.UseI2Localization);

            gui.Space15();

            switch (monoBeh.Settings.ComponentSettings.ImageComponent)
            {
                case ImageComponent.UnityImage:
                case ImageComponent.RawImage:
                    this.UnityImageSettingsTab.Draw();
                    break;
                case ImageComponent.SubcShape:
                    this.Shapes2DSettingsTab.Draw();
                    break;
                case ImageComponent.ProceduralImage:
                    this.JoshPuiSettingsSection.Draw();
                    break;
                case ImageComponent.RoundedImage:
                    this.DttPuiSettingsSection.Draw();
                    break;
                case ImageComponent.MPImage:
                    this.MPImageSettingsTab.Draw();
                    break;
                case ImageComponent.SpriteRenderer:
                    this.SR_SettingsEditor.Draw();
                    break;
            }

            gui.Space15();

            switch (monoBeh.Settings.ComponentSettings.TextComponent)
            {
                case TextComponent.UnityText:
                    this.DefaultTextSettingsTab.Draw();
                    break;
                case TextComponent.TextMeshPro:
                case TextComponent.RTLTextMeshPro:
                    this.TextMeshSettingsTab.Draw();
                    break;
            }

            gui.Space15();
            this.ButtonSettingsTab.Draw();
#if DABUTTON_EXISTS
            gui.Space15();
            this.DabSettingsTab.Draw();
#endif

            gui.Space30();
        }

        private UnityImageSection unityImageSettingsTab;
        internal UnityImageSection UnityImageSettingsTab => monoBeh.Bind(ref unityImageSettingsTab, scriptableObject);

        private Shapes2DSection shapesSettingsTab;
        internal Shapes2DSection Shapes2DSettingsTab => monoBeh.Bind(ref shapesSettingsTab, scriptableObject);

        private JoshPuiSection joshPuiSettingsSection;
        internal JoshPuiSection JoshPuiSettingsSection => monoBeh.Bind(ref joshPuiSettingsSection, scriptableObject);

        private DttPuiSection dttPuiSettingsSection;
        internal DttPuiSection DttPuiSettingsSection => monoBeh.Bind(ref dttPuiSettingsSection, scriptableObject);

        private MPImageSection mpImageSettingsTab;
        internal MPImageSection MPImageSettingsTab => monoBeh.Bind(ref mpImageSettingsTab, scriptableObject);


        private SrSection srSettings;
        public SrSection SR_SettingsEditor => monoBeh.Bind(ref srSettings, scriptableObject);

        private TextMeshSection textMeshSettingsTab;
        internal TextMeshSection TextMeshSettingsTab => monoBeh.Bind(ref textMeshSettingsTab, scriptableObject);

        private UnityTextSection defaultTextSettingsTab;
        internal UnityTextSection DefaultTextSettingsTab => monoBeh.Bind(ref defaultTextSettingsTab, scriptableObject);


        private ButtonSection buttonSettingsTab;
        internal ButtonSection ButtonSettingsTab => monoBeh.Bind(ref buttonSettingsTab, scriptableObject);

        private DabSection dabSettingsTab;
        internal DabSection DabSettingsTab => monoBeh.Bind(ref dabSettingsTab, scriptableObject);
    }
}