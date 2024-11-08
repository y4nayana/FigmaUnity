using DA_Assets.DAI;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DA_Assets.Singleton
{
    public class AssetConfig<T> : SingletonScriptableObject<T> where T : ScriptableObject
    {
        public string ProductVersion => productVersion;
        [SerializeField] string productVersion;

        public InternalLocalizator Localizator => localizator.Init();
        [SerializeField] InternalLocalizator localizator;
    }

    [Serializable]
    public class InternalLocalizator
    {
        [SerializeField] TextAsset locFile;
        private List<LocItem> localizations = new List<LocItem>();

        bool _loading = false;

        public InternalLocalizator Init()
        {
            if (locFile == null)
            {
                throw new NullReferenceException("Localization file missing.");
            }

            if (localizations.IsEmpty() && !_loading)
            {
                _loading = true;

                DARunner.ExecuteOnMainThread(() =>
                {
                    localizations = ConvertFileToLocItems(locFile.text);
                    _loading = false;
                });
            }

            return this;
        }

        private List<LocItem> ConvertFileToLocItems(string csvText)
        {
            List<LocItem> locItems = new List<LocItem>();

            using (var reader = new StringReader(csvText))
            {
                string line;
                LocItem currentLocItem = default;
                StringBuilder currentText = null;

                while ((line = reader.ReadLine()) != null)
                {
                    if (currentLocItem.Equals(default(LocItem)))
                    {
                        string[] columns = line.Split(';');
                        if (columns.Length >= 2)
                        {
                            currentLocItem = new LocItem
                            {
                                key = columns[0],
                                en = columns[1]
                            };

                            if (currentLocItem.en.StartsWith("\"") && !currentLocItem.en.EndsWith("\""))
                            {
                                currentText = new StringBuilder();
                                currentText.AppendLine(currentLocItem.en.TrimStart('\"'));
                            }
                            else
                            {
                                locItems.Add(currentLocItem);
                                currentLocItem = default;
                            }
                        }
                    }
                    else if (currentText != null)
                    {
                        if (line.EndsWith("\"") && !line.EndsWith(";\""))
                        {
                            currentText.AppendLine(line.TrimEnd('\"'));
                            currentLocItem.en = currentText.ToString();
                            locItems.Add(currentLocItem);
                            currentLocItem = default;
                            currentText = null;
                        }
                        else
                        {
                            currentText.AppendLine(line);
                        }
                    }
                }
            }

            return locItems;
        }

        public string GetLocalizedText(Enum key, params object[] args)
        {
            return GetLocalizedText(key.ToString(), args);
        }

        private string GetLocalizedText(string key, params object[] args)
        {
            foreach (LocItem item in localizations)
            {
                if (item.key == key)
                {
                    string txt = item.en;

                    if (txt == "")
                    {
                        return key;
                    }

                    try
                    {
                        return string.Format(txt, args);
                    }
                    catch
                    {
                        return txt;
                    }
                }
            }

            return key;
        }
    }

    internal struct LocItem
    {
        internal string key { get; set; }
        internal string en { get; set; }
    }
}