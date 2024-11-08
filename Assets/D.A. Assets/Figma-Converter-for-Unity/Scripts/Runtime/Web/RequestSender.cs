using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.Logging;
using DA_Assets.Networking;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable IDE0052

namespace DA_Assets.FCU
{
    [Serializable]
    public class RequestSender : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        [SerializeField] float pbarProgress;
        public float PbarProgress => pbarProgress;

        [SerializeField] float pbarBytes;
        public float PbarBytes => pbarBytes;

        [SerializeField] int _requestCount;
        [SerializeField] bool _timeoutActive;
        [SerializeField] int _remainingTime;

        private static int requestCount = 0;
        private static bool timeoutActive = false;
        private static int remainingTime = 0;

        public void RefreshLimiterData()
        {
            _requestCount = requestCount;
            _timeoutActive = timeoutActive;
            _remainingTime = remainingTime;
        }

        private async Task CheckRateLimit()
        {
            while (requestCount >= FcuConfig.Instance.ApiRequestsCountLimit)
            {
                if (timeoutActive == false)
                {
                    timeoutActive = true;
                    remainingTime = FcuConfig.Instance.ApiTimeoutSec;
                    _ = LogRemainingTime();
                }

                await Task.Delay(1000);
            }

            requestCount++;
        }

        private async Task LogRemainingTime()
        {
            while (remainingTime > 0)
            {
                DALogger.Log(FcuLocKey.log_api_waiting.Localize(remainingTime));
                RefreshLimiterData();
                await Task.Delay(1000);
                remainingTime--;
            }

            requestCount = 0;
            timeoutActive = false;
        }

        public async Task<DAResult<T>> SendRequest<T>(DARequest request)
        {
            bool hasResult = false;
            DAResult<T> result = default;

            await SendRequest<T>(request, _result =>
            {
                hasResult = true;
                result = _result;
            });

            while (!hasResult)
            {
                await Task.Yield();
            }

            return result;
        }

        public async Task SendRequest<T>(DARequest request, Return<T> @return)
        {
            await CheckRateLimit();

            UnityHttpClient webRequest;

            switch (request.RequestType)
            {
                case RequestType.Post:
                    webRequest = UnityHttpClient.Post(request.Query, request.WWWForm);
                    break;
                default:
                    webRequest = UnityHttpClient.Get(request.Query);
                    break;
            }

            using (webRequest)
            {
                if (request.RequestHeader.IsDefault() == false)
                {
                    webRequest.SetRequestHeader(request.RequestHeader.Name, request.RequestHeader.Value);
                }

                try
                {
                    _ = webRequest.SendWebRequest();
                }
                catch (InvalidOperationException)
                {
                    DALogger.LogError(FcuLocKey.log_enable_http_project_settings.Localize());
                    monoBeh.AssetTools.StopImport(StopImportReason.Error);
                    return;
                }
                catch (Exception ex)
                {
                    DALogger.LogException(ex);
                }

                await UpdateRequestProgressBar(webRequest);
                await MoveRequestProgressBarToEnd();

                DAResult<T> result = new DAResult<T>();

                if (request.RequestType == RequestType.GetFile)
                {
                    result.Success = true;
                    result.Object = (T)(object)webRequest.downloadHandler.data;
                }
                else
                {
                    string text = webRequest.downloadHandler.text;

                    _ = request.WriteLog(text);

                    if (typeof(T) == typeof(string))
                    {
                        result.Success = true;
                        result.Object = (T)(object)text;
                    }
                    else
                    {
                        await TryParseResponse<T>(text, request, webRequest, x => result = x);
                    }
                }

                @return.Invoke(result);
            }
        }

        private async Task TryParseResponse<T>(string text, DARequest request, UnityHttpClient webRequest, Return<T> @return)
        {
            DAResult<T> finalResult = new DAResult<T>();

            DAResult<WebError> webError = await DAJson.FromJsonAsync<WebError>(text);

            if (webError.Object.Message != null)
            {
                FcuLogger.Debug($"TryParseResponse | 0");

                finalResult.Success = false;
                finalResult.Error = webError.Object;
                @return.Invoke(finalResult);
                return;
            }

            bool isRequestError = webRequest.result != WR_Result.Success;

            if (isRequestError)
            {
                finalResult.Success = false;

                if (webRequest.error.Contains("SSL"))
                {
                    FcuLogger.Debug($"TryParseResponse | 1");
                    finalResult.Error = new WebError(909, text);
                }
                else
                {
                    FcuLogger.Debug($"TryParseResponse | 2");
                    finalResult.Error = new WebError((int)webRequest.responseCode, webRequest.error);
                }

                @return.Invoke(finalResult);
                return;
            }

            if (text.Contains("<pre>Cannot GET "))
            {
                FcuLogger.Debug($"TryParseResponse | 3");

                finalResult.Error = new WebError(404, text);
                @return.Invoke(finalResult);
                return;
            }

            DAResult<T> obj = await DAJson.FromJsonAsync<T>(text);

            if (obj.Success)
            {
                FcuLogger.Debug($"TryParseResponse | 4");

                finalResult.Success = true;
                finalResult.Object = obj.Object;

                if (request.Name == RequestName.Project)
                {
                    monoBeh.ProjectCacher.Cache(obj.Object);
                }

                @return.Invoke(finalResult);
                return;
            }
            else
            {
                FcuLogger.Debug($"TryParseResponse | 5");

                finalResult.Success = false;
                finalResult.Error = obj.Error;
            }

            @return.Invoke(finalResult);
        }

        private async Task UpdateRequestProgressBar(UnityHttpClient webRequest)
        {
            while (webRequest.isDone == false)
            {
                if (monoBeh.IsCancellationRequested(TokenType.Import))
                    return;

                if (pbarProgress < 1f)
                {
                    pbarProgress += 0.01f;
                }
                else
                {
                    pbarProgress = 0;
                }

                if (webRequest.downloadedBytes == 0)
                {
                    pbarBytes += 100;
                }
                else
                {
                    pbarBytes = webRequest.downloadedBytes;
                }

                await Task.Yield();
            }
        }

        private async Task MoveRequestProgressBarToEnd()
        {
            float left = 1f - pbarProgress;

            int steps = 10;
            float stepIncrement = left / steps;

            for (int i = 0; i < steps; i++)
            {
                pbarProgress += stepIncrement;
                await Task.Yield();
            }

            pbarProgress = 0f;
            pbarBytes = 0f;
        }
    }
}