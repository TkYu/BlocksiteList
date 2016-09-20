using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlocksiteList
{
    public class Program
    {
        const string fuck = @"[Greatfirelist]
! Expires: 7d
! Title: Greatfire List
! fu(o)c(r)k from https://github.com/gfwlist/tinylist
! Last Modified: {0}
!
! HomePage: https://github.com/TkYu/BlocksiteList
! License: https://github.com/TkYu/BlocksiteList/blob/master/LICENSE
!
!##############General List Start###############
{1}
!##############General List End#################

!##############Greatfire List Start#################
{2}
!##############Greatfire List End#################
";

        private static readonly string[] deepBlackList =
        {
            "blogspot.com",
            "dropbox.com",
            "facebook.com",
            "fbcdn.net",
            "ggpht.com",
            "gmodules.com",
            "google.com",
            "google.com.hk",
            "googleapis.com",
            "googleusercontent.com",
            "instagram.com",
            "twitter.com",
            "zh.wikipedia.org",
            "youtube.com",
            "scmpchinese.com",
            "scmp.com"
        };

        const string fucker = @"https://en.greatfire.org/search/blocked?page=";

        static readonly string[] elastic =
        {
            @"http://192.168.10.1:9200/_analyze?analyzer=ik_smart&pretty=false&text=",
            @"http://192.168.10.9:9200/_analyze?analyzer=ik_smart&pretty=false&text=",
            @"http://192.168.10.10:9200/_analyze?analyzer=ik_smart&pretty=false&text=",
            @"http://192.168.10.11:9200/_analyze?analyzer=ik_smart&pretty=false&text="
        };

        private static bool useLocal;
        public static void Main(string[] args)
        {
            int pageSize = 0;
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
            var lst = new ConcurrentBag<MObject>();
            Parallel.For(0, pageSize, new ParallelOptions {MaxDegreeOfParallelism = 24}, (i, loopState) =>
            {
                Console.WriteLine("Working on page {0}", i);
                try
                {
                    var content = Take(i); //sync
                    var items = Parse(content);
                    foreach (var mObject in items)
                        lst.Add(mObject);
                }
                catch (Exception)
                {
                    //TODO hehe
                }
            });
            Console.WriteLine("Take Finished\nStart Analysis");
            var result = new ConcurrentBag<string>();
            var tokeniz = new ConcurrentDictionary<string, int>();
            Parallel.ForEach(lst, new ParallelOptions { MaxDegreeOfParallelism = 12 }, (item, loopState) =>
            {
                if (item.Percent < 50) return;
                var split = item.Domain.Split('/');
                if (split.Length == 1)
                {//domain
                    result.Add(item.Domain);
                }
                else if (split.Length == 3 && item.Domain.StartsWith("http"))
                {//domain with scheme
                    result.Add("|" + item.Domain);
                }
                else
                {//url address
                    var tail = split[split.Length - 1];
                    if (tail.Contains("%") && tail.Length > 6)
                    {//with path
                        var dec = tail.UrlDecode();
                        if (dec.Any(c => c > 127))
                        {//with chinese path
                            try
                            {
                                var r = Tokeniz(tail, System.Threading.Thread.CurrentThread.ManagedThreadId);
                                foreach (var t in r.tokens.Where(c => c.type == "CN_WORD"))
                                    tokeniz.AddOrUpdate(t.token, 1, (id, count) => count + 1);
                            }
                            catch
                            {
                                //TODO excuseme?
                            }
                        }
                        else
                        {//without chinese path
                            //Too mach, drop
                            //result.Add("||" + item.Domain + "|");
                        }

                    }
                    else
                    {//others
                        //Too mach, drop
                        //result.Add("||" + item.Domain);
                    }
                }
            });
            Console.WriteLine("Analysis Done");
            var blackWords = string.Join("\n", tokeniz.OrderByDescending(c => c.Value).Select(c => $"词语：{c.Key}；词频：{c.Value}"));
            File.WriteAllText("O_BlackWords.txt", blackWords);
            var resultContent = string.Format(fuck, DateTime.Now.ToString("R"), string.Join("\n", deepBlackList.Select(c => $"||{c}")), string.Join("\n", result));
            resultContent = resultContent.Replace("[Greatfirelist]", $"[Greatfirelist]\n! Checksum: {CalcSum(resultContent)}");
            File.WriteAllText("O_greatfirelist.txt", resultContent);

            File.WriteAllText("BlackWords.txt", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(blackWords)));
            File.WriteAllText("greatfirelist.txt", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(resultContent)));
            Console.WriteLine("All Finished");
        }

        private static ElasticResult Tokeniz(string input,int threadid)
        {
            using (var hc = new HttpClient())
            {
                int fp = threadid%elastic.Length;
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
            //var clean = System.Text.RegularExpressions.Regex.Replace(content, @"! Checksum:.*\n", "").Replace("\r", "").Replace("\n\n", "\n");
            var clean = content.Replace("\r", "").Replace("\n\n", "\n");
            using (var sha1 = System.Security.Cryptography.MD5.Create())
                return Convert.ToBase64String(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(clean))).TrimEnd('=');
        }

        private static bool IsRunningOnCore()
        {
            var fileName = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            return fileName == "dotnet";
        }
    }
}