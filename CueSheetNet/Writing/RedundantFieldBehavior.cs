namespace CueSheetNet;

public enum RedundantFieldBehavior
{
    /// <summary>
    /// Only write inheritable track fields that are different from sheet implicit value.
    /// </summary>
    Never,
    /// <summary>
    /// Write inheritable fields only if they were explicitly set in the track, even if the value is the same as inherited
    /// </summary>
    IfExplicitlySet,
    /// <summary>
    /// Always write all inheritable fields, even if not set.
    /// </summary>
    Always,
};
