using System.Collections.Generic;
using System.Threading.Tasks;
using WaxRentals.Data.Entities;

namespace WaxRentals.Data.Manager
{
    public interface IProcess
    {

        // Rentals

        Task<IEnumerable<Rental>> PullNewRentals();
        Task ProcessRentalPayment(int rentalId);
        
        Task<IEnumerable<Rental>> PullPaidRentalsToStake();
        Task ProcessRentalStaking(int rentalId, string source, string transaction);

        Task<IEnumerable<Rental>> PullSweepableRentals();
        Task ProcessRentalSweep(int rentalId, string transaction);

        Task<Rental> PullNextClosingRental(string source);
        Task ProcessRentalClosing(int rentalId, string transaction);

        // Purchases

        Task<Purchase> PullNextPurchase();
        Task ProcessPurchase(int purchaseId, string transaction);

        // Welcome Packages

        Task<IEnumerable<WelcomePackage>> PullNewWelcomePackages();
        Task ProcessWelcomePackagePayment(int packageId);

        Task<IEnumerable<WelcomePackage>> PullPaidWelcomePackagesToFund();
        Task ProcessWelcomePackageFunding(int packageId, string fundTransaction);

        Task<IEnumerable<WelcomePackage>> PullFundedWelcomePackagesMissingNft();
        Task ProcessWelcomePackageNft(int packageId, string nftTransaction);

        Task<IEnumerable<WelcomePackage>> PullFundedWelcomePackagesMissingRental();
        Task ProcessWelcomePackageRental(int packageId, int rentalId);

        Task<IEnumerable<WelcomePackage>> PullSweepableWelcomePackages();
        Task ProcessWelcomePackageSweep(int packageId, string transaction);

        // Welcome Packages With Keys

        Task<IEnumerable<WelcomePackageWithKeys>> PullNewWelcomePackagesWithKeys();
        Task ProcessWelcomePackageWithKeysPayment(int packageId);

        Task<IEnumerable<WelcomePackageWithKeys>> PullPaidWelcomePackagesWithKeysToFund();
        Task ProcessWelcomePackageWithKeysFunding(int packageId, string fundTransaction);

        Task<IEnumerable<WelcomePackageWithKeys>> PullFundedWelcomePackagesWithKeysMissingNft();
        Task ProcessWelcomePackageWithKeysNft(int packageId, string nftTransaction);

        Task<IEnumerable<WelcomePackageWithKeys>> PullFundedWelcomePackagesWithKeysMissingRental();
        Task ProcessWelcomePackageWithKeysRental(int packageId, int rentalId);

        Task<IEnumerable<WelcomePackageWithKeys>> PullSweepableWelcomePackagesWithKeys();
        Task ProcessWelcomePackageWithKeysSweep(int packageId, string transaction);

    }
}
