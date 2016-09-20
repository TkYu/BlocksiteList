using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlocksiteList
{
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
