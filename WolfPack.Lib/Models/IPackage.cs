using System.Collections.Generic;
using System.IO;

namespace WolfPack.Lib.Services
{
    public interface IPackage : IValidatableItem
    {
        List<IValidatableItem> Items { get; set; }
        IValidatableItem GetItemByRelativePath(string relativePath);
    }
}