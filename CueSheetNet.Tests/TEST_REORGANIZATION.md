# Test reorganization plan

Goal: one class per subject under test, so every test about the same behavior lives together. Folders group classes by area. Folder names do not have to match class names. No test logic changes; only moves, renames, and extracting duplicated helpers.

## Problems in the current layout

- `CueTimeTests` and `CueTimeFormatTests` both cover CueTime parsing, and both cover arithmetic/overflow. They are split by when they were written, not by topic.
- `EditingTests` (17 tests) mixes index editing, track editing, and file navigation.
- `StructureTests` mixes clone-preservation, write-preservation, and sheet-layout tests.
- Clone tests are scattered: `SavingTests.LoadCloneSame`, `StructureTests.*Cloned`, `FilePathTests.CloneDoesNotShareCdTextFile`, `TrackNavigationTests.CloneDoesNotMaterializeInheritedRemarksOnTracks`.
- `SavingTests` and `RoundTripTests` cover the same topic (parse, write, compare). `SavingTests.Files` takes one argument and covers only the `SavingTests` folder; `RoundTripTests.Files` takes folder and file name and covers four folders.
- `LoadingTests` only asserts parsed values, so it is a parsing test.
- `TreeParsingTests` tests `CueTreeFormatter` (name formatting), not parsing. The name is misleading.
- A sheet-loading helper is copy-pasted in several classes (see Shared code).

## Target layout

```
Support/
    Utils.cs                  (moved, unchanged)
    Extensions.cs             (moved, unchanged)
    AssemblyInfo.cs           (new, holds the TestDataSourceDiscovery assembly attribute)
    TestSheets.cs             (new, see Shared code)
    TempDirectory.cs          (new, see Shared code)
    CueTimeTestData.cs        (new, data sources from CueTimeTests)
CueTime/
    CueTimeParsingTests.cs
    CueTimeFormattingTests.cs
    CueTimeArithmeticTests.cs
    CueTimeConversionTests.cs
Parsing/
    SheetParsingTests.cs
    EncodingDetectionTests.cs
Writing/
    WritingTests.cs
    RoundTripTests.cs
    PropertyPreservationTests.cs
Model/
    CloningTests.cs
    EqualityTests.cs
    CueIndexTests.cs
    TrackEditingTests.cs
    TrackNavigationTests.cs
    SheetStructureTests.cs
FileSystem/
    CopyingTests.cs
    FilePathTests.cs
    RelativePathTests.cs
Naming/
    CueTreeFormatterTests.cs
```

## Class by class: what moves where

### CueTime/

**CueTimeParsingTests** (new). Receives:
- from `CueTimeTests`: `TryParseTestString`, `ParseTestString`, `ParseTestSpan`, `TryParseTestSpan`, `TryParseTestStringFail`, `TryParseTestSpanFail`, `ParseTestStringFail`, `ParseTestSpanFail`, and the `ThrownType` helper (private; used only by `ParseTestStringFail` and `ParseTestSpanFail`)
- from `CueTimeFormatTests`: `ParseKeepsSignOfZeroMinutes`, `ParseRejectsMalformed`, `ParseAndTryParseAgree`

**CueTimeFormattingTests** (rename of `CueTimeFormatTests`, parse and arithmetic tests removed). Keeps: `DefaultToString`, `ExtremesFormatWithoutThrowing`, `CustomFormatPadsOnTheLeft`, `EscapedCharacterIsLiteral`, `TrailingBackslashDoesNotThrowIndexOutOfRange`, `TryFormatToSmallDestinationReturnsFalse`. The try/catch in `TrailingBackslashDoesNotThrowIndexOutOfRange` swallows `FormatException`; leave it, not part of this move.

**CueTimeArithmeticTests** (new). Receives:
- from `CueTimeTests`: `ComparisonTest`, `ComparisonTestThrow`, `CheckedOverflowDetectedTest2`
- from `CueTimeFormatTests`: `ModuloByZeroThrows`, `DivideByFractionOverflowThrows`

**CueTimeConversionTests** (new, small). Receives `RoundTripConversionTest_CtTsCt` and `TestConversion` from `CueTimeTests`.

