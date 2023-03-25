using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using WaxRentals.Data.Context;
using WaxRentals.Data.Manager;
using static WaxRentals.Data.Config.Constants;

namespace WaxRentals.Data.Config
{
    public static class Dependencies
    {

        public static void AddDependencies(this IServiceCollection services)
        {
            var env = GetEnvironmentVariables();

            services.AddSingleton(provider =>
                JObject.Parse(
                    File.ReadAllText(env[EnvironmentVariables.DbFile])
                ).ToObject<WaxDb>()
            );

            services.AddDbContextFactory<WaxRentalsContext>((provider, options) =>
                options.UseSqlServer(provider.GetRequiredService<WaxDb>().ConnectionString)
            );
            services.AddSingleton<IExplore, DataManager>();
            services.AddSingleton<IInsert, DataManager>();
            services.AddSingleton<ILog, DataManager>();
            services.AddSingleton<IManage, DataManager>();
            services.AddSingleton<IProcess, DataManager>();
            //services.AddSingleton<ITrackWax, DataManager>();
            services.AddSingleton<ITrackWax, TrackWaxManager>();
        }

        private static IDictionary<string, string> GetEnvironmentVariables()
        {
            var env = Environment.GetEnvironmentVariables();
            var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in env.Keys)
            {
                dic.Add(key, (string)env[key]);
            }
            return dic;
        }

    }
}
