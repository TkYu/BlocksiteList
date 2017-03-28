using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BlocksiteList
{
    public class MapObject
    {
        public List<string> url { get; } = new List<string>();
        public List<string> urlWithHttps { get; } = new List<string>();
        public List<IPAddress> ipAddress { get; } = new List<IPAddress>();
        public List<IPEndPoint> ipEndPoint { get; } = new List<IPEndPoint>();
        public List<string> pureDomain { get; } = new List<string>();
        public List<string> pureDomainWithPort { get; } = new List<string>();
        public List<string> badList { get; } = new List<string>();

        public void Save()
        {
            File.WriteAllText("url.txt", string.Join(Environment.NewLine, url));
            File.WriteAllText("urlWithHttps.txt", string.Join(Environment.NewLine, urlWithHttps));
            File.WriteAllText("ipAddress.txt", string.Join(Environment.NewLine, ipAddress.Select(c => c.ToString())));
            File.WriteAllText("ipEndPoint.txt", string.Join(Environment.NewLine, ipEndPoint.Select(c => c.ToString())));
            File.WriteAllText("pureDomain.txt", string.Join(Environment.NewLine, pureDomain));
            File.WriteAllText("pureDomainWithPort.txt", string.Join(Environment.NewLine, pureDomainWithPort));
            File.WriteAllText("badList.txt", string.Join(Environment.NewLine, badList));
        }
    }
    public class ReduceObject
    {
        public KeyValuePair<string, int>[] BadWords { get; set; }
        public KeyValuePair<string, int>[] Result { get; set; }
    }
    public class MObject
    {
        public MObject(string url, string date, int percent, string[] tags)
        {
            Url = url.Contains("%") ? WebUtility.UrlDecode(url) : url;
            Date = date;
            Percent = percent;
            Tags = tags;
        }

        public string Url { get; }
        public string Date { get; }
        public int Percent { get; }
        public string[] Tags { get; }
    }

    public class Token
    {
        public string token { get; set; }
        public int start_offset { get; set; }
        public int end_offset { get; set; }
        public PanGu.WordType type { get; set; }
        public int position { get; set; }
    }
}
