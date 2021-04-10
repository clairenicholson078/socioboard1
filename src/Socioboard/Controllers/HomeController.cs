﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Socioboard.Extensions;
using Socioboard.Helpers;
using System.Net.Http;
using Domain.Socioboard.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Filters;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Socioboard.Helper;
using Newtonsoft.Json;
using System.Net;

namespace Socioboard.Controllers
{
    public class HomeController : Controller
    {
        private Helpers.AppSettings _appSettings;
        private readonly ILogger _logger;
        private readonly IHostingEnvironment _appEnv;
        public HomeController(ILogger<HomeController> logger, IHostingEnvironment appEnv, Microsoft.Extensions.Options.IOptions<Helpers.AppSettings> settings)
        {
            _appSettings = settings.Value;
            _logger = logger;
            _appEnv = appEnv;
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

        // [ResponseCache(Duration = 100)]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index()
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");

            HttpContext.Session.SetObjectAsJson("twosteplogin", "false");
            if (user == null)
            {
                return RedirectToAction("Index", "Index");
            }
            await SaveSessionData();

            try
            {
                if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Free)
                {
                    ViewBag.AccountType = "Free";
                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Deluxe)
                {
                    ViewBag.AccountType = "Deluxe";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Premium)
                {
                    ViewBag.AccountType = "Premium";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Topaz)
                {
                    ViewBag.AccountType = "Topaz";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Platinum)
                {
                    ViewBag.AccountType = "Platinum";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Gold)
                {
                    ViewBag.AccountType = "Gold";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Ruby)
                {
                    ViewBag.AccountType = "Ruby";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Standard)
                {
                    ViewBag.AccountType = "Standard";

                }
                if (user.ExpiryDate < DateTime.UtcNow)
                {
                    //return RedirectToAction("UpgradePlans", "Index");
                    if (user.TrailStatus != Domain.Socioboard.Enum.UserTrailStatus.inactive)
                    {
                        List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
                        Param.Add(new KeyValuePair<string, string>("Id", user.Id.ToString()));
                        HttpResponseMessage respon = await WebApiReq.PostReq("/api/User/UpdateTrialStatus", Param, "", "", _appSettings.ApiDomain);
                        if (respon.IsSuccessStatusCode)
                        {
                            Domain.Socioboard.Models.User _user = await respon.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                            HttpContext.Session.SetObjectAsJson("User", _user);
                            user = _user;
                        }
                    }

                }
                else if ((user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.notadded || user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.inprogress) && (user.AccountType != Domain.Socioboard.Enum.SBAccountType.Free))
                {
                    HttpContext.Session.SetObjectAsJson("paymentsession", true);
                    return RedirectToAction("PayPalAccount", "Home", new { emailId = user.EmailId, IsLogin = true });
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Index", "Index");
            }
            string sessionSelectedGroupId = HttpContext.Session.GetObjectFromJson<string>("selectedGroupId");
            HttpResponseMessage response = await WebApiReq.GetReq("/api/Groups/GetUserGroupData?userId=" + user.Id + "&groupId=" + sessionSelectedGroupId, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    Domain.Socioboard.Models.GetUserGroupData groups = await response.Content.ReadAsAsync<Domain.Socioboard.Models.GetUserGroupData>();
                    //int groupscount = groups.lstgroup.Count;
                    //int groupsMaxCount = Domain.Socioboard.Helpers.SBHelper.GetMaxGroupCount(user.AccountType);
                    //ViewBag.groupsCount = groupscount;
                    //ViewBag.groupsMaxCount = groupsMaxCount;
                    //ViewBag.AccountType = user.AccountType;
                    //if (groupscount > groupsMaxCount)
                    //{
                    //    ViewBag.groupsdowngrade = "true";
                    //}
                    //else
                    //{
                    //    ViewBag.groupsdowngrade = "false";
                    //}
                    ViewBag.groups = Newtonsoft.Json.JsonConvert.SerializeObject(groups.lstgroup);

                    if (!string.IsNullOrEmpty(sessionSelectedGroupId))
                    {
                        ViewBag.selectedGroupId = sessionSelectedGroupId;
                        try
                        {
                            var keyValuePairProfiles = groups.myProfiles.Single(x => x.Key == Convert.ToInt32(sessionSelectedGroupId));
                            List<Domain.Socioboard.Models.Groupprofiles> groupProfiles = keyValuePairProfiles.Value.ToList();
                            ViewBag.groupProfiles = Newtonsoft.Json.JsonConvert.SerializeObject(groupProfiles);
                            int count = groupProfiles.Count;
                            int MaxCount = Domain.Socioboard.Helpers.SBHelper.GetMaxProfileCount(user.AccountType);
                            ViewBag.profileCount = count;
                            ViewBag.MaxCount = MaxCount;
                            ViewBag.AccountType = user.AccountType;
                            if (count > MaxCount)
                            {
                                ViewBag.downgrade = "true";
                            }
                            else
                            {
                                ViewBag.downgrade = "false";
                            }
                        }
                        catch (Exception)
                        {

                            ViewBag.groupProfiles = Newtonsoft.Json.JsonConvert.SerializeObject(new List<Groupprofiles>());
                            int count = 0;
                            int MaxCount = Domain.Socioboard.Helpers.SBHelper.GetMaxProfileCount(user.AccountType);
                            ViewBag.profileCount = count;
                            ViewBag.MaxCount = MaxCount;
                            ViewBag.AccountType = user.AccountType;
                            if (count > MaxCount)
                            {
                                ViewBag.downgrade = "true";
                            }
                            else
                            {
                                ViewBag.downgrade = "false";
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            long selectedGroupId = groups.lstgroup.Single(t => t.groupName == Domain.Socioboard.Consatants.SocioboardConsts.DefaultGroupName).id;
                            HttpContext.Session.SetObjectAsJson("selectedGroupId", selectedGroupId);
                            ViewBag.selectedGroupId = selectedGroupId;
                            List<Domain.Socioboard.Models.Groupprofiles> groupProfiles = new List<Groupprofiles>();
                            if (groups.myProfiles.Count != 0)
                            {
                                var keyValuePairProfiles = groups.myProfiles.Single(x => x.Key == selectedGroupId);
                                groupProfiles = keyValuePairProfiles.Value.ToList();
                            }
                            ViewBag.groupProfiles = Newtonsoft.Json.JsonConvert.SerializeObject(groupProfiles);
                            int count = groupProfiles.Count;
                            int MaxCount = Domain.Socioboard.Helpers.SBHelper.GetMaxProfileCount(user.AccountType);
                            ViewBag.profileCount = count;
                            ViewBag.MaxCount = MaxCount;
                            if (count > MaxCount)
                            {
                                ViewBag.downgrade = "true";
                            }
                            else
                            {
                                ViewBag.downgrade = "false";
                            }
                        }
                        catch
                        {
                            ViewBag.groupProfiles = Newtonsoft.Json.JsonConvert.SerializeObject(new List<Groupprofiles>());
                            int count = 0;
                            int MaxCount = Domain.Socioboard.Helpers.SBHelper.GetMaxProfileCount(user.AccountType);
                            ViewBag.profileCount = count;
                            ViewBag.MaxCount = MaxCount;
                            ViewBag.AccountType = user.AccountType;
                            if (count > MaxCount)
                            {
                                ViewBag.downgrade = "true";
                            }
                            else
                            {
                                ViewBag.downgrade = "false";
                            }
                        }


                    }
                }
                catch (Exception ex)
                {
                    HttpContext.Session.Remove("User");
                    HttpContext.Session.Remove("selectedGroupId");
                    HttpContext.Session.Clear();
                    ViewBag.user = null;
                    ViewBag.selectedGroupId = null;
                    ViewBag.groupProfiles = null;
                    TempData["Error"] = "Some thing went wrong.";
                    return RedirectToAction("Index", "Index");
                }
            }
            else
            {
                return RedirectToAction("Index", "Index");
            }
            ViewBag.user = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            return View();
        }

