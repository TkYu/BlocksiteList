using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlocksiteList
{
    public static class Program
    {
        #region var
        private const string fuck = @"[AutoProxy]
! Expires: 7d
! Title: Greatfire List
! Fu(o)c(r)k from https://github.com/gfwlist/tinylist
! Last Modified: {0}
!
! HomePage: https://github.com/TkYu/BlocksiteList
! License: https://github.com/TkYu/BlocksiteList/blob/master/LICENSE
!
! Okay let's go

!##############General List Start###############
{1}
!##############General List End#################

!##############Greatfire List Start#################
{2}
!##############Greatfire List End#################

!################White List Start################
@@||aliyun.com
@@||baidu.com
@@||bing.com
@@||bt.byr.cn
@@||chinaso.com
@@|http://nrch.culture.tw/
@@||fonts.googleapis.com
@@||cn.gravatar.com
@@||csi.gstatic.com
@@||fonts.gstatic.com
@@||haosou.com
@@||jd.com
@@||jike.com
@@|http://translate.google.cn
@@|http://www.google.cn/maps
@@||http2.golang.org
@@||gov.cn
@@||bt.neu6.edu.cn
@@||qq.com
@@||sina.cn
@@||sina.com.cn
@@||sogou.com
@@||so.com
@@||soso.com
@@||taobao.com
@@||weibo.com
@@||yahoo.cn
@@||youdao.com
@@||zhongsou.com
@@|http://ime.baidu.jp
!################White List End##################
";

        private static readonly string[] deepBlackList =
        {
            "appspot.com",
            "blogspot.com",
            "chrome.com",
            "dropbox.com",
            "facebook.com",
            "fbcdn.net",
            "ggpht.com",
            "gmodules.com",
            "google.com",
            "google.co.jp",
            "google.co.kr",
            "google.com.hk",
            "google.com.sg",
            "google.com.tw",
            "googleapis.com",
            "googleusercontent.com",
            "instagram.com",
            "twitter.com",
            "zh.wikipedia.org",
            "youtube.com",
            "scmpchinese.com",
            "scmp.com"
        };

        private const string google = @"
/^https?:\/\/([^\/]+\.)*google\.(ac|ad|ae|al|am|as|at|az|ba|be|bf|bg|bi|bj|bs|bt|by|ca|cat|cd|cf|cg|ch|ci|cl|cm|co.ao|co.bw|co.ck|co.cr|co.id|co.il|co.in|co.jp|co.ke|co.kr|co.ls|co.ma|com|com.af|com.ag|com.ai|com.ar|com.au|com.bd|com.bh|com.bn|com.bo|com.br|com.bz|com.co|com.cu|com.cy|com.do|com.ec|com.eg|com.et|com.fj|com.gh|com.gi|com.gt|com.hk|com.jm|com.kh|com.kw|com.lb|com.ly|com.mm|com.mt|com.mx|com.my|com.na|com.nf|com.ng|com.ni|com.np|com.om|com.pa|com.pe|com.pg|com.ph|com.pk|com.pr|com.py|com.qa|com.sa|com.sb|com.sg|com.sl|com.sv|com.tj|com.tr|com.tw|com.ua|com.uy|com.vc|com.vn|co.mz|co.nz|co.th|co.tz|co.ug|co.uk|co.uz|co.ve|co.vi|co.za|co.zm|co.zw|cv|cz|de|dj|dk|dm|dz|ee|es|fi|fm|fr|ga|ge|gg|gl|gm|gp|gr|gy|hk|hn|hr|ht|hu|ie|im|iq|is|it|je|jo|kg|ki|kz|la|li|lk|lt|lu|lv|md|me|mg|mk|ml|mn|ms|mu|mv|mw|mx|ne|nl|no|nr|nu|org|pl|pn|ps|pt|ro|rs|ru|rw|sc|se|sh|si|sk|sm|sn|so|sr|st|td|tg|tk|tl|tm|tn|to|tt|us|vg|vn|vu|ws)\/.*/";

        private const string fucker = @"https://en.greatfire.org/search/blocked?page=";

        private static readonly string[] elastic =
        {
            @"http://192.168.10.1:9200/_analyze?analyzer=ik_smart&pretty=false&text=",
            @"http://192.168.10.9:9200/_analyze?analyzer=ik_smart&pretty=false&text=",
            @"http://192.168.10.10:9200/_analyze?analyzer=ik_smart&pretty=false&text=",
            @"http://192.168.10.11:9200/_analyze?analyzer=ik_smart&pretty=false&text="
        };

        private static readonly System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex(@"^[\d\.:]+$", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static bool useLocal;
        #endregion
        public static void Main(string[] args)
        {
            
            var pageSize = 0;
            if (Directory.Exists("local"))
            {
                pageSize = Directory.GetFiles("local", "*.txt", SearchOption.TopDirectoryOnly).Length - 1;
                Console.Write($"LocalCache DetectedPageSize:{pageSize}, press enter to start");
                Console.ReadLine();
                useLocal = true;
            }

            if (pageSize == 0)
            {
                Console.Write("PageSize:");
                if (!int.TryParse(Console.ReadLine(), out pageSize))
                {
                    Console.WriteLine("NaN");
                    return;
                }
            }

            if (pageSize == 0) return;
            Console.WriteLine("Task Start");
            var lst = Prepare(pageSize);
            Console.WriteLine("Take Finished\nStart MR");
            var result = MapReduce(lst);
            Console.WriteLine("MR Finished\nWrite File...");
            result.Save();
            Console.WriteLine("All Finished");
        }

        #region MR
        public static Tuple<string, string> MapReduce(MObject[] list)
        {
            Console.WriteLine("Start Map");
            var map = Map(list);
            Console.WriteLine("Map Finished\nStart Reduce");
            var reduce = Reduce(map, Math.Min(Environment.ProcessorCount, elastic.Length*2));
            Console.WriteLine("Reduce Finished\nStart Finalize");
            return Finalize(reduce);
        }
        public static void Save(this Tuple<string, string> result)
        {
            File.WriteAllText("O_BlackWords.txt", result.Item1);
            File.WriteAllText("O_greatfirelist.txt", result.Item2);
            File.WriteAllText("BlackWords.txt", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(result.Item1)));
            File.WriteAllText("greatfirelist.txt", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(result.Item2)));
            //File.WriteAllText("BlackWords.txt", string.Join("\n", Split(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(result.Item1)),64)));
            //File.WriteAllText("greatfirelist.txt", string.Join("\n", Split(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(result.Item2)),64)));
        }
        public static MapObject Mapping(this MObject[] list)
        {
            var ret = new MapObject();
            foreach (var url in list)
            {
                var split = url.Domain.Split('/');
                if (split.Length == 1)
                {//domain
                    if (!url.Domain.Contains("%"))
                    {
                        if (rx.IsMatch(url.Domain))
                        {
                            if (url.Domain.Contains(":"))
                                ret.pureIPAddressWithPort.Add(url.Domain);
                            else
                                ret.pureIPAddress.Add(url.Domain);
                        }
                        else
                        {
                            ret.pureHost.Add(url.Domain);
                        }
                    }
                }
                else if (split.Length == 3 && url.Domain.StartsWith("https"))
                {//domain with scheme
                    ret.urlWithHttps.Add(split[2]);
                }
                else if (split.Length == 3 && url.Domain.StartsWith("http"))
                {//domain with scheme
                    //urlWithHttp.Add(split[2]);
                    throw new Exception("wocao");
                }
                else
                {//url address
                    var tail = split[split.Length - 1];
                    if (!ret.urlWithQuery.Contains(tail)) ret.urlWithQuery.Add(tail);
                }
            }
            return ret;
        }
        public static MapObject Combine(this MapObject mo)
        {
            mo.urlWithHttps = mo.urlWithHttps.Distinct().ToList();
            mo.pureIPAddressWithPort = mo.pureIPAddressWithPort.Distinct().ToList();
            mo.pureHost = mo.pureHost.Distinct().ToList();
            var ipdistinct = mo.pureIPAddress.Distinct().ToDictionary(k=>k, v =>
            {
                var split = v.Split('.');
                return $"{split[0]}.{split[1]}.{split[2]}";
            });
            var ipCounter = ipdistinct.GroupBy(c => c.Value).ToDictionary(k => k.Key, v => v.Count());
            var filtered = ipCounter.Where(c => c.Value > 10).Select(c=>c.Key).ToArray();
            var result = (from ip in ipdistinct
                          where !filtered.Contains(ip.Value)
                          select ip.Key).ToList();
            result.AddRange(filtered.Select(c => $"{c}.*"));
            mo.pureIPAddress = result;
            return mo;
        }
        public static Tuple<string,string> Finalize(ReduceObject ro)
        {
            var BlackWords = string.Join("\n", ro.Tokeniz.OrderByDescending(c => c.Value).Select(c => $"词语：{c.Key}；词频：{c.Value}"));
            var resultContent = string.Format(fuck, 
                DateTime.Now.ToString("R"), 
                string.Join("\n", deepBlackList.Select(c => $"||{c}")) + google,
                string.Join("\n", ro.Result));
            resultContent = resultContent.Replace("\r", "").Replace("[AutoProxy]", $"[AutoProxy]\n! Checksum: {CalcSum(resultContent)}");
            return new Tuple<string, string>(BlackWords, resultContent);
        }
        public static MapObject Map(MObject[] list)
        {
            return list.Mapping().Combine();
        }
        public static ReduceObject Reduce(MapObject mo, int? limit = null)
        {
            var result = new ReduceObject();
            Console.WriteLine("Merge");
            result.Result.AddRange(mo.pureIPAddress);
            result.Result.AddRange(mo.pureHost.Select(c => c.StartsWith("www.") ? $"||{c.Substring(4, c.Length - 4)}" : $"||{c}"));
            foreach (var item in mo.urlWithHttps)
            {
                if(mo.pureHost.All(c => c != item))
                    result.Result.Add("|https://" + item);
            }
            Console.WriteLine("Take BlackWords");
            var tokeniz = new ConcurrentDictionary<string, int>();
            Parallel.ForEach(mo.urlWithQuery, new ParallelOptions { MaxDegreeOfParallelism = limit ?? Environment.ProcessorCount }, (query, loopState) =>
            {
                var dec = query.UrlDecode();
                if (string.IsNullOrWhiteSpace(dec)) return;
                if (dec.Any(c => c > 127))
                {//with chinese path
                    try
                    {
                        var r = Tokeniz(query, System.Threading.Thread.CurrentThread.ManagedThreadId);
                        foreach (var t in r.tokens.Where(c => c.type == "CN_WORD" || c.type == "ENGLISH"))
                            tokeniz.AddOrUpdate(t.token, 1, (id, count) => count + 1);
                    }
                    catch
                    {
                        //TODO excuseme?
                    }
                }
                else
                {
                    //var spl = dec.Split(' ');
                    //foreach (var s in spl)
                    //{
                    //    result.Tokeniz.AddOrUpdate(s, 1, (id, count) => count + 1);
                    //}
                }
            });
            result.Tokeniz = new Dictionary<string, int>(tokeniz);
            return result;
        }
        #endregion

        #region Methods

        private static MObject[] Prepare(int pageSize, int? limit = null)
        {
            var lst = new ConcurrentBag<MObject>();
            Parallel.For(0, pageSize, new ParallelOptions {MaxDegreeOfParallelism = limit ?? Environment.ProcessorCount}, (i, loopState) =>
            {
                Console.WriteLine("Working on page {0}", i);
                try
                {
                    var content = Take(i); //sync
                    var items = Parse(content).Where(c => c.Percent > 75);
                    foreach (var mObject in items)
                        lst.Add(mObject);
                }
                catch (Exception)
                {
                    //TODO hehe
                }
            });
            return lst.ToArray();
        }

        private static ElasticResult Tokeniz(string input, int threadid)
        {
            using (var hc = new HttpClient())
            {
                int fp = threadid % elastic.Length;
                var result = hc.GetStringAsync(elastic[fp] + input).ConfigureAwait(false).GetAwaiter().GetResult();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ElasticResult>(result);
            }
        }

        private static IEnumerable<MObject> Parse(string content)
        {
            //cheers to big memory! fuck yiled.
            var lst = new List<MObject>();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(content);
            var findclasses = doc.DocumentNode.Descendants("tr").Where(d => d.Attributes.Contains("class") && (d.Attributes["class"].Value.Contains("odd") || d.Attributes["class"].Value.Contains("even")));
            foreach (var node in findclasses)
            {
                if (node.HasAttributes && node.FirstChild.HasAttributes)
                {
                    var first = node.ChildNodes;
                    var a = first[0];
                    if (a.InnerText.Contains("."))
                    {
                        if (deepBlackList.Any(a.InnerText.Contains)) continue;
                        string domain, date;
                        string[] tags;
                        int percent;
                        try
                        {
                            domain = a.ChildNodes[0].Attributes[0].Value.TrimStart('/').Replace("https/", "https://").Replace("http/", "http://");
                            date = first[1].InnerText;
                            percent = int.Parse(first[2].InnerText.Replace("%", ""));
                            tags = first[3].ChildNodes.Where(c => c.Name == "a").Select(c => c.InnerText).ToArray();
                        }
                        catch (Exception)
                        {
                            //TODO
                            continue;
                        }
                        lst.Add(new MObject(domain, date, percent, tags));
                    }
                }
            }
            return lst;
        }

        private static string Take(int currPage)
        {
            if (useLocal)
            {
                return File.ReadAllText($"local\\{currPage}.txt");
            }
            using (var hc = new HttpClient())
                return hc.GetStringAsync(fucker + currPage).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        private static bool CheckSum(string content)
        {
            var curr = new System.Text.RegularExpressions.Regex(@"Checksum:(.*?)\n");
            if (curr.IsMatch(content))
            {
                var current = curr.Match(content).Groups[1].Value.Trim();
                return current == CalcSum(content);
            }
            return false;
        }
        private static string CalcSum(string content)
        {
            var clean = System.Text.RegularExpressions.Regex.Replace(content, @"! Checksum:.*\n", "").Replace("\r", "").Replace("\n\n", "\n");
            //var clean = content.Replace("\r", "").Replace("\n\n", "\n");
            using (var sha1 = System.Security.Cryptography.MD5.Create())
                return Convert.ToBase64String(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(clean))).TrimEnd('=');
        }

        private static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize).Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        private static bool IsRunningOnCore()
        {
            var fileName = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            return fileName == "dotnet";
        }
        #endregion
    }
}