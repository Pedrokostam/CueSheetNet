using System.Runtime.InteropServices;

namespace CueSheetNet.TextParser;

/// <summary>
/// Used to replace all occurences of double quotes in the field's value.
/// Quotation parsing is not standardized and some music players may ignore everything after the seconds occurence of a double quote.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct InnerQuotation
{
    private const char DoubleQuote = '"';

    public const char DefaultOpening = '“';
    public const char DefaultClosing = '”';

    /// <summary>
    /// No replacement is done. Strings returned as is. (Replacement quotes set to '"').
    /// </summary>
    public static readonly InnerQuotation NoReplacement = new(DoubleQuote);
    /// <summary>
    /// Quotation marks consisting of simple symmetrical apostrophes. 'Example'.
    /// </summary>
    public static readonly InnerQuotation Apostrophes = new('\'', '\'');
    /// <summary>
    /// Quotation marks consisting of two asymmetrical double quotes, both at the top. Commonly used in English. “Example”.
    /// </summary>
    public static readonly InnerQuotation CurvedDoubleTopQuotation = new(DefaultOpening, DefaultClosing);
    /// <summary>
    /// Quotation marks consisting of two asymmetrical double quotes, both comma like, opening one at the bottom, closing one at the top. „Example”.
    /// </summary>
    public static readonly InnerQuotation CommaLikeBottomTopQuotation = new('„', '”');
    /// <summary>
    /// Quotation marks consisting of two asymmetrical double angle marks. Also known as guillemets. «Example».
    /// </summary>
    public static readonly InnerQuotation DoubleAngleQuotation = new('«', '»');
    /// <summary>
    /// Quotation marks consisting of two asymmetrical double angle marks. «Example»
    /// </summary>
    public static readonly InnerQuotation Guillemets = DoubleAngleQuotation;

    public readonly bool Symmetrical => ClosingQuote == OpeningQuote;
    /// <summary>
    /// Return whether this quotation replacement is redundant, i.e. replaces '"' with '"'
    /// </summary>
    private readonly bool Redundant => ClosingQuote == DoubleQuote && OpeningQuote == DoubleQuote;

    public char OpeningQuote { get; init; }
    public char ClosingQuote { get; init; }
    /// <summary>
    /// Initialize a new instance of InnerQuotation with default opening and closing
    /// </summary>
    public InnerQuotation()
    {
        OpeningQuote = DefaultOpening;
        ClosingQuote = DefaultClosing;
    }
    /// <summary>
    /// Initialize a new instance of InnerQuotation with specified opening and closing
    /// </summary>
    public InnerQuotation(char openingQuote, char closingQuote) : this()
    {
        OpeningQuote = openingQuote;
        ClosingQuote = closingQuote;
    }
    /// <summary>
    /// Initialize a new instance of InnerQuotation with specified char as both the opening and closing
    /// </summary>
    public InnerQuotation(char symmetricalQuote) : this(symmetricalQuote, symmetricalQuote) { }

    /// <summary>
    /// Replaces any double standard quotes ( " ) in the string with <see cref="OpeningQuote"/> and <see cref="ClosingQuote"/>.
    /// If replacement quotes are set to double quotes, string is returned as is.
    /// </summary>
    /// <param name="input">Input string to replace double quotes in.</param>
    /// <returns>The input string with double quotes replaced by <see cref="OpeningQuote"/> and <see cref="ClosingQuote"/>.</returns>
    //[return: NotNullIfNotNull(nameof(input))]
    public string? ReplaceQuotes(string? input)
    {
        if (input == null || Redundant)
            return input;

        if (Symmetrical)
            return input.Replace(DoubleQuote, OpeningQuote);

        return ReplaceNonsymmetricalQuotes(input);
    }

    public static implicit operator InnerQuotation(char symmetricalQuotation) => new(symmetricalQuotation);

    public bool IsValid => OpeningQuote != default && ClosingQuote != default;

    private string ReplaceNonsymmetricalQuotes(string input)
    {
        List<int> ints = [];
        int j = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == DoubleQuote)
            {
                ints.Add(i);
            }
        }
        if (ints.Count == 0)
            return input;
        Span<char> inter = stackalloc char[input.Length];
        input.CopyTo(inter);
        //For the first half of the list
        for (int i = 0; i < ints.Count / 2; i++)
        {
            // take left and right extrema
            int openingIndex = ints[i]; // opening index (from left)
            inter[openingIndex] = OpeningQuote;
            int closingIndex = ints[^(i + 1)]; // closing index (from right) - +1 because ^0 is outside collection
            inter[closingIndex] = ClosingQuote;
        }
        if (ints.Count % 2 == 1) // if there was an odd number of quotations, the middle one will be an opening
        {
            inter[j / 2 + 1] = OpeningQuote;
        }
        return inter.ToString();
    }
}
