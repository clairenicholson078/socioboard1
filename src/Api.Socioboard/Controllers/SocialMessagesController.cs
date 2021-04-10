﻿using Microsoft.AspNetCore.Mvc;
using Api.Socioboard.Model;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors;
using System.Threading.Tasks;
using Domain.Socioboard.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Threading;
using Api.Socioboard.Repositories;
using Domain.Socioboard.Models.Mongo;
using MongoDB.Bson;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Drawing;
using Api.Socioboard.Helper;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Newtonsoft.Json;
using Socioboard.Facebook.Data;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Api.Socioboard.Controllers
{
    [EnableCors("AllowAll")]
    [Route("api/[controller]")]
    public class SocialMessagesController : Controller
    {

        public SocialMessagesController(ILogger<FacebookController> logger, Microsoft.Extensions.Options.IOptions<Helper.AppSettings> settings, IHostingEnvironment appEnv)
        {
            _logger = logger;
            _appSettings = settings.Value;
            _redisCache = new Helper.Cache(_appSettings.RedisConfiguration);
            _appEnv = appEnv;
        }
        private readonly ILogger _logger;
        private Helper.AppSettings _appSettings;
        private Helper.Cache _redisCache;
        private readonly IHostingEnvironment _appEnv;

        /// <summary>
        /// To compose message
        /// </summary>
        /// <param name="message">message provided by the user for posting</param>
        /// <param name="profileId">id of profiles of the user</param>
        /// <param name="userId">id of the user</param>  
        /// <param name="imagePath">path for taking image</param>
        /// <param name="link"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        [HttpPost("ComposeMessage")]
        public async Task<IActionResult> ComposeMessage(string message, string profileId, long userId, string imagePath, string link, Domain.Socioboard.Enum.UrlShortener shortnerStatus, Domain.Socioboard.Enum.MediaType mediaType, IFormFile files)
        {
            var filename = "";
            var apiimgPath = "";
            var uploads = string.Empty;
            string imgPath = string.Empty;
            string temp = string.Empty;
            string tempmsg = message.Replace("<br>", "");
            if (shortnerStatus == Domain.Socioboard.Enum.UrlShortener.bitlyUri)
            {
                temp = Utility.GetConvertedUrls(ref tempmsg, shortnerStatus);
                tempmsg = temp;
            }
            else if (shortnerStatus == Domain.Socioboard.Enum.UrlShortener.jmpUri)
            {
                temp = Utility.GetConvertedUrls(ref tempmsg, shortnerStatus);
                tempmsg = temp;
            }
            if (files != null)
            {

                if (files.Length > 0)
                {
                    var fileName = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue.Parse(files.ContentDisposition).FileName.Trim('"');
                    // await file.s(Path.Combine(uploads, fileName));
                    filename = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue
                            .Parse(files.ContentDisposition)
                            .FileName
                            .Trim('"');
                    var tempName = Domain.Socioboard.Helpers.SBHelper.RandomString(10) + '.' + fileName.Split('.')[1];
                    //apiimgPath = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{tempName}";

                    filename = _appEnv.WebRootPath + "\\upload" + $@"\{tempName}";
                    imgPath = filename;
                    uploads = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{tempName}";
                    // size += file.Length;
                    try
                    {
                        using (FileStream fs = System.IO.File.Create(filename))
                        {
                            files.CopyTo(fs);
                            fs.Flush();
                        }
                        filename = uploads;
                    }
                    catch (System.Exception ex)
                    {
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            uploads = imagePath;
                        }
                    }

                }
            }
            else if (!string.IsNullOrEmpty(imagePath))
            {
                uploads = imagePath;
                imgPath = imagePath;
            }

            //string[] updatedmessgae = Regex.Split(message, "<br>");

            //message = postmessage;

            var dbr = new DatabaseRepository(_logger, _appEnv);
            string[] lstProfileIds = null;
            if (profileId != null)
            {
                lstProfileIds = profileId.Split(',');
            }
            else
            {
                return Ok("profileId required");
            }

            foreach (var item in lstProfileIds)
            {
                var tempItemProfileId = string.Empty;
                var tempItemProfileName = string.Empty;

                if (item.Contains("`"))
                {
                    var profileSingle = item.Split('`');
                    tempItemProfileId = profileSingle[0];
                    tempItemProfileName = profileSingle[1];
                }
                else
                {
                    tempItemProfileId = item;
                }

                var updatedText = string.Empty;
                var postmessage = string.Empty;
                var url = string.Empty;

                if (!string.IsNullOrEmpty(tempmsg))
                {
                    var updatedMessgae = Regex.Split(tempmsg, "<br>");

                    foreach (var items in updatedMessgae)
                    {
                        if (!string.IsNullOrEmpty(items))
                        {
                            if (items.Contains("https://") || items.Contains("http://"))
                            {
                                if (string.IsNullOrEmpty(url))
                                {
                                    url = items;
                                    if (items.Contains("https://"))
                                    {
                                        var links = Utility.getBetween(url + "###", "https", "###");
                                        links = "https" + links;
                                        try
                                        {
                                            url = links.Split(' ')[0];
                                            link = url;
                                        }
                                        catch (Exception)
                                        {
                                            url = links;
                                            link = url;
                                        }
                                    }
                                    if (items.Contains("http://"))
                                    {
                                        string links = Utility.getBetween(url + "###", "http", "###");
                                        links = "http" + links;
                                        try
                                        {
                                            url = links.Split(' ')[0].ToString();
                                            link = url;
                                        }
                                        catch (Exception)
                                        {
                                            url = links;
                                            link = url;
                                        }
                                    }

                                }

                            }
                            if (items.Contains("hhh") || items.Contains("nnn"))
                            {
                                if (items.Contains("hhh"))
                                {
                                    postmessage = postmessage + "\n\r" + items.Replace("hhh", "#");
                                }
                                else
                                {
                                    postmessage = postmessage + "\n\r" + items;
                                }
                            }
                            else
                            {
                                postmessage = postmessage + "\n\r" + items;
                            }
                        }

                    }
                }
                try
                {
                    if (!string.IsNullOrEmpty(url))
                    {
                        // link = url;
                        //updatedtext = postmessage.Replace(url, "");
                        updatedText = postmessage;

                    }
                    else
                    {
                        updatedText = postmessage;
                    }


                }
                catch (Exception ex)
                {

                }

                if (tempItemProfileId.StartsWith("fb"))
                {
                    try
                    {
                        new Thread(delegate ()
                        {
                            string prId = tempItemProfileId.Substring(3, tempItemProfileId.Length - 3);
                            Domain.Socioboard.Models.Facebookaccounts objFacebookAccount = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                            string ret = Helper.FacebookHelper.ComposeMessage(objFacebookAccount.FbProfileType, objFacebookAccount.AccessToken, objFacebookAccount.FbUserId, updatedText, prId, userId, uploads, link, mediaType, objFacebookAccount.FbUserName, dbr, _logger);

                        }).Start();
                    }
                    catch (Exception ex)
                    {

                    }
                }
                else if (tempItemProfileId.StartsWith("page"))
                {
                    try
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                var pageId = item.Replace("page_", string.Empty);

                                var accountDetails = FacebookRepository.getFacebookAccount(pageId, _redisCache, dbr);

                                var pageAccessToken =
                                    FacebookApiHelper.GetPageAccessToken(pageId, accountDetails.AccessToken, string.Empty);

                                var response = FacebookApiHelper.PublishPost(accountDetails.FbProfileType, pageAccessToken, accountDetails.FbUserId, updatedText, pageId, uploads, link);

                                FacebookHelper.UpdatePublishedDetails(accountDetails.FbProfileType, accountDetails.FbUserId,
                                    updatedText, pageId, userId, uploads, mediaType, accountDetails.FbUserName, dbr);

                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message);
                                _logger.LogError(ex.StackTrace);
                            }

                        });

                        #region Old Methods
                        //new Thread(delegate ()
                        //              {
                        //                  string prId = temp_item_profileId.Substring(5, temp_item_profileId.Length - 5);
                        //                  Domain.Socioboard.Models.Facebookaccounts objFacebookAccount = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                        //                  string ret = Helper.FacebookHelper.ComposeMessage(objFacebookAccount.FbProfileType, objFacebookAccount.AccessToken, objFacebookAccount.FbUserId, updatedtext, prId, userId, uploads, link, mediaType, objFacebookAccount.FbUserName, dbr, _logger);
                        //              }).Start();
                        #endregion
                    }
                    catch (Exception ex)
                    {

                    }
                }



                else if (tempItemProfileId.StartsWith("urlfb"))
                {
                    try
                    {
                        new Thread(delegate ()
                        {
                            string prId = tempItemProfileId.Substring(6, tempItemProfileId.Length - 6);
                            Domain.Socioboard.Models.Facebookaccounts objFacebookAccount = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                            string ret = Helper.FacebookHelper.UrlComposeMessage(objFacebookAccount.FbProfileType, objFacebookAccount.AccessToken, objFacebookAccount.FbUserId, updatedText, prId, userId, uploads, link, mediaType, objFacebookAccount.FbUserName, dbr, _logger);

                        }).Start();
                    }
                    catch (Exception ex)
                    {

                    }
                }



                else if (tempItemProfileId.StartsWith("tw"))
                {
                    try
                    {

                        new Thread(delegate ()
                        {
                            string prId = tempItemProfileId.Substring(3, tempItemProfileId.Length - 3);
                            string ret = Helper.TwitterHelper.PostTwitterMessage(_appSettings, _redisCache, tempmsg, prId, userId, uploads, true, mediaType, tempItemProfileName, dbr, _logger);
                        }).Start();

                    }
                    catch (Exception ex)
                    {

                    }
                }

                else if (tempItemProfileId.StartsWith("lin"))
                {
                    try
                    {
                        new Thread(delegate ()
                        {
                            string prId = tempItemProfileId.Substring(4, tempItemProfileId.Length - 4);
                            string ret = Helper.LinkedInHelper.PostLinkedInMessage(uploads, userId, tempmsg, prId, imgPath, mediaType, tempItemProfileName, _redisCache, _appSettings, dbr);

                        }).Start();
                    }
                    catch (Exception ex)
                    {

                    }
                }


                else if (tempItemProfileId.StartsWith("Cmpylinpage"))
                {
                    try
                    {
                        new Thread(delegate ()
                        {
                            string prId = tempItemProfileId.Substring(12, tempItemProfileId.Length - 12);
                            string ret = Helper.LinkedInHelper.PostLinkedInCompanyPagePost(uploads, imgPath, userId, tempmsg, prId, mediaType, tempItemProfileName, _redisCache, dbr, _appSettings);
                        }).Start();

                    }
                    catch (Exception ex)
                    {

                    }
                }

            }

            return Ok("Posted");

        }

        [HttpPost("ScheduleMessage")]
        public async Task<ActionResult> ScheduleMessage(string message, string profileId, long userId, string imagePath, string link, string scheduledatetime, string localscheduletime, IFormFile files, string messageText, Domain.Socioboard.Enum.MediaType mediaType)
        {

            string updatemessage = Request.Form["messageText"];
            if (message == "none")
            {
                messageText = updatemessage;
            }
            message = messageText;
            var filename = "";
            string postmessage = "";
            string tempfilepath = "";
            string img = "";
            var uploads = _appEnv.WebRootPath + "\\wwwwroot\\upload\\" + profileId;
            if (files != null)
            {

                if (files.Length > 0)
                {
                    var fileName = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue.Parse(files.ContentDisposition).FileName.Trim('"');
                    // await file.s(Path.Combine(uploads, fileName));
                    filename = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue
                            .Parse(files.ContentDisposition)
                            .FileName
                            .Trim('"');
                    //apiimgPath = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{Domain.Socioboard.Helpers.SBHelper.RandomString(10) + '.' + fileName.Split('.')[1]}";
                    var tempName = Domain.Socioboard.Helpers.SBHelper.RandomString(10) + '.' + fileName.Split('.')[1];
                    filename = _appEnv.WebRootPath + "\\upload" + $@"\{tempName}";
                    tempfilepath = filename;


                    uploads = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{tempName}";

                    // size += file.Length;
                    using (FileStream fs = System.IO.File.Create(filename))
                    {
                        files.CopyTo(fs);
                        fs.Flush();
                    }
                    filename = uploads;
                }
            }
            else if (!string.IsNullOrEmpty(imagePath))
            {
                filename = imagePath;
                tempfilepath = filename;
            }
            try
            {
                var client = new ImgurClient("5f1ad42ec5988b7", "f3294c8632ef8de6bfcbc46b37a23d18479159c5");
                var endpoint = new ImageEndpoint(client);
                IImage image;
                using (var fs = new FileStream(tempfilepath, FileMode.Open))
                {
                    image = endpoint.UploadImageStreamAsync(fs).GetAwaiter().GetResult();
                }
                img = image.Link;
            }
            catch (Exception)
            {
                img = tempfilepath;
            }

            string[] updatedmessgae = Regex.Split(message, "<br>");
            foreach (var item in updatedmessgae)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    if (item.Contains("https://") || item.Contains("http://"))
                    {
                        try
                        {
                            link = "http" + Utility.getBetween(item, "http", " ");
                            try
                            {
                                link = link.Split(' ')[0].ToString();
                            }
                            catch (Exception)
                            {

                                link = "http" + Utility.getBetween(item, "http", " ");
                            }
                        }
                        catch
                        {
                            string temp_item = item + "###";
                            link = "http" + Utility.getBetween(temp_item, "http", "###");
                            try
                            {
                                link = link.Split(' ')[0].ToString();
                            }
                            catch (Exception)
                            {

                                link = "http" + Utility.getBetween(temp_item, "http", "###");
                            }
                        }
                    }
                    if (item.Contains("hhh") || item.Contains("nnn"))
                    {
                        if (item.Contains("hhh"))
                        {
                            postmessage = postmessage + "\n\r" + item.Replace("hhh", "#");
                        }
                    }
                    else
                    {
                        postmessage = postmessage + "\n\r" + item;
                    }
                }
            }
            try
            {
                link = "";
                //updatedtext = postmessage.Replace(url, "");
                // updatedtext = postmessage;
                message = postmessage;
            }
            catch (Exception ex)
            {
                message = postmessage;
            }

            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            string[] lstProfileIds = null;
            if (profileId != null)
            {
                lstProfileIds = profileId.Split(',');
                profileId = lstProfileIds[0];
            }
            else
            {
                return Ok("profileId required");
            }

            string retunMsg = string.Empty;

            foreach (var item in lstProfileIds)
            {
                if (item.StartsWith("fb"))
                {
                    try
                    {
                        string prId = item.Substring(3, item.Length - 3);
                        Domain.Socioboard.Models.Facebookaccounts objFacebookaccounts = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                        Helper.ScheduleMessageHelper.ScheduleMessage(prId, objFacebookaccounts.FbUserName, message, Domain.Socioboard.Enum.SocialProfileType.Facebook, userId, link, filename, "https://graph.facebook.com/" + prId + "/picture?type=small", scheduledatetime, localscheduletime, mediaType, _appSettings, _redisCache, dbr, _logger);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                        //return Ok("Issue With Facebook schedulers");
                    }
                }
                if (item.StartsWith("page"))
                {
                    try
                    {
                        string prId = item.Substring(5, item.Length - 5);
                        Domain.Socioboard.Models.Facebookaccounts objFacebookaccounts = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                        Helper.ScheduleMessageHelper.ScheduleMessage(prId, objFacebookaccounts.FbUserName, message, Domain.Socioboard.Enum.SocialProfileType.FacebookFanPage, userId, link, filename, "https://graph.facebook.com/" + prId + "/picture?type=small", scheduledatetime, localscheduletime, mediaType, _appSettings, _redisCache, dbr, _logger);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                        // return Ok("Issue With Facebook Page schedulers");
                    }
                }
                if (item.StartsWith("urlfb"))
                {
                    try
                    {
                        string linkurl = message;
                        link = linkurl;
                        string prId = item.Substring(6, item.Length - 6);
                        Domain.Socioboard.Models.Facebookaccounts objFacebookaccounts = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                        Helper.ScheduleMessageHelper.ScheduleMessage(prId, objFacebookaccounts.FbUserName, link, Domain.Socioboard.Enum.SocialProfileType.Facebook, userId, link, filename, "https://graph.facebook.com/" + prId + "/picture?type=small", scheduledatetime, localscheduletime, mediaType, _appSettings, _redisCache, dbr, _logger);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                        //return Ok("Issue With Facebook schedulers");
                    }
                }

                if (item.StartsWith("urlpage"))
                {
                    try
                    {
                        string linkurl = message;
                        link = linkurl;
                        string prId = item.Substring(8, item.Length - 8);
                        Domain.Socioboard.Models.Facebookaccounts objFacebookaccounts = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                        Helper.ScheduleMessageHelper.ScheduleMessage(prId, objFacebookaccounts.FbUserName, link, Domain.Socioboard.Enum.SocialProfileType.FacebookFanPage, userId, link, filename, "https://graph.facebook.com/" + prId + "/picture?type=small", scheduledatetime, localscheduletime, mediaType, _appSettings, _redisCache, dbr, _logger);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                        //return Ok("Issue With Facebook schedulers");
                    }
                }

                if (item.StartsWith("tw"))
                {
                    try
                    {
                        string prId = item.Substring(3, item.Length - 3);
                        Domain.Socioboard.Models.TwitterAccount objTwitterAccount = Api.Socioboard.Repositories.TwitterRepository.getTwitterAccount(prId, _redisCache, dbr);
                        Helper.ScheduleMessageHelper.ScheduleMessage(prId, objTwitterAccount.twitterScreenName, message, Domain.Socioboard.Enum.SocialProfileType.Twitter, userId, "", filename, objTwitterAccount.profileImageUrl, scheduledatetime, localscheduletime, mediaType, _appSettings, _redisCache, dbr, _logger);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                    }
                }
                if (item.StartsWith("lin"))
                {
                    try
                    {
                        string prId = item.Substring(4, item.Length - 4);
                        Domain.Socioboard.Models.LinkedInAccount objLinkedInAccount = Api.Socioboard.Repositories.LinkedInAccountRepository.getLinkedInAccount(prId, _redisCache, dbr);
                        Helper.ScheduleMessageHelper.ScheduleMessage(prId, objLinkedInAccount.LinkedinUserName, message, Domain.Socioboard.Enum.SocialProfileType.LinkedIn, userId, "", img, objLinkedInAccount.ProfileImageUrl, scheduledatetime, localscheduletime, mediaType, _appSettings, _redisCache, dbr, _logger);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);

                        // return Ok("Issue With Linkedin schedulers");
                    }
                }
                if (item.StartsWith("Cmpylinpage"))
                {
                    try
                    {
                        string prId = item.Substring(12, item.Length - 12);
                        Domain.Socioboard.Models.LinkedinCompanyPage objLinkedinCompanyPage = Api.Socioboard.Repositories.LinkedInAccountRepository.getLinkedinCompanyPage(prId, _redisCache, dbr);
                        Helper.ScheduleMessageHelper.ScheduleMessage(prId, objLinkedinCompanyPage.LinkedinPageName, message, Domain.Socioboard.Enum.SocialProfileType.LinkedInComapanyPage, userId, "", img, objLinkedinCompanyPage.LogoUrl, scheduledatetime, localscheduletime, mediaType, _appSettings, _redisCache, dbr, _logger);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);

                        // return Ok("Issue With Linkedin Page schedulers");
                    }
                }

            }
            return Ok("scheduled");
        }

        [HttpPost("DaywiseScheduleMessage")]
        public async Task<ActionResult> DaywiseScheduleMessage(string message, string profileId, string weekdays, string localscheduletime, long userId, string imagePath, string link, IFormFile files, string messageText)
        {
            message = messageText;
            var filename = "";
            string postmessage = "";
            string tempfilepath = "";
            string img = "";
            var uploads = _appEnv.WebRootPath + "\\wwwwroot\\upload\\" + profileId;
            if (files != null)
            {

                if (files.Length > 0)
                {
                    var fileName = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue.Parse(files.ContentDisposition).FileName.Trim('"');
                    // await file.s(Path.Combine(uploads, fileName));
                    filename = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue
                            .Parse(files.ContentDisposition)
                            .FileName
                            .Trim('"');
                    //apiimgPath = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{Domain.Socioboard.Helpers.SBHelper.RandomString(10) + '.' + fileName.Split('.')[1]}";
                    var tempName = Domain.Socioboard.Helpers.SBHelper.RandomString(10) + '.' + fileName.Split('.')[1];
                    filename = _appEnv.WebRootPath + "\\upload" + $@"\{tempName}";
                    tempfilepath = filename;


                    uploads = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{tempName}";

                    // size += file.Length;
                    using (FileStream fs = System.IO.File.Create(filename))
                    {
                        files.CopyTo(fs);
                        fs.Flush();
                    }
                    filename = uploads;
                }
            }
            else if (!string.IsNullOrEmpty(imagePath))
            {
                filename = imagePath;
                tempfilepath = filename;
            }
            try
            {
                var client = new ImgurClient("5f1ad42ec5988b7", "f3294c8632ef8de6bfcbc46b37a23d18479159c5");
                var endpoint = new ImageEndpoint(client);
                IImage image;
                using (var fs = new FileStream(tempfilepath, FileMode.Open))
                {
                    image = endpoint.UploadImageStreamAsync(fs).GetAwaiter().GetResult();
                }
                img = image.Link;
            }
            catch (Exception)
            {
                img = tempfilepath;
            }

            string[] updatedmessgae = Regex.Split(message, "<br>");
            foreach (var item in updatedmessgae)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    if (item.Contains("https://") || item.Contains("http://"))
                    {
                        try
                        {
                            link = "http" + Utility.getBetween(item, "http", " ");
                            try
                            {
                                link = link.Split(' ')[0].ToString();
                            }
                            catch (Exception)
                            {

                                link = "http" + Utility.getBetween(item, "http", " ");
                            }
                        }
                        catch
                        {
                            string temp_item = item + "###";
                            link = "http" + Utility.getBetween(temp_item, "http", "###");
                            try
                            {
                                link = link.Split(' ')[0].ToString();
                            }
                            catch (Exception)
                            {

                                link = "http" + Utility.getBetween(temp_item, "http", "###");
                            }
                        }
                    }
                    if (item.Contains("hhh") || item.Contains("nnn"))
                    {
                        if (item.Contains("hhh"))
                        {
                            postmessage = postmessage + "\n\r" + item.Replace("hhh", "#");
                        }
                    }
                    else
                    {
                        postmessage = postmessage + "\n\r" + item;
                    }
                }
            }
            try
            {
                message = postmessage.Replace(link, ""); ;
            }
            catch (Exception ex)
            {
                message = postmessage;
            }

            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            string[] lstProfileIds = null;
            if (profileId != null)
            {
                lstProfileIds = profileId.Split(',');
                profileId = lstProfileIds[0];
            }
            else
            {
                return Ok("profileId required");
            }
            List<string> days = new List<string>();
            List<string> weekval = null;
            List<string> SelectedDays = new List<string>();

            if (weekdays != null)
            {

                weekval = weekdays.Split(',').ToList();
                foreach (var day in weekval)
                {
                    if (day != "")
                    {
                        try
                        {
                            days.Add(day);
                            var enabledDay = ((DayOfWeek) Enum.Parse(typeof(DayOfWeek), day, true)).ToString();
                            SelectedDays.Add(enabledDay);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }

                weekdays = weekval[0];
            }
            else
            {
                return Ok("profileId required");
            }

            string retunMsg = string.Empty;

            var scheduleDay = new Domain.Socioboard.Helpers.ScheduleDays();
            scheduleDay.SelectedDays = SelectedDays;
            var week = JsonConvert.SerializeObject(SelectedDays);

            foreach (var item in lstProfileIds)
            {
               // foreach (var week in days)
               // {
                    if (item.StartsWith("fb"))
                    {
                        try
                        {
                            string prId = item.Substring(3, item.Length - 3);
                            Domain.Socioboard.Models.Facebookaccounts objFacebookaccounts = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                            Helper.ScheduleMessageHelper.DaywiseScheduleMessage(prId, objFacebookaccounts.FbUserName, week, message, Domain.Socioboard.Enum.SocialProfileType.Facebook, userId, link, filename, "https://graph.facebook.com/" + prId + "/picture?type=small", localscheduletime, _appSettings, _redisCache, dbr, _logger);
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError(ex.StackTrace);
                            return Ok("Issue With Facebook schedulers");
                        }
                    }
                    if (item.StartsWith("page"))
                    {
                        try
                        {
                            string prId = item.Substring(5, item.Length - 5);
                            Domain.Socioboard.Models.Facebookaccounts objFacebookaccounts = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(prId, _redisCache, dbr);
                            Helper.ScheduleMessageHelper.DaywiseScheduleMessage(prId, objFacebookaccounts.FbUserName, week, message, Domain.Socioboard.Enum.SocialProfileType.FacebookFanPage, userId, link, filename, "https://graph.facebook.com/" + prId + "/picture?type=small", localscheduletime, _appSettings, _redisCache, dbr, _logger);
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError(ex.StackTrace);
                            return Ok("Issue With Facebook Page schedulers");
                        }
                    }
                    if (item.StartsWith("tw"))
                    {
                        try
                        {
                            string prId = item.Substring(3, item.Length - 3);
                            Domain.Socioboard.Models.TwitterAccount objTwitterAccount = Api.Socioboard.Repositories.TwitterRepository.getTwitterAccount(prId, _redisCache, dbr);
                            Helper.ScheduleMessageHelper.DaywiseScheduleMessage(prId, objTwitterAccount.twitterScreenName, week, message, Domain.Socioboard.Enum.SocialProfileType.Twitter, userId, "", filename, objTwitterAccount.profileImageUrl, localscheduletime, _appSettings, _redisCache, dbr, _logger);
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError(ex.StackTrace);
                            return Ok("Issue With twitter schedulers");
                        }
                    }
                    if (item.StartsWith("lin"))
                    {
                        try
                        {
                            string prId = item.Substring(4, item.Length - 4);
                            Domain.Socioboard.Models.LinkedInAccount objLinkedInAccount = Api.Socioboard.Repositories.LinkedInAccountRepository.getLinkedInAccount(prId, _redisCache, dbr);
                            Helper.ScheduleMessageHelper.DaywiseScheduleMessage(prId, objLinkedInAccount.LinkedinUserName, week, message, Domain.Socioboard.Enum.SocialProfileType.LinkedIn, userId, "", img, objLinkedInAccount.ProfileImageUrl, localscheduletime, _appSettings, _redisCache, dbr, _logger);
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError(ex.StackTrace);

                            return Ok("Issue With Linkedin schedulers");
                        }
                    }
                    if (item.StartsWith("Cmpylinpage"))
                    {
                        try
                        {
                            string prId = item.Substring(12, item.Length - 12);
                            Domain.Socioboard.Models.LinkedinCompanyPage objLinkedinCompanyPage = Api.Socioboard.Repositories.LinkedInAccountRepository.getLinkedinCompanyPage(prId, _redisCache, dbr);
                            Helper.ScheduleMessageHelper.DaywiseScheduleMessage(prId, objLinkedinCompanyPage.LinkedinPageName, week, message, Domain.Socioboard.Enum.SocialProfileType.LinkedInComapanyPage, userId, "", img, objLinkedinCompanyPage.LogoUrl, localscheduletime, _appSettings, _redisCache, dbr, _logger);
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError(ex.StackTrace);
                            return Ok("Issue With Linkedin Page schedulers");
                        }
                    }
               // }


            }
            return Ok("Message scheduled successfully");
        }

        [HttpPost("PluginComposemessage")]
        public IActionResult PluginComposemessage(string profile, string twitterText, string tweetId, string tweetUrl, string facebookText, string url, string imgUrl, long userId)
        {
            string[] profiles = profile.Split(',');

            int i = 0;
            foreach (var item in profiles)
            {
                string[] ids = item.Split('~');
                if (ids[1] == "facebook")
                {
                    string updatedtext = "";
                    string postmessage = "";
                    DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
                    Domain.Socioboard.Models.Facebookaccounts objFacebookAccount = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(ids[0], _redisCache, dbr);
                    if (!string.IsNullOrEmpty(facebookText))
                    {
                        string[] updatedmessgae = Regex.Split(facebookText, "<br>");
                        foreach (var items in updatedmessgae)
                        {
                            if (!string.IsNullOrEmpty(items))
                            {
                                if (items.Contains("https://") || items.Contains("http://"))
                                {
                                    if (string.IsNullOrEmpty(url))
                                    {
                                        url = items;
                                        if (items.Contains("https://"))
                                        {
                                            string link = Utility.getBetween(url + "###", "https", "###");
                                            link = "https" + link;
                                            try
                                            {
                                                url = link.Split(' ')[0].ToString();
                                            }
                                            catch (Exception)
                                            {
                                                link = "https" + link;
                                            }
                                        }
                                        if (items.Contains("http://"))
                                        {
                                            string link = Utility.getBetween(url + "###", "http", "###");
                                            link = "http" + link;
                                            try
                                            {
                                                url = link.Split(' ')[0].ToString();
                                            }
                                            catch (Exception)
                                            {
                                                link = "http" + link;
                                            }
                                        }

                                    }

                                }
                                if (items.Contains("hhh") || items.Contains("nnn"))
                                {
                                    if (items.Contains("hhh"))
                                    {
                                        postmessage = postmessage + "\n\r" + items.Replace("hhh", "#");
                                    }
                                    else
                                    {
                                        postmessage = postmessage + "\n\r" + items;
                                    }
                                }
                                else
                                {
                                    postmessage = postmessage + "\n\r" + items;
                                }
                            }

                        }
                    }
                    if (!string.IsNullOrEmpty(url))
                    {
                        updatedtext = postmessage.Replace(url, "").Replace("ppp", "+");
                    }
                    else
                    {
                        updatedtext = postmessage.Replace("ppp", "+");
                    }
                    int count = dbr.GetCount<ScheduledMessage>(t => t.shareMessage == updatedtext && t.profileId == objFacebookAccount.FbUserId && t.url == imgUrl && t.scheduleTime.Date == DateTime.UtcNow.Date);
                    if (count > 0)
                    {
                        i++;
                    }
                    else
                    {

                        string ret = Helper.FacebookHelper.ComposeMessage(objFacebookAccount.FbProfileType, objFacebookAccount.AccessToken, objFacebookAccount.FbUserId, updatedtext, ids[0], userId, imgUrl, url, 0, "", dbr, _logger);
                    }

                }
                else
                {
                    DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
                    if (!string.IsNullOrEmpty(twitterText) || !string.IsNullOrEmpty(imgUrl))
                    {
                        twitterText = twitterText + " " + tweetUrl;
                        int count = dbr.GetCount<ScheduledMessage>(t => t.shareMessage == twitterText && t.profileId == ids[0] && t.url == imgUrl && t.scheduleTime.Date == DateTime.UtcNow.Date);
                        if (count > 0)
                        {
                            i++;
                        }
                        else
                        {
                            string ret = Helper.TwitterHelper.PostTwitterMessage(_appSettings, _redisCache, twitterText, ids[0], userId, imgUrl, true, 0, "", dbr, _logger);
                        }

                    }
                    else
                    {
                        string data = TwitterRepository.TwitterRetweet_post(ids[0], tweetId, userId, 0, dbr, _logger, _redisCache, _appSettings);
                    }
                }
            }
            if (i > 0)
            {
                return Ok("it seems you already posted this message to few profiles");
            }
            return Ok("successfully posted");
        }

        [HttpPost("PluginScheduleMessage")]
        public IActionResult PluginScheduleMessage(string profile, string twitterText, string tweetId, string tweetUrl, string facebookText, string url, string imgUrl, long userId, string scheduleTime, string localscheduleTime, Domain.Socioboard.Enum.MediaType mediaType)
        {
            string[] profiles = profile.Split(',');
            foreach (var item in profiles)
            {
                string[] ids = item.Split('~');
                if (ids[1] == "facebook")
                {
                    try
                    {
                        string updatedtext = "";
                        string postmessage = "";
                        if (!string.IsNullOrEmpty(facebookText))
                        {
                            string[] updatedmessgae = Regex.Split(facebookText, "<br>");
                            foreach (var items in updatedmessgae)
                            {
                                if (!string.IsNullOrEmpty(items))
                                {
                                    if (items.Contains("https://") || items.Contains("http://"))
                                    {
                                        if (string.IsNullOrEmpty(url))
                                        {
                                            url = items;
                                            if (items.Contains("https://"))
                                            {
                                                string link = Utility.getBetween(url + "###", "https", "###");
                                                link = "https" + link;
                                                try
                                                {
                                                    url = link.Split(' ')[0].ToString();
                                                }
                                                catch (Exception)
                                                {
                                                    url = link;
                                                }
                                            }
                                            if (items.Contains("http://"))
                                            {
                                                string link = Utility.getBetween(url + "###", "http", "###");
                                                link = "http" + link;
                                                try
                                                {
                                                    url = link.Split(' ')[0].ToString();
                                                }
                                                catch (Exception)
                                                {
                                                    url = link;
                                                }
                                            }
                                        }

                                    }
                                    if (items.Contains("hhh") || items.Contains("nnn"))
                                    {
                                        if (items.Contains("hhh"))
                                        {
                                            postmessage = postmessage + "\n\r" + items.Replace("hhh", "#");
                                        }
                                        else
                                        {
                                            postmessage = postmessage + "\n\r" + items;
                                        }
                                    }
                                    else
                                    {
                                        postmessage = postmessage + "\n\r" + items;
                                    }
                                }
                            }
                        }
                        updatedtext = postmessage.Replace("ppp", "+");
                        DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
                        Domain.Socioboard.Models.Facebookaccounts objFacebookAccount = Api.Socioboard.Repositories.FacebookRepository.getFacebookAccount(ids[0], _redisCache, dbr);
                        Helper.ScheduleMessageHelper.ScheduleMessage(ids[0], objFacebookAccount.FbUserName, updatedtext.ToString(), Domain.Socioboard.Enum.SocialProfileType.FacebookFanPage, userId, url, imgUrl, "https://graph.facebook.com/" + ids[0] + "/picture?type=small", scheduleTime, localscheduleTime, mediaType, _appSettings, _redisCache, dbr, _logger);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                    try
                    {
                        DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
                        if (!string.IsNullOrEmpty(twitterText) || !string.IsNullOrEmpty(imgUrl))
                        {
                            twitterText = twitterText + " " + tweetUrl;
                            Domain.Socioboard.Models.TwitterAccount objTwitterAccount = Api.Socioboard.Repositories.TwitterRepository.getTwitterAccount(ids[0], _redisCache, dbr);
                            Helper.ScheduleMessageHelper.ScheduleMessage(ids[0], objTwitterAccount.twitterScreenName, twitterText, Domain.Socioboard.Enum.SocialProfileType.Twitter, userId, "", imgUrl, objTwitterAccount.profileImageUrl, scheduleTime, localscheduleTime, mediaType, _appSettings, _redisCache, dbr, _logger);
                        }
                        else
                        {
                            string data = TwitterRepository.TwitterRetweet_post(ids[0], tweetId, userId, 0, dbr, _logger, _redisCache, _appSettings);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            return Ok();
        }

        [HttpPost("UploadImageplugin")]
        public IActionResult UploadImageplugin(IFormFile files)
        {
            string filename = "";
            string uploads = "";
            if (files != null)
            {

                if (files.Length > 0)
                {
                    var fileName = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue.Parse(files.ContentDisposition).FileName.Trim('"');
                    // await file.s(Path.Combine(uploads, fileName));
                    filename = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue
                            .Parse(files.ContentDisposition)
                            .FileName
                            .Trim('"');
                    var tempName = Domain.Socioboard.Helpers.SBHelper.RandomString(10) + '.' + fileName.Split('.')[1];
                    //apiimgPath = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{tempName}";

                    filename = _appEnv.WebRootPath + "\\upload" + $@"\{tempName}";

                    uploads = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{tempName}";
                    // size += file.Length;
                    try
                    {
                        using (FileStream fs = System.IO.File.Create(filename))
                        {
                            files.CopyTo(fs);
                            fs.Flush();
                        }
                        filename = uploads;
                    }
                    catch (System.Exception ex)
                    {

                    }
                }
            }
            return Ok(uploads);
        }

        [HttpPost("SchedulePagePost")]
        public IActionResult SchedulePagePost(string profileId, int TimeInterVal, long UserId)
        {
            int addedPageCount = 0;
            int invalidaccessToken = 0;
            profileId = profileId.TrimEnd(',');
            try
            {
                try
                {
                    foreach (var items in profileId.Split(','))
                    {
                        string pageUrls = Request.Form["pageUrls"];
                        Domain.Socioboard.Models.Facebookaccounts _Facebookaccounts = new Facebookaccounts();
                        DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
                        _Facebookaccounts = FacebookRepository.getFacebookAccount(items, _redisCache, dbr);
                        List<Domain.Socioboard.Models.Mongo.PageDetails> pageid = Helper.FacebookHelper.GetFbPagePostDetails(pageUrls, _Facebookaccounts.AccessToken);
                        if (pageid.Count == 0)
                        {
                            invalidaccessToken++;
                        }
                        foreach (Domain.Socioboard.Models.Mongo.PageDetails item in pageid)
                        {
                            addedPageCount++;
                            if (_Facebookaccounts != null)
                            {
                                LinkShareathon _LinkShareathon = new LinkShareathon();
                                _LinkShareathon.Id = ObjectId.GenerateNewId();
                                _LinkShareathon.strId = ObjectId.GenerateNewId().ToString();
                                _LinkShareathon.Userid = UserId;
                                _LinkShareathon.Facebookusername = _Facebookaccounts.FbUserName;
                                _LinkShareathon.FacebookPageUrl = item.PageUrl;
                                _LinkShareathon.TimeInterVal = TimeInterVal;
                                _LinkShareathon.Facebookpageid = item.PageId;
                                _LinkShareathon.IsActive = true;
                                MongoRepository _ShareathonRepository = new MongoRepository("LinkShareathon", _appSettings);
                                _ShareathonRepository.Add(_LinkShareathon);
                                new Thread(delegate ()
                               {
                                   Helper.FacebookHelper.SchedulePagePost(_Facebookaccounts.AccessToken, items, item.PageId, TimeInterVal);
                               }).Start();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            catch (System.Exception ex)
            {

            }
            if (addedPageCount == profileId.Length)
            {
                return Ok("successfully added");
            }
            else if (invalidaccessToken > 0)
            {
                return Ok(invalidaccessToken + "pages access token expired");
            }

            return Ok("successfully added");
        }

        [HttpPost("DraftScheduleMessage")]
        public async Task<ActionResult> DraftScheduleMessage(string message, long userId, string scheduledatetime, long groupId, string imagePath, Domain.Socioboard.Enum.MediaType mediaType, IFormFile files)
        {
            var uploads = _appEnv.WebRootPath + "\\wwwwroot\\upload\\";
            var filename = "";
            string postmessage = "";
            if (files != null)
            {

                if (files.Length > 0)
                {
                    var fileName = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue.Parse(files.ContentDisposition).FileName.Trim('"');
                    // await file.s(Path.Combine(uploads, fileName));
                    filename = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue
                            .Parse(files.ContentDisposition)
                            .FileName
                            .Trim('"');
                    var tempName = Domain.Socioboard.Helpers.SBHelper.RandomString(10) + '.' + fileName.Split('.')[1];
                    //filename = _appEnv.WebRootPath + $@"\{tempName}";
                    filename = _appEnv.WebRootPath + "\\upload" + $@"\{tempName}";
                    uploads = _appSettings.ApiDomain + "/api/Media/get?id=" + $@"{tempName}";

                    // size += file.Length;
                    using (FileStream fs = System.IO.File.Create(filename))
                    {
                        files.CopyTo(fs);
                        fs.Flush();
                    }
                    filename = uploads;
                }
            }
            else if (!string.IsNullOrEmpty(imagePath))
            {
                uploads = imagePath;
                filename = uploads;
            }


            string[] updatedmessgae = Regex.Split(message, "<br>");
            foreach (var item in updatedmessgae)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    if (item.Contains("hhh") || item.Contains("nnn"))
                    {
                        if (item.Contains("hhh"))
                        {
                            postmessage = postmessage + "\n\r" + item.Replace("hhh", "#");
                        }
                    }
                    else
                    {
                        postmessage = postmessage + "\n\r" + item;
                    }
                }
            }
            if (scheduledatetime == null)
            {
                scheduledatetime = DateTime.UtcNow.ToString();
            }

            message = postmessage;
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            Helper.ScheduleMessageHelper.DraftScheduleMessage(message, userId, groupId, filename, scheduledatetime, mediaType, _appSettings, _redisCache, dbr, _logger);
            return Ok();
        }
        [HttpGet("GetAllScheduleMessage")]
        public IActionResult GetAllScheduleMessage(long userId, long groupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduledMessage = Repositories.ScheduledMessageRepository.getUsreScheduleMessage(userId, groupId, _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage);
        }

        [HttpGet("GetAllDaywiseScheduleMessage")]
        public IActionResult GetAllDaywiseScheduleMessage(long userId, long groupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.DaywiseSchedule> lstScheduledMessage = Repositories.ScheduledMessageRepository.getUsreDaywiseScheduleMessage(userId, groupId, _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage);
        }

        [HttpGet("DeleteSocialMessages")]
        public IActionResult DeleteSocialMessages(long socioqueueId, long userId, long GroupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduledMessage = Repositories.ScheduledMessageRepository.DeleteSocialMessages(socioqueueId, userId, GroupId, _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage);
        }

        [HttpGet("DeleteDaywiseSocialMessages")]
        public IActionResult DeleteDaywiseSocialMessages(long socioqueueId, long userId, long GroupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.DaywiseSchedule> lstScheduledMessage = Repositories.ScheduledMessageRepository.DeleteDaywiseSocialMessages(socioqueueId, userId, GroupId, _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage);
        }

        [HttpGet("DeleteMultiSocialMessages")]
        public IActionResult DeleteMultiSocialMessages(string socioqueueId, long userId, long GroupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            string[] lstSocioqueIdstr = socioqueueId.Split(',');
            List<long> lstSocioqueIds = (Array.ConvertAll(lstSocioqueIdstr, Convert.ToInt64)).ToList();
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduledMessage = Repositories.ScheduledMessageRepository.DeleteMultiSocialMessages(lstSocioqueIds, userId, GroupId, _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage);
        }

        [HttpGet("DeleteMultiDaywiseSocialMessages")]
        public IActionResult DeleteMultiDaywiseSocialMessages(string socioqueueId, long userId, long GroupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            string[] lstSocioqueIdstr = socioqueueId.Split(',');
            List<long> lstSocioqueIds = (Array.ConvertAll(lstSocioqueIdstr, Convert.ToInt64)).ToList();
            List<Domain.Socioboard.Models.DaywiseSchedule> lstScheduledMessage = Repositories.ScheduledMessageRepository.DeleteMultiDaywiseSocialMessages(lstSocioqueIds, userId, GroupId, _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage);
        }

        [HttpGet("EditScheduleMessage")]
        public IActionResult EditScheduleMessage(long socioqueueId, long userId, long GroupId, string message)
        {
            string postmessage = "";
            string[] updatedmessgae = Regex.Split(message, "<br>");

            foreach (var item in updatedmessgae)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    if (item.Contains("hhh") || item.Contains("nnn"))
                    {
                        if (item.Contains("hhh"))
                        {
                            postmessage = postmessage + item.Replace("hhh", "#");
                        }
                        if (item.Contains("nnn"))
                        {
                            postmessage = postmessage.Replace("nnn", "&");
                        }
                        if (item.Contains("ppp"))
                        {
                            postmessage = postmessage.Replace("ppp", "+");
                        }
                        if (item.Contains("jjj"))
                        {
                            postmessage = postmessage.Replace("jjj", "-+");
                        }
                    }
                    else
                    {
                        postmessage = postmessage + "\n\r" + item;
                    }
                }
            }
            message = postmessage;

            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduleMessage = Repositories.ScheduledMessageRepository.EditScheduleMessage(socioqueueId, userId, GroupId, message, _redisCache, _appSettings, dbr);
            return Ok(lstScheduleMessage);
        }

        [HttpGet("EditDaywiseScheduleMessage")]
        public IActionResult EditDaywiseScheduleMessage(long socioqueueId, long userId, long GroupId, string message)
        {
            string postmessage = "";
            string[] updatedmessgae = Regex.Split(message, "<br>");

            foreach (var item in updatedmessgae)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    if (item.Contains("hhh") || item.Contains("nnn"))
                    {
                        if (item.Contains("hhh"))
                        {
                            postmessage = postmessage + item.Replace("hhh", "#");
                        }
                        if (item.Contains("nnn"))
                        {
                            postmessage = postmessage.Replace("nnn", "&");
                        }
                        if (item.Contains("ppp"))
                        {
                            postmessage = postmessage.Replace("ppp", "+");
                        }
                        if (item.Contains("jjj"))
                        {
                            postmessage = postmessage.Replace("jjj", "-+");
                        }
                    }
                    else
                    {
                        postmessage = postmessage + "\n\r" + item;
                    }
                }
            }
            message = postmessage;

            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.DaywiseSchedule> lstScheduleMessage = Repositories.ScheduledMessageRepository.EditDaywiseScheduleMessage(socioqueueId, userId, GroupId, message, _redisCache, _appSettings, dbr);
            return Ok(lstScheduleMessage);
        }


        [HttpGet("GetAllSentMessages")]
        public IActionResult GetAllSentMessages(long userId, long groupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduledMessage = Repositories.ScheduledMessageRepository.GetAllSentMessages(userId, groupId, _redisCache, _appSettings, dbr);
            if (lstScheduledMessage != null)
            {
                return Ok(lstScheduledMessage.OrderByDescending(t => t.scheduleTime));
            }
            else
            {
                return Ok(null);
            }
        }

        [HttpGet("GetAllSentMessagesCount")]
        public IActionResult GetAllSentMessagesCount(long userId, long groupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            int GetAllSentMessagesCount = Repositories.ScheduledMessageRepository.GetAllSentMessagesCount(userId, groupId, dbr, _redisCache);
            return Ok(GetAllSentMessagesCount);
        }

        [HttpGet("getAllSentMessageDetailsforADay")]
        public IActionResult getAllSentMessageDetailsforADay(long userId, long groupId, string day)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduledMessage = Repositories.ScheduledMessageRepository.getAllSentMessageDetailsforADay(userId, groupId, int.Parse(day), _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage.OrderByDescending(t => t.scheduleTime));
        }

        [HttpGet("getAllSentMessageDetailsByDays")]
        public IActionResult getAllSentMessageDetailsByDays(long userId, long groupId, string days)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduledMessage = Repositories.ScheduledMessageRepository.getAllSentMessageDetailsByDays(userId, groupId, int.Parse(days), _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage.OrderByDescending(t => t.scheduleTime));
        }

        [HttpGet("getAllSentMessageDetailsByMonth")]
        public IActionResult getAllSentMessageDetailsByMonth(long userId, long groupId, string month)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduledMessage = Repositories.ScheduledMessageRepository.getAllSentMessageDetailsByMonth(userId, groupId, int.Parse(month), _redisCache, _appSettings, dbr);
            return Ok(lstScheduledMessage.OrderByDescending(t => t.scheduleTime));
        }

        [HttpGet("GetAllScheduleMessageCalendar")]
        public IActionResult GetAllScheduleMessageCalendar(long userId, long groupId)
        {
            DatabaseRepository dbr = new DatabaseRepository(_logger, _appEnv);
            List<Domain.Socioboard.Models.ScheduledMessage> lstScheduledMessage = Repositories.ScheduledMessageRepository.getUserAllScheduleMessage(userId, groupId, _redisCache, _appSettings, dbr);

            var eventList = from e in lstScheduledMessage
                            select new
                            {
                                id = e.id,
                                title = e.shareMessage,
                                //start = new DateTime(e.scheduleTime.Year, e.scheduleTime.Month, e.scheduleTime.Day, e.scheduleTime.Hour, e.scheduleTime.Minute, e.scheduleTime.Second).ToString("yyyy-MM-dd HH':'mm':'ss"),

                                //start = (DateTime.Parse(e.scheduleTime.ToString()).ToLocalTime()),
                                //start = Convert.ToDateTime(TimeZoneInfo.ConvertTimeFromUtc(e.scheduleTime, TimeZoneInfo.Local)),
                                // start = Convert.ToDateTime(CompareDateWithclient(DateTime.UtcNow.ToString(), e.scheduleTime.ToString())),
                                //url
                                start = e.calendertime,
                                allDay = false,
                                description = e.shareMessage,
                                profileId = e.profileId,
                                Image = e.picUrl,
                                ProfileImg = e.picUrl
                                //Image = "/Themes/" + path + "/" +e.PicUrl.Split(new string[] { path }, StringSplitOptions.None)[2],
                            };
            var rows = eventList.ToArray();
            return Ok(rows);
        }


        public static string CompareDateWithclient(string clientdate, string scheduletime)
        {
            try
            {
                var dt = DateTime.Parse(scheduletime);
                var clientdt = DateTime.Parse(clientdate);
                //  DateTime client = Convert.ToDateTime(clientdate);
                DateTime client = Convert.ToDateTime(TimeZoneInfo.ConvertTimeToUtc(clientdt, TimeZoneInfo.Local));
                DateTime server = DateTime.UtcNow;
                DateTime schedule = Convert.ToDateTime(TimeZoneInfo.ConvertTimeToUtc(dt, TimeZoneInfo.Local));
                {
                    var kind = schedule.Kind; // will equal DateTimeKind.Unspecified
                    if (DateTime.Compare(client, server) > 0)
                    {
                        double minutes = (server - client).TotalMinutes;
                        schedule = schedule.AddMinutes(minutes);
                    }
                    else if (DateTime.Compare(client, server) == 0)
                    {
                    }
                    else if (DateTime.Compare(client, server) < 0)
                    {
                        double minutes = (server - client).TotalMinutes;
                        schedule = schedule.AddMinutes(minutes);
                    }
                }
                return TimeZoneInfo.ConvertTimeFromUtc(schedule, TimeZoneInfo.Local).ToString();
                // return schedule.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return "";
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


    }
}
