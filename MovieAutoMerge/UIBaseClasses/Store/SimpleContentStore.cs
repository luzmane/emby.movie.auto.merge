using System;

using Emby.Web.GenericEdit;

namespace MovieAutoMerge.UIBaseClasses.Store
{
    public class SimpleContentStore<TOptionType> where TOptionType : EditableOptionsBase, new()
    {
        protected readonly object _lockObj = new object();
        protected TOptionType _options;

        public virtual TOptionType GetOptions()
        {
            lock (_lockObj)
            {
                return _options ?? (_options = new TOptionType());
            }
        }

        public virtual void SetOptions(TOptionType newOptions)
        {
            if (newOptions == null)
            {
                throw new ArgumentNullException(nameof(newOptions));
            }

            lock (_lockObj)
            {
                _options = newOptions;
            }
        }
    }
}
