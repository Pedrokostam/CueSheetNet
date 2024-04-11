using System.Runtime.CompilerServices;

namespace CueSheetNet.Internal
{
    public abstract class CueItemBase(CueSheet parent) : IParentSheet
    {
        internal bool Orphaned { get; set; } = false;
        protected CueSheet _ParentSheet = parent;
        public CueSheet ParentSheet
        {
            get
            {
                CheckOrphaned();
                return _ParentSheet;
            }
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