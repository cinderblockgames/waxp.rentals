﻿using System;
using System.Timers;
using WaxRentals.Data.Manager;

namespace WaxRentals.Monitoring
{
    public abstract class Monitor : IDisposable
    {

        #region " Event "

        public event EventHandler Updated;
        protected ILog Log { get; }

        protected void RaiseEvent()
        {
            try
            {
                Updated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void Initialize()
        {
            Elapsed();
            RaiseEvent();
        }

        #endregion

        #region " Timer "

        private readonly Timer _timer;

        protected Monitor(TimeSpan interval, ILog log)
        {
            Log = log;

            _timer = new Timer(interval.TotalMilliseconds);
            _timer.Elapsed += (_, _) => Elapsed();
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Elapsed -= (_, _) => Elapsed();
            using (_timer)
            {
                _timer.Stop();
            }
            GC.SuppressFinalize(this);
        }

        protected virtual void Elapsed()
        {
            try
            {
                if (Tick())
                {
                    RaiseEvent();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected abstract bool Tick();

        #endregion

    }

    public abstract class Monitor<T> : Monitor
    {

        #region " Event "

        protected Monitor(TimeSpan interval, ILog log) : base(interval, log) { }

        public new event EventHandler<T> Updated;

        protected void RaiseEvent(T result)
        {
            try
            {
                Updated?.Invoke(this, result);
                RaiseEvent();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        #endregion

        #region " Timer "

        protected override void Elapsed()
        {
            try
            {
                if (Tick(out T args))
                {
                    RaiseEvent(args);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected abstract bool Tick(out T result);

        protected override bool Tick()
        {
            // This should never be called.
            return false;
        }

        #endregion

    }
}
