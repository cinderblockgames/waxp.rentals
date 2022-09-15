using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Eos.Cryptography;
using Eos.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WaxRentals.Data.Manager;
using static WaxRentals.Monitoring.Config.Constants;
using static WaxRentals.Waxp.Config.Constants;

namespace WaxRentals.Waxp.Transact
{
    internal class WrappedAccount : IWaxAccount
    {

        public string Account { get; }

        private readonly PrivateKey _active;
        private readonly IClientFactory _client;
        private readonly ILog _log;
        private readonly List<Authorization> _authorization;
        private readonly HttpClient _http;

        public WrappedAccount(string account, PrivateKey active, IClientFactory client, ILog log)
        {
            Account = account;
            _active = active;
            _client = client;
            _log = log;
            _http = new HttpClient { Timeout = QuickTimeout };

            _authorization = new List<Authorization>
            {
                new Authorization
                {
                    Actor = new Name(Account),
                    Permission = new Name("active")
                }
            };
        }

        #region " IWaxAccount "

        #region " Balances "

        public async Task<(bool, AccountBalances)> GetBalances()
        {
            var balances = new AccountBalances();
            var apiTask = _client.ProcessApi(async client =>
            {
                var account = await client.GetAccount(Account);
                if (account != null)
                {
                    decimal.TryParse(account.CoreLiquidBalance?.Split(' ')?.FirstOrDefault(), out decimal liquid);
                    decimal.TryParse(account.RefundRequest?.CpuAmount?.Split(' ')?.FirstOrDefault(), out decimal cpuRefund);
                    decimal.TryParse(account.RefundRequest?.NetAmount?.Split(' ')?.FirstOrDefault(), out decimal netRefund);
                    balances.Available = liquid;
                    balances.Unstaking = cpuRefund + netRefund;
                }
            });
            var jsonTask = GetValueFromJson(
                string.Format(Locations.StakedEndpointFormat, Account),
                Protocol.StakedWaxFormats.Select(format => string.Format(format, Account)),
                Protocol.Decimals
            );

            await Task.WhenAll(apiTask, jsonTask);
            balances.Staked = await jsonTask;
            balances.Account = Account;
            return (await apiTask, balances);
        }

        private async Task<decimal> GetValueFromJson(string url, IEnumerable<string> selectors, int scale)
        {
            try
            {
                var json = JObject.Parse(await _http.GetStringAsync(url));
                double result = 0;
                foreach (var selector in selectors)
                {
                    var nodes = json.SelectTokens(selector);
                    foreach (var node in nodes)
                    {
                        result += node.Value<double>() / Math.Pow(10, scale);
                    }
                }
                return Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                await _log.Error(ex);
                return 0;
            }
        }

        #endregion

        public async Task<(bool, string)> Stake(string account, decimal cpu, decimal net)
        {
            return await Process(
                new StakeAction
                {
                    Authorization = _authorization,
                    Data = new StakeData
                    {
                        Cpu = new LongCurrency($"{cpu} WAX"),
                        Net = new LongCurrency($"{net} WAX"),
                        From = new Name(Account),
                        Receiver = new Name(account),
                        Transfer = false
                    }
                }
            );
        }

        public async Task<(bool, string)> Unstake(string account, decimal cpu, decimal net)
        {
            return await Process(
                new UnstakeAction
                {
                    Authorization = _authorization,
                    Data = new UnstakeData
                    {
                        Cpu = new LongCurrency($"{cpu} WAX"),
                        Net = new LongCurrency($"{net} WAX"),
                        From = new Name(Account),
                        Receiver = new Name(account),
                        Transfer = false
                    }
                }
            );
        }

        public async Task<(bool, string)> ClaimRefund()
        {
            return await Process(
                new RefundAction
                {
                    Authorization = _authorization,
                    Data = new RefundData
                    {
                        Owner = new Name(Account)
                    }
                }
            );
        }

        public async Task<(bool, string)> Send(string account, decimal wax, string memo = null)
        {
            return await Process(
                new TransferAction
                {
                    Authorization = _authorization,
                    Account = new Name("eosio.token"),
                    Data = new TransferData
                    {
                        From = new Name(Account),
                        To = new Name(account),
                        Quantity = new LongCurrency($"{wax} WAX"),
                        Memo = memo
                    }
                }
            );
        }

