using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MovieAutoMerge.ScheduledTasks.Model;

using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MovieAutoMerge.Utils
{
    internal static class EmbyHelper
    {
        private static readonly object Locker = new Object();

        internal static Dictionary<string, string> GetAvailableTranslations(string key)
        {
            var basePath = Plugin.Instance.GetType().Namespace + $".i18n.{key}.";
            return typeof(EmbyHelper).Assembly.GetManifestResourceNames()
                .Where(i => i.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                .Select(i =>
                    new TranslationInfo
                    {
                        Locale = Path.GetFileNameWithoutExtension(i.Substring(basePath.Length)),
                        EmbeddedResourcePath = i
                    })
                .ToDictionary(i => i.Locale, j => j.EmbeddedResourcePath);
        }

        internal static TaskTranslation GetTaskTranslation(
            Dictionary<string, TaskTranslation> translations,
            IServerConfigurationManager serverConfigurationManager,
            IJsonSerializer jsonSerializer,
            Dictionary<string, string> availableTranslations)
        {
            if (translations.TryGetValue(serverConfigurationManager.Configuration.UICulture, out TaskTranslation translation))
            {
                return translation;
            }

            lock (Locker)
            {
                if (!translations.TryGetValue(serverConfigurationManager.Configuration.UICulture, out TaskTranslation tmp))
                {
                    if (!availableTranslations.TryGetValue(serverConfigurationManager.Configuration.UICulture, out var resourcePath))
                    {
                        resourcePath = availableTranslations["en-US"];
                    }

                    using (Stream stream = typeof(EmbyHelper).Assembly.GetManifestResourceStream(resourcePath))
                    {
                        translation = jsonSerializer.DeserializeFromStream<TaskTranslation>(stream);
                    }

                    if (translation != null)
                    {
                        translations.Add(serverConfigurationManager.Configuration.UICulture, translation);
                    }
                }
                else
                {
                    translation = tmp;
                }
            }

            return translation;
        }
    }
}
