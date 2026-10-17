# Permitted actions and consequences

## Removal

### Removing an index of a track

Normally removing the index lowers the value of following indices by 1. However, there are some special cases:
If the track has no index higher than 1, removing index 1 will either remove the track (if it was the only index) or promote index 0 to index 1 (if present).
Removing index 0 of a track has no consequences on other indices, unless the track is split across files EAC-style. If so, the track is no longer EAC-style and should be fully treated as belonging to the next file.


### Removing a track

Removing a track should remove all its indices.
As such, the preceding track is functionally extended by the removed track's duration, but data-wise is unaffected. The following track is completely unaffected.
By default, removing the track lowers the indices of the following tracks by 1. However it should be possible to leave the unaffected.


### Removing a file

Removing a file is only possible if the sheet contains more than one file.
If possible, it removes all tracks (and indices).
If the file had the index 0 of an EAC-style track, see the section on removing indices.
If the file had the index 1 of an EAC-style track, it also removes the last track of the preceding file.


## Changing

### Changing the value of an index of a track

Not possible, the index can only be removed or added.

### Changing the time of an index of a track

Freely doable, but it should throw an error if the new time would conflicts with any other index of any track. If the datafile is present, setting time outside the file should throw an error.

There should be a method that can shift the time of all indices of a track by a certain amount, positive or negative (convenience function). Same restrictions as above.

### Changing the track type

It is not checked so everything is permitted


## Addition

### Adding an index to a track

The adding method should just add an index with value one higher than the last index. Their time is subject to the same restrictions as changing the time of an index.
As such, it is impossible to have two indices with the same value.
Adding index 0 only via a dedicated method.
Adding index 1 is impossible, as no track can exist without it.

### Adding a track

Adding a track requires the index 1 time, optional index 0 time, optional more indices, and optional number.
If multiple indices are given, they cannot split an existing track (they must be contained between the last index of the previous track and the first index of the next track). If they do, an error should be thrown.
Index-time and file-time collision done as always.

Adding the track should increase the number of the following tracks by 1, unless there is a gap in the numbering. (tracks 1,2,4,5 - adding track before 2 - track 1,2,3,4,5, where 2 became 3)
If the track number is specified, it should be used. If it conflicts with an existing track, an error should be thrown. Renumbering of the following tracks should be done as above.

#### Splitting a track

If you wish to split a track into two, first remove the indices you wish to move to the new track, then add a new track with those indices.











