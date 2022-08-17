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

        private static ImportSettings _importSettings
        {
            get
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                    .Build();

                return new ImportSettings
                {
                    Services = configuration.GetSection("ImportSettings:setting").GetChildren().Select(s => new ImportInt
                    {
                        Async = Convert.ToBoolean(s.GetSection("async").Value),
                        SizeUpload = Convert.ToInt32(s.GetSection("sizeUpload").Value),
                        Individual = Convert.ToBoolean(s.GetSection("individual").Value),
                        Block = Convert.ToBoolean(s.GetSection("individual").Value)
                    }).ToList()

                };
            }
        }

        public async static Task<string> CallMethodSave(string service, string method, string content, string token, string strategy ="")
        {
            try
            {
                Console.Write(" Inicio CallMethodSave ");
                var _set = _integration;
                var _imp = _importSettings;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                _method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
                string URL = string.Format($"{_service.URL}{_method}" );
                if (strategy.Length > 0)
                    URL = URL + strategy;
                else 
                    URL = URL + "?async=" + _imp.Services[0].Async + "&strategy=CREATE_AND_UPDATE";
                  
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
                            Console.Write(" Error CallMethodSave: 404 " + e.Message.ToString());
                            return resp.ToString();
                        }
                        Console.Write(" Fin CallMethodSave: " + result.ToString());
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(" Error CallMethodSave: " + ex.Message.ToString());
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
        public async static Task<string> CallMethod(string service, string method, string token, string reference)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                _method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
                string URL = string.Format($"{_service.URL}{_method}{reference}");
                
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

        public async static Task<string> CallMethodTask(string service, string program, string task, string token)
        {
            try
            {
                Console.Write(" Inicio CallMethodTask");
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                //var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                //_method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
                string URL = string.Format($"{_service.URL}{task}");
                Console.Write(" URL CallMethodTask" + URL.ToString());
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
                        Console.Write(" Response CallMethodTask" + result.ToString());
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(" Error CallMethodTask" + ex.Message.ToString());
                throw ex;
            }
        }
        public async static Task<string> CallMethodSummary(string service, string program, string uid, string category, string token)
        {
            try
            {
                Console.Write(" Inicio CallMethodSummary");
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();      
               
                string URL = string.Format($"{_service.URL}/system/taskSummaries/{category}/{uid}");

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
                        Console.Write(" Result CallMethodSummary; " + result.ToString());
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(" Error CallMethodSummary; " + ex.Message.ToString());
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
        public async static Task<string> CallMethodClear(string service, string method, string oupath,string program, string startDate, string endDate, string token)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                _method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
                string URL = string.Format($"{_service.URL}{_method}?orgUnit={oupath}&program={program}&ouMode=DESCENDANTS&startDate={startDate}&endDate={endDate}&skipPaging=true");
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

        public async static Task<string> CallMethodClearEnrollments(string service, string method, string oupath, string program, string startDate, string endDate, string token)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                var _method = _service.Methods.Where(m => m.Method.Equals(method)).FirstOrDefault().Value;
                _method = !string.IsNullOrEmpty(_method) ? string.Format($"/{_method}") : null;
               
                string URL = string.Format($"{_service.URL}{_method}{oupath}&ouMode=DESCENDANTS&program={program}&programStartDate={startDate}&programEndDate={endDate}&paging=false");
                
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

        public async static Task<string> CallMethodGetUserSetting(string service,  string token)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                string URL = string.Format($"{_service.URL}/me?fields=settings[keyUiLocale],id,name,displayName,surname,firstName,email,userCredentials[userRoles[id,name]],path&paging=false");
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

        public async static Task<string> CallMethodOUCountry(string service, string method, string token)
        {
            try
            {
                var _set = _integration;
                var _service = _set.Services.Where(s => s.Name.Equals(service)).ToList().FirstOrDefault();
                string URL = string.Format($"{_service.URL}/{method}?fields=id,name,code,path&paging=false");

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

    }
}
