using log4net;
using Moq;
using NEA.ArchiveModel;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using WolfPack.Lib.Services;

namespace WolfpackTest
{
    public class PackagePlannerTests
    {
        [Test]
        public void PlanPackage_Item_ReturnSinglePackage()
        {
            var items = GetItems(PackPriority.FirstPackage, 1024, 1);
            var itemFeeder = GetItemFeederMock(items);
            var log = new Mock<ILog>();

            var planner = new PackagePlanner(1024, "", itemFeeder, log.Object, "prefix");
            planner.Plan();

            Assert.AreEqual(1, planner.PlannedPackages.Count);
            Assert.NotNull(planner.PlannedPackages[0].RelativePath);
            Assert.AreEqual(items.Count, planner.PlannedPackages[0].Items.Count);
        }

        [Test]
        public void PlanPackage_FilesWithFirstPackagePriorityOverSizeLimit_ReturnSinglePackage()
        {
            var items = GetItems(PackPriority.FirstPackage, 1025, 10);
            var itemFeeder = GetItemFeederMock(items);
            var log = new Mock<ILog>();

            var planner = new PackagePlanner(1024, "", itemFeeder, log.Object, "prefix");
            planner.Plan();

            Assert.AreEqual(1, planner.PlannedPackages.Count);
            Assert.AreEqual(items.Count, planner.PlannedPackages[0].Items.Count);
        }

        [Test]
        public void PlanPackage_FilesWithHighPriorityUnderSizeLimit_ReturnSinglePackage()
        {
            var items = GetItems(PackPriority.High, 1024, 1);
            var itemFeeder = GetItemFeederMock(items);
            var log = new Mock<ILog>();

            var planner = new PackagePlanner(1024, "", itemFeeder, log.Object, "prefix");
            planner.Plan();

            Assert.AreEqual(1, planner.PlannedPackages.Count);
            Assert.AreEqual(items.Count, planner.PlannedPackages[0].Items.Count);
        }

        [Test]
        public void PlanPackage_FilesWithHighPriorityOverSizeLimit_ReturnMultiplePackage()
        {
            var items = GetItems(PackPriority.High, 1024, 10);
            var itemFeeder = GetItemFeederMock(items);
            var log = new Mock<ILog>();

            var planner = new PackagePlanner(1024, "", itemFeeder, log.Object, "prefix");
            planner.Plan();

            Assert.AreEqual(10, planner.PlannedPackages.Count);
            Assert.AreNotEqual(planner.PlannedPackages[0].RelativePath, planner.PlannedPackages[1].RelativePath);
        }

        [Test]
        public void PlanPackage_ItemsWithSizeOverSizeLimit_ReturnOnePackagePerFile()
        {
            var items = GetItems(PackPriority.High, 2048, 1);
            items.AddRange(GetItems(PackPriority.Low, 2048, 1));

            var itemFeeder = GetItemFeederMock(items);
            var log = new Mock<ILog>();

            var planner = new PackagePlanner(1024, "", itemFeeder, log.Object, "prefix");
            planner.Plan();

            Assert.AreEqual(2, planner.PlannedPackages.Count);
        }

        [Test]
        public void PlanPackage_FilesWithMixedPriority_ReturnMultiplePackageWithHighPriorityFirst()
        {
            var items = new List<PrioritizableValidatableItem>();

            var item1 = new PrioritizableValidatableItem();
            item1.Priority = PackPriority.Low;
            item1.Size = 2048;
            item1.RelativePath = "lowPriorityFile.txt";
            items.Add(item1);


            var item2 = new PrioritizableValidatableItem();
            item2.Priority = PackPriority.High;
            item1.Size = 2048;
            item2.RelativePath = "highPriorityFile.txt";
            items.Add(item2);

            var itemFeeder = GetItemFeederMock(items);
            var log = new Mock<ILog>();

            var planner = new PackagePlanner(1024, "", itemFeeder, log.Object, "prefix");
            planner.Plan();

            Assert.AreEqual(2, planner.PlannedPackages.Count);
            Assert.AreEqual("highPriorityFile.txt", planner.PlannedPackages[0].Items[0].RelativePath);
            Assert.AreEqual("lowPriorityFile.txt", planner.PlannedPackages[1].Items[0].RelativePath);
        }

        [Test]
        [TestCase(PackPriority.FirstPackage)]
        [TestCase(PackPriority.High)]
        [TestCase(PackPriority.Low)]
        public void PlanPackage_FilesWithSamePriority_ReturnPackageWithItemsOrderedByRelativePath(PackPriority priority)
        {
            var items = new List<PrioritizableValidatableItem>();

            var item1 = new PrioritizableValidatableItem();
            item1.Priority = priority;
            item1.Size = 1;
            item1.RelativePath = "fileB.txt";
            items.Add(item1);


            var item2 = new PrioritizableValidatableItem();
            item2.Priority = priority;
            item1.Size = 1;
            item2.RelativePath = "fileA.txt";
            items.Add(item2);

            var itemFeeder = GetItemFeederMock(items);
            var log = new Mock<ILog>();

            var planner = new PackagePlanner(1024, "", itemFeeder, log.Object, "prefix");
            planner.Plan();

            Assert.AreEqual(1, planner.PlannedPackages.Count);
            Assert.AreEqual("fileA.txt", planner.PlannedPackages[0].Items[0].RelativePath);
            Assert.AreEqual("fileB.txt", planner.PlannedPackages[0].Items[1].RelativePath);
        }

        private static IItemFeeder GetItemFeederMock(List<PrioritizableValidatableItem> items)
        {
            var itemFeeder = new Mock<IItemFeeder>();
            itemFeeder.Setup(i => i.GetItems()).Returns(items);
            return itemFeeder.Object;
        }

        private static List<PrioritizableValidatableItem> GetItems(PackPriority priority, long size, int count)
        {
            var items = new List<PrioritizableValidatableItem>();

            // Add packages with given priority
            for (int i = 0; i < count; i++)
            {
                var item = new PrioritizableValidatableItem();
                item.Priority = priority;

                // 1 mb size
                item.Size = size;

                items.Add(item);
            }

            return items;
        }
    }
}
