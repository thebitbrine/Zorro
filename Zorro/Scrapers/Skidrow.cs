﻿using HtmlAgilityPack;
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
using System.Globalization;

namespace Zorro.Scrapers
{
    public static class SkidrowScraper
    {
        public static void Skidrow()
        {
            List<string> AllLinks = new List<string>();
            string OGLink = "https://www.skidrowreloaded.com/pc/?lcp_page1=%page%"; //arg
            string Link = "";
            int Tries = 0;
            bool Added = false;
            int LastPage = 0;
            var BaseUrl = new Uri(OGLink).Host;
            string[] PageVaildIfContains = new string[]
                {
                    "DOWNLOAD",
                    "PC GAMES"
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


            string TitlePath = "//div[contains(@class, \"post\")]/h2"; //arg
            //string ContentPath = "//*[@class='entry-content']"; //arg
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
                    e.Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CleanText(System.Net.WebUtility.HtmlDecode(Doc.DocumentNode.SelectSingleNode(TitlePath).InnerText)).ToLower());
                    //e.Content = Doc.DocumentNode.SelectSingleNode(ContentPath).InnerHtml;

                    try { e.Size = GetBetween(page.ToLower(), "size:", "b").Replace("<strong>", "").ToUpper().Replace(",", ".") + "B"; } catch { }
                    e.Link = _Link;
                    e.IndexDate = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}";
                    e.Repacker = "Skidrow";
                    Entries.Add(e);
                    Console.WriteLine(JsonConvert.SerializeObject(e));
                }
            }

            var es = JsonConvert.SerializeObject(Entries);
            File.WriteAllText($"Data/{BaseUrl}.json", es);

        }
    }
}
