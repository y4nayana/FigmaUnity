using DA_Assets.FCU.Model;
using DA_Assets.DAI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using DA_Assets.FCU.Extensions;
using UnityEngine;
using DA_Assets.Logging;
using DA_Assets.Extensions;
using System.Collections.Concurrent;

#if JSONNET_EXISTS
using Newtonsoft.Json;
#endif

namespace DA_Assets.FCU
{
    [Serializable]
    public class SpriteDownloader : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        private int _maxConcurrentDownloads = 100;
        private int _maxDownloadAttempts = 3;
        private float _maxChunkSize = 24_000_000;
        private int _errorLogSplitLimit = 50;
        private int _logDelayMs = 1000;

        public async Task DownloadSprites(List<FObject> fobjects)
        {
            DALogger.Log($"DownloadSprites");

            List<FObject> needDownload = fobjects.Where(x => x.Data.NeedDownload).ToList();

            if (needDownload.IsEmpty())
            {
                DALogger.Log($"DownloadSprites no need");
                return;
            }

            int totalCount = needDownload.Count;
            int downloadedCount = 0;
            int lastLoggedCount = -1;

            CancellationTokenSource downloadLogTokenSource = new CancellationTokenSource();

            var missingSpriteLinks = await GetSpriteLinks(needDownload);

            SemaphoreSlim semaphore = new SemaphoreSlim(_maxConcurrentDownloads);
            List<Task> tasks = new List<Task>();
            Dictionary<string, FObject> idToFObject = needDownload.ToDictionary(f => f.Id);

            ConcurrentBag<FObject> failedObjects = new ConcurrentBag<FObject>();

            Task downloadLoggingTask = Task.Run(async () =>
            {
                while (!downloadLogTokenSource.Token.IsCancellationRequested)
                {
                    if (lastLoggedCount != downloadedCount)
                    {
                        DALogger.Log(FcuLocKey.log_downloading_images.Localize(downloadedCount, totalCount));
                        lastLoggedCount = downloadedCount;
                    }

                    try
                    {
                        await Task.Delay(_logDelayMs, downloadLogTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, downloadLogTokenSource.Token);

            DALogger.Log(FcuLocKey.log_start_download_images.Localize());

            foreach (var formatChunks in missingSpriteLinks)
            {
                foreach (var chunk in formatChunks.Value)
                {
                    foreach (var idFormatLink in chunk)
                    {
                        await semaphore.WaitAsync();
                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                bool success = await DownloadSprite(idFormatLink, idToFObject, _maxDownloadAttempts);

                                if (success)
                                {
                                    Interlocked.Increment(ref downloadedCount);
                                }
                                else
                                {
                                    if (idToFObject.TryGetValue(idFormatLink.Id, out FObject failedFObject))
                                    {
                                        failedObjects.Add(failedFObject);
                                    }
                                }
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                        tasks.Add(task);
                    }
                }
            }

            await Task.WhenAll(tasks);

            downloadLogTokenSource.Cancel();
            await downloadLoggingTask;

            DALogger.Log(FcuLocKey.log_downloading_images.Localize(downloadedCount, totalCount));

            LogFailedDownloads(failedObjects);
        }

        public async Task<bool> DownloadSprite(IdFormatLink idFormatLink, Dictionary<string, FObject> idToFObject, int maxDownloadAttempts)
        {
            if (idFormatLink.Link.IsEmpty())
            {
                return false;
            }

            bool hasFObject = idToFObject.TryGetValue(idFormatLink.Id, out FObject fobject);

            DARequest request = new DARequest
            {
                RequestType = RequestType.GetFile,
                Query = idFormatLink.Link
            };

            DAResult<byte[]> result = default;

            int attempts = 0;

            while (attempts < maxDownloadAttempts && result.Object == null)
            {
                attempts++;
                result = await monoBeh.RequestSender.SendRequest<byte[]>(request);
            }

            switch (result.Error.Status)
            {
                case 909:
                    DALogger.LogError(FcuLocKey.log_ssl_error.Localize(result.Error.Message, result.Error.Status));
                    monoBeh.Events.OnImportFail?.Invoke(monoBeh);
                    monoBeh.AssetTools.StopImport(StopImportReason.Error);
                    break;
            }

            if (result.Object != null && hasFObject)
            {
                try
                {
                    File.WriteAllBytes(fobject.Data.SpritePath, result.Object);
                    return true;
                }
                catch (Exception ex)
                {
                    DALogger.Log($"Failed to write sprite '{fobject.Data.SpritePath}': {ex.Message}");
                    return false;
                }
            }

            return false;
        }

        public async Task<Dictionary<ImageFormat, List<List<IdFormatLink>>>> GetSpriteLinks(List<FObject> fobjects)
        {
            var idFormatChunks = GetIdFormatChunks(fobjects);
            var idFormatLinkChunks = new Dictionary<ImageFormat, List<List<IdFormatLink>>>();

            int totalLinks = idFormatChunks.Sum(kvp => kvp.Value.Count * kvp.Value.SelectMany(list => list).Count());
            int obtainedLinks = 0;
            int lastLoggedLinks = -1;

            CancellationTokenSource linkLogTokenSource = new CancellationTokenSource();

            Task linkLoggingTask = Task.Run(async () =>
            {
                while (!linkLogTokenSource.Token.IsCancellationRequested)
                {
                    if (lastLoggedLinks != obtainedLinks)
                    {
                        DALogger.Log(FcuLocKey.log_getting_links.Localize(obtainedLinks, totalLinks));
                        lastLoggedLinks = obtainedLinks;
                    }

                    try
                    {
                        await Task.Delay(_logDelayMs, linkLogTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, linkLogTokenSource.Token);

            foreach (var idFormatChunk in idFormatChunks)
            {
                if (monoBeh.IsCancellationRequested(TokenType.Import))
                    break;

                foreach (List<IdFormatLink> chunk in idFormatChunk.Value)
                {
                    IEnumerable<string> ids = chunk.Select(x => x.Id);

                    DARequest request = RequestCreator.CreateImageLinksRequest(
                        monoBeh.Settings.MainSettings.ProjectUrl,
                        idFormatChunk.Key.ToString().ToLower(),
                        monoBeh.Settings.ImageSpritesSettings.ImageScale,
                        ids,
                        monoBeh.Authorizer.Token);

                    DAResult<FigmaImageRequest> result = await monoBeh.RequestSender.SendRequest<FigmaImageRequest>(request);
                    FigmaImageRequest fir = result.Object;

                    if (!idFormatLinkChunks.ContainsKey(idFormatChunk.Key))
                    {
                        idFormatLinkChunks[idFormatChunk.Key] = new List<List<IdFormatLink>>();
                    }

                    if (fir.images == null)
                    {
                        break;
                    }

                    List<IdFormatLink> linkChunk = new List<IdFormatLink>();

                    foreach (var idFormat in chunk)
                    {
                        fir.images.TryGetValue(idFormat.Id, out string link);

                        if (monoBeh.Settings.MainSettings.Https == false)
                        {
                            link = link.Replace("https://", "http://");
                        }

                        linkChunk.Add(new IdFormatLink
                        {
                            Id = idFormat.Id,
                            Format = idFormat.Format,
                            Link = link ?? string.Empty
                        });

                        Interlocked.Increment(ref obtainedLinks);
                    }

                    idFormatLinkChunks[idFormatChunk.Key].Add(linkChunk);
                }
            }

            linkLogTokenSource.Cancel();
            await linkLoggingTask;

            DALogger.Log(FcuLocKey.log_getting_links.Localize(obtainedLinks, totalLinks));

            return idFormatLinkChunks;
        }

        public Dictionary<ImageFormat, List<List<IdFormatLink>>> GetIdFormatChunks(List<FObject> fobjects)
        {
            var formatChunks = new Dictionary<ImageFormat, List<List<IdFormatLink>>>();
            var currentChunkSizes = new Dictionary<ImageFormat, float>();

            foreach (FObject fobject in fobjects)
            {
                if (!fobject.GetBoundingSize(out Vector2 bSize))
                {
                    continue;
                }

                float area = bSize.x * bSize.y;
                ImageFormat imageFormat = fobject.Data.ImageFormat;

                if (!formatChunks.ContainsKey(imageFormat))
                {
                    formatChunks[imageFormat] = new List<List<IdFormatLink>> { new List<IdFormatLink>() };
                    currentChunkSizes[imageFormat] = 0;
                }

                List<List<IdFormatLink>> chunks = formatChunks[imageFormat];
                List<IdFormatLink> currentChunk = chunks[chunks.Count - 1];
                float currentChunkSize = currentChunkSizes[imageFormat];

                if (currentChunkSize + area > _maxChunkSize)
                {
                    currentChunk = new List<IdFormatLink>();
                    chunks.Add(currentChunk);
                    currentChunkSize = 0;
                }

                string extension = imageFormat.ToString();
                currentChunk.Add(new IdFormatLink { Id = fobject.Id, Format = extension });
                currentChunkSize += area;
                currentChunkSizes[imageFormat] = currentChunkSize;
            }

            return formatChunks;
        }

        private void LogFailedDownloads(ConcurrentBag<FObject> failedObjects)
        {
            if (failedObjects.Count() > 0)
            {
                List<List<string>> comps = failedObjects.Select(x => x.Data.NameHierarchy).Split(_errorLogSplitLimit);

                foreach (List<string> comp in comps)
                {
                    string hierarchies = string.Join("\n", comp);

                    DALogger.LogError(
                        FcuLocKey.log_malformed_url.Localize(comp.Count, hierarchies));
                }
            }
        }

        public struct FigmaImageRequest
        {
#if JSONNET_EXISTS
            [JsonProperty("err")]
#endif
            public string error;
#if JSONNET_EXISTS
            [JsonProperty("images")]
#endif
            // key = id, value = link
            public Dictionary<string, string> images;
        }

        public struct IdFormatLink
        {
            public string Id { get; set; }
            public string Format { get; set; }
            public string Link { get; set; }
        }
    }

}





