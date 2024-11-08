using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.FCU
{
    [Serializable]
    public class TagSetter : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        int _logDelayMs = 5000;
        public Dictionary<FcuTag, int> TagsCounter { get; set; } = new Dictionary<FcuTag, int>();

        public async Task SetTags(FObject page)
        {
            CancellationTokenSource logCts = new CancellationTokenSource();

            _ = LogTagging(logCts);

            await Task.Run(() =>
            {
                CreateSyncHelpers(page);
                SetTagsByFigma(page);
                SetSmartTags(page);
                SetIgnoredObjects(page);
            }, monoBeh.GetToken(TokenType.Import));

            logCts.Cancel();
        }

        private async Task LogTagging(CancellationTokenSource logCts)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsed = DateTime.Now - startTime;

            DALogger.Log(FcuLocKey.log_tagging.Localize((int)elapsed.TotalSeconds));

            try
            {
                while (!logCts.Token.IsCancellationRequested && !monoBeh.GetToken(TokenType.Import).IsCancellationRequested)
                {
                    await Task.Delay(_logDelayMs, logCts.Token);
                    elapsed = DateTime.Now - startTime;
                    DALogger.Log(FcuLocKey.log_tagging.Localize((int)elapsed.TotalSeconds));
                }
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                elapsed = DateTime.Now - startTime;
                DALogger.Log(FcuLocKey.log_tagging.Localize((int)elapsed.TotalSeconds));
            }
        }

        private void CreateSyncHelpers(FObject parent)
        {
            if (parent.ContainsTag(FcuTag.Frame))
            {
                parent.Data.Hierarchy = new List<FcuHierarchy>
                {
                    new FcuHierarchy
                    {
                        Index = -1,
                        Name = parent.Data.Names.ObjectName,
                        Guid = parent.Data.Names.UitkGuid
                    }
                };
            }

            Parallel.For(0, parent.Children.Count, i =>
            {
                FObject child = parent.Children[i];

                child.Data = new SyncData
                {
                    Id = child.Id,
                    ChildIndexes = new List<int>(),
                    Parent = parent,
                    Graphic = monoBeh.GraphicHelpers.GetGraphic(child),
                };

                monoBeh.NameSetter.SetNames(child);
                child.Data.Hierarchy.AddRange(parent.Data.Hierarchy);

                int sceneIndex = GetNewIndex(parent, i);
                child.Data.Hierarchy.Add(new FcuHierarchy
                {
                    Index = sceneIndex,
                    Name = child.Data.Names.ObjectName,
                    Guid = child.Data.Names.UitkGuid,
                });

                parent.Children[i] = child;

                if (child.Children.IsEmpty())
                    return;

                CreateSyncHelpers(child);
            });
        }

        private void SetTagsByFigma(FObject parent)
        {
            Parallel.For(0, parent.Children.Count, i =>
            {
                FObject child = parent.Children[i];

                child.Data.IsEmpty = IsEmpty(child);

                if (child.Name.IsScrollContent())
                {
                    child.Data.ForceContainer = true;
                }
                else if (child.Name.IsScrollViewport())
                {
                    child.Data.ForceContainer = true;
                }

                if (GetManualTag(child, out FcuTag manualTag))
                {
                    child.AddTag(manualTag);
                    FcuLogger.Debug($"GetManualTag | {child.Name} | {manualTag}", FcuLogType.SetTag);

                    if (manualTag == FcuTag.Image)
                    {
                        child.Data.ForceImage = true;
                    }
                    else if (manualTag == FcuTag.Container)
                    {
                        child.Data.ForceContainer = true;
                    }
                    else if (manualTag == FcuTag.Ignore)
                    {
                        child.Data.IsEmpty = true;
                    }
                }

                if (child.ContainsTag(FcuTag.Background))
                {
                    child.AddTag(FcuTag.Image);
                }

                if (parent.ContainsTag(FcuTag.Page) && !child.ContainsImageEmojiVideo())
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
                    if (child.HasVisibleProperty(x => x.Children))
                    {
                        child.AddTag(FcuTag.AutoLayoutGroup);
                    }
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
                    child.AddTag(FcuTag.Image);
                }
                else if (child.HasVisibleProperty(x => x.Fills) || child.HasVisibleProperty(x => x.Strokes))
                {
                    child.AddTag(FcuTag.Image);
                }

                if (child.Effects.IsEmpty() == false)
                {
                    Effect[] allShadows = child.Effects.Where(x => x.IsShadowType()).ToArray();

                    if (monoBeh.IsUGUI() && monoBeh.UsingTrueShadow() && !monoBeh.UsingSpriteRenderer())
                    {
                        if (allShadows.Length > 0)
                        {
                            child.AddTag(FcuTag.Shadow);
                        }
                    }
                    else if (monoBeh.IsUITK())
                    {
                        if (allShadows.Length > 0)
                        {
                            child.AddTag(FcuTag.Shadow);
                        }
                    }
                    else if (monoBeh.IsNova())
                    {
                        if (allShadows.Length > 0)
                        {
                            child.AddTag(FcuTag.Shadow);
                        }
                    }

                    if (monoBeh.IsNova())
                    {
                        foreach (Effect effect in child.Effects)
                        {
                            if (effect.Type == EffectType.BACKGROUND_BLUR)
                            {
                                child.AddTag(FcuTag.Blur);
                            }
                        }
                    }
                }

                child.Data.IsOverlappedByStroke = IsOverlappedByStroke(child);

                if (child.Opacity.HasValue && child.Opacity != 1)
                {
                    child.AddTag(FcuTag.CanvasGroup);
                }

                if (!child.HasVisibleProperty(x => x.Children))
                    return;

                SetTagsByFigma(child);
            });
        }

        private void SetSmartTags(FObject parent)
        {
            Parallel.For(0, parent.Children.Count, i =>
            {
                FObject child = parent.Children[i];

                if (Is9slice(child))
                {
                    child.AddTag(FcuTag.Slice9);
                    child.AddTag(FcuTag.Image);
                    child.Data.ForceImage = true;
                    return;
                }

                bool isInputFieldTextComponents = (parent.Name.IsInputTextArea() || parent.ContainsTag(FcuTag.InputField) || parent.ContainsTag(FcuTag.PasswordField)) && child.ContainsTag(FcuTag.Text);

                if (child.Data.IsEmpty && !isInputFieldTextComponents)
                {
                    child.Data.TagReason = nameof(child.Data.IsEmpty);
                    FcuLogger.Debug($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    return;
                }

                if (child.Data.ForceImage)
                {
                    ///If a component is tagged with the 'img' tag, it will downloaded as a single image,
                    ///which means there is no need to look for child components for it.
                    child.Data.TagReason = nameof(child.Data.ForceImage);
                    FcuLogger.Debug($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    return;
                }

                if (child.IsRootSprite(parent))
                {
                    ///If the component is a vector that is at the root of your frame,
                    ///then we recognize it as a single image and do not look for child components for it,
                    ///because vectors do not have it.
                    child.AddTag(FcuTag.Image);
                    child.Data.ForceImage = true;

                    child.Data.TagReason = nameof(TagExtensions.IsRootSprite);
                    FcuLogger.Debug($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    return;
                }

                if (monoBeh.Settings.MainSettings.RawImport == false)
                {
                    bool hasButtonTags = child.ContainsCustomButtonTags();
                    bool hasIcon = ContainsIcon(child);
                    bool singleImage = CanBeSingleImage(child);

                    if (hasIcon)
                    {
                        child.Data.ForceContainer = true;
                        child.AddTag(FcuTag.Container);

                        child.Data.TagReason = nameof(ContainsIcon);
                        FcuLogger.Debug($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    }
                    else if (singleImage && hasButtonTags)
                    {
                        child.Data.ForceImage = true;
                        child.AddTag(FcuTag.Image);
                        child.RemoveNotDownloadableTags();

                        child.Data.TagReason = nameof(TagExtensions.ContainsCustomButtonTags);
                        FcuLogger.Debug($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                        return;
                    }
                    else if (singleImage)
                    {
                        ///If the component tree contains only vectors and/or components whose tags
                        ///have flag 'CanBeInsideSingleImage == false', recognize that component as a single image.
                        child.Data.ForceImage = true;
                        child.AddTag(FcuTag.Image);
                        child.RemoveNotDownloadableTags();

                        child.Data.TagReason = "SingleImage";
                        FcuLogger.Debug($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                        return;
                    }
                    else if (child.Type == NodeType.BOOLEAN_OPERATION)
                    {
                        child.Data.ForceImage = true;
                        child.AddTag(FcuTag.Image);

                        child.Data.TagReason = "BOOLEAN_OPERATION";
                        return;
                    }
                }

                if (child.HasVisibleProperty(x => x.Children))
                {
                    child.Data.TagReason = "children not empty";
                    FcuLogger.Debug($"{nameof(SetSmartTags)} | {child.Data.TagReason} | {child.Data.NameHierarchy}", FcuLogType.SetTag);
                    child.AddTag(FcuTag.Container);
                }

                if (!child.HasVisibleProperty(x => x.Children))
                    return;

                SetSmartTags(child);
            });
        }


        /// <summary>
        /// If the stroke is too thick relative to the height or width of the object, it overlaps the fill.
        /// In such a case, we do not download the image for this component, and use the stroke color as the fill.
        /// </summary>
        private bool IsOverlappedByStroke(FObject fobject)
        {
            bool blockedByStroke = false;

            if (fobject.HasVisibleProperty(x => x.Fills) && fobject.HasVisibleProperty(x => x.Strokes) && !fobject.ContainsTag(FcuTag.Shadow))
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
        /// Retrieving the index of an element in the hierarchy, considering the <see cref="SyncData.IsEmpty"/> flag.
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

                if (!child.Data.IsEmpty)
                {
                    count++;
                }
            }

            return count;
        }

        private bool Is9slice(FObject fobject)
        {
            if (fobject.Children.IsEmpty())
                return false;

            if (fobject.Children.Count != 9)
                return false;

            AnchorType child0 = fobject.Children[0].GetFigmaAnchor();
            AnchorType child1 = fobject.Children[1].GetFigmaAnchor();
            AnchorType child2 = fobject.Children[2].GetFigmaAnchor();
            AnchorType child3 = fobject.Children[3].GetFigmaAnchor();
            AnchorType child4 = fobject.Children[4].GetFigmaAnchor();
            AnchorType child5 = fobject.Children[5].GetFigmaAnchor();
            AnchorType child6 = fobject.Children[6].GetFigmaAnchor();
            AnchorType child7 = fobject.Children[7].GetFigmaAnchor();
            AnchorType child8 = fobject.Children[8].GetFigmaAnchor();

            if (child0 == AnchorType.TopLeft &&
                child1 == AnchorType.HorStretchTop &&
                child2 == AnchorType.TopRight &&
                child3 == AnchorType.VertStretchLeft &&
                child4 == AnchorType.StretchAll &&
                child5 == AnchorType.VertStretchRight &&
                child6 == AnchorType.BottomLeft &&
                child7 == AnchorType.HorStretchBottom &&
                child8 == AnchorType.BottomRight)
            {
                return true;
            }

            return false;
        }

        private void SetIgnoredObjects(FObject parent)
        {
            Parallel.For(0, parent.Children.Count, i =>
            {
                FObject child = parent.Children[i];

                if (child.Data.IsEmpty)
                {
                    child.SetFlagToAllChilds(x => x.Data.IsEmpty = true);
                    return;
                }

                if (child.Data.ForceImage)
                {
                    child.SetFlagToAllChilds(x => x.Data.IsEmpty = true);
                    return;
                }

                if (!child.HasVisibleProperty(x => x.Children))
                    return;

                SetIgnoredObjects(child);
            });
        }

        internal bool GetManualTag(FObject fobject, out FcuTag manualTag)
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
                bool tagFind = FindManualTag(fobject.Name, fcuTag);

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
                    FcuLogger.Debug($"CheckForTag | GetFigmaType | {name} | tag: {figmaTag}", FcuLogType.SetTag);
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
            }

            return true;
        }

        private bool IsEmpty(FObject fobject)
        {
            int count = 0;
            IsEmptyRecursive(fobject, ref count);
            return count == 0;
        }

        private void IsEmptyRecursive(FObject fobject, ref int count)
        {
            if (count > 0)
                return;

            if (fobject.Opacity == 0)
                return;

            if (!fobject.IsVisible())
                return;

            if (fobject.ContainsTag(FcuTag.Ignore))
                return;

            if (fobject.IsZeroSize() && fobject.Type != NodeType.LINE)
                return;

            bool hasFills = !fobject.Fills.IsEmpty() && fobject.Fills.Any(x => x.IsVisible());
            bool hasStrokes = !fobject.Strokes.IsEmpty() && fobject.Strokes.Any(x => x.IsVisible());
            bool hasEffects = !fobject.Effects.IsEmpty() && fobject.Effects.Any(x => x.IsVisible());

            if (hasFills || hasStrokes || hasEffects)
            {
                count++;
                return;
            }

            if (!fobject.HasVisibleProperty(x => x.Children))
                return;

            foreach (FObject item in fobject.Children)
                IsEmptyRecursive(item, ref count);
        }

        public async Task CountTags(List<FObject> fobjects)
        {
            await Task.Run(() =>
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
            }, monoBeh.GetToken(TokenType.Import));

        }
    }
}
