using System;
using System.Threading;
using System.Threading.Tasks;
using SLSAK.Extensions;
using File = SLSAK.Docker.IO.File;

namespace WaxRentals.Data.Manager
{
    internal class TrackWaxManager : ITrackWax
    {
        
        private ILog Log { get; }
        private string FileLocation { get; } = "/run/files/waxhistorycheck";
        
        private static readonly ReaderWriterLockSlim _rwls = new();

        public TrackWaxManager(ILog log)
        {
            Log = log;
        }

        public async Task<DateTime?> GetLastHistoryCheck()
        {
            DateTime? result = DateTime.Now; // Default to not re-processing everything.
            
            try
            {
                _rwls.SafeRead(() =>
                    result = DateTime.Parse(
                        File.ReadAllText(FileLocation)
                    )
                );
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return result;
        }

        public async Task SetLastHistoryCheck(DateTime last)
        {
            try
            {
                _rwls.SafeWrite(() =>
                    File.WriteAllText(
                        FileLocation,
                        DateTime.Now.ToString("u")
                    )
                );
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

    }
}