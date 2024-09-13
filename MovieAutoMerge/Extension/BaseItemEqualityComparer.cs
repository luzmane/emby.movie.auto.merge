using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using MediaBrowser.Controller.Entities;

namespace MovieAutoMerge.Extension
{
    [ExcludeFromCodeCoverage]
    internal class BaseItemEqualityComparer : IEqualityComparer<BaseItem>
    {
        /// <inheritdoc />
        public bool Equals(BaseItem x, BaseItem y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Id.Equals(y.Id);
        }

        /// <inheritdoc />
        public int GetHashCode(BaseItem obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
