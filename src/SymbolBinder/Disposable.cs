namespace SymbolBinder
{
    using System;

    /// <summary>
    /// dispose pattern
    /// </summary>
    public abstract class Disposable : IDisposable
    {
        #region Fields
        private bool disposed = false;
        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                // Dispose managed resources.
                if (disposing)
                {
                    OnDisposing();
                }

                // Dispose unmanaged resources.

                disposed = true;
            }
        }

        protected abstract void OnDisposing();
    }
}
