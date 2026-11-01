using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CueSheetNet.Test;

/// <summary>
/// Helper class for efficient debug output that avoids string formatting overhead
/// when no debugger is attached.
/// </summary>
internal static class DebugHelper
{
    /// <summary>
    /// Writes a line using an interpolated string, which is only evaluated when
    /// the debugger is actually attached. This avoids expensive string allocations
    /// in Release builds when debugging is not active.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLine(string message)
    {
        Debug.WriteLineIf(Debugger.IsAttached, message);
    }

    /// <summary>
    /// Writes a line using an interpolated string with a custom condition.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLineIf(bool condition, string message)
    {
        Debug.WriteLineIf(condition, message);
    }
}
