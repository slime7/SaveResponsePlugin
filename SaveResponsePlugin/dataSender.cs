using System;
using System.IO;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Grabacr07.KanColleWrapper;
using Nekoxy;
using Codeplex.Data;

namespace SaveResponsePlugin
{
    internal sealed class dataSender : IDisposable
    {
        CompositeDisposable Disposable { get; } = new CompositeDisposable();

        public dataSender(string sessionId)
        {
            try
            {
                Disposable.Add(KanColleClient.Current.Proxy.ApiSessionSource
                    .Subscribe(OnSession));
            }
            catch (Exception e)
            {

                File.AppendAllText(System.Environment.CurrentDirectory + "/plugins/SaveResponsePlugin.log",
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss : ") + e.Message + "\n");
                return;
            }
        }

        async void OnSession(Session session)
        {
            string CONFIG_FILE = System.Environment.CurrentDirectory + "/plugins/SaveResponsePlugin.config.json";
            try
            {
                string configContent;
                if (File.Exists(CONFIG_FILE))
                {
                    configContent = File.ReadAllText(CONFIG_FILE);
                }
                else
                {
                    var defaultConfig = new
                    {
                        url = "",
                        u = "",
                        kcsapifilter = new[] {
                        "/kcsapi/api_port/port",
                        "/kcsapi/api_req_hensei/change",
                        "/kcsapi/api_get_member/material",
                        "/kcsapi/api_get_member/require_info",
                        "/kcsapi/api_get_member/mapinfo",
                        "/kcsapi/api_req_map/start","/kcsapi/api_req_sortie/battleresult",
                        "/kcsapi/api_get_member/ship_deck",
                        "/kcsapi/api_req_map/next",
                        "/kcsapi/api_get_member/questlist",
                        "/kcsapi/api_get_member/deck",
                        "/kcsapi/api_req_kousyou/createitem",
                        "/kcsapi/api_req_kousyou/getship",
                        "/kcsapi/api_get_member/kdock",
                        "/kcsapi/api_req_kousyou/createship",
                        "/kcsapi/api_req_kousyou/remodel_slotlist",
                        "/kcsapi/api_req_kousyou/remodel_slotlist_detail",
                        "/kcsapi/api_req_kousyou/remodel_slot"
                      }
                    };
                    configContent = DynamicJson.Serialize(defaultConfig);
                    File.WriteAllText(CONFIG_FILE, configContent);
                }
                var config = DynamicJson.Parse(@configContent);

                string[] prefixes = config.kcsapifilter;
                var requestUri = session.Request.PathAndQuery.Split('?').First();
                bool shouldSend = config.url.Length > 0 && prefixes.Any(prefix => requestUri.StartsWith(prefix));
                string gamepost = Regex.Replace(session.Request.BodyAsString, "(api%5Ftoken=\\w{40}&)|(&api%5Ftoken=\\w{40})", "");
                string svdata = session.Response.BodyAsString;

                if (shouldSend)
                {
                    Uri url = new Uri(config.url);
                    await SendRequestDataAsync(url, gamepost, svdata, requestUri, config.u);
                }
            }
            catch (Exception e)
            {

                File.AppendAllText(System.Environment.CurrentDirectory + "/plugins/SaveResponsePlugin.log",
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss : ") + e.Message + "\n");
                return;
            }
        }

        async Task SendRequestDataAsync(Uri url, string gamepost, string svdata, string path, string u)
        {

            HttpContent postU = new StringContent(u);
            HttpContent postPath = new StringContent(path);
            HttpContent postSvdata = new StringContent(svdata);
            HttpContent postGamepost = new StringContent(gamepost);

            MultipartFormDataContent postContent = new MultipartFormDataContent();
            postContent.Add(postU, "u");
            postContent.Add(postPath, "path");
            postContent.Add(postSvdata, "svdata");
            postContent.Add(postGamepost, "gamepost");

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

            await client.PostAsync(url, postContent);
        }

        bool isDisposed_;
        public void Dispose()
        {
            if (isDisposed_) { return; }
            Disposable.Dispose();
            isDisposed_ = true;
            GC.SuppressFinalize(this);
        }
    }
}
