using Emby.Web.GenericEdit.Elements;

using MediaBrowser.Common;
using MediaBrowser.Model.Logging;
using MovieAutoMerge.UI;
using MovieAutoMerge.UIBaseClasses.Store;

namespace MovieAutoMerge.Storage
{
    public class PluginOptionsStore : SimpleFileStore<MainPageUI>
    {
        public PluginOptionsStore(IApplicationHost applicationHost, ILogger logger, string pluginFullName)
            : base(applicationHost, logger, pluginFullName)
        {
        }

        public override void SetOptions(MainPageUI newOptions)
        {
            newOptions.SplitProviderId = string.Empty;
            newOptions.SplitProviderType = string.Empty;
            newOptions.SplitStatusItem.Status = ItemStatus.Unavailable;
            newOptions.SplitStatusItem.StatusText = "Operation not started yet";

            base.SetOptions(newOptions);
        }
    }
}
