using Impodatos.Domain;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Common.Security
{
    public class RequestHttp
    {
        private static Integration _integration
        {
            get
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                    .Build();
           
                return new Integration
                {
                    Services = configuration.GetSection("integration:services").GetChildren().Select(s => new ServicesInt
                    {
                        Name = s.GetSection("name").Value,
                        URL = s.GetSection("url").Value,
                        Authentication = new Auth
                        {                         
                            User = s.GetSection("authentication:user").Value != null ?
                                s.GetSection("authentication:user").Value : null,
                            Pass = s.GetSection("authentication:pass").Value != null ?
                             s.GetSection("authentication:pass").Value : null,
                            UrlLogin = s.GetSection("authentication:url").Value != null ?
                             s.GetSection("authentication:url").Value : null
                        },
                        Methods = s.GetSection("methods").GetChildren().Select(m => new Methods
                        {
                            Method = m.GetSection("method").Value,
                            Value = m.GetSection("value").Value
                        }).ToList()
                    }).ToList()
                };
            }
        }

        public async static Task<string> CallMethod(string service, string method, string content, string token, string strategy ="")
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                _method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
                string URL = string.Format($"{_service.URL}{_method}");
                if (strategy.Length > 0)
                    URL = URL + strategy;

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Post, URL))
                using (var stringContent = new StringContent(content, Encoding.UTF8, "application/json"))
                {
                    if (_service.Authentication.User != null)
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Headers.Add("Accept", "application/json");
                    request.Content = stringContent;
                    client.Timeout = TimeSpan.FromMinutes(15);
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        var result = "";
                        try
                        {
                            response.EnsureSuccessStatusCode();
                            result = await response.Content.ReadAsStringAsync();
                        }
                        catch (Exception e) {
                            ResponseDto resp = new ResponseDto();
                            resp.Status = "404";
                            return resp.ToString(); }
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async static Task<string> CallMethod(string service, string method, string token)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                _method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
                string URL = string.Format($"{_service.URL}{_method}");

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, URL))
                {
                    if (_service.Authentication.User != null)
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Headers.Add("Accept", "application/json"); 
                    using (var response = await client
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var result = await response.Content.ReadAsStringAsync();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async static Task<string> CallGetMethod(string service, string method, string content, string ou,string token)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                _method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
                string URL = string.Format($"{_service.URL}{_method}{content}");
                if (method == "validatetrak")
                    URL = string.Format($"{_service.URL}{_method}{content}&ou={ou}");
                if (method == "validateenroll")
                    URL = string.Format($"{_service.URL}{_method}{ou}&trackedEntityInstance={content}");

            using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, URL))
                {                    
                    if (_service.Authentication.User != null)
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Headers.Add("Accept", "application/json");
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var result = await response.Content.ReadAsStringAsync();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async static Task<string> CallMethodClear(string service, string method, string oupath, string startDate, string endDate, string token)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                _method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
                string URL = string.Format($"{_service.URL}{_method}?orgUnit={oupath}&ouMode=DESCENDANTS&startDate={startDate}&endDate={endDate}&skipPaging=true");
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, URL))
                {
                    if (_service.Authentication.User != null)
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Headers.Add("Accept", "application/json");
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var result = await response.Content.ReadAsStringAsync();
                        return result;
                    }
                }
                    
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async static Task<string> CallMethodLogin(string service, string method, string user, string pass)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                string URL = _service.Authentication.UrlLogin;
                var client = new RestClient(URL);
                var request = new RestRequest(_method, Method.Post);
                request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_service.Authentication.User}:{_service.Authentication.Pass}")));
                request.AlwaysMultipartFormData = true;
                request.AddParameter("grant_type", "password");
                request.AddParameter("username", user );
                request.AddParameter("password", pass);
                var response = await client.ExecuteAsync(request);
                return JsonConvert.SerializeObject(response.Content);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
