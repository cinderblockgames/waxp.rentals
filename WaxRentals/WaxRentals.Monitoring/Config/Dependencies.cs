﻿using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using WaxRentals.Data.Manager;
using WaxRentals.Monitoring.Notifications;
using WaxRentals.Monitoring.Prices;
using WaxRentals.Monitoring.Recents;
using static WaxRentals.Monitoring.Config.Constants;
using File = System.IO.File;

namespace WaxRentals.Monitoring.Config
{
    public static class Dependencies
    {

        public static void AddDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IPriceMonitor>(provider =>
                new PriceMonitor(
                    TimeSpan.FromMinutes(2),
                    provider.GetRequiredService<IDataFactory>(),
                    $"https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&include_24hr_change=true&ids={Coins.Banano},{Coins.Wax}"
                )
            );

            services.AddSingleton<IInsightsMonitor>(provider =>
                new InisghtsMonitor(
                    TimeSpan.FromMinutes(1),
                    provider.GetRequiredService<IDataFactory>()
                )
            );

            var telegram = JObject.Parse(File.ReadAllText(Secrets.TelegramInfo)).ToObject<TelegramInfo>();

            services.AddSingleton<ITelegramNotifier>(provider =>
                new TelegramNotifier(
                    new TelegramBotClient(telegram.Token, new HttpClient { Timeout = QuickTimeout }),
                    new ChatId(telegram.TargetChat),
                    provider.GetRequiredService<IDataFactory>()
                )
            );
        }

    }
}
