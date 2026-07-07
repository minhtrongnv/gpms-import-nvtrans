using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nvtrans
{
    public class ApiHelper
    {
        private readonly HttpClient _client;

        public ApiHelper()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.UseCookies = false;

            _client = new HttpClient(handler);
            _client.Timeout = TimeSpan.FromMinutes(5);

            _client.DefaultRequestHeaders.Add("User-Agent", "NvtransImportJob/1.0");
            _client.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            _client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            string cookie = ConfigurationManager.AppSettings["AspCookie"];

            if (!string.IsNullOrWhiteSpace(cookie))
            {
                _client.DefaultRequestHeaders.Add("Cookie", cookie);
            }
        }

        public async Task<string> PostFormAsync(string url, Dictionary<string, string> formData)
        {
            using (FormUrlEncodedContent content = new FormUrlEncodedContent(formData))
            {
                HttpResponseMessage response = await _client.PostAsync(url, content);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "API failed. Status: " + (int)response.StatusCode +
                        Environment.NewLine +
                        responseText
                    );
                }

                return responseText;
            }
        }
    }
}