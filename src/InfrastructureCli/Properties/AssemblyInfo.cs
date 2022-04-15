using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InfrastructureCli.Tests")]

#if NETCOREAPP3_1

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

#endif