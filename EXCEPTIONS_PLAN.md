# Exception plan for CueSheetNet

Design: standard BCL exceptions everywhere, plus exactly one custom type, `CueParseException : FormatException`, which carries the line number that `FormatException` cannot.

I read every `throw` site in `CueSheetNet/` and the types in `CueSheetNet/Exceptions/`. I did not build or run anything. Line numbers refer to the current working tree and will drift.

## Rule

| Situation | Exception |
|---|---|
| Bad argument to any method, including an argument that conflicts with the sheet's current content (time already used, time outside the file, track from another file) | `ArgumentException` / `ArgumentOutOfRangeException` / `ArgumentNullException`, with `ParamName` set |
| Object state makes the call impossible regardless of arguments (no source file, no file or track to add an index to) | `InvalidOperationException` |
| `CueTime` arithmetic | `OverflowException`, `DivideByZeroException`, `NotFiniteNumberException` |
| Malformed cue text | `CueParseException` (is a `FormatException`) |
| Malformed time string passed to `CueTime.Parse` | `FormatException` |
| Corrupt binary data (audio headers, encoding detection) | `InvalidDataException` |
| File system | BCL IO exceptions as they come |

Precedent for the edit-side rule: `Dictionary.Add` with a duplicate key throws `ArgumentException`, not a dedicated duplicate-key type.

## Current state

- `CueException` (abstract) has one real subclass; the nine editing exceptions derive from plain `Exception`.
- All 12 types except `CueException`, `InvalidDataFormatException`, and `UnsupportedDataFormatException` sit in `Exceptions/UnexpectedFileException.cs`.
- Never thrown: `CueUnexpectedItemException` (its `Throw` method throws but is declared to return the exception), `UnexpectedFileException`, `UnsupportedDataFormatException`.
- `NegativeIndexTimeException` has a private constructor that does not pass the message to the base, and the same condition throws `ArgumentOutOfRangeException` from some `Insert*` overloads (`CueSheet.cs:156`, `:201`) and `NegativeIndexTimeException` from others (`:289`, `:323`).
- `InvalidDataFormatException` (internal) duplicates `InvalidDataException`, which `CdFormatReader` and `CueEncodingTester` already use.
- The reader throws `NotImplementedException` for "index before track" and "gap before track/file" (`CueReader.cs:543`, `:575`) when `IgnoreIncorrectPlacement` is false.
- `ArgumentNullException.ThrowIfNull(cueSheet.SourceFile, "Cannot save ...")` (`CueExtensions.cs:141`, `CueWriter.cs:340`): the second parameter is the argument name, not the message, so the message is lost, and the problem is sheet state, not an argument.
- Message typos: "index index", "Atempted", `unexceptedFile`.

## Target

```csharp
namespace CueSheetNet.Exceptions;

public class CueParseException(string message, int lineNumber, string line, Exception? inner = null)
    : FormatException(message, inner)
{
    public int LineNumber { get; } = lineNumber;
    public string Line { get; } = line;
}
```

Because it derives from `FormatException`, `catch (FormatException)` and `Assert.ThrowsException<FormatException>`-style handling keep working (the latter only with a derived-type-aware assertion; see Tests). No other custom exception types. `CueException` is deleted.

Extension point: if a caller later needs to distinguish edit failures programmatically, add `CueEditException : ArgumentException` with a reason enum. That is non-breaking. Same for `UnexpectedItemException : CueParseException` for placement errors. Neither is needed now.

## Changes, by file

### Exceptions/

1. Add `CueParseException.cs` as above.
2. Delete: `CueException`, `CueUnexpectedItemException`, `UnexpectedFileException`, `UnexpectedIndexException`, `DuplicateTimeException`, `IndexOutsideOfFileException`, `IndexOutsideOfTrackException`, `InvalidTrackPlacementException`, `NegativeIndexTimeException`, `InvalidIndexTimeException`, `InvalidIndexPositionException`, `TrackSplitAcrossFileException`, `InvalidIndexEditException`, `UnsupportedDataFormatException`, `InvalidDataFormatException`. This empties `UnexpectedFileException.cs`; delete the file.
3. Keep the message text from the deleted types (with typos fixed) as the messages of the replacement exceptions below.

### CueSheet.cs

| Line | Current | Replacement |
|---|---|---|
| 156, 201 | `ArgumentOutOfRangeException.ThrowIfNegative` | no change |
| 159 | `ArgumentException` track not in file | no change; set `paramName: nameof(track)` |
| 162, 205 | `DuplicateTimeException(time, file)` | `ArgumentException($"Index {time} already exists in file \"{file}\"", nameof(time))` |
| 179 | `IndexOutsideOfTrackException` | `ArgumentOutOfRangeException(nameof(time), time, "...outside of the track's range")` |
| 230 | `InvalidTrackPlacementException(firstIndex)` | `ArgumentException(..., nameof(firstIndex))` |
| 289, 323 | `NegativeIndexTimeException.ThrowIfNegative` | `ArgumentOutOfRangeException.ThrowIfNegative`, same as 156 and 201 |
| 339, 355, 376 | `InvalidIndexPositionException` | `ArgumentException` or `ArgumentOutOfRangeException`, param `index` |
| 346 | `TrackSplitAcrossFileException(track)` | `ArgumentException(..., nameof(track))` |
| 361 | `InvalidIndexTimeException` | `ArgumentOutOfRangeException`, param `index` |
| 372 | `UnexpectedIndexException` | `InvalidOperationException("Cannot add an index without a file or track")` |
| 403 (`ThrowIfOutsideOfFile`) | `IndexOutsideOfFileException` | `ArgumentOutOfRangeException(nameof(time), time, "...greater than the duration of the file")`; the helper needs the parameter name, so pass it in |

### CueIndex.cs