        private async Task SaveSessionData()
        {
            Domain.Socioboard.Models.SessionHistory session = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.SessionHistory>("revokedata");
            if (session != null)
            {
                List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
                Param.Add(new KeyValuePair<string, string>("systemId", session.systemId));
                HttpResponseMessage respon = await WebApiReq.PostReq("/api/User/UpdateSessiondata", Param, "", "", _appSettings.ApiDomain);
                if (respon.IsSuccessStatusCode)
                {
                    try
                    {
                        Domain.Socioboard.Models.SessionHistory _SessionHistory = await respon.Content.ReadAsAsync<Domain.Socioboard.Models.SessionHistory>();
                        HttpContext.Session.SetObjectAsJson("revokedata", _SessionHistory);
                    }
                    catch (Exception)
                    {
                        HttpContext.Session.Remove("User");
                        HttpContext.Session.Remove("selectedGroupId");
                        HttpContext.Session.Clear();
                        HttpContext.Session.Remove("revokedata");
                    }
                }

            }
        }

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveSessiondata(string ip, string browserName, string userAgent)
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            string systemId = string.Empty;
            userAgent = getBetween(userAgent, "Mozilla/5.0 (", ";");
            string brwdata = browserName + " on " + userAgent;
            List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
            Param.Add(new KeyValuePair<string, string>("ip", ip));
            Param.Add(new KeyValuePair<string, string>("brwdata", brwdata));
            Param.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
            HttpResponseMessage respon = await WebApiReq.PostReq("/api/User/SaveSessiondata", Param, "", "", _appSettings.ApiDomain);
            if (respon.IsSuccessStatusCode)
            {
                Domain.Socioboard.Models.SessionHistory _SessionHistory = await respon.Content.ReadAsAsync<Domain.Socioboard.Models.SessionHistory>();
                systemId = _SessionHistory.systemId;
                HttpContext.Session.SetObjectAsJson("revokedata", _SessionHistory);

            }
            Domain.Socioboard.Models.SessionHistory session = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.SessionHistory>("revokedata");
            return Content(systemId);
        }




