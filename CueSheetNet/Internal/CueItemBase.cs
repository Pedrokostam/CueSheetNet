using System.Runtime.CompilerServices;

namespace CueSheetNet.Internal
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
        /// <summary>
        /// Throws exception if the source object of the property is null
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected void CheckOrphaned([CallerMemberName] string name = "N/A")
        {
            if (Orphaned)
                throw new ArgumentNullException(GetType().Name + " does not belong to any " + name);
        }
    }
}