using System;
using System.Threading.Tasks;
using WaxRentals.Service.Shared.Connectors;
using WaxRentals.Service.Shared.Entities;

namespace WaxRentals.Processing.Processors
{
    internal class RentalClosingProcessor : Processor<Result<RentalInfo>>
    {

        private IRentalService Rentals { get; }
        private IWaxService Wax { get; }

        public RentalClosingProcessor(ITrackService track, IRentalService rentals, IWaxService wax)
            : base(track)
        {
            Rentals = rentals;
            Wax = wax;
        }

        protected override Func<Task<Result<RentalInfo>>> Get => Rentals.NextClosing;
        protected async override Task<bool> Process(Result<RentalInfo> result)
        {
            if (result.Success && result.Value != null)
            {
                await Process(result.Value);
                return true; // Check for another.
            }
            return false;
        }

        private async Task Process(RentalInfo rental)
        {
            try
            {
                var result = await Wax.Unstake(rental.Cpu, rental.Net, rental.WaxAccount, rental.SourceAccount);
                if (result.Success)
                {
                    await Rentals.ProcessClosing(rental.Id, result.Value);
                }
            }
            catch (Exception ex)
            {
                Log(ex, context: rental);
            }
        }

    }
}