- Line 116: `InvalidIndexEditException` becomes `ArgumentOutOfRangeException(nameof(newTime), newTime, "...")`. `CueIndexTests.MoveTimeOutOfBoundsThrows` updates to match.
- Line 38: already standard.

### CueReader.cs

- Per-line dispatch (around `:305`): wrap in one try/catch so no edit failure escapes parsing:

  ```csharp
  try { /* existing dispatch */ }
  catch (Exception e) when (e is ArgumentException or InvalidOperationException)
  {
      throw new CueParseException($"Line {CurrentLineIndex}: {e.Message}", CurrentLineIndex, line.ToString(), e);
  }
  ```

  The arguments given to `AddIndex`, `InsertFile`, and `InsertTrack` come from file text, so an argument error there is a content error. The original exception stays as `InnerException`. Risk: this can mask a reader bug that passes a wrong argument; `InnerException` and the test below are the mitigation. The reader's own `InvalidOperationException` at `:187` and `:191` (no source configured) are thrown before the per-line loop, so they stay outside the filter.
- Existing `throw new FormatException(...)` at `:305`, `:549`, `:556`, `:582` become `CueParseException` with the line number and line.
- `:543` and `:575` (`NotImplementedException`) become `CueParseException`.
- Keep `UnreachableException` at `:202`, `:203` and `InvalidEnumArgumentException` at `:204`.

### CueExtensions.cs and CueWriter.cs

- `CueExtensions.cs:141` and `CueWriter.cs:340`: replace `ArgumentNullException.ThrowIfNull(sheet.SourceFile, "...")` with `if (sheet.SourceFile is null) throw new InvalidOperationException("Cannot save cue sheet without a source file. Use SaveAs() ...");`.
- Lines 22 and 71 (source file does not exist): `InvalidOperationException` is correct when `SourceFile` is null. If the path is set but the file is missing, throw `FileNotFoundException`. Check which case each message means and split if both occur.
- Lines 73 and 103 (path is a drive root): `DirectoryNotFoundException` does not describe it; use `ArgumentException` with the parameter name.

### FormatReaders/

- Replace `InvalidDataFormatException` with `InvalidDataException` in `FlacFormatReader.cs:53,57` and `WaveFormatReader.cs:63,72,76,94,98`.
- Check `FormatReader.cs:61` (`catch (Exception x)`) to confirm nothing depends on the old type.

### CueTime.cs

No new types.
- Keep `DivideByZeroException` (`:318`, `:688`, `:698`), `NotFiniteNumberException` (`:696`, `:718`), `OverflowException` (`:573`), and `ArgumentException` in `CompareTo(object)` (`:637`; the `IComparable` contract).
- Lines 484 and 487: check what `Parse` does with malformed text. Convention (`int.Parse`): null throws `ArgumentNullException`, anything unparseable throws `FormatException`. If these lines throw `ArgumentException` for empty or malformed input, change to `FormatException`.

### Smaller items

- `Remarks.cs:40`: `new InvalidOperationException()` has no message. Add one, or use `UnreachableException` if the branch cannot occur.
- `CueWriter.cs:118`, `NameParsing/PropertyParseToken.cs:13,30`, `PathHelper.cs:52`: `NotSupportedException`, fine.
- `FilePath.cs`, `Remark.cs:25`, `CueWriterSettings.cs`: `ArgumentException`, fine.
- `ResettableSlice.cs:94,105`: `KeyNotFoundException`, fine.

## Tests

- MSTest `Assert.ThrowsException<T>` matches the exact type. Assertions that name a deleted type need new targets:
  - `EditingTests.CannotAddDuplicateIndex`: `DuplicateTimeException` becomes `ArgumentException`; add `ParamName == "time"`.
  - `CueIndexTests.MoveTimeOutOfBoundsThrows`: `InvalidIndexEditException` becomes `ArgumentOutOfRangeException`.
  - `EditingTests.InsertIndexWithNegativeTimeThrows` and `InsertIndexWithTrackFromOtherFileThrows`: already standard, no change.
- `ParsingTests.AssertFormatError`: accept only `CueParseException`. `UnknownKeywordThrowsFormatException` (`ThrowsException<FormatException>`) becomes `ThrowsException<CueParseException>`.
- Add tests:
  - Bad sheets (duplicate index time, index before first track with `IgnoreIncorrectPlacement = false`, gap before track, track split across files, index number out of range) throw `CueParseException`; for the edit-derived cases, `InnerException` is an `ArgumentException`, and `LineNumber` is correct.
  - `InsertTrack` with a negative time throws `ArgumentOutOfRangeException` (currently `NegativeIndexTimeException`).
  - `Save()` on a sheet without `SourceFile` throws `InvalidOperationException`.
  - Run every `.cue` in a bad-input test folder and assert the only exceptions that come out are `CueParseException` or `InvalidDataException` (encoding detection).

## Compatibility

Removing 15 public and internal exception types is a breaking change for callers that name them, and `Insert*`/`MoveTime` change the exception types they throw. Release as a major version, or note it in the changelog if the package is still pre-1.0. `catch (FormatException)` and `catch (ArgumentException)` callers keep working.

## Order of work

1. Add `CueParseException`.
2. Switch `CueSheet.cs` and `CueIndex.cs` to standard exceptions (with `ParamName`) and update the edit tests.
3. Reader: convert `FormatException` and `NotImplementedException` to `CueParseException`; add the per-line wrap; add the bad-sheet tests.
4. Replace `InvalidDataFormatException` with `InvalidDataException`.
5. Fix `CueExtensions`/`CueWriter` (`ThrowIfNull` misuse, drive-root, missing-file).
6. Check `CueTime.Parse` exception types.
7. Delete the unused types and the old file; fix remaining compile errors; run the full suite after each step.
