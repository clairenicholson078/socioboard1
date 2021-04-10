﻿using Domain.Socioboard.Models.Mongo;
using Facebook;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Socioboard.Google.Custom;
using Socioboard.Twitter.App.Core;
using Socioboard.Twitter.Authentication;
using Socioboard.Twitter.Twitter.Core.TweetMethods;
using SociobordRssDataServices.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Socioboard.Facebook.Data;

namespace SociobordRssDataServices.Rss
{
    public class RssFeed
    {
        public static int apiHitsCount = 0;
        public static int MaxapiHitsCount = 20;
        public static int twtapiHitsCount = 0;
        public static int twtMaxapiHitsCount = 50;

        public static void updateRssFeeds(Domain.Socioboard.Models.Mongo.Rss _rss)
        {
            ParseFeedUrl(_rss.RssFeedUrl, _rss.ProfileType, _rss.ProfileId, _rss.UserId, _rss.ProfileName, _rss.ProfileImageUrl);

        }

        public static string postRssFeeds(Domain.Socioboard.Models.Mongo.Rss _rss)
        {
            MongoRepository mongorepo = new MongoRepository("RssFeed");
            DatabaseRepository dbr = new DatabaseRepository();
            List<Domain.Socioboard.Models.Mongo.RssFeed> objrssdata = new List<Domain.Socioboard.Models.Mongo.RssFeed>();
            var ret = mongorepo.Find<Domain.Socioboard.Models.Mongo.RssFeed>(t => t.ProfileId == _rss.ProfileId && t.Status == false);
            var task = Task.Run(async () =>
            {
                return await ret;
            });
            IList<Domain.Socioboard.Models.Mongo.RssFeed> _objrssdata = task.Result;
            objrssdata = _objrssdata.ToList();
            foreach (var item in objrssdata)
            {
                if (_objrssdata.First().ProfileType == Domain.Socioboard.Enum.SocialProfileType.Facebook || _objrssdata.First().ProfileType == Domain.Socioboard.Enum.SocialProfileType.FacebookFanPage)
                {
                    try
                    {
                        if (item.Status == false)
                        {
                            Domain.Socioboard.Models.Facebookaccounts lstFbAcc = dbr.Single<Domain.Socioboard.Models.Facebookaccounts>(t => t.FbUserId.Equals(item.ProfileId));
                            // Domain.Socioboard.Models.Facebookaccounts _Facebookaccounts = Repositories.FacebookRepository.getFacebookAccount(item.ProfileId, _redisCache, dbr);
                            string msg = FacebookComposeMessageRss(item.Title, lstFbAcc.AccessToken, lstFbAcc.FbUserId, item.Title, item.Link, item.strId);

                            var builders = Builders<BsonDocument>.Filter;
                            FilterDefinition<BsonDocument> filter = builders.Eq("strId", item.strId);
                            var update = Builders<BsonDocument>.Update.Set("Status", true);
                            mongorepo.Update<Domain.Socioboard.Models.Mongo.RssFeed>(update, filter);
                            var resu = mongorepo.Find<Domain.Socioboard.Models.Mongo.RssFeed>(t => t.strId == item.strId);
                            var tasks = Task.Run(async () =>
                            {
                                return await resu;
                            });
                            IList<Domain.Socioboard.Models.Mongo.RssFeed> _rssdata = tasks.Result;
                            Console.WriteLine("rss Data");
                            Console.WriteLine(_rssdata);

                        }

                    }
                    catch (Exception ex)
                    {
                        return "";
                    }
                    Thread.Sleep(20*1000);
                }
                if (_objrssdata.First().ProfileType == Domain.Socioboard.Enum.SocialProfileType.Twitter)
                {
                    try

                    {
                        if (item.Status == false)
                        {
                            Domain.Socioboard.Models.TwitterAccount lstTwtAcc = dbr.Single<Domain.Socioboard.Models.TwitterAccount>(t => t.twitterUserId.Equals(item.ProfileId));
                            string msg = TwitterComposeMessageRss(item.Link, lstTwtAcc.oAuthToken, lstTwtAcc.oAuthSecret, lstTwtAcc.twitterUserId, lstTwtAcc.twitterScreenName, item.strId);

                            var builders = Builders<BsonDocument>.Filter;
                            FilterDefinition<BsonDocument> filter = builders.Eq("strId", item.strId);
                            var update = Builders<BsonDocument>.Update.Set("Status", true);
                            mongorepo.Update<Domain.Socioboard.Models.Mongo.RssFeed>(update, filter);
                            var resu = mongorepo.Find<Domain.Socioboard.Models.Mongo.RssFeed>(t => t.strId == item.strId);
                            var tasks = Task.Run(async () =>
                            {
                                return await resu;
                            });
                            IList<Domain.Socioboard.Models.Mongo.RssFeed> _rssdata = tasks.Result;
                            Console.WriteLine("rss Data");
                            Console.WriteLine(_rssdata);
                        }                
                    }
                    catch (Exception ex)
                    {
                        return "";
                    }
                    Thread.Sleep(20 * 000);
                }
            }
            return "";
              //  string facebookdata = FacebookComposeMessageRss(objRssFeeds.Message, _Facebookaccounts.AccessToken, _Facebookaccounts.FbUserId, objRssFeeds.Title, objRssFeeds.Link, objRssFeeds.strId);

        }

