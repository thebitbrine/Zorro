using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using static Zorro.Classes;
using static Zorro.Toolbox;

namespace Zorro.Scrapers.API
{
    class ThePirateBay
    {
        public static List<Entry> Query(string Query)
        {
            var Entries = new List<Entry>();
            try
            {
                var Raw = GetWebString($"https://apibay.org/q.php?q={Query}");
                var TPBList = JsonConvert.DeserializeObject<List<TPB>>(Raw);
                foreach (var TPB in TPBList)
                {
                    if (TPB.status == "trusted" || TPB.status == "vip")
                    {
                        if (TPB.category >= 400 && TPB.category < 500)
                        {
                            var Entry = new Entry()
                            {
                                Title = TPB.name,
                                Size = BytesToString(TPB.size),
                                IndexDate = new DateTime(1970,1,1).AddMilliseconds(TPB.added * 1000).ToString("yyyy-MM-dd HH:mm"),
                                Collection = "ThePirateBay",
                                Repacker = $"{TPB.username}@TPB",
                                Link = $"https://thepiratebay.org/description.php?id={TPB.id}"
                            };
                            Entries.Add(Entry);
                        }
                    }
                }
            }
            catch { }
            return Entries;
        }

        public static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
    public class TPB
    {
        public string id { get; set; }
        public string name { get; set; }
        public long size { get; set; }
        public string username { get; set; }
        public long added { get; set; }
        public string status { get; set; }
        public int category { get; set; }
    }

}
