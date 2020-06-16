using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Zorro.Scrapers.API;
using static Zorro.Classes;
using static Zorro.Program;
using static Zorro.Toolbox;

namespace Zorro
{
    public static class WebTools
    {

        public static void RandomGame(HttpListenerContext Context)
        {
            string Game = Entries[new Random().Next(0, Entries.Count - 1)].Title;
            Context.Response.Redirect($"../query?q={Game}");
        }

        public static void Index(HttpListenerContext Context)
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
            try { API.Respond(File.ReadAllText("Web/index.html").Replace("%recents%", Recents.ToString()).Replace("%count%", $"With more than {String.Format("{0:n0}", Entries.Count)} games indexed!"), "text/html", Context); } catch { }
            CollectStats(Context, "/");
        }

        public static void Web(HttpListenerContext Context)
        {
            try
            {
                var Path = Context.Request.RawUrl;
                if (Path.StartsWith('/'))
                    Path = Path.Remove(0, 1);

                if (File.Exists(Path))
                    API.Respond(new FileStream(Path, FileMode.Open, FileAccess.Read), MimeMapping.GetMimeType(new FileInfo(Path).Extension), Context);
            }
            catch { }
        }
        public static void Query(HttpListenerContext Context)
        {
            try
            {
                var body = new StreamReader(Context.Request.InputStream).ReadToEnd();
                var q = HttpUtility.UrlDecode(body).Replace("q=", "").Replace("+", " ");
                if (string.IsNullOrWhiteSpace(q) && Context.Request.QueryString.AllKeys.Contains("q"))
                    q = Context.Request.QueryString["q"];
                if (string.IsNullOrWhiteSpace(q) && LastQueries.Any())
                {
                    Context.Response.Redirect("../random");
                }
                else
                {
                    var res = Entries.Where(x => x.Title.ToLower().Contains(q.ToLower())).Take(10).ToList();
                    res.AddRange(ThePirateBay.Query(q));
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
                    var template = File.ReadAllText("Web/search.html");
                    API.Respond(template.Replace("%data%", Data.ToString()).Replace("%q%", q), "text/html", Context);
                    CollectStats(Context, q);
                }
            }
            catch { }
        }


        public static void OpenLink(HttpListenerContext Context)
        {
            try
            {
                var Query = HttpUtility.UrlDecode(Context.Request.QueryString["q"]);
                CollectStats(Context, Query);
                Context.Response.Redirect(Context.Request.QueryString["link"]);
            }
            catch { }
        }

        public static void CollectStats(HttpListenerContext Context, string Query)
        {
            try
            {
                var Request = new iRequest()
                {
                    Endpoint = Context.Request.RemoteEndPoint.ToString(),
                    Headers = ToDictionary(Context.Request.Headers),
                    Referer = Context.Request.UrlReferrer,
                    URI = Context.Request.Url,
                    UserAgent = Context.Request.UserAgent,
                    UserLanguages = Context.Request.UserLanguages
                };

                var Stat = new Stat() { Request = Request, Query = Query };
                while (LastQueries.Count > 3)
                    LastQueries.RemoveAt(0);
                if (Query != "/" && !string.IsNullOrWhiteSpace(Query))
                {
                    string ShowQ = Query;
                    if (!LastQueries.Contains(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ShowQ)))
                        LastQueries.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ShowQ));
                }

                Zorro.MongoDB.SendStat(Stat);
            }
            catch { }
        }

        public static Dictionary<string, string> ToDictionary(NameValueCollection nvc)
        {
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }

        public class Stat
        {
            public DateTime Date = DateTime.UtcNow;
            public iRequest Request;
            public string Query;

        }

        public class iRequest
        {
            public Uri URI;
            public string Endpoint;
            public Uri Referer;
            public string UserAgent;
            public string[] UserLanguages;
            public Dictionary<string, string> Headers;
        }

    }
}
