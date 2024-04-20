using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Helpers;

internal class StringHelper
{
    /// <summary>
    /// Count how many time the character at the specified position is repeated in a sequence.
    /// </summary>
    /// <param name="source">Character span.</param>
    /// <param name="startPosition">Position of the character to be counted.</param>
    /// <returns>The number of time the character is repeated in a sequence.</returns>
    public static int CountSubsequence(ReadOnlySpan<char> source, int startPosition)
    {
        char patternChar = source[startPosition];
        int index = startPosition + 1;
        var length = source.Length;
        while (index < length && source[index] == patternChar)
        {
            index++;
        }
        return index - startPosition;
    }
}
