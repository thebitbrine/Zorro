using System;
using System.Collections.Generic;
using System.Text;

namespace Zorro
{
    public static class Toolbox
    {
        public static string GetWebString(string URL)
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

        public static string FormatSize(string Size)
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

        public static string GetBetween(string strSource, string strStart, string strEnd)
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
        public static string CleanText(string Input)
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
    }
}
