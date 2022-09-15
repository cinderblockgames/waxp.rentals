using System.Diagnostics;
using System.IO;
using Nano.Net;

namespace VanityAddressGenerator
{
    class Program
    {
        static void Main()
        {
            var vanity = "keys";
            var filename = $"{vanity}.seed.txt";

            var sw = new Stopwatch();
            sw.Start();
            var (seed, address) = GenerateSeed(vanity);
            sw.Stop();

            File.WriteAllText(filename, $"Generated {address} from {seed} in {sw.Elapsed}.");
            Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
        }

        private static (string, string) GenerateSeed(string vanity)
        {
            string seed, address;
            do
            {
                seed = Utils.GenerateSeed();
                var account = new Account(seed, 0, "ban");
                address = account.Address;
            }
            while (!(address[5..(5 + vanity.Length)].ToLower() == vanity.ToLower()));
            return (seed, address);
        }
    }
}
