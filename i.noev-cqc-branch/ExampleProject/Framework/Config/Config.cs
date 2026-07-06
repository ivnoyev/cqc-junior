using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ExampleProject.Framework.Config
{
    public static class Config
    {
        private static readonly IConfiguration Configuration;

        static Config()
        {
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            Configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("config.json", optional: false, reloadOnChange: false)
                .Build();
        }

        public static string Get(string key)
        {
            var value = Configuration[key];
            if (value == null)
            {
                throw new InvalidOperationException($"Sorry, unable to find key '{key}' in config.json");
            }
            return value;
        }

        public static int GetInt(string key)
        {
            return int.Parse(Get(key));
        }
    }
}
