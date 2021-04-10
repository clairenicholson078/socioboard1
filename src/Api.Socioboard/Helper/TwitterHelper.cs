﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Socioboard.Models;
using Api.Socioboard.Model;
using Microsoft.Extensions.Logging;
using Socioboard.Twitter.Twitter.Core.TweetMethods;
using Socioboard.Twitter.App.Core;
using Newtonsoft.Json.Linq;
using Socioboard.Twitter.Authentication;
using Socioboard.Twitter.Twitter.Core.SearchMethods;
using System.IO;
using System.Net;
using System.IO.Compression;
using MongoDB.Driver;
using MongoDB.Bson;
using Socioboard.Twitter.Twitter.Core.UserMethods;
using Socioboard.Twitter.Twitter.Core.FollowersMethods;
using Socioboard.Twitter.Twitter.Core.TimeLineMethods;
using Socioboard.Twitter.Twitter.Core.DirectMessageMethods;
using Socioboard.Twitter.Twitter.Core;
using Socioboard.Twitter.TwitterUtilites;

namespace Api.Socioboard.Helper
{
    public static class TwitterHelper
    {

        public static string PostTwitterMessage(AppSettings _AppSettings, Cache _redisCache, string message, string profileid, long userid, string url, bool isScheduled, Domain.Socioboard.Enum.MediaType mediaType, string profileName, DatabaseRepository dbr, ILogger _logger, string sscheduledmsgguid = "")
        {
            bool rt = false;
            string ret = "";
            string str = "";
            int Twtsc = 0;
            Domain.Socioboard.Models.TwitterAccount objTwitterAccount = Api.Socioboard.Repositories.TwitterRepository.getTwitterAccount(profileid, _redisCache, dbr);
            oAuthTwitter OAuthTwt = new oAuthTwitter(_AppSettings.twitterConsumerKey, _AppSettings.twitterConsumerScreatKey, _AppSettings.twitterRedirectionUrl);
            OAuthTwt.AccessToken = objTwitterAccount.oAuthToken;
            OAuthTwt.AccessTokenSecret = objTwitterAccount.oAuthSecret;
            OAuthTwt.TwitterScreenName = objTwitterAccount.twitterScreenName;
            OAuthTwt.TwitterUserId = objTwitterAccount.twitterUserId;
            Tweet twt = new Tweet();
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    PhotoUpload ph = new PhotoUpload();
                    string res = string.Empty;
                    // rt = ph.Tweet(url, message, OAuthTwt);
                    if (url.Contains("mp4"))
                    {
                        var webClient = new WebClient();
                        byte[] img = webClient.DownloadData(url);
                        rt = videoUploading(img, message, objTwitterAccount.oAuthToken, objTwitterAccount.oAuthSecret, _AppSettings);
                    }
                    else
                    {
                        rt = ph.NewTweet(url, message, OAuthTwt, ref res);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("PostTwitterMessageWithImage" + ex.StackTrace);
                    _logger.LogError("PostTwitterMessageWithImage" + ex.Message);
                }
            }
            else
            {
                try
                {
                    JArray post = twt.Post_Statuses_Update(OAuthTwt, message);
                    ret = post[0]["id_str"].ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogError("PostTwitterMessage" + ex.StackTrace);
                    _logger.LogError("PostTwitterMessage" + ex.Message);
                }
            }

            if (!string.IsNullOrEmpty(ret) || rt == true)
            {

                ScheduledMessage scheduledMessage = new ScheduledMessage();
                scheduledMessage.createTime = DateTime.UtcNow;
                scheduledMessage.picUrl = objTwitterAccount.profileImageUrl;
                scheduledMessage.profileId = profileid;
                scheduledMessage.profileType = Domain.Socioboard.Enum.SocialProfileType.Twitter;
                scheduledMessage.scheduleTime = DateTime.UtcNow;
                scheduledMessage.shareMessage = message;
                scheduledMessage.socialprofileName = objTwitterAccount.twitterScreenName;
                scheduledMessage.userId = userid;
                scheduledMessage.status = Domain.Socioboard.Enum.ScheduleStatus.Compleated;
                scheduledMessage.url = url;
                scheduledMessage.mediaType = mediaType;
                //scheduledMessage.socialprofileName = profileName;
                dbr.Add<ScheduledMessage>(scheduledMessage);


            }
            else
            {
                str = "Message not posted";
            }

            return str;
        }

