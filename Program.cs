using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DomainFinder {
    class Program {
        static string BaseUrl = "https://checkapi.aliyun.com/check/checkdomain?&callback=&domain={0}.com&token={1}&_={2}";
        static string BaseTokenUrl = "https://ynuf.alipay.com/service/um.json?xv=0.8.1&xt={0}&xa=check-web-hichina-com23";
        static DateTime BaseTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static string Vchar = "abcdefghijklmnopqrstuvwxyz";
        static string Token;

        static void Main(string[] args) {
            SaveToMongo();
        }

        static void Query() {
            var v = Config.MongoDbConn;
            Token = GenerateTokenAsync().Result;
            Console.WriteLine(Token);

            var index = 1;
            var names = Page(1, 1000);
            do {
                if (null != names && names.Count > 0) {
                    Parallel.ForEach(names, new ParallelOptions() {
                        MaxDegreeOfParallelism = 1
                    },
                    (I) => {
                        Next(I);
                    });
                }

                names = Page(1, 1000);
                Console.WriteLine($"{DateTime.Now}:{index++}:{names.Count}");
                Thread.Sleep(1000);
            } while (true);
        }

        static void SaveToMongo(int length = 6) {
            var db = MongoDbHelper.Instance.GetDb("TEST");
            var col = db.GetCollection<BsonDocument>("domain");
            foreach (var name in Get(Vchar, length)) {
                Console.WriteLine(name);

                var filterDef = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", name)
                );
                var updateDef = Builders<BsonDocument>.Update.
                    Set("Length", name.Length);

                var upOptions = new UpdateOptions() {
                    IsUpsert = true
                };

                col.UpdateOne(filterDef, updateDef, upOptions);
            }
        }

        static List<string> Page(int pageIndex, int pageSize) {
            //var names = Get(Vchar, 5).OrderBy(I => I, StringComparer.CurrentCulture);

            var db = MongoDbHelper.Instance.GetDb("TEST");
            var col = db.GetCollection<BsonDocument>("domain");

            var filterDef = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Exists("COM", false),
                new BsonDocument("$where", "this._id !=null && this._id.length == 4")
            );

            var res = col.Find(filterDef).
                Skip((pageIndex - 1) * pageSize).
                Limit(pageSize).
                ToList().
                Select(I => I["_id"].ToString()).ToList();

            return res;
        }

        static void Next(string name) {
            try {
                bool newToken = false;
                bool valid = false;
                do {
                    var timestamp = (long)DateTime.UtcNow.Subtract(BaseTimestamp).TotalMilliseconds;
                    var url = string.Format(BaseUrl, name, Token, timestamp);
                    valid = IsValid(url, out newToken);

                    if (newToken) {
                        Token = GenerateTokenAsync().Result;
                        Console.WriteLine(Token);
                        timestamp = (long)DateTime.UtcNow.Subtract(BaseTimestamp).TotalMilliseconds;
                    }
                } while (newToken);

                var db = MongoDbHelper.Instance.GetDb("TEST");
                var col = db.GetCollection<BsonDocument>("domain");

                var filterDef = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", name)
                );
                var updateDef = Builders<BsonDocument>.Update.
                    Set("COM", new BsonDocument {
                    { "Valid" , valid }
                    });

                col.UpdateOne(filterDef, updateDef);

            } catch (Exception) {
            }
        }

        // {"errorCode":213,"errorMsg":"Timeout","success":"false"}
        // {"module":[{"name":"abc.com","avail":0,"tld":"com"}],"errorCode":0,"success":"true"}
        // {"module":[{"name":"abc123423412341432.com","avail":1,"tld":"com"}],"errorCode":0,"success":"true"}
        static bool IsValid(string url, out bool newToken) {
            newToken = false;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Headers["Host"] = "checkapi.aliyun.com";
            req.Headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            req.Headers["User-Agent"] = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:52.0) Gecko/20100101 Firefox/52.0";
            using (var res = req.GetResponseAsync().Result) {
                using (var sReader = new StreamReader(res.GetResponseStream())) {
                    var resStr = sReader.ReadToEnd();
                    JObject obj = JObject.Parse(resStr);
                    if (obj["success"].ToString() == "true") {
                        var jsonArray = obj["module"].ToString();
                        var resArray = JArray.Parse(jsonArray);
                        if (resArray.Count > 0) {
                            var resObj = resArray[0];
                            if (resObj["avail"].ToString() == "1") {
                                return true;
                            }
                        }
                    } else {
                        newToken = true;
                    }
                }
            }

            return false;
        }

        static void GeNames() {
            //Dictionary<string, string> all = new Dictionary<string, string>();
            List<string> all = new List<string>();
            foreach (var item in Get(Vchar, 3)) {
                //all[item] = item;
                all.Add(item);
            }

            var str = all.
                //Keys.ToList().
                OrderBy(I => I, StringComparer.OrdinalIgnoreCase).
                OrderBy(I => I.Length).
                Aggregate(new StringBuilder(), (sb, i) => sb.AppendLine(i)).ToString();
            File.WriteAllText("D:\\all.txt", str);

            Thread.CurrentThread.Join();
        }

        static IEnumerable<string> Get(string from, int take) {
            if (take-- > 0) {
                for (int i = 0; i < from.Length; i++) {
                    foreach (var cur in Get(from, take)) {
                        if (cur.Contains(from[i])) continue;
                        yield return from[i] + cur;
                    }

                    yield return from[i].ToString();
                }
            }
        }

        static async Task<string> GenerateTokenAsync() {
            var data = new string[]{"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
                     , "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"};

            List<string> result = new List<string>();
            var length = data.Length;
            for (var i = 0; i < 32; i++) {
                var random = new Random((Guid.NewGuid().GetHashCode()));
                var randomNum = random.Next(0, 31);
                result.Add(data[randomNum]);
            }
            var token = result.Aggregate((i, j) => i + "" + j);

            token = $"check-web-hichina-com:{token}";
            var url = string.Format(BaseTokenUrl, token, "check-web-hichina-com");
            Console.WriteLine(url);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Headers["User-Agent"] = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36";
            req.Headers["Host"] = "ynuf.alipay.com";
            using (var res = await req.GetResponseAsync()) {

            }

            return token;
        }
    }
}
