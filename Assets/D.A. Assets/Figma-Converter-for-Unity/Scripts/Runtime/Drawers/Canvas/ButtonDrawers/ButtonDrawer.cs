using DA_Assets.FCU.Model;
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class ButtonDrawer : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        [SerializeField] List<FObject> buttons = new List<FObject>();
        public List<FObject> Buttons => buttons;

        public void Init()
        {
            buttons.Clear();
        }

        public void Draw(FObject fobject)
        {
            fobject.Data.ButtonComponent = monoBeh.Settings.ButtonSettings.ButtonComponent;

            switch (monoBeh.Settings.ButtonSettings.ButtonComponent)
            {
                /*case ButtonComponent.DAButton:
                    {
#if DABUTTON_EXISTS
                        fobject.Data.GameObject.TryAddComponent(out DAButton _);
#endif
                    }
                    break;*/
                case ButtonComponent.FcuButton:
                    {
                        fobject.Data.GameObject.TryAddComponent(out FcuButton _);
                    }
                    break;
                default:
                    {
                        fobject.Data.GameObject.TryAddComponent(out UnityEngine.UI.Button _);
                    }
                    break;
            }

            buttons.Add(fobject);
        }

        public async Task SetTargetGraphics()
        {
            foreach (FObject fobject in buttons)
            {
                switch (fobject.Data.ButtonComponent)
                {
                    case ButtonComponent.FcuButton:
                        {
                            this.FcuButtonDrawer.SetupFcuButton(fobject.Data);
                        }
                        break;
                    /*case ButtonComponent.DAButton:
                        {
                            this.DAButtonDrawer.SetupDaButton(fobject.Data);
                        }
                        break;*/
                    default:
                        {
                            this.UnityButtonDrawer.SetupUnityButton(fobject.Data);
                        }
                        break;
                }

                await Task.Yield();
            }
        }

        [SerializeField] DAButtonDrawer dabDrawer;
        [SerializeProperty(nameof(dabDrawer))]
        public DAButtonDrawer DAButtonDrawer => monoBeh.Link(ref dabDrawer);

        [SerializeField] FcuButtonDrawer fcubDrawer;
        [SerializeProperty(nameof(fcubDrawer))]
        public FcuButtonDrawer FcuButtonDrawer => monoBeh.Link(ref fcubDrawer);

        [SerializeField] UnityButtonDrawer ubDrawer;
        [SerializeProperty(nameof(ubDrawer))]
        public UnityButtonDrawer UnityButtonDrawer => monoBeh.Link(ref ubDrawer);
    }
}