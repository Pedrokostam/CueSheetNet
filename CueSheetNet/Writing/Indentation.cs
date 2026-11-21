using System.Runtime.InteropServices;

namespace CueSheetNet;

[StructLayout(LayoutKind.Auto)]
public readonly record struct Indentation
{
    public char Character
    {
        get;
        init
        {
            if (!char.IsWhiteSpace(value))
                throw new ArgumentException($"Indentation character must be whitespace (is: '{value}' - 0x{(int)value:X})", nameof(value));
            field = value;
        }
    }
    public int Depth
    {
        get;
        init => field = Math.Max(value, 0);
    }
    public Indentation(int depth, char character)
    {
        Depth = depth;
        Character = character;
    }
    /// <summary>
    /// Indentation of 2 spaces per level.
    /// </summary>
    public static readonly Indentation Standard = new Indentation(2, ' ');
    /// <summary>
    /// Indentation of 1 tab per level.
    /// </summary>
    public static readonly Indentation Tab = new Indentation(1, '\t');
    /// <summary>
    /// No indentation, all lines will start without any whitespace.
    /// </summary>
    public static readonly Indentation None = new Indentation(0, ' ');

    public string GetIndentation(int level) => Depth > 0 ? new string(Character, level * Depth) : string.Empty;
    public StringBuilder AppendIndentation(int level, StringBuilder builder) => Depth > 0 ? builder.Append(Character, level * Depth) : builder;
}
