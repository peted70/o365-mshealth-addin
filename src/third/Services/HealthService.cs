using Microsoft.Framework.OptionsModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace third.Services
{
    public class HealthAuthOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public interface IHealthService
    {
        string AuthCode { get; set; }
        string AccessToken { get; set; }
        string RefreshToken { get; set; }

        Uri CreateAuthCodeRequestUri(Uri RedirectUri);
        Task<string> GetAccessTokenAsync(Uri RedirectUri);
        Task<Profile> GetProfileAsync();
        Task<Summaries> GetSummaryAsync();
    }

    public class CaloriesBurnedSummary
    {
        public string period { get; set; }
        public int? totalCalories { get; set; }
    }

    public class HeartRateSummary
    {
        public string period { get; set; }
        public int? averageHeartRate { get; set; }
    }

    public class DistanceSummary
    {
        public string period { get; set; }
        public int? totalDistance { get; set; }
    }

    public class Summary
    {
        public string userId { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string period { get; set; }
        public string duration { get; set; }
        public int stepsTaken { get; set; }
        public CaloriesBurnedSummary caloriesBurnedSummary { get; set; }
        public HeartRateSummary heartRateSummary { get; set; }
        public DistanceSummary distanceSummary { get; set; }
    }

    public class Summaries
    {
        public List<Summary> summaries { get; set; }
        public int itemCount { get; set; }
    }
    public class Profile
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime BirthDay { get; set; }

        public string Gender { get; set; }

        public int Height { get; set; }
        public int Weight { get; set; }
    }

    public class HealthService : IHealthService
    {
        public HealthService(IOptions<HealthAuthOptions> optionsAccessor)
        {
            _optionsAccessor = optionsAccessor;
        }

        IOptions<HealthAuthOptions> _optionsAccessor;

        private const string ApiVersion = "v1";

        public string AuthCode { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public Uri CreateAuthCodeRequestUri(Uri RedirectUri)
        {
            // This constructs a uri which requests an auth code from the windows live service
            // https://login.live.com/oauth20_authorize.srf?client_id={client_id} 
            // &scope={scope}&response_type=code&redirect_uri={redirect_uri} 
            var uri = new UriBuilder("https", "login.live.com");
            uri.Path = "oauth20_authorize.srf";
            var ClientId = _optionsAccessor.Options.ClientId;
            var Scope = "mshealth.ReadProfile mshealth.ReadActivityHistory mshealth.ReadDevices mshealth.ReadActivityLocation offline_access";

            var Redirect = RedirectUri.ToString();

            Redirect = WebUtility.UrlEncode(Redirect);
            Scope = WebUtility.UrlEncode(Scope);
            ClientId = WebUtility.UrlEncode(ClientId);

            uri.Query = $"client_id={ClientId}&scope={Scope}&response_type=code&redirect_uri={Redirect}";

            return uri.Uri;
        }

        public async Task<string> GetAccessTokenAsync(Uri RedirectUri)
        {
            var ClientSecret = _optionsAccessor.Options.ClientSecret;
            var ClientId = _optionsAccessor.Options.ClientId;

            HttpClient client = new HttpClient();
            var formparams = new Dictionary<string, string>();
            formparams["client_id"] = ClientId;
            formparams["client_secret"] = ClientSecret;
            formparams["code"] = AuthCode;
            formparams["grant_type"] = "authorization_code";
            formparams["redirect_uri"] = RedirectUri.ToString();
            HttpContent content = new FormUrlEncodedContent(formparams);

            var urib = new UriBuilder();
            urib.Scheme = "https";
            urib.Host = "login.live.com";
            urib.Path = "oauth20_token.srf";

            var response = await client.PostAsync(urib.Uri, content);
            var retStr = await response.Content.ReadAsStringAsync();

            dynamic jsonObj = JsonConvert.DeserializeObject(retStr);
            AccessToken = jsonObj.access_token;
            RefreshToken = jsonObj.refresh_token;

            return AccessToken;
        }

        private async Task<string> MakeRequestAsync(string path, string query = "")
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            var ub = new UriBuilder("https://api.microsofthealth.net");

            ub.Path = ApiVersion + "/" + path;
            ub.Query = query;

            string resStr = string.Empty;
            var resp = await http.GetAsync(ub.Uri);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                // If we are unauthorized here assume that our token may have expired and use the  
                // refresh token to get a new one and then try the request again.. 

                // TODO:
                // Re-issue the same request (will use new auth token now) 
                return await MakeRequestAsync(path, query);
            }

            if (resp.IsSuccessStatusCode)
            {
                resStr = await resp.Content.ReadAsStringAsync();
            }
            return resStr;
        }

        public async Task<Profile> GetProfileAsync()
        {
            var res = await MakeRequestAsync("me/profile");
            dynamic obj = JsonConvert.DeserializeObject(res);
            var profile = new Profile()
            {
                FirstName = obj.firstName,
                LastName = obj.lastName,
                Gender = obj.gender,
                Height = obj.height,
                Weight = obj.weight,
                //BirthDay = DateTime.Parse(obj.birthdate),
            };
            return profile;
        }

        public async Task<Summaries> GetSummaryAsync()
        {
            // ideally pick this time range from UI
            var startTime = DateTime.Now.AddDays(-3).ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            var res = await MakeRequestAsync("me/summaries/Daily", $"startTime={startTime}");

            // Format the JSON string 
            var obj = (Summaries)JsonConvert.DeserializeObject(res, typeof(Summaries));
            return obj;
        }
    }
}
