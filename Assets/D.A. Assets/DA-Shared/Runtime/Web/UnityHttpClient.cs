using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.Networking
{
    public class UnityHttpClient : IDisposable
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private HttpRequestMessage _requestMessage;
        private CancellationTokenSource _cancellationTokenSource;

        public DownloadHandler downloadHandler { get; private set; }

        public long downloadedBytes { get; private set; }
        public string error { get; private set; }
        public bool isDone { get; private set; }
        public long responseCode { get; private set; }
        public WR_Result result { get; private set; }

        public float downloadProgress { get; private set; }

        private UnityHttpClient(HttpMethod method, string url, HttpContent content = null)
        {
            _requestMessage = new HttpRequestMessage(method, url);
            if (content != null)
            {
                _requestMessage.Content = content;
            }

            downloadHandler = new DownloadHandler();
            _cancellationTokenSource = new CancellationTokenSource();

            downloadedBytes = 0;
            error = string.Empty;
            isDone = false;
            responseCode = 0;
            result = WR_Result.InProgress;
            downloadProgress = 0f;
        }

        public static UnityHttpClient Get(string url)
        {
            return new UnityHttpClient(HttpMethod.Get, url);
        }

        public static UnityHttpClient Post(string url, WWWForm form)
        {
            var content = new ByteArrayContent(form.data);

            foreach (var header in form.headers)
            {
                content.Headers.Add(header.Key, header.Value);
            }
            return new UnityHttpClient(HttpMethod.Post, url, content);
        }

        public void SetRequestHeader(string name, string value)
        {
            if (_requestMessage.Headers.Contains(name))
            {
                _requestMessage.Headers.Remove(name);
            }
            _requestMessage.Headers.Add(name, value);
        }

        public async Task SendWebRequest()
        {
            try
            {
                using (var response = await _httpClient.SendAsync(_requestMessage, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    responseCode = (long)response.StatusCode;

                    if (response.IsSuccessStatusCode)
                    {
                        var contentLength = response.Content.Headers.ContentLength ?? -1L;
                        var buffer = new byte[8192];
                        int bytesRead;

                        using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            var memoryStream = new System.IO.MemoryStream();
                            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token).ConfigureAwait(false)) > 0)
                            {
                                memoryStream.Write(buffer, 0, bytesRead);
                                downloadedBytes += bytesRead;

                                if (contentLength > 0)
                                {
                                    downloadProgress = (float)downloadedBytes / contentLength;
                                }
                                else
                                {
                                    downloadProgress = -1f;
                                }
                            }

                            var data = memoryStream.ToArray();
                            var encoding = Encoding.UTF8; 
                            var charset = response.Content.Headers.ContentType?.CharSet;

                            if (!string.IsNullOrEmpty(charset))
                            {
                                try
                                {
                                    encoding = Encoding.GetEncoding(charset);
                                }
                                catch (Exception)
                                {
                                    Debug.LogWarning($"Unsupported encoding '{charset}', using UTF-8 instead.");
                                }
                            }

                            var text = encoding.GetString(data);

                            downloadHandler.SetData(data);
                            downloadHandler.SetText(text);
                            result = WR_Result.Success;
                        }
                    }
                    else
                    {
                        downloadHandler.SetError($"Error: {response.StatusCode}");
                        error = $"Error: {response.StatusCode}";
                        result = WR_Result.ProtocolError;
                    }
                }
            }
            catch (Exception ex)
            {
                downloadHandler.SetError(ex.Message);
                error = ex.Message;
                result = WR_Result.ConnectionError;
            }
            finally
            {
                isDone = true;
            }
        }

        public void Dispose()
        {
            _requestMessage?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }

    public class DownloadHandler
    {
        public byte[] data { get; private set; }
        public string text { get; private set; }
        public string error { get; private set; }

        public void SetData(byte[] data)
        {
            this.data = data;
        }

        public void SetText(string text)
        {
            this.text = text;
        }

        public void SetError(string error)
        {
            this.error = error;
        }

        public bool HasError => !string.IsNullOrEmpty(error);
    }

    public enum WR_Result
    {
        InProgress,
        Success,
        ConnectionError,
        ProtocolError,
        DataProcessingError
    }
}