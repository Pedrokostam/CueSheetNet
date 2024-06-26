﻿namespace CueSheetNet;

/// <summary>
/// Which fields are set for a CUE object.
/// </summary>
[Flags]
public enum FieldsSet
{
    None = 0,
    Title = 1 << 0,
    Performer = 1 << 1,
    Composer = 1 << 2,
}
