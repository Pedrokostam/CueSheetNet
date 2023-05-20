using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Internal;
[Flags]
internal enum FieldSetFlags
{
    None = 0,
    Title = 1,
    Performer = 2,
    Composer = 4,
}
