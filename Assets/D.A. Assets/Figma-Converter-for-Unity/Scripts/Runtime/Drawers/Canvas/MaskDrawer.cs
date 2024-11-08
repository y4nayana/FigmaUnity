using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class MaskDrawer : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        public void Draw(FObject fobject)
        {
            bool get = monoBeh.CurrentProject.TryGetByIndex(fobject.Data.ParentIndex, out FObject target);

            if (get == false && fobject.ContainsTag(FcuTag.Frame) == false)
            {
                return;
            }

            GameObject targetGo;

            if (fobject.IsObjectMask())
            {
                targetGo = target.Data.GameObject;
            }
            else
            {
                targetGo = fobject.Data.GameObject;

                if (!fobject.ContainsTag(FcuTag.Container))
                {
                    return;
                }
            }

            if (fobject.IsFrameMask() || fobject.IsClipMask())
            {
                if (monoBeh.IsNova())
                {
#if NOVA_UI_EXISTS
                    targetGo.TryAddComponent(out Nova.ClipMask unityMask);
#endif
                }
                else if (monoBeh.UseImageLinearMaterial())
                {
                    targetGo.TryAddComponent(out Mask unityMask);
                }
                else if (!monoBeh.UsingSpriteRenderer())
                {
                    targetGo.TryAddComponent(out RectMask2D unityMask);
                }
            }
            else if (fobject.IsObjectMask())
            {
                if (monoBeh.IsNova())
                {
#if NOVA_UI_EXISTS
                    targetGo.TryAddComponent(out Nova.ClipMask unityMask);
                    Sprite sprite = monoBeh.SpriteProcessor.GetSprite(fobject);
                    unityMask.Mask = sprite.texture;
#endif
                }
                else if (!monoBeh.UsingSpriteRenderer())
                {
                    monoBeh.CanvasDrawer.ImageDrawer.Draw(fobject, targetGo);
                    targetGo.TryAddComponent(out Mask unityMask);
                    unityMask.showMaskGraphic = false;
                }

                fobject.Data.GameObject.Destroy();
            }
        }
    }
}