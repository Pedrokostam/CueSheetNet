using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CueSheetNet;
internal static class ExceptionHelper
{
    public static void ThrowIfNull([NotNull]object? value, [CallerMemberName] string? name = null, string? message = null)
    {
        if (value == null)
        {
            if (message is null)
            {
                throw new ArgumentNullException(name);
            }
            throw new ArgumentNullException(name, message);
        }

    }

    public static void ThrowIfNullOrEmpty([NotNull] string? value, [CallerMemberName] string? name = null, string? message = null)
    {
        message ??= "The value cannot be an empty string.";
        if (string.IsNullOrEmpty(value))
        {
            if (message is null)
            {
                throw new ArgumentException(name);
            }
            throw new ArgumentException(name, message);
        }
    }

    public static void ThrowIfNullOrWhitespace([NotNull] string? value, [CallerMemberName] string? name = null, string? message = null)
    {
        message ??= "The value cannot be an empty string or composed entirely of whitespace.";
        if (string.IsNullOrWhiteSpace(value))
        {
            if (message is null)
            {
                throw new ArgumentException(name);
            }
            throw new ArgumentException(name, message);
        }
    }
}
