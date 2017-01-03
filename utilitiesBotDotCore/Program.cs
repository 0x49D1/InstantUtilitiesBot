using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Microsoft.Extensions.Configuration;
using System.IO;
using utilitiesBotDotCore.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace utilitiesBotDotCore
{
    public class Program
    {

        private static IConfigurationRoot _config;
        private static IServiceProvider _provider;
        private static void ConfigBuilder()
        {
            _config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            IServiceCollection serv = new ServiceCollection();
            Resolver(serv);
            _provider = serv.BuildServiceProvider();
            var lgFactory = _provider.GetService<ILoggerFactory>();
            lgFactory.AddConsole();
            lgFactory.AddNLog();
        }
        private static void Resolver(IServiceCollection serv)
        {
            serv.AddOptions();
            serv.AddLogging();
            
            serv.Configure<UtilitiesBotSettings>(_config.GetSection("UtilitiesBotSettings"));
            serv.AddScoped<Services.IService, Services.Service>();
        }
        static void Main(string[] args)
        {
            ConfigBuilder();

            var log = _provider.GetService<ILogger<Program>>();
            for (int i = 0; i < 1000; i++)
                log.LogInformation("ERROR--------------------------------------Error");

            //_provider.GetService<Services.IService>().Run();
        }

    }
}
