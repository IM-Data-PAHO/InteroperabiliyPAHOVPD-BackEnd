using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Domain
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string Connection { get; set; }
        public string KeyApp { get; set; }
        public string KeyDataBase { get; set; }
        public string PayWay { get; set; }
        public string PayPSE { get; set; }
        public string SecretMiddleware { get; set; }
        public int ResponseResult { get; set; }
        public int ErrorResult { get; set; }
    }

    public class TokenManagement
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int AccessExpiration { get; set; }
        public int RefreshExpiration { get; set; }
    }

    public class Logger
    {
        public string Default { get; set; }
        public List<string> Segments { set; get; }
    }

    public class RepositorySettings
    {
        public List<PathImages> Pictures { get; set; }
    }

    public class PathImages
    {
        public string Name { get; set; }
        public string File { get; set; }
        public string Path { get; set; }
    }

    public class Integration
    {
        public List<ServicesInt> Services { get; set; }
    }

    public class ServicesInt
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public Auth Authentication { get; set; }
        public List<Methods> Methods { get; set; }
    }
    public class Auth
    {
        public string Token { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string UrlLogin { get; set; }
    }

    public class Methods
    {
        public string Method { get; set; }
        public string Value { get; set; }
    }

    public class EmailSettings
    {
        public string Server { get; set; }
        public string From { get; set; }
        public string Pass { get; set; }
        public int Port { get; set; }
    }

    public class Template
    {
        public string Path { get; set; }
        public List<Pages> Pages { get; set; }
    }

    public class Pages
    {
        public string Name { get; set; }
        public string Template { get; set; }
    }

    public class EmailConfig
    {
        public EmailSettings EmailSettings { get; set; }
        public Template TemplateSettings { get; set; }
    }

    public class GlobalSettings
    {
        public bool Decrypt { set; get; }
        public AppSettings AppSettings { set; get; }
        public List<Auth> UsersIntegration { set; get; }
    }

    public class CallSettings
    {
        public string NumberCall { get; set; }
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
    }

    public class ImportSettings
    {
        public List<ImportInt> Services { get; set; }
    }

    public class ImportInt
    {
        public bool Async { get; set; }
        public int SizeUpload { get; set; }
        public bool Individual { get; set; }
        public bool Block { get; set; }
    }
}
