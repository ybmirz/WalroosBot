
using AquacraftBot.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace AquacraftBot
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services) 
        {
            var serviceProvider = services.BuildServiceProvider();
            var json = string.Empty;
            using (var fs = File.OpenRead("./Resources/config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = sr.ReadToEnd();

            // connecting to the firestore database
            string path = AppDomain.CurrentDomain.BaseDirectory + @"/Resources/walroos-bot-data-adminsdk.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
            GlobalData.database = FirestoreDb.Create("walroos-bot-data");
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            var config = JsonConvert.DeserializeObject<BotConfiguration>(json);
            var bot = new Bot(config, serviceProvider);
            services.AddSingleton(bot);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (var anouncement in GlobalData.announcements)
            {
                anouncement.Stop();
                anouncement.Dispose();
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        { 

        }
    }
}