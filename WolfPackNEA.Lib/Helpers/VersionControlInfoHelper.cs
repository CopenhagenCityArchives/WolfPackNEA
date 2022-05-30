using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WolfPack.Lib.Helpers
{
    public class VersionControlInfoHelper
    {
        public static IEnumerable<string> GetInfo()
        {
            string[] wantedFields = { "MajorMinorPatch", "BranchName", "Sha", "UncommittedChanges", "CommitDate", "InformationalVersion" };
            var gitVersionInformationType = Assembly.GetExecutingAssembly().GetType("GitVersionInformation");
            var fields = gitVersionInformationType.GetFields();

            foreach (var field in fields)
            {
                if (wantedFields.Contains(field.Name))
                {
                    yield return $"{field.Name}: {field.GetValue(null)}";
                }
            }
        }
    }
}