        // [ResponseCache(Duration = 100)]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexForTwoStep(string EmailId)
        {
            HttpResponseMessage _response = await WebApiReq.GetReq("/api/User/GetUserData?emailId=" + EmailId, "", "", _appSettings.ApiDomain);
            if (_response.IsSuccessStatusCode)
            {
                Domain.Socioboard.Models.User user1 = await _response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                HttpContext.Session.SetObjectAsJson("User", user1);
                // ViewBag.user = user;
            }
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");

            if (user == null)
            {
                return RedirectToAction("Index", "Index");
            }


            try
            {
                if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Free)
                {
                    ViewBag.AccountType = "Free";
                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Deluxe)
                {
                    ViewBag.AccountType = "Deluxe";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Premium)
                {
                    ViewBag.AccountType = "Premium";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Topaz)
                {
                    ViewBag.AccountType = "Topaz";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Platinum)
                {
                    ViewBag.AccountType = "Platinum";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Gold)
                {
                    ViewBag.AccountType = "Gold";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Ruby)
                {
                    ViewBag.AccountType = "Ruby";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Standard)
                {
                    ViewBag.AccountType = "Standard";

                }
                if (user.ExpiryDate < DateTime.UtcNow)
                {
                    //return RedirectToAction("UpgradePlans", "Index");
                    if (user.TrailStatus != Domain.Socioboard.Enum.UserTrailStatus.inactive)
                    {
                        List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
                        Param.Add(new KeyValuePair<string, string>("Id", user.Id.ToString()));
                        HttpResponseMessage respon = await WebApiReq.PostReq("/api/User/UpdateTrialStatus", Param, "", "", _appSettings.ApiDomain);
                        if (respon.IsSuccessStatusCode)
                        {
                            Domain.Socioboard.Models.User _user = await respon.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                            HttpContext.Session.SetObjectAsJson("User", _user);
                            user = _user;
                        }
                    }

                }
                else if ((user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.notadded || user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.inprogress) && (user.AccountType != Domain.Socioboard.Enum.SBAccountType.Free))
                {
                    HttpContext.Session.SetObjectAsJson("paymentsession", true);
                    return RedirectToAction("PayPalAccount", "Home", new { emailId = user.EmailId, IsLogin = true });
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Index", "Index");
            }
            HttpResponseMessage response = await WebApiReq.GetReq("/api/Groups/GetUserGroups?userId=" + user.Id, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    List<Domain.Socioboard.Models.Groups> groups = await response.Content.ReadAsAsync<List<Domain.Socioboard.Models.Groups>>();
                    ViewBag.groups = Newtonsoft.Json.JsonConvert.SerializeObject(groups);
                    string sessionSelectedGroupId = HttpContext.Session.GetObjectFromJson<string>("selectedGroupId");
                    if (!string.IsNullOrEmpty(sessionSelectedGroupId))
                    {
                        ViewBag.selectedGroupId = sessionSelectedGroupId;
                        HttpResponseMessage groupProfilesResponse = await WebApiReq.GetReq("/api/GroupProfiles/GetGroupProfiles?groupId=" + sessionSelectedGroupId, "", "", _appSettings.ApiDomain);
                        if (groupProfilesResponse.IsSuccessStatusCode)
                        {
                            List<Domain.Socioboard.Models.Groupprofiles> groupProfiles = await groupProfilesResponse.Content.ReadAsAsync<List<Domain.Socioboard.Models.Groupprofiles>>();
                            ViewBag.groupProfiles = Newtonsoft.Json.JsonConvert.SerializeObject(groupProfiles);
                            // string profileCount = await ProfilesHelper.GetUserProfileCount(user.Id, _appSettings, _logger);
                            // int count = Convert.ToInt32(profileCount);
                            int count = groupProfiles.Count;
                            int MaxCount = Domain.Socioboard.Helpers.SBHelper.GetMaxProfileCount(user.AccountType);
                            ViewBag.profileCount = count;
                            ViewBag.MaxCount = MaxCount;
                            ViewBag.AccountType = user.AccountType;
                            if (count > MaxCount)
                            {
                                ViewBag.downgrade = "true";
                            }
                            else
                            {
                                ViewBag.downgrade = "false";
                            }
                        }



                    }
                    else
                    {
                        long selectedGroupId = groups.FirstOrDefault(t => t.groupName == Domain.Socioboard.Consatants.SocioboardConsts.DefaultGroupName).id;
                        HttpContext.Session.SetObjectAsJson("selectedGroupId", selectedGroupId);
                        ViewBag.selectedGroupId = selectedGroupId;
                        HttpResponseMessage groupProfilesResponse = await WebApiReq.GetReq("/api/GroupProfiles/GetGroupProfiles?groupId=" + selectedGroupId, "", "", _appSettings.ApiDomain);
                        if (groupProfilesResponse.IsSuccessStatusCode)
                        {
                            List<Domain.Socioboard.Models.Groupprofiles> groupProfiles = await groupProfilesResponse.Content.ReadAsAsync<List<Domain.Socioboard.Models.Groupprofiles>>();
                            ViewBag.groupProfiles = Newtonsoft.Json.JsonConvert.SerializeObject(groupProfiles);
                            //string profileCount = await ProfilesHelper.GetUserProfileCount(user.Id, _appSettings, _logger);
                            // int count = Convert.ToInt32(profileCount);
                            int count = groupProfiles.Count;
                            int MaxCount = Domain.Socioboard.Helpers.SBHelper.GetMaxProfileCount(user.AccountType);
                            ViewBag.profileCount = count;
                            ViewBag.MaxCount = MaxCount;
                            if (count > MaxCount)
                            {
                                ViewBag.downgrade = "true";
                            }
                            else
                            {
                                ViewBag.downgrade = "false";
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    HttpContext.Session.Remove("User");
                    HttpContext.Session.Remove("selectedGroupId");
                    HttpContext.Session.Clear();
                    ViewBag.user = null;
                    ViewBag.selectedGroupId = null;
                    ViewBag.groupProfiles = null;
                    TempData["Error"] = "Some thing went wrong.";
                    return RedirectToAction("Index", "Index");
                }
            }
            else
            {
                return RedirectToAction("Index", "Index");
            }
            ViewBag.user = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> PluginComposeMessage()
        {
            string profile = Request.Form["profile"];
            string twitterText = Request.Form["twitterText"];
            string tweetId = Request.Form["tweetId"];
            string tweetUrl = Request.Form["tweetUrl"];
            string facebookText = Request.Form["facebookText"];
            string url = Request.Form["url"];
            string imgUrl = Request.Form["imgUrl"];
            string output = "";
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user == null)
            {
                return View("Rlogin");
            }
            List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
            Param.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
            Param.Add(new KeyValuePair<string, string>("tweetUrl", tweetUrl));
            Param.Add(new KeyValuePair<string, string>("facebookText", facebookText));
            Param.Add(new KeyValuePair<string, string>("url", url));
            Param.Add(new KeyValuePair<string, string>("imgUrl", imgUrl));
            Param.Add(new KeyValuePair<string, string>("tweetId", tweetId));
            Param.Add(new KeyValuePair<string, string>("twitterText", twitterText));
            Param.Add(new KeyValuePair<string, string>("profile", profile));
            HttpResponseMessage respon = await WebApiReq.PostReq("/api/SocialMessages/PluginComposemessage", Param, "", "", _appSettings.ApiDomain);
            if (respon.IsSuccessStatusCode)
            {
                output = await respon.Content.ReadAsStringAsync();
            }
            return Content(output);
        }


        [HttpPost]
        public async Task<IActionResult> PluginScheduleMessage(string scheduleTime, string clientTime)
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user == null)
            {
                return View("Rlogin");
            }
            string profiles = Request.Form["profile"];
            string twitterText = Request.Form["twitterText"];
            string tweetId = Request.Form["tweetId"];
            string tweetUrl = Request.Form["tweetUrl"];
            string facebookText = Request.Form["facebookText"];
            string url = Request.Form["url"];
            string imgUrl = Request.Form["imgUrl"];
            string sdTime = Convert.ToDateTime(scheduleTime).ToString("yyyy-MM-dd HH:mm:ss");
            List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
            Param.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
            Param.Add(new KeyValuePair<string, string>("tweetUrl", tweetUrl));
            Param.Add(new KeyValuePair<string, string>("facebookText", facebookText));
            Param.Add(new KeyValuePair<string, string>("url", url));
            Param.Add(new KeyValuePair<string, string>("imgUrl", imgUrl));
            Param.Add(new KeyValuePair<string, string>("tweetId", tweetId));
            Param.Add(new KeyValuePair<string, string>("twitterText", twitterText));
            Param.Add(new KeyValuePair<string, string>("profile", profiles));
            Param.Add(new KeyValuePair<string, string>("scheduleTime", sdTime));
            Param.Add(new KeyValuePair<string, string>("localscheduleTime", clientTime));
            HttpResponseMessage respon = await WebApiReq.PostReq("/api/SocialMessages/PluginScheduleMessage", Param, "", "", _appSettings.ApiDomain);
            if (respon.IsSuccessStatusCode)
            {
            }
            return Content("");
        }



        [HttpGet]
        public string changeSelectdGroupId(long groupId)
        {
            HttpContext.Session.SetObjectAsJson("selectedGroupId", groupId);
            return "changed";
        }




    

