using System.Collections.Concurrent;

namespace SharedMoleRes.Server
{
    public static class DicStorage
    {
        public static ConcurrentDictionary<string, object> Storage = new ConcurrentDictionary<string, object>();
    }
}
