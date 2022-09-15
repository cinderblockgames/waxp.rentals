using System.Threading.Tasks;

namespace WaxRentals.Waxp.Transact
{
    public interface IWaxAccount
    {

        public string Account { get; }

        Task<(bool Success, AccountBalances Balances)> GetBalances();
        Task<(bool Success, string)> Stake(string account, decimal cpu, decimal net);
        Task<(bool Success, string)> Unstake(string account, decimal cpu, decimal net);
        Task<(bool Success, string)> ClaimRefund();
        Task<(bool Success, string)> Send(string account, decimal wax, string memo = null);
        Task<(bool Success, string)> SendAsset(string account, string asset, string memo);
        Task<(bool Success, string)> CreateAccount(string account, string ownerPublicKey, string activePublicKey, int ram);

    }
}
