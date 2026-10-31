# Schema

## Sheet

The sheet may contain the following fields:

- **FILE** - A path to the audio/data file, relative to the sheet's path.  Basically mandatory.
- **CDTEXTFILE** - A path to the file with CD-TEXT info.
- **PERFOMER** - The name of the main performer of the album.
- **SONGWRITER** - The name of the main songwriter of the album.
- **TITLE** - The album title.
- **CATALOG** - the barcode, may be UPC or EAN, but to keep it clean UPC codes should be prefixed by a '0' creating a valid EAN (13 digits)

## FILE

A single file of the sheet, usually containg the entire disc data. 
Usage: `FILE <Path to file> <File Type>`
File type can be one of the following:

- **WAVE** - Uncompressed audio data in a RIFF container (common on Windows). Also a catch-all for other audio formats not mentioned in specification, like FLAC.

- **AIFF** - Uncompressed audio data in an IFF container (common on Apple platforms).

- **MP3** - Compressed lossy audio data.

- **BINARY** - Little-endian binary.

- **MOTOROLA** - Big-endian binary.

Software audio players usually do not really pay attention to the type. As long as it is an audio type (first 3) they will recognize the actual type of the file. In some cases they may recognize other types - Foobar200 recognizes **FLAC** as an audio file.

Files should not have any fields other that **TRACK**. No **REMs**!

## Track

A single audio/data track. It belongs to a single file (except for EAC-style) and can have the following fields:

- **PERFOMER (I)** - The name of the track performer.
- **SONGWRITER (I)** - The name of the track songwriter.
- **ISRC** - International Standard Recording Code
- **TITLE** - The track title.
- **INDEX** - indices of the track in the disc/file. INDEX 1 is mandatory for each track
- **PREGAP** and **POSTGAP** - weird field that contain information that is not necessary for playback and often not supported well. Explained below.
- **FLAGS** - subcodes of up to 4 flags
  - *DCP* - Digital copy permitted
  - *4CH* - Four-channel audio
  - *PRE* - Pre-emphasis enabled
  - *SCMS* - Serial Copy Management System

Track numbers are global. Changing the file does not reset the track number.
Track numbers are always in an ascending order, but they do not have to be consecutive. For example, a sheet may contain tracks 1, 2, 3, 5, 6. Track 4 is missing. The justification for this is that the tracks 3 and 4 (or 4 and 5) are actually a single track, but were merged upon the owner's dicretion.

CDRWIN: First track can be an arbitrary number, but the following ones must be consecutive
 - But most players can handle gaps in tracks

Items marked with **I** can be inherited from the sheet. If a track has those fields, they take precedence over the sheet's fields.

### Pregap and Postgap

Those fields specify how much silence should be added before or after the track. However, most software kinda ignores those, especially players.

Converters and burners may actually process those fields which will add silence in the given place. For example this:

```Cue
...
FILE "01 - Laisse Tomber les Filles.wav" WAVE
  TRACK 01 AUDIO
    TITLE "Laisse Tomber les Filles"
    PERFORMER "April March"
    PREGAP 00:00:30
    INDEX 01 00:00:00

FILE "02 - Deux Garçons Pour Une Fille.wav" WAVE
  TRACK 02 AUDIO
    INDEX 01 00:00:00
...
```

When converting to a single file may result in this:

```Cue
...
FILE "April March - Chick Habit.flac" WAVE
  TRACK 01 AUDIO
    TITLE "Laisse Tomber les Filles"
    PERFORMER "April March"
    INDEX 00 00:00:00
    INDEX 01 00:00:30
  TRACK 02 AUDIO
    TITLE "Deux Garçons Pour Une Fille"
    PERFORMER "April March"
    INDEX 01 02:06:25
```

Thirty frames of silence were added, **PREGAP** was swapped for `INDEX 0`. Note that the original file did not contain that silence - the sound really did start at frame 0. 
Each following track now starts 30 frames later compared to the sheet where the pregap was ignored.

However, some pieces of software ignore those fields, have strict requiremenets (pregap only at the beginning of file), or may treat them as invalid (CueTools do not recognize postgap).

It's best to assume that pregap and postgap do not refer to actual physical structure of the file the sheet refers to. They are for processing the files and not for playback. They should be read, but ignored for the sake of duration calculation

CDRWIN: PREGAP must occur before any indices; POSTGAP must occur after all indices

## INDEX

