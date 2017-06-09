

namespace Microsoft.AspNetCore.SignalR.Tools
{
    public interface IHubProxyWriter
    {
        void WriteStartProxies();
        void WriteStartProxy(string name, string namespaceName);
        void WriteStartMethod(string name, string returnTypeName, string returnTypeNamespace);
        void WriteParameter(string name, string typeName, string typeNamespace);
        void WriteEndMethod();
        void WriteEndProxy();
        void WriteEndProxies();
    }
}
