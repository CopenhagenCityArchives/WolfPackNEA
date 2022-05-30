using System;
using System.Collections.Generic;
using System.Text;

namespace WolfPack.Lib.Helpers
{
    public class EnvironmentInfoHelper
    {
        public static IEnumerable<string> GetInfo()
        {
            yield return $"Machine name: {Environment.MachineName}";
            yield return $"OS version: {Environment.OSVersion}";
            yield return $"User domain name: {Environment.UserDomainName}";
            yield return $"User name: {Environment.UserName}";
            yield return $"Processor count: {Environment.ProcessorCount}";
            yield return $"WolfPack directory: {Environment.CurrentDirectory}";
            yield return $"Common language runtime: {Environment.Version} ";
        }
    }
}
