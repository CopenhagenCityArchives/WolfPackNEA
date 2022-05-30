using System;
using System.Collections.Generic;
using System.Text;

namespace WolfPack.Lib.Services
{
    public interface IItemFeeder
    {
        IEnumerable<PrioritizableValidatableItem> GetItems();
        void AddItem(string absolutePath, string relativePath, byte[] checksum, PackPriority priority);
        bool CanFeedFromLocation(string location);
    }
}
