using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Zorro.Classes;
using static Zorro.Toolbox;
using static Zorro.Program;

namespace Zorro.Scrapers
{
    public static class FitGirlScraper
    {
        public static void Fitgirl()
        {
            List<string> AllLinks = new List<string>();
            string OGLink = "http://fitgirl-repacks.site/all-my-repacks-a-z/?lcp_page0=%page%"; //arg
            string Link = "";
            int Tries = 0;
            bool Added = false;
            int LastPage = 0;
            var BaseUrl = new Uri(OGLink).Host;
            string[] PageVaildIfContains = new string[]
                {
                    "class=\"entry-title\"",
                    "Download",
                    "Repack Size",
                    "Original Size",
                    "Screenshots"
                }; //arg

            //Surface scrape
            for (int i = 1; i < MaxPages; i++)
            {
                Link = OGLink.Replace("%page%", $"{i}");
                var home = GetWebString(Link);
                var matches = Regex.Matches(home, "<a href=\"(.*?)\"", RegexOptions.Singleline).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                foreach (var Match in matches)
                {
                    if (!AllLinks.Contains(Match) && Match.Contains(BaseUrl))
                    {
                        AllLinks.Add(Match);
                        Console.WriteLine(Match);
                        Added = true;
                    }
                }

                if (!Added)
                    Tries++;
                Added = false;
                if (Tries >= 3)
                {
                    LastPage = i;
                    break;
                }
            }


            string TitlePath = "//*[@class='entry-title']"; //arg
            string ContentPath = "//*[@class='entry-content']"; //arg
            List<Entry> Entries = new List<Entry>();

            //Deep scrape
            foreach (var _Link in AllLinks)
            {
                bool valid = false;
                var page = GetWebString(_Link);
                valid = PageVaildIfContains.All(page.Contains);


                if (valid && new Uri(_Link).AbsolutePath != "/")
                {
                    var e = new Entry();
                    HtmlDocument Doc = new HtmlDocument();
                    Doc.LoadHtml(page);
                    e.Title = CleanText(System.Net.WebUtility.HtmlDecode(Doc.DocumentNode.SelectSingleNode(TitlePath).InnerText));
                    //e.Content = Doc.DocumentNode.SelectSingleNode(ContentPath).InnerHtml;
                    e.IndexDate = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}";
                    e.Repacker = "FitGirl";
                    e.Size = GetBetween(page, "Repack Size:", "</strong>").Replace("<strong>", "").ToUpper().Replace(",", ".");
                    e.Link = _Link;
                    Entries.Add(e);
                    Console.WriteLine(JsonConvert.SerializeObject(e));
                }
            }

            var es = JsonConvert.SerializeObject(Entries);
            File.WriteAllText($"Data/{BaseUrl}.json", es);
        }
    }
}
