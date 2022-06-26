﻿using Microsoft.AspNetCore.Mvc;
using WaxRentals.Banano.Transact;
using WaxRentals.Data.Entities;
using WaxRentals.Data.Manager;
using WaxRentals.Service.Caching;
using WaxRentals.Service.Shared.Entities.Input;
using static WaxRentals.Service.Config.Constants;

namespace WaxRentals.Service.Controllers
{
    public class RentalController : ServiceBase
    {

        private CostsCache Costs { get; }
        private LimitsCache Limits { get; }

        private IBananoAccountFactory Banano { get; }

        public RentalController(
            IDataFactory factory,
            
            CostsCache costs,
            LimitsCache limits,
            
            IBananoAccountFactory banano)
            : base(factory)
        {
            Costs = costs;
            Limits = limits;

            Banano = banano;
        }

        [HttpPost("New")]
        public async Task<JsonResult> New([FromBody] RentalInput input)
        {
            try
            {
                // Validate.

                var limits = Limits.GetLimits();

                if (input.Cpu + input.Net < limits.WaxMinimumRent)
                {
                    return Fail($"Must rent at least {limits.WaxMinimumRent} WAX.");
                }
                else if (input.Cpu + input.Net > limits.WaxMaximumRent)
                {
                    return Fail($"Cannot rent more than {limits.WaxMaximumRent} WAX in one transaction right now.");
                }
                else if (input.Days < 1)
                {
                    return Fail("Must rent for at least one day.");
                }

                // Open.

                var costs = Costs.GetCosts();

                int id;
                if (input.Free)
                {
                    id = await Factory.Insert.OpenRental(input.Account, input.Days, input.Cpu, input.Net, 0, Status.Pending);
                }
                else
                {
                    var cost = (input.Cpu + input.Net) * input.Days * costs.WaxRentPriceInBanano;
                    cost = decimal.Round(cost, 4);

                    id = await Factory.Insert.OpenRental(input.Account, RentalDays(input.Days), input.Cpu, input.Net, cost);
                }

                var account = Banano.BuildAccount((uint)id);
                return Succeed(account.Address);
            }
            catch (Exception ex)
            {
                try
                {
                    await Factory.Log.Error(ex);
                    return Fail(ex.Message);
                }
                catch
                {
                    return Fail("Unknown error.");
                }
            }
        }

        private int RentalDays(int days)
        {
            return (days >= Calculations.DaysDoubleThreshold) ? (days * 2) : days;
        }

    }
}