The Index field describes the placement of of something in the disc/file. There can be a 100 indices, from 0 to 99.
Index 1 is mandatory for all tracks and denotes the actual start of the track. `Index 0` is treated as belonging to the track and can be used to hide something (`HTOA`) or cut off leading silence or noise, for example. This is sometime called *pre-gap* (not to be confused with **PREGAP**). 

Since `INDEX 0` is technically part of the track the CD player will increase the track number after `INDEX 0`, but will be ignored when seeking (that is, skipping to a such a track skips to `INDEX 1`).

An example of `INDEX 0`  being used is when you have a recording of a live performancem, the band says the title of the next song and begins to play it. `INDEX 0` may be before the announcement, `INDEX 1` after. This way, when playing the whole album in one sessions you hear all band announcement and comments, but when you skip to the track you begin it immediately.

`INDEX 2` and greater are used for the data track, they do not have any meaning for audio tracks.

The timecode used in indices does not use milliseconds - those are CDDA frames. There are *75 frames* in a second.

The first index of a track must be 00:00:00 (either 0 or 1)


### HTOA - Hidden Track One Audio

Non-silent audio data located before between `INDEX 0` and `INDEX 1` of the first track on an audio CD.

Not all CD players can access it.

## REM fields

REM denotes a remark, an optional field that may be applied to a sheet or a track. The common convention is to specify the type after the REM keyword, followed by the actual value, e.g. `REM DATE 1997`,

While the REM types are arbitrary there are some types that are commonly used and generally recognized by different applications:

- **GENRE**
- **DATE**
- **DISCID**- usually the CDDB disc id
- **UPC** - when people forget **CATALOG** is for that.
- **COMMENT** - for values that are truly arbitrary, often contains the ripping software's designation.
- **COMPOSER**
- **REPLAYGAIN_TRACK_GAIN** - Track gain in decibels
- **REPLAYGAIN_TRACK_PEAK**
- **REPLAYGAIN_ALBUM_GAIN** - Album gain in decibels
- **REPLAYGAIN_ALBUM_PEAK**

Remarks do not have to be unique, especially Comments.

Fields Genre, Date, DiscID, Composer are treated as if they were first-class fields - they can be accessed as properties, as opossed to going through the list of Remarks

## Common fields

The following fields can appear in **TRACKs** and the whole **Sheet**:

- **PERFOMER**

- **REM**

- **SONGWRITER**

- **TITLE**

- Some **REM** fields, like **DATE**

Tracks can inherit values from the sheet (except **TITLE**). So if all the tracks are perfomed by the same artist, the performer should be marked once for the whole sheet, and not for each track.

## Sheet-styles

### Single file (standard)

There is only one file, which contains all the tracks. This is the most common style.

The sheet itself does not provide the information about the length of the files and, by extension, the length of the last track.

### Multiple files

Similar to the above, but the number of the files might be between 1 and the number of tracks. Each file contains one or more tracks, but no track is split across files.

The same lack of information about the length of the files and the last track applies.

There are three ways to handle gaps between tracks:
 - Use `INDEX 0` just as you would in a single file style. Each file will contain the actual length of gap (usually silence) before the track, making the track begin with silence. Faithful to the CD layout, but uncommon due to wasted space and time in the file.
 - Use **PREGAP** and **POSTGAP** fields to specify the length of the gap. This cuts of the gaps from the data stream but allows to recreate them when needed (i.e. burning to a CD). Only makes sense if the gaps were truly silent.
 - Leave out the gaps altogether. Loses information so it is impossible to recreate the original CD layout.

### EAC-style 

A variation of the multiple files style. It aims to retains the actual gaps, but put them at the end of files instead of the beginning, making them less annoying.

A side efect is that it indirectly gives the information about the length of all tracks, except the last one (like the single file style). This is because the last track of a file is always terminated by `INDEX 0` in the next file. The time of that index is the length of the track.

The major downside of this style is that in the actual text file of the sheet, the tracks fields (like **TITLE**, **PERFOMER**, etc.) are written under the preceding file - only `INDEX 1` is written under the track's actual file.
Makes it harder to read and REALLY ANNOYING TO PARSE.

### Style conclusion

Out of all the styles, only two are not able to always replicate the original CD layout: multiple files without gaps and multiple files with **PREGAP**/**POSTGAP**.

The first one loses the gaps completely, the second one loses the actual audio data of the gaps.

Single, EAC, and mutiple with `INDEX 0` are able to faithfully reproduce the original CD layout.