        public static string ParseFeedUrl(string TextUrl, Domain.Socioboard.Enum.SocialProfileType profiletype, string profileid, long userId, string profileName, string profileImageUrl)
        {

            MongoRepository _RssFeedRepository = new MongoRepository("RssFeed");
            try
            {

                XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
                xmlDoc.Load(TextUrl);
                var abc = xmlDoc.DocumentElement.GetElementsByTagName("item");
                if (profiletype == Domain.Socioboard.Enum.SocialProfileType.Facebook || profiletype == Domain.Socioboard.Enum.SocialProfileType.FacebookFanPage)
                {
                    Model.DatabaseRepository dbr = new DatabaseRepository();
                    Domain.Socioboard.Models.Facebookaccounts _Facebookaccounts = dbr.Find<Domain.Socioboard.Models.Facebookaccounts>(t => t.FbUserId == profileid).First();
                    //if (_Facebookaccounts.SchedulerUpdate.AddHours(1) <= DateTime.UtcNow)
                    //{
                    if (_Facebookaccounts != null)
                    {
                        if (_Facebookaccounts.IsActive)
                        {

                            foreach (XmlElement item in abc)
                            {
                                int count = 0;
                                Domain.Socioboard.Models.Mongo.RssFeed objRssFeeds = new Domain.Socioboard.Models.Mongo.RssFeed();
                                try
                                {
                                    objRssFeeds.Id = ObjectId.GenerateNewId();
                                    objRssFeeds.strId = ObjectId.GenerateNewId().ToString();
                                    objRssFeeds.ProfileName = profileName;
                                    objRssFeeds.ProfileImageUrl = profileImageUrl;

                                    try
                                    {
                                        objRssFeeds.Message = item.ChildNodes[9].InnerText;
                                        objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                        objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);

                                    }
                                    catch (Exception ex)
                                    {
                                        try
                                        {
                                            if (item.ChildNodes[2].InnerText.Contains("www") && item.ChildNodes[2].InnerText.Contains("http"))
                                            {
                                                objRssFeeds.Message = item.ChildNodes[1].InnerText;
                                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                                            }
                                            else
                                            {
                                                objRssFeeds.Message = item.ChildNodes[2].InnerText;
                                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                                            }
                                        }
                                        catch
                                        {
                                            objRssFeeds.Message = item.ChildNodes[1].InnerText;
                                            objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                            objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                                        }
                                    }
                                    if(string.IsNullOrEmpty(objRssFeeds.Message))
                                    {
                                        objRssFeeds.Message = item.ChildNodes[7].InnerText;
                                    }

                                    try
                                    {
                                        objRssFeeds.PublishingDate = DateTime.Parse(item.ChildNodes[4].InnerText).ToString("yyyy/MM/dd HH:mm:ss");
                                    }
                                    catch (Exception ex)
                                    {
                                        objRssFeeds.PublishingDate = DateTime.Parse(item.ChildNodes[3].InnerText).ToString("yyyy/MM/dd HH:mm:ss");
                                    }

                                    if (item.ChildNodes[0].Name== "guid")
                                    {
                                        objRssFeeds.Title = item.ChildNodes[1].InnerText; 
                                    }
                                    else
                                    {
                                        objRssFeeds.Title = item.ChildNodes[0].InnerText;
                                    }

                                    if (item.ChildNodes[1].InnerText.Contains("www") || item.ChildNodes[1].InnerText.Contains("http"))
                                    {
                                        try
                                        {
                                            objRssFeeds.Image = item.ChildNodes[1].InnerText;
                                            objRssFeeds.Image = getBetween(objRssFeeds.Image, "src=\"", "\"");
                                            objRssFeeds.Link = item.ChildNodes[1].InnerText;
                                            objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");
                                        }
                                        catch (Exception ex)
                                        {
                                            objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                            objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");
                                        }
                                    }
                                    else
                                    {
                                        objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                        objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");
                                        if(string.IsNullOrEmpty(objRssFeeds.Link))
                                        {
                                            objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                        }
                                    }
                                    objRssFeeds.RssFeedUrl = TextUrl;
                                    objRssFeeds.ProfileId = profileid;
                                    objRssFeeds.ProfileType = profiletype;
                                    objRssFeeds.Status = false;
                                    var ret = _RssFeedRepository.Find<Domain.Socioboard.Models.Mongo.RssFeed>(t => t.Link.Equals(objRssFeeds.Link) && t.ProfileId.Equals(profileid) && t.ProfileType.Equals(profiletype) && t.Status==true);
                                    var task = Task.Run(async () =>
                                    {
                                        return await ret;
                                    });
                                    //int count = task.Result.Count; change by sweta on 06/04/2018
                                    count = task.Result.Count;
                                    if (count < 1)
                                    {
                                        _RssFeedRepository.Add<Domain.Socioboard.Models.Mongo.RssFeed>(objRssFeeds);
                                    }

                                }
                                catch (Exception ex)
                                {

                                }
                                if (apiHitsCount < MaxapiHitsCount && count<1)
                                {
                                    string facebookdata = FacebookComposeMessageRss(objRssFeeds.Message, _Facebookaccounts.AccessToken, _Facebookaccounts.FbUserId, objRssFeeds.Title, objRssFeeds.Link, objRssFeeds.strId);
                                    if (!string.IsNullOrEmpty(facebookdata))
                                    {
                                        apiHitsCount++;

                                    }
                                }
                                else
                                {
                                    apiHitsCount = 0;
                                    return "ok";
                                }
                            }
                            _Facebookaccounts.SchedulerUpdate = DateTime.UtcNow;
                            dbr.Update<Domain.Socioboard.Models.Facebookaccounts>(_Facebookaccounts);

                        }
                    }
                    //}
                    //else
                    //{
                    //    apiHitsCount = 0;
                    //}
                }
                if (profiletype == Domain.Socioboard.Enum.SocialProfileType.Twitter)
                {
                    Model.DatabaseRepository dbr = new DatabaseRepository();
                    Domain.Socioboard.Models.TwitterAccount _TwitterAccount = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => t.twitterUserId == profileid).First();
                    //if (_TwitterAccount.SchedulerUpdate.AddMinutes(15) <= DateTime.UtcNow)
                    //{
                    if (_TwitterAccount != null)
                    {
                        if (_TwitterAccount.isActive)
                        {

                            foreach (XmlElement item in abc)
                            {
                                Domain.Socioboard.Models.Mongo.RssFeed objRssFeeds = new Domain.Socioboard.Models.Mongo.RssFeed();
                                try
                                {
                                    objRssFeeds.Id = ObjectId.GenerateNewId();
                                    objRssFeeds.strId = ObjectId.GenerateNewId().ToString();
                                    objRssFeeds.ProfileName = profileName;
                                    objRssFeeds.ProfileImageUrl = profileImageUrl;

                                    try
                                    {
                                        objRssFeeds.Message = item.ChildNodes[9].InnerText;
                                        objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                        objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);

                                    }
                                    catch (Exception ex)
                                    {
                                        try
                                        {
                                            if (item.ChildNodes[2].InnerText.Contains("www") && item.ChildNodes[2].InnerText.Contains("http"))
                                            {
                                                objRssFeeds.Message = item.ChildNodes[1].InnerText;
                                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                                            }
                                            else
                                            {
                                                objRssFeeds.Message = item.ChildNodes[2].InnerText;
                                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                                            }
                                        }
                                        catch
                                        {
                                            objRssFeeds.Message = item.ChildNodes[1].InnerText;
                                            objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                            objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                                        }
                                    }

                                    if (string.IsNullOrEmpty(objRssFeeds.Message))
                                    {
                                        objRssFeeds.Message = item.ChildNodes[7].InnerText;
                                    }
                                    try
                                    {
                                        objRssFeeds.PublishingDate = DateTime.Parse(item.ChildNodes[4].InnerText).ToString("yyyy/MM/dd HH:mm:ss");
                                    }
                                    catch (Exception ex)
                                    {
                                        objRssFeeds.PublishingDate = DateTime.Parse(item.ChildNodes[3].InnerText).ToString("yyyy/MM/dd HH:mm:ss");
                                    }

                                    objRssFeeds.Title = item.ChildNodes[0].InnerText;

                                    if (item.ChildNodes[1].InnerText.Contains("www") || item.ChildNodes[1].InnerText.Contains("http"))
                                    {
                                        try
                                        {
                                            objRssFeeds.Image = item.ChildNodes[1].InnerText;
                                            objRssFeeds.Image = getBetween(objRssFeeds.Image, "src=\"", "\"");
                                            objRssFeeds.Link = item.ChildNodes[1].InnerText;
                                            objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");
                                            if (string.IsNullOrEmpty(objRssFeeds.Link))
                                            {
                                                objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                            objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");
                                            if (string.IsNullOrEmpty(objRssFeeds.Link))
                                            {
                                                objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                        objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");
                                        if (string.IsNullOrEmpty(objRssFeeds.Link))
                                        {
                                            objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                        }
                                    }
                                    objRssFeeds.RssFeedUrl = TextUrl;
                                    objRssFeeds.ProfileId = profileid;
                                    objRssFeeds.ProfileType = profiletype;
                                    objRssFeeds.Status = false;
                                    var ret = _RssFeedRepository.Find<Domain.Socioboard.Models.Mongo.RssFeed>(t => t.Link.Equals(objRssFeeds.Link) && t.ProfileId.Equals(profileid) && t.ProfileType.Equals(profiletype));
                                    var task = Task.Run(async () =>
                                    {
                                        return await ret;
                                    });
                                    int count = task.Result.Count;
                                    if (count < 1)
                                    {
                                        _RssFeedRepository.Add<Domain.Socioboard.Models.Mongo.RssFeed>(objRssFeeds);
                                    }

                                }
                                catch (Exception ex)
                                {

                                }
                                if (twtapiHitsCount < twtMaxapiHitsCount)
                                {

                                    string twitterdata = TwitterComposeMessageRss(objRssFeeds.Message, _TwitterAccount.oAuthToken, _TwitterAccount.oAuthSecret, _TwitterAccount.twitterUserId, _TwitterAccount.twitterScreenName, objRssFeeds.strId);
                                    if (!string.IsNullOrEmpty(twitterdata))
                                    {
                                        twtapiHitsCount++;

                                    }
                                }
                                else
                                {
                                    twtapiHitsCount = 0;
                                    return "ok";
                                }

                            }
                            _TwitterAccount.SchedulerUpdate = DateTime.UtcNow;
                            dbr.Update<Domain.Socioboard.Models.TwitterAccount>(_TwitterAccount);
                        }
                    }
                    //}
                    //else
                    //{
                    //    twtapiHitsCount = 0;
                    //}
                }


                return "ok";
            }
            catch (Exception ex)
            {

                return "invalid url";
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

        public static string FacebookComposeMessageRss(string message, string accessToken, string FbUserId, string title, string link, string rssFeedId)
        {
            string ret = "";
            FacebookClient fb = new FacebookClient();
            MongoRepository rssfeedRepo = new MongoRepository("RssFeed");
            try
            {
                var pageAccessToken = FacebookApiHelper.GetPageAccessToken(FbUserId, accessToken, string.Empty);
                if (string.IsNullOrEmpty(pageAccessToken))              
                    return string.Empty;
                FacebookApiHelper.PublishPostOnPage(pageAccessToken, FbUserId, message, string.Empty, link);

                #region Old Methods 
                //fb.AccessToken = accessToken;
                //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
                //var args = new Dictionary<string, object>();
                //if (message != null)
                //{
                //    args["message"] = message;
                //}

                //args["link"] = link;
                //ret = fb.Post("v2.7/" + FbUserId + "/feed", args).ToString(); 
                #endregion

                var builders = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filter = builders.Eq("strId", rssFeedId);
                var update = Builders<BsonDocument>.Update.Set("Status", true);
                rssfeedRepo.Update<Domain.Socioboard.Models.Mongo.RssFeed>(update, filter);
                Thread.Sleep(1000*600);
                return ret = "Messages Posted Successfully";
            }
            catch (Exception ex)
            {
                if(ex.Message.Contains("Error validating access token: Session has expired on"))
                {
                    Model.DatabaseRepository dbr = new DatabaseRepository();
                    Domain.Socioboard.Models.Facebookaccounts _Facebookaccounts = dbr.Find<Domain.Socioboard.Models.Facebookaccounts>(t => t.FbUserId == FbUserId).First();
                    _Facebookaccounts.IsActive = false;
                    dbr.Update<Domain.Socioboard.Models.Facebookaccounts>(_Facebookaccounts);
                }
                Console.WriteLine(ex.Message);
                apiHitsCount = MaxapiHitsCount;
                return ret = "";
            }
        }

        public static string TwitterComposeMessageRss(string message, string OAuthToken, string OAuthSecret, string profileid, string TwitterScreenName, string rssFeedId)
        {
            string ret = "";
            oAuthTwitter OAuthTwt = new oAuthTwitter("h4FT0oJ46KBBMwbcifqZMw", "yfowGI2g21E2mQHjtHjUvGqkfbI7x26WDCvjiSZOjas", "https://www.socioboard.com/TwitterManager/Twitter");
            // oAuthTwitter OAuthTwt = new oAuthTwitter("MbOQl85ZcvRGvp3kkOOJBlbFS", "GF0UIXnTAX28hFhN1ISNf3tURHARZdKWlZrsY4PlHm9A4llYjZ", "http://serv1.socioboard.com/TwitterManager/Twitter");
            OAuthTwt.AccessToken = OAuthToken;
            OAuthTwt.AccessTokenSecret = OAuthSecret;
            OAuthTwt.TwitterScreenName = TwitterScreenName;
            OAuthTwt.TwitterUserId = profileid;
            Tweet twt = new Tweet();
            if (message.Length>139)
            {
                message = message.Substring(0, 139); 
            }
            MongoRepository rssfeedRepo = new MongoRepository("RssFeed");
            try
            {
                JArray post = twt.Post_Statuses_Update(OAuthTwt, message);

                var builders = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filter = builders.Eq("strId", rssFeedId);
                var update = Builders<BsonDocument>.Update.Set("Status", true);
                rssfeedRepo.Update<Domain.Socioboard.Models.Mongo.RssFeed>(update, filter);
                Thread.Sleep(1000 * 60 * 10);
                return ret = "Messages Posted Successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                twtapiHitsCount = twtMaxapiHitsCount;
                return ret = "";
            }
        }

        public static void updateRssContentFeeds(Domain.Socioboard.Models.Mongo.RssNewsContents _rssNews, string keywords)
        {
            ParseContentFeedUrl(_rssNews.RssFeedUrl, _rssNews.ProfileId, keywords);
            addContentfeedsdata(keywords);
            addGplusContentfeedsdata(keywords,_rssNews.ProfileId);
        }

        public static void addContentfeedsdata(string keyword)
        {
            MongoRepository mongorepo = new MongoRepository("RssNewsContentsFeeds");
            string timeline = TwitterHashTag.TwitterBoardHashTagSearch(keyword, null);
            int i = 0;
            if (!string.IsNullOrEmpty(timeline) && !timeline.Equals("[]"))
            {
                foreach (JObject obj in JArray.Parse(timeline))
                {
                    RssNewsContentsFeeds contentFeedsDet = new RssNewsContentsFeeds();
                    contentFeedsDet.Id = ObjectId.GenerateNewId();
                    i++;
                    try
                    {
                        contentFeedsDet.Link = JArray.Parse(obj["entities"]["expanded_url"].ToString())[0]["url"].ToString();
                    }
                    catch
                    {
                        try
                        {
                            contentFeedsDet.Link = JArray.Parse(obj["entities"]["urls"].ToString())[0]["expanded_url"].ToString();
                        }
                        catch (Exception e)
                        {

                        }
                    }
                    try
                    {
                        contentFeedsDet.Image = JArray.Parse(obj["extended_entities"]["media"].ToString())[0]["media_url"].ToString();
                    }
                    catch
                    {
                        try
                        {
                            contentFeedsDet.Image = JArray.Parse(obj["entities"]["media"].ToString())[0]["media_url"].ToString();
                        }
                        catch (Exception e)
                        {

                        }
                    }

                    try
                    {
                        contentFeedsDet.Title = obj["text"].ToString();
                    }
                    catch (Exception e)
                    {

                    }

                    try
                    {
                        string Const_TwitterDateTemplate = "ddd MMM dd HH:mm:ss +ffff yyyy";
                        contentFeedsDet.PublishingDate = Domain.Socioboard.Helpers.SBHelper.ConvertToUnixTimestamp(DateTime.ParseExact((string)obj["created_at"], Const_TwitterDateTemplate, new System.Globalization.CultureInfo("en-US"))).ToString();
                    }
                    catch (Exception e)
                    {

                    }
                    contentFeedsDet.keywords = keyword;

                    var ret = mongorepo.Find<RssNewsContentsFeeds>(t=>t.Image == contentFeedsDet.Image);
                    var task = Task.Run(async () =>
                    {
                        return await ret;
                    });
                    int count = task.Result.Count;
                    if (count < 1)
                    {
                        try
                        {
                            mongorepo.Add<RssNewsContentsFeeds>(contentFeedsDet);
                        }
                        catch (Exception e) { }

                    }
                    else
                    {
                    }
                }
            }
        }

        public static bool addGplusContentfeedsdata(string keywords, string userId)
        {
            MongoRepository mongorepo = new MongoRepository("RssNewsContentsFeeds");
            bool output = false;
            try
            {
                string searchResultObj = GplusTagSearch.GooglePlusgetUserRecentActivitiesByHashtag(keywords);
                JObject GplusActivities = JObject.Parse(GplusTagSearch.GooglePlusgetUserRecentActivitiesByHashtag(keywords));

                foreach (JObject obj in JArray.Parse(GplusActivities["items"].ToString()))
                {
                    RssNewsContentsFeeds contentGFeedsDet = new RssNewsContentsFeeds();
                    contentGFeedsDet.Id = ObjectId.GenerateNewId();

                    try
                    {
                        foreach (JObject att in JArray.Parse(obj["object"]["attachments"].ToString()))
                        {
                            contentGFeedsDet.Image = att["fullImage"]["url"].ToString();

                            contentGFeedsDet.Link = att["url"].ToString();

                            contentGFeedsDet.Title = att["displayName"].ToString();
                        }
                    }
                    catch { }
                    try
                    {
                        contentGFeedsDet.PublishingDate = Domain.Socioboard.Helpers.SBHelper.ConvertToUnixTimestamp(DateTime.Parse(obj["published"].ToString())).ToString();
                    }
                    catch { }


                    contentGFeedsDet.UserId = userId;
                    contentGFeedsDet.keywords = keywords;
                    var ret = mongorepo.Find<RssNewsContentsFeeds>(t => t.Title == contentGFeedsDet.Title);
                    var task = Task.Run(async () =>
                    {
                        return await ret;
                    });
                    int count = task.Result.Count;
                    if (count < 1)
                    {
                        try
                        {
                            mongorepo.Add<RssNewsContentsFeeds>(contentGFeedsDet);
                            output = true;
                        }
                        catch { }
                    }


                }
                return output;
            }
            catch { }

            return output;

        }


        public static string ParseContentFeedUrl(string RssFeedUrl, string ProfileId, string keywords)
        {
            MongoRepository _RssFeedRepository = new MongoRepository("RssNewsContentsFeeds");
            try
            {
                XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
                xmlDoc.Load(RssFeedUrl);
                var abc = xmlDoc.DocumentElement.GetElementsByTagName("item");

                foreach (XmlElement item in abc)
                {
                    Domain.Socioboard.Models.Mongo.RssNewsContentsFeeds objRssFeeds = new Domain.Socioboard.Models.Mongo.RssNewsContentsFeeds();
                    try
                    {
                        objRssFeeds.Id = ObjectId.GenerateNewId();
                        // objRssFeeds.strId = ObjectId.GenerateNewId().ToString();
                        try
                        {
                            objRssFeeds.Message = item.ChildNodes[9].InnerText;
                            objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                            objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);

                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                if (item.ChildNodes[2].InnerText.Contains("www") && item.ChildNodes[2].InnerText.Contains("http"))
                                {
                                    objRssFeeds.Message = item.ChildNodes[1].InnerText;
                                    objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                    objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                                }
                                else
                                {
                                    objRssFeeds.Message = item.ChildNodes[2].InnerText;
                                    objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                    objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                                }
                            }
                            catch
                            {
                                objRssFeeds.Message = item.ChildNodes[1].InnerText;
                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "<.*?>", string.Empty).Replace("[&#8230;]", "");
                                objRssFeeds.Message = Regex.Replace(objRssFeeds.Message, "@<[^>]+>|&nbsp;", string.Empty);
                            }
                        }


                        try
                        {
                            objRssFeeds.PublishingDate = DateTime.Parse(item.ChildNodes[4].InnerText).ToString("yyyy/MM/dd HH:mm:ss");
                        }
                        catch (Exception ex)
                        {
                            objRssFeeds.PublishingDate = DateTime.Parse(item.ChildNodes[3].InnerText).ToString("yyyy/MM/dd HH:mm:ss");
                        }

                        objRssFeeds.Title = item.ChildNodes[0].InnerText;

                        if (item.ChildNodes[1].InnerText.Contains("www") || item.ChildNodes[1].InnerText.Contains("http"))
                        {
                            try
                            {
                                objRssFeeds.Image = item.ChildNodes[1].InnerText;
                                objRssFeeds.Image = getBetween(objRssFeeds.Image, "src=\"", "\"");
                                objRssFeeds.Link = item.ChildNodes[1].InnerText;
                                objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");


                            }
                            catch (Exception ex)
                            {
                                objRssFeeds.Link = item.ChildNodes[2].InnerText;
                                objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");
                            }
                        }
                        else
                        {
                            objRssFeeds.Link = item.ChildNodes[2].InnerText;


                            //  objRssFeeds.Link = getBetween(objRssFeeds.Link, "<a href=\"", "\">");
                        }
                        if (item.BaseURI.Contains("http://feeds.bbci.co.uk") || item.BaseURI.Contains("http://www.hindustantimes.com"))
                        {
                            objRssFeeds.Image = item.ChildNodes[5].OuterXml;
                            objRssFeeds.Image = getBetween(objRssFeeds.Image, "url=\"", "\"");//media:content url="
                        }



                        objRssFeeds.RssFeedUrl = RssFeedUrl;
                        objRssFeeds.UserId = ProfileId;
                        objRssFeeds.keywords = keywords;


                        var ret = _RssFeedRepository.Find<Domain.Socioboard.Models.Mongo.RssNewsContentsFeeds>(t => t.Link == objRssFeeds.Link && t.UserId == ProfileId);
                        var task = Task.Run(async () =>
                        {
                            return await ret;
                        });
                        int count = task.Result.Count;
                        if (count < 1)
                        {
                            _RssFeedRepository.Add(objRssFeeds);
                        }

                    }
                    catch (Exception ex)
                    {

                    }

                }
                return "ok";
            }
            catch (Exception ex)
            {
                return "Invalid Url";
            }
        }

    }
}
