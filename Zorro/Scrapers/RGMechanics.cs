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
    public static class RGMechanicsScraper
    {
        public static void RGMechanics()
        {
            List<string> AllLinks = new List<string>();
            string OGLink = "https://rg-mechanics.org/?page%page%"; //arg
            string Link = "";
            int Tries = 0;
            bool Added = false;
            int LastPage = 0;
            var BaseUrl = new Uri(OGLink).Host;
            string[] PageVaildIfContains = new string[]
                {
                    "downloadtorrent",
                    ".torrent",
                    "Скачать торрент"
                }; //arg

            //Surface scrape
            for (int i = 1; i < MaxPages; i++)
            {
                Link = OGLink.Replace("%page%", $"{i}");
                var home = GetWebString(Link);
                var matches = Regex.Matches(home, "<a href=\"(.*?)\"", RegexOptions.Singleline).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                foreach (var Match in matches)
                {
                    string link = Match;
                    if (link.StartsWith("/games/"))
                        link = $"http://{BaseUrl}{link}";

                    if (!AllLinks.Contains(link) && link.Contains(BaseUrl) && link.Contains("/games/"))
                    {
                        AllLinks.Add(link);
                        Console.WriteLine(link);
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


            string TitlePath = "//div[@class=\"full_top_bg\"]/div/h1"; //arg
            //string ContentPath = "//*[@class='page_content']"; //arg
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
                    try { e.Title = CleanText(System.Net.WebUtility.HtmlDecode(Doc.DocumentNode.SelectSingleNode(TitlePath).InnerText).Replace("скачать торрент", "")); }
                    catch
                    {
                        TitlePath = "//div[@class=\"full_top_bg\"]/div";
                        e.Title = CleanText(System.Net.WebUtility.HtmlDecode(Doc.DocumentNode.SelectSingleNode(TitlePath).InnerText).Replace("скачать торрент", ""));
                    }
                    var size = Doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'downloadtorrent')]").InnerText;
                    if (string.IsNullOrWhiteSpace(size))
                    {
                        e.Size = GetBetween(page, "азмер: ", "B") + "B";
                    }
                    else
                    {
                        try
                        {
                            e.Size = GetBetween(size, "(", ")").ToUpper().Replace(",", ".").Replace(" ", "").Replace("ГБ", " GB");
                        }
                        catch { }
                    }
                    e.IndexDate = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}";
                    e.Repacker = "RG Mechanics";
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
