using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using WaxRentals.Data.Context;
using WaxRentals.Data.Entities;
using static WaxRentals.Data.Config.Constants;

namespace WaxRentals.Data.Manager
{
    internal class DataManager : IInsert, IProcess, ITrackWax, ILog, IExplore, IManage
    {

        private IDbContextFactory<WaxRentalsContext> Factory { get; }
        private static DateTime Abandoned { get { return DateTime.UtcNow.AddDays(-1); } } // Abandon New Rentals/Packages after one day.

        public DataManager(IDbContextFactory<WaxRentalsContext> factory)
        {
            Factory = factory;
        }

        #region " ProcessWithFactory "

        private async Task ProcessWithFactory(Func<WaxRentalsContext, Task> action)
        {
            using var context = await Factory.CreateDbContextAsync();
            await action(context);
        }

        private async Task<T> ProcessWithFactory<T>(Func<WaxRentalsContext, Task<T>> func)
        {
            using var context = await Factory.CreateDbContextAsync();
            return await func(context);
        }

        #endregion

        #region " IInsert "

        public async Task<int> OpenRental(string account, int days, decimal cpu, decimal net, decimal banano, Status status = Status.New)
        {
            return await ProcessWithFactory(async context =>
            {
                // Prevent spamming of the same unpaid account info.
                var existing = context.Rentals.SingleOrDefault(rental =>
                    rental.TargetWaxAccount == account &&
                    rental.RentalDays == days &&
                    rental.CPU == cpu &&
                    rental.NET == net &&
                    rental.Banano == banano &&
                    rental.StatusId == (int)status &&
                    rental.Inserted > Abandoned);
                if (existing != null)
                {
                    return existing.RentalId;
                }

                var rental = new Rental
                {
                    TargetWaxAccount = account,
                    RentalDays = days,
                    CPU = cpu,
                    NET = net,
                    Banano = banano,
                    Status = status
                };
                if (status > Status.New)
                {
                    rental.Paid = DateTime.UtcNow;
                }
                context.Rentals.Add(rental);

                await context.SaveChangesAsync();
                return rental.RentalId;
            });
        }

