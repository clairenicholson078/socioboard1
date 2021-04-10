﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Socioboard.Extensions;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Socioboard.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Socioboard.Controllers
{
    public class FacebookManagerController : Controller
    {
        private Helpers.AppSettings _appSettings;
        private readonly ILogger _logger;

        public FacebookManagerController(ILogger<FacebookManagerController> logger, Microsoft.Extensions.Options.IOptions<Helpers.AppSettings> settings)
        {
            _appSettings = settings.Value;
            _logger = logger;
        }

        //public override void OnActionExecuting(ActionExecutingContext filterContext)
        //{
        //    Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
        //    Domain.Socioboard.Models.SessionHistory session = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.SessionHistory>("revokedata");
        //    if (session != null)
        //    {
        //        SortedDictionary<string, string> strdi = new SortedDictionary<string, string>();
        //        strdi.Add("systemId", session.systemId);
        //        string respo = CustomHttpWebRequest.HttpWebRequest("POST", "/api/User/checksociorevtoken", strdi, _appSettings.ApiDomain);
        //        if (respo != "false")
        //        {
        //            if (user != null)
        //            {
        //                SortedDictionary<string, string> strdic = new SortedDictionary<string, string>();
        //                strdic.Add("UserName", user.EmailId);
        //                if (string.IsNullOrEmpty(user.Password))
        //                {
        //                    strdic.Add("Password", "sociallogin");
        //                }
        //                else
        //                {
        //                    strdic.Add("Password", user.Password);
        //                }


        //                string response = CustomHttpWebRequest.HttpWebRequest("POST", "/api/User/CheckUserLogin", strdic, _appSettings.ApiDomain);

        //                if (!string.IsNullOrEmpty(response))
        //                {
        //                    Domain.Socioboard.Models.User _user = Newtonsoft.Json.JsonConvert.DeserializeObject<Domain.Socioboard.Models.User>(response);
        //                    HttpContext.Session.SetObjectAsJson("User", _user);
        //                }
        //                else
        //                {
        //                    HttpContext.Session.Remove("User");
        //                    HttpContext.Session.Remove("selectedGroupId");
        //                    HttpContext.Session.Clear();
        //                    HttpContext.Session.Remove("revokedata");
        //                }
        //            }
        //        }
        //        else
        //        {
        //            HttpContext.Session.Remove("User");
        //            HttpContext.Session.Remove("selectedGroupId");
        //            HttpContext.Session.Clear();
        //            HttpContext.Session.Remove("revokedata");
        //        }

        //    }
        //    base.OnActionExecuting(filterContext);
        //}

        [HttpGet]
        public ActionResult getFbLoginUrl(string plan)
        {
            HttpContext.Session.SetObjectAsJson("fblogin", "Fb_Login");
            if (!string.IsNullOrEmpty(plan))
            {
                HttpContext.Session.SetObjectAsJson("RegisterPlan", plan);
            }
            return Content(Socioboard.Facebook.Auth.Authentication.GetFacebookRedirectLink(_appSettings.FacebookAuthUrl, _appSettings.FacebookClientId, _appSettings.FacebookRedirectUrl));
        }
        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult> Facebook(string code)
        {
            string fbLogin = HttpContext.Session.GetObjectFromJson<string>("fblogin");
            string fbSocial = HttpContext.Session.GetObjectFromJson<string>("fbSocial");
            //string fbtype = HttpContext.Session.GetObjectFromJson<string>("fbprofiletype");
            string plan = HttpContext.Session.GetObjectFromJson<string>("RegisterPlan");


            if (!string.IsNullOrEmpty(fbLogin) && fbLogin.Equals("Fb_Login"))
            {
                HttpContext.Session.SetObjectAsJson("fblogin", null);
                string accessToken = string.Empty;
                try
                {
                    accessToken = Socioboard.Facebook.Auth.Authentication.GetAccessToken(_appSettings.FacebookClientId, _appSettings.FacebookRedirectUrl, _appSettings.FacebookClientSecretKey, code);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    return Content(ex.Message);
                }

                Domain.Socioboard.Models.User user = null;
                List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
                Parameters.Add(new KeyValuePair<string, string>("AccessToken", accessToken));
                Parameters.Add(new KeyValuePair<string, string>("accType", plan));
                HttpResponseMessage response = await WebApiReq.PostReq("/api/User/FacebookLogin", Parameters, "", "", _appSettings.ApiDomain);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        user = await response.Content.ReadAsAsync<Domain.Socioboard.Models.User>();
                        HttpContext.Session.SetObjectAsJson("User", user);
                        if (user.ExpiryDate < DateTime.UtcNow && user.AccountType == Domain.Socioboard.Enum.SBAccountType.Free)
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else if ((user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.notadded || user.PayPalAccountStatus == Domain.Socioboard.Enum.PayPalAccountStatus.inprogress) && (user.AccountType != Domain.Socioboard.Enum.SBAccountType.Free))
                        {
                            if (user.PaymentType == Domain.Socioboard.Enum.PaymentType.paypal)
                            {
                                return RedirectToAction("PayPalAccount", "Home", new { emailId = user.EmailId, IsLogin = true });
                            }
                            else
                            {
                                return RedirectToAction("paymentWithPayUMoney", "Index");
                            }
                            //return RedirectToAction("PayPalAccount", "Home", new { emailId = user.EmailId, IsLogin = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            TempData["Error"] = await response.Content.ReadAsStringAsync();
                            return RedirectToAction("Index", "Index");
                        }
                        catch (Exception exe) { }
                    }

                }
                return RedirectToAction("Index", "Home");
            }
            else if (fbSocial.Equals("Fb_Account"))
            {
                HttpContext.Session.SetObjectAsJson("fbSocial", null);
                return RedirectToAction("AddFbAcc", "FacebookManager", new { code = code });
            }
            else if (fbSocial.Equals("Fb_Page"))
            {
                HttpContext.Session.SetObjectAsJson("fbSocial", null);
                return RedirectToAction("AddFbPage", "FacebookManager", new { code = code });
            }
            else if (fbSocial.Equals("0"))
            {
                HttpContext.Session.SetObjectAsJson("fbSocial", fbSocial);
                return RedirectToAction("ReConnectAcc", "FacebookManager", new { code = code });
            }
            else if (fbSocial.Equals("null"))
            {
                HttpContext.Session.SetObjectAsJson("fbSocial", fbSocial);
                return RedirectToAction("ReConnectfbpage", "FacebookManager", new { code = code });
            }
            return RedirectToAction("Index", "Index");
        }

        [Route("socioboard/recfbcont/")]
        [HttpGet("recfbcont")]
        public ActionResult recfbcont(string id, string fbprofileType)
        {

            if (fbprofileType == "0")
            {
                try
                {

                    HttpContext.Session.SetObjectAsJson("fbSocial", fbprofileType);

                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                HttpContext.Session.SetObjectAsJson("fbSocial", fbprofileType);
            }

            HttpContext.Session.SetObjectAsJson("profileId", id);


            return Content(Socioboard.Facebook.Auth.Authentication.GetFacebookRedirectLink(_appSettings.FacebookAuthUrl, _appSettings.FacebookClientId, _appSettings.FacebookRedirectUrl));
        }

        [HttpGet]
        public async Task<ActionResult> AddFacebookAcc(string Op)
        {
            int count = 0;
            string profileCount = "";
            List<Domain.Socioboard.Models.Groups> groups = new List<Domain.Socioboard.Models.Groups>();
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            HttpResponseMessage response = await WebApiReq.GetReq("/api/Groups/GetUserGroups?userId=" + user.Id, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                  groups   = await response.Content.ReadAsAsync<List<Domain.Socioboard.Models.Groups>>();
                }
                catch { }
            }
            string sessionSelectedGroupId = HttpContext.Session.GetObjectFromJson<string>("selectedGroupId");
            if (!string.IsNullOrEmpty(sessionSelectedGroupId))
            {
                HttpResponseMessage groupProfilesResponse = await WebApiReq.GetReq("/api/GroupProfiles/GetGroupProfiles?groupId=" + sessionSelectedGroupId, "", "", _appSettings.ApiDomain);
                if (groupProfilesResponse.IsSuccessStatusCode)
                {
                    List<Domain.Socioboard.Models.Groupprofiles> groupProfiles = await groupProfilesResponse.Content.ReadAsAsync<List<Domain.Socioboard.Models.Groupprofiles>>();
                    profileCount = groupProfiles.Count.ToString();
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
                    profileCount = groupProfiles.Count.ToString();
                }
            }
            //  string profileCount = await ProfilesHelper.GetUserProfileCount(user.Id, _appSettings, _logger);
            try
            {
                count = Convert.ToInt32(profileCount);

            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error while getting profile count.";
                return RedirectToAction("Index", "Home");
            }

            int MaxCount = Domain.Socioboard.Helpers.SBHelper.GetMaxProfileCount(user.AccountType);
            if (count >= MaxCount)
            {
                TempData["Error"] = "Max profile Count reached.";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                if (string.IsNullOrEmpty(Op))
                {
                    HttpContext.Session.SetObjectAsJson("fbSocial", "Fb_Account");
                }
                else
                {
                    HttpContext.Session.SetObjectAsJson("fbSocial", "Fb_Page");

                }
                return Redirect(Socioboard.Facebook.Auth.Authentication.GetFacebookRedirectLink(_appSettings.FacebookAuthUrl, _appSettings.FacebookClientId, _appSettings.FacebookRedirectUrl));
            }
        }
        public async Task<ActionResult> AddFbAcc(string code)
        {
            string accessToken = string.Empty;
            try
            {
                accessToken = Socioboard.Facebook.Auth.Authentication.GetAccessToken(_appSettings.FacebookClientId, _appSettings.FacebookRedirectUrl, _appSettings.FacebookClientSecretKey, code);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                _logger.LogError(ex.StackTrace);
                TempData["Error"] = "Issue with Token";
                return RedirectToAction("Index", "Home");
            }
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            string groupId = HttpContext.Session.GetObjectFromJson<string>("selectedGroupId");
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("accessToken", accessToken));
            Parameters.Add(new KeyValuePair<string, string>("groupId", groupId));
            Parameters.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/Facebook/AddFacebookAccount", Parameters, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["Error"] = "Error while hitting api.";
                return RedirectToAction("Index", "Home");
            }
        }
        public async Task<ActionResult> AddFbPage(string code)
        {
            string accessToken = string.Empty;
            try
            {
                accessToken = Socioboard.Facebook.Auth.Authentication.GetAccessToken(_appSettings.FacebookClientId, _appSettings.FacebookRedirectUrl, _appSettings.FacebookClientSecretKey, code);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                _logger.LogError(ex.StackTrace);
                TempData["Error"] = "Issue with Token";
                return RedirectToAction("Index", "Home");
            }
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            string groupId = HttpContext.Session.GetObjectFromJson<string>("selectedGroupId");
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("accesstoken", accessToken));
            Parameters.Add(new KeyValuePair<string, string>("groupId", groupId));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/Facebook/GetFacebookPages", Parameters, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                List<Domain.Socioboard.Models.Facebookpage> lstpages = await response.Content.ReadAsAsync<List<Domain.Socioboard.Models.Facebookpage>>();
                if (lstpages.Count > 0)
                {
                    TempData["fbPages"] = Newtonsoft.Json.JsonConvert.SerializeObject(lstpages);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Error"] = "No page linked with this account";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                TempData["Error"] = "Error while hitting api.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<ActionResult> ReConnectAcc(string code)
        {
            string accessToken = string.Empty;
            try
            {
                accessToken = Socioboard.Facebook.Auth.Authentication.GetAccessToken(_appSettings.FacebookClientId, _appSettings.FacebookRedirectUrl, _appSettings.FacebookClientSecretKey, code);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                _logger.LogError(ex.StackTrace);
                TempData["Error"] = "Issue with Token";
                return RedirectToAction("Index", "Home");
            }
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            string groupId = HttpContext.Session.GetObjectFromJson<string>("selectedGroupId");

            List<KeyValuePair<string, string>> RecParameters = new List<KeyValuePair<string, string>>();
            string profileId = HttpContext.Session.GetObjectFromJson<string>("profileId");//profileId
            string fbreconnect = HttpContext.Session.GetObjectFromJson<string>("fbSocial");//fbSocial

            RecParameters.Add(new KeyValuePair<string, string>("accessToken", accessToken));
            RecParameters.Add(new KeyValuePair<string, string>("groupId", groupId));
            RecParameters.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
            RecParameters.Add(new KeyValuePair<string, string>("reconnect", fbreconnect));
            RecParameters.Add(new KeyValuePair<string, string>("profileId", profileId));



            HttpResponseMessage response = await WebApiReq.PostReq("/api/Facebook/ReconnectFbAccount", RecParameters, "", "", _appSettings.ApiDomain);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["Error"] = "Error while hitting api.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<ActionResult> ReConnectfbpage(string code)
        {
            string accessToken = string.Empty;
            try
            {
                accessToken = Socioboard.Facebook.Auth.Authentication.GetAccessToken(_appSettings.FacebookClientId, _appSettings.FacebookRedirectUrl, _appSettings.FacebookClientSecretKey, code);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                _logger.LogError(ex.StackTrace);
                TempData["Error"] = "Issue with Token";
                return RedirectToAction("Index", "Home");
            }
            Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
            string groupId = HttpContext.Session.GetObjectFromJson<string>("selectedGroupId");
            string profileId = HttpContext.Session.GetObjectFromJson<string>("profileId");//profileId
            List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Add(new KeyValuePair<string, string>("accesstoken", accessToken));
            Parameters.Add(new KeyValuePair<string, string>("groupId", groupId));
            Parameters.Add(new KeyValuePair<string, string>("profileId", profileId));
            HttpResponseMessage response = await WebApiReq.PostReq("/api/Facebook/GetFacebookPagesDet", Parameters, "", "", _appSettings.ApiDomain);
            if (response.IsSuccessStatusCode)
            {
                List<Domain.Socioboard.Models.Facebookpage> lstpages = await response.Content.ReadAsAsync<List<Domain.Socioboard.Models.Facebookpage>>();
                lstpages = lstpages.Where(t => t.ProfilePageId.Contains(profileId)).ToList();
                if (lstpages.Count > 0)
                {
                    TempData["fbReconnect"] = Newtonsoft.Json.JsonConvert.SerializeObject(lstpages);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Error"] = "Page has not been linked with current login account,please login with linked facebook account";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                TempData["Error"] = "Error while hitting api.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
