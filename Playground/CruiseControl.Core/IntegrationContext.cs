﻿namespace CruiseControl.Core
{
    using System;
    using System.Threading;

    /// <summary>
    /// The context for performing an integration - provides functionality for co-ordination
    /// between threads.
    /// </summary>
    public class IntegrationContext
    {
        #region Private fields
        private readonly ManualResetEvent eventHandle = new ManualResetEvent(true);
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationContext"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        public IntegrationContext(ServerItem item)
        {
            this.Item = item;
        }
        #endregion

        #region Public properties
        #region Item
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public ServerItem Item { get; private set; }
        #endregion

        #region WasCancelled
        /// <summary>
        /// Gets or sets a value indicating whether this context was cancelled.
        /// </summary>
        /// <value><c>true</c> if context was cancelled; otherwise, <c>false</c>.</value>
        public bool WasCancelled { get; private set; }
        #endregion

        #region IsLocked
        /// <summary>
        /// Gets a value indicating whether this instance is locked.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is locked; otherwise, <c>false</c>.
        /// </value>
        public bool IsLocked { get; private set; }
        #endregion

        #region IsCompleted
        /// <summary>
        /// Gets a value indicating whether this instance is completed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is completed; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompleted { get; private set; }
        #endregion
        #endregion

        #region Public events
        #region Completed
        /// <summary>
        /// Occurs when this context has been completed.
        /// </summary>
        public event EventHandler Completed;
        #endregion

        #region Released
        /// <summary>
        /// Occurs when this context has been released.
        /// </summary>
        public event EventHandler Released;
        #endregion
        #endregion

        #region Public methods
        #region Lock()
        /// <summary>
        /// Locks this context to prevent a thread from continuing.
        /// </summary>
        public void Lock()
        {
            this.IsLocked = true;
            this.eventHandle.Reset();
        }
        #endregion

        #region Release()
        /// <summary>
        /// Releases any locks.
        /// </summary>
        public void Release()
        {
            this.IsLocked = false;
            this.eventHandle.Set();
            if (this.Released != null)
            {
                this.Released(this, EventArgs.Empty);
            }
        }
        #endregion

        #region Wait()
        /// <summary>
        /// Waits for the context to be unlocked.
        /// </summary>
        /// <param name="timeout">A timeout period.</param>
        /// <returns><c>true</c> if the context was unlocked; <c>false</c> otherwise.</returns>
        public bool Wait(TimeSpan timeout)
        {
            var wasSignaled = this.eventHandle.WaitOne(timeout);
            return wasSignaled && !this.WasCancelled;
        }
        #endregion

        #region Cancel()
        /// <summary>
        /// Cancels this instance.
        /// </summary>
        public void Cancel()
        {
            this.WasCancelled = true;
            this.Release();
        }
        #endregion

        #region Complete()
        /// <summary>
        /// Marks this context as completed.
        /// </summary>
        public void Complete()
        {
            this.IsCompleted = true;
            if (this.Completed != null)
            {
                this.Completed(this, EventArgs.Empty);
            }
        }
        #endregion
        #endregion
    }
}
