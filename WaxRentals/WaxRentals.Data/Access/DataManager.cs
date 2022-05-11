﻿using System;
using System.Linq;
using System.Threading.Tasks;
using WaxRentals.Data.Context;
using WaxRentals.Data.Entities;

namespace WaxRentals.Data.Access
{
    internal class DataManager : IInsert, IProcess
    {

        private WaxRentalsContext Context { get; }

        public DataManager(WaxRentalsContext context)
        {
            Context = context;
        }

        #region " IInsert "

        public async Task<Address> OpenAccount(string waxAccount, decimal cpu, decimal net)
        {
            var account = new Account
            {
                WaxAccount = waxAccount,
                CPU = cpu,
                NET = net
            };
            account = Context.Accounts.Add(account);
            await Context.SaveChangesAsync();
            return Context.Addresses.Single(addr => addr.AddressId == account.AccountId);
        }

        public async Task ApplyCredit(int accountId, decimal banano, string bananoTransaction)
        {
            Context.Credits.Add(
                new Credit
                {
                    AccountId = accountId,
                    Banano = banano,
                    BananoTransaction = bananoTransaction
                }
            );
            await Context.SaveChangesAsync();
        }

        public async Task ApplyPayment(string waxAccount, decimal wax, string waxTransaction, string bananoAddress, decimal banano, Status status)
        {
            Context.Payments.Add(
                new Payment
                {
                    WaxAccount = waxAccount,
                    Wax = wax,
                    WaxTransaction = waxTransaction,
                    BananoAddress = bananoAddress,
                    Banano = banano,
                    Status = status
                }
            );
            await Context.SaveChangesAsync();
        }

        #endregion

        #region " IProcess "

        public async Task<Credit> PullNextCredit()
        {
            return await Context.Database
                                .SqlQuery<Credit>("[dbo].[PullNextCredit]")
                                .SingleOrDefaultAsync();
        }

        public async Task<Payment> PullNextPayment()
        {
            return await Context.Database
                                .SqlQuery<Payment>("[dbo].[PullNextPayment]")
                                .SingleOrDefaultAsync();
        }

        public Task<Account> PullNextClosingAccount()
        {
            var account = Context.Accounts.FirstOrDefault(account => account.PaidThrough < DateTime.UtcNow);
            return Task.FromResult(account);
        }

        public async Task ProcessCredit(int creditId, DateTime paidThrough)
        {
            var credit = Context.Credits.Single(credit => credit.CreditId == creditId && credit.Status == Status.Pending);
            credit.Account.PaidThrough = paidThrough;
            credit.Status = Status.Processed;
            await Context.SaveChangesAsync();
        }

        public async Task ProcessPayment(int paymentId, string bananoTransaction)
        {
            var payment = Context.Payments.Single(payment => payment.PaymentId == paymentId && payment.Status == Status.Pending);
            payment.BananoTransaction = bananoTransaction;
            await Context.SaveChangesAsync();
        }

        public async Task ProcessAccountClosing(int accountId)
        {
            var account = Context.Accounts.Single(account => account.AccountId == accountId);
            account.PaidThrough = null;
            await Context.SaveChangesAsync();
        }

        #endregion

    }
}