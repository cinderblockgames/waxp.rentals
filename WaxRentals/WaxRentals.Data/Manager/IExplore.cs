using System.Collections.Generic;
using System.Threading.Tasks;
using WaxRentals.Data.Entities;

namespace WaxRentals.Data.Manager
{
    public interface IExplore
    {

        Task<IEnumerable<Rental>> GetLatestRentals();
        Task<IEnumerable<Purchase>> GetLatestPurchases();
        Task<IEnumerable<WelcomePackage>> GetLatestWelcomePackages();
        Task<IEnumerable<MonthlyStats>> GetMonthlyStats();

        Task<IEnumerable<Rental>> GetRentalsByBananoAddresses(IEnumerable<string> addresses);
        Task<IEnumerable<Rental>> GetRentalsByWaxAccount(string account);
        Task<IEnumerable<WelcomePackage>> GetWelcomePackagesByBananoAddresses(IEnumerable<string> addresses);
        Task<IEnumerable<WelcomePackage>> GetWelcomePackagesByWaxMemo(string memo);
        Task<IEnumerable<WelcomePackageWithKeys>> GetWelcomePackagesWithKeysByBananoAddresses(IEnumerable<string> addresses);
        Task<IEnumerable<WelcomePackageWithKeys>> GetWelcomePackagesWithKeysByWaxAccount(string account);

    }
}
