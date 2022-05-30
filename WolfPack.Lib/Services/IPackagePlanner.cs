using System.Collections.Generic;

namespace WolfPack.Lib.Services
{
    public interface IPackagePlanner
    {
        public void Plan();
        public void SetItems();
        public List<Package> SetPlannedPackages();
        public void WriteItemsPackagesMapCSV();
        public List<Package> PlannedPackages { get; set; }
    }
}