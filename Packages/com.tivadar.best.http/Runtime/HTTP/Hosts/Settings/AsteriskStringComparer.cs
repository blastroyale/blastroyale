using System.Collections.Generic;

namespace Best.HTTP.Hosts.Settings
{
    /// <summary>
    /// Moves any added asterisk(*) to the end of the list.
    /// </summary>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstruction]
    internal sealed class AsteriskStringComparer : IComparer<string>
    {
        public static readonly AsteriskStringComparer Instance = new AsteriskStringComparer();

        public int Compare(string x, string y)
        /*{
            var comparedTo = x.CompareTo(y);

            // Equal?
            if (comparedTo == 0)
                return 0;

            return (x, y) switch
            {
                ("*", _) => 1,
                (_, "*") => -1,
                _ => x.CompareTo(y)
            };
        }*/
        => (x, y) switch
        {
            ("*", "*") => 0,
            ("*", _) => 1,
            (_, "*") => -1,
            _ => x.CompareTo(y)
        };
    }
}
