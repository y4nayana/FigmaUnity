﻿using DA_Assets.DAI;
using DA_Assets.Logging;
using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class TextFontsSettings : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {

        [SerializeField] TextComponent textComponent = TextComponent.UnityText;
        public TextComponent TextComponent
        {
            get => textComponent;
            set
            {
                switch (value)
                {
                    case TextComponent.TextMeshPro:
#if TextMeshPro == false
                        DALogger.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(TextComponent.TextMeshPro)));
                        textComponent = TextComponent.UnityText;
                        return;
#else
                        break;
#endif
                }

                SetValue(ref textComponent, value);
            }
        }


        [SerializeField] bool overrideLetterSpacing = false;
        public bool OverrideLetterSpacing { get => overrideLetterSpacing; set => SetValue(ref overrideLetterSpacing, value); }

        [SerializeField] bool overrideLineSpacing = false;
        public bool OverrideLineSpacingPx { get => overrideLineSpacing; set => SetValue(ref overrideLineSpacing, value); }
    }
}