        private async Task logoutsessiondata()
        {
            Domain.Socioboard.Models.SessionHistory session = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.SessionHistory>("revokedata");
            if (session != null)
            {
                List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
                Param.Add(new KeyValuePair<string, string>("systemId", session.systemId));
                Param.Add(new KeyValuePair<string, string>("sessionId", session.id.ToString()));
                HttpResponseMessage respon = await WebApiReq.PostReq("/api/User/RevokeSession", Param, "", "", _appSettings.ApiDomain);
                if (respon.IsSuccessStatusCode)
                {
                    HttpContext.Session.Remove("revokedata");
                }
            }
        }

        [ResponseCache(Duration = 100)]
        [HttpGet]
        public async Task<IActionResult> Revoke(string sessionId)
        {
            HttpContext.Session.Remove("User");
            HttpContext.Session.Remove("selectedGroupId");
            HttpContext.Session.Clear();
            ViewBag.user = null;
            ViewBag.selectedGroupId = null;
            ViewBag.groupProfiles = null;
            List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>();
            Param.Add(new KeyValuePair<string, string>("sessionId", sessionId));
            HttpResponseMessage respon = await WebApiReq.PostReq("/api/User/RevokeSession", Param, "", "", _appSettings.ApiDomain);
            if (respon.IsSuccessStatusCode)
            {
            }

            return Ok();
        }

        [ResponseCache(Duration = 100)]
        [HttpGet]
        public IActionResult AdminLogout()
        {
            HttpContext.Session.Remove("User");
            HttpContext.Session.Remove("selectedGroupId");
            HttpContext.Session.Clear();
            ViewBag.user = null;
            ViewBag.selectedGroupId = null;
            ViewBag.groupProfiles = null;
            //return View("Company");
            //return RedirectToAction("Index", "Company");
            return Ok();
        }