        public async Task<(bool, string)> SendAsset(string account, string asset, string memo)
        {
            return await Process(
                new TransferAssetsAction
                {
                    Authorization = _authorization,
                    Data = new TransferAssetsData
                    {
                        From = new Name(Account),
                        To = new Name(account),
                        AssetIds = new string[] { asset },
                        Memo = memo
                    }
                }
            );
        }

        public async Task<(bool, string)> CreateAccount(string account, string ownerPublicKey, string activePublicKey, int ram/*, decimal cpu, decimal net*/)
        {
            return await Process(
                new NewAccountAction
                {
                    Authorization = _authorization,
                    Data = new NewAccountData
                    {
                        Creator = new Name(Account),
                        Name = new Name(account),
                        Owner = new Authority
                        {
                            Keys = new List<AuthorityKey>
                            {
                                new AuthorityKey { Key = new PublicKey(ownerPublicKey), Weight = 1 }
                            },
                            Threshold = 1
                        },
                        Active = new Authority
                        {
                            Keys = new List<AuthorityKey>
                            {
                                new AuthorityKey { Key = new PublicKey(activePublicKey), Weight = 1 }
                            },
                            Threshold = 1
                        }
                    }
                },
                new BuyRamAction
                {
                    Authorization = _authorization,
                    Data = new BuyRamData
                    {
                        Bytes = ram,
                        Payer = new Name(Account),
                        Receiver = new Name(account)
                    }
                }/*,
                new StakeAction
                {
                    Authorization = _authorization,
                    Data = new StakeData
                    {
                        Cpu = new LongCurrency($"{cpu} WAX"),
                        Net = new LongCurrency($"{net} WAX"),
                        From = new Name(Account),
                        Receiver = new Name(account),
                        Transfer = true
                    }
                }*/
            );
        }

        #endregion

        #region " Not Used "

        public async Task<(bool, string)> BuyRam(string account, int bytes)
        {
            return await Process(
                new BuyRamAction
                {
                    Authorization = _authorization,
                    Data = new BuyRamData
                    {
                        Bytes = bytes,
                        Payer = new Name(Account),
                        Receiver = new Name(account)
                    }
                }
            );
        }

        #endregion

        #region " Process "

        private async Task<(bool, string)> Process(params IAction[] actions)
        {
            if (!actions.Any())
            {
                return (false, null);
            }

            Transaction transaction = null;
            var success = await _client.ProcessApi(async client =>
                transaction = await client.CreateTransaction(actions.ToList())
            );
            if (success)
            {
                return await Process(transaction);
            }
            else
            {
                return (false, null);
            }
        }

        private async Task<(bool, string)> Process(Transaction transaction)
        {
            string hash = default;
            var success = await _client.ProcessApi(
                async client =>
                {
                    var signed = await client.SignTransaction(transaction, _active);
                    hash = await client.PushTransaction(signed);
                }
            );
            return (success, hash);
        }

        #endregion

        #region " Derived Classes "

        private class LongCurrency : Currency
        {
            public LongCurrency(string value) : base(value) { }
            public override string ToString()
            {
                return $"{Amount:0.00000000} {Ticker}";
            }
        }

        private class RefundData
        {
            [JsonProperty("owner")]
            public Name Owner { get; set; }
        }

        private class RefundAction : Eos.Models.Action<RefundData>
        {
            public override Name Account => "eosio";
            public override Name Name => "refund";
        }

        private class TransferAssetsData
        {
            [JsonProperty("from")]
            public Name From { get; set; }

            [JsonProperty("to")]
            public Name To { get; set; }

            [JsonProperty("asset_ids")]
            public string[] AssetIds { get; set; }

            [JsonProperty("memo")]
            public string Memo { get; set; }
        }

        private class TransferAssetsAction : Eos.Models.Action<TransferAssetsData>
        {
            public override Name Account => "atomicassets";
            public override Name Name => "transfer";
        }

        #endregion

    }
}
