﻿using Microsoft.AspNetCore.Mvc;
using WaxRentals.Banano.Transact;
using WaxRentals.Data.Manager;
using WaxRentals.Service.Caching;
using WaxRentals.Service.Shared.Entities;
using WaxConstants = WaxRentals.Waxp.Config.Constants;

namespace WaxRentals.Service.Controllers
{
    public class AppController : ServiceBase
    {

        private Cache Cache { get; }
        private IBananoAccount Banano { get; }

        public AppController(
            IDataFactory factory,
            
            Cache cache,
            IBananoAccount banano)
            : base(factory)
        {
            Cache = cache;
            Banano = banano;
        }

        [HttpGet("State")]
        public JsonResult State()
        {
            var prices = Cache.Prices.GetPrices();
            var waxInfo = Cache.WaxInfo.GetBalances();
            return Succeed(
                new AppState
                {
                    BananoPrice   = Price(prices.Banano),
                    BananoAddress = Banano.Address,
                    BananoBalance = Balance(Cache.BananoInfo.GetBalance()),

                    WaxPrice                    = Price(prices.Wax),
                    WaxAccount                  = WaxConstants.Protocol.Account,
                    WaxStaked                   = Balance(waxInfo.Staked),
                    WaxWorkingAccount           = waxInfo.Today,
                    WaxBalanceAvailableToday    = Balance(waxInfo.Available),
                    WaxBalanceAvailableTomorrow = Balance(waxInfo.Unstaking)
                }
            );
        }

        [HttpGet("Insights")]
        public JsonResult Insights()
        {
            return Fail("This method is not implemented.");
        }

        #region " Rounding "

        private decimal Price(decimal value)
        {
            return Math.Round(value, 6);
        }

        private decimal Balance(decimal value)
        {
            return Math.Round(value, 4);
        }

        #endregion

    }
}