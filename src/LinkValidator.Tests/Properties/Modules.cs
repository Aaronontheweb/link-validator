using System.Runtime.CompilerServices;

namespace LinkValidator.Tests.Properties;

public static class Modules
{
    [ModuleInitializer]
    public static void Initialize() =>
        VerifyDiffPlex.Initialize();
}