using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if JSONNET_EXISTS
using Newtonsoft.Json;
#endif

namespace DA_Assets.FCU
{
    public class DAJson
    {
#if JSONNET_EXISTS
        private static JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            Error = (sender, error) => error.ErrorContext.Handled = true,
            Formatting = Formatting.Indented
        };
#endif
        public static string ToJson(object obj)
        {
#if JSONNET_EXISTS
            return JsonConvert.SerializeObject(obj, settings);
#else
            return "";
#endif
        }

        public static T FromJson<T>(string json)
        {
#if JSONNET_EXISTS
            return JsonConvert.DeserializeObject<T>(json, settings);
#else
            return default(T);
#endif
        }

        public static async Task<DAResult<T>> FromJsonAsync<T>(string json)
        {
            DAResult<T> @return = new DAResult<T>();

            bool endThread = false;

            _ = Task.Run(() =>
            {
                try
                {
#if JSONNET_EXISTS == false
                    throw new MissingComponentException("Json.NET packaghe is not installed.");
#endif
                    JFResult jfr = DAFormatter.Format<T>(json);

                    if (jfr.IsValid == false)
                    {
                        throw new Exception("Not valid json.");
                    }

                    if (jfr.MatchTargetType == false)
                    {
                        throw new InvalidCastException("The input json does not match the target type.");
                    }
#if JSONNET_EXISTS
                    @return.Object = JsonConvert.DeserializeObject<T>(json, settings);
#endif
                    @return.Success = true;
                    endThread = true;
                }
                catch (InvalidCastException ex)
                {
#if DEBUG
                    //Debug.LogException(ex);
#endif
                    @return.Success = false;
                    @return.Error = new WebError(29, null, ex);
                    endThread = true;
                }
                catch (MissingComponentException ex)
                {
#if DEBUG
                    Debug.LogException(ex);
#endif
                    @return.Success = false;
                    @return.Error = new WebError(455, null, ex);
                    endThread = true;
                }
                catch (ThreadAbortException ex)
                {
#if DEBUG
                    Debug.LogException(ex);
#endif
                    @return.Success = false;
                    @return.Error = new WebError(-1, null, ex);
                    endThread = true;
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.LogException(ex);
#endif
                    @return.Success = false;
                    @return.Error = new WebError(422, null, ex);
                    endThread = true;
                }
            });

            while (endThread == false)
            {
                await Task.Delay(100);
            }

            return @return;
        }
    }
}
