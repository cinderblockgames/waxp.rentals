﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WaxRentals.Data.Context;
using WaxRentals.Data.Entities;

namespace WaxRentals.Data.Manager
{
    internal class DataManager : IInsert, IProcess, ITrackWax, ILog
    {

        private WaxRentalsContext Context { get; }

        public DataManager(WaxRentalsContext context)
        {
            Context = context;
        }

        #region " IInsert "

        public async Task<int> OpenRental(string account, int days, decimal cpu, decimal net, decimal banano)
        {
            var rental = Context.Rentals.Add(
                new Rental
                {
                    TargetWaxAccount = account,
                    RentalDays = days,
                    CPU = cpu,
                    NET = net,
                    Banano = banano
                }
            );
            await Context.SaveChangesAsync();
            return rental.RentalId;
        }

        public async Task OpenPurchase(decimal wax, string transaction, string bananoAddress, decimal banano, Status status)
        {
            Context.Purchases.Add(
                new Purchase
                {
                    Wax = wax,
                    WaxTransaction = transaction,
                    PaymentBananoAddress = bananoAddress,
                    Banano = banano,
                    Status = status
                }
            );
            await Context.SaveChangesAsync();
        }

        #endregion

        #region " IProcess "

        public Task<IEnumerable<Rental>> PullNewRentals()
        {
            IEnumerable<Rental> rentals = Context.Rentals.Where(
                // If the rental hasn't been funded within 24 hours, assume it's abandoned.
                // If it needs to be reactivated, change the Inserted date in the database.
                rental => rental.Status == Status.New && rental.Inserted > DateTime.UtcNow.AddDays(-1)
            ).ToList();
            return Task.FromResult(rentals);
        }


        public async Task ProcessRentalPayment(int rentalId)
        {
            var rental = Context.Rentals.Single(rental => rental.RentalId == rentalId && rental.Status == Status.Pending);
            rental.Paid = DateTime.UtcNow;
            rental.Status = Status.Pending;
            await Context.SaveChangesAsync();
        }

        public async Task ProcessRentalStaking(int rentalId, string source, string transaction)
        {
            var rental = Context.Rentals.Single(rental => rental.RentalId == rentalId && rental.Status == Status.Pending);
            rental.SourceWaxAccount = source;
            rental.StakeWaxTransaction = transaction;
            rental.Status = Status.Processed;
            await Context.SaveChangesAsync();
        }


        public Task<Rental> PullNextClosingRental()
        {
            var rental = Context.Rentals.FirstOrDefault(rental => rental.Status == Status.Processed && rental.PaidThrough < DateTime.UtcNow);
            return Task.FromResult(rental);
        }

        public async Task ProcessRentalClosing(int rentalId, string transaction)
        {
            var rental = Context.Rentals.FirstOrDefault(rental => rental.Status == Status.Processed && rental.PaidThrough < DateTime.UtcNow);
            rental.UnstakeWaxTransaction = transaction;
            rental.Status = Status.Closed;
            await Context.SaveChangesAsync();
        }


        public async Task<Purchase> PullNextPurchase()
        {
            return await Context.Database
                                .SqlQuery<Purchase>("[dbo].[PullNextPurchase]")
                                .SingleOrDefaultAsync();
        }

        public async Task ProcessPurchase(int purchaseId, string transaction)
        {
            var purchase = Context.Purchases.Single(purchase => purchase.PurchaseId == purchaseId && purchase.Status == Status.Pending);
            purchase.BananoTransaction = transaction;
            purchase.Status = Status.Processed;
            await Context.SaveChangesAsync();
        }

        #endregion

        #region " ITrackWax "

        public DateTime? GetLastHistoryCheck()
        {
            return Context.WaxHistory
                          .OrderByDescending(history => history.LastRun)
                          .FirstOrDefault()
                         ?.LastRun;
        }

        public void SetLastHistoryCheck(DateTime last)
        {
            Context.WaxHistory.Add(new WaxHistory { LastRun = last });
            Context.SaveChanges();
        }

        #endregion

        #region " ILog "

        public async Task Error(Exception exception, object context = null)
        {
            try
            {
                var log = new Error
                {
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    TargetSite = exception.TargetSite?.Name,
                    InnerExceptions = exception.InnerException?.ToString()
                };
                if (context != null)
                {
                    log.Context = JObject.FromObject(context).ToString();
                }

                Context.Errors.Add(log);
                await Context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion

    }
}