        public static bool videoUploading(byte[] binary, string tweetmessage, string accesstoken, string tokensecret, Helper.AppSettings _appsetting)
        {
            try
            {
                Tweetinvi.Auth.SetUserCredentials(_appsetting.twitterConsumerKey, _appsetting.twitterConsumerScreatKey, accesstoken, tokensecret);
                string mediaType = "video/mp4";
                var uploader = Tweetinvi.Upload.CreateChunkedUploader();
                var half = (binary.Length / 2);
                var first = binary.Take(half).ToArray();
                var second = binary.Skip(half).ToArray();
                if (uploader.Init(mediaType, binary.Length))
                {
                    if (uploader.Append(first, "media"))
                    {
                        if (uploader.Append(second, "media"))
                        {
                            var media = uploader.Complete();
                            var tweet = Tweetinvi.Tweet.PublishTweet(tweetmessage, new Tweetinvi.Parameters.PublishTweetOptionalParameters
                            {
                                Medias = { media }
                            });
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static List<Domain.Socioboard.ViewModels.DiscoveryViewModal> DiscoverySearchTwitter(oAuthTwitter oauth, string keyword, long userId, long groupId)
        {

            List<Domain.Socioboard.ViewModels.DiscoveryViewModal> lstDiscoverySearch = new List<Domain.Socioboard.ViewModels.DiscoveryViewModal>();
            Search search = new Search();
            JArray twitterSearchResult = search.Get_Search_Tweets(oauth, keyword);
            foreach (var item in twitterSearchResult)
            {
                var results = item["statuses"];
                foreach (var chile in results)
                {


                    try
                    {
                        Domain.Socioboard.ViewModels.DiscoveryViewModal objDiscoverySearch = new Domain.Socioboard.ViewModels.DiscoveryViewModal();
                        objDiscoverySearch.CreatedTime = Utility.ParseTwitterTime(chile["created_at"].ToString().TrimStart('"').TrimEnd('"')); ;
                        objDiscoverySearch.EntryDate = DateTime.Now;
                        objDiscoverySearch.FromId = chile["user"]["id_str"].ToString().TrimStart('"').TrimEnd('"');
                        objDiscoverySearch.FromName = chile["user"]["screen_name"].ToString().TrimStart('"').TrimEnd('"');
                        objDiscoverySearch.ProfileImageUrl = chile["user"]["profile_image_url"].ToString().TrimStart('"').TrimEnd('"');
                        objDiscoverySearch.SearchKeyword = keyword;
                        objDiscoverySearch.Network = "twitter";
                        objDiscoverySearch.Message = chile["text"].ToString().TrimStart('"').TrimEnd('"').Replace("&amp", " ");
                        objDiscoverySearch.MessageId = chile["id_str"].ToString().TrimStart('"').TrimEnd('"');
                        objDiscoverySearch.UserId = userId;
                        lstDiscoverySearch.Add(objDiscoverySearch);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
            return lstDiscoverySearch;
        }

        public static List<DiscoverySmart> TwitterTweetSearchWithGeoLocation(string q, string geoCode)
        {
            if (q.Contains("#"))
            {
                q = Uri.EscapeUriString(q);
            }
            try
            {
                string url = string.Empty;
                if (!string.IsNullOrEmpty(geoCode))
                {
                    url = q.Trim() + "&geocode=" + geoCode + "&count=20&result_type=recent";
                }
                else
                {
                    url = q.Trim() + "&count=20&result_type=recent";
                }
                string ret = string.Empty;
                JArray output = new JArray();
                SortedDictionary<string, string> requestParameters = new SortedDictionary<string, string>();

                var oauth_url = "https://api.twitter.com/1.1/search/tweets.json?q=" + url;
                var headerFormat = "Bearer {0}";
                var authHeader = string.Format(headerFormat, "AAAAAAAAAAAAAAAAAAAAAOZyVwAAAAAAgI0VcykgJ600le2YdR4uhKgjaMs%3D0MYOt4LpwCTAIi46HYWa85ZcJ81qi0D9sh8avr1Zwf7BDzgdHT");

                var postBody = requestParameters.ToWebString();
                ServicePointManager.Expect100Continue = false;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(oauth_url + "?"
                       + requestParameters.ToWebString());

                request.Headers.Add("Authorization", authHeader);
                request.Method = "GET";
                request.Headers.Add("Accept-Encoding", "gzip");
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                Stream responseStream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
                using (var reader = new StreamReader(responseStream))
                {
                    var objText = reader.ReadToEnd();
                    output = JArray.Parse(JObject.Parse(objText)["statuses"].ToString());
                }
                Helper.DiscoverySmart _Discovery;
                List<Helper.DiscoverySmart> lstDiscovery = new List<Helper.DiscoverySmart>();
                try
                {
                    foreach (var item in output)
                    {
                        try
                        {
                            _Discovery = new Helper.DiscoverySmart();

                            _Discovery.text = item["text"].ToString();
                            _Discovery.created_at = Utility.ParseTwitterTime(item["created_at"].ToString());
                            _Discovery.tweet_id = item["id_str"].ToString();
                            _Discovery.twitter_id = item["user"]["id_str"].ToString();
                            _Discovery.profile_image_url = item["user"]["profile_image_url"].ToString();
                            _Discovery.screan_name = item["user"]["screen_name"].ToString();
                            _Discovery.name = item["user"]["name"].ToString();
                            _Discovery.description = item["user"]["description"].ToString();
                            _Discovery.followers_count = item["user"]["followers_count"].ToString();
                            _Discovery.friends_count = item["user"]["friends_count"].ToString();

                            lstDiscovery.Add(_Discovery);
                        }
                        catch { }
                    }
                }
                catch { }
                return lstDiscovery;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static string TwitterComposeMessageRss(string message, string OAuthToken, string OAuthSecret, string profileid, string TwitterScreenName, string rssFeedId, Helper.AppSettings _appSettings)
        {
            string ret = "";
            oAuthTwitter OAuthTwt = new oAuthTwitter();
            OAuthTwt.AccessToken = OAuthToken;
            OAuthTwt.AccessTokenSecret = OAuthSecret;
            OAuthTwt.TwitterScreenName = TwitterScreenName;
            OAuthTwt.TwitterUserId = profileid;
            Tweet twt = new Tweet();
            MongoRepository rssfeedRepo = new MongoRepository("RssFeed", _appSettings);
            try
            {
                JArray post = twt.Post_Statuses_Update(OAuthTwt, message);

                var builders = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filter = builders.Eq("strId", rssFeedId);
                var update = Builders<BsonDocument>.Update.Set("Status", true);
                rssfeedRepo.Update<Domain.Socioboard.Models.Mongo.RssFeed>(update, filter);

                return ret = "Messages Posted Successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ret = "Message Could Not Posted";
            }
        }


        public static List<Domain.Socioboard.Models.TwitterRecentFollower> TwitterRecentFollower(long groupId, Model.DatabaseRepository dbr, Helper.AppSettings _appSettings)
        {
            string[] profileids = null;
            List<Domain.Socioboard.Models.TwitterRecentFollower> lstTwitterRecentFollower = new List<Domain.Socioboard.Models.TwitterRecentFollower>();
            List<Domain.Socioboard.Models.Groupprofiles> lstGroupprofiles = dbr.Find<Domain.Socioboard.Models.Groupprofiles>(t => t.groupId == groupId && t.profileType == Domain.Socioboard.Enum.SocialProfileType.Twitter).ToList();
            profileids = lstGroupprofiles.Select(t => t.profileId).ToArray();
            List<Domain.Socioboard.Models.TwitterAccount> lstAccRepo = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => profileids.Contains(t.twitterUserId) && t.isActive).ToList();
            oAuthTwitter oauth = null;
            Users twtUsers = new Users();
            foreach (Domain.Socioboard.Models.TwitterAccount itemTwt in lstAccRepo)
            {
                oauth = new oAuthTwitter();
                oauth.AccessToken = itemTwt.oAuthToken;
                oauth.AccessTokenSecret = itemTwt.oAuthSecret;
                oauth.TwitterScreenName = itemTwt.twitterScreenName;
                oauth.TwitterUserId = itemTwt.twitterUserId;
                oauth.ConsumerKey = _appSettings.twitterConsumerKey;
                oauth.ConsumerKeySecret = _appSettings.twitterConsumerScreatKey;
                JArray jarresponse = twtUsers.Get_Followers_ById(oauth, itemTwt.twitterUserId);
                foreach (var item in jarresponse)
                {
                    int resposecount = 0;
                    if (item["ids"] != null)
                    {
                        foreach (var child in item["ids"])
                        {
                            if (resposecount < 2)
                            {
                                JArray userprofile = twtUsers.Get_Users_LookUp(oauth, child.ToString());
                                foreach (var items in userprofile)
                                {
                                    Domain.Socioboard.Models.TwitterRecentFollower objTwitterRecentFollower = new Domain.Socioboard.Models.TwitterRecentFollower();
                                    resposecount++;
                                    objTwitterRecentFollower.screen_name = items["screen_name"].ToString();
                                    objTwitterRecentFollower.name = items["name"].ToString();
                                    objTwitterRecentFollower.profile_image_url = items["profile_image_url"].ToString();
                                    lstTwitterRecentFollower.Add(objTwitterRecentFollower);
                                }
                            }
                        }
                    }
                }
            }
            return lstTwitterRecentFollower;
        }

        public static void PostTwitterDirectmessage(string toId, string message, string profileId, long UserId, Model.DatabaseRepository dbr, 
            Helper.AppSettings _appSettings, Helper.Cache _redisCache,string recipientScreenName, string recipientImageUrl, string senderScreenName, string senderImageUrl)
        {
            Domain.Socioboard.Models.Mongo.MongoDirectMessages objDirectMessages = new Domain.Socioboard.Models.Mongo.MongoDirectMessages();
            // Domain.Socioboard.Models.TwitterAccount objTwitterAccount = Repositories.TwitterRepository.getTwitterAccount(profileId, _redisCache,dbr);
            Domain.Socioboard.Models.TwitterAccount objTwitterAccount = new TwitterAccount();
            objTwitterAccount = dbr.Single<TwitterAccount>(t => t.userId == UserId && t.twitterUserId.Contains(profileId));
            if (objTwitterAccount == null)
            {
                objTwitterAccount = dbr.Single<TwitterAccount>(t => t.userId == UserId && t.twitterUserId.Contains(toId));
                toId = profileId;
            }
            oAuthTwitter OAuthTwt = new oAuthTwitter(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, _appSettings.twitterRedirectionUrl);
            OAuthTwt.AccessToken = objTwitterAccount.oAuthToken;
            OAuthTwt.AccessTokenSecret = objTwitterAccount.oAuthSecret;
            OAuthTwt.TwitterScreenName = objTwitterAccount.twitterScreenName;
            OAuthTwt.TwitterUserId = objTwitterAccount.twitterUserId;
            const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";
            TwitterUser twtuser = new TwitterUser();
            JArray ret = new JArray();
            try
            {
                var objTwitterDirectMessages = new TwitterApiHelper(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, OAuthTwt.AccessToken, OAuthTwt.AccessTokenSecret);
                var task = new Task(async () =>
                {
                    var response = await objTwitterDirectMessages.DirectMessage(message, toId);

                    if (!string.IsNullOrEmpty(response))
                    {

                        try
                        {
                            var jobject = JObject.Parse(response);

                            objDirectMessages.messageId = jobject.SelectToken("event.id")?.ToString() ?? string.Empty;
                            objDirectMessages.message = jobject.SelectToken("event.message_create.message_data.text")?.ToString() ?? string.Empty;
                            objDirectMessages.profileId = objTwitterAccount.twitterUserId;

                            var timestamp = jobject.SelectToken("event.created_timestamp")?.ToString() ?? string.Empty;
                            objDirectMessages.timeStamp = Math.Floor(double.Parse(timestamp) / 1000); 

                            try
                            {
                                var dateTime =
                                    Domain.Socioboard.Helpers.SBHelper
                                        .ConvertUnixTimeStamp(timestamp);

                                objDirectMessages.createdDate = dateTime.ToString("yyyy/MM/dd HH:mm:ss"); 
                            }
                            catch (Exception ex)
                            {
                                objDirectMessages.createdDate = DateTime.Now.ToString();
                            }

                            objDirectMessages.entryDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                            objDirectMessages.recipientId = jobject.SelectToken("event.message_create.target.recipient_id")?.ToString() ?? string.Empty;
                            objDirectMessages.senderId = jobject.SelectToken("event.message_create.sender_id")?.ToString() ?? string.Empty;


                            /* Previous Response
                             *  objDirectMessages.messageId = ret[0]["id_str"].ToString();
                            objDirectMessages.message = ret[0]["text"].ToString();
                            objDirectMessages.profileId = objTwitterAccount.twitterUserId;
                            objDirectMessages.createdDate = DateTime.ParseExact(ret[0]["created_at"].ToString().TrimStart('"').TrimEnd('"'), format, System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy/MM/dd HH:mm:ss");
                            objDirectMessages.timeStamp = Domain.Socioboard.Helpers.SBHelper.ConvertToUnixTimestamp(DateTime.ParseExact(ret[0]["created_at"].ToString().TrimStart('"').TrimEnd('"'), format, System.Globalization.CultureInfo.InvariantCulture));
                            objDirectMessages.entryDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                            objDirectMessages.recipientId = ret[0]["recipient"]["id_str"].ToString();
                            objDirectMessages.recipientProfileUrl = ret[0]["recipient"]["profile_image_url_https"].ToString();
                            objDirectMessages.recipientScreenName = ret[0]["recipient"]["screen_name"].ToString();
                            objDirectMessages.senderId = ret[0]["sender"]["id_str"].ToString();
                            objDirectMessages.senderProfileUrl = ret[0]["sender"]["profile_image_url_https"].ToString();
                            objDirectMessages.senderScreenName = ret[0]["sender"]["screen_name"].ToString();
                             */

                            objDirectMessages.recipientProfileUrl = recipientImageUrl;
                            objDirectMessages.recipientScreenName = recipientScreenName;

                            objDirectMessages.senderProfileUrl = senderImageUrl;
                            objDirectMessages.senderScreenName = senderScreenName;

                            objDirectMessages.type = Domain.Socioboard.Enum.MessageType.TwitterDirectMessageSent;
                            MongoRepository mongorepo = new MongoRepository("MongoDirectMessages", _appSettings);
                            mongorepo.Add(objDirectMessages);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                });
                task.Start();              
            }
            catch (Exception ex)
            {

            }
        }

        public static void PostTwitterDirectmessage(string toId, string message, string profileId, long UserId, Model.DatabaseRepository dbr, Helper.AppSettings _appSettings, Helper.Cache _redisCache)
        {
            Domain.Socioboard.Models.Mongo.MongoDirectMessages objDirectMessages = new Domain.Socioboard.Models.Mongo.MongoDirectMessages();
            // Domain.Socioboard.Models.TwitterAccount objTwitterAccount = Repositories.TwitterRepository.getTwitterAccount(profileId, _redisCache,dbr);
            Domain.Socioboard.Models.TwitterAccount objTwitterAccount = new TwitterAccount();
            objTwitterAccount = dbr.Single<TwitterAccount>(t => t.userId == UserId && t.twitterUserId.Contains(profileId));
            if (objTwitterAccount == null)
            {
                objTwitterAccount = dbr.Single<TwitterAccount>(t => t.userId == UserId && t.twitterUserId.Contains(toId));
                toId = profileId;
            }
            oAuthTwitter OAuthTwt = new oAuthTwitter(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, _appSettings.twitterRedirectionUrl);
            OAuthTwt.AccessToken = objTwitterAccount.oAuthToken;
            OAuthTwt.AccessTokenSecret = objTwitterAccount.oAuthSecret;
            OAuthTwt.TwitterScreenName = objTwitterAccount.twitterScreenName;
            OAuthTwt.TwitterUserId = objTwitterAccount.twitterUserId;
            const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";
            TwitterUser twtuser = new TwitterUser();
            JArray ret = new JArray();
            try
            {
                var objTwitterDirectMessages = new TwitterApiHelper(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, OAuthTwt.AccessToken, OAuthTwt.AccessTokenSecret);
                var task = new Task(async () =>
                {
                    var response = await objTwitterDirectMessages.DirectMessage(message, toId);

                    if (!string.IsNullOrEmpty(response))
                    {

                        try
                        {
                            var jobject = JObject.Parse(response);

                            objDirectMessages.messageId = jobject.SelectToken("event.id")?.ToString() ?? string.Empty;
                            objDirectMessages.message = jobject.SelectToken("event.message_create.message_data.text")?.ToString() ?? string.Empty;
                            objDirectMessages.profileId = objTwitterAccount.twitterUserId;

                            var timestamp = jobject.SelectToken("event.created_timestamp")?.ToString() ?? string.Empty;
                            objDirectMessages.timeStamp = double.Parse(timestamp);

                            try
                            {
                                objDirectMessages.createdDate = Domain.Socioboard.Helpers.SBHelper.ConvertFromUnixTimestamp(double.Parse(timestamp)).ToString();
                            }
                            catch (Exception)
                            {
                                objDirectMessages.createdDate = DateTime.Now.ToString();
                            }

                            objDirectMessages.entryDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                            objDirectMessages.recipientId = jobject.SelectToken("event.message_create.target.recipient_id")?.ToString() ?? string.Empty;
                            objDirectMessages.senderId = jobject.SelectToken("event.message_create.sender_id")?.ToString() ?? string.Empty;


                            /* Previous Response
                             *  objDirectMessages.messageId = ret[0]["id_str"].ToString();
                            objDirectMessages.message = ret[0]["text"].ToString();
                            objDirectMessages.profileId = objTwitterAccount.twitterUserId;
                            objDirectMessages.createdDate = DateTime.ParseExact(ret[0]["created_at"].ToString().TrimStart('"').TrimEnd('"'), format, System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy/MM/dd HH:mm:ss");
                            objDirectMessages.timeStamp = Domain.Socioboard.Helpers.SBHelper.ConvertToUnixTimestamp(DateTime.ParseExact(ret[0]["created_at"].ToString().TrimStart('"').TrimEnd('"'), format, System.Globalization.CultureInfo.InvariantCulture));
                            objDirectMessages.entryDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                            objDirectMessages.recipientId = ret[0]["recipient"]["id_str"].ToString();
                            objDirectMessages.recipientProfileUrl = ret[0]["recipient"]["profile_image_url_https"].ToString();
                            objDirectMessages.recipientScreenName = ret[0]["recipient"]["screen_name"].ToString();
                            objDirectMessages.senderId = ret[0]["sender"]["id_str"].ToString();
                            objDirectMessages.senderProfileUrl = ret[0]["sender"]["profile_image_url_https"].ToString();
                            objDirectMessages.senderScreenName = ret[0]["sender"]["screen_name"].ToString();
                             */

                            objDirectMessages.recipientProfileUrl = string.Empty;
                            objDirectMessages.recipientScreenName = string.Empty;

                            objDirectMessages.senderProfileUrl = string.Empty;
                            objDirectMessages.senderScreenName = string.Empty;

                            objDirectMessages.type = Domain.Socioboard.Enum.MessageType.TwitterDirectMessageSent;
                            MongoRepository mongorepo = new MongoRepository("MongoDirectMessages", _appSettings);
                            mongorepo.Add(objDirectMessages);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                });
                task.Start();
            }
            catch (Exception ex)
            {

            }
        }



        public static bool FollowAccount(oAuthTwitter OAuth, string Screen_name, string user_id)
        {
            bool IsFollowed = false;

            string RequestUrl = "https://api.twitter.com/1.1/friendships/create.json";
            SortedDictionary<string, string> strdic = new SortedDictionary<string, string>();
            if (!string.IsNullOrEmpty(Screen_name))
            {
                strdic.Add("screen_name", Screen_name);
            }
            else if (!string.IsNullOrEmpty(user_id))
            {
                strdic.Add("user_id", user_id);
            }
            else
            {
                return false;
            }
            strdic.Add("follow", "true");
            string response = OAuth.OAuthWebRequest(oAuthTwitter.Method.POST, RequestUrl, strdic);
            if (!string.IsNullOrEmpty(response))
            {
                IsFollowed = true;
            }
            return IsFollowed;
        }


        public static List<Domain.Socioboard.Models.TwitterMutualFans> twittermutual(long groupId, Model.DatabaseRepository dbr, Helper.AppSettings _appSettings)
        {
            string[] profileids = null;
            List<Domain.Socioboard.Models.TwitterMutualFans> lstMutualFan = new List<Domain.Socioboard.Models.TwitterMutualFans>();
            List<Domain.Socioboard.Models.Groupprofiles> lstGroupprofiles = dbr.Find<Domain.Socioboard.Models.Groupprofiles>(t => t.groupId == groupId && t.profileType == Domain.Socioboard.Enum.SocialProfileType.Twitter).ToList();
            profileids = lstGroupprofiles.Select(t => t.profileId).ToArray();
            List<Domain.Socioboard.Models.TwitterAccount> lstAccRepo = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => profileids.Contains(t.twitterUserId) && t.isActive).ToList();
            oAuthTwitter oaut = null;
            Users twtUser = new Users();
            foreach (Domain.Socioboard.Models.TwitterAccount itemTwt in lstAccRepo)
            {

                oaut = new oAuthTwitter();
                oaut.AccessToken = itemTwt.oAuthToken;
                oaut.AccessTokenSecret = itemTwt.oAuthSecret;
                oaut.TwitterScreenName = itemTwt.twitterScreenName;
                oaut.TwitterUserId = itemTwt.twitterUserId;
                oaut.ConsumerKey = _appSettings.twitterConsumerKey;
                oaut.ConsumerKeySecret = _appSettings.twitterConsumerScreatKey;
                JArray jarresponse = twtUser.Get_Followers_ById(oaut, itemTwt.twitterUserId);
                JArray user_data = JArray.Parse(jarresponse[0]["ids"].ToString());

                JArray jarrespons = twtUser.Get_Friends_ById(oaut, itemTwt.twitterUserId);
                JArray user_data_2 = JArray.Parse(jarrespons[0]["ids"].ToString());
                foreach (var items in user_data.Intersect(user_data_2))
                {
                    string userid = items.ToString();
                    JArray userprofile = twtUser.Get_Users_LookUp(oaut, userid);
                    foreach (var item in userprofile)
                    {
                        Domain.Socioboard.Models.TwitterMutualFans objTwitterRecent = new Domain.Socioboard.Models.TwitterMutualFans();
                        objTwitterRecent.screen_name = item["screen_name"].ToString();
                        objTwitterRecent.name = item["name"].ToString();
                        lstMutualFan.Add(objTwitterRecent);
                    }

                }
            }
            return lstMutualFan;
        }


        public static List<Domain.Socioboard.Models.TwitterMutualFans> twitterfans(long groupId, Model.DatabaseRepository dbr, Helper.AppSettings _appSettings)
        {
            string[] profileids = null;
            List<Domain.Socioboard.Models.TwitterMutualFans> lstTwitterUserfans = new List<Domain.Socioboard.Models.TwitterMutualFans>();
            List<Domain.Socioboard.Models.Groupprofiles> lstGroupprofiles = dbr.Find<Domain.Socioboard.Models.Groupprofiles>(t => t.groupId == groupId && t.profileType == Domain.Socioboard.Enum.SocialProfileType.Twitter).ToList();
            profileids = lstGroupprofiles.Select(t => t.profileId).ToArray();
            List<Domain.Socioboard.Models.TwitterAccount> lstAccRepo = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => profileids.Contains(t.twitterUserId) && t.isActive).ToList();
            oAuthTwitter oaut = null;
            Users twtUser = new Users();
            List<string> mutualfanlist = new List<string>();
            List<string> followersId = new List<string>();
            foreach (Domain.Socioboard.Models.TwitterAccount itemTwt in lstAccRepo)
            {
                oaut = new oAuthTwitter();
                oaut.AccessToken = itemTwt.oAuthToken;
                oaut.AccessTokenSecret = itemTwt.oAuthSecret;
                oaut.TwitterScreenName = itemTwt.twitterScreenName;
                oaut.TwitterUserId = itemTwt.twitterUserId;
                oaut.ConsumerKey = _appSettings.twitterConsumerKey;
                oaut.ConsumerKeySecret = _appSettings.twitterConsumerScreatKey;
                JArray jarresponse = twtUser.Get_Followers_ById(oaut, itemTwt.twitterUserId);
                JArray user_data = JArray.Parse(jarresponse[0]["ids"].ToString());
                JArray jarrespons = twtUser.Get_Friends_ById(oaut, itemTwt.twitterUserId);
                JArray user_data_2 = JArray.Parse(jarrespons[0]["ids"].ToString());
                foreach (var items in user_data.Intersect(user_data_2))
                {
                    mutualfanlist.Add(items.ToString());
                }
                foreach (var itemss in user_data)
                {
                    followersId.Add(itemss.ToString());
                }
                List<string> fansId = followersId.Except(mutualfanlist).ToList();
                foreach (var items in fansId)
                {
                    Domain.Socioboard.Models.TwitterMutualFans objTwitterFans = new Domain.Socioboard.Models.TwitterMutualFans();
                    JArray userprofile = twtUser.Get_Users_LookUp(oaut, items);
                    foreach (var item in userprofile)
                    {
                        objTwitterFans.screen_name = item["screen_name"].ToString();
                        objTwitterFans.name = item["name"].ToString();
                        lstTwitterUserfans.Add(objTwitterFans);
                    }
                }

            }
            return lstTwitterUserfans;
        }


        public static IEnumerable<Domain.Socioboard.Models.TwitterMutualFans> twitterfollowerslist(string profileId, Model.DatabaseRepository dbr, Helper.AppSettings _appSettings)
        {
            var profileids = profileId;
            var lstfollowerlist = new List<Domain.Socioboard.Models.TwitterMutualFans>();
            //List<Domain.Socioboard.Models.Groupprofiles> lstGroupprofiles = dbr.Find<Domain.Socioboard.Models.Groupprofiles>(t => t.groupId == groupId && t.profileType == Domain.Socioboard.Enum.SocialProfileType.Twitter).ToList();
            //profileids = lstGroupprofiles.Select(t => t.profileId).ToArray();
            var lstAccRepo = dbr.Find<TwitterAccount>(t => profileids.Contains(t.twitterUserId) && t.isActive).ToList();
            oAuthTwitter oaut = null;
            var twtUser = new Users();
            foreach (var itemTwt in lstAccRepo)
            {

                oaut = new oAuthTwitter
                {
                    AccessToken = itemTwt.oAuthToken,
                    AccessTokenSecret = itemTwt.oAuthSecret,
                    TwitterScreenName = itemTwt.twitterScreenName,
                    TwitterUserId = itemTwt.twitterUserId,
                    ConsumerKey = _appSettings.twitterConsumerKey,
                    ConsumerKeySecret = _appSettings.twitterConsumerScreatKey
                };
                var jarresponse = twtUser.Get_Followers_ById(oaut, itemTwt.twitterUserId);
                var userData = JArray.Parse(jarresponse[0]["ids"].ToString());
                foreach (var items in userData)
                {
                    var userid = items.ToString();
                    var userprofile = twtUser.Get_Users_LookUp(oaut, userid);
                    foreach (var item in userprofile)
                    {
                        var objTwitterFollowers = new TwitterMutualFans
                        {
                            screen_name = item["screen_name"].ToString(),
                            name = item["name"].ToString(),
                            description = item["description"].ToString(),
                            followers = item["followers_count"].ToString(),
                            following = item["friends_count"].ToString(),
                            location = item["location"].ToString(),
                            profile_image_url = item["profile_image_url"].ToString()
                        };
                        lstfollowerlist.Add(objTwitterFollowers);
                        yield return objTwitterFollowers;
                    }

                }
            }

        }
        public static string TwitterBlockUsers(string profileId, string toTwitterUserId, Model.DatabaseRepository dbr, ILogger _logger, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.TwitterAccount twtacc = new Domain.Socioboard.Models.TwitterAccount();
            Domain.Socioboard.Models.TwitterAccount imtwtacc = _redisCache.Get<Domain.Socioboard.Models.TwitterAccount>(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId);
            if (imtwtacc == null)
            {
                twtacc = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => t.twitterUserId.Equals(profileId)).FirstOrDefault();
                if (twtacc != null)
                {
                    _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId, twtacc);
                }
            }
            else
            {
                twtacc = imtwtacc;
            }
            oAuthTwitter oAuth = new oAuthTwitter(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, _appSettings.twitterRedirectionUrl);
            oAuth.AccessToken = twtacc.oAuthToken;
            oAuth.AccessTokenSecret = twtacc.oAuthSecret;
            oAuth.TwitterScreenName = twtacc.twitterScreenName;
            oAuth.TwitterUserId = twtacc.twitterUserId;
            Blocks blkss = new Blocks();
            try
            {
                string blockUserResp = blkss.BlocksUserByUserId(oAuth, toTwitterUserId);
                if (blockUserResp != "")
                {
                    return "User blocked successfully";
                }
                else
                {
                    return "Issue in user blocking";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("user blocking" + ex.StackTrace);
                _logger.LogError("user blocking" + ex.Message);
                return "api issue while user blocking";
            }
        }


        public static string TwitterUnBlockUsers(string profileId, string toTwitterUserId, Model.DatabaseRepository dbr, ILogger _logger, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.TwitterAccount twtacc = new Domain.Socioboard.Models.TwitterAccount();
            Domain.Socioboard.Models.TwitterAccount imtwtacc = _redisCache.Get<Domain.Socioboard.Models.TwitterAccount>(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId);
            if (imtwtacc == null)
            {
                twtacc = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => t.twitterUserId.Equals(profileId)).FirstOrDefault();
                if (twtacc != null)
                {
                    _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId, twtacc);
                }
            }
            else
            {
                twtacc = imtwtacc;
            }
            oAuthTwitter oAuth = new oAuthTwitter(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, _appSettings.twitterRedirectionUrl);
            oAuth.AccessToken = twtacc.oAuthToken;
            oAuth.AccessTokenSecret = twtacc.oAuthSecret;
            oAuth.TwitterScreenName = twtacc.twitterScreenName;
            oAuth.TwitterUserId = twtacc.twitterUserId;
            Blocks blkss = new Blocks();
            try
            {
                string unBlockUserResp = blkss.UnBlocksUserByUserId(oAuth, toTwitterUserId);
                if (unBlockUserResp != "")
                {
                    return "User unblocked successfully";
                }
                else
                {
                    return "Issue in user unblocking";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("user unblocking" + ex.StackTrace);
                _logger.LogError("user unblocking" + ex.Message);
                return "api issue while user unblocking";
            }
        }

        public static string TwitterUserFollow(string profileId, string toTwitterUserId, Model.DatabaseRepository dbr, ILogger _logger, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.TwitterAccount twtacc = new Domain.Socioboard.Models.TwitterAccount();
            Domain.Socioboard.Models.TwitterAccount imtwtacc = _redisCache.Get<Domain.Socioboard.Models.TwitterAccount>(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId);
            if (imtwtacc == null)
            {
                twtacc = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => t.twitterUserId.Equals(profileId)).FirstOrDefault();
                if (twtacc != null)
                {
                    _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId, twtacc);
                }
            }
            else
            {
                twtacc = imtwtacc;
            }
            oAuthTwitter oAuth = new oAuthTwitter(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, _appSettings.twitterRedirectionUrl);
            oAuth.AccessToken = twtacc.oAuthToken;
            oAuth.AccessTokenSecret = twtacc.oAuthSecret;
            oAuth.TwitterScreenName = twtacc.twitterScreenName;
            oAuth.TwitterUserId = twtacc.twitterUserId;
            FollowerManage follow_obj = new FollowerManage();
            try
            {
                string followUserResp = follow_obj.FollowUserByUserId(oAuth, toTwitterUserId);
                if (followUserResp != "")
                {
                    return "User followed successfully";
                }
                else
                {
                    return "Issue in user following";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("user following" + ex.StackTrace);
                _logger.LogError("user following" + ex.Message);
                return "api issue while user following";
            }
        }

        public static Domain.Socioboard.Models.TwitterFriendRelation TwitterFrindsRelation(string profileId, string toTwitterUserId, Model.DatabaseRepository dbr, ILogger _logger, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.TwitterAccount twtacc = new Domain.Socioboard.Models.TwitterAccount();
            Domain.Socioboard.Models.TwitterAccount imtwtacc = _redisCache.Get<Domain.Socioboard.Models.TwitterAccount>(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId);
            if (imtwtacc == null)
            {
                twtacc = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => t.twitterUserId.Equals(profileId)).FirstOrDefault();
                if (twtacc != null)
                {
                    _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId, twtacc);
                }
            }
            else
            {
                twtacc = imtwtacc;
            }
            oAuthTwitter oAuth = new oAuthTwitter(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, _appSettings.twitterRedirectionUrl);
            oAuth.AccessToken = twtacc.oAuthToken;
            oAuth.AccessTokenSecret = twtacc.oAuthSecret;
            oAuth.TwitterScreenName = twtacc.twitterScreenName;
            oAuth.TwitterUserId = twtacc.twitterUserId;
            FollowerManage follow_obj = new FollowerManage();
            try
            {
                Domain.Socioboard.Models.TwitterFriendRelation _objLstData = new Domain.Socioboard.Models.TwitterFriendRelation();
                string friendRelationResp = follow_obj.RelatnFriendshipByUserId(oAuth, toTwitterUserId);
                JObject jFriendRelationResp = JObject.Parse(friendRelationResp);
                if (friendRelationResp == "")
                {
                    return null;
                }
                else
                {
                    _objLstData.myId = jFriendRelationResp["relationship"]["source"]["id_str"].ToString();
                    _objLstData.myScreenName = jFriendRelationResp["relationship"]["source"]["screen_name"].ToString();
                    _objLstData.myFollowing = jFriendRelationResp["relationship"]["source"]["following"].ToString();
                    _objLstData.myFollowedBy = jFriendRelationResp["relationship"]["source"]["followed_by"].ToString();
                    _objLstData.myLiveFollowing = jFriendRelationResp["relationship"]["source"]["live_following"].ToString();
                    _objLstData.myFollowingReceived = jFriendRelationResp["relationship"]["source"]["following_received"].ToString();
                    _objLstData.myFollowingRequested = jFriendRelationResp["relationship"]["source"]["following_requested"].ToString();
                    _objLstData.myNotificationsEnabled = jFriendRelationResp["relationship"]["source"]["notifications_enabled"].ToString();
                    _objLstData.myCanDm = jFriendRelationResp["relationship"]["source"]["can_dm"].ToString();
                    _objLstData.myBlocking = jFriendRelationResp["relationship"]["source"]["blocking"].ToString();
                    _objLstData.myBlockedBy = jFriendRelationResp["relationship"]["source"]["blocked_by"].ToString();
                    _objLstData.myMuting = jFriendRelationResp["relationship"]["source"]["muting"].ToString();
                    _objLstData.myWantRetweets = jFriendRelationResp["relationship"]["source"]["want_retweets"].ToString();
                    _objLstData.myAllReplies = jFriendRelationResp["relationship"]["source"]["all_replies"].ToString();
                    _objLstData.myMarkedSpam = jFriendRelationResp["relationship"]["source"]["marked_spam"].ToString();
                    _objLstData.targetId = jFriendRelationResp["relationship"]["target"]["id_str"].ToString();
                    _objLstData.targetScreenName = jFriendRelationResp["relationship"]["target"]["screen_name"].ToString();
                    _objLstData.targetFollowing = jFriendRelationResp["relationship"]["target"]["following"].ToString();
                    _objLstData.targetFollowedBy = jFriendRelationResp["relationship"]["target"]["followed_by"].ToString();
                    _objLstData.targetFollowingReceived = jFriendRelationResp["relationship"]["target"]["following_received"].ToString();
                    _objLstData.targetFollowingRequsted = jFriendRelationResp["relationship"]["target"]["following_requested"].ToString();
                    return _objLstData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("error finding friendship relation" + ex.StackTrace);
                _logger.LogError("error finding friendship relation" + ex.Message);
                return null;
            }
        }

        public static List<Domain.Socioboard.Models.TwitterContactSearch> twitterConstactSearchlist(string profileId, string contact, Model.DatabaseRepository dbr, Helper.AppSettings _appSettings)
        {
            string profileids = profileId;
            List<Domain.Socioboard.Models.TwitterContactSearch> lstContact = new List<Domain.Socioboard.Models.TwitterContactSearch>();
            Domain.Socioboard.Models.TwitterAccount itemTwt = dbr.Single<Domain.Socioboard.Models.TwitterAccount>(t => profileids.Contains(t.twitterUserId));
            oAuthTwitter oaut = null;
            Users twtUser = new Users();
            oaut = new oAuthTwitter();
            oaut.AccessToken = itemTwt.oAuthToken;
            oaut.AccessTokenSecret = itemTwt.oAuthSecret;
            oaut.TwitterScreenName = itemTwt.twitterScreenName;
            oaut.TwitterUserId = itemTwt.twitterUserId;
            oaut.ConsumerKey = _appSettings.twitterConsumerKey;
            oaut.ConsumerKeySecret = _appSettings.twitterConsumerScreatKey;
            JArray jarresponse = twtUser.Get_Users_Search(oaut, contact, "20");
            JArray user_data = JArray.Parse(jarresponse[0]["ids"].ToString());
            foreach (var items in user_data)
            {
                string userid = items.ToString();
                JArray userprofile = twtUser.Get_Users_LookUp(oaut, userid);
                foreach (var item in userprofile)
                {
                    Domain.Socioboard.Models.TwitterContactSearch objTwitterContact = new Domain.Socioboard.Models.TwitterContactSearch();
                    objTwitterContact.screen_name = item["screen_name"].ToString();
                    objTwitterContact.name = item["name"].ToString();
                    objTwitterContact.description = item["description"].ToString();
                    objTwitterContact.followers = item["followers_count"].ToString();
                    objTwitterContact.following = item["friends_count"].ToString();
                    objTwitterContact.location = item["location"].ToString();
                    objTwitterContact.profile_image_url = item["profile_image_url"].ToString();
                    lstContact.Add(objTwitterContact);
                }

            }

            return lstContact;
        }

        public static List<Domain.Socioboard.Models.TwitterMutualFans> twitterUnfollowerslist(string profileId, Model.DatabaseRepository dbr, Helper.AppSettings _appSettings)
        {
            string profileids = profileId;
            List<Domain.Socioboard.Models.TwitterMutualFans> UnfollowBackID = new List<Domain.Socioboard.Models.TwitterMutualFans>();
            // List<Domain.Socioboard.Models.Groupprofiles> lstGroupprofiles = dbr.Find<Domain.Socioboard.Models.Groupprofiles>(t => t.groupId == groupId && t.profileType == Domain.Socioboard.Enum.SocialProfileType.Twitter).ToList();
            //profileids = lstGroupprofiles.Select(t => t.profileId).ToArray();
            List<Domain.Socioboard.Models.TwitterAccount> lstAccRepo = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => profileids.Contains(t.twitterUserId) && t.isActive).ToList();
            oAuthTwitter oaut = null;
            Users twtUser = new Users();
            foreach (Domain.Socioboard.Models.TwitterAccount itemTwt in lstAccRepo)
            {

                oaut = new oAuthTwitter();
                oaut.AccessToken = itemTwt.oAuthToken;
                oaut.AccessTokenSecret = itemTwt.oAuthSecret;
                oaut.TwitterScreenName = itemTwt.twitterScreenName;
                oaut.TwitterUserId = itemTwt.twitterUserId;
                oaut.ConsumerKey = _appSettings.twitterConsumerKey;
                oaut.ConsumerKeySecret = _appSettings.twitterConsumerScreatKey;
                JArray jarresponse = twtUser.Get_Followers_ById(oaut, itemTwt.twitterUserId);
                JArray user_data = JArray.Parse(jarresponse[0]["ids"].ToString());

                JArray jarrespons = twtUser.Get_Friends_ById(oaut, itemTwt.twitterUserId);
                JArray user_data_2 = JArray.Parse(jarrespons[0]["ids"].ToString());


                var items = user_data.Intersect(user_data_2).ToArray();
                var hgh = user_data_2.Intersect(items).ToArray();
                // Array itema = user_data.Intersect(items);
                Array UnfollowBack_id = user_data_2.Union(items).Except(hgh).ToArray();
                foreach (var ite in UnfollowBack_id)
                {
                    string userid = ite.ToString();
                    JArray userprofile = twtUser.Get_Users_LookUp(oaut, userid);
                    foreach (var item in userprofile)
                    {
                        Domain.Socioboard.Models.TwitterMutualFans objTwitterRecent = new Domain.Socioboard.Models.TwitterMutualFans();
                        objTwitterRecent.screen_name = item["screen_name"].ToString();
                        objTwitterRecent.name = item["name"].ToString();
                        objTwitterRecent.description = item["description"].ToString();
                        objTwitterRecent.followers = item["followers_count"].ToString();
                        objTwitterRecent.following = item["friends_count"].ToString();
                        objTwitterRecent.location = item["location"].ToString();
                        objTwitterRecent.profile_image_url = item["profile_image_url"].ToString();
                        UnfollowBackID.Add(objTwitterRecent);
                    }

                }
            }
            return UnfollowBackID;
        }

        public static List<Domain.Socioboard.Models.TwitterMentionSugg> TwitterMentionBased(string profileId, Model.DatabaseRepository dbr, ILogger _logger, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            List<Domain.Socioboard.Models.TwitterMentionSugg> lstMentionsUsers = new List<TwitterMentionSugg>();
            Domain.Socioboard.Models.TwitterAccount twtacc = new Domain.Socioboard.Models.TwitterAccount();
            Domain.Socioboard.Models.TwitterAccount imtwtacc = _redisCache.Get<Domain.Socioboard.Models.TwitterAccount>(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId);
            if (imtwtacc == null)
            {
                twtacc = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => t.twitterUserId.Equals(profileId)).FirstOrDefault();
                if (twtacc != null)
                {
                    _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId, twtacc);
                }
            }
            else
            {
                twtacc = imtwtacc;
            }
            oAuthTwitter oAuth = new oAuthTwitter(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, _appSettings.twitterRedirectionUrl);
            oAuth.AccessToken = twtacc.oAuthToken;
            oAuth.AccessTokenSecret = twtacc.oAuthSecret;
            oAuth.TwitterScreenName = twtacc.twitterScreenName;
            oAuth.TwitterUserId = twtacc.twitterUserId;
            TimeLine timeline_obj = new TimeLine();
            try
            {
                JArray jMentionResp = timeline_obj.Get_Statuses_Mentions_Timeline(oAuth);
                if (jMentionResp == null)
                {
                    return null;
                }
                else
                {
                    foreach (var items in jMentionResp)
                    {
                        Domain.Socioboard.Models.TwitterMentionSugg _objLstData = new Domain.Socioboard.Models.TwitterMentionSugg();

                        _objLstData.postId = items["id_str"].ToString();
                        _objLstData.textMsg = items["text"].ToString();
                        _objLstData.fromName = items["user"]["name"].ToString();
                        _objLstData.fromScreenName = items["user"]["screen_name"].ToString();
                        _objLstData.fromLocation = items["user"]["location"].ToString();
                        if (_objLstData.fromLocation == "")
                        {
                            _objLstData.fromLocation = "NA";
                        }
                        _objLstData.fromFollowers = items["user"]["followers_count"].ToString();
                        _objLstData.fromFollowing = items["user"]["friends_count"].ToString();
                        _objLstData.fromProfilePic = items["user"]["profile_image_url_https"].ToString();
                        _objLstData.fromProfileLinkUrl = "https://twitter.com/" + _objLstData.fromScreenName;
                        _objLstData.postLinkUrl = "https://twitter.com/SB/status/" + _objLstData.postId;
                        lstMentionsUsers.Add(_objLstData);
                    }
                }
                return lstMentionsUsers.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("error finding friendship relation" + ex.StackTrace);
                _logger.LogError("error finding friendship relation" + ex.Message);
                return null;
            }
        }
        public static List<Domain.Socioboard.Models.TwitterMentionSugg> TwitterConversation(string profileId, Model.DatabaseRepository dbr, ILogger _logger, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            List<Domain.Socioboard.Models.TwitterMentionSugg> lstConveUsers = new List<TwitterMentionSugg>();
            Domain.Socioboard.Models.TwitterAccount twtacc = new Domain.Socioboard.Models.TwitterAccount();
            Domain.Socioboard.Models.TwitterAccount imtwtacc = _redisCache.Get<Domain.Socioboard.Models.TwitterAccount>(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId);
            if (imtwtacc == null)
            {
                twtacc = dbr.Find<Domain.Socioboard.Models.TwitterAccount>(t => t.twitterUserId.Equals(profileId)).FirstOrDefault();
                if (twtacc != null)
                {
                    _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheTwitterAccount + profileId, twtacc);
                }
            }
            else
            {
                twtacc = imtwtacc;
            }
            oAuthTwitter oAuth = new oAuthTwitter(_appSettings.twitterConsumerKey, _appSettings.twitterConsumerScreatKey, _appSettings.twitterRedirectionUrl);
            oAuth.AccessToken = twtacc.oAuthToken;
            oAuth.AccessTokenSecret = twtacc.oAuthSecret;
            oAuth.TwitterScreenName = twtacc.twitterScreenName;
            oAuth.TwitterUserId = twtacc.twitterUserId;
            DirectMessage timeline_obj = new DirectMessage();
            List<string> tempScreenNames = new List<string>();
            try
            {
                JArray jMentionResp = timeline_obj.Get_Direct_Messages(oAuth, 100);
                if (jMentionResp == null)
                {
                    return null;
                }
                else
                {
                    foreach (var items in jMentionResp)
                    {
                        Domain.Socioboard.Models.TwitterMentionSugg _objLstData = new Domain.Socioboard.Models.TwitterMentionSugg();

                        _objLstData.postId = items["id_str"].ToString();
                        _objLstData.textMsg = items["sender"]["description"].ToString();
                        _objLstData.fromName = items["sender"]["name"].ToString();
                        _objLstData.fromScreenName = items["sender"]["screen_name"].ToString();
                        _objLstData.fromLocation = items["sender"]["location"].ToString();
                        if (_objLstData.fromLocation == "")
                        {
                            _objLstData.fromLocation = "NA";
                        }
                        _objLstData.fromFollowers = items["sender"]["followers_count"].ToString();
                        _objLstData.fromFollowing = items["sender"]["friends_count"].ToString();
                        _objLstData.fromProfilePic = items["sender"]["profile_image_url_https"].ToString();
                        _objLstData.fromProfileLinkUrl = "https://twitter.com/" + _objLstData.fromScreenName;
                        _objLstData.postLinkUrl = "https://twitter.com/SB/status/" + _objLstData.postId;
                        if (!tempScreenNames.Contains(_objLstData.fromScreenName))
                        {
                            lstConveUsers.Add(_objLstData);
                        }
                        tempScreenNames.Add(_objLstData.fromScreenName);
                    }
                }
                lstConveUsers.RemoveAll(t => t.fromScreenName == twtacc.twitterScreenName);
                return lstConveUsers.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("error finding friendship relation" + ex.StackTrace);
                _logger.LogError("error finding friendship relation" + ex.Message);
                return null;
            }
        }


    }
}
