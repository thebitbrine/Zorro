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
using static Zorro.Classes;
using static Zorro.Scrapers.Starter;
using static Zorro.WebTools;

namespace Zorro
{
    class Program
    {
        static void Main(string[] args)
        {
            StartServer();
        }

        public static int MaxPages = 2000;
        public static QuickMan API;
        public static List<Entry> Entries = new List<Entry>();
        public static List<string> Stats = new List<string>();
        public static List<string> LastQueries = new List<string>();

        public static void StartServer()
        {
#if DEBUG 
            MaxPages = 1;
#endif

            Entries.Add(new Entry() { IndexDate = "2019-7-16 4:22", Link = "https://github.com/thebitbrine", Repacker = "TheBitBrine", Size = "64 KB", Title = "Zorro v1.1" });
            
            Directory.CreateDirectory("Data");
            
            new Thread(UpdateLists) { IsBackground = true }.Start();

            API = new QuickMan();
            var Endpoints = new Dictionary<string, Action<HttpListenerContext>>();

            Endpoints.Add("query", Query);
            Endpoints.Add("/", Index);
            Endpoints.Add("Web", WebTools.Web);
            Endpoints.Add("open", OpenLink);
            Endpoints.Add("random", RandomGame);
            API.Start(8130, Endpoints, 20);

        }
       
    }
}
