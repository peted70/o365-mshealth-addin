using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Hosting;
using System;
using System.Diagnostics;
using third.Services;

namespace third.Controllers
{
    public class HomeController : Controller
    {
        private IHttpContextAccessor _contextAccessor;

        public IHealthService HealthService { get; set; }

        public HomeController(IHttpContextAccessor contextAccessor, IHealthService healthService)
        {
            _contextAccessor = contextAccessor;
            HealthService = healthService;
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HealthService.AuthCode))
            {
                var ub = new UriBuilder(_contextAccessor.HttpContext.Request.Scheme + "://" +
                    _contextAccessor.HttpContext.Request.Host.Value + "/" + "Home/Token");

                var uri = HealthService.CreateAuthCodeRequestUri(ub.Uri);
                return Redirect(uri.ToString());
            }

            // If we get here we should be good to call on the Health APIs
            var profile = await HealthService.GetProfileAsync();

            return View(profile);
        }

        public async Task<IActionResult> Token(string code)
        {
            Debug.WriteLine($"hello world {code}");
            if (!string.IsNullOrEmpty(code) && string.IsNullOrEmpty(HealthService.AccessToken))
            {
                HealthService.AuthCode = code;

                var ub = new UriBuilder(_contextAccessor.HttpContext.Request.Scheme + "://" +
                    _contextAccessor.HttpContext.Request.Host.Value + "/" + "Home/Token");

                string at = await HealthService.GetAccessTokenAsync(ub.Uri);

                return Redirect("Index");
            }
            
            return Redirect("Index");
        }

        public async Task<IActionResult> HealthSummary()
        {
            var summary = await HealthService.GetSummaryAsync();
            return Json(summary);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";
            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
