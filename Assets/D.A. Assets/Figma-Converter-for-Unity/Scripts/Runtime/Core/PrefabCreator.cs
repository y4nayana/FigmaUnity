using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using DA_Assets.DAI;
using DA_Assets.Tools;
using DA_Assets.Logging;
using DA_Assets.Extensions;
using System.Threading.Tasks;
using System.Threading;

#if NOVA_UI_EXISTS
using Nova;
#endif

namespace DA_Assets.FCU
{
    [Serializable]
    public class PrefabCreator : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        private List<GameObject> toDestroy;
        [SerializeField] List<PrefabStruct> pstructs;

        public void CreatePrefabs()
        {
            _ = CreatePrefabsAsync();
        }

        public async Task CreatePrefabsAsync()
        {
            CancellationTokenSource cts = monoBeh.CancellationTokenController.CreateNew(TokenType.Prefab);
            CancellationToken token = cts.Token;

            try
            {
#if UNITY_EDITOR
                DALogger.Log(FcuLocKey.log_start_creating_prefabs.Localize());
                await Task.Delay(100);

                bool backuped = SceneBackuper.TryBackupActiveScene();

                if (!backuped)
                {
                    DALogger.LogError(FcuLocKey.log_cant_execute_because_no_backup.Localize());
                    return;
                }

                SceneBackuper.MakeActiveSceneDirty();

                pstructs = new List<PrefabStruct>();
                toDestroy = new List<GameObject>();
                int prefabCount = 0;

                SyncHelper[] syncHelpers = monoBeh.SyncHelpers.GetAllSyncHelpers();
                monoBeh.SyncHelpers.RestoreRootFrames(syncHelpers);

                foreach (SyncHelper syncObject in syncHelpers)
                {
                    if (syncObject.IsPartOfAnyPrefab())
                    {
                        continue;
                    }

                    PrefabStruct ps = CreatePrefabStruct(syncObject);
                    pstructs.Add(ps);
                }

                int prefabNumber = 1;

                for (int i = 0; i < pstructs.Count(); i++)
                {
                    for (int j = 0; j < pstructs.Count(); j++)
                    {
                        if (i == j)
                            continue;

                        if (pstructs[i].Hash != pstructs[j].Hash)
                            continue;

                        if (pstructs[j].PrefabNumber == 0)
                        {
                            PrefabStruct ps = pstructs[i];
                            ps.PrefabNumber = prefabNumber;
                            pstructs[i] = ps;
                            prefabNumber++;
                            break;
                        }
                        else
                        {
                            PrefabStruct ps = pstructs[i];
                            ps.PrefabNumber = pstructs[j].PrefabNumber;
                            pstructs[i] = ps;
                            break;
                        }
                    }
                }

                for (int i = 0; i < pstructs.Count(); i++)
                {
                    PrefabStruct psi = pstructs[i];

                    for (int j = 0; j < pstructs.Count(); j++)
                    {
                        PrefabStruct psj = pstructs[j];

                        if (psi.Hash != psj.Hash)
                            continue;

                        if (psi.PrefabNumber != psj.PrefabNumber)
                            continue;

                        if (psi.Current.Data.CanBePrefab() == false)
                            continue;

                        token.ThrowIfCancellationRequested();

                        if (psj.Prefab == null)
                        {
                            CreatePrefab(ref pstructs, i);
                            prefabCount++;
                        }
                        else
                        {
                            InstantiatePrefab(ref psi, i, psj.Prefab);
                        }

                        break;
                    }

                }

                foreach (GameObject item in toDestroy)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        item.Destroy();
                    }
                    catch (Exception ex)
                    {
                        DALogger.LogException(ex);
                    }
                }

                foreach (var ps in pstructs)
                {
                    token.ThrowIfCancellationRequested();

                    if (ps.PrefabPath == null)
                        continue;

                    if (ps.InstantiatedPrefab == null)
                        continue;

                    if (UnityEditor.PrefabUtility.GetPrefabAssetType(ps.InstantiatedPrefab.gameObject) == UnityEditor.PrefabAssetType.NotAPrefab)
                        continue;

                    ps.InstantiatedPrefab.gameObject.SaveAsPrefabAsset(ps.PrefabPath, out SyncHelper savedPrefab, out Exception ex);
                }

                foreach (PrefabStruct ps in pstructs)
                {
                    token.ThrowIfCancellationRequested();

                    if (ps.Current.Data.CanBePrefab() == false)
                    {
                        try
                        {
                            ps.Current.gameObject.Destroy();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }

                    }
                }

                await monoBeh.SyncHelpers.SetFcuToAllSyncHelpersAsync(null);

                syncHelpers = monoBeh.SyncHelpers.GetAllSyncHelpers();
                monoBeh.SyncHelpers.RestoreRootFrames(syncHelpers);

                DALogger.Log(FcuLocKey.log_prefabs_created.Localize(prefabCount));
#endif
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"The task was stopped.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private PrefabStruct CreatePrefabStruct(SyncHelper syncObject)
        {
            PrefabStruct ps = new PrefabStruct();

            ps.Current = syncObject;

            ps.Parent = syncObject.transform.parent.GetComponent<SyncHelper>();

            List<SyncHelper> childs = syncObject.GetComponentsInChildren<SyncHelper>(true).ToList();
            childs.RemoveAt(0);

            ps.Hash = syncObject.Data.Hash;
            ps.Id = syncObject.Data.Id;
            ps.Childs = childs.ToArray();

            if (monoBeh.IsUGUI())
            {
                ps.Current.Data.UguiTransformData = UguiTransformData.Create(syncObject.GetComponent<RectTransform>());
            }
            else if (monoBeh.IsNova())
            {
#if NOVA_UI_EXISTS
                ps.Current.Data.NovaTransformData = new NovaTransformData(syncObject.GetComponent<UIBlock>());
#endif
            }

            ps.SiblingIndex = syncObject.transform.GetSiblingIndex();
            ps.PrefabNumber = 0;

            return ps;
        }