**CueTimeTests**: delete once empty. Before deleting:
- `CueTimeTests.cs` line 4 holds `[assembly: TestDataSourceDiscovery(TestDataSourceDiscoveryOption.DuringExecution)]`. Move it to `Support/AssemblyInfo.cs`, otherwise `[DynamicData]` discovery changes for the whole assembly. Drop the unused `using Metalama.Framework;`.
- The static data (`TestTexts`, `FailTexts`, `TestObjectComparisons`, `TestObjectComparisonsThrow`, `OverflowingOperations`, `TestTickToFrameConversion`, and the `Cm`/`Ts` helpers they use) moves to `Support/CueTimeTestData.cs`. Consumers: Parsing uses `TestTexts` and `FailTexts`; Arithmetic uses `TestObjectComparisons`, `TestObjectComparisonsThrow`, `OverflowingOperations`; Conversion uses `TestTickToFrameConversion`.
- A `[DynamicData]` that points at another class needs the type argument: `[DynamicData(nameof(CueTimeTestData.TestTexts), typeof(CueTimeTestData))]`. This attribute edit is the only change to the moved tests.

### Parsing/

**SheetParsingTests** (rename of `ParsingTests`). Receives:
- all `LoadingTests` methods (`Title`, `Performer`, `TracKTitle`, `TracKPerformer`) plus the three `TestItem*` properties and their `Init`. Fix the `TracK` typos while there. Its `Load(name)` becomes `TestSheets.Load("LoadingTests", name)`.
- `CueEncodingTests.TestParsingMinimum` (reads `Parsing/MinimalFoobarCue.cue` and asserts parsed values; it is not about encodings).

`AssertFormatError` is used only here and stays private.

**EncodingDetectionTests** (rename of `CueEncodingTests`). Keeps `TestEncodingDetection`, `Init`, and the helpers. The class is `partial` (source-generated `UtfBitFinder` regex); keep the `partial` modifier.

Delete `LoadingTests`.

### Writing/

**WritingTests**: unchanged (the five writer bug-regression tests).

**RoundTripTests**: already has `WrittenLinesMatchSource` and `WriteIsStable` over its four-folder `Files` source. Merge `SavingTests` into it (`LoadCloneSame` excepted, see CloningTests). Receives `SaveReloadSame`, `SaveCloneSame`, and `AssertSame`. The class then has two data sources with different shapes: `SavingTests`-folder-only (one argument, file name) and all four folders (two arguments, folder and file name). Rename `SavingTests.Files` to `SavingFiles` and `RoundTripTests.Files` to `AllFiles`. Delete `SavingTests`.

**PropertyPreservationTests** (new). Receives from `StructureTests`: `TrackProps`, `SheetProps`, `TestReflectionStrings`, `Init` and the `SheetPropertyInfos`/`TrackPropertyInfos` fields, and the six tests `TestTrackPropertiesPreservedCloned`, `TestTrackPropertiesPreservedWritten`, `TestSheetPropertiesPreservedCloned`, `TestSheetPropertiesPreservedWritten`, `TestReflectionPropertiesPreservedWritten`, `TestReflectionPropertiesPreservedCloned`. They share all of that data and helper code and differ only in the clone-or-write step, so they stay together as one class rather than being split into clone/write.

### Model/

**CloningTests** (new). Receives:
- `SavingTests.LoadCloneSame` (uses `SavingFiles` and `AssertSame`; make those `internal` or move `AssertSame` to `Support`)
- `FilePathTests.CloneDoesNotShareCdTextFile`
- `TrackNavigationTests.CloneDoesNotMaterializeInheritedRemarksOnTracks`

**EqualityTests**: keeps its tests. Its `Load(name)` stays as a thin wrapper over `TestSheets.Load("Equality", name + ".cue")`.

**CueIndexTests**: keeps `MoveTime*`. Receives from `EditingTests`:
- `CannotAddDuplicateIndex`, `CanAddThirdIndex`, `CanAddBetweenIndex`
- `TracksWithThreeOrMoreIndicesAreSlicedCorrectly` (with the `ManyIndices` const)
- `InsertBeforeZerothIndexKeepsPositionsUnique` (with `AssertPositionsAscending`)
- `InsertBeforeFirstIndexRenumbersItToSecond` (with the `NoZerothIndices` const)
- `SplitIndexIsConsideredAsLimitOfPreviousTrack`, `SplitIndexIsNotConsideredAsLimitOfNext` (both load `LoadingTests/MultiFile.cue`)
- `InsertIndexWithInvalidTrackThrows`, `InsertIndexWithNegativeTimeThrows`
- `CueIndexMoveChecksLimits`

The local `Load()` helper is replaced by `TestSheets.Base()`.

**TrackEditingTests** (new). Receives from `EditingTests`: `InsertTrackAfterLastIndexOfLastFile`, `InsertTrackWithoutFreeNumberKeepsPositionsUnique`, `InsertTrackKeepsLaterNumberingGaps`, `Helion`, `Helion10` (both load `LoadingTests/Judas Priest - Screaming For Vengeance.cue`). Also receives `StructureTests.NonConsecutiveTrackNumbersArePreserved`.

