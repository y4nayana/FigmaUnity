using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Shared;
using DA_Assets.Shared.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DA_Assets.FCU
{
    [Serializable]
    public class TagSetter : MonoBehaviourBinder<FigmaConverterUnity>
    {
        public Dictionary<FcuTag, int> TagsCounter { get; set; }

        public void SetTags(FObject page)
        {
            DALogger.Log(FcuLocKey.log_tagging.Localize());

            NameSetter.ClearNames();

            SetTagsByFigma(page);
            SetSmartTags(page);
        }

        private void SetTagsByFigma(FObject parent)
        {
            if (parent.ContainsTag(FcuTag.Frame))
            {
                parent.Data.Hierarchy = new List<FcuHierarchy>
                {
                    new FcuHierarchy
                    {
                        Index = -1,
                        Name = parent.Data.ObjectName,
                        Guid = parent.Data.UitkGuid
                    }
                };
            }

            for (int i = 0; i < parent.Children.Count; i++)
            {
                FObject child = parent.Children[i];

                child.Data = new SyncData
                {
                    Id = child.Id,
                    ChildIndexes = new List<int>(),
                    Ignore = !child.IsVisible(),
                    Parent = parent,
                };

                monoBeh.NameSetter.SetNames(child);

                if (GetManualTag(child, out FcuTag manualTag))
                {
                    child.AddTag(manualTag);
                    monoBeh.Log($"GetManualTag | {child.Name} | {manualTag}", FcuLogType.SetTag);

                    if (manualTag == FcuTag.Image)
                    {
                        child.Data.ForceImage = true;
                    }
                    else if (manualTag == FcuTag.Container)
                    {
                        child.Data.ForceContainer = true;
                    }
                }

                if (parent.ContainsTag(FcuTag.Page))
                {
                    child.AddTag(FcuTag.Frame);
                }

                if (child.Type == NodeType.INSTANCE)
                {
                    //TODO
                }

                if (child.LayoutWrap == LayoutWrap.WRAP ||
                    child.LayoutMode == LayoutMode.HORIZONTAL ||
                    child.LayoutMode == LayoutMode.VERTICAL)
                {
                    child.AddTag(FcuTag.AutoLayoutGroup);
                }

                if (child.PreserveRatio.ToBoolNullFalse())
                {
                    child.AddTag(FcuTag.AspectRatioFitter);
                }

                if (child.IsAnyMask())
                {
                    child.AddTag(FcuTag.Mask);
                }

                if (child.Name.ToLower() == "button")
                {
                    child.AddTag(FcuTag.Button);
                }

                if (child.Type == NodeType.TEXT)
                {
                    child.AddTag(FcuTag.Text);

                    if (child.Style.IsDefault() == false)
                    {
                        if (child.Style.TextAutoResize == "WIDTH_AND_HEIGHT")
                        {
                            child.AddTag(FcuTag.ContentSizeFitter);
                        }
                    }
                }
                else if (child.Type == NodeType.VECTOR)
                {
                    child.AddTag(FcuTag.Vector);
                    child.AddTag(FcuTag.Image);
                }
                else if (child.HasActiveProperty(x => x.Fills) || child.HasActiveProperty(x => x.Strokes))
                {
                    child.AddTag(FcuTag.Image);
                }

                if (child.Effects.IsEmpty() == false)
                {
                    foreach (Effect effect in child.Effects)
                    {
                        if (effect.IsShadowType() && monoBeh.Settings.ComponentSettings.ShadowComponent == ShadowComponent.TrueShadow)
                        {
                            child.AddTag(FcuTag.Shadow);
                            break;
                        }
                    }
                }

                child.Data.IsOverlappedByStroke = IsOverlappedByStroke(child);

                if (child.Opacity != null && child.Opacity != 1)
                {
                    child.AddTag(FcuTag.CanvasGroup);
                }

                child.Data.Hierarchy.AddRange(parent.Data.Hierarchy);

                int sceneIndex = GetNewIndex(parent, i);
                child.Data.Hierarchy.Add(new FcuHierarchy
                {
                    Index = sceneIndex,
                    Name = child.Data.ObjectName,
                    Guid = child.Data.UitkGuid,
                });

                parent.Children[i] = child;

                if (child.Children.IsEmpty())
                {
                    continue;
                }

                SetTagsByFigma(child);
            }
        }

        /// <summary>
        /// If the stroke is too thick relative to the height or width of the object, it overlaps the fill.
        /// In such a case, we do not download the image for this component, and use the stroke color as the fill.
        /// </summary>
        private bool IsOverlappedByStroke(FObject fobject)
        {
            bool blockedByStroke = false;

            if (fobject.HasActiveProperty(x => x.Fills) && fobject.HasActiveProperty(x => x.Strokes) && !fobject.ContainsTag(FcuTag.Shadow))
            {
                if (fobject.IndividualStrokeWeights.IsDefault())
                {
                    float twoSides = fobject.StrokeWeight * 2;

                    if (twoSides >= fobject.Size.y)
                    {
                        blockedByStroke = true;
                    }
                    else if (twoSides >= fobject.Size.x)
                    {
                        blockedByStroke = true;
                    }
                }
                else
                {
                    float topBottomStrokes = fobject.IndividualStrokeWeights.Top + fobject.IndividualStrokeWeights.Bottom;
                    float leftRightStrokes = fobject.IndividualStrokeWeights.Left + fobject.IndividualStrokeWeights.Right;

                    if (topBottomStrokes >= fobject.Size.y)
                    {
                        blockedByStroke = true;
                    }
                    else if (leftRightStrokes >= fobject.Size.x)
                    {
                        blockedByStroke = true;
                    }
                }
            }

            return blockedByStroke;
        }

        /// <summary>
        /// Retrieving the index of an element in the hierarchy, considering the <see cref="FObject.Data.Ignore"/> flag.
        /// </summary>
        private int GetNewIndex(FObject parent, int figmaIndex)
        {
            int count = 0;

            for (int i = 0; i < figmaIndex; i++)
            {
                FObject child = parent.Children[i];

                if (child.Data == null)
                {
                    break;
                }

                if (!child.Data.Ignore)
                {
                    count++;
                }
            }

            return count;
        }
        private void SetSmartTags(FObject parent)
        {
            foreach (FObject child in parent.Children)
            {
                child.Data.IsEmpty = IsEmpty(child);

                if (child.Data.IsEmpty)
                {
                    child.Data.TagReason = nameof(child.Data.IsEmpty);
                    monoBeh.Log($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    continue;
                }

                if (child.Data.ForceImage)
                {
                    ///If a component is tagged with the 'img' tag, it will downloaded as a single image,
                    ///which means there is no need to look for child components for it.
                    child.Data.TagReason = nameof(child.Data.ForceImage);
                    monoBeh.Log($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    continue;
                }

                if (child.IsRootVector(parent))
                {
                    ///If the component is a vector that is at the root of your frame, 
                    ///then we recognize it as a single image and do not look for child components for it, 
                    ///because vectors do not have it.
                    child.AddTag(FcuTag.Image);
                    child.Data.ForceImage = true;

                    child.Data.TagReason = nameof(TagExtensions.IsRootVector);
                    monoBeh.Log($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    continue;
                }

                if (monoBeh.Settings.MainSettings.RawImport == false)
                {
                    bool hasButtonTags = child.ContainsCustomButtonTags();
                    bool hasIcon = ContainsIcon(child);
                    bool singleImage = CanBeSingleImage(child);

                    if (singleImage && hasButtonTags)
                    {
                        child.Data.ForceImage = true;
                        child.AddTag(FcuTag.Image);
                        child.RemoveNotDownloadableTags();

                        child.Data.TagReason = nameof(TagExtensions.ContainsCustomButtonTags);
                        monoBeh.Log($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                        continue;
                    }
                    else if (hasIcon)
                    {
                        child.Data.ForceContainer = true;
                        child.AddTag(FcuTag.Container);

                        child.Data.TagReason = nameof(ContainsIcon);
                        monoBeh.Log($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    }
                    else if (singleImage)
                    {
                        ///If the component tree contains only vectors and/or components whose tags
                        ///have flag 'CanBeInsideSingleImage == false', recognize that component as a single image.
                        child.Data.ForceImage = true;
                        child.AddTag(FcuTag.Image);
                        child.RemoveNotDownloadableTags();

                        child.Data.TagReason = "SingleImage";
                        monoBeh.Log($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                        continue;
                    }
                }

                if (child.Children.IsEmpty() == false)
                {
                    child.Data.TagReason = "children not empty";
                    monoBeh.Log($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    child.AddTag(FcuTag.Container);
                }

                if (child.Children.IsEmpty())
                    continue;

                SetSmartTags(child);
            }
        }

        private bool GetManualTag(FObject fobject, out FcuTag manualTag)
        {
            if (fobject.Name.Contains(FcuConfig.Instance.RealTagSeparator) == false)
            {
                manualTag = FcuTag.None;
                return false;
            }

            IEnumerable<FcuTag> fcuTags = Enum.GetValues(typeof(FcuTag))
               .Cast<FcuTag>()
               .Where(x => x != FcuTag.None);

            foreach (FcuTag fcuTag in fcuTags)
            {
                bool tagFind = FindManualTag(fobject.Data.ObjectName, fcuTag);

                if (tagFind)
                {
                    manualTag = fcuTag;
                    return true;
                }
            }

            manualTag = FcuTag.None;
            return false;
        }

        private bool FindManualTag(string name, FcuTag fcuTag)
        {
            string figmaTag = fcuTag.GetTagConfig().FigmaTag.ToLower();

            if (figmaTag.IsEmpty())
                return false;

            string tempName = name.ToLower().Replace(" ", "");

            string[] nameParts = tempName.Split(FcuConfig.Instance.RealTagSeparator);

            if (nameParts.Length > 0)
            {
                string tagPart = nameParts[0];
                string cleaned = Regex.Replace(tagPart, "[^a-z]", "");

                if (cleaned == figmaTag)
                {
                    monoBeh.Log($"CheckForTag | GetFigmaType | {name} | tag: {figmaTag}", FcuLogType.SetTag);
                    return true;
                }
            }

            return false;
        }

        private bool ContainsIcon(FObject fobject)
        {
            if (fobject.Children.IsEmpty())
                return false;

            foreach (FObject item in fobject.Children)
            {
                if (item.Name.ToLower().Contains("icon"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanBeSingleImage(FObject fobject)
        {
            if (fobject.Children.IsEmpty())
                return false;

            int count = 0;

            CanBeSingleImageRecursive(fobject, ref count);
            return count == 0;
        }

        private void CanBeSingleImageRecursive(FObject fobject, ref int count)
        {
            if (CanBeInsideSingleImage(fobject) == false)
            {
                count++;
                return;
            }

            if (fobject.Children.IsEmpty())
                return;

            foreach (FObject child in fobject.Children)
                CanBeSingleImageRecursive(child, ref count);
        }

        private bool CanBeInsideSingleImage(FObject fobject)
        {
            if (fobject.Data.ForceContainer)
                return false;

            if (fobject.Data.ForceImage)
                return false;

            foreach (FcuTag fcuTag in fobject.Data.Tags)
            {
                TagConfig tc = fcuTag.GetTagConfig();

                if (tc.CanBeInsideSingleImage == false)
                    return false;

                /*if (monoBeh.UsingTrueShadow() && fcuTag == FcuTag.Shadow)
                {
                    return false;
                }*/
            }

            return true;
        }

        public bool IsEmpty(FObject fobject)
        {
            int count = 0;
            IsEmptyRecursive(fobject, ref count);
            return count == 0;
        }

        public void IsEmptyRecursive(FObject fobject, ref int count)
        {
            if (count > 0)
                return;

            if (fobject.Opacity == 0)
                return;

            if (fobject.IsZeroSize() && fobject.Type != NodeType.LINE)
                return;

            else if (fobject.IsVisible() == false)
                return;
            else if (fobject.Fills.IsEmpty() &&
                fobject.Strokes.IsEmpty() &&
                fobject.Effects.IsEmpty())
            {

            }
            else
            {
                count++;
                return;
            }

            if (fobject.Children.IsEmpty())
                return;

            foreach (var item in fobject.Children)
                IsEmptyRecursive(item, ref count);
        }

        public void CountTags(List<FObject> fobjects)
        {
            ConcurrentDictionary<FcuTag, ConcurrentBag<bool>> tagsCounter = new ConcurrentDictionary<FcuTag, ConcurrentBag<bool>>();

            Array fcuTags = Enum.GetValues(typeof(FcuTag));

            foreach (FcuTag tag in fcuTags)
            {
                tagsCounter.TryAdd(tag, new ConcurrentBag<bool>());
            }

            Parallel.ForEach(fobjects, fobject =>
            {
                if (fobject.Data.GameObject == null)
                {
                    return;
                }

                foreach (FcuTag tag in fobject.Data.Tags)
                {
                    tagsCounter[tag].Add(true);
                }
            });

            Dictionary<FcuTag, int> dictionary = tagsCounter.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            );

            this.TagsCounter = dictionary;
        }
    }
}