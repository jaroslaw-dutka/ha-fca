using System.Collections.Specialized;

namespace FcaAssistant.Extensions;

public static class NameValueCollectionExtensions
{
    public static Dictionary<string, string> ToDictionary(this NameValueCollection nameValueCollection) => 
        nameValueCollection.AllKeys.ToDictionary(key => key ?? string.Empty, key => nameValueCollection[key] ?? string.Empty);
}