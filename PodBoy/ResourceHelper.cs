using System;
using System.Reflection;

namespace PodBoy
{
    public static class ResourceHelper
    {
        public static Uri LoadResourceUri(string pathInApplication, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            if (pathInApplication[0] == '/')
            {
                pathInApplication = pathInApplication.Substring(1);
            }
            return new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication,
                UriKind.Absolute);
        }
    }
}