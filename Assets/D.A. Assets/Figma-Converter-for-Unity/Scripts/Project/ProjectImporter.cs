using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Shared;
using DA_Assets.Shared.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DA_Assets.FCU
{
    [Serializable]
    public class ProjectImporter : MonoBehaviourBinder<FigmaConverterUnity>
    {
        public IEnumerator StartImport()
        {
            monoBeh.FolderCreator.CreateAll();

            List<FObject> currPage = monoBeh.CurrentProject.CurrentPage;

            currPage.Clear();
            monoBeh.CurrentProject.LastImportedFrames.Clear();

            SceneBackuper.BackupActiveScene();

            monoBeh.CanvasDrawer.AddCanvasComponent();
            monoBeh.Events.OnImportStart?.Invoke(monoBeh);

            List<string> selectedIds = GetSelectedFrameIds();

            if (selectedIds.Count < 1)
            {
                DALogger.Log(FcuLocKey.log_nothing_to_import.Localize());
                yield break;
            }

            DAResult<List<FObject>> result = default;
            yield return monoBeh.ProjectDownloader.DownloadAllNodes(selectedIds, x => result = x);

            FObject virtualPage = new FObject
            {
                Id = FcuConfig.PARENT_ID,
                Name = monoBeh.CurrentProject.FigmaProject.Name,
                Children = result.Object,
                Data = new SyncData
                {
                    GameObject = monoBeh.gameObject,
                    ObjectName = FcuTag.Page.ToString(),
                    Tags = new List<FcuTag>
                    {
                        FcuTag.Page
                    }
                }
            };

            monoBeh.TagSetter.SetTags(virtualPage);

            ConvertTreeToList(virtualPage, currPage);
            yield return monoBeh.ImageTypeSetter.SetImageTypes(currPage);
            monoBeh.HashGenerator.SetHashes(currPage);

            //Setting root frames before creating game objects to use them in import algorithms.
            monoBeh.CurrentProject.SetRootFrames(currPage);

            if (monoBeh.IsUGUI() || monoBeh.IsNova())
            {
                yield return monoBeh.CurrentProject.LoadLocalPrefabs();

                SyncHelper[] syncHelpers = monoBeh.SyncHelpers.GetAllSyncHelpers();

                if (syncHelpers.Length > 0)
                {
                    //Setting root frames to existing game objects.
                    monoBeh.SyncHelpers.RestoreRootFrames(syncHelpers);

                    monoBeh.HashCacher.LoadCache(syncHelpers);

                    PreImportInput diffData = PreImportDataCreator.GetPreImportData(currPage, syncHelpers.ToList());
                    PreImportOutput diffCheckResult = default;

                    yield return monoBeh.AssetTools.ReselectFcu();
                    monoBeh.DelegateHolder.ShowDifferenceChecker(diffData, _ => diffCheckResult = _);

                    while (diffCheckResult.IsDefault())
                    {
                        yield return WaitFor.Delay1();
                    }

                    List<FObject> tempPage = new List<FObject>();

                    foreach (var diffModel in diffCheckResult.ToImport)
                    {
                        foreach (FObject fobject in currPage)
                        {
                            if (diffModel == fobject.Id)
                            {
                                tempPage.Add(fobject);
                            }
                        }
                    }

                    currPage = tempPage;

                    yield return monoBeh.CanvasDrawer.GameObjectDrawer.DestroyMissing(diffCheckResult.ToRemove);
                }

                monoBeh.HashCacher.WriteCache(currPage);

                yield return monoBeh.CanvasDrawer.GameObjectDrawer.Draw(virtualPage);
            }

            //Setting root frames after creating game objects so that the root frames are serialized in the inspector.
            monoBeh.CurrentProject.SetRootFrames(currPage);
            monoBeh.TagSetter.CountTags(currPage);


            monoBeh.SpriteColorizer.SetSingleColors(currPage);

            yield return monoBeh.DuplicateFinder.SetDuplicateFlags(currPage);
            yield return monoBeh.SpritePathSetter.SetSpritePaths(currPage);
            yield return monoBeh.SpriteDownloader.DownloadSprites(currPage);
            yield return monoBeh.SpriteGenerator.GenerateSprites(currPage);
            yield return monoBeh.SpriteColorizer.ColorizeSprites(currPage);
            yield return monoBeh.FontDownloader.DownloadFonts(currPage);
            yield return monoBeh.SpriteWorker.MarkAsSprites(currPage);

            if (monoBeh.IsUGUI())
            {
                yield return monoBeh.TransformSetter.SetTransformPos(currPage);
                yield return monoBeh.CanvasDrawer.DrawToCanvas(currPage);
                yield return monoBeh.TransformSetter.SetTransformPosAndAnchors(currPage);

                if (monoBeh.UsingSpriteRenderer())
                {
                    yield return monoBeh.CanvasDrawer.FixSpriteRenderers(currPage);
                }
                else if (monoBeh.UsingDttPui())
                {
                    yield return monoBeh.CanvasDrawer.FixDttImages(currPage);
                }
            }
            else if (monoBeh.IsUITK())
            {
#if FCU_UITK_EXT_EXISTS
                yield return monoBeh.UITK_Converter.Convert(virtualPage, currPage);
#endif
            }
            else if (monoBeh.IsNova())
            {
                yield return monoBeh.TransformSetter.SetTransformPos(currPage);
                yield return monoBeh.NovaDrawer.DrawToScene(currPage);
                yield return monoBeh.TransformSetter.SetTransformPosAndAnchors(currPage);
                yield return monoBeh.TransformSetter.SetNovaPositions(currPage);
            }

            if (monoBeh.Settings.ScriptGeneratorSettings.IsEnabled)
            {
                //yield return monoBeh.ScriptGenerator.Generate();
            }

            if (monoBeh.Settings.DebugSettings.DebugMode == false)
            {
                ClearAfterImport();
            }

            AssetTools.MakeActiveSceneDirty();
            monoBeh.Events.OnImportComplete?.Invoke(monoBeh);

            monoBeh.AssetTools.ShowRateMe();

            DALogger.LogSuccess(FcuLocKey.log_import_complete.Localize());
        }

        private List<string> GetSelectedFrameIds()
        {
            List<string> selected = monoBeh.InspectorDrawer.SelectableDocument.Childs
                .SelectMany(si => si.Childs)
                .Where(si => si.Selected)
                .Select(si => si.FObject.Id)
                .ToList();

            return selected;
        }

        public void ConvertTreeToList(FObject parent, List<FObject> fobjects)
        {
            foreach (FObject child in parent.Children)
            {
                if (child.Data.Ignore)
                    continue;

                int parentIndex = fobjects.IndexOf(parent);
                child.Data.ParentIndex = parentIndex;

                fobjects.Add(child);

                if (child.Data.Parent.ContainsTag(FcuTag.Page) == false)
                {
                    fobjects[child.Data.ParentIndex].Data.ChildIndexes.Add(fobjects.Count() - 1);
                }

                if (child.Data.ForceImage)
                {
                    //TODO: move to TagSetter.
                    child.SetFlagToAllChilds(x => x.Data.Ignore = true);
                    continue;
                }

                if (child.Children.IsEmpty())
                    continue;

                ConvertTreeToList(child, fobjects);
            }
        }

        private void ClearAfterImport()
        {
            SyncHelper[] syncHelpers = monoBeh.SyncHelpers.GetAllSyncHelpers();

            Parallel.ForEach(syncHelpers, fobject =>
            {
                fobject.Data.SpritePath = null;
                fobject.Data.Link = null;
            });

            monoBeh.ImageTypeSetter.DownloadableIds.Clear();
            monoBeh.ImageTypeSetter.GenerativeIds.Clear();
            monoBeh.ImageTypeSetter.DrawableIds.Clear();
            monoBeh.ImageTypeSetter.NoneIds.Clear();

            monoBeh.CanvasDrawer.ButtonDrawer.Buttons.Clear();
        }
    }
}
