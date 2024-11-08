using DA_Assets.DAI;

namespace DA_Assets.FCU
{
    internal class ButtonsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity, BlackInspector>
    {
        internal void Draw()
        {
            gui.TabHeader(FcuLocKey.label_buttons_tab.Localize(), FcuLocKey.tooltip_buttons_tab.Localize());
            gui.Space15();

            gui.DrawObjectFields(monoBeh.Settings.ButtonSettings);
            gui.DrawObjectFields(monoBeh.Settings.ButtonSettings.UnityButtonSettings);
        }
    }
}