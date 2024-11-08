using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class GameObjectDrawer : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        List<GameObject> tempRectFrames = new List<GameObject>();

        public void ClearTempRectFrames()
        {
            foreach (var item in tempRectFrames)
            {
                if (item != null)
                {
                    item.Destroy();
                }
            }

            tempRectFrames.Clear();
        }

        public void Draw(FObject parent)
        {
            DALogger.Log(FcuLocKey.log_instantiate_game_objects.Localize());
            DrawFObject(parent);
        }

        public void DrawFObject(FObject parent)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                if (monoBeh.IsCancellationRequested(TokenType.Import))
                    return;

                FObject fobject = parent.Children[i];

                if (fobject.Data.IsEmpty)
                {
                    FcuLogger.Debug($"InstantiateGameObjects | continue | {fobject.Data.NameHierarchy}");
                    continue;
                }

                SyncHelper syncHelper;

                if (monoBeh.SyncHelpers.IsExistsOnCurrentCanvas(fobject, out syncHelper))
                {
                    FcuLogger.Debug($"InstantiateGameObjects | 1 | {fobject.Data.NameHierarchy}", FcuLogType.GameObjectDrawer);
                }
                else if (monoBeh.CurrentProject.HasLocalPrefab(fobject.Data, out SyncHelper localPrefab))
                {
                    FcuLogger.Debug($"InstantiateGameObjects | 2 | {fobject.Data.NameHierarchy}", FcuLogType.GameObjectDrawer);
#if UNITY_EDITOR
                    syncHelper = (SyncHelper)UnityEditor.PrefabUtility.InstantiatePrefab(localPrefab);
#endif
                    int counter = 0;
                    monoBeh.SyncHelpers.SetFcuToAllChilds(syncHelper.gameObject, ref counter);

                    SetFigmaIds(fobject, syncHelper);
                    monoBeh.Events.OnObjectInstantiate?.Invoke(monoBeh, fobject.Data.GameObject);
                }
                else
                {
                    FcuLogger.Debug($"InstantiateGameObjects | 3 | {fobject.Data.NameHierarchy}", FcuLogType.GameObjectDrawer);
                    syncHelper = MonoBehExtensions.CreateEmptyGameObject().AddComponent<SyncHelper>();
                    monoBeh.Events.OnObjectInstantiate?.Invoke(monoBeh, fobject.Data.GameObject);
                }

                fobject.SetData(syncHelper, monoBeh);
                fobject.Data.GameObject.name = fobject.Data.Names.ObjectName;

                if (monoBeh.IsUGUI())
                {
                    fobject.Data.GameObject.TryAddComponent(out RectTransform _1);
                }

                AddRectGameObject(fobject);

                int goLayer;

                if (fobject.ContainsTag(FcuTag.Blur))
                {
                    goLayer = LayerTools.AddLayer(FcuConfig.Instance.BlurredObjectTag);
                }
                else
                {
                    goLayer = monoBeh.Settings.MainSettings.GameObjectLayer;
                }

                fobject.Data.GameObject.layer = goLayer;

                SetParent(fobject, parent);
                SetParentRect(fobject, parent);

                if (fobject.Children.IsEmpty())
                    continue;

                DrawFObject(fobject);
            }
        }

        private void AddRectGameObject(FObject fobject)
        {
            GameObject rectGameObject = MonoBehExtensions.CreateEmptyGameObject();
            rectGameObject.name = fobject.Data.GameObject.name + " | RectTransform";

            if (fobject.ContainsTag(FcuTag.Frame))
            {
                tempRectFrames.Add(rectGameObject);
            }

            fobject.Data.RectGameObject = rectGameObject;
            fobject.Data.RectGameObject.TryAddComponent(out RectTransform _2);

            fobject.Data.RectGameObject.TryAddComponent(out Image rectImg);
            rectImg.color = monoBeh.GraphicHelpers.GetRectTransformColor(fobject);
        }

        private void SetParent(FObject fobject, FObject parent)
        {
            if (!fobject.Data.GameObject.transform.parent.IsPartOfAnyPrefab())
            {
                Transform pt = parent.Data.GameObject.transform;
                fobject.Data.GameObject.transform.SetParent(pt);
            }
        }

        private void SetParentRect(FObject fobject, FObject parent)
        {
            if (!fobject.Data.RectGameObject.transform.parent.IsPartOfAnyPrefab())
            {
                Transform pt = parent.Data.RectGameObject.transform;
                fobject.Data.RectGameObject.transform.SetParent(pt);
            }
        }

        private void SetFigmaIds(FObject rootFObject, SyncHelper rootSyncObject)
        {
            Dictionary<string, int> items = new Dictionary<string, int>();

            foreach (var childIndex in rootFObject.Data.ChildIndexes)
            {
                if (monoBeh.CurrentProject.TryGetByIndex(childIndex, out FObject childFO))
                {
                    items.Add(childFO.Id, childFO.Data.Hash);
                }
            }

            SyncHelper[] soChilds = rootSyncObject.GetComponentsInChildren<SyncHelper>(true);

            foreach (var soChild in soChilds)
            {
                string idToRemove = null;

                foreach (var item in items)
                {
                    if (item.Value == soChild.Data.Hash)
                    {
                        idToRemove = item.Key;
                        break;
                    }
                }

                if (idToRemove == null)
                    continue;

                items.Remove(idToRemove);
                soChild.Data.Id = idToRemove;

                if (monoBeh.CurrentProject.TryGetById(idToRemove, out FObject gbi))
                {
                    SetFigmaIds(gbi, soChild);
                }
            }
        }

        public async Task DestroyMissing(IEnumerable<SyncData> diffCheckResult)
        {
            foreach (SyncData item in diffCheckResult)
            {
                try
                {
                    FcuLogger.Debug($"DestroyMissing | {item.NameHierarchy}");
                    item.GameObject.Destroy();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(ex);
                }

                await Task.Yield();
            }
        }

        public async Task DestroyMissing(List<FObject> fobjects)
        {
            SyncHelper[] syncHelpers = monoBeh.SyncHelpers.GetAllSyncHelpers();

            ConcurrentBag<SyncHelper> toDestroy = new ConcurrentBag<SyncHelper>();

            Parallel.ForEach(syncHelpers, syncHelper =>
            {
                bool find = false;

                foreach (FObject fobject in fobjects)
                {
                    if (syncHelper.Data.Id == fobject.Data.Id)
                    {
                        find = true;
                        break;
                    }
                }

                if (find == false)
                {
                    FcuLogger.Debug($"DestroyMissing | {syncHelper.Data.NameHierarchy}");
                    toDestroy.Add(syncHelper);
                }
            });

            foreach (SyncHelper sh in toDestroy)
            {
                try
                {
                    sh.gameObject.Destroy();
                }
                catch
                {

                }

                await Task.Yield();
            }
        }
    }
}