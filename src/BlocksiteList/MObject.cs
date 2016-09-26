using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlocksiteList
{
    public class MapObject
    {
        public List<string> urlWithQuery { get; set; } = new List<string>();
        public List<string> urlWithHttps { get; set; } = new List<string>();
        public List<string> pureIPAddress { get; set; } = new List<string>();
        public List<string> pureIPAddressWithPort { get; set; } = new List<string>();
        public List<string> pureHost { get; set; } = new List<string>();
    }
    public class ReduceObject
    {
        public Dictionary<string, int> Tokeniz { get; set; } = new Dictionary<string, int>();
        public List<string> Result { get; set; } = new List<string>();
    }
    public class MObject
    {
        public MObject(string domain,string date,int percent,string[] tags)
        {
            Domain = domain;
            Date = date;
            Percent = percent;
            Tags = tags;
        }

        public string Domain { get; }
        public string Date { get;  }
        public int Percent { get; }
        public string[] Tags { get; }
    }

    public class ElasticResult
    {
        public ElasticToken[] tokens { get; set; }
    }

    public class ElasticToken
    {
        public string token { get; set; }
        public int start_offset { get; set; }
        public int end_offset { get; set; }
        public string type { get; set; }
        public int position { get; set; }
    }
}
