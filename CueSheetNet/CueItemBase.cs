using System.Runtime.CompilerServices;

namespace CueSheetNet
{
    public abstract class CueItemBase
    {
        internal bool Orphaned { get; set; }
        protected CueSheet _ParentSheet;
        internal CueSheet ParentSheet
        {
            get
            {
                CheckOrphaned();
                return _ParentSheet;
            }
        }
        public CueItemBase(CueSheet parent)
        {
            Orphaned = false;
            _ParentSheet = parent;
        }
        protected void CheckOrphaned([CallerMemberName] string name = "N/A")
        {
            if (Orphaned)
                throw new ArgumentNullException(GetType().Name + " does not belong to any " + name);
        }
    }
}