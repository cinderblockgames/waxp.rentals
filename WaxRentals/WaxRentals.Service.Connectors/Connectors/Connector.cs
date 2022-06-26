﻿using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WaxRentals.Data.Manager;
using WaxRentals.Service.Shared.Entities;

namespace WaxRentals.Service.Shared.Connectors
{
    internal abstract class Connector
    {

        protected HttpClient Client { get; }
        protected ILog Log { get; }
        protected static JsonSerializerOptions SerializerOptions { get; }

        static Connector()
        {
            SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public Connector(Uri baseUrl, ILog log)
        {
            Client = new HttpClient
            {
                BaseAddress = baseUrl
            };
            Log = log;
        }

        protected async Task<Result<TOut>> Post<TOut>(string path, object data)
        {
            var json = JsonSerializer.Serialize(data);
            return await Process<TOut>(async () =>
                await Client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"))
            );
        }

        protected async Task<Result<TOut>> Get<TOut>(string path)
        {
            return await Process<TOut>(async () => await Client.GetAsync(path));
        }

        protected async Task<Result<TOut>> Process<TOut>(Func<Task<HttpResponseMessage>> target)
        {
            try
            {
                var response = await target();
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Result<TOut>>(content, SerializerOptions) ?? Result<TOut>.Fail($"Unable to deserialize response from server:{Environment.NewLine}{content}");
                }
                return Result<TOut>.Fail($"Unsuccessful response from server: {(int)response.StatusCode} {response.StatusCode}");
            }
            catch (Exception ex)
            {
                try
                {
                    await Log.Error(ex);
                    return Result<TOut>.Fail(ex.Message);
                }
                catch
                {
                    return Result<TOut>.Fail("Unknown error.");
                }
            }
        }

    }
}