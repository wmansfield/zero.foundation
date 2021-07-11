using System.Collections.Generic;

namespace Zero.Foundation.Plugins
{
   public interface IPlugin
    {
        string DisplayName { get; }
        string DisplayVersion { get; }

        T RetrieveMetaData<T>(string token);

        object InvokeCommand(string name, Dictionary<string, object> caseInsensitiveParameters);
        
    }
}