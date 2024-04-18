namespace CueSheetNet;

/// <summary>
/// Defines an object that has belongs to a parent sheet.
/// </summary>
public interface IParentSheet
{
    CueSheet ParentSheet { get; }
}