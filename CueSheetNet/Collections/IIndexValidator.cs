namespace CueSheetNet.Collections;

public interface IIndexValidator
{
    /// <summary>
    /// Checks whether <paramref name="value"/> can be inserted at the given index.
    /// Requirements:
    /// <list type="table">
    ///     <item>
    ///         <term><see cref="CueIndex.Time">Time</see></term>
    ///         <description>later than preceding index, earlier than following.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CueIndex.Number">Number</see></term>
    ///         <description>greater than preceding index, smaller than following. Only checks in the file of the parent track.</description>
    ///     </item>
    /// </list>
    /// <para>
    /// The preceding values is at <c>(<paramref name="index"/>-1)</c><br/>
    /// The following value if at <c>(<paramref name="index"/>)</c> or <c>(<paramref name="index"/>+1)</c>, depending on <paramref name="replacesItem"/>.
    /// </para>
    /// </summary>
    /// <param name="replacesItem">Set it to <see langword="true"/> if the validation should not test against the item at the <paramref name="index"/>.
    /// <para/>
    /// Set it to <see langword="false"/> if that value should be treated as if it was a following value.
    /// </param>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <exception cref="NotImplementedException"></exception>
    public bool ValidateIndex(int index, CueTime time, int number, bool replacesItem);
}
