using System.Runtime.InteropServices;
using System.Text;

namespace BllipParser.Native
{
    internal static class NativeMethods
    {
        public const int SupportedMaxNumThread = 64;//This is defined by a macro called MAXNUMTHREADS. If that value changed, please also change this value.

        private const string DllName = "bllip_parser_for_nvbg.dll";//defined in the project configuration of "BLLIP Parser Dll for NVBG" project

        [DllImport(DllName, EntryPoint = "initialize", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Initialize(int argc, [In] string[] argv);

        [DllImport(DllName, EntryPoint = "parse_and_format_to_buffer", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ParseAndFormatToBuffer(int threadId, string text, uint bufferSize, StringBuilder buffer);
    }
}
