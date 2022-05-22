﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WaxRentals.Data.Entities;
using WaxRentals.Data.Manager;
using WaxRentals.Monitoring.Extensions;

namespace WaxRentals.Monitoring.Recents
{
    internal class RecentMonitor : Monitor, IRecentMonitor
    {

        private readonly ReaderWriterLockSlim _rentalsLock = new();
        private IEnumerable<Rental> _rentals;
        public IEnumerable<Rental> Rentals { get { return _rentalsLock.SafeRead(() => _rentals); } }

        private readonly ReaderWriterLockSlim _purchasesLock = new();
        private IEnumerable<Purchase> _purchases;
        public IEnumerable<Purchase> Purchases { get { return _purchasesLock.SafeRead(() => _purchases); } }

        public RecentMonitor(TimeSpan interval, IDataFactory factory)
            : base(interval, factory)
        {
            // Nothing additional.
        }

        protected override bool Tick()
        {
            var update = false;

            try
            {
                var rentals = Factory.Explore.GetRecentRentals();
                var purchases = Factory.Explore.GetRecentPurchases();

                if (_rentals == null || _purchases == null)
                {
                    update = true;
                    _rentalsLock.SafeWrite(() => _rentals = rentals);
                    _purchasesLock.SafeWrite(() => _purchases = purchases);
                }
                else
                {
                    if (Differ(_rentals, rentals, rental => rental.RentalId))
                    {
                        update = true;
                        _rentalsLock.SafeWrite(() => _rentals = rentals);
                    }

                    if (Differ(_purchases, purchases, purchase => purchase.PurchaseId))
                    {
                        update = true;
                        _purchasesLock.SafeWrite(() => _purchases = purchases);
                    }
                }
            }
            catch (Exception ex)
            {
                Factory.Log.Error(ex);
            }

            return update;
        }

        private bool Differ<T>(IEnumerable<T> left, IEnumerable<T> right, Func<T, int> get)
        {
            var leftIds = left.Select(get);
            var rightIds = right.Select(get);
            return leftIds.Except(rightIds).Any() || rightIds.Except(leftIds).Any();
        }

    }
}