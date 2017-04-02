using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Allows use of using (var ctx = VersionManager.Instance.PushState(...)) { ... code ... }
    /// </summary>
    public class VersioningContext : IDisposable
    {
        public ItemVersion CurrentVersion { get; private set; }

        internal VersioningContext(ItemVersion contextVsn)
        {
            CurrentVersion = contextVsn;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    VersionManager.Instance.PopState();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
