using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace MIG.SurveyPlatform.MapGeneration.Serialization
{
    /// <summary>
    /// Orders strings containing integers how humans would expect.
    /// e.g. Natural string comparison returns "1", "2", "10" instead of "1", "10", "2"
    /// https://stackoverflow.com/a/248613
    /// </summary>
    public sealed class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            if (a == b) return 0; // It seemed to hang in the case where a and b equals null
            return SafeNativeMethods.StrCmpLogicalW(a, b);
        }

        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }

        public static NaturalStringComparer Instance { get; } = new NaturalStringComparer();
    }
}