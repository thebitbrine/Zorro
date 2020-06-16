using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static Zorro.Classes;
using static Zorro.Program;

namespace Zorro.Scrapers
{
    public static class Starter
    {
        public static void UpdateLists()
        {
            while (true)
            {
                try
                {
                    var Files = Directory.GetFiles("Data");

                    foreach (var File in Files)
                    {
                        if (File.EndsWith(".json"))
                        {
                            try
                            {
                                var _List = System.IO.File.ReadAllText(File);
                                var newList = JsonConvert.DeserializeObject<List<Entry>>(_List);

                                List<Entry> NewEntries = new List<Entry>();
                                foreach (var en in newList)
                                {
                                    if (!Entries.Any(x => x.Link == en.Link))
                                        NewEntries.Add(en);
                                }
                                if (NewEntries.Any())
                                {
                                    Entries.AddRange(NewEntries);
                                    Zorro.MongoDB.PushBatch(NewEntries);
                                }
                            }
                            catch { }
                        }
                    }
#if DEBUG
                    Thread.Sleep(5000);
#else
                    Thread.Sleep(60000);
#endif
                }
                catch { }
            }
        }

        public static void Dispatcher()
        {
            while (true)
            {
                try
                {
                    new Thread(XatabScraper.Xatab) { IsBackground = true }.Start();
                    new Thread(FitGirlScraper.Fitgirl) { IsBackground = true }.Start();
                    new Thread(SkidrowScraper.Skidrow) { IsBackground = true }.Start();
                    new Thread(RGMechanicsScraper.RGMechanics) { IsBackground = true }.Start();
                    new Thread(ElAmigosScraper.ElAmigos) { IsBackground = true }.Start();
                    new Thread(KapitalSinScraper.KapitalSin) { IsBackground = true }.Start();
                    Thread.Sleep(86400000);
                }
                catch { }
            }
        }

    }
}