        [HttpGet]
        public IActionResult IsSessionExist()
        {
            string twostep = HttpContext.Session.GetObjectFromJson<string>("twosteplogin");
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user == null)
            {
                return Content("false");
            }
            else if (twostep == "true")
            {
                return Content("TwoStepLogin");
            }
            else
            {
                try
                {
                    if (user.ExpiryDate < DateTime.UtcNow)
                    {
                        // return false;

                    }
                    else if (user.TwostepEnable == true)
                    {
                        // HttpContext.Session.SetObjectAsJson("twosteplogin", "true");
                        return Content("TwoStepLogin");
                    }
                }
                catch (Exception)
                {
                    return Content("false");
                }
                return Content("true");
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserSession()
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            string EmailId = string.Empty;
            string password = string.Empty;
            string sociorevtoken = string.Empty;

            try
            {
                if (user == null)
                {
                    if (Request.Cookies["sociorevtoken"] != null)
                    {
                        sociorevtoken = Request.Cookies["sociorevtoken"].ToString();
                        sociorevtoken = PluginHelper.Base64Decode(sociorevtoken);
                        try
                        {
                            List<KeyValuePair<string, string>> Parame = new List<KeyValuePair<string, string>>();
                            Parame.Add(new KeyValuePair<string, string>("systemId", sociorevtoken));
                            HttpResponseMessage _response = await WebApiReq.PostReq("/api/User/checksociorevtoken", Parame, "", "", _appSettings.ApiDomain);
                            if (_response.IsSuccessStatusCode)
                            {
                                try
                                {
                                    Domain.Socioboard.Models.SessionHistory _session = await _response.Content.ReadAsAsync<Domain.Socioboard.Models.SessionHistory>();
                                    HttpContext.Session.SetObjectAsJson("revokedata", _session);
                                    sociorevtoken = "true";
                                }
                                catch (Exception ex)
                                {
                                    sociorevtoken = "false";
                                }
                            }
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                    if (sociorevtoken == "true")
                    {
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
                                if (user.TwostepEnable == true)
                                {
                                    HttpContext.Session.SetObjectAsJson("twosteplogin", "true");
                                    return Content("false");
                                }
                                return Content("true");
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }

                }
                else
                {
                    return Content("true");
                }
            }
            catch (Exception ex)
            {

            }
            return Content("false");
        }

        [HttpPost]
        public async Task<IActionResult> UserAuth()
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            if (user == null)
            {
                return Json(null);
            }
            else
            {
                try
                {
                    if (user.ExpiryDate < DateTime.UtcNow)
                    {
                        return Json(user);

                    }
                }
                catch (Exception)
                {
                    return Json(null);
                }
                return Json(user);
            }
        }

        [HttpGet]
        public async Task<ActionResult> UpdateUser()
        {
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");

            HttpResponseMessage response = await WebApiReq.GetReq("/api/User/GetUser?Id=" + user.Id, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                user = await response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                HttpContext.Session.SetObjectAsJson("User", user);
            }
            return Json(user);
        }

        public IActionResult Error()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> GroupInvite(string token, string email, long id)
        {

            string res = string.Empty;
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("email", email));
            Parameters.Add(new KeyValuePair<string, string>("code", token));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/GroupMember/ActivateGroupMember", Parameters, "", "", _appSettings.ApiDomain);

            if (response.IsSuccessStatusCode)
            {
                res = await response.Content.ReadAsStringAsync();
                if (res.Equals("updated"))
                {
                    TempData["Success"] = "Added Successfully to team";
                    return RedirectToAction("index", "Home");
                }
                else
                {
                    TempData["Error"] = "Invalid Link.";
                    return RedirectToAction("index", "Home");
                }

            }
            else
            {
                TempData["Error"] = "Error while hiting api";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<ActionResult> ForgotPassword(string emailId, string token)
        {
            string res = string.Empty;
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("emailId", emailId));
            Parameters.Add(new KeyValuePair<string, string>("accessToken", token));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/User/validateforgotpwdToken", Parameters, "", "", _appSettings.ApiDomain);

            if (response.IsSuccessStatusCode)
            {
                res = await response.Content.ReadAsStringAsync();
                if (res.Equals("You can change the password"))
                {
                    TempData["res"] = res;
                    TempData["EmailId"] = emailId;
                    TempData["token"] = token;
                    return RedirectToAction("ResetPassword", "Index");
                }
                else if (res.Equals("Link Expired."))
                {
                    TempData["Error"] = res;
                    return RedirectToAction("Index", "Index");


                }
                else if (res.Equals("Wrong Link"))
                {
                    TempData["Wrong Link"] = res;
                    return RedirectToAction("Index", "Index");

                }
                else
                {
                    TempData["EmailId does not exist"] = "Email id does not exist";
                    return RedirectToAction("Index", "Index");
                }
            }
            else
            {
                TempData["Error"] = "Error while hiting api";
                return RedirectToAction("Index", "Index");
            }

        }


        [HttpGet]
        public async Task<ActionResult> Active(string id, string Token)
        {
            string res = string.Empty;
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("Id", id));
            Parameters.Add(new KeyValuePair<string, string>("Token", Token));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/User/VerifyEmail", Parameters, "", "", _appSettings.ApiDomain);

            HttpResponseMessage userresponse = await WebApiReq.GetReq("/api/User/GetUser?Id=" + id, "", "", _appSettings.ApiDomain);
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    //res = await response.Content.ReadAsStringAsync();
                    //TempData["Error"] = res;
                    //return RedirectToAction("index", "index");
                    if (userresponse.IsSuccessStatusCode)
                    {
                        string package = string.Empty;
                        User user = await userresponse.Content.ReadAsAsync<User>();
                        if (user != null)
                        {
                            HttpContext.Session.SetObjectAsJson("User", user);
                        }
                        else
                        {
                            return RedirectToAction("index", "index");
                        }


                        List<KeyValuePair<string, string>> Parameter = new List<KeyValuePair<string, string>>();
                        if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Free)
                        {
                            package = "Free";
                        }
                        else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Deluxe)
                        {
                            package = "Deluxe";

                        }
                        else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Premium)
                        {
                            package = "Premium";

                        }
                        else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Topaz)
                        {
                            package = "Topaz";

                        }
                        else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Platinum)
                        {
                            package = "Platinum";

                        }
                        else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Gold)
                        {
                            package = "Gold";

                        }
                        else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Ruby)
                        {
                            package = "Ruby";

                        }
                        else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Standard)
                        {
                            package = "Standard";

                        }

                        if (package != "Free")
                        {
                            Parameter.Add(new KeyValuePair<string, string>("packagename", package));
                            HttpResponseMessage respons = await WebApiReq.PostReq("/api/PaymentTransaction/GetPackage", Parameter, "", "", _appSettings.ApiDomain);
                            if (respons.IsSuccessStatusCode)
                            {
                                try
                                {
                                    if ((user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.notadded || user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.inprogress) && (user.AccountType != Domain.Socioboard.Enum.SBAccountType.Free))
                                    {

                                        Domain.Socioboard.Models.Package _Package = await respons.Content.ReadAsAsync<Domain.Socioboard.Models.Package>();
                                        HttpContext.Session.SetObjectAsJson("Package", _Package);
                                        if (user.PaymentType == Domain.Socioboard.Enum.PaymentType.paypal)
                                        {
                                            HttpContext.Session.SetObjectAsJson("paymentsession", true);
                                            return Redirect(Helpers.Payment.RecurringPaymentWithPayPal(_Package.amount, _Package.packagename, user.FirstName + " " + user.LastName, user.PhoneNumber, user.EmailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.callBackUrl, _appSettings.cancelurl, _appSettings.notifyUrl, "", _appSettings.PaypalURL));
                                        }
                                        else
                                        {
                                            HttpContext.Session.SetObjectAsJson("paymentsession", true);
                                            return RedirectToAction("paymentWithPayUMoney", "Index", new { contesnt = false });
                                        }
                                    }
                                    else
                                    {
                                        return RedirectToAction("Index", "Home");
                                    }
                                }
                                catch (Exception ex) { }

                            }
                        }
                        else
                        {
                            List<KeyValuePair<string, string>> _Parameters = new List<KeyValuePair<string, string>>();
                            _Parameters.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
                            HttpResponseMessage _response = await WebApiReq.PostReq("/api/User/UpdateFreeUser", _Parameters, "", "", _appSettings.ApiDomain);
                            if (response.IsSuccessStatusCode)
                            {
                                try
                                {
                                    Domain.Socioboard.Models.User _user = await _response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                                    HttpContext.Session.SetObjectAsJson("User", _user);
                                    return RedirectToAction("Index", "Home");
                                }
                                catch { }
                            }
                        }
                        return RedirectToAction("index", "index");
                    }
                }
                else
                {
                    TempData["Error"] = "Error while hiting api";
                    return RedirectToAction("index", "index");
                }
                return RedirectToAction("index", "index");
            }
            catch (Exception ex)
            {
                return RedirectToAction("index", "index");
            }

        }


        [HttpGet]
        public async Task<ActionResult> PayPalAccount(string emailId, bool IsLogin)
        {
            var userresponse = await WebApiReq.GetReq("/api/User/GetUserData?emailId=" + emailId, "", "", _appSettings.ApiDomain);

            if (userresponse.IsSuccessStatusCode)
            {
                var package = string.Empty;
                var user = await userresponse.Content.ReadAsAsync<User>();
                HttpContext.Session.SetObjectAsJson("User", user);

                switch (user.AccountType)
                {
                    case Domain.Socioboard.Enum.SBAccountType.Free:
                        package = "Free";
                        break;
                    case Domain.Socioboard.Enum.SBAccountType.Deluxe:
                        package = "Deluxe";
                        break;
                    case Domain.Socioboard.Enum.SBAccountType.Premium:
                        package = "Premium";
                        break;
                    case Domain.Socioboard.Enum.SBAccountType.Topaz:
                        package = "Topaz";
                        break;
                    case Domain.Socioboard.Enum.SBAccountType.Platinum:
                        package = "Platinum";
                        break;
                    case Domain.Socioboard.Enum.SBAccountType.Gold:
                        package = "Gold";
                        break;
                    case Domain.Socioboard.Enum.SBAccountType.Ruby:
                        package = "Ruby";
                        break;
                    case Domain.Socioboard.Enum.SBAccountType.Standard:
                        package = "Standard";
                        break;
                }

                var parameter = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("packagename", package)
                };

                var paymentResponse = await WebApiReq.PostReq("/api/PaymentTransaction/GetPackage", parameter, "", "", _appSettings.ApiDomain);

                if (paymentResponse.IsSuccessStatusCode)
                {
                    try
                    {
                        var sessionPackage = await paymentResponse.Content.ReadAsAsync<Package>();
                        HttpContext.Session.SetObjectAsJson("Package", sessionPackage);

                        if (!IsLogin)
                        {
                            HttpContext.Session.SetObjectAsJson("paymentsession", true);
                            if (user.PaymentType == Domain.Socioboard.Enum.PaymentType.paypal)
                            {                             
                                var paypalUrl = Payment.RecurringPaymentWithPayPal(sessionPackage.amount, sessionPackage.packagename, user.FirstName + " " + user.LastName, user.PhoneNumber, user.EmailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.callBackUrl, _appSettings.cancelurl, _appSettings.notifyUrl, "", _appSettings.PaypalURL);
                                return Content(paypalUrl);
                            }
                            else
                            {
                                return RedirectToAction("paymentWithPayUMoney", "Index");
                            }
                        }
                        else
                        {
                            HttpContext.Session.SetObjectAsJson("paymentsession", true);
                            if (user.PaymentType == Domain.Socioboard.Enum.PaymentType.paypal)
                            {
                             
                                return Redirect(Payment.RecurringPaymentWithPayPal(sessionPackage.amount, sessionPackage.packagename, user.FirstName + " " + user.LastName, user.PhoneNumber, user.EmailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.callBackUrl, _appSettings.cancelurl, _appSettings.notifyUrl, "", _appSettings.PaypalURL));
                            }
                            else
                            {
                                return RedirectToAction("paymentWithPayUMoney", "Index", new { contesnt = false });
                            }
                        }
                    }
                    catch (Exception ex) { }

                }

            }
            return Content("");
        }


        BaseFont f_cb = BaseFont.CreateFont("c:\\windows\\fonts\\calibrib.ttf", BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
        BaseFont f_cn = BaseFont.CreateFont("c:\\windows\\fonts\\calibri.ttf", BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
        private void writeText(PdfContentByte cb, string Text, int X, int Y, BaseFont font, int Size)
        {
            cb.SetFontAndSize(font, Size);
            cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, Text, X, Y, 0);
        }

        private PdfTemplate PdfFooter(PdfContentByte cb)//, DataRow drFoot)
        {
            // Create the template and assign height
            PdfTemplate tmpFooter = cb.CreateTemplate(580, 100);
            // Move to the bottom left corner of the template
            tmpFooter.MoveTo(1, 1);
            // Place the footer content
            tmpFooter.Stroke();
            // Begin writing the footer
            tmpFooter.BeginText();
            // Set the font and size
            tmpFooter.SetFontAndSize(f_cn, 8);
            // Write out details from the payee table


            string supplier = "Socioboard";
            string address1 = "TV Complex, 2, 60 Feet Rd, 6th Block";
            string address2 = " Koramangala, Bengaluru, Karnataka";
            string zip = "560034";

            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, supplier.ToString(), 0, 50, 0);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, address1.ToString(), 0, 42, 0);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, address2.ToString(), 0, 34, 0);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, zip.ToString(), 0, 26, 0);

            // Bold text for ther headers
            tmpFooter.SetFontAndSize(f_cb, 8);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "Phone :", 215, 50, 0);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "Mail   :", 215, 42, 0);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "Web   :", 215, 34, 0);
            //tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "Legal info", 400, 53, 0);
            // Regular text for infomation fields

            string phone = "+91 7406317771";
            string mail = "sumit@socioboard.com";
            string web = "https://www.socioboard.com";
            tmpFooter.SetFontAndSize(f_cn, 8);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, phone.ToString(), 265, 50, 0);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, mail.ToString(), 265, 42, 0);
            tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, web.ToString(), 265, 34, 0);
            //tmpFooter.ShowTextAligned(PdfContentByte.ALIGN_LEFT, drFoot["xtrainfo"].ToString(), 400, 45, 0);
            // End text
            tmpFooter.EndText();
            // Stamp a line above the page footer
            cb.SetLineWidth(0f);
            cb.MoveTo(30, 60);
            cb.LineTo(570, 60);
            cb.Stroke();
            // Return the footer template
            return tmpFooter;
        }

        public async Task<FileContentResult> generatePaymentInvoicePdf(string id, string package)
        {
            //string csv = "GroupName, ProfileName, Message,postImgUrl";
            //return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "linkedInLeads_" + (DateTime.Now.Ticks).ToString() + ".csv");
            Int32 invoiceId = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string filename = "Invoice_" + invoiceId + ".pdf";
            string ReportFilePath = _appEnv.WebRootPath + "\\contents\\socioboard\\Invoice\\" + filename;
            List<KeyValuePair<string, string>> Parameter = new List<KeyValuePair<string, string>>();
            Parameter.Add(new KeyValuePair<string, string>("id", id));
            HttpResponseMessage respons = await WebApiReq.PostReq("/api/PaymentTransaction/GetPaymentTransactiondata", Parameter, "", "", _appSettings.ApiDomain);
            if (respons.IsSuccessStatusCode)
            {
                Domain.Socioboard.Models.PaymentTransaction _PaymentTransaction = await respons.Content.ReadAsAsync<Domain.Socioboard.Models.PaymentTransaction>();

                try
                {
                    using (System.IO.FileStream fs = new FileStream(_appEnv.WebRootPath + "\\contents\\socioboard\\Invoice\\" + "Invoice_" + invoiceId + ".pdf", FileMode.Create))
                    {
                        Document document = new iTextSharp.text.Document(PageSize.A4, 25, 25, 30, 1);

                        PdfWriter writer = PdfWriter.GetInstance(document, fs);

                        // Add meta information to the document
                        document.AddAuthor("Jitendra Kumar");
                        document.AddCreator("Sample application using iTestSharp");
                        document.AddKeywords("User Invoice");
                        document.AddSubject("Invoice");
                        document.AddTitle("User invoice");

                        // Open the document to enable you to write to the document
                        document.Open();

                        // Makes it possible to add text to a specific place in the document using 
                        // a X & Y placement syntax.
                        PdfContentByte cb = writer.DirectContent;
                        // Add a footer template to the document
                        ///cb.AddTemplate(PdfFooter(cb, drPayee), 30, 1);
                       // cb.AddTemplate(PdfFooter(cb), 30, 1);

                        // Add a logo to the invoice                  

                        iTextSharp.text.Image png = iTextSharp.text.Image.GetInstance("https://www.socioboard.com/contents/socioboard/images/Socioboard.png");
                        png.ScaleAbsolute(200, 55);
                        png.SetAbsolutePosition(40, 750);
                        cb.AddImage(png);

                        // First we must activate writing
                        cb.BeginText();

                        // First we write out the header information

                        //User Value
                        string invoiceType = "";

                        DateTime invoiceDate = DateTime.Now;
                        DateTime dueDate = DateTime.Now.AddMonths(1);



                        // Start with the invoice type header
                        writeText(cb, invoiceType.ToString(), 350, 820, f_cb, 14);
                        // HEader details; invoice number, invoice date, due date and customer Id
                        writeText(cb, "Invoice No    :", 350, 800, f_cb, 10);
                        writeText(cb, invoiceId.ToString(), 420, 800, f_cn, 10);
                        writeText(cb, "Invoice date :", 350, 788, f_cb, 10);
                        writeText(cb, invoiceDate.ToString("dd/MM/yyyy"), 420, 788, f_cn, 10);
                        writeText(cb, "Due date      :", 350, 776, f_cb, 10);
                        writeText(cb, dueDate.ToString("dd/MM/yyyy"), 420, 776, f_cn, 10);
                        string delCustomerName = _PaymentTransaction.Payername;
                        string email = _PaymentTransaction.payeremail;
                        string media = _PaymentTransaction.media;

                        int left_margin = 40;
                        int top_margin = 720;
                        writeText(cb, "User Details :-", left_margin, top_margin, f_cb, 10);
                        writeText(cb, delCustomerName.ToString(), left_margin, top_margin - 12, f_cn, 10);
                        writeText(cb, email.ToString(), left_margin, top_margin - 24, f_cn, 10);
                        writeText(cb, media.ToString(), left_margin, top_margin - 36, f_cn, 10);
                        cb.EndText();
                        // Separate the header from the rows with a line
                        // Draw a line by setting the line width and position
                        cb.SetLineWidth(0f);
                        cb.MoveTo(40, 670);
                        cb.LineTo(560, 670);
                        cb.Stroke();
                        // Don't forget to call the BeginText() method when done doing graphics!
                        cb.BeginText();

                        // Before we write the lines, it's good to assign a "last position to write"
                        // variable to validate against if we need to make a page break while outputting.
                        // Change it to 510 to write to test a page break; the fourth line on a new page
                        int lastwriteposition = 100;

                        // Loop thru the rows in the rows table
                        // Start by writing out the line headers
                        top_margin = 645;
                        left_margin = 40;
                        // Line headers
                        writeText(cb, "Package", left_margin, top_margin, f_cb, 10);
                        writeText(cb, "Description", left_margin + 70, top_margin, f_cb, 10);
                        cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT, "Payment Date", left_margin + 285, top_margin, 0);
                        writeText(cb, "TransactionId", left_margin + 322, top_margin, f_cb, 10);
                        cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT, "PaymentStatus ", left_margin + 455, top_margin, 0);
                        writeText(cb, "Paid Amount", left_margin + 475, top_margin, f_cb, 10);


                        cb.EndText();
                        // Separate the header from the rows with a line
                        // Draw a line by setting the line width and position
                        cb.SetLineWidth(0f);
                        cb.MoveTo(40, 630);
                        cb.LineTo(560, 630);
                        cb.Stroke();
                        // Don't forget to call the BeginText() method when done doing graphics!
                        cb.BeginText();
                        // First item line position starts here
                        top_margin = 600;
                        string Package = package;
                        string Description = "This package provide .... facilities";
                        DateTime Payment_Date = _PaymentTransaction.paymentdate;
                        string PaymentStatus = _PaymentTransaction.paymentstatus;
                        string Paid_Amount = _PaymentTransaction.amount;
                        string TransactionId = _PaymentTransaction.paymentId;


                        writeText(cb, Package.ToString(), left_margin, top_margin, f_cn, 10);
                        writeText(cb, Description.ToString(), left_margin + 65, top_margin, f_cn, 10);
                        cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT, Payment_Date.ToString("dd/MM/yyyy"), left_margin + 285, top_margin, 0);
                        writeText(cb, TransactionId.ToString(), left_margin + 322, top_margin, f_cn, 10);
                        cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT, PaymentStatus.ToString(), left_margin + 455, top_margin, 0);
                        writeText(cb, Paid_Amount.ToString(), left_margin + 475, top_margin, f_cn, 10);
                        cb.EndText();

                        // Close the document, the writer and the filestream!
                        document.Close();
                        writer.Close();
                        fs.Close();

                    }
                }
                catch (Exception error)
                {
                }

                // return File(pdfBytes, "application/pdf", filename);

            }
            byte[] pdfBytes = System.IO.File.ReadAllBytes(ReportFilePath);
            if (System.IO.File.Exists(ReportFilePath))
            {
                System.IO.File.Delete(ReportFilePath);
            }
            // return new FileStreamResult(new MemoryStream(pdfBytes), "application / pdf") { FileDownloadName = filename };

            return File(pdfBytes, "application/pdf", filename);
        }



        [HttpGet]
        public async Task<ActionResult> ActiveYoutubeGroup(string Token)
        {
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("Token", Token));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/YoutubeGroup/ValidateEmail", Parameters, "", "", _appSettings.ApiDomain);

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public async Task<ActionResult> BluesnapAccount(string emailId, bool IsLogin)
        {
            HttpResponseMessage userresponse = await WebApiReq.GetReq("/api/User/GetUserData?emailId=" + emailId, "", "", _appSettings.ApiDomain);
            if (userresponse.IsSuccessStatusCode)
            {
                string package = string.Empty;
                User user = await userresponse.Content.ReadAsAsync<User>();
                HttpContext.Session.SetObjectAsJson("User", user);
                if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Free)
                {
                    package = "Free";
                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Deluxe)
                {
                    package = "Deluxe";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Premium)
                {
                    package = "Premium";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Topaz)
                {
                    package = "Topaz";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Platinum)
                {
                    package = "Platinum";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Gold)
                {
                    package = "Gold";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Ruby)
                {
                    package = "Ruby";

                }
                else if (user.AccountType == Domain.Socioboard.Enum.SBAccountType.Standard)
                {
                    package = "Standard";

                }
                List<KeyValuePair<string, string>> Parameter = new List<KeyValuePair<string, string>>();
                Parameter.Add(new KeyValuePair<string, string>("packagename", package));
                HttpResponseMessage respons = await WebApiReq.PostReq("/api/PaymentTransaction/GetPackage", Parameter, "", "", _appSettings.ApiDomain);
                if (respons.IsSuccessStatusCode)
                {
                    try
                    {
                        Domain.Socioboard.Models.Package _Package = await respons.Content.ReadAsAsync<Domain.Socioboard.Models.Package>();
                        HttpContext.Session.SetObjectAsJson("Package", _Package);
                        if (!IsLogin)
                        {
                            if (user.PaymentType == Domain.Socioboard.Enum.PaymentType.paypal)
                            {
                                HttpContext.Session.SetObjectAsJson("paymentsession", true);
                                string paypalUrl = Helpers.Payment.RecurringPaymentWithPayPal(_Package.amount, _Package.packagename, user.FirstName + " " + user.LastName, user.PhoneNumber, user.EmailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.callBackUrl, _appSettings.cancelurl, _appSettings.notifyUrl, "", _appSettings.PaypalURL);
                                return Content(paypalUrl);
                            }
                            else
                            {
                                HttpContext.Session.SetObjectAsJson("paymentsession", true);
                                return RedirectToAction("paymentWithPayUMoney", "Index");
                            }
                        }
                        else
                        {
                            if (user.PaymentType == Domain.Socioboard.Enum.PaymentType.paypal)
                            {
                                HttpContext.Session.SetObjectAsJson("paymentsession", true);
                                return Redirect(Helpers.Payment.RecurringPaymentWithPayPal(_Package.amount, _Package.packagename, user.FirstName + " " + user.LastName, user.PhoneNumber, user.EmailId, "USD", _appSettings.paypalemail, _appSettings.callBackUrl, _appSettings.failUrl, _appSettings.callBackUrl, _appSettings.cancelurl, _appSettings.notifyUrl, "", _appSettings.PaypalURL));
                            }
                            else
                            {
                                HttpContext.Session.SetObjectAsJson("paymentsession", true);
                                return RedirectToAction("paymentWithPayUMoney", "Index", new { contesnt = false });
                            }
                        }
                    }
                    catch (Exception ex) { }

                }

            }
            return Content("");
        }


        [HttpPost]
        public async Task<ActionResult> bluesnapCardPaymentEncp()
        {
            string content = await new StreamReader(Request.Body).ReadToEndAsync();
            content = (("{\"" + content).Replace("=","\":\"").Replace("&","\",\"") + "\"}").Replace("exp-year", "expyear").Replace("exp-month", "expmonth").Replace("cardholder-name", "cardholdername");

            creditCard creditCardObj= JsonConvert.DeserializeObject<creditCard>(content);
            User user = new Domain.Socioboard.Models.User();
            try
            {
                user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            }
            catch
            {
                user = null;
            }
            string planId = "";

            if (user != null)
            {
                string tempPlan = user.AccountType.ToString();
                //object value = Enum.Parse(Domain.Socioboard.Enum.SBAccountTypeBlueSnap, tempPlan);
                Domain.Socioboard.Enum.SBAccountTypeBlueSnap myStatus;
                Enum.TryParse(tempPlan, out myStatus);
                planId = ((int)myStatus).ToString();


                string xmlData = "<?xml version='1.0'?>" +
                                "<recurring-subscription xmlns='http://ws.plimus.com'>" +
                                "<plan-id>" + planId + "</plan-id>" +
                                "<payer-info>" +
                                "<first-name>" + creditCardObj.cardholdername + "</first-name>" +
                                "<last-name>" + creditCardObj.cardholdername + "</last-name>" +
                                "<zip>12345</zip>" +
                                "<phone>1234567890</phone>" +
                                "</payer-info>" +
                                "<payment-source>" +
                                "<credit-card-info>" +
                                "<credit-card>" +
                                "<encrypted-card-number>" + creditCardObj.encryptedCreditCard + "</encrypted-card-number>" +
                                "<encrypted-security-code>" + creditCardObj.encryptedCvv + "</encrypted-security-code>" +
                                "<expiration-month>" + creditCardObj.expmonth + "</expiration-month>" +
                                "<expiration-year>" + creditCardObj.expyear + "</expiration-year>" +
                                "</credit-card>" +
                                "</credit-card-info>" +
                                "</payment-source>" +
                                "<transaction-fraud-info>" +
                                "<fraud-session-id>1234</fraud-session-id>" +
                                "</transaction-fraud-info>" +
                                "</recurring-subscription>";

                List<KeyValuePair<string, string>> Parameter = new List<KeyValuePair<string, string>>();
                Parameter.Add(new KeyValuePair<string, string>("XMLData", xmlData));
                Parameter.Add(new KeyValuePair<string, string>("emailId", user.EmailId));
                HttpResponseMessage respons = await WebApiReq.PostReq("/api/PaymentTransaction/PostBlueSnapSubscription", Parameter, "", "", _appSettings.ApiDomain);

                if(respons.StatusCode== HttpStatusCode.OK)
                {
                    HttpResponseMessage userresponse = await WebApiReq.GetReq("/api/User/GetUserData?emailId=" + user.EmailId, "", "", _appSettings.ApiDomain);
                    if (userresponse.IsSuccessStatusCode)
                    {
                        User userTemp = await userresponse.Content.ReadAsAsync<User>();
                        HttpContext.Session.SetObjectAsJson("User", userTemp);
                    }
                    return RedirectToAction("SignIn", "Index");
                }

            }
            else
            {
                ViewBag.errorCard = "Error";
                return View("payBlueSnap");
            }
            ViewBag.errorCard = "Error";
            return View("payBlueSnap");
        }

        public class creditCard
        {
            public string cardholdername { get; set; }
            public string expmonth { get; set; }
            public string expyear { get; set; }
            public string ccLast4Digits { get; set; }
            public string encryptedCreditCard { get; set; }
            public string encryptedCvv { get; set; }
        }

        //Blue snap payment page
        [HttpGet]
        public async Task<IActionResult> payBlueSnap(string emailId)
        {
            HttpResponseMessage userresponse = await WebApiReq.GetReq("/api/User/GetUserData?emailId=" + emailId, "", "", _appSettings.ApiDomain);
            if (userresponse.IsSuccessStatusCode)
            {
                User user = await userresponse.Content.ReadAsAsync<User>();
                HttpContext.Session.SetObjectAsJson("User", user);
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ipnBluesnap(string transactionType, long subscriptionId)
        {
            try
            {
                _logger.LogInformation("========================================transactiontype======================================");
                _logger.LogInformation(transactionType);
                _logger.LogInformation("========================================subscriptionId======================================");
                _logger.LogInformation(subscriptionId.ToString());



                List<KeyValuePair<string, string>> Parameter = new List<KeyValuePair<string, string>>();
                Parameter.Add(new KeyValuePair<string, string>("subscriptionId", subscriptionId.ToString()));

                HttpResponseMessage respons = await WebApiReq.PostReq("/api/PaymentTransaction/GetBlueSnapSubscription", Parameter, "", "", _appSettings.ApiDomain);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("========================================exception======================================");

                _logger.LogInformation(ex.Message);
            }



            return Ok();
        }

    }
}
