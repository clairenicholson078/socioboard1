using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Socioboard.Helpers;
using Socioboard.Extensions;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Socioboard.Controllers
{
    public class InstagramManagerController : Controller
    {
        private Helpers.AppSettings _appSettings;
        private readonly ILogger _logger;

        public InstagramManagerController(ILogger<InstagramManagerController> logger, Microsoft.Extensions.Options.IOptions<Helpers.AppSettings> settings)
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
        public async Task<ActionResult> AuthenticateInstagram()
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
                    groups = await response.Content.ReadAsAsync<List<Domain.Socioboard.Models.Groups>>();
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
           // string profileCount = await ProfilesHelper.GetUserProfileCount(user.Id, _appSettings, _logger);
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
                HttpContext.Session.SetObjectAsJson("Instagram", "Instagram_Account");
                string authUrl = _appSettings.InsagramAuthUrl + "&client_id=" + _appSettings.InstagramClientKey + "&redirect_uri=" + _appSettings.InstagramCallBackURL;
                return Redirect(authUrl);
            }
        }


        [HttpGet]
        public async Task<ActionResult> Instagram(string code)
        {
            string twitterSession = HttpContext.Session.GetObjectFromJson<string>("Instagram");
            string groupId = HttpContext.Session.GetObjectFromJson<string>("selectedGroupId");
            if (twitterSession.Equals("Instagram_Account"))
            {
                HttpContext.Session.SetObjectAsJson("Instagram", null);
                Domain.Socioboard.Models.User user = HttpContext.Session.GetObjectFromJson<Domain.Socioboard.Models.User>("User");
                List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
                Parameters.Add(new KeyValuePair<string, string>("code", code));
                Parameters.Add(new KeyValuePair<string, string>("groupId", groupId));
                Parameters.Add(new KeyValuePair<string, string>("userId", user.Id.ToString()));
                HttpResponseMessage response = await WebApiReq.PostReq("/api/Instagram/AddInstagramAccount", Parameters, "", "", _appSettings.ApiDomain);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = await response.Content.ReadAsStringAsync();
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Error"] = await response.Content.ReadAsStringAsync();
                    return RedirectToAction("Index", "Home");
                }
            }
            return View();
        }
    }
}