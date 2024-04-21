using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CueSheetNet.Helpers;

internal static class ExceptionHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull(
        [NotNull] object? value,
        [CallerMemberName] string? name = null,
        string? message = null
    )
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEqual<T>(
        T expected,
        T actual,
        string message
    )
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException(message);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty(
        [NotNull] string? value,
        [CallerMemberName] string? name = null,
        string? message = null
    )
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhitespace(
        [NotNull] string? value,
        [CallerMemberName] string? name = null,
        string? message = null
    )
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
