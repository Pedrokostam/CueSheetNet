# PLAN

The current plan is to think about and try to get rid of the Inner/Public dichotomy.

Have just one type, each instance will only have a reference to the sheet and nothing else.

CueIndex - class - add bool field IsSplit
	You might need to add a private guid to make it trully unique
CueTrack - class - finds its file via sheet (what is it, like at most 20 reference comparisons and array accesses?)
CueFile - class for data and audio - basically as is
Link - readonly struct - get rid of eac - IsSplit takes its role

Flattening the class tree will allow to simplify the ResettableSlice - TItem and TItemInner become the same thing
It will also reduce the number of ResettableCollections

The onlyu drawback I can think of now is that I can't use any struct besides CueTime and Link.

During parsing we can already create the final objects and don't have to deal with the reference web.				