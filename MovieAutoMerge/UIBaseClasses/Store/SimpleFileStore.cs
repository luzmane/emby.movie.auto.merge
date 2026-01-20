using System;
using System.IO;

using Emby.Web.GenericEdit;

using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MovieAutoMerge.UIBaseClasses.Store
{
    public class SimpleFileStore<TOptionType> : SimpleContentStore<TOptionType> where TOptionType : EditableOptionsBase, new()
    {
        private readonly ILogger _logger;
        private readonly string _pluginFullName;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly string _pluginConfigPath;

        public SimpleFileStore(IApplicationHost applicationHost, ILogger logger, string pluginFullName)
        {
            _logger = logger;
            _pluginFullName = pluginFullName;
            _jsonSerializer = applicationHost.Resolve<IJsonSerializer>();
            _fileSystem = applicationHost.Resolve<IFileSystem>();

            var applicationPaths = applicationHost.Resolve<IApplicationPaths>();
            _pluginConfigPath = applicationPaths.PluginConfigurationsPath;

            if (!_fileSystem.DirectoryExists(_pluginConfigPath))
            {
                _fileSystem.CreateDirectory(_pluginConfigPath);
            }

            OptionsFileName = $"{pluginFullName}.json";
        }

        public event EventHandler<FileSavingEventArgs> FileSaving;

        public event EventHandler<FileSavedEventArgs> FileSaved;

        public virtual string OptionsFileName { get; }

        public string OptionsFilePath => Path.Combine(_pluginConfigPath, OptionsFileName);

        public override TOptionType GetOptions()
        {
            lock (_lockObj)
            {
                return _options ?? ReloadOptions();
            }
        }

        public TOptionType ReloadOptions()
        {
            lock (_lockObj)
            {
                try
                {
                    _options = !_fileSystem.FileExists(OptionsFilePath)
                        ? new TOptionType()
                        : _jsonSerializer.DeserializeFromFile<TOptionType>(OptionsFilePath);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error loading plugin options for {0} from {1}", ex, _pluginFullName, OptionsFilePath);
                }

                return _options ?? new TOptionType();
            }
        }

        public override void SetOptions(TOptionType newOptions)
        {
            if (newOptions == null)
            {
                throw new ArgumentNullException(nameof(newOptions));
            }

            var savingArgs = new FileSavingEventArgs(newOptions);
            FileSaving?.Invoke(this, savingArgs);

            if (savingArgs.Cancel)
            {
                return;
            }

            lock (_lockObj)
            {
                _jsonSerializer.SerializeToFile(newOptions, OptionsFilePath);

                _options = newOptions;
            }

            var savedArgs = new FileSavedEventArgs(newOptions);
            FileSaved?.Invoke(this, savedArgs);
        }
    }
}
