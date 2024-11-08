using DA_Assets.DAI;
using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class ButtonSettings : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        [SerializeField] ButtonComponent buttonComponent = ButtonComponent.UnityButton;
        [FcuInspectorProperty(ComponentType.EnumField, FcuLocKey.label_button_type, FcuLocKey.tooltip_button_type)]
        public ButtonComponent ButtonComponent
        {
            get => buttonComponent;
            set
            {
                /*switch (value)
                {
                    case ButtonComponent.DAButton:
#if DABUTTON_EXISTS == false
                        DALogger.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ButtonComponent.DAButton)));
                        buttonComponent = ButtonComponent.UnityButton;
                        return;
#endif
                        break;
                }*/

                SetValue(ref buttonComponent, value);
            }
        }


        [SerializeField] UnityButtonSettings unityButtonSettings;
        [SerializeProperty(nameof(unityButtonSettings))]
        public UnityButtonSettings UnityButtonSettings => monoBeh.Link(ref unityButtonSettings);

        [SerializeField] DAButtonSettings dabSettings;
        [SerializeProperty(nameof(dabSettings))]
        public DAButtonSettings DabSettings => monoBeh.Link(ref dabSettings);
    }
}