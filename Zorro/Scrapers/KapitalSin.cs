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
    public static class KapitalSinScraper
    {
        public static void KapitalSin()
        {
            List<string> AllLinks = new List<string>();
            string OGLink = "http://www.kapitalsin.com/forum/index.php?board=24.%page%"; //arg
            string Link = "";
            int Tries = 0;
            bool Added = false;
            int LastPage = 0;
            var BaseUrl = new Uri(OGLink).Host;
            string[] PageVaildIfContains = new string[]
                {
                    "énero",
                    "amaño",
                }; //arg

            //Surface scrape
            for (int i = 0; i < MaxPages; i = i + 10)
            {
                Link = OGLink.Replace("%page%", $"{i}");
                var home = GetWebString(Link);
                var shit = GetBetween(home, "PHPSESSID=", "&amp;");
                home = home.Replace($"PHPSESSID={shit}&amp;", "");
                var matches = Regex.Matches(home, "<a href=\"(.*?)\"", RegexOptions.Singleline).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                foreach (var Match in matches)
                {
                    var _match = Match;
                    if (_match.Contains("#"))
                        _match = _match.Remove(Match.IndexOf('#'));
                    if (!AllLinks.Contains(_match) && _match.Contains(BaseUrl))
                    {
                        AllLinks.Add(_match);
                        Console.WriteLine(_match);
                        Tries = 0;
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


            string TitlePath = "//div[@class=\"keyinfo\"]/h5"; //arg
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
                    e.Title = CleanText(System.Net.WebUtility.HtmlDecode(Doc.DocumentNode.SelectSingleNode(TitlePath).InnerText));
                    //e.Content = Doc.DocumentNode.SelectSingleNode(ContentPath).InnerHtml;
                    e.IndexDate = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}";
                    e.Repacker = "KapitalSin";
                    e.Size = GetBetween(page.ToUpper(), "AMAÑO", "B").ToUpper().Replace(",", ".").Replace("</STRONG>: ", "").Replace(":</STRONG> ", "") + "B";
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
