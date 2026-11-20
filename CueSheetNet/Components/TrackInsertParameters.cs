using CueSheetNet.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Components;

public readonly record struct TrackInsertParameters
{
    public TrackType Type { get; init; }
    public string? Title { get; init; }
    public string? Performer { get; init; }
    public string? Songwriter { get; init; }
    public string? Isrc { get; init; }
    public TrackFlags Flags { get; init; }
    public int? Position { get; init; }
    public CueTime? Postgap { get; init; }
    public CueTime? Pregap { get; init; }

    public TrackInsertParameters(string type) : this(TrackType.FromString(type))
    {
    }
    public TrackInsertParameters([DisallowNull] TrackType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
    }
    public static TrackInsertParameters Audio => new TrackInsertParameters(TrackType.AUDIO);

}
