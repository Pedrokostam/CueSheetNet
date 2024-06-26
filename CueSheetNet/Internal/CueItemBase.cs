﻿using System.Runtime.CompilerServices;

namespace CueSheetNet.Internal
{
    public abstract class CueItemBase(CueSheet parent) : IParentSheet
    {
        internal bool Orphaned { get; set; } = false;

        protected CueSheet _parentSheet = parent;

        /// <summary>
        /// The sheet mentioning the file.
        /// </summary>
        public CueSheet ParentSheet
        {
            get
            {
                CheckOrphaned();
                return _parentSheet;
            }
        }

        /// <summary>
        /// Throws exception if the source object of the property is null.
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected void CheckOrphaned()
        {
            if (Orphaned)
                throw new ArgumentNullException(GetType().Name + " does not belong to any object");
        }
    }
}