**TrackNavigationTests**: keeps its remaining tests (`DurationIsDistanceToNextTrack`, `Previous*`, `NextTrackOfLastTrackIsNull`, and the `ThreeTracks` const). Receives `EditingTests.NextFileOfLastFileIsNull`.

**EditingTests checklist** (17 tests, each must land exactly once): `CueIndexTests` 11, `TrackEditingTests` 5, `TrackNavigationTests` 1.

**SheetStructureTests** (rename of `StructureTests`, after the moves above). Keeps `MultiFileEacLayout` (loads `SavingTests/MultiFile.cue`). `NewCue()` moves to `TestSheets` (also needed by `PropertyPreservationTests`). With one test left, merge `MultiFileEacLayout` into `TrackNavigationTests` instead and delete the class; keep the class only if a second structure test turns up.

Delete `EditingTests`.

### FileSystem/

**CopyingTests**: unchanged except `CreateTempDirectory` moves to `Support/TempDirectory.cs`. The commented-out `ConvertTest` block can be deleted or left; not part of this move.

**FilePathTests**: keeps everything except `CloneDoesNotShareCdTextFile` (moved to `CloningTests`). `AddFile` stays here; it is used by no other class.

**RelativePathTests**: unchanged, moved to the folder.

### Naming/

**CueTreeFormatterTests** (rename of `TreeParsingTests`). Contents unchanged.

## Shared code

**`Support/Samples.cs`**: typed catalog of every shared test sheet (`Samples.Base`, `Samples.MultiFile`, ...) and the only place that spells folder and file names. `Sample.Load()`, `.ReadText()`, `.Path`. Corpora for data-driven tests: `RoundTripCorpus` (`Sheets/Complete`, `Equality`, `SpandauBalletTrue`), `SaveCorpus` (Aqualung, True, MultiFile), `Encodings`. `SamplesTests` fails if a sample file is missing or an unreferenced `.cue` appears under `Sheets` or `Equality`.

**`Support/TestSheets.cs`**: `Parse(text)` for inline sheets (a sheet used by one test, where the text is the point, stays inline) and `NewCue()`.

**`Support/TempDirectory.cs`**: `Create(params string[] parts)`, from `CopyingTests.CreateTempDirectory` (11 call sites in `CopyingTests`). Only `CopyingTests` uses it today. `SavingTests` uses `Path.GetTempFileName()`; leave it.

**`Support/CueTimeTestData.cs`**: the `[DynamicData]` sources from `CueTimeTests` (see CueTime/).

`Utils.cs` and `Extensions.cs` move to `Support/` unchanged. `Extensions.Contains` overloads duplicate `string.Contains` on net8.0 and are candidates for deletion, but that is separate from this reorganization.

## Constraints

- `TestItems/` is organised by content, not by test class: `Sheets/Complete` (round-trippable), `Sheets/Partial` (fragments, parse-only), `Equality` (`Base.cue` plus one-difference variants), `EncodingDetection`, `CopyingTests` (a package with audio; its `Spandau Ballet - True.cue` is the single copy of that sheet). No byte-identical duplicates. `CueSheetNet.Tests.csproj` needs no change.
- Keep the namespace `CueSheetNet.Tests` for every file regardless of folder, so no `using` changes are needed.
- Not touched: `InsertBeforeFirstIndexRenumbersItToSecond` catches every exception and returns, so it can pass without asserting. Fixing that is a logic change and out of scope.

## Steps and verification

Do the move in steps with a gate after each:

0. Record the baseline test count: `dotnet test --list-tests` (count the listed tests).
1. Add `Support/` helpers and `AssemblyInfo.cs`.
2. CueTime split.
3. Merge Loading into Parsing and Saving into RoundTrip.
4. Split Editing/Structure.
5. Clone tests.
6. Renames and folder moves.

Gate after every step, via Bash:
- `dotnet build -v q -nologo -clp:"ErrorsOnly;NoSummary"` has no errors.
- `dotnet test -v q -nologo --logger "console;verbosity=minimal"` passes, and the listed test count equals the step 0 baseline.
- `dotnet format whitespace --verify-no-changes` and `dotnet format style --verify-no-changes` report nothing (the repo `.editorconfig` configures analyzer severities).
- LSP `documentSymbol` on each new or renamed file, and `findReferences` on moved helpers (`Utils.GetFile`, `AssertSame`, `SavingFiles`, `TestSheets.*`), show no dangling references or duplicate definitions.

CI (`.github/workflows/dotnet.yml`) runs restore, build, test; no change needed.