        private void CreatePrefab(ref List<PrefabStruct> pstructs, int i)
        {
            PrefabStruct ps = pstructs[i];

            RemoveParent(ref ps);

            ps.PrefabPath = GetPrefabPath(ps);

            if (ps.Current.gameObject.SaveAsPrefabAsset(ps.PrefabPath, out SyncHelper savedPrefab, out Exception ex))
            {
                ps.Prefab = savedPrefab;
                InstantiatePrefab(ref ps, i, ps.Prefab);
            }
            else
            {
                DALogger.LogException(ex);
            }

            RestoreParent(ref ps);
            pstructs[i] = ps;
        }

        private void RestoreParent(ref PrefabStruct ps)
        {
            if (ps.Current.Data.Tags.Contains(FcuTag.Frame))
            {
                ps.Current.transform.SetParent(monoBeh.transform);
            }
            else
            {
                ps.Current.transform.SetParent(ps.Parent.transform);
            }

            foreach (SyncHelper childTransform in ps.Childs)
            {
                if (childTransform == null)
                    continue;

                childTransform.transform.SetParent(ps.InstantiatedPrefab.transform);
            }
        }

        private void RemoveParent(ref PrefabStruct ps)
        {
            ps.Current.transform.SetParent(null);

            foreach (SyncHelper childTransform in ps.Childs)
            {
                if (childTransform == null)
                    continue;

                childTransform.transform.SetParent(null);
            }
        }

        private void InstantiatePrefab(ref PrefabStruct ps, int i, UnityEngine.Object pref)
        {
#if UNITY_EDITOR
            if (ps.Current.Data.CanBePrefab() == false)
            {
                ps.InstantiatedPrefab = ps.Current;
            }
            else
            {

                ps.InstantiatedPrefab = (SyncHelper)UnityEditor.PrefabUtility.InstantiatePrefab(pref);

                if (ps.Current.Data.Tags.Contains(FcuTag.Frame))
                {
                    ps.InstantiatedPrefab.transform.SetParent(monoBeh.transform);
                }
                else
                {
                    ps.InstantiatedPrefab.transform.SetParent(ps.Parent.transform);
                }

                ps.InstantiatedPrefab.name = ps.Current.name;
                ps.InstantiatedPrefab.transform.SetSiblingIndex(pstructs[i].SiblingIndex);

                if (monoBeh.IsUGUI())
                {
                    ps.Current.Data.UguiTransformData.ApplyTo(ps.InstantiatedPrefab.GetComponent<RectTransform>());
                }
                else if (monoBeh.IsNova())
                {
#if NOVA_UI_EXISTS
                    ps.Current.Data.NovaTransformData.ApplyTo(ps.InstantiatedPrefab.GetComponent<UIBlock>());
#endif
                }

                ps.InstantiatedPrefab.Data.Id = ps.Id;

                if (ps.Current.Data.Tags.Contains(FcuTag.Text))
                {
                    ps.InstantiatedPrefab.gameObject.SetText(ps.Current.gameObject.GetText(), monoBeh.IsNova(), monoBeh.UsingTextMesh());
                }

                toDestroy.Add(ps.Current.gameObject);
            }

            pstructs[i] = ps;
#endif
        }

        private string GetPrefabName(SyncHelper sh)
        {
            string prefabName;

            if (monoBeh.Settings.PrefabSettings.TextPrefabNameType != TextPrefabNameType.Figma &&
                sh.Data.Tags.Contains(FcuTag.Text))
            {
                prefabName = $"{sh.Data.Names.HumanizedTextPrefabName.Trim()} {sh.Data.Hash}";
            }
            else
            {
                prefabName = $"{sh.gameObject.name.Trim()} {sh.Data.Hash}";
            }

            return prefabName;
        }

        private string GetPrefabPath(PrefabStruct ps)
        {
            string GetSubDir(SyncData sd)
            {
                try
                {
                    if (sd.Tags.Contains(FcuTag.Text))
                        return "Texts";
                    else if (sd.Tags.Contains(FcuTag.Image))
                        return "Images";
                    else if (sd.Tags.Contains(FcuTag.Button))
                        return "Buttons";
                    else if (sd.Tags.Contains(FcuTag.InputField))
                        return "InputFields";
                    else
                        return "Other";
                }
                catch
                {
                    return "Other";
                }
            }

            if (ps.Current.Data.RootFrame == null)
            {
                SyncData myRootFrame = monoBeh.SyncHelpers.GetRootFrame(ps.Current.Data);
                ps.Current.Data.RootFrame = myRootFrame;
            }

            Debug.Log($"{ps.Current} | {ps.Current?.Data} | {ps.Current?.Data?.RootFrame} | {ps.Current?.Data?.RootFrame?.Names}");
            string frameDir = Path.Combine(monoBeh.Settings.PrefabSettings.PrefabsPath, ps.Current.Data.RootFrame.Names.FileName);

            string subDir = GetSubDir(ps.Current

.Data);
            string fullDir = Path.Combine(frameDir, subDir);
            fullDir.CreateFolderIfNotExists();

            string prefabName = GetPrefabName(ps.Current);
            string prefabPath = Path.Combine(frameDir, subDir, $"{prefabName}.prefab");

            string result = prefabPath.GetPathRelativeToProjectDirectory();
            return result;
        }
    }
}