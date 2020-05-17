using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using TheBitBrine;

namespace Zorro
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().StartServer();
        }
        private int MaxPages = 2000;
        private QuickMan API;
        private List<Entry> Entries = new List<Entry>();
        public void StartServer()
        {
            Entries.Add(new Entry() { IndexDate = "2019-7-16 4:22", Link = "https://github.com/thebitbrine", Repacker = "TheBitBrine", Size = "64 KB", Title = "Zorro v1.1" });
            

            Directory.CreateDirectory("Data");
            
            new Thread(UpdateLists) { IsBackground = true }.Start();
            new Thread(WriteStats) { IsBackground = true }.Start();

            API = new QuickMan();
            var Endpoints = new Dictionary<string, Action<HttpListenerContext>>();

            Endpoints.Add("query", Query);
            Endpoints.Add("/", Index);
            Endpoints.Add("Web", Web);
            Endpoints.Add("open", OpenLink);
            Endpoints.Add("random", RandomGame);
            API.Start(8130, Endpoints, 20);

        }

        public void RandomGame(HttpListenerContext Context)
        {
            string Game = Entries[new Random().Next(0, Entries.Count - 1)].Title;
            Context.Response.Redirect($"../query?q={Game}");
        }

        public void Index(HttpListenerContext Context)
        {
            StringBuilder Recents = new StringBuilder();
            Recents.AppendLine($"<a class=\"bubble\" href=\"/random\">[Random]</a>");
            foreach (var Rec in LastQueries)
            {
                string ShowQ = Rec;
                if (ShowQ.Length > 10)
                {
                    ShowQ = ShowQ.Remove(10, ShowQ.Length - 10) + "...";
                }
                Recents.AppendLine($"<a class=\"bubble\" href=\"/query?q={Rec}\">{ShowQ}</a>");
            }
            try { API.Respond(File.ReadAllText(Rooter("Web/index.html")).Replace("%recents%",Recents.ToString()).Replace("%count%", $"With more than {String.Format("{0:n0}", Entries.Count)} games indexed!"), "text/html", Context); } catch { }
            CollectStats(Context, "/", 1);
        }

        public void Web(HttpListenerContext Context)
        {
            try
            {
                var Path = Context.Request.RawUrl;
                if (Path.StartsWith('/'))
                    Path = Path.Remove(0, 1);

                if (File.Exists(Rooter(Path)))
                    API.Respond(new FileStream(Rooter(Path), FileMode.Open, FileAccess.Read), MimeMapping.GetMimeType(new FileInfo(Path).Extension), Context);
            }
            catch { }
        }
        public void Query(HttpListenerContext Context)
        {
            try
            {
                //var q = Context.Request.QueryString["q"];
                var body = new StreamReader(Context.Request.InputStream).ReadToEnd();
                var q = HttpUtility.UrlDecode(body).Replace("q=", "").Replace("+"," ");
                if (string.IsNullOrWhiteSpace(q) && Context.Request.QueryString.AllKeys.Contains("q"))
                    q = Context.Request.QueryString["q"];
                if (string.IsNullOrWhiteSpace(q) && LastQueries.Any())
                {
                    Context.Response.Redirect("../random");
                }
                else
                {
                    var res = Entries.Where(x => x.Title.ToLower().Contains(q.ToLower())).Take(10).ToList();
                    if (q.Contains(" "))
                    {
                        var qu = q.Split(' ');
                        foreach (var que in qu)
                        {
                            res.AddRange(Entries.Where(x => x.Title.ToLower().Contains(que.ToLower())).ToList().Take(10));
                        }
                    }

                    List<Entry> FEntries = new List<Entry>();
                    foreach (var Entry in res)
                    {
                        if (!FEntries.Contains(Entry))
                            FEntries.Add(Entry);
                    }


                    var Data = new StringBuilder();
                    foreach (var entry in FEntries.Take(30).GroupBy(x => x.Link).Select(y => y.First()))
                    {
                        Data.AppendLine($"<tr><td class=\"column1\">{entry.IndexDate}</td><td class=\"column2\">{entry.Title}</td><td class=\"column3\">{FormatSize(entry.Size)}</td><td class=\"column4\">{entry.Repacker}</td><td class=\"column5\"><a href=\"/open?q={q}&link={entry.Link}\" target=\"_blank\">Source</a></td></tr>");
                    }
                    var template = File.ReadAllText(Rooter("Web/search.html"));
                    API.Respond(template.Replace("%data%", Data.ToString()).Replace("%q%", q), "text/html", Context);
                    CollectStats(Context, q, FEntries.Count);
                }
            }
            catch { }
        }

       List<string> Stats = new List<string>();
       List<string> LastQueries = new List<string>();

        public void OpenLink(HttpListenerContext Context)
        {
            try
            {
                string Stat = $"{Context.Request.RemoteEndPoint} > {HttpUtility.UrlDecode(Context.Request.QueryString["q"])} > {HttpUtility.UrlDecode(Context.Request.QueryString["link"])}";
                Stats.Add(Stat);
                Console.WriteLine(Stat);
                Context.Response.Redirect(Context.Request.QueryString["link"]);
            }
            catch { }
        }

        public void CollectStats(HttpListenerContext Context, string Query, int ResCount)
        {
            try
            {
                if (ResCount > 0)
                {
                    string Stat = $"{Context.Request.RemoteEndPoint} > {Context.Request.RawUrl} > {Query}";
                    while (LastQueries.Count > 3)
                        LastQueries.RemoveAt(0);
                    if (Query != "/" && !string.IsNullOrWhiteSpace(Query))
                    {
                        string ShowQ = Query;
                        if(!LastQueries.Contains(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ShowQ)))
                            LastQueries.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ShowQ));
                    }

                    Stats.Add(Stat);
                    Console.WriteLine(Stat);
                }
            }
            catch { }
        }

        public void WriteStats()
        {
            while (true)
            {
                try
                {
                    File.AppendAllLines(Rooter("Stats.csv"), Stats.ToArray());
                }
                catch { }
                Thread.Sleep(60000);
            }
        }

        public string FormatSize(string Size)
        {
            StringBuilder FinalSize = new StringBuilder();
            string ID = "";
            if (Size.ToLower().Contains("mb"))
                ID = "MB";
            if (Size.ToLower().Contains("gb"))
                ID = "GB";
            if (Size.ToLower().Contains("kb"))
                ID = "KB";
            var nums = Size.ToCharArray();
            foreach (var Char in nums)
            {
                if (!char.IsLetter(Char) && char.IsNumber(Char) && Char != '.' && Char != ',')
                    FinalSize.Append(Char);

                if (!char.IsNumber(Char) && Char == '.' || Char == ',')
                    FinalSize.Append('.');
                if (Char == '/' || Char == '\\')
                    FinalSize.Append(" / ");
            }
            FinalSize.Append($" {ID}");
            return FinalSize.ToString();

        }
        public bool FirstRun = true;
        public void UpdateLists()
        {
            while (true)
            {
                try
                {
                    var Files = Directory.GetFiles(Rooter("Data"));

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
                                Entries.AddRange(NewEntries);

                            }
                            catch { }
                        }
                    }

                    new Thread(Xatab) { IsBackground = true }.Start();
                    new Thread(Fitgirl) { IsBackground = true }.Start();
                    new Thread(Skidrow) { IsBackground = true }.Start();
                    new Thread(RGMechanics) { IsBackground = true }.Start();
                    new Thread(ElAmigos) { IsBackground = true }.Start();
                    new Thread(KapitalSin) { IsBackground = true }.Start();
                    Thread.Sleep(86400000);
                }
                catch { }
            }
        }

        public string GetBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (!string.IsNullOrWhiteSpace(strSource) && strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return null;
            }
        }
        public string CleanText(string Input)
        {
            string res = Input.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\b", "");
            if (res.StartsWith(' '))
                res = res.Remove(0, 1);

            if (res.EndsWith(' '))
                res = res.Remove(res.Length - 1, 1);

            while (res.Contains("  "))
                res = res.Replace("  ", " ");
            return res;
        }
        public string Rooter(string RelPath)
        {
            return RelPath;
        }


        public void RGMechanics()
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

        public void Xatab()
        {
            List<string> AllLinks = new List<string>();
            string OGLink = "https://xatab-repack.net/?page%page%"; //arg
            string Link = "";
            int Tries = 0;
            bool Added = false;
            int LastPage = 0;
            var BaseUrl = new Uri(OGLink).Host;
            string[] PageVaildIfContains = new string[]
                {
                    "Жанр",
                    "ТОРРЕНТ",
                    "Коментарии"
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


            string TitlePath = "//*[@class='pc_title']"; //arg
            string ContentPath = "//*[@class='page_content']"; //arg
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
                    if (page.Contains("Размер:"))
                        e.Size = (GetBetween(page.ToUpper().Replace("Б", " B"), "РАЗМЕР:", "B").ToUpper().Replace(",", ".").Replace(" ", "") + "B").Replace("GB", " GB");
                    e.IndexDate = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}";
                    e.Repacker = "Xatab";
                    e.Link = _Link;
                    Entries.Add(e);
                    Console.WriteLine(JsonConvert.SerializeObject(e));
                }
            }

            var es = JsonConvert.SerializeObject(Entries);
            File.WriteAllText($"Data/{BaseUrl}.json", es);

        }


        public void Fitgirl()
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

        public void ElAmigos()
        {
            List<string> AllLinks = new List<string>();
            string OGLink = "https://www.elamigos-games.com/?page=%page%"; //arg
            string Link = "";
            int Tries = 0;
            bool Added = false;
            int LastPage = 0;
            var BaseUrl = new Uri(OGLink).Host;
            string[] PageVaildIfContains = new string[]
                {
                    "TORRENT",
                    "games_tumbl",
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


            string TitlePath = "//div[@class=\"container\"]/h2"; //arg
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
                    e.Repacker = "ElAmigos";
                    e.Size = GetBetween(e.Title, "ElAmigos, ", "GB") + " GB";
                    e.Link = _Link;
                    Entries.Add(e);
                    Console.WriteLine(JsonConvert.SerializeObject(e));
                }
            }

            var es = JsonConvert.SerializeObject(Entries);
            File.WriteAllText($"Data/{BaseUrl}.json", es);

        }

        public void KapitalSin()
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
                    if(_match.Contains("#"))
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
                    e.Size = GetBetween(page.ToUpper(), "AMAÑO", "B").ToUpper().Replace(",",".").Replace("</STRONG>: ","").Replace(":</STRONG> ","") + "B";
                    e.Link = _Link;
                    Entries.Add(e);
                    Console.WriteLine(JsonConvert.SerializeObject(e));
                }
            }

            var es = JsonConvert.SerializeObject(Entries);
            File.WriteAllText($"Data/{BaseUrl}.json", es);

        }


        public void Skidrow()
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


        public class Entry
        {
            public string IndexDate;
            public string Title;
            public string Size;
            public string Repacker;
            public string Link;
        }

        public string GetWebString(string URL)
        {
            var client = new System.Net.WebClient() { Encoding = System.Text.Encoding.UTF8 };
            client.Headers.Add("user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36");
            string res = "";
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    res = client.DownloadString(URL);
                    return res;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(1000 * i);
                }
            }
            return "";
        }

    }
}
