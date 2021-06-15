
using AquacraftBot.Services;
using AquacraftBot.Services.GiveawayServices;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Timers;

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

            CurrentDomain_ProcessStart();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            var config = JsonConvert.DeserializeObject<BotConfiguration>(json);
            var bot = new Bot(config, serviceProvider);
            services.AddSingleton(bot);
        }

        // Bot comes alive it does:
        /*
         * Initilizes the Global GTimer to update embeds
         * Start new timers by getting reference from database Giveaway Selection
         */
        static async void CurrentDomain_ProcessStart()
        {
            GlobalData.EmbedUpdateGTimer.Interval = 30000; // Updates every 30 seconds
            GlobalData.EmbedUpdateGTimer.Elapsed += GiveawayService.GEmbedUpdateEvent;
            GlobalData.EmbedUpdateGTimer.AutoReset = true;
            GlobalData.EmbedUpdateGTimer.Start();

            Query allGItems = GlobalData.database.Collection("Giveaways");
            QuerySnapshot GItemSnaps = await allGItems.GetSnapshotAsync();
            foreach (DocumentSnapshot GItemSnap in GItemSnaps)
            {
                var GItem = GItemSnap.ConvertTo<GiveawayModel>();
                if (!GItem.Ended)
                {
                    if ((GItem.EndAt.ToDateTime() - DateTime.UtcNow) < TimeSpan.Zero)
                    { continue; }
                    Timer gTimer = new Timer((GItem.EndAt.ToDateTime() - DateTime.UtcNow).TotalMilliseconds);
                    gTimer.Elapsed += (source, e) => GiveawayService.GiveawayEndedEvent(source, e, GItem);
                    gTimer.AutoReset = false;
                    gTimer.Start();
                    GlobalData.GiveawayTimers.Add(GItem.gID,gTimer);
                    Console.WriteLine($"Giveaway #{GItem.gID} Timer has been restarted! Will end at {GItem.EndAt.ToDateTime()}!");
                }
            }
        }

        // Bot dies it does:
        /*
         * Ends all announcer timers
         * Ends and Dispose Global GTimer to update embeds
         * Ends all Giveaway timers running currently as well
         */
        private async void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (var anouncement in GlobalData.announcements)
            {
                anouncement.Stop();
                anouncement.Dispose();
                Console.WriteLine("An announcement has been disposed of!");
            }

            if (GlobalData.EmbedUpdateGTimer.Enabled)
            {
                GlobalData.EmbedUpdateGTimer.Stop();
                GlobalData.EmbedUpdateGTimer.Dispose();
                Console.WriteLine("EmbedUpdatingGTimer has been disposed of!");
            }

            foreach (var gTimer in GlobalData.GiveawayTimers)
            {                                
                gTimer.Value.Stop();
                gTimer.Value.Dispose();
                Console.WriteLine($"GTimer #{gTimer.Key} has been disposed of!");
            }    
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        { 

        }
    }
}