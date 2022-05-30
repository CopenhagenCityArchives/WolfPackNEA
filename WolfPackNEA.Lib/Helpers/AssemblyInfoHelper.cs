using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace WolfPack.Lib.Helpers
{
    public class AssemblyInfoHelper
    {
        public static IEnumerable<string> GetInfo()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            
            //Make an array for the list of assemblies.
            Assembly[] assems = currentDomain.GetAssemblies();

            foreach (var assem in assems)
            {
                if(assem.GetName().Name.Equals("WolfPackNEA.Lib") || assem.GetName().Name.Equals("WolfPack.Lib")){
                    yield return $"{assem.GetName().Name} {assem.GetName().Version} depends on: ";
                    var referencedAssemblies = assem.GetReferencedAssemblies();
                    foreach (var assembly in referencedAssemblies)
                        yield return $" -> {assembly.Name} {assembly.Version}";
                }
            }
        }
    }
}
