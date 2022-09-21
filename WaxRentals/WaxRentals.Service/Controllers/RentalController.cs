using Microsoft.AspNetCore.Mvc;
using WaxRentals.Banano.Transact;
using WaxRentals.Data.Manager;
using WaxRentals.Service.Caching;
using WaxRentals.Service.Config;
using WaxRentals.Service.Shared.Entities;
using WaxRentals.Service.Shared.Entities.Input;
using WaxRentals.Waxp.Transact;
using static WaxRentals.Service.Shared.Config.Constants;
using Status = WaxRentals.Data.Entities.Status;

namespace WaxRentals.Service.Controllers
{
    public class RentalController : ServiceBase
    {

        private IInsert Insert { get; }
        private IProcess Process { get; }
        private IExplore Explore { get; }
        private IManage Manage { get; }

        private Cache Cache { get; }

        private IWaxAccounts Wax { get; }
        private IBananoAccountFactory Banano { get; }
        private Mapper Mapper { get; }

        public RentalController(
            ILog log,
            IInsert insert,
            IProcess process,
            IExplore explore,
            IManage manage,
            
            Cache cache,
            
            IWaxAccounts wax,
            IBananoAccountFactory banano,
            Mapper mapper)
            : base(log)
        {
            Insert = insert;
            Process = process;
            Explore = explore;
            Manage = manage;

            Cache = cache;

            Wax = wax;
            Banano = banano;
            Mapper = mapper;
        }

        #region " Create "

        [HttpPost("Create")]
        public async Task<JsonResult> Create([FromBody] NewRentalInput input)
        {
            try
            {
                // Validate.

                var limits = Cache.Limits.GetLimits();
                var waxBalances = Cache.WaxInfo.GetBalances();

                if (input.Days < 1)
                {
                    return Fail("Must rent for at least one day.");
                }
                if (input.Cpu + input.Net < limits.WaxMinimumRent)
                {
                    return Fail($"Must rent at least {limits.WaxMinimumRent:#,##0.#} WAX.");
                }
                if (input.Free && (input.Cpu + input.Net) > waxBalances.Available)
                {
                    return Fail($"Cannot provide a free rental of more than {waxBalances.Available:#,##0.#} WAX right now.");
                }
                if (!input.Free && (input.Cpu + input.Net > limits.WaxMaximumRent))
                {
                    return Fail($"Cannot rent more than {limits.WaxMaximumRent:#,##0.#} WAX in one transaction right now.");
                }

                // Open.

                int id;
                if (input.Free)
                {
                    id = await Insert.OpenRental(input.Account, input.Days, input.Cpu, input.Net, 0, Status.Pending);
                }
                else
                {
                    var cost = (input.Cpu + input.Net) * input.Days * Cache.Costs.GetCosts().WaxRentPriceInBanano;
                    cost = decimal.Round(cost, 4);

                    id = await Insert.OpenRental(input.Account, RentalDays(input.Days), input.Cpu, input.Net, cost);
                }

                var account = Banano.BuildAccount(id);
                return Succeed(new NewRental { Id = id, Address = account.Address });
            }
            catch (Exception ex)
            {
                try
                {
                    await Log.Error(ex);
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
            return (days >= Rentals.DaysDoubleThreshold) ? (days * 2) : days;
        }

        #endregion

        #region " Read "

        [HttpGet("ByWaxAccount/{account}")]
        public async Task<JsonResult> ByWaxAccount(string account)
        {
            var rentals = await Explore.GetRentalsByWaxAccount(account);
            return Succeed(rentals.Select(Mapper.Map));
        }

        [HttpPost("ByBananoAddresses")]
        public async Task<JsonResult> ByBananoAddresses([FromBody] IEnumerable<string> addresses)
        {
            var rentals = await Explore.GetRentalsByBananoAddresses(addresses);
            return Succeed(rentals.Select(Mapper.Map));
        }

        [HttpGet("New")]
        public async Task<JsonResult> New()
        {
            var rentals = await Process.PullNewRentals();
            return Succeed(rentals.Select(Mapper.Map));
        }

        [HttpGet("Paid")]
        public async Task<JsonResult> Paid()
        {
            var rentals = await Process.PullPaidRentalsToStake();
            return Succeed(rentals.Select(Mapper.Map));
        }

        [HttpGet("Sweepable")]
        public async Task<JsonResult> Sweepable()
        {
            var rentals = await Process.PullSweepableRentals();
            return Succeed(rentals.Select(Mapper.Map));
        }

        [HttpGet("NextClosing")]
        public async Task<JsonResult> NextClosing()
        {
            // Only unstake rentals that are staked by today's account.
            var rental = await Process.PullNextClosingRental(Wax.Today.Account);
            return Succeed(Mapper.Map(rental));
        }

        #endregion

        #region " Update "

        [HttpPost("ProcessPayment")]
        public async Task<JsonResult> ProcessPayment([FromBody] ProcessInput input)
        {
            await Process.ProcessRentalPayment(input.Id);
            return Succeed();
        }

        [HttpPost("ProcessStake")]
        public async Task<JsonResult> ProcessStake([FromBody] ProcessRentalInput input)
        {
            await Process.ProcessRentalStaking(input.Id, input.Source, input.Transaction);
            return Succeed();
        }

        [HttpPost("ProcessSweep")]
        public async Task<JsonResult> ProcessSweep([FromBody] ProcessInput input)
        {
            await Process.ProcessRentalSweep(input.Id, input.Transaction);
            return Succeed();
        }

        [HttpPost("ProcessClosing")]
        public async Task<JsonResult> ProcessClosing([FromBody] ProcessInput input)
        {
            await Process.ProcessRentalClosing(input.Id, input.Transaction);
            return Succeed();
        }

        [HttpPost("ExtendRental")]
        public async Task<JsonResult> ExtendRental([FromBody] ExtendRentalInput input)
        {
            var rental = await Manage.ExtendRental(input.Address, input.Days);
            return Succeed(rental);
        }

        [HttpPost("ExpireRental")]
        public async Task<JsonResult> ExpireRental([FromBody] ExpireRentalInput input)
        {
            var rental = await Manage.ExpireRental(input.Address);
            return Succeed(rental);
        }

        #endregion

    }
}