        public async Task<bool> OpenPurchase(decimal wax, string transaction, string bananoAddress, decimal banano, Status status)
        {
            return await ProcessWithFactory(async context =>
            {
                // Some endpoints don't filter very accurately, so make sure we're not trying to insert the same transaction more than once.
                // (Note that WaxTransaction has to be unique in the database, so this is just preventing an exception.)
                var exists = context.Purchases.Any(purchase => purchase.WaxTransaction == transaction);
                if (!exists)
                {
                    context.Purchases.Add(
                        new Purchase
                        {
                            Wax = wax,
                            WaxTransaction = transaction,
                            PaymentBananoAddress = bananoAddress,
                            Banano = banano,
                            Status = status
                        }
                    );
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            });
        }

        public async Task<int> OpenWelcomePackage(string account, string memo, decimal wax, decimal banano)
        {
            return await ProcessWithFactory(async context =>
            {
                // Prevent spamming of the same unpaid package info.
                var existing = context.WelcomePackages.SingleOrDefault(package =>
                    package.TargetWaxAccount == account &&
                    package.Memo == memo &&
                    package.Inserted > Abandoned);
                if (existing != null)
                {
                    existing.Wax = wax;
                    existing.Banano = banano;
                    await context.SaveChangesAsync();
                    return existing.PackageId;
                }

                var package = context.WelcomePackages.Add(
                    new WelcomePackage
                    {
                        TargetWaxAccount = account,
                        Memo = memo,
                        Wax = wax,
                        Banano = banano,
                        Status = Status.New
                    }
                );
                await context.SaveChangesAsync();
                return package.Entity.PackageId;
            });
        }

        #endregion

        #region " IProcess "

        #region " Rentals "

        public async Task<IEnumerable<Rental>> PullNewRentals()
        {
            return await ProcessWithFactory(context =>
            {
                // If the rental hasn't been funded within 24 hours, assume it's abandoned.
                // If it needs to be reactivated, change the Inserted date in the database.
                // But probably, the user will just make a new one.
                IEnumerable<Rental> rentals = context.Rentals.Where(
                    rental => rental.StatusId == (int)Status.New && rental.Inserted > Abandoned
                ).ToList();
                return Task.FromResult(rentals);
            });
        }

        public async Task ProcessRentalPayment(int rentalId)
        {
            await ProcessWithFactory(async context =>
            {
                var rental = context.Rentals.SingleOrDefault(rental =>
                    rental.RentalId == rentalId && rental.StatusId == (int)Status.New);
                if (rental != null)
                {
                    rental.Paid = DateTime.UtcNow;
                    rental.Status = Status.Pending;
                    await context.SaveChangesAsync();
                }
            });
        }

        public async Task<IEnumerable<Rental>> PullPaidRentalsToStake()
        {
            return await ProcessWithFactory(context =>
            {
                IEnumerable<Rental> rentals = context.Rentals.Where(
                    rental => rental.StatusId == (int)Status.Pending && rental.StakeWaxTransaction == null
                ).ToList();
                return Task.FromResult(rentals);
            });
        }

        public async Task ProcessRentalStaking(int rentalId, string source, string transaction)
        {
            await ProcessWithFactory(async context =>
            {
                var rental = context.Rentals.SingleOrDefault(rental =>
                    rental.RentalId == rentalId && rental.StatusId == (int)Status.Pending);
                if (rental != null)
                {
                    rental.Staked = DateTime.UtcNow;
                    rental.SourceWaxAccount = source;
                    rental.StakeWaxTransaction = transaction;
                    rental.Status = Status.Processed;
                    await context.SaveChangesAsync();
                }
            });
        }

        public async Task<IEnumerable<Rental>> PullSweepableRentals()
        {
            return await ProcessWithFactory(context =>
            {
                IEnumerable<Rental> rentals = context.Rentals.Where(
                    rental => rental.StatusId == (int)Status.Processed && rental.SweepBananoTransaction == null && rental.Banano > 0 // Filter out free rentals.
                ).ToList();
                return Task.FromResult(rentals);
            });
        }

        public async Task ProcessRentalSweep(int rentalId, string transaction)
        {
            await ProcessWithFactory(async context =>
            {
                var rental = context.Rentals.Single(rental =>
                    rental.RentalId == rentalId && rental.StatusId == (int)Status.Processed);
                rental.SweepBananoTransaction = transaction;
                await context.SaveChangesAsync();
            });
        }

        public async Task<Rental> PullNextClosingRental(string source)
        {
            return await ProcessWithFactory(context =>
            {
                var rental = context.Rentals.FirstOrDefault(rental =>
                    rental.StatusId == (int)Status.Processed && rental.PaidThrough < DateTime.UtcNow && rental.SourceWaxAccount == source);
                return Task.FromResult(rental);
            });
        }

        public async Task ProcessRentalClosing(int rentalId, string transaction)
        {
            await ProcessWithFactory(async context =>
            {
                var rental = context.Rentals.FirstOrDefault(rental =>
                    rental.StatusId == (int)Status.Processed && rental.PaidThrough < DateTime.UtcNow);
                rental.UnstakeWaxTransaction = transaction;
                rental.Status = Status.Closed;
                await context.SaveChangesAsync();
            });
        }

        #endregion

        #region " Purchases "

        public async Task<Purchase> PullNextPurchase()
        {
            return await ProcessWithFactory(context =>
            {
                return Task.FromResult(
                    context.Purchases
                           .FromSqlRaw("[dbo].[PullNextPurchase]")
                           .AsEnumerable()
                           .SingleOrDefault()
                    );
            });
        }

        public async Task ProcessPurchase(int purchaseId, string transaction)
        {
            await ProcessWithFactory(async context =>
            {
                var purchase = context.Purchases.Single(purchase =>
                    purchase.PurchaseId == purchaseId && purchase.StatusId == (int)Status.Pending);
                purchase.BananoTransaction = transaction;
                purchase.Status = Status.Processed;
                await context.SaveChangesAsync();
            });
        }

        #endregion

        #region " Welcome Packages "

        public async Task<IEnumerable<WelcomePackage>> PullNewWelcomePackages()
        {
            return await ProcessWithFactory(async context =>
            {
                // If the package hasn't been funded within 24 hours, assume it's abandoned.
                // If it needs to be reactivated, change the Inserted date in the database.
                // But probably, the user will just make a new one.
                return await (from package in context.WelcomePackages
                              where package.StatusId == (int)Status.New && package.Inserted > Abandoned
                              select package).ToArrayAsync();
            });
        }

        public async Task ProcessWelcomePackagePayment(int packageId)
        {
            await ProcessWithFactory(async context =>
            {
                var package = context.WelcomePackages.SingleOrDefault(
                    package => package.PackageId == packageId && package.StatusId == (int)Status.New);
                if (package != null)
                {
                    package.Paid = DateTime.UtcNow;
                    package.Status = Status.Pending;
                    await context.SaveChangesAsync();
                }
            });
        }

        public async Task<IEnumerable<WelcomePackage>> PullPaidWelcomePackagesToFund()
        {
            return await ProcessWithFactory(async context =>
            {
                return await (from package in context.WelcomePackages
                              where package.StatusId == (int)Status.Pending && package.FundTransaction == null
                              select package).ToArrayAsync();
            });
        }

        public async Task ProcessWelcomePackageFunding(int packageId, string fundTransaction)
        {
            await ProcessWithFactory(async context =>
            {
                var package = context.WelcomePackages.SingleOrDefault(
                    package => package.PackageId == packageId && package.StatusId == (int)Status.Pending);
                if (package != null)
                {
                    package.FundTransaction = fundTransaction;
                    package.Status = Status.Processed;
                    await context.SaveChangesAsync();
                }
            });
        }

        public async Task<IEnumerable<WelcomePackage>> PullFundedWelcomePackagesMissingNft()
        {
            return await ProcessWithFactory(async context =>
            {
                return await (from package in context.WelcomePackages
                              where package.StatusId == (int)Status.Processed && package.NftTransaction == null
                              select package).ToArrayAsync();
            });
        }

        public async Task ProcessWelcomePackageNft(int packageId, string nftTransaction)
        {
            await ProcessWithFactory(async context =>
            {
                var package = context.WelcomePackages.SingleOrDefault(
                    package => package.PackageId == packageId && package.StatusId == (int)Status.Processed);
                if (package != null)
                {
                    package.NftTransaction = nftTransaction;
                    await context.SaveChangesAsync();
                }
            });
        }

        public async Task<IEnumerable<WelcomePackage>> PullFundedWelcomePackagesMissingRental()
        {
            return await ProcessWithFactory(async context =>
            {
                return await (from package in context.WelcomePackages
                              where package.StatusId == (int)Status.Processed && package.RentalId == null
                              select package).ToArrayAsync();
            });
        }

        public async Task ProcessWelcomePackageRental(int packageId, int rentalId)
        {
            await ProcessWithFactory(async context =>
            {
                var package = context.WelcomePackages.SingleOrDefault(
                    package => package.PackageId == packageId && package.StatusId == (int)Status.Processed);
                if (package != null)
                {
                    package.RentalId = rentalId;
                    await context.SaveChangesAsync();
                }
            });
        }

        public async Task<IEnumerable<WelcomePackage>> PullSweepableWelcomePackages()
        {
            return await ProcessWithFactory(async context =>
            {
                return await (from package in context.WelcomePackages
                              where package.StatusId == (int)Status.Processed && package.SweepBananoTransaction == null
                              select package).ToArrayAsync();
            });
        }

        public async Task ProcessWelcomePackageSweep(int packageId, string transaction)
        {
            await ProcessWithFactory(async context =>
            {
                var package = context.WelcomePackages.SingleOrDefault(
                    package => package.PackageId == packageId && package.StatusId == (int)Status.Processed);
                package.SweepBananoTransaction = transaction;
                await context.SaveChangesAsync();
            });
        }

        #endregion

        #endregion

        #region " ITrackWax "

        public async Task<DateTime?> GetLastHistoryCheck()
        {
            return await ProcessWithFactory(async context =>
            {
                var history = await context.WaxHistory
                                           .OrderByDescending(history => history.LastRun)
                                           .FirstOrDefaultAsync();
                return history?.LastRun;
            });
        }

        public async Task SetLastHistoryCheck(DateTime last)
        {
            await ProcessWithFactory(async context =>
            {
                context.WaxHistory.Add(new WaxHistory { LastRun = last });
                await context.SaveChangesAsync();
            });
        }

        #endregion

        #region " ILog "

        public async Task Error(Exception exception, string error = null, object context = null)
        {
            return;
            await ProcessWithFactory(async ctx =>
            {
                try
                {
                    var log = new Error
                    {
                        Message = exception.Message,
                        ErrorObject = error,
                        StackTrace = exception.StackTrace,
                        TargetSite = exception.TargetSite?.Name,
                        InnerExceptions = exception.InnerException?.ToString()
                    };
                    if (context != null)
                    {
                        log.Context = JToken.FromObject(context).ToString();
                    }

                    ctx.Errors.Add(log);
                    await ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        public async Task Message(Guid requestId, string url, MessageDirection direction, string message)
        {
            return;
            await ProcessWithFactory(async context =>
            {
                try
                {
                    var log = new Message
                    {
                        RequestId = requestId,
                        Url = url,
                        Direction = Enum.GetName(direction),
                        MessageObject = message
                    };

                    context.Messages.Add(log);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        public async Task ClearOlderRecords()
        {
            return;
            await ProcessWithFactory(async context =>
                await context.Database.ExecuteSqlRawAsync("[logs].[ClearOlderRecords]")
            );
        }

        #endregion

        #region " IExplore "

        public async Task<IEnumerable<Rental>> GetLatestRentals()
        {
            return await ProcessWithFactory(async context =>
            {
                return await context.Rentals
                                    .Where(rental => rental.StatusId == (int)Status.Processed || rental.StatusId == (int)Status.Closed)
                                    .OrderByDescending(rental => rental.RentalId)
                                    .Take(Display.Recents)
                                    .ToArrayAsync();
            });
        }

        public async Task<IEnumerable<Purchase>> GetLatestPurchases()
        {
            return await ProcessWithFactory(async context =>
            {
                return await context.Purchases
                                    .Where(purchase => purchase.StatusId == (int)Status.Processed && purchase.BananoTransaction != null)
                                    .OrderByDescending(purchase => purchase.PurchaseId)
                                    .Take(Display.Recents)
                                    .ToArrayAsync();
            });
        }

        public async Task<IEnumerable<WelcomePackage>> GetLatestWelcomePackages()
        {
            return await ProcessWithFactory(async context =>
            {
                return await context.WelcomePackages
                                    .Where(package => package.StatusId == (int)Status.Processed)
                                    .OrderByDescending(package => package.PackageId)
                                    .Take(Display.Recents)
                                    .Include(package => package.Rental)
                                    .ToArrayAsync();
            });
        }

        public async Task<IEnumerable<MonthlyStats>> GetMonthlyStats()
        {
            return await ProcessWithFactory(async context =>
            {
                // Can't seem to execute a stored procedure without having a DbSet on the context, so just go direct.
                using var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "[reporting].[MonthlyStats]";
                using var reader = await command.ExecuteReaderAsync();

                var stats = new List<MonthlyStats>();
                while (reader.Read())
                {
                    stats.Add(
                        new MonthlyStats
                        {
                            Year                  = reader.GetInt32  (reader.GetOrdinal(nameof(MonthlyStats.Year))),
                            Month                 = reader.GetInt32  (reader.GetOrdinal(nameof(MonthlyStats.Month))),
                            WaxDaysRented         = reader.GetDecimal(reader.GetOrdinal(nameof(MonthlyStats.WaxDaysRented))),
                            WaxDaysFree           = reader.GetDecimal(reader.GetOrdinal(nameof(MonthlyStats.WaxDaysFree))),
                            WaxPurchasedForSite   = reader.GetDecimal(reader.GetOrdinal(nameof(MonthlyStats.WaxPurchasedForSite))),
                            WelcomePackagesOpened = reader.GetInt32  (reader.GetOrdinal(nameof(MonthlyStats.WelcomePackagesOpened)))
                        }
                    );
                }
                return stats;
            });
        }

        public async Task<IEnumerable<Rental>> GetRentalsByBananoAddresses(IEnumerable<string> addresses)
        {
            return await ProcessWithFactory(async context =>
            {
                return await (from rental in context.Rentals
                              where addresses.Contains(rental.BananoAddress) && (rental.StatusId != (int)Status.New || rental.Inserted > Abandoned)
                              orderby rental.PaidThrough, rental.Paid, rental.RentalId
                              select rental).ToArrayAsync();
            });
        }

        public async Task<IEnumerable<Rental>> GetRentalsByWaxAccount(string account)
        {
            return await ProcessWithFactory(async context =>
            {
                return await context.Rentals
                                    .Where(rental => rental.TargetWaxAccount == account &&
                                                     (rental.StatusId != (int)Status.New || rental.Inserted > Abandoned))
                                    .OrderBy(rental => rental.PaidThrough)
                                    .ThenBy(rental => rental.Paid)
                                    .ThenBy(rental => rental.RentalId)
                                    .ToArrayAsync();
            });
        }

        public async Task<IEnumerable<WelcomePackage>> GetWelcomePackagesByBananoAddresses(IEnumerable<string> addresses)
        {
            return await ProcessWithFactory(async context =>
            {
                return await (from package in context.WelcomePackages
                              where addresses.Contains(package.BananoAddress) && (package.StatusId != (int)Status.New || package.Inserted > Abandoned)
                              orderby package.PackageId
                              select package).ToArrayAsync();
            });
        }

        public async Task<IEnumerable<WelcomePackage>> GetWelcomePackagesByWaxMemo(string memo)
        {
            return await ProcessWithFactory(async context =>
            {
                return await context.WelcomePackages
                                    .Where(package => package.Memo == memo &&
                                                      (package.StatusId != (int)Status.New || package.Inserted > Abandoned))
                                    .OrderBy(package => package.Paid)
                                    .ThenBy(package => package.PackageId)
                                    .ToArrayAsync();
            });
        }

        #endregion

        #region " IManage "

        public async Task<Rental> ExtendRental(string address, int days)
        {
            return await ProcessWithFactory(async context =>
            {
                var rental = context.Rentals.SingleOrDefault(rental =>
                    rental.BananoAddress == address && rental.StatusId == (int)Status.Processed);
                if (rental != null)
                {
                    var through = rental.PaidThrough.Value.AddDays(days);
                    if (through < DateTime.UtcNow)
                    {
                        return await ExpireRental(address);
                    }
                    else
                    {
                        rental.RentalDays += days;
                        await context.SaveChangesAsync();
                        return rental;
                    }
                }
                return null;
            });
        }

        public async Task<Rental> ExpireRental(string address)
        {
            return await ProcessWithFactory(async context =>
            {
                var rental = context.Rentals.SingleOrDefault(rental =>
                    rental.BananoAddress == address && rental.StatusId == (int)Status.Processed);
                if (rental != null)
                {
                    // Set to expire now, but it won't be picked up to unstake until the source day comes again.
                    rental.RentalDays = (int)(DateTime.UtcNow - rental.Paid.Value).TotalDays;
                    await context.SaveChangesAsync();
                    return rental;
                }
                return null;
            });
        }

        #endregion

    }
}
