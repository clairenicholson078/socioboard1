﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Socioboard.Helpers;
using System.Net.Http;
using Socioboard.Extensions;
using Domain.Socioboard.ViewModels;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using System.Compat.Web;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using Facebook;
using Socioboard.Helper;
using System.Linq;
using Domain.Socioboard.Interfaces.Services;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Filters;



// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Socioboard.Controllers
{
    public class IndexController : Controller
    {

        private Helpers.AppSettings _appSettings;
        private readonly ILogger _logger;
        IEmailSender emailSender;

        public IndexController(Microsoft.Extensions.Options.IOptions<Helpers.AppSettings> settings, ILogger<IndexController> logger)
        {
            _appSettings = settings.Value;
            _logger = logger;

        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            Domain.Socioboard.Models.SessionHistory session = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.SessionHistory>("revokedata");
            if (session != null)
            {
                SortedDictionary<string, string> strdi = new SortedDictionary<string, string>();
                strdi.Add("systemId", session.systemId);
                string respo = CustomHttpWebRequest.HttpWebRequest("POST", "/api/User/checksociorevtoken", strdi, _appSettings.ApiDomain);
                if (respo != "false")
                {
                    if (user == null)
                    {
                        string EmailId = string.Empty;
                        string password = string.Empty;
                        if (Request.Cookies["socioboardemailId"] != null)
                        {
                            EmailId = Request.Cookies["socioboardemailId"].ToString();
                            EmailId = PluginHelper.Base64Decode(EmailId);
                        }
                        if (Request.Cookies["socioboardToken"] != null)
                        {
                            password = Request.Cookies["socioboardToken"].ToString();
                            password = PluginHelper.Base64Decode(password);
                        }
                        if (!string.IsNullOrEmpty(EmailId))
                        {
                            SortedDictionary<string, string> strdic = new SortedDictionary<string, string>();
                            strdic.Add("UserName", EmailId);
                            if (string.IsNullOrEmpty(password))
                            {
                                strdic.Add("Password", "sociallogin");
                            }
                            else
                            {
                                strdic.Add("Password", password);
                            }


                            string response = CustomHttpWebRequest.HttpWebRequest("POST", "/api/User/CheckUserLogin", strdic, _appSettings.ApiDomain);

                            if (!string.IsNullOrEmpty(response))
                            {
                                Domain.Socioboard.Models.User _user = Newtonsoft.Json.JsonConvert.DeserializeObject<Domain.Socioboard.Models.User>(response);
                                HttpContext.Session.SetObjectAsJson("User", _user);
                            }
                        }
                    }
                }
                else
                {
                    HttpContext.Session.Remove("User");
                    HttpContext.Session.Remove("selectedGroupId");
                    HttpContext.Session.Clear();
                    HttpContext.Session.Remove("revokedata");
                    user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
                }

            }
            else
            {
                string sociorevtoken = string.Empty;
                if (Request.Cookies["sociorevtoken"] != null)
                {
                    sociorevtoken = Request.Cookies["sociorevtoken"].ToString();
                    sociorevtoken = PluginHelper.Base64Decode(sociorevtoken);
                    SortedDictionary<string, string> strdi = new SortedDictionary<string, string>();
                    strdi.Add("systemId", sociorevtoken);
                    string respo = CustomHttpWebRequest.HttpWebRequest("POST", "/api/User/checksociorevtoken", strdi, _appSettings.ApiDomain);
                    if (respo != "false")
                    {
                        if (user == null)
                        {
                            string EmailId = string.Empty;
                            string password = string.Empty;
                            if (Request.Cookies["socioboardemailId"] != null)
                            {
                                EmailId = Request.Cookies["socioboardemailId"].ToString();
                                EmailId = PluginHelper.Base64Decode(EmailId);
                            }
                            if (Request.Cookies["socioboardToken"] != null)
                            {
                                password = Request.Cookies["socioboardToken"].ToString();
                                password = PluginHelper.Base64Decode(password);
                            }
                            if (!string.IsNullOrEmpty(EmailId))
                            {
                                SortedDictionary<string, string> strdic = new SortedDictionary<string, string>();
                                strdic.Add("UserName", EmailId);
                                if (string.IsNullOrEmpty(password))
                                {
                                    strdic.Add("Password", "sociallogin");
                                }
                                else
                                {
                                    strdic.Add("Password", password);
                                }


                                string response = CustomHttpWebRequest.HttpWebRequest("POST", "/api/User/CheckUserLogin", strdic, _appSettings.ApiDomain);

                                if (!string.IsNullOrEmpty(response))
                                {
                                    Domain.Socioboard.Models.User _user = Newtonsoft.Json.JsonConvert.DeserializeObject<Domain.Socioboard.Models.User>(response);
                                    HttpContext.Session.SetObjectAsJson("User", _user);
                                }
                            }

                        }
                    }
                }
            }

            base.OnActionExecuting(filterContext);
        }
        // GET: /<controller>/
        // [ResponseCache(Duration = 604800)]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        // [ResponseCache(Duration = 100)]
        public async Task<IActionResult> Index()
        {
            //await Task.Run(() =>
            //{
            //    UserSession();
            //}
            //);
            //new Thread(delegate ()
            //{
            //    UserSession();
            //}).Start();
            //  await UserSession();

            var user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");

            var twostep = HttpContext.Session.GetObjectFromJson<string>("twosteplogin");

            var paymentsession = HttpContext.Session.GetObjectFromJson<bool>("paymentsession");

            if (paymentsession)
            {
                user = null;
                HttpContext.Session.Remove("User");
                HttpContext.Session.Remove("revokedata");
                HttpContext.Session.Remove("selectedGroupId");
                HttpContext.Session.Remove("paymentsession");
                HttpContext.Session.Clear();
            }

            ViewBag.ApiDomain = _appSettings.ApiDomain;
            ViewBag.Domain = _appSettings.Domain;

            if (user == null)
            {
                return View();
            }
            else if (twostep == "true")
            {
                return View("TwoStepOtp");
            }
            else
            {
                try
                {
                    if (user.ExpiryDate < DateTime.UtcNow)
                    {
                        return RedirectToAction("UpgradePlans", "Index");
                    }
                }
                catch (Exception)
                {
                    return RedirectToAction("Index", "Index");
                }
                //return RedirectToAction("Index", "Home");
                return View();
            }
        }

        //[HttpGet]
        //private async Task UserSession()
        //{
        //    Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
        //    string EmailId = string.Empty;
        //    string password = string.Empty;
        //    string sociorevtoken = string.Empty;

        //    try
        //    {
        //        if (user == null)
        //        {
        //            if (Request.Cookies["sociorevtoken"] != null)
        //            {
        //                sociorevtoken = Request.Cookies["sociorevtoken"].ToString();
        //                sociorevtoken = PluginHelper.Base64Decode(sociorevtoken);
        //                try
        //                {
        //                    List<KeyValuePair<string, string>> Parame = new List<KeyValuePair<string, string>>();
        //                    Parame.Add(new KeyValuePair<string, string>("systemId", sociorevtoken));
        //                    HttpResponseMessage _response = await WebApiReq.PostReq("/api/User/checksociorevtoken", Parame, "", "", _appSettings.ApiDomain);
        //                    if (_response.IsSuccessStatusCode)
        //                    {
        //                        try
        //                        {
        //                            Domain.Socioboard.Models.SessionHistory _session = await _response.Content.ReadAsAsync<Domain.Socioboard.Models.SessionHistory>();
        //                            HttpContext.Session.SetObjectAsJson("revokedata", _session);
        //                            sociorevtoken = "true";
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            sociorevtoken = "false";
        //                        }
        //                    }
        //                }
        //                catch (Exception)
        //                {

        //                    throw;
        //                }
        //            }
        //            if (sociorevtoken == "true")
        //            {
        //                if (Request.Cookies["socioboardemailId"] != null)
        //                {
        //                    EmailId = Request.Cookies["socioboardemailId"].ToString();
        //                    EmailId = PluginHelper.Base64Decode(EmailId);
        //                }
        //                if (Request.Cookies["socioboardToken"] != null)
        //                {
        //                    password = Request.Cookies["socioboardToken"].ToString();
        //                    password = PluginHelper.Base64Decode(password);
        //                }
        //            }
        //            if (!string.IsNullOrEmpty(EmailId) && !string.IsNullOrEmpty(password))
        //            {
        //                List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
        //                Parameters.Add(new KeyValuePair<string, string>("UserName", EmailId));
        //                Parameters.Add(new KeyValuePair<string, string>("Password", password));
        //                HttpResponseMessage _response = await WebApiReq.PostReq("/api/User/CheckUserLogin", Parameters, "", "", _appSettings.ApiDomain);
        //                if (_response.IsSuccessStatusCode)
        //                {
        //                    try
        //                    {
        //                        user = await _response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
        //                        HttpContext.Session.SetObjectAsJson("User", user);
        //                    }
        //                    catch (Exception ex)
        //                    {

        //                    }
        //                }
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginViewModel userViewModel)
        {
            string output = string.Empty;
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("UserName", userViewModel.UserName));
            Parameters.Add(new KeyValuePair<string, string>("Password", userViewModel.Password));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/User/Login", Parameters, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {

                try
                {
                    Domain.Socioboard.Models.User user = await response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                    //HttpContext.Session.SetObjectAsJson("User", user);
                    if (user.UserType == "SuperAdmin")
                    {
                        HttpContext.Session.SetObjectAsJson("User", user);
                        return Content("SuperAdmin");
                        //HttpContext.Session["Id"] = user.Id;

                    }
                    // output = "1";
                    if (user.ExpiryDate < DateTime.UtcNow)
                    {
                        //return RedirectToAction("UpgradePlans", "Index");
                        // return Content("UpgradePlans");
                        List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
                        Param.Add(new KeyValuePair<string, string>("Id", user.Id.ToString()));
                        HttpResponseMessage respon = await WebApiReq.PostReq("/api/User/UpdateTrialStatus", Param, "", "", _appSettings.ApiDomain);
                        if (respon.IsSuccessStatusCode)
                        {
                            Domain.Socioboard.Models.User _user = await respon.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                            HttpContext.Session.SetObjectAsJson("User", _user);
                            return Content("Trail Expire");
                        }
                    }
                    else if ((user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.notadded || user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.inprogress) && (user.AccountType != Domain.Socioboard.Enum.SBAccountType.Free))
                    {
                        return Content("2");
                    }
                    if (user.TwostepEnable == true)
                    {
                        HttpContext.Session.SetObjectAsJson("User", user);
                        return Content("TwoStepLogin");
                    }

                    HttpContext.Session.SetObjectAsJson("User", user);
                    output = "1";
                }
                catch (Exception e)
                {
                    try
                    {
                        output = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                    }
                    return Content(output);
                }

            }
            return Content(output);
        }

        public ActionResult TwoStepOtp()
        {
            HttpContext.Session.SetObjectAsJson("twosteplogin", "true");
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user != null)
            {
                ViewBag.emailId = user.EmailId;
                ViewBag.phonenumber = user.PhoneNumber;
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Index");
            }
        }

        [HttpGet]
        public ActionResult SignIn()
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            string twostep = HttpContext.Session.GetObjectFromJson<string>("twosteplogin");
            bool paymentsession = HttpContext.Session.GetObjectFromJson<bool>("paymentsession");
            if (paymentsession == true)
            {
                user = null;
                HttpContext.Session.Remove("User");
                HttpContext.Session.Remove("revokedata");
                HttpContext.Session.Remove("selectedGroupId");
                HttpContext.Session.Remove("paymentsession");
                HttpContext.Session.Clear();
            }
            ViewBag.ApiDomain = _appSettings.ApiDomain;
            ViewBag.Domain = _appSettings.Domain;
            if (user == null)
            {
                return View();
            }
            else if (twostep == "true")
            {
                return View("TwoStepOtp");
            }
            else
            {
                try
                {
                    if (user.ExpiryDate < DateTime.UtcNow)
                    {
                        return RedirectToAction("UpgradePlans", "Index");

                    }
                }
                catch (Exception)
                {
                    return RedirectToAction("Index", "Index");
                }
                return RedirectToAction("Index", "Home");
                //return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AjaxPluginLogin()
        {
            string output = string.Empty;
            string uname = Request.Form["email"].ToString();
            string pass = Request.Form["password"].ToString();
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("UserName", uname));
            Parameters.Add(new KeyValuePair<string, string>("Password", pass));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/User/Login", Parameters, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {

                try
                {
                    Domain.Socioboard.Models.User user = await response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                    HttpContext.Session.SetObjectAsJson("User", user);
                    if (user.UserType == "SuperAdmin")
                    {
                        return Content("SuperAdmin");

                    }
                    output = "1";
                    if (user.ExpiryDate < DateTime.UtcNow)
                    {
                        //return RedirectToAction("UpgradePlans", "Index");
                        // return Content("UpgradePlans");
                        List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
                        Param.Add(new KeyValuePair<string, string>("Id", user.Id.ToString()));
                        HttpResponseMessage respon = await WebApiReq.PostReq("/api/User/UpdateTrialStatus", Param, "", "", _appSettings.ApiDomain);
                        if (respon.IsSuccessStatusCode)
                        {
                            Domain.Socioboard.Models.User _user = await respon.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                            HttpContext.Session.SetObjectAsJson("User", _user);
                            return Content("Trail Expire");
                        }
                    }
                    else if ((user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.notadded || user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.inprogress) && (user.AccountType != Domain.Socioboard.Enum.SBAccountType.Free))
                    {
                        //return RedirectToAction("PayPalAccount", "Home", new { emailId = user.EmailId,IsLogin = false });
                        return Content("2");
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        output = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                    }
                    return Content(output);
                }

            }
            return Content(output);
        }


        public async Task<IActionResult> PaymentSuccessful()
        {
            var output = "false";

            var user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            var package = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.Package>("Package");

            var trasactionId = Generatetxnid();

            #region Fetching the details from Paypal

            string media = Request.Form["charset"];

            string subscrDate = Request.Form["subscr_date"];

            if (string.IsNullOrEmpty(subscrDate))
                subscrDate = Request.Form["payment_date"];


            string ipnTrackId = Request.Form["ipn_track_id"];

            string payerEmail = Request.Form["payer_email"];

            string amount = Request.Form["amount3"];

            if (string.IsNullOrEmpty(amount))
            {
                amount = Request.Form["mc_gross"];
            }

            string mcCurrency = Request.Form["mc_currency"];

            string payername = Request.Form["first_name"] + " " + Request.Form["last_name"];

            string txnType = Request.Form["txn_type"];

            string itemName = Request.Form["item_name"];

            string paymentStatus = Request.Form["payment_status"];

            if (string.IsNullOrEmpty(paymentStatus))
            {
                paymentStatus = "Completed";
            }

            string paymentId = Request.Form["subscr_id"];

            if (string.IsNullOrEmpty(paymentId))
            {
                paymentId = Request.Form["txn_id"];
            }

            #endregion

            var plan = package.packagename;

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("userId", user.Id.ToString()),
                new KeyValuePair<string, string>("UserName", user.FirstName + " " + user.LastName),
                new KeyValuePair<string, string>("email", user.EmailId),
                new KeyValuePair<string, string>("amount", package.amount),
                new KeyValuePair<string, string>("PaymentType", user.PaymentType.ToString()),
                new KeyValuePair<string, string>("trasactionId", trasactionId),
                new KeyValuePair<string, string>("paymentId", paymentId),
                new KeyValuePair<string, string>("accType", plan),
                new KeyValuePair<string, string>("subscr_date", subscrDate.Replace("PDT", "").Replace("PST", "")),
                new KeyValuePair<string, string>("payer_email", payerEmail),
                new KeyValuePair<string, string>("Payername", payername),
                new KeyValuePair<string, string>("payment_status", paymentStatus),
                new KeyValuePair<string, string>("item_name", itemName),
                new KeyValuePair<string, string>("media", media)
            };

            var response = await WebApiReq.PostReq("/api/PaymentTransaction/UpgradeAccount", parameters, "", "", _appSettings.ApiDomain);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string data = await response.Content.ReadAsStringAsync();
                    if (data == "payment done")
                    {                      
                        var responseMessage = await WebApiReq.GetReq("/api/User/GetUser?Id=" + user.Id, "", "", _appSettings.ApiDomain);

                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                Domain.Socioboard.Models.User _user = await responseMessage.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                                if (user.ReferralStatus == "InActive" && user.ReferdBy != null)
                                {

                                    var refrralresponse = await WebApiReq.PostReq("/api/User/UpdateRefrralStatus", parameters, "", "", _appSettings.ApiDomain);
                                    if (refrralresponse.IsSuccessStatusCode)
                                    {
                                        HttpContext.Session.SetObjectAsJson("User", _user);
                                        return RedirectToAction("Index", "Index");
                                    }
                                    else
                                    {
                                        HttpContext.Session.SetObjectAsJson("User", _user);
                                        return RedirectToAction("Index", "Index");
                                    }
                                }
                                else
                                {
                                    HttpContext.Session.SetObjectAsJson("User", _user);
                                    return RedirectToAction("Index", "Index");
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception e)
                {
                    return RedirectToAction("Index", "Index");
                }

            }
            return Content(output);
        }

        [HttpGet]
        public async Task<IActionResult> SBApp(string profileType, string url, string content, string imageUrl, string name, string userImage, string screenName, string tweet, string tweetId, string type, string EmailId)
        {
            string password = "";
            Domain.Socioboard.Helpers.PluginData _PluginData = new Domain.Socioboard.Helpers.PluginData();
            _PluginData.profileType = profileType;
            _PluginData.content = content;
            _PluginData.imageUrl = imageUrl;
            _PluginData.name = name;
            _PluginData.screenName = screenName;
            _PluginData.tweet = tweet;
            _PluginData.tweetId = tweetId;
            _PluginData.url = url;
            _PluginData.userImage = userImage;
            _PluginData.type = type;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                if (imageUrl.Equals(url))
                {
                    if (type == "timeline-image")
                    {
                        _PluginData.url = string.Empty;
                    }
                    if (profileType == "image")
                    {
                        _PluginData.url = string.Empty;
                    }
                }
            }
            if (profileType == "website")
            {
                if (url.Contains(".jpg") || url.Contains(".png"))
                {
                    _PluginData.imageUrl = url;
                    _PluginData.url = string.Empty;
                }
            }
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user == null)
            {
                if (Request.Cookies["socioboardpluginemailId"] != null)
                {
                    EmailId = Request.Cookies["socioboardpluginemailId"].ToString();
                    EmailId = PluginHelper.Base64Decode(EmailId);
                }
                if (Request.Cookies["socioboardpluginToken"] != null)
                {
                    password = Request.Cookies["socioboardpluginToken"].ToString();
                    password = PluginHelper.Base64Decode(password);
                }
                if (!string.IsNullOrEmpty(EmailId) && !string.IsNullOrEmpty(password))
                {
                    List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
                    Parameters.Add(new KeyValuePair<string, string>("UserName", EmailId));
                    Parameters.Add(new KeyValuePair<string, string>("Password", password));
                    HttpResponseMessage _response = await WebApiReq.PostReq("/api/User/CheckUserLogin", Parameters, "", "", _appSettings.ApiDomain);
                    if (_response.IsSuccessStatusCode)
                    {
                        try
                        {
                            user = await _response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                            HttpContext.Session.SetObjectAsJson("User", user);
                        }
                        catch { }
                    }
                    else
                    {
                        ViewBag.User = "false";
                        return View("Rlogin");
                    }
                }
            }
            if (user != null)
            {
                if (!string.IsNullOrEmpty(url) && profileType != "pinterest")
                {
                    Domain.Socioboard.Helpers.ThumbnailDetails plugindata = PluginHelper.CreateThumbnail(url);
                    _PluginData._ThumbnailDetails = plugindata;
                }

                ViewBag.plugin = _PluginData;
                ViewBag.emailId = user.EmailId;
                ViewBag.password = user.Password;
                ViewBag.userId = user.Id.ToString();
                HttpResponseMessage response = await WebApiReq.GetReq("/api/Groups/GetUserGroups?userId=" + user.Id, "", "", _appSettings.ApiDomain);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        List<Domain.Socioboard.Models.Groups> groups = await response.Content.ReadAsAsync<List<Domain.Socioboard.Models.Groups>>();
                        long selectedGroupId = groups.FirstOrDefault(t => t.groupName == Domain.Socioboard.Consatants.SocioboardConsts.DefaultGroupName).id;
                        List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
                        parameters.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
                        HttpResponseMessage _response = await WebApiReq.GetReq("/api/GroupProfiles/GetPluginProfile?groupId=" + selectedGroupId, "", "", _appSettings.ApiDomain);
                        if (_response.IsSuccessStatusCode)
                        {
                            try
                            {
                                List<Domain.Socioboard.Helpers.PluginProfile> lstsb = new List<Domain.Socioboard.Helpers.PluginProfile>();
                                lstsb = await _response.Content.ReadAsAsync<List<Domain.Socioboard.Helpers.PluginProfile>>();
                                return View("RMain", lstsb);
                            }
                            catch { }
                        }
                    }
                    catch
                    {

                    }
                }

            }
            return View("Rlogin");
        }
        [HttpPost]
        public ActionResult IsUserSession()
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user != null)
            {
                return Content("user");
            }
            else
            {
                return Content("");
            }
        }


        [HttpPost]
        public ActionResult IsUserSessionPlugin()
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user != null)
            {
                return Content(user.EmailId);
            }
            else
            {
                return Content("");
            }
        }
        [HttpPost]
        public async Task<IActionResult> PluginSignUp()
        {
            string output = "";
            string name = Request.Form["name"].ToString();
            string email = Request.Form["email"].ToString();
            string password = Request.Form["password"].ToString();
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("FirstName", name));
            Parameters.Add(new KeyValuePair<string, string>("EmailId", email));
            Parameters.Add(new KeyValuePair<string, string>("Password", password));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/User/Register", Parameters, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                output = await response.Content.ReadAsStringAsync();
            }
            return Content(output);
        }



        public IActionResult PaymentFailed()
        {
            return RedirectToAction("Index", "Index");
        }
        public IActionResult PaymentCancel()
        {
            return RedirectToAction("Index", "Index");
        }


        public async void PaymentNotify(string code)
        {
            //_logger.LogError("paypalnotifications start");
            //foreach (var key in Request.Form.Keys)
            //{
            //    _logger.LogError(key+":"+Request.Form[key]);
            //}
            //_logger.LogError("paypalnotifications end");
            //return RedirectToAction("Index", "Index");
            string media = Request.Form["charset"];
            string subscr_date = Request.Form["subscr_date"];
            string ipn_track_id = Request.Form["ipn_track_id"];
            string payer_email = Request.Form["payer_email"];
            string amount = Request.Form["amount3"];
            string mc_currency = Request.Form["mc_currency"];
            string Payername = Request.Form["first_name"] + " " + Request.Form["last_name"];
            string txn_type = Request.Form["txn_type"];
            string item_name = Request.Form["item_name"];
            string subscr_id = Request.Form["subscr_id"];
            string payment_status = Request.Form["payment_status"];
            if (string.IsNullOrEmpty(payment_status))
            {
                payment_status = "Completed";
            }
            string txn_id = Request.Form["txn_id"];
            if (payment_status == "Completed")
            {
                List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
                Parameters.Add(new KeyValuePair<string, string>("subscr_id", subscr_id));
                Parameters.Add(new KeyValuePair<string, string>("txn_id", txn_id));
                Parameters.Add(new KeyValuePair<string, string>("subscr_date", subscr_date.Replace("PDT", "").Replace("PST", "")));
                Parameters.Add(new KeyValuePair<string, string>("payer_email", payer_email));
                Parameters.Add(new KeyValuePair<string, string>("Payername", Payername));
                Parameters.Add(new KeyValuePair<string, string>("payment_status", payment_status));
                Parameters.Add(new KeyValuePair<string, string>("item_name", item_name));
                Parameters.Add(new KeyValuePair<string, string>("amount", amount));
                Parameters.Add(new KeyValuePair<string, string>("media", media));
                HttpResponseMessage response = await WebApiReq.PostReq("/api/PaymentTransaction/UpdateRecurringUser", Parameters, "", "", _appSettings.ApiDomain);
                if (response.IsSuccessStatusCode)
                {
                }
            }
        }

        public IActionResult UpgradePlans()
        {
            ViewBag.user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> UpgradeAccount(string packagename)
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user == null)
            {
                return RedirectToAction("Index", "Index");
            }
            if (packagename != "Free")
            {
                List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
                Parameters.Add(new KeyValuePair<string, string>("packagename", packagename));
                HttpResponseMessage response = await WebApiReq.PostReq("/api/PaymentTransaction/GetPackage", Parameters, "", "", _appSettings.ApiDomain);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        Domain.Socioboard.Models.Package _Package = await response.Content.ReadAsAsync<Domain.Socioboard.Models.Package>();
                        HttpContext.Session.SetObjectAsJson("Package", _Package);

                        if (user.CreateDate.AddDays(29) <= DateTime.UtcNow)
                        {
                            if (user.PaymentType == Domain.Socioboard.Enum.PaymentType.paypal)
                            {
                                return Content(Helpers.Payment.RecurringPaymentWithPayPalUpgrade(_Package.amount, _Package.packagename, user.FirstName + " " + user.LastName, user.PhoneNumber, user.EmailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.callBackUrl, _appSettings.cancelurl, _appSettings.notifyUrl, "", _appSettings.PaypalURL));
                            }
                            else
                            {
                                return RedirectToAction("paymentWithPayUMoney", "Index");
                            }
                        }
                        else
                        {
                            if (user.PaymentType == Domain.Socioboard.Enum.PaymentType.paypal)
                            {
                                return Content(Helpers.Payment.RecurringPaymentWithPayPalUpgrade(_Package.amount, _Package.packagename, user.FirstName + " " + user.LastName, user.PhoneNumber, user.EmailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.callBackUrl, _appSettings.cancelurl, _appSettings.notifyUrl, "", _appSettings.PaypalURL));
                            }
                            else
                            {
                                return RedirectToAction("paymentWithPayUMoney", "Index");
                            }
                        }
                    }
                    catch { }

                }
            }
            else
            {
                List<KeyValuePair<string, string>> _Parameters = new List<KeyValuePair<string, string>>();
                _Parameters.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
                HttpResponseMessage _response = await WebApiReq.PostReq("/api/User/UpdateFreeUser", _Parameters, "", "", _appSettings.ApiDomain);
                if (_response.IsSuccessStatusCode)
                {
                    try
                    {
                        Domain.Socioboard.Models.User _user = await _response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                        HttpContext.Session.SetObjectAsJson("User", _user);
                        return Content("");
                    }
                    catch { }
                }
            }
            return RedirectToAction("Index", "Index");
        }


        [HttpGet]
        public async Task<IActionResult> AgencyPlan(string firstName, string lastName, string company, string emailId, string phoneNumber, string message, string demoPlanType, string amount)
        {
            Domain.Socioboard.Models.AgencyUser _AgencyUser = new Domain.Socioboard.Models.AgencyUser();
            _AgencyUser.userName = firstName + " " + lastName;
            _AgencyUser.company = company;
            _AgencyUser.email = emailId;
            _AgencyUser.message = message;
            _AgencyUser.phnNumber = phoneNumber;
            _AgencyUser.planType = demoPlanType;
            _AgencyUser.amount = double.Parse(amount);
            HttpContext.Session.SetObjectAsJson("AgencyUser", _AgencyUser);
            return Content(Helpers.Payment.AgencyPayment(amount, demoPlanType, firstName + " " + lastName, phoneNumber, emailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.AgencycallBackUrl, _appSettings.cancelurl, "", "", _appSettings.PaypalURL));
        }

        public async Task<IActionResult> AgencyPaymentSuccessful()
        {
            Domain.Socioboard.Models.AgencyUser _AgencyUser = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.AgencyUser>("AgencyUser");
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("userName", _AgencyUser.userName));
            Parameters.Add(new KeyValuePair<string, string>("planType", _AgencyUser.planType));
            Parameters.Add(new KeyValuePair<string, string>("phnNumber", _AgencyUser.phnNumber));
            Parameters.Add(new KeyValuePair<string, string>("message", _AgencyUser.message));
            Parameters.Add(new KeyValuePair<string, string>("email", _AgencyUser.email));
            Parameters.Add(new KeyValuePair<string, string>("company", _AgencyUser.company));
            Parameters.Add(new KeyValuePair<string, string>("amount", _AgencyUser.amount.ToString()));

            HttpResponseMessage response = await WebApiReq.PostReq("/api/AgencyUser/UpdateUserInfo", Parameters, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string returndata = await response.Content.ReadAsStringAsync();
                    return Content("");
                }
                catch { }

            }
            return Content("");
        }


        [HttpGet]
        public async Task<IActionResult> paymentWithPayUMoney(bool contesnt)
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            Domain.Socioboard.Models.Package _Package = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.Package>("Package");
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("key", _appSettings.key));
            string amount = (Convert.ToDouble(_Package.amount) * Convert.ToDouble(_appSettings.moneyconvertion)).ToString();
            string txnid = Generatetxnid();
            Parameters.Add(new KeyValuePair<string, string>("txnid", txnid));
            Parameters.Add(new KeyValuePair<string, string>("amount", amount));
            Parameters.Add(new KeyValuePair<string, string>("productinfo", _Package.packagename));
            Parameters.Add(new KeyValuePair<string, string>("firstname", user.FirstName));
            Parameters.Add(new KeyValuePair<string, string>("email", user.EmailId));
            Parameters.Add(new KeyValuePair<string, string>("phone", user.PhoneNumber));
            Parameters.Add(new KeyValuePair<string, string>("surl", _appSettings.surl));
            Parameters.Add(new KeyValuePair<string, string>("furl", _appSettings.failUrl));
            string hashString = _appSettings.key.ToString() + "|" + txnid + "|" + amount + "|" + _Package.packagename.ToString() + "|" + user.FirstName.ToString() + "|" + user.EmailId.ToString() + "|||||||||||" + _appSettings.salt.ToString();
            string hash = Generatehash512(hashString);
            Parameters.Add(new KeyValuePair<string, string>("hash", hash));
            Parameters.Add(new KeyValuePair<string, string>("service_provider", "payu_paisa"));
            HttpResponseMessage response = await WebApiReq.PostReq("/_payment", Parameters, "", "", "https://secure.payu.in");
            if (response.IsSuccessStatusCode)
            {
                // string data = await response.Content.ReadAsStringAsync();
                if (contesnt != false)
                {
                    return Content(response.RequestMessage.RequestUri.OriginalString);
                }
                else
                {
                    //return Redirect(response.RequestMessage.RequestUri.OriginalString);
                    //return Redirect(response.RequestMessage.RequestUri.OriginalString);
                    return Content(response.RequestMessage.RequestUri.OriginalString);
                }
            }
            else
            {
                return null;
            }
        }

        public async Task<IActionResult> PayUMoneySuccessful(FormCollection form)
        {
            string status = Request.Form["status"].ToString();
            string plan = string.Empty;
            string output = "false";
            Domain.Socioboard.Models.Package _Package = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.Package>("Package");
            if (status == "success")
            {
                if (_Package.packagename == "Free")
                {
                    plan = "Free";
                }
                else if (_Package.packagename == "Deluxe")
                {
                    plan = "Deluxe";

                }
                else if (_Package.packagename == "Premium")
                {
                    plan = "Premium";

                }
                else if (_Package.packagename == "Topaz")
                {
                    plan = "Topaz";

                }
                else if (_Package.packagename == "Platinum")
                {
                    plan = "Platinum";

                }
                else if (_Package.packagename == "Gold")
                {
                    plan = "Gold";

                }
                else if (_Package.packagename == "Ruby")
                {
                    plan = "Ruby";

                }
                else if (_Package.packagename == "Standard")
                {
                    plan = "Standard";

                }
                string payuMoneyId = Request.Form["payuMoneyId"].ToString();
                string PG_TYPE = Request.Form["PG_TYPE"].ToString();
                string txnid = Request.Form["txnid"].ToString();
                string amount = Request.Form["amount"].ToString();
                string productinfo = Request.Form["productinfo"].ToString();
                Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");

                List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
                Parameters.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
                Parameters.Add(new KeyValuePair<string, string>("UserName", user.FirstName + " " + user.LastName));
                Parameters.Add(new KeyValuePair<string, string>("email", user.EmailId));
                Parameters.Add(new KeyValuePair<string, string>("amount", amount));
                Parameters.Add(new KeyValuePair<string, string>("PaymentType", user.PaymentType.ToString()));
                Parameters.Add(new KeyValuePair<string, string>("trasactionId", txnid));
                Parameters.Add(new KeyValuePair<string, string>("paymentId", payuMoneyId));
                Parameters.Add(new KeyValuePair<string, string>("accType", plan));
                HttpResponseMessage response = await WebApiReq.PostReq("/api/PaymentTransaction/UpgradeAccount", Parameters, "", "", _appSettings.ApiDomain);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        string data = await response.Content.ReadAsStringAsync();
                        if (data == "payment done")
                        {
                            List<KeyValuePair<string, string>> _Parameters = new List<KeyValuePair<string, string>>();
                            // _Parameters.Add(new KeyValuePair<string, string>("Id", user.Id.ToString()));
                            HttpResponseMessage _response = await WebApiReq.GetReq("/api/User/GetUser?Id=" + user.Id.ToString(), "", "", _appSettings.ApiDomain);
                            if (response.IsSuccessStatusCode)
                            {
                                try
                                {
                                    Domain.Socioboard.Models.User _user = await _response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                                    HttpContext.Session.SetObjectAsJson("User", _user);
                                    //SendInvoiceMerchant(_user.PhoneNumber, _user.EmailId, _user.FirstName, amount, productinfo, txnid, _appSettings.PayUMoneyURL);
                                    // output = "true";
                                    return RedirectToAction("Index", "Home");
                                }
                                catch { }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        return RedirectToAction("Index", "Index");
                    }

                }
            }
            return RedirectToAction("Index", "Index");
        }

        public async void SendInvoiceMerchant(string customerPhone, string customerEmail, string customerName, string amount, string paymentDescription, string transactionId, string PayUMoneyURL)
        {
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("customerPhone", customerPhone));
            Parameters.Add(new KeyValuePair<string, string>("customerEmail", customerEmail));
            Parameters.Add(new KeyValuePair<string, string>("customerName", customerName));
            Parameters.Add(new KeyValuePair<string, string>("amount", amount));
            Parameters.Add(new KeyValuePair<string, string>("paymentDescription", paymentDescription));
            Parameters.Add(new KeyValuePair<string, string>("transactionId", transactionId));
            try
            {
                HttpResponseMessage response = await WebApiReq.PostReqPayUMoney("payment/payment/addInvoiceMerchantAPI?", Parameters, "", "", _appSettings.payurl, _appSettings.AuthorizationKey);
                if (response.IsSuccessStatusCode)
                {
                    Redirect(response.RequestMessage.RequestUri.OriginalString);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public string Generatetxnid()
        {
            var rnd = new Random();
            var strHash = Generatehash512(rnd.ToString() + DateTime.Now);
            var txnid1 = strHash.Substring(0, 20);

            return txnid1;
        }
        public string Generatehash512(string text)
        {

            byte[] message = Encoding.UTF8.GetBytes(text);

            UnicodeEncoding UE = new UnicodeEncoding();
            byte[] hashValue;
            SHA512Managed hashString = new SHA512Managed();
            string hex = "";
            hashValue = hashString.ComputeHash(message);
            foreach (byte x in hashValue)
            {
                hex += String.Format("{0:x2}", x);
            }
            return hex;

        }

        [HttpGet]
        public async Task<IActionResult> TrainingPlan(string firstName, string lastName, string company, string emailId, string phoneNumber, string message, string planType, string amount)
        {
            Domain.Socioboard.Models.Training _Training = new Domain.Socioboard.Models.Training();
            _Training.FirstName = firstName;
            _Training.LastName = lastName;
            _Training.EmailId = emailId;
            _Training.Message = message;
            _Training.PhoneNo = phoneNumber;
            _Training.Company = company;
            // HttpResponseMessage response = await WebApiReq.PostReq("/api/Training/updateTrainingDetails", Parameters, "", "", _appSettings.ApiDomain);
            _Training.PaymentStatus = planType;
            _Training.PaymentAmount = double.Parse(amount);
            HttpContext.Session.SetObjectAsJson("Training", _Training);
            HttpContext.Session.SetObjectAsJson("paymentsession", true);
            return Content(Helpers.Payment.AgencyPayment(amount, planType, firstName + " " + lastName, phoneNumber, emailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.TrainingcallBackUrl, _appSettings.cancelurl, "", "", _appSettings.PaypalURL));

        }


        public async Task<IActionResult> TrainingPaymentSuccessful()
        {
            Domain.Socioboard.Models.Training _training = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.Training>("Training");
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("firstname", _training.FirstName));
            Parameters.Add(new KeyValuePair<string, string>("lastname", _training.LastName));
            Parameters.Add(new KeyValuePair<string, string>("phoneNo", _training.PhoneNo));
            Parameters.Add(new KeyValuePair<string, string>("message", _training.Message));
            Parameters.Add(new KeyValuePair<string, string>("emailId", _training.EmailId));
            Parameters.Add(new KeyValuePair<string, string>("company", _training.Company));
            Parameters.Add(new KeyValuePair<string, string>("amount", _training.PaymentAmount.ToString()));

            HttpResponseMessage response = await WebApiReq.PostReq("/api/Training/updateTrainingDetails", Parameters, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string returndata = await response.Content.ReadAsStringAsync();
                    return Content("");
                }
                catch { }

            }
            return Content("");
        }




        public IActionResult Company()
        {
            return View();
        }
        public IActionResult Products()
        {
            return View();
        }

        public IActionResult Agency()
        {
            ViewBag.ApiDomain = _appSettings.ApiDomain;
            return View();
        }
        public IActionResult Enterprise()
        {
            ViewBag.ApiDomain = _appSettings.ApiDomain;
            return View();
        }

        //public IActionResult Plans()
        //{
        //    ViewBag.ApiDomain = _appSettings.ApiDomain;
        //    return View();
        //}


        [HttpGet]
        public async Task<IActionResult> plans()
        {
            HttpResponseMessage _response = await WebApiReq.GetReq("/api/User/GetPlans", "", "", _appSettings.ApiDomain);
            List<Domain.Socioboard.Models.Package> lstsb = new List<Domain.Socioboard.Models.Package>();
            lstsb = await _response.Content.ReadAsAsync<List<Domain.Socioboard.Models.Package>>();

            ViewBag.plugin = lstsb;
            ViewBag.ApiDomain = _appSettings.ApiDomain;
            ViewBag.Domain = _appSettings.Domain;
            return View("Plans");
        }

        public IActionResult Download()
        {
            ViewBag.ApiDomain = _appSettings.ApiDomain;
            return View();
        }

        [HttpGet]
        public IActionResult Careers()
        {
            return View();
        }



        [Route("PrivacyPolicy")]

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Refund_Policy()
        {
            return View();
        }
        public IActionResult Training()
        {
            return View();
        }
        public IActionResult Sitemap()
        {
            return View();
        }
        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult ResetPassword()
        {
            ViewBag.ApiDomain = _appSettings.ApiDomain;
            return View();
        }

        public IActionResult friendRemove()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> board(string boardName)
        {
            HttpResponseMessage response = await WebApiReq.GetReq("/api/BoardMe/getBoardByName?boardName=" + boardName, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    ViewBag.board = await response.Content.ReadAsAsync<Domain.Socioboard.Models.MongoBoards>();
                    return View();
                }
                catch (Exception e)
                {
                    string output = string.Empty;
                    try
                    {
                        output = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                    }
                    return Content(output);
                }

            }



            return View();
        }

        [Route("TermsConditions")]

        public IActionResult Conditions()
        {
            return View();
        }


    }
}
