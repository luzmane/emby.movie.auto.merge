using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Serialization;

namespace MovieAutoMerge.I18n
{
    public class PluginResourceManager
    {
        private const string CultureFallback = "en";

        private static readonly object Locker = new Object();

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private string _currentCulture = CultureFallback;
        private readonly Dictionary<string, string> _availableTranslations;
        private Dictionary<string, string> _currentTranslation = new Dictionary<string, string>();

        public PluginResourceManager(IJsonSerializer jsonSerializer, IServerConfigurationManager serverConfigurationManager)
        {
            _jsonSerializer = jsonSerializer;
            _serverConfigurationManager = serverConfigurationManager;
            var basePath = typeof(Plugin).Namespace + ".I18n.Resources.";
            _availableTranslations = typeof(PluginResourceManager).Assembly.GetManifestResourceNames()
                .Where(i => i.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(i => Path.GetFileNameWithoutExtension(i.Substring(basePath.Length)), j => j);
        }

        public string GetString(string name)
        {
            var cultureName = _serverConfigurationManager.Configuration.UICulture.ToLowerInvariant();
            if (cultureName == _currentCulture && _currentTranslation.Count > 0)
            {
                return _currentTranslation[name];
            }

            if (_availableTranslations.TryGetValue(cultureName, out var resourcePath))
            {
                lock (Locker)
                {
                    using (Stream stream = typeof(PluginResourceManager).Assembly.GetManifestResourceStream(resourcePath))
                    {
                        _currentTranslation = _jsonSerializer.DeserializeFromStream<Dictionary<string, string>>(stream);
                        _currentCulture = cultureName;
                    }
                }

                return _currentTranslation[name];
            }
            else
            {
                lock (Locker)
                {
                    using (Stream stream = typeof(PluginResourceManager).Assembly.GetManifestResourceStream(_availableTranslations[CultureFallback]))
                    {
                        _currentTranslation = _jsonSerializer.DeserializeFromStream<Dictionary<string, string>>(stream);
                        _currentCulture = CultureFallback;
                    }
                }

                return _currentTranslation[name];
            }
        }
    }
}
