﻿using Api.Socioboard.Helper;
using Api.Socioboard.Model;
using Domain.Socioboard.Models.Mongo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Socioboard.GoogleLib.App.Core;
using Socioboard.GoogleLib.Authentication;
using Socioboard.GoogleLib.GAnalytics.Core.AnalyticsMethod;
using Socioboard.GoogleLib.Youtube.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Socioboard.Repositories
{
    public static class GplusRepository
    {

        public static Domain.Socioboard.Models.Googleplusaccounts getGPlusAccount(string GPlusUserId, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            try
            {
                Domain.Socioboard.Models.Googleplusaccounts inMemGplusAcc = _redisCache.Get<Domain.Socioboard.Models.Googleplusaccounts>(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusAccount + GPlusUserId);
                if (inMemGplusAcc != null)
                {
                    return inMemGplusAcc;
                }
            }
            catch { }

            List<Domain.Socioboard.Models.Googleplusaccounts> lstGPlusAcc = dbr.Find<Domain.Socioboard.Models.Googleplusaccounts>(t => t.GpUserId.Equals(GPlusUserId) && t.IsActive).ToList();
            if (lstGPlusAcc != null && lstGPlusAcc.Count() > 0)
            {
                _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusAccount + GPlusUserId, lstGPlusAcc.First());
                return lstGPlusAcc.First();
            }
            else
            {
                return null;
            }



        }

        public static Domain.Socioboard.Models.GoogleAnalyticsAccount getGAAccount(string GaAccountId, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            try
            {
                Domain.Socioboard.Models.GoogleAnalyticsAccount inMemGAAcc = _redisCache.Get<Domain.Socioboard.Models.GoogleAnalyticsAccount>(Domain.Socioboard.Consatants.SocioboardConsts.CacheGAAccount + GaAccountId);
                if (inMemGAAcc != null)
                {
                    return inMemGAAcc;
                }
            }
            catch { }

            List<Domain.Socioboard.Models.GoogleAnalyticsAccount> lstGAAcc = dbr.Find<Domain.Socioboard.Models.GoogleAnalyticsAccount>(t => t.GaProfileId.Equals(GaAccountId)).ToList();
            if (lstGAAcc != null && lstGAAcc.Count() > 0)
            {
                _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusAccount + GaAccountId, lstGAAcc.First());
                return lstGAAcc.First();
            }
            else
            {
                return null;
            }



        }
        public static int AddGplusAccount(JObject profile,  Model.DatabaseRepository dbr, Int64 userId, Int64 groupId, string accessToken,string refreshToken ,Helper.Cache _redisCache, Helper.AppSettings settings, ILogger _logger)
        {
            int isSaved = 0;
            Domain.Socioboard.Models.Googleplusaccounts gplusAcc = GplusRepository.getGPlusAccount(Convert.ToString(profile["id"]), _redisCache, dbr);
            oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus(settings.GoogleConsumerKey,settings.GoogleConsumerSecret,settings.GoogleRedirectUri);
           
            if (gplusAcc != null && gplusAcc.IsActive == false)
            {
                gplusAcc.IsActive = true;
                gplusAcc.UserId = userId;
                gplusAcc.AccessToken = accessToken;
                gplusAcc.RefreshToken = refreshToken;
                gplusAcc.EntryDate = DateTime.UtcNow;
                try
                {
                    gplusAcc.GpUserName = profile["displayName"].ToString();
                }
                catch
                {
                    try
                    {
                        gplusAcc.GpUserName = profile["name"].ToString();
                    }
                    catch { }
                }
                try
                {
                    gplusAcc.GpProfileImage = Convert.ToString(profile["image"]["url"]);
                }
                catch
                {
                    try
                    {
                        gplusAcc.GpProfileImage = Convert.ToString(profile["picture"]);
                    }
                    catch { }

                }
                gplusAcc.AccessToken = accessToken;
                try
                {
                    gplusAcc.about = Convert.ToString(profile["tagline"]);
                }
                catch 
                {
                    gplusAcc.about = "";
                }
                try
                {
                    gplusAcc.college = Convert.ToString(profile["organizations"][0]["name"]);
                }
                catch
                {
                    gplusAcc.college = "";
                }
                try
                {
                    gplusAcc.coverPic = Convert.ToString(profile["cover"]["coverPhoto"]["url"]);
                }
                catch
                {
                    gplusAcc.coverPic = "";
                }
                try
                {
                    gplusAcc.education = Convert.ToString(profile["organizations"][0]["type"]);
                }
                catch
                {
                    gplusAcc.education = "";
                }
                try
                {
                    gplusAcc.EmailId = Convert.ToString(profile["emails"][0]["value"]);
                }
                catch
                {
                    gplusAcc.EmailId = "";
                }
                try
                {
                    gplusAcc.gender = Convert.ToString(profile["gender"]);
                }
                catch
                {
                    gplusAcc.gender = "";
                }
                try
                {
                    gplusAcc.workPosition = Convert.ToString(profile["occupation"]);
                }
                catch
                {
                    gplusAcc.workPosition = "";
                }
                gplusAcc.LastUpdate = DateTime.UtcNow;
                #region Get_InYourCircles
                try
                {
                    string _InyourCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleList.Replace("[userId]", gplusAcc.GpUserId).Replace("[collection]", "visible") + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_InyourCircles = JObject.Parse(_InyourCircles);
                    gplusAcc.InYourCircles = Convert.ToInt32(J_InyourCircles["totalItems"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.InYourCircles = 0;
                }
                #endregion

                #region Get_HaveYouInCircles
                try
                {
                    string _HaveYouInCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleProfile + gplusAcc.GpUserId + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_HaveYouInCircles = JObject.Parse(_HaveYouInCircles);
                    gplusAcc.HaveYouInCircles = Convert.ToInt32(J_HaveYouInCircles["circledByCount"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.HaveYouInCircles = 0;
                }
                #endregion




                dbr.Update<Domain.Socioboard.Models.Googleplusaccounts>(gplusAcc);
            }
            else
            {
                gplusAcc = new Domain.Socioboard.Models.Googleplusaccounts();
                gplusAcc.UserId = userId;
                gplusAcc.GpUserId = profile["id"].ToString();
                try {
                    gplusAcc.GpUserName = profile["displayName"].ToString();
                }
                catch {
                    try {
                        gplusAcc.GpUserName = profile["name"].ToString();
                    }
                    catch { }
                }
                gplusAcc.IsActive = true;
                gplusAcc.AccessToken = accessToken;
                gplusAcc.RefreshToken = refreshToken;
                gplusAcc.EntryDate = DateTime.UtcNow;
                try {
                    gplusAcc.GpProfileImage = Convert.ToString(profile["image"]["url"]);
                }
                catch
                {
                    try
                    {
                        gplusAcc.GpProfileImage = Convert.ToString(profile["picture"]);
                    }
                    catch { }
                    
                }
                gplusAcc.AccessToken = accessToken;
                try
                {
                    gplusAcc.about = Convert.ToString(profile["tagline"]);
                }
                catch
                {
                    gplusAcc.about = "";
                }
                try
                {
                    gplusAcc.college = Convert.ToString(profile["organizations"][0]["name"]);
                }
                catch
                {
                    gplusAcc.college = "";
                }
                try
                {
                    gplusAcc.coverPic = Convert.ToString(profile["cover"]["coverPhoto"]["url"]);
                }
                catch
                {
                    gplusAcc.coverPic = "";
                }
                try
                {
                    gplusAcc.education = Convert.ToString(profile["organizations"][0]["type"]);
                }
                catch
                {
                    gplusAcc.education = "";
                }
                try
                {
                    gplusAcc.EmailId = Convert.ToString(profile["email"][0]["value"]);                  

                }
                catch
                {
                   try{
                            try
                            {
                                gplusAcc.EmailId = Convert.ToString(profile["email"]);
                            }
                            catch(Exception ex)
                            {
                                gplusAcc.EmailId = "";
                            }
                     }
                    catch(Exception ex)
                    {
                        gplusAcc.EmailId = "";
                    }
                    //gplusAcc.EmailId = "";
                }
                try
                {
                    gplusAcc.gender = Convert.ToString(profile["gender"]);
                }
                catch
                {
                    gplusAcc.gender = "";
                }
                try
                {
                    gplusAcc.workPosition = Convert.ToString(profile["occupation"]);
                }
                catch
                {
                    gplusAcc.workPosition = "";
                }
                gplusAcc.LastUpdate = DateTime.UtcNow;


                #region Get_InYourCircles
                try
                {
                    string _InyourCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleList.Replace("[userId]", gplusAcc.GpUserId).Replace("[collection]", "visible") + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_InyourCircles = JObject.Parse(_InyourCircles);
                    gplusAcc.InYourCircles = Convert.ToInt32(J_InyourCircles["totalItems"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.InYourCircles = 0;
                }
                #endregion

                #region Get_HaveYouInCircles
                try
                {
                    string _HaveYouInCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleProfile + gplusAcc.GpUserId + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_HaveYouInCircles = JObject.Parse(_HaveYouInCircles);
                    gplusAcc.HaveYouInCircles = Convert.ToInt32(J_HaveYouInCircles["circledByCount"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.HaveYouInCircles = 0;
                }
                #endregion

                 isSaved = dbr.Add<Domain.Socioboard.Models.Googleplusaccounts>(gplusAcc);
            }
           
            if (isSaved == 1)
            {
                List<Domain.Socioboard.Models.Googleplusaccounts> lstgplusAcc = dbr.Find<Domain.Socioboard.Models.Googleplusaccounts>(t => t.GpUserId.Equals(gplusAcc.GpUserId)).ToList();
                if (lstgplusAcc != null && lstgplusAcc.Count() > 0)
                {
                    isSaved = GroupProfilesRepository.AddGroupProfile(groupId, lstgplusAcc.First().GpUserId, lstgplusAcc.First().GpUserName, userId, lstgplusAcc.First().GpProfileImage, Domain.Socioboard.Enum.SocialProfileType.GPlus, dbr);
                    //codes to delete cache
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheUserProfileCount + userId);
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGroupProfiles + groupId);

                    
                    if (isSaved == 1)
                    {
                        new Thread(delegate ()
                        {
                            GetUserActivities(gplusAcc.GpUserId,gplusAcc.AccessToken,settings,_logger);

                        }).Start();


                    }
                }

            }
            return isSaved;

        }


        public static int ReconnectGplusAccount(JObject profile, Model.DatabaseRepository dbr, Int64 userId, string accessToken, string refreshToken, Helper.Cache _redisCache, Helper.AppSettings settings, ILogger _logger)
        {
            int isSaved = 0;
            Domain.Socioboard.Models.Googleplusaccounts gplusAcc = GplusRepository.getGPlusAccount(Convert.ToString(profile["id"]), _redisCache, dbr);
            oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus(settings.GoogleConsumerKey, settings.GoogleConsumerSecret, settings.GoogleRedirectUri);

            if (gplusAcc != null )//&& gplusAcc.IsActive == false)
            {
                gplusAcc.IsActive = true;
                gplusAcc.UserId = userId;
                gplusAcc.AccessToken = accessToken;
                gplusAcc.RefreshToken = refreshToken;
                gplusAcc.EntryDate = DateTime.UtcNow;
                try
                {
                    gplusAcc.GpUserName = profile["displayName"].ToString();
                }
                catch
                {
                    try
                    {
                        gplusAcc.GpUserName = profile["name"].ToString();
                    }
                    catch { }
                }
                try
                {
                    gplusAcc.GpProfileImage = Convert.ToString(profile["image"]["url"]);
                }
                catch
                {
                    try
                    {
                        gplusAcc.GpProfileImage = Convert.ToString(profile["picture"]);
                    }
                    catch { }

                }
                gplusAcc.AccessToken = accessToken;
                try
                {
                    gplusAcc.about = Convert.ToString(profile["tagline"]);
                }
                catch
                {
                    gplusAcc.about = "";
                }
                try
                {
                    gplusAcc.college = Convert.ToString(profile["organizations"][0]["name"]);
                }
                catch
                {
                    gplusAcc.college = "";
                }
                try
                {
                    gplusAcc.coverPic = Convert.ToString(profile["cover"]["coverPhoto"]["url"]);
                }
                catch
                {
                    gplusAcc.coverPic = "";
                }
                try
                {
                    gplusAcc.education = Convert.ToString(profile["organizations"][0]["type"]);
                }
                catch
                {
                    gplusAcc.education = "";
                }
                try
                {
                    gplusAcc.EmailId = Convert.ToString(profile["emails"][0]["value"]);
                }
                catch
                {
                    gplusAcc.EmailId = "";
                }
                try
                {
                    gplusAcc.gender = Convert.ToString(profile["gender"]);
                }
                catch
                {
                    gplusAcc.gender = "";
                }
                try
                {
                    gplusAcc.workPosition = Convert.ToString(profile["occupation"]);
                }
                catch
                {
                    gplusAcc.workPosition = "";
                }
                gplusAcc.LastUpdate = DateTime.UtcNow;
                #region Get_InYourCircles
                try
                {
                    string _InyourCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleList.Replace("[userId]", gplusAcc.GpUserId).Replace("[collection]", "visible") + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_InyourCircles = JObject.Parse(_InyourCircles);
                    gplusAcc.InYourCircles = Convert.ToInt32(J_InyourCircles["totalItems"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.InYourCircles = 0;
                }
                #endregion

                #region Get_HaveYouInCircles
                try
                {
                    string _HaveYouInCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleProfile + gplusAcc.GpUserId + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_HaveYouInCircles = JObject.Parse(_HaveYouInCircles);
                    gplusAcc.HaveYouInCircles = Convert.ToInt32(J_HaveYouInCircles["circledByCount"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.HaveYouInCircles = 0;
                }
                #endregion
                int isaved = dbr.Update<Domain.Socioboard.Models.Googleplusaccounts>(gplusAcc);
                if (isaved == 1)
                {
                    return isaved;
                }
            }           
            return isSaved;
        }


        public static void GetUserActivities(string ProfileId, string AcessToken, Helper.AppSettings settings, ILogger _logger)
        {
            oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus(settings.GoogleConsumerKey,settings.GoogleConsumerSecret,settings.GoogleRedirectUri);
            try
            {
                //Domain.Socioboard.Domain.GooglePlusActivities _GooglePlusActivities = null;
                MongoGplusFeed _GooglePlusActivities;
                string _Activities = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetActivitiesList.Replace("[ProfileId]", ProfileId) + "?key=" + settings.GoogleApiKey, AcessToken);
                JObject J_Activities = JObject.Parse(_Activities);
                foreach (var item in J_Activities["items"])
                {
                    _GooglePlusActivities = new MongoGplusFeed();
                    _GooglePlusActivities.Id = ObjectId.GenerateNewId();
                    //_GooglePlusActivities.UserId = Guid.Parse(UserId);
                    _GooglePlusActivities.GpUserId = ProfileId;
                    try
                    {
                        _GooglePlusActivities.FromUserName = item["actor"]["displayName"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.FromId = item["actor"]["id"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.ActivityId = item["id"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.ActivityUrl = item["url"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.Title = item["title"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.FromProfileImage = item["actor"]["image"]["url"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.Content = item["object"]["content"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.PublishedDate = Convert.ToDateTime(item["published"].ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.PlusonersCount = Convert.ToInt32(item["object"]["plusoners"]["totalItems"].ToString());
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.RepliesCount = Convert.ToInt32(item["object"]["replies"]["totalItems"].ToString());
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.ResharersCount = Convert.ToInt32(item["object"]["resharers"]["totalItems"].ToString());
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.AttachmentType = item["object"]["attachments"][0]["objectType"].ToString();
                        if (_GooglePlusActivities.AttachmentType == "video")
                        {
                            _GooglePlusActivities.Attachment = item["object"]["attachments"][0]["embed"]["url"].ToString();
                        }
                        else if (_GooglePlusActivities.AttachmentType == "photo")
                        {
                            _GooglePlusActivities.Attachment = item["object"]["attachments"][0]["image"]["url"].ToString();
                        }
                        else if (_GooglePlusActivities.AttachmentType == "album")
                        {
                            _GooglePlusActivities.Attachment = item["object"]["attachments"][0]["thumbnails"][0]["image"]["url"].ToString();
                        }
                        else if (_GooglePlusActivities.AttachmentType == "article")
                        {
                            try
                            {
                                _GooglePlusActivities.Attachment = item["object"]["attachments"][0]["image"]["url"].ToString();
                            }
                            catch { }
                            try
                            {
                                _GooglePlusActivities.ArticleDisplayname = item["object"]["attachments"][0]["displayName"].ToString();
                            }
                            catch { }
                            try
                            {
                                _GooglePlusActivities.ArticleContent = item["object"]["attachments"][0]["content"].ToString();
                            }
                            catch { }
                            try
                            {
                                _GooglePlusActivities.Link = item["object"]["attachments"][0]["url"].ToString();
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        _GooglePlusActivities.AttachmentType = "note";
                        _GooglePlusActivities.Attachment = "";
                    }
                    MongoRepository gplusFeedRepo = new MongoRepository("MongoGplusFeed", settings);
                    var ret = gplusFeedRepo.Find<MongoGplusFeed>(t => t.ActivityId.Equals(_GooglePlusActivities.ActivityId));
                    var task = Task.Run(async () => {
                        return await ret;
                    });
                    int count = task.Result.Count;
                    if (count < 1)
                    {
                        gplusFeedRepo.Add(_GooglePlusActivities);

                    }
                    else
                    {
                        FilterDefinition<BsonDocument> filter = new BsonDocument("ActivityId", _GooglePlusActivities.ActivityId);
                        var update = Builders<BsonDocument>.Update.Set("PlusonersCount", _GooglePlusActivities.PlusonersCount).Set("RepliesCount", _GooglePlusActivities.RepliesCount).Set("ResharersCount", _GooglePlusActivities.ResharersCount);
                        gplusFeedRepo.Update<MongoGplusFeed>(update, filter);
                    }
                    new Thread(delegate () {
                       GetGooglePlusComments(_GooglePlusActivities.ActivityId, AcessToken, ProfileId,settings,_logger);
                    }).Start();

                    new Thread(delegate ()
                    {
                        //GetGooglePlusLikes(_GooglePlusActivities.ActivityId, AcessToken, ProfileId, status);
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUserActivities => " + ex.Message);
            }
        }

        public static string AddGaSites(string profiledata, long userId, long groupId, Helper.Cache _redisCache, Helper.AppSettings _appSettings, Model.DatabaseRepository dbr, IHostingEnvironment _appEnv)
        {
            int isSaved = 0;
            Analytics _Analytics = new Analytics(_appSettings.GoogleConsumerKey, _appSettings.GoogleConsumerSecret, _appSettings.GoogleRedirectUri);
            Domain.Socioboard.Models.GoogleAnalyticsAccount _GoogleAnalyticsAccount;
            string[] GAdata = Regex.Split(profiledata, "<:>");
            _GoogleAnalyticsAccount = Repositories.GplusRepository.getGAAccount(GAdata[5], _redisCache, dbr);

            if (_GoogleAnalyticsAccount != null && _GoogleAnalyticsAccount.IsActive == false)
            {
                try
                {
                    _GoogleAnalyticsAccount.UserId = userId;
                    _GoogleAnalyticsAccount.IsActive = true;
                    _GoogleAnalyticsAccount.EntryDate = DateTime.UtcNow;
                    _GoogleAnalyticsAccount.EmailId = GAdata[4];
                    _GoogleAnalyticsAccount.GaAccountId = GAdata[2];
                    _GoogleAnalyticsAccount.GaAccountName = GAdata[3];
                    _GoogleAnalyticsAccount.GaWebPropertyId = GAdata[7];
                    _GoogleAnalyticsAccount.GaProfileId = GAdata[5];
                    _GoogleAnalyticsAccount.GaProfileName = GAdata[6];
                    _GoogleAnalyticsAccount.AccessToken = GAdata[0];
                    _GoogleAnalyticsAccount.RefreshToken = GAdata[1];
                    _GoogleAnalyticsAccount.WebsiteUrl = GAdata[8];
                    string visits = string.Empty;
                    string pageviews = string.Empty;
                    try
                    {
                        string analytics = _Analytics.getAnalyticsData(GAdata[5], "ga:visits,ga:pageviews", DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"), GAdata[0]);
                        JObject JData = JObject.Parse(analytics);
                        visits = JData["totalsForAllResults"]["ga:visits"].ToString();
                        pageviews = JData["totalsForAllResults"]["ga:pageviews"].ToString();
                    }
                    catch (Exception ex)
                    {
                        visits = "0";
                        pageviews = "0";
                    }
                    _GoogleAnalyticsAccount.Views = Double.Parse(pageviews);
                    _GoogleAnalyticsAccount.Visits = Double.Parse(visits);
                    _GoogleAnalyticsAccount.ProfilePicUrl = "https://www.socioboard.com/Contents/Socioboard/images/analytics_img.png";
                    _GoogleAnalyticsAccount.EntryDate = DateTime.UtcNow;


                }
                catch (Exception ex)
                {

                }
                dbr.Update<Domain.Socioboard.Models.GoogleAnalyticsAccount>(_GoogleAnalyticsAccount);
            }
            else if (_GoogleAnalyticsAccount == null)
            {
                try
                {
                    _GoogleAnalyticsAccount = new Domain.Socioboard.Models.GoogleAnalyticsAccount();
                    _GoogleAnalyticsAccount.UserId = userId;
                    _GoogleAnalyticsAccount.IsActive = true;
                    _GoogleAnalyticsAccount.EntryDate = DateTime.UtcNow;
                    _GoogleAnalyticsAccount.EmailId = GAdata[4];
                    _GoogleAnalyticsAccount.GaAccountId = GAdata[2];
                    _GoogleAnalyticsAccount.GaAccountName = GAdata[3];
                    _GoogleAnalyticsAccount.GaWebPropertyId = GAdata[7];
                    _GoogleAnalyticsAccount.GaProfileId = GAdata[5];
                    _GoogleAnalyticsAccount.GaProfileName = GAdata[6];
                    _GoogleAnalyticsAccount.AccessToken = GAdata[0];
                    _GoogleAnalyticsAccount.RefreshToken = GAdata[1];
                    _GoogleAnalyticsAccount.WebsiteUrl = GAdata[8];
                    string visits = string.Empty;
                    string pageviews = string.Empty;
                    try
                    {
                        string analytics = _Analytics.getAnalyticsData(GAdata[5], "ga:visits,ga:pageviews", DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"), GAdata[0]);
                        JObject JData = JObject.Parse(analytics);
                        visits = JData["totalsForAllResults"]["ga:visits"].ToString();
                        pageviews = JData["totalsForAllResults"]["ga:pageviews"].ToString();
                    }
                    catch (Exception ex)
                    {
                        visits = "0";
                        pageviews = "0";
                    }
                    _GoogleAnalyticsAccount.Views = Double.Parse(pageviews);
                    _GoogleAnalyticsAccount.Visits = Double.Parse(visits);
                    _GoogleAnalyticsAccount.ProfilePicUrl = "https://www.socioboard.com/Themes/Socioboard/Contents/img/analytics_img.png";
                    _GoogleAnalyticsAccount.EntryDate = DateTime.UtcNow;


                }
                catch (Exception ex)
                {

                }
                isSaved = dbr.Add<Domain.Socioboard.Models.GoogleAnalyticsAccount>(_GoogleAnalyticsAccount);
            }

            else if (_GoogleAnalyticsAccount != null && _GoogleAnalyticsAccount.IsActive == true)
            {
                if (_GoogleAnalyticsAccount.UserId != userId)
                {
                    return "added by other";
                }
            }


            if (isSaved == 1)
            {
                List<Domain.Socioboard.Models.GoogleAnalyticsAccount> lstgaAcc = dbr.Find<Domain.Socioboard.Models.GoogleAnalyticsAccount>(t => t.GaProfileId.Equals(_GoogleAnalyticsAccount.GaProfileId)).ToList();
                if (lstgaAcc != null && lstgaAcc.Count() > 0)
                {
                    isSaved = GroupProfilesRepository.AddGroupProfile(groupId, lstgaAcc.First().GaProfileId, lstgaAcc.First().GaProfileName, userId, lstgaAcc.First().ProfilePicUrl, Domain.Socioboard.Enum.SocialProfileType.GoogleAnalytics, dbr);
                    //codes to delete cache
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheUserProfileCount + userId);
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGroupProfiles + groupId);


                }

            }


            return isSaved.ToString();
        }


        public static void GetGooglePlusComments(string feedId, string AccessToken, string profileId, Helper.AppSettings settings, ILogger _logger)
        {
            MongoRepository gplusCommentRepo = new MongoRepository("GoogleplusComments",settings);
            oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus(settings.GoogleConsumerKey,settings.GoogleConsumerSecret,settings.GoogleRedirectUri);

            MongoGoogleplusComments _GoogleplusComments = new MongoGoogleplusComments();
            try
            {
                string _Comments = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetCommentListByActivityId.Replace("[ActivityId]", feedId) + "?key=" + settings.GoogleApiKey, AccessToken);
                JObject J_Comments = JObject.Parse(_Comments);
                List<MongoGoogleplusComments> lstGoogleplusComments = new List<MongoGoogleplusComments>();
                foreach (var item in J_Comments["items"])
                {
                    try
                    {
                        _GoogleplusComments.Id = ObjectId.GenerateNewId();
                        _GoogleplusComments.Comment = item["object"]["content"].ToString();
                        _GoogleplusComments.CommentId = item["id"].ToString();
                        _GoogleplusComments.CreatedDate = Convert.ToDateTime(item["published"]).ToString("yyyy/MM/dd HH:mm:ss");
                        _GoogleplusComments.FeedId = feedId;
                        _GoogleplusComments.FromId = item["actor"]["id"].ToString();
                        _GoogleplusComments.FromImageUrl = item["actor"]["image"]["url"].ToString();
                        _GoogleplusComments.FromName = item["actor"]["url"].ToString();
                        _GoogleplusComments.FromUrl = item["actor"]["url"].ToString();
                        _GoogleplusComments.GplusUserId = profileId;

                        lstGoogleplusComments.Add(_GoogleplusComments);

                        //if (!objGoogleplusCommentsRepository.IsExist(_GoogleplusComments.CommentId))
                        //{
                        //    objGoogleplusCommentsRepository.Add(_GoogleplusComments);
                        //}

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }

                gplusCommentRepo.AddList(lstGoogleplusComments);

            }
            catch (Exception ex)
            {
            }

        }

        //public void GetGooglePlusLikes(string feedId, string AccessToken, string ProfileId, int Status, Helper.AppSettings settings, ILogger _logger)
        //{
        //    oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus();

        //    Domain.Socioboard.Domain.GoogleplusLike _GoogleplusLike = new Domain.Socioboard.Domain.GoogleplusLike();
        //    try
        //    {
        //        string _Likes = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strLike.Replace("[ActivityId]", feedId) + "?key=" + ConfigurationManager.AppSettings["Api_Key"].ToString(), AccessToken);
        //        JObject J_Likes = JObject.Parse(_Likes);

        //        foreach (var item in J_Likes["items"])
        //        {
        //            try
        //            {
        //                _GoogleplusLike.Id = Guid.NewGuid();
        //                _GoogleplusLike.FromId = item["id"].ToString();
        //                _GoogleplusLike.FromImageUrl = item["image"]["url"].ToString();
        //                _GoogleplusLike.FromName = item["displayName"].ToString();
        //                _GoogleplusLike.ProfileId = ProfileId;
        //                _GoogleplusLike.FromUrl = item["url"].ToString();
        //                _GoogleplusLike.FeedId = feedId;

        //                if (!objGoogleplusCommentsRepository.IsLikeExist(_GoogleplusLike.FromId, feedId))
        //                {
        //                    objGoogleplusCommentsRepository.AddLikes(_GoogleplusLike);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                logger.Error(ex.Message);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }

        //}

        public static string DeleteProfile(Model.DatabaseRepository dbr, string profileId, long userId, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.GoogleAnalyticsAccount fbAcc = dbr.Find<Domain.Socioboard.Models.GoogleAnalyticsAccount>(t => t.GaProfileId.Equals(profileId) && t.UserId == userId && t.IsActive).FirstOrDefault();
            Domain.Socioboard.Models.User user = dbr.Find<Domain.Socioboard.Models.User>(t => t.Id.Equals(userId) && t.EmailId == fbAcc.EmailId && t.EmailValidateToken == "Google").FirstOrDefault();
            if (user != null)
            {
                dbr.Delete<Domain.Socioboard.Models.User>(user);
                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheFacebookAccount + userId);
            }

            if (fbAcc != null)
            {
                //fbAcc.IsActive = false;
                //dbr.Update<Domain.Socioboard.Models.GoogleAnalyticsAccount>(fbAcc);
                dbr.Delete<Domain.Socioboard.Models.GoogleAnalyticsAccount>(fbAcc);
                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGAAccount + profileId);
                return "Deleted";
            }
            else
            {
                return "Account Not Exist";
            }
        }

        public static string DeleteGplusProfile(Model.DatabaseRepository dbr, string profileId, long userId, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.Googleplusaccounts fbAcc = dbr.Find<Domain.Socioboard.Models.Googleplusaccounts>(t => t.GpUserId.Equals(profileId) && t.UserId == userId && t.IsActive).FirstOrDefault();
            if (fbAcc != null)
            {
                //fbAcc.IsActive = false;
                //dbr.Update<Domain.Socioboard.Models.Googleplusaccounts>(fbAcc);
                dbr.Delete<Domain.Socioboard.Models.Googleplusaccounts>(fbAcc);
                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusAccount + profileId);
                return "Deleted";
            }
            else
            {
                return "Account Not Exist";
            }
        }
        public static List<Domain.Socioboard.Models.Mongo.MongoGplusFeed> getgoogleplusActivity(string profileId,Helper.Cache _redisCache,Helper.AppSettings _appSettings)
        {
            MongoRepository gplusFeedRepo = new MongoRepository("MongoGplusFeed", _appSettings);
            List<Domain.Socioboard.Models.Mongo.MongoGplusFeed> iMmemMongoGplusFeed = _redisCache.Get<List<Domain.Socioboard.Models.Mongo.MongoGplusFeed>>(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusRecent100Feeds + profileId);
            if(iMmemMongoGplusFeed!=null && iMmemMongoGplusFeed.Count>0)
            {
                return iMmemMongoGplusFeed;
            }
            else
            {
                var builder = Builders<MongoGplusFeed>.Sort;
                var sort = builder.Descending(t => t.PublishedDate);
                var ret = gplusFeedRepo.FindWithRange<Domain.Socioboard.Models.Mongo.MongoGplusFeed>(t => t.GpUserId.Equals(profileId), sort, 0, 100);
                var task=Task.Run(async() =>{
                    return await ret;
                });
                IList<Domain.Socioboard.Models.Mongo.MongoGplusFeed> lstMongoGplusFeed = task.Result.ToList();
                if (lstMongoGplusFeed.Count>0)
                {
                    _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusRecent100Feeds + profileId, lstMongoGplusFeed.ToList());
                }
                return lstMongoGplusFeed.ToList();
            }
        }

        public static List<Domain.Socioboard.Models.Mongo.MongoGplusFeed> getgoogleplusActivity(string profileId, Helper.Cache _redisCache, Helper.AppSettings _appSettings, int skip, int count, string postType)
        {
            MongoRepository gplusFeedRepo = new MongoRepository("MongoGplusFeed", _appSettings);            
                var builder = Builders<MongoGplusFeed>.Sort;
                var sort = builder.Descending(t => t.PublishedDate);
                var ret = gplusFeedRepo.FindWithRange<Domain.Socioboard.Models.Mongo.MongoGplusFeed>(t => t.GpUserId.Equals(profileId) && t.AttachmentType.Equals(postType), sort, skip, count);
                var task = Task.Run(async () => {
                    return await ret;
                });
                IList<Domain.Socioboard.Models.Mongo.MongoGplusFeed> lstMongoGplusFeed = task.Result.ToList();

                return lstMongoGplusFeed.ToList();
        }

        public static string DeleteYoutubeChannelProfile(Model.DatabaseRepository dbr, string profileId, long userId, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.YoutubeChannel fbAcc = dbr.Find<Domain.Socioboard.Models.YoutubeChannel>(t => t.YtubeChannelId.Equals(profileId) && t.UserId == userId && t.IsActive).FirstOrDefault();
            if (fbAcc != null)
            {
                //fbAcc.IsActive = false;
                //dbr.Update<Domain.Socioboard.Models.YoutubeChannel>(fbAcc);
                dbr.Delete<Domain.Socioboard.Models.YoutubeChannel>(fbAcc);
                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheYTChannel + profileId);
                return "Deleted";
            }
            else
            {
                return "Account Not Exist";
            }
        }

        public static string DeleteGAProfile(Model.DatabaseRepository dbr, string profileId, long userId, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.GoogleAnalyticsAccount fbAcc = dbr.Find<Domain.Socioboard.Models.GoogleAnalyticsAccount>(t => t.GaProfileId.Equals(profileId) && t.UserId == userId && t.IsActive).FirstOrDefault();
            if (fbAcc != null)
            {
                fbAcc.IsActive = false;
                dbr.Update<Domain.Socioboard.Models.GoogleAnalyticsAccount>(fbAcc);
                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGAAccount + profileId);
                return "Deleted";
            }
            else
            {
                return "Account Not Exist";
            }
        }

        #region Repository codes for Youtube Channel.....


        public static Domain.Socioboard.Models.YoutubeChannel getYTChannel(string YtChannelId, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            try
            {
                Domain.Socioboard.Models.YoutubeChannel inMemYTChannel = _redisCache.Get<Domain.Socioboard.Models.YoutubeChannel>(Domain.Socioboard.Consatants.SocioboardConsts.CacheYTChannel + YtChannelId);
                if (inMemYTChannel != null)
                {
                    return inMemYTChannel;
                }
            }
            catch { }

            List<Domain.Socioboard.Models.YoutubeChannel> lstYTChannel = dbr.Find<Domain.Socioboard.Models.YoutubeChannel>(t => t.YtubeChannelId.Equals(YtChannelId)).ToList();
            if (lstYTChannel != null && lstYTChannel.Count() > 0)
            {
                _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheYTChannel + YtChannelId, lstYTChannel.First());
                return lstYTChannel.First();
            }
            else
            {
                return null;
            }



        }


        public static string AddYoutubeChannels(string profiledata, long userId, long groupId, Helper.Cache _redisCache, Helper.AppSettings _appSettings, Model.DatabaseRepository dbr, IHostingEnvironment _appEnv)
        {
            int isSaved = 0;
            Channels _Channels = new Channels(_appSettings.GoogleConsumerKey, _appSettings.GoogleConsumerSecret, _appSettings.GoogleRedirectUri);
            Domain.Socioboard.Models.YoutubeChannel _YoutubeChannel;
            string[] YTdata = Regex.Split(profiledata, "<:>");
            _YoutubeChannel = Repositories.GplusRepository.getYTChannel(YTdata[2], _redisCache, dbr);

            string channel_email = "";
            try
            {
                Video _videos = new Video(_appSettings.GoogleConsumerKey, _appSettings.GoogleConsumerSecret, _appSettings.GoogleRedirectUri);
                string Channel_info = _videos.Get_Channel_info(YTdata[0]);
                JObject JChannel_info = JObject.Parse(Channel_info);
                channel_email = JChannel_info["email"].ToString();
            }
            catch
            {

            }

            if (_YoutubeChannel != null)
            {
                if (_YoutubeChannel != null && _YoutubeChannel.IsActive == true)
                {
                    if (_YoutubeChannel.UserId == userId)
                    {
                        return "Youtube already added by you";
                    }
                    else if (_YoutubeChannel.UserId != userId)
                    {
                        return "Youtube added by any other";
                    }
                }
                else
                {

                    try
                    {

                        _YoutubeChannel.UserId = userId;
                        _YoutubeChannel.YtubeChannelId = YTdata[2];
                        _YoutubeChannel.YtubeChannelName = YTdata[3];
                        _YoutubeChannel.ChannelpicUrl = YTdata[9];
                        _YoutubeChannel.WebsiteUrl = "https://www.youtube.com/channel/" + YTdata[2];
                        _YoutubeChannel.EntryDate = DateTime.UtcNow;
                        _YoutubeChannel.YtubeChannelDescription = YTdata[4];
                        _YoutubeChannel.IsActive = true;
                        _YoutubeChannel.AccessToken = YTdata[0];
                        _YoutubeChannel.RefreshToken = YTdata[1];
                        _YoutubeChannel.PublishingDate = Convert.ToDateTime(YTdata[5]);
                        _YoutubeChannel.VideosCount = Convert.ToDouble(YTdata[8]);
                        _YoutubeChannel.CommentsCount = Convert.ToDouble(YTdata[7]);
                        _YoutubeChannel.SubscribersCount = Convert.ToDouble(YTdata[10]);
                        _YoutubeChannel.ViewsCount = Convert.ToDouble(YTdata[6]);
                        _YoutubeChannel.Channel_EmailId = channel_email;
                        _YoutubeChannel.Days90Update = false;
                    }
                    catch (Exception ex)
                    {

                    }
                    isSaved = dbr.Update<Domain.Socioboard.Models.YoutubeChannel>(_YoutubeChannel);
                }
            }
            else
            {
                _YoutubeChannel = new Domain.Socioboard.Models.YoutubeChannel();
                try
                {
                    _YoutubeChannel.UserId = userId;
                    _YoutubeChannel.YtubeChannelId = YTdata[2];
                    _YoutubeChannel.YtubeChannelName = YTdata[3];
                    _YoutubeChannel.ChannelpicUrl = YTdata[9];
                    _YoutubeChannel.WebsiteUrl = "https://www.youtube.com/channel/" + YTdata[2];
                    _YoutubeChannel.EntryDate = DateTime.UtcNow;
                    _YoutubeChannel.YtubeChannelDescription = YTdata[4];
                    _YoutubeChannel.IsActive = true;
                    _YoutubeChannel.AccessToken = YTdata[0];
                    _YoutubeChannel.RefreshToken = YTdata[1];
                    _YoutubeChannel.PublishingDate = Convert.ToDateTime(YTdata[5]);
                    _YoutubeChannel.VideosCount = Convert.ToDouble(YTdata[8]);
                    _YoutubeChannel.CommentsCount = Convert.ToDouble(YTdata[7]);
                    _YoutubeChannel.SubscribersCount = Convert.ToDouble(YTdata[10]);
                    _YoutubeChannel.ViewsCount = Convert.ToDouble(YTdata[6]);
                    _YoutubeChannel.Channel_EmailId = channel_email;
                    _YoutubeChannel.Days90Update = false;

                }
                catch (Exception ex)
                {

                }
                isSaved = dbr.Add<Domain.Socioboard.Models.YoutubeChannel>(_YoutubeChannel);
            }

            if (isSaved == 1)
            {
                List<Domain.Socioboard.Models.YoutubeChannel> lstytChannel = dbr.Find<Domain.Socioboard.Models.YoutubeChannel>(t => t.YtubeChannelId.Equals(_YoutubeChannel.YtubeChannelId)).ToList();
                if (lstytChannel != null && lstytChannel.Count() > 0)
                {
                    isSaved = GroupProfilesRepository.AddGroupProfile(groupId, lstytChannel.First().YtubeChannelId, lstytChannel.First().YtubeChannelName, userId, lstytChannel.First().ChannelpicUrl, Domain.Socioboard.Enum.SocialProfileType.YouTube, dbr);
                    //codes to delete cache
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheUserProfileCount + userId);
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGroupProfiles + groupId);

                }

            }
            return isSaved.ToString();
        }



        public static void InitialYtFeedsAdd(string channelid, string AccessToken, Helper.AppSettings settings, ILogger _logger)
        {

            MongoRepository youtubefeedsrepo = new MongoRepository("YoutubeVideos", settings);
            Video _Videos = new Video(settings.GoogleConsumerKey, settings.GoogleConsumerSecret, settings.GoogleRedirectUri);



            try
            {
                
                string videos = _Videos.Get_Videosby_Channel(channelid, AccessToken, "snippet,contentDetails,statistics");
                JObject JVideodata = JObject.Parse(videos);
                string videoimage = null;

                foreach (var item in JVideodata["items"])
                {
                    MongoYoutubeFeeds _YoutubeFeeds = new MongoYoutubeFeeds();

                    string publishdate = item["snippet"]["publishedAt"].ToString();
                    string title = item["snippet"]["title"].ToString();
                    string description = item["snippet"]["description"].ToString();
                    if (description == "")
                    {
                        description = "No Description";
                    }
                    try
                    {
                        videoimage = item["snippet"]["thumbnails"]["high"]["url"].ToString();
                    }
                    catch
                    {
                        videoimage = item["snippet"]["thumbnails"]["medium"]["url"].ToString();
                    }
                    string videoid = item["contentDetails"]["upload"]["videoId"].ToString();

                    //Add in Mongo via mongo_model

                    _YoutubeFeeds.Id = ObjectId.GenerateNewId();
                    _YoutubeFeeds.YtChannelId = channelid;
                    _YoutubeFeeds.YtVideoId = videoid;
                    _YoutubeFeeds.VdoTitle = title;
                    _YoutubeFeeds.VdoDescription = description;
                    _YoutubeFeeds.VdoPublishDate = publishdate;
                    _YoutubeFeeds.VdoImage = videoimage;
                    _YoutubeFeeds.VdoUrl = "https://www.youtube.com/watch?v=" + videoid;
                    _YoutubeFeeds.VdoEmbed = "https://www.youtube.com/embed/" + videoid;

                    youtubefeedsrepo.Add(_YoutubeFeeds);

                }

            }
            catch(Exception ex)
            {

            }


        }


        public static List<Domain.Socioboard.Models.Mongo.YoutubeFeed> GetYoutubeFeeds(string ChannelId, string sortType, Helper.Cache _redisCache, Helper.AppSettings settings)
        {
            if (sortType == "none")
            {
                List<Domain.Socioboard.Models.Mongo.YoutubeFeed> lstyoutubefeed = new List<Domain.Socioboard.Models.Mongo.YoutubeFeed>();
                List<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList> lstYoutubeFeedsLs = new List<YoutubeVideoDetailsList>();
                MongoRepository mongorepo = new MongoRepository("YoutubeVideosDetailedList", settings);
                var builder = Builders<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList>.Sort;
                var sort = builder.Descending(t => t.publishedAt);
                var result = mongorepo.Find<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList>(t => t.channelId.Equals(ChannelId));
                var task = Task.Run(async () =>
                {
                    return await result;
                });
                IList<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList> lstYtFeeds = task.Result;
                IList<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList> lstYtFeeds_sorted;
                try
                {
                    lstYtFeeds_sorted = lstYtFeeds.OrderByDescending(t => Convert.ToDateTime(t.publishedAt)).ToList();
                }
                catch
                {
                    lstYtFeeds_sorted = lstYtFeeds;
                }
                lstYoutubeFeedsLs = lstYtFeeds_sorted.ToList();
                List<string> channelIds = new List<string>();
                foreach (var x in lstYoutubeFeedsLs)
                {
                    channelIds.Add(x.YtvideoId);
                }
                MongoRepository mongorepocmnt = new MongoRepository("YoutubeVideosComments", settings);
                var result_cmnt = mongorepocmnt.Find<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => channelIds.Contains(t.videoId) && t.active != false);
                var task_cmnt = Task.Run(async () =>
                {
                    return await result_cmnt;
                });
                IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtComments = task_cmnt.Result;
                List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> tempData = lstYtComments.ToList();
                foreach (var item_time in tempData)
                {
                    var time = Convert.ToDateTime(item_time.publishTime).AddMinutes(330);
                    item_time.publishTime = time.ToString();
                }
                foreach (var item in lstYoutubeFeedsLs)
                {
                    Domain.Socioboard.Models.Mongo.YoutubeFeed _intafeed = new Domain.Socioboard.Models.Mongo.YoutubeFeed();
                    List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtPostCommentTemp = tempData.Where(t => t.videoId == item.YtvideoId).ToList();
                    lstYtPostCommentTemp = lstYtPostCommentTemp.OrderByDescending(t => Convert.ToDateTime(t.publishTime)).ToList();
                    _intafeed._youtubefeed = item;
                    _intafeed._youtubecomment = lstYtPostCommentTemp.ToList();
                    lstyoutubefeed.Add(_intafeed);
                }
                return lstyoutubefeed;
            }
            else
            {
                List<Domain.Socioboard.Models.Mongo.YoutubeFeed> lstyoutubefeed = new List<Domain.Socioboard.Models.Mongo.YoutubeFeed>();
                List<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList> lstYoutubeFeedsLs = new List<YoutubeVideoDetailsList>();
                MongoRepository mongorepo = new MongoRepository("YoutubeVideosDetailedList", settings);
                var builder = Builders<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList>.Sort;
                var sort = builder.Descending(t => t.publishedAt);
                var result = mongorepo.Find<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList>(t => t.channelId.Equals(ChannelId));
                var task = Task.Run(async () =>
                {
                    return await result;
                });
                IList<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList> lstYtFeeds = task.Result;
                IList<Domain.Socioboard.Models.Mongo.YoutubeVideoDetailsList> lstYtFeeds_sorted;

                if (sortType == "mLikes")
                {
                    lstYtFeeds_sorted = lstYtFeeds.OrderByDescending(t => (t.likeCount)).ToList();
                }
                else if (sortType == "mComments")
                {
                    lstYtFeeds_sorted = lstYtFeeds.OrderByDescending(t => (t.commentCount)).ToList();
                }
                else if (sortType == "lLikes")
                {
                    lstYtFeeds_sorted = lstYtFeeds.OrderBy(t => (t.likeCount)).ToList();
                }
                else
                {
                    lstYtFeeds_sorted = lstYtFeeds.OrderBy(t => (t.commentCount)).ToList();
                }
                lstYoutubeFeedsLs = lstYtFeeds_sorted.ToList();
                List<string> channelIds = new List<string>();
                foreach (var x in lstYoutubeFeedsLs)
                {
                    channelIds.Add(x.YtvideoId);
                }
                MongoRepository mongorepocmnt = new MongoRepository("YoutubeVideosComments", settings);
                var result_cmnt = mongorepocmnt.Find<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => channelIds.Contains(t.videoId) && t.active != false);
                var task_cmnt = Task.Run(async () =>
                {
                    return await result_cmnt;
                });
                IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtComments = task_cmnt.Result;
                List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> tempData = lstYtComments.ToList();
                foreach (var item_time in tempData)
                {
                    var time = Convert.ToDateTime(item_time.publishTime).AddMinutes(330);
                    item_time.publishTime = time.ToString();
                }
                foreach (var item in lstYoutubeFeedsLs)
                {
                    Domain.Socioboard.Models.Mongo.YoutubeFeed _intafeed = new Domain.Socioboard.Models.Mongo.YoutubeFeed();
                    List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtPostCommentTemp = tempData.Where(t => t.videoId == item.YtvideoId).ToList();
                    lstYtPostCommentTemp = lstYtPostCommentTemp.OrderByDescending(t => Convert.ToDateTime(t.publishTime)).ToList();
                    _intafeed._youtubefeed = item;
                    _intafeed._youtubecomment = lstYtPostCommentTemp.ToList();
                    lstyoutubefeed.Add(_intafeed);
                }
                return lstyoutubefeed;
            }
        }



        public static void InitialYtCommentsAdd(string channelid, string AccessToken, Helper.AppSettings settings, ILogger _logger)
        {
            //Find Youtube feeds by channel Id
            List<Domain.Socioboard.Models.Mongo.MongoYoutubeFeeds> lstyoutubefeed = new List<Domain.Socioboard.Models.Mongo.MongoYoutubeFeeds>();
            MongoRepository mongorepo = new MongoRepository("YoutubeVideos", settings);
            var builder = Builders<Domain.Socioboard.Models.Mongo.MongoYoutubeFeeds>.Sort;
            var sort = builder.Descending(t => t.VdoPublishDate);
            var result = mongorepo.Find<Domain.Socioboard.Models.Mongo.MongoYoutubeFeeds>(t => t.YtChannelId.Equals(channelid));
            var task = Task.Run(async () =>
            {
                return await result;
            });
            IList<Domain.Socioboard.Models.Mongo.MongoYoutubeFeeds> lstYtFeeds = task.Result;
            List<string> VideoIds = new List<string>();
            VideoIds = lstYtFeeds.Select(t => t.YtVideoId).ToList();
            //MongoRepository youtubecommentsrepo = new MongoRepository("YoutubeVideosComments", settings);
            Video _Videos = new Video(settings.GoogleConsumerKey, settings.GoogleConsumerSecret, settings.GoogleRedirectUri);
            foreach (string items in VideoIds)
            {
                try
                {
                    string Comments = _Videos.Get_CommentsBy_VideoId(items, AccessToken, "statistics,contentDetails,snippet", settings.GoogleApiKey);
                    JObject JCommentdata = JObject.Parse(Comments);
                    foreach (var item in JCommentdata["items"])
                    {
                        MongoYoutubeComments _YoutubeComments = new MongoYoutubeComments();
                        try
                        {
                            string videoId = item["snippet"]["videoId"].ToString();
                            string commentId = item["id"].ToString();
                            string authorDisplayName = item["snippet"]["topLevelComment"]["snippet"]["authorDisplayName"].ToString();
                            string authorProfileImageUrl = item["snippet"]["topLevelComment"]["snippet"]["authorProfileImageUrl"].ToString();
                            authorProfileImageUrl = authorProfileImageUrl.Replace(".jpg", "");
                            string authorChannelUrl = item["snippet"]["topLevelComment"]["snippet"]["authorChannelUrl"].ToString();
                            string authorChannelId = item["snippet"]["topLevelComment"]["snippet"]["authorChannelId"]["value"].ToString();
                            string textDisplay = item["snippet"]["topLevelComment"]["snippet"]["textDisplay"].ToString();
                            string textOriginal = item["snippet"]["topLevelComment"]["snippet"]["textOriginal"].ToString();
                            string viewerRating = item["snippet"]["topLevelComment"]["snippet"]["viewerRating"].ToString();
                            string likeCount = item["snippet"]["topLevelComment"]["snippet"]["likeCount"].ToString();
                            string publishedAt = item["snippet"]["topLevelComment"]["snippet"]["publishedAt"].ToString();
                            string updatedAt = item["snippet"]["topLevelComment"]["snippet"]["updatedAt"].ToString();
                            string totalReplyCount = item["snippet"]["totalReplyCount"].ToString();


                            //Add in Mongo via mongo_model


                            _YoutubeComments.Id = ObjectId.GenerateNewId();
                            _YoutubeComments.videoId = videoId;
                            _YoutubeComments.commentId = commentId;
                            _YoutubeComments.authorDisplayName = authorDisplayName;
                            _YoutubeComments.authorProfileImageUrl = authorProfileImageUrl;
                            _YoutubeComments.authorChannelUrl = authorChannelUrl;
                            _YoutubeComments.authorChannelId = authorChannelId;
                            _YoutubeComments.commentDisplay = textDisplay;
                            _YoutubeComments.commentOriginal = textOriginal;
                            _YoutubeComments.viewerRating = viewerRating;
                            _YoutubeComments.likesCount = likeCount;
                            _YoutubeComments.publishTime = publishedAt;
                            _YoutubeComments.updatedTime = updatedAt;
                            _YoutubeComments.totalReplyCount = totalReplyCount;
                            _YoutubeComments.ChannelId = channelid;
                            _YoutubeComments.active = true;
                            _YoutubeComments.review = false;
                            _YoutubeComments.sbGrpTaskAssign = false;
                            try
                            {
                                MongoRepository _mongoRepo = new MongoRepository("YoutubeVideosComments", settings);
                                var ret = _mongoRepo.Find<MongoYoutubeComments>(t => t.commentId.Equals(_YoutubeComments.commentId));
                                var task_Reports = Task.Run(async () =>
                                {
                                    return await ret;
                                });
                                int count_Reports = task_Reports.Result.Count;
                                if (count_Reports < 1)
                                {
                                    try
                                    {
                                        _mongoRepo.Add(_YoutubeComments);
                                    }
                                    catch { }
                                }
                                else
                                {
                                    try
                                    {
                                        FilterDefinition<BsonDocument> filter = new BsonDocument("commentId", _YoutubeComments.commentId);
                                        var update = Builders<BsonDocument>.Update.Set("commentDisplay", _YoutubeComments.commentDisplay).Set("commentOriginal", _YoutubeComments.commentOriginal).Set("publishTimeUnix", _YoutubeComments.publishTimeUnix).Set("review", _YoutubeComments.review).Set("sbGrpTaskAssign", _YoutubeComments.sbGrpTaskAssign);
                                        _mongoRepo.Update<MongoYoutubeComments>(update, filter);
                                    }
                                    catch { }
                                }
                            }
                            catch (Exception ex) { }


                        }
                        catch (Exception ex)
                        {

                        }

                    }

                }
                catch (Exception ex)
                {

                }
            }


        }


        public static void InitialYtReplyCommentsAdd(string channelid, string AccessToken, Helper.AppSettings settings, ILogger _logger)
        {
            List<Domain.Socioboard.Models.Mongo.MongoYoutubeFeeds> lstyoutubefeed = new List<Domain.Socioboard.Models.Mongo.MongoYoutubeFeeds>();
            MongoRepository mongoreposs = new MongoRepository("YoutubeVideosComments", settings);
            var result = mongoreposs.Find<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => t.ChannelId == channelid);
            var task = Task.Run(async () =>
            {
                return await result;
            });
            IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstParentComment = task.Result;
            foreach (var itemMainComments in lstParentComment)
            {
                Video objVideo = new Video(settings.GoogleConsumerKey, settings.GoogleConsumerSecret, settings.GoogleRedirectUri);
                try
                {
                    string commentsReply = objVideo.Get_CommentsRepliesBy_CmParentId(itemMainComments.commentId, settings.GoogleApiKey);
                    JObject jCommentsReply = JObject.Parse(commentsReply);
                    MongoYoutubeComments _ObjMongoYtCommentsReply;
                    foreach (var itemReply in jCommentsReply["items"])
                    {
                        _ObjMongoYtCommentsReply = new MongoYoutubeComments();
                        _ObjMongoYtCommentsReply.Id = ObjectId.GenerateNewId();
                        try
                        {
                            _ObjMongoYtCommentsReply.ChannelId = itemMainComments.ChannelId;
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.videoId = itemMainComments.videoId;
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.commentId = itemReply["id"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.authorDisplayName = itemReply["snippet"]["authorDisplayName"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.authorProfileImageUrl = itemReply["snippet"]["authorProfileImageUrl"].ToString().Replace(".jpg", "");
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.authorChannelUrl = itemReply["snippet"]["authorChannelUrl"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.authorChannelId = itemReply["snippet"]["authorChannelId"]["value"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.commentDisplay = itemReply["snippet"]["textDisplay"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.commentOriginal = itemReply["snippet"]["textOriginal"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.viewerRating = itemReply["snippet"]["viewerRating"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.likesCount = itemReply["snippet"]["likeCount"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.publishTime = itemReply["snippet"]["publishedAt"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.publishTimeUnix = Helper.DateExtension.ToUnixTimestamp(Convert.ToDateTime(_ObjMongoYtCommentsReply.publishTime));
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.updatedTime = itemReply["snippet"]["updatedAt"].ToString();
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.totalReplyCount = "ReplyType";
                        }
                        catch { }
                        try
                        {
                            _ObjMongoYtCommentsReply.parentIdforReply = itemReply["snippet"]["parentId"].ToString();
                        }
                        catch { }
                        _ObjMongoYtCommentsReply.active = true;
                        _ObjMongoYtCommentsReply.review = false;
                        _ObjMongoYtCommentsReply.sbGrpTaskAssign = false;
                        try
                        {
                            MongoRepository _mongoRepo = new MongoRepository("YoutubeVideosCommentsReply", settings);
                            var ret = _mongoRepo.Find<MongoYoutubeComments>(t => t.commentId.Equals(_ObjMongoYtCommentsReply.commentId));
                            var task_Reports = Task.Run(async () =>
                            {
                                return await ret;
                            });
                            int count_Reports = task_Reports.Result.Count;
                            if (count_Reports < 1)
                            {
                                try
                                {
                                    _mongoRepo.Add(_ObjMongoYtCommentsReply);
                                }
                                catch { }
                            }
                            else
                            {
                                try
                                {
                                    FilterDefinition<BsonDocument> filter = new BsonDocument("commentId", _ObjMongoYtCommentsReply.commentId);
                                    var update = Builders<BsonDocument>.Update.Set("commentDisplay", _ObjMongoYtCommentsReply.commentDisplay).Set("commentOriginal", _ObjMongoYtCommentsReply.commentOriginal).Set("publishTimeUnix", _ObjMongoYtCommentsReply.publishTimeUnix).Set("review", _ObjMongoYtCommentsReply.review).Set("sbGrpTaskAssign", _ObjMongoYtCommentsReply.sbGrpTaskAssign);
                                    _mongoRepo.Update<MongoYoutubeComments>(update, filter);
                                }
                                catch { }
                            }
                        }
                        catch (Exception ex) { }
                    }
                }
                catch
                {

                }
            }
        }

        public static List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> GetYoutubeComments(string VideoId, Helper.Cache _redisCache, Helper.AppSettings settings)
        {

            List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstyoutubefeed = new List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>();
            MongoRepository mongorepo = new MongoRepository("YoutubeVideosComments", settings);
            var builder = Builders<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>.Sort;
            var sort = builder.Descending(t => t.publishTime);
            var result = mongorepo.Find<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => t.videoId.Equals(VideoId) && t.active != false);
            var task = Task.Run(async () =>
            {
                return await result;
            });
            IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtComments = task.Result;
            try
            {
                var lstYtComments_sorted = lstYtComments.OrderByDescending(t => Convert.ToDateTime(t.publishTime));
                foreach(var item in lstYtComments_sorted)
                {
                    var time = Convert.ToDateTime(item.publishTime).AddMinutes(330);
                    item.publishTime = time.ToString();
                }
                return lstYtComments_sorted.ToList();
            }
            catch
            {
                return lstYtComments.ToList();
            }

        }


        public static void PostCommentsYt(string channelId, string videoId, string commentText, Helper.AppSettings settings, ILogger _logger, Model.DatabaseRepository dbr)
        {
            commentText = commentText.Replace("\n","\\n");
            MongoRepository youtubefeedsrepo = new MongoRepository("YoutubeVideosComments", settings);
            Video _Videos = new Video(settings.GoogleConsumerKey, settings.GoogleConsumerSecret, settings.GoogleRedirectUri);

            List<Domain.Socioboard.Models.YoutubeChannel> lstYtChannel = dbr.Find<Domain.Socioboard.Models.YoutubeChannel>(t => t.YtubeChannelId.Equals(channelId)).ToList();

            try
            {

                string videos = _Videos.Post_Comments_toVideo(lstYtChannel.First().RefreshToken, videoId, commentText);
                JObject JVideodata = JObject.Parse(videos);
                MongoYoutubeComments _YoutubeComments = new MongoYoutubeComments();

                string cmntvideoId = JVideodata["snippet"]["videoId"].ToString();
                string commentId = JVideodata["id"].ToString();
                string authorDisplayName = JVideodata["snippet"]["topLevelComment"]["snippet"]["authorDisplayName"].ToString();
                string authorProfileImageUrl = JVideodata["snippet"]["topLevelComment"]["snippet"]["authorProfileImageUrl"].ToString();
                authorProfileImageUrl = authorProfileImageUrl.Replace(".jpg", "");
                string authorChannelUrl = JVideodata["snippet"]["topLevelComment"]["snippet"]["authorChannelUrl"].ToString();
                string authorChannelId = JVideodata["snippet"]["topLevelComment"]["snippet"]["authorChannelId"]["value"].ToString();
                string textDisplay = JVideodata["snippet"]["topLevelComment"]["snippet"]["textDisplay"].ToString();
                string textOriginal = JVideodata["snippet"]["topLevelComment"]["snippet"]["textOriginal"].ToString();
                string viewerRating = JVideodata["snippet"]["topLevelComment"]["snippet"]["viewerRating"].ToString();
                string likeCount = JVideodata["snippet"]["topLevelComment"]["snippet"]["likeCount"].ToString();
                string publishedAt = JVideodata["snippet"]["topLevelComment"]["snippet"]["publishedAt"].ToString();
                string updatedAt = JVideodata["snippet"]["topLevelComment"]["snippet"]["updatedAt"].ToString();
                string totalReplyCount = JVideodata["snippet"]["totalReplyCount"].ToString();

                //Add in Mongo via mongo_model
                
                _YoutubeComments.Id = ObjectId.GenerateNewId();
                _YoutubeComments.videoId = cmntvideoId;
                _YoutubeComments.commentId = commentId;
                _YoutubeComments.authorDisplayName = authorDisplayName;
                _YoutubeComments.authorProfileImageUrl = authorProfileImageUrl;
                _YoutubeComments.authorChannelUrl = authorChannelUrl;
                _YoutubeComments.authorChannelId = authorChannelId;
                _YoutubeComments.commentDisplay = textDisplay;
                _YoutubeComments.commentOriginal = textOriginal;
                _YoutubeComments.viewerRating = viewerRating;
                _YoutubeComments.likesCount = likeCount;
                _YoutubeComments.publishTime = publishedAt;
                _YoutubeComments.updatedTime = updatedAt;
                _YoutubeComments.totalReplyCount = totalReplyCount;
                _YoutubeComments.ChannelId = channelId;
                _YoutubeComments.active = true;
                _YoutubeComments.review = false;
                _YoutubeComments.sbGrpTaskAssign = false;
                _YoutubeComments.publishTimeUnix = DateExtension.ConvertToUnixTimestamp(Convert.ToDateTime(publishedAt));

                youtubefeedsrepo.Add(_YoutubeComments);
            }
                

            
            catch (Exception ex)
            {

            }


        }


        public static List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> GetAllYoutubeComments(string ChannelId, Helper.Cache _redisCache, Helper.AppSettings settings)
        {
            List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstyoutubeParentComment = new List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>();
            List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstyoutubeChildComment = new List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>();
            Thread ParentThread = new Thread(() =>
            {
                lstyoutubeParentComment = GetParentCommentYt(ChannelId, settings);
            }); ParentThread.Start();

            Thread ChildThread = new Thread(() =>
            {
                lstyoutubeChildComment = GetChildCommentYt(ChannelId, settings);
            }); ChildThread.Start();

            ParentThread.Join();
            ChildThread.Join();
            List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> newList = new List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>();
            if (lstyoutubeParentComment.Count==0 && lstyoutubeChildComment.Count==0)
            {
                return null;
            }
            else
            {
                newList = lstyoutubeParentComment.Concat(lstyoutubeChildComment).ToList();
            }
            try
            {
                var lstYtComments_sorted = newList.OrderByDescending(t => Convert.ToDateTime(t.publishTime));
                foreach (var item in lstYtComments_sorted)
                {
                    var time = Convert.ToDateTime(item.publishTime).AddMinutes(330);
                    item.publishTime = time.ToString();
                }
                return lstYtComments_sorted.ToList();
            }
            catch
            {
                return newList.ToList();
            }
        }


        public static List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> GetParentCommentYt(string channelId, Helper.AppSettings settings)
        {

            MongoRepository mongorepo = new MongoRepository("YoutubeVideosComments", settings);
            var builder = Builders<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>.Sort;
            var sort = builder.Descending(t => t.publishTimeUnix);
            //var result = mongorepo.FindWithRange<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => t.ChannelId.Equals(channelId) && t.ChannelId != t.authorChannelId && t.active != false, sort, 0, 50);
            var result = mongorepo.FindWithRange<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => t.ChannelId.Equals(channelId) && t.active != false && t.authorChannelId != channelId, sort, 0, 50);
            var task = Task.Run(async () =>
            {
                return await result;
            });
            try
            {
                IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstyoutubeParentComment = task.Result;
                return lstyoutubeParentComment.ToList();
            }
            catch(Exception ex)
            {
                return null;
            }
        }
        public static List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> GetChildCommentYt(string channelId, Helper.AppSettings settings)
        {
            MongoRepository mongorepo = new MongoRepository("YoutubeVideosCommentsReply", settings);
            var builder = Builders<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>.Sort;
            var sort = builder.Descending(t => t.publishTimeUnix);
            var result = mongorepo.FindWithRange<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => t.ChannelId.Equals(channelId) && t.active != false && t.authorChannelId != channelId, sort, 0, 50);
            var task = Task.Run(async () =>
            {
                return await result;
            });
            try
            {
                IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstyoutubeParentComment = task.Result;
                return lstyoutubeParentComment.ToList();
            }
            catch
            {
                return null;
            }
        }
        #endregion

        public static List<Domain.Socioboard.Models.Mongo.MongoYoutubeCommentsWtRepl> GetYoutubeCommentsWithReply(string VideoId, Helper.Cache _redisCache, Helper.AppSettings settings)
        {

            List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstyoutubeComments = new List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>();
            List<Domain.Socioboard.Models.Mongo.MongoYoutubeCommentsWtRepl> lstyoutubeCommentsWithReply = new List<Domain.Socioboard.Models.Mongo.MongoYoutubeCommentsWtRepl>();
            MongoRepository mongorepo = new MongoRepository("YoutubeVideosComments", settings);
            var builder = Builders<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>.Sort;
            var sort = builder.Descending(t => t.publishTime);
            var result = mongorepo.Find<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => t.videoId.Equals(VideoId) && t.active != false);
            var task = Task.Run(async () =>
            {
                return await result;
            });
            IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtComments = task.Result;
            IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtFeeds_sorted;

            try
            {
                lstYtFeeds_sorted = lstYtComments.OrderByDescending(t => Convert.ToDateTime(t.publishTime)).ToList();
                foreach (var item in lstYtFeeds_sorted)
                {
                    var time = Convert.ToDateTime(item.publishTime).AddMinutes(330);
                    item.publishTime = time.ToString();
                }
            }
            catch
            {
                lstYtFeeds_sorted = lstYtComments;
            }
            List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtFeeds_sorted_List = lstYtFeeds_sorted.ToList();
            List<string> channelIds = new List<string>();
            foreach (var x in lstYtFeeds_sorted_List)
            {
                channelIds.Add(x.videoId);
            }
            MongoRepository mongorepocmnt = new MongoRepository("YoutubeVideosCommentsReply", settings);
            var result_cmnt = mongorepocmnt.Find<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => channelIds.Contains(t.videoId) && t.active != false);
            var task_cmnt = Task.Run(async () =>
            {
                return await result_cmnt;
            });
            IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtCommentsChild = task_cmnt.Result;
            List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> tempData = lstYtCommentsChild.ToList();
            foreach (var item_time in tempData)
            {
                var time = Convert.ToDateTime(item_time.publishTime).AddMinutes(330);
                item_time.publishTime = time.ToString();
            }
            foreach (var item in lstYtFeeds_sorted)
            {
                Domain.Socioboard.Models.Mongo.MongoYoutubeCommentsWtRepl _intafeed = new Domain.Socioboard.Models.Mongo.MongoYoutubeCommentsWtRepl();
                List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstYtPostCommentTemp = tempData.Where(t => t.parentIdforReply == item.commentId).ToList();
                lstYtPostCommentTemp = lstYtPostCommentTemp.OrderByDescending(t => Convert.ToDateTime(t.publishTime)).ToList();
                _intafeed._ParentComments = item;
                _intafeed._ChildComments = lstYtPostCommentTemp.ToList();
                lstyoutubeCommentsWithReply.Add(_intafeed);
            }

            return lstyoutubeCommentsWithReply;


        }

        public static void PostCommentsYtReply(string channelId, string idParentComment, string commentText, string videoId, Helper.AppSettings settings, ILogger _logger, Model.DatabaseRepository dbr)
        {
            commentText = commentText.Replace("\n", "\\n");
            MongoRepository youtubefeedsrepo = new MongoRepository("YoutubeVideosCommentsReply", settings);
            Video _Videos = new Video(settings.GoogleConsumerKey, settings.GoogleConsumerSecret, settings.GoogleRedirectUri);

            Domain.Socioboard.Models.YoutubeChannel lstYtChannel = dbr.Single<Domain.Socioboard.Models.YoutubeChannel>(t => t.YtubeChannelId.Equals(channelId));

            try
            {

                string commentReply = _Videos.replyToComment(lstYtChannel.RefreshToken, idParentComment, commentText);
                JObject JcommentReply = JObject.Parse(commentReply);
                MongoYoutubeComments _ObjMongoYtCommentsReply = new MongoYoutubeComments();

                _ObjMongoYtCommentsReply.Id = ObjectId.GenerateNewId();
                try
                {
                    _ObjMongoYtCommentsReply.ChannelId = channelId;
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.videoId = videoId;
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.commentId = JcommentReply["id"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.authorDisplayName = JcommentReply["snippet"]["authorDisplayName"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.authorProfileImageUrl = JcommentReply["snippet"]["authorProfileImageUrl"].ToString().Replace(".jpg", "");
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.authorChannelUrl = JcommentReply["snippet"]["authorChannelUrl"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.authorChannelId = JcommentReply["snippet"]["authorChannelId"]["value"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.commentDisplay = JcommentReply["snippet"]["textDisplay"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.commentOriginal = JcommentReply["snippet"]["textOriginal"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.viewerRating = JcommentReply["snippet"]["viewerRating"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.likesCount = JcommentReply["snippet"]["likeCount"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.publishTime = JcommentReply["snippet"]["publishedAt"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.publishTimeUnix = DateExtension.ConvertToUnixTimestamp(Convert.ToDateTime(_ObjMongoYtCommentsReply.publishTime));
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.updatedTime = JcommentReply["snippet"]["updatedAt"].ToString();
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.totalReplyCount = "ReplyType";
                }
                catch { }
                try
                {
                    _ObjMongoYtCommentsReply.parentIdforReply = JcommentReply["snippet"]["parentId"].ToString();
                }
                catch { }
                _ObjMongoYtCommentsReply.active = true;
                _ObjMongoYtCommentsReply.review = false;
                _ObjMongoYtCommentsReply.sbGrpTaskAssign = false;

                youtubefeedsrepo.Add(_ObjMongoYtCommentsReply);
            }
            catch (Exception ex)
            {

            }
        }

        public static void ReviewedComment(string commentId, string sbUserName, Int64 sbUserId, bool status, string commentType, Helper.AppSettings settings, ILogger _logger, Model.DatabaseRepository dbr)
        {
            MongoRepository mongorepo;
            if (commentType == "main")
            {
                mongorepo = new MongoRepository("YoutubeVideosComments", settings);
            }
            else
            {
                mongorepo = new MongoRepository("YoutubeVideosCommentsReply", settings);
            }
            try
            {
                if (status == true)
                {
                    FilterDefinition<BsonDocument> filter = new BsonDocument("commentId", commentId);
                    var update = Builders<BsonDocument>.Update.Set("review", status).Set("reviewedBy", sbUserName).Set("reviewedBysbUserId", sbUserId).Set("sbGrpTaskAssign", false);
                    mongorepo.Update<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(update, filter);
                }
                else
                {
                    FilterDefinition<BsonDocument> filter = new BsonDocument("commentId", commentId);
                    var update = Builders<BsonDocument>.Update.Set("review", status).Set("reviewedBy", sbUserName).Set("reviewedBysbUserId", sbUserId).Set("sbGrpTaskAssign", status);
                    mongorepo.Update<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(update, filter);
                }
            }
            catch { }
        }
    }
}
