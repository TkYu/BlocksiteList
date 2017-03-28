using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlocksiteList
{
    class Worker
    {
        public async Task Work()
        {
            Console.WriteLine("Task Start");
            var pages = await TakeMO();
            Console.WriteLine("Take Finished\nStart MR");
            await Task.Run(() => MapReduce(pages));
            Console.WriteLine("All Finished");
        }

        #region MapReduce
        private void MapReduce(MObject[] list)
        {
            Console.WriteLine("Start Map");
            var map = Mapping(list);
            Console.WriteLine("Map Finished\nStart Reduce");
            var reduce = Reduce(map);
            Console.WriteLine("Reduce Finished\nStart Finalize");
            var final = Finalize(reduce);
            Console.WriteLine("Finalize Finished\nWriteFile");
            File.WriteAllText("O_blackwords.txt", final.badWords);
            File.WriteAllText("O_greatfirelist.txt", final.resultContent);
            File.WriteAllText("blackwords.txt", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(final.badWords)));
            File.WriteAllText("greatfirelist.txt", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(final.resultContent)));
        }
        readonly Regex rgxIP = new Regex(@"(\d+\.\d+\.\d+\.\d+):?(\d{1,5})?", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        MapObject Mapping(MObject[] list)
        {
            var ret = new MapObject();
            foreach (var item in list)
            {
                if (!item.Url.Contains('.'))
                {
                    ret.badList.Add(item.Url);
                    continue;
                }
                if (item.Url.Contains('?') && !item.Url.Contains('/'))
                {
                    ret.badList.Add(item.Url);
                    continue;
                }
                if (item.Url.Length>8 && (char.IsNumber(item.Url[0]) || char.IsNumber(item.Url[7]) || char.IsNumber(item.Url[8])))
                {
                    //maybe ipaddress
                    var mt = rgxIP.Match(item.Url);
                    if (mt.Success)
                    {
                        if (IPAddress.TryParse(mt.Groups[1].Value, out IPAddress ip))
                        {
                            if (string.IsNullOrEmpty(mt.Groups[2].Value))
                                ret.ipAddress.Add(ip);
                            else
                                ret.ipEndPoint.Add(new IPEndPoint(ip, int.Parse(mt.Groups[2].Value)));
                            continue;
                        }
                    }
                }
                if(item.Url[0] == 'h' && isHttpScheme(item.Url))
                {
                    //start with h
                    ret.urlWithHttps.Add(item.Url);
                    continue;
                }
                if (item.Url.Contains('/'))
                {
                    ret.url.Add(item.Url);
                    continue;
                }
                if(item.Url.Contains(':'))
                    ret.pureDomainWithPort.Add(item.Url);
                else
                    ret.pureDomain.Add(item.Url);
            }
            return ret;

            bool isHttpScheme(string url)
            {
                if (url[0] == 'h' && url[1] == 't' && url[2] == 't' && url[3] == 'p' && url[4] == 's' && url[5] == ':')
                    return true;
                if (url[0] == 'h' && url[1] == 't' && url[2] == 't' && url[3] == 'p' && url[4] == ':')
                    return true;
                return false;
            }
        }

        ReduceObject Reduce(MapObject mo)
        {
            var result = new ReduceObject();
            //mo.Save();
            Console.WriteLine("Merge");
            var domains = new ConcurrentDictionary<string, int>();
            var badwords = new ConcurrentDictionary<string, int>();
            mo.pureDomain.AsParallel().ForAll(d =>
            {
                domains.AddOrUpdate(d, 1, IncBy1);
            });
            mo.url.AsParallel().ForAll(url =>
            {
                var fidx = url.IndexOf('/');
                var domain = url.Substring(0, fidx);
                var query = url.Substring(fidx + 1, url.Length - fidx - 1);
                if (query.Any(c => c > 127))
                {
                    var r = Tokeniz(query);
                    foreach (var t in r.Where(c => c.type == PanGu.WordType.SimplifiedChinese || c.type == PanGu.WordType.TraditionalChinese))
                        badwords.AddOrUpdate(t.token, 1, IncBy1);
                }
                else
                {
                    domains.AddOrUpdate(domain, 1, IncBy1);
                }
            });
            result.BadWords = badwords.OrderByDescending(c=>c.Value).ToArray();

            var final = new ConcurrentDictionary<string, int>();
            var uc = new ConcurrentDictionary<string, int>();
            domains.AsParallel().ForAll(c =>
            {
                if (c.Value == 1)
                {
                    var spl = c.Key.Split('.');
                    if (spl.Length > 2)
                        uc.AddOrUpdate(c.Key.Substring(spl[0].Length, c.Key.Length - spl[0].Length), 1, IncBy1);
                }
            });
            domains.AsParallel().ForAll(c =>
            {
                if (c.Key.EndsWith(".cn")) return;
                if (c.Value > 1)
                {
                    final.AddOrUpdate(c.Key.TrimStart('w','W'), c.Value, (s, b) => b + c.Value);
                }
                else
                {
                    var spl = c.Key.Split('.');
                    var pad = c.Key.Substring(spl[0].Length, c.Key.Length - spl[0].Length);
                    if (spl.Length == 2)
                        final.AddOrUpdate(c.Key, c.Value, (s, b) => b + c.Value);
                    else if (uc.TryGetValue(pad, out int value) && value > 30)
                        final.AddOrUpdate(pad, value, IncBy1);
                    else
                        final.AddOrUpdate(c.Key.TrimStart('w'), c.Value, (s, b) => b + c.Value);
                }
            });
            result.Result = final.ToArray();
            return result;

            int IncBy1(string inputString, int inc) => inc + 1;
        }

        (string badWords,string resultContent) Finalize(ReduceObject ro)
        {
            var BlackWords = string.Join("\n", ro.BadWords.OrderByDescending(c => c.Value).Select(c => $"词语：{c.Key}；词频：{c.Value}"));
            var resultContent = string.Format(fuck,
                DateTime.Now.ToString("R"),
                string.Join("\n", ro.Result.Select(c => c.Key[0] == '.' ? $"||{c.Key.Substring(1, c.Key.Length - 1)}" : c.Key)));
            resultContent = resultContent.Replace("\r", "").Replace("[AutoProxy]", $"[AutoProxy]\n! Checksum: {CalcSum(resultContent)}");
            return (badWords: BlackWords, resultContent: resultContent);
        }

        bool CheckSum(string content)
        {
            var curr = new Regex(@"Checksum:(.*?)\n");
            if (curr.IsMatch(content))
            {
                var current = curr.Match(content).Groups[1].Value.Trim();
                return current == CalcSum(content);
            }
            return false;
        }

        string CalcSum(string content)
        {
            var clean = Regex.Replace(content, @"! Checksum:.*\n", "").Replace("\r", "").Replace("\n\n", "\n");
            //var clean = content.Replace("\r", "").Replace("\n\n", "\n");
            using (var sha1 = System.Security.Cryptography.MD5.Create())
                return Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(clean))).TrimEnd('=');
        }
        #endregion

        #region TakePages

        private async Task<MObject[]> TakeMO()
        {
            if (!Directory.Exists("local")) Directory.CreateDirectory("local");
            var dt = DateTime.Today.ToString("yyyyMMdd");
            var ldt = $"local\\{dt}";
            if (!Directory.Exists(ldt) && File.Exists($"{dt}.zip"))
                ZipFile.ExtractToDirectory($"{dt}.zip", "local");
            if (!Directory.Exists(ldt))
            {
                dt = DateTime.Today.AddDays(-1).ToString("yyyyMMdd");
                ldt = $"local\\{dt}";
                if (!Directory.Exists(ldt) && File.Exists($"{dt}.zip"))
                    ZipFile.ExtractToDirectory($"{dt}.zip", "local");
            }
            if (!Directory.Exists(ldt))
                await TakePages(ldt);
            var files = Directory.GetFiles(ldt, "*.txt", SearchOption.TopDirectoryOnly);
            var lst = new ConcurrentBag<MObject>();
            files.AsParallel().ForAll(fileName =>
            {
                //Console.WriteLine("Working on page {0}", fileName);
                var content = File.ReadAllText(fileName);
                var items = ParsePage(content).Where(c => c.Percent == 100);
                foreach (var mObject in items)
                    lst.Add(mObject);
            });
            return lst.ToArray();
        }


        const string fucker = @"https://en.greatfire.org/search/blocked?page=";
        readonly Regex rgxPage = new Regex(@"href=""/search/blocked\?page=(\d+)""\>last ?");
        private async Task TakePages(string dt)
        {
            if (!Directory.Exists(dt)) Directory.CreateDirectory(dt);
            int errCount = 0;
            using (var hc = new System.Net.Http.HttpClient())
            {
                var firstpage = await hc.GetStringAsync(fucker + 0);
                var mch = rgxPage.Match(firstpage);
                if (mch.Success)
                {
                    int lastPage = int.Parse(mch.Groups[1].Value);
                    if (!File.Exists($"{dt}/0.txt")) File.WriteAllText($"{dt}/0.txt", firstpage);
                    Console.WriteLine($"LastPage is {lastPage}");
                    int index = 1;
                    while (index <= lastPage && errCount < 5)
                    {
                        try
                        {
                            var file = $"{dt}/{index}.txt";
                            if (!File.Exists(file))
                            {
                                Console.WriteLine("FetchingPage:" + index);
                                File.WriteAllBytes(file, await hc.GetByteArrayAsync(fucker + index));
                            }
                            index++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error:" + ex.Message);
                            errCount++;
                            System.Threading.Thread.Sleep(60000);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Cannot parse page");
                }
            }
            ZipFile.CreateFromDirectory(dt, dt + ".zip", CompressionLevel.Fastest, true);
            Console.WriteLine("TakePages finished!");
        }

        #endregion

        #region ParsePage
        private static IEnumerable<MObject> ParsePage(string content)
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

        #endregion

        #region Tokeniz
        public static Token[] Tokeniz(string input)
        {
            var segment = new PanGu.Segment();
            var words = segment.DoSegment(input);
            return words.Select(c => new Token { token = c.Word, end_offset = c.GetEndPositon(), position = c.Position, type = c.WordType }).ToArray();
        }
        #endregion

        #region Template
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

!##############Greatfire List Start#################
{1}
!##############Greatfire List End#################

!################White List Start################
@@||paypal.com
@@||aliyun.com
@@||baidu.com
@@||bing.com
@@||bt.byr.cn
@@||chinaso.com
@@|http://nrch.culture.tw/
@@||haosou.com
@@||jd.com
@@||jike.com
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
@@||dl.google.com
@@||kh.google.com
@@||fonts.googleapis.com
@@||cn.gravatar.com
@@||csi.gstatic.com
@@||fonts.gstatic.com
@@|http://translate.google.cn
@@|http://www.google.cn/maps
!################White List End##################
";

        #endregion
    }
}
