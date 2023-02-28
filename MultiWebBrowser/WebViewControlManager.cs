using Microsoft.Web.WebView2.Core;
using MultiWebBrowser.Customs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmlDataTable;

namespace MultiWebBrowser
{
    public static class WebViewControlManager
    {
        private class UrlCookie
        {
            public string key { get; set; }
            public string Url { get; set; }
            public string Cookie { get; set; }
        }
        public class WebViewData
        {
            public string key { get; set; }
            public string Url { get; set; }
        }
        private static XmlDataTableEntitys DataTable = new XmlDataTableEntitys("./Data/");
        private static XmlDataTableFile Cookies { get => DataTable.GetCreateTable("Cookies", typeof(UrlCookie)); }
        private static XmlDataTableFile WebViews { get => DataTable.GetCreateTable("WebViews", new DataColumn("key", typeof(string)), new DataColumn("Url", typeof(string))); }

        static WebViewControlManager()
        {
            
        }
        public static WebViewData[] QueryWebViews()
        {
            return WebViews.QueryToDynamic(q => true).Select(s=>new WebViewData() { key=s.key,Url=s.Url}).ToArray();
        }
        public static WebViewData? AddWebView(string key,string url)
        {
            //if (WebViews.Query(q => q.Columns[0] == key).Any()) return null;
            var q = WebViews.Query(q => q.Columns[0]+"" == key);
            if (q.Any()) return null;
            var data = new WebViewData()
            {
                key = key,
                Url = url
            };
            var b =WebViews.Insert(key,url);
            return b?data:null;
        }
        public static bool DeleteWebView(String key)
        {
            var q = WebViews.Query(q => q.Columns[0] + "" == key);
            if (!q.Any()) return false;
            if (!WebViews.Delete(q.First().Line.ID)) return false;
            var q1=Cookies.Query(q => q.Columns[0] + "" == key);
            if (q1.Any())
            {
                foreach (var i in q1)
                {
                    Cookies.Delete(i.Line.ID);
                }
            }
            return true;
        }
        public static bool SaveWebView(string key,string url)
        {
            var datas = WebViews.Query(q => true);
            var q = WebViews.Query(q => q.Columns[0]+"" == key);
            if (q.Any())
            {
                return WebViews.Update(q[0].Line.ID, key, url);
            }
            else
            {
                return AddWebView(key, url) != null;
            }

        }
        public static async void SaveCookie(WebViewControl c, string key, String? url = null)
        {
            var d = Cookies.Query(q => q.Columns[0]+"" == key && q.Columns[1] + "" == url);
            url = url ?? c.Source.ToString();
            var cs = await c.CookieManager.GetCookiesAsync(url);
            if (d.Any())
            {
                Cookies.Update(d[0].Line.ID, key, url, CookieToStringData(cs));
                return;
            }
            Cookies.Insert(key, url, CookieToStringData(cs));
        }
        private static String CookieToStringData(CoreWebView2Cookie cookie)
        {
            return cookie.Name + ',' + cookie.Value + ',' + cookie.Domain + ',' + cookie.Path;
        }
        private static String CookieToStringData(IEnumerable<CoreWebView2Cookie> cookie)
        {
            List<String> ls = new List<string>();
            ls.AddRange(cookie.Select(s => CookieToStringData(s)));
            return JsonConvert.SerializeObject(ls);
        }
        private static CoreWebView2Cookie[] CookieStringToCookie(CoreWebView2CookieManager Manager,String CookieData)
        {
            return JsonConvert.DeserializeObject<string[]>(CookieData).Select(d1=> jtj(d1.Split(','))).ToArray();
            CoreWebView2Cookie jtj(string[] d)
            {
                try
                {
                    return Manager.CreateCookie(d[0], d[1], d[2], d[3]);
                }
                catch
                {
                    return Manager.CreateCookie("NULL", "NULL", d[2], d[3]);
                }
            }
        }
        private static UrlCookie[]? QueryCookies(string key,string url)
        {
            var d=Cookies.Query(q => q.Columns[0] + "" == key&&q.Columns[1] + "" == url);
            return d.Length==0 ? null : d.Select(s=>new UrlCookie() { key=d.First().GetString("key"),Cookie= d[0].GetString("Cookie"),Url=d.First().GetString("Url") }).ToArray();
        }
        public static CoreWebView2Cookie[]? QueryWebCookies(CoreWebView2CookieManager manager,string key,string url)
        {
            var cs = QueryCookies(key,url);
            if (cs == null) return null;
            //var d =JsonConvert.DeserializeObject<CoreWebView2Cookie[]>(cs.First().Cookie);
            var d = CookieStringToCookie(manager, cs[0].Cookie);
            Array.Clear(cs);
            return d;
        }
        public static bool DeleteCookies(string key,string url)
        {
            var d=Cookies.Query(q => q.Columns[0] + "" == key&&q.Columns[1] + "" == url);
            return d.Length == 0 ? false : Cookies.Remove(d.First().Line.ID);
        }
    }
}
