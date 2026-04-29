using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace TraceEventTests
{
    public class WindowsFactAttribute : FactAttribute
    {
        public WindowsFactAttribute([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base(sourceFilePath, sourceLineNumber)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "Test requires Windows platform";
            }
        }
    }
}
