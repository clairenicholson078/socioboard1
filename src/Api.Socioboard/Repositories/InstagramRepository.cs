﻿using Api.Socioboard.Helper;
using Api.Socioboard.Model;
using Domain.Socioboard.Models.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Socioboard.Instagram.App.Core;
using Socioboard.Instagram.Authentication;
using Socioboard.Instagram.Instagram.Core.MediaMethods;
using Socioboard.Instagram.Instagram.Core.RelationshipMethods;
using Socioboard.Instagram.Instagram.Core.UsersMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Socioboard.Repositories
{
    public class InstagramRepository
    {

        public static Domain.Socioboard.Models.Instagramaccounts getInstagramAccount(string instagramUserId, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            try
            {
                Domain.Socioboard.Models.Instagramaccounts inMemTwitterAcc = _redisCache.Get<Domain.Socioboard.Models.Instagramaccounts>(Domain.Socioboard.Consatants.SocioboardConsts.CacheInstagramAccount + instagramUserId);
                if (inMemTwitterAcc != null)
                {
                    return inMemTwitterAcc;
                }
            }
            catch { }

            List<Domain.Socioboard.Models.Instagramaccounts> lstInstagramaccounts = dbr.Find<Domain.Socioboard.Models.Instagramaccounts>(t => t.InstagramId.Equals(instagramUserId) ).ToList();
            if (lstInstagramaccounts != null && lstInstagramaccounts.Count() > 0)
            {
                _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheInstagramAccount + instagramUserId, lstInstagramaccounts.First());
                return lstInstagramaccounts.First();
            }
            else
            {
                return null;
            }



        }
        public static string AddInstagramAccount(string client_id, string client_secret, string redirect_uri, string code, long userId, long groupId, Model.DatabaseRepository dbr, ILogger _logger, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            string ret = string.Empty;
            oAuthInstagram objInsta = new oAuthInstagram();
            ConfigurationIns configi = new ConfigurationIns("https://api.instagram.com/oauth/authorize/", client_id, client_secret, redirect_uri, "https://api.instagram.com/oauth/access_token", "https://api.instagram.com/v1/", "");
            oAuthInstagram _api = new oAuthInstagram();
            _api = oAuthInstagram.GetInstance(configi);
            AccessToken access = new AccessToken();
            access = _api.AuthGetAccessToken(code);
            UserController objusercontroller = new UserController();
            if (access != null)
            {

                Domain.Socioboard.Models.Instagramaccounts Instagramaccounts = new Domain.Socioboard.Models.Instagramaccounts();
                Domain.Socioboard.Models.Instagramaccounts objInstagramAccount;
                #region InstagramAccount
                InstagramResponse<User> objuser = objusercontroller.GetUserDetails(access.user.id, access.access_token);

                objInstagramAccount = new Domain.Socioboard.Models.Instagramaccounts();
                objInstagramAccount.AccessToken = access.access_token;
                objInstagramAccount.InstagramId = access.user.id;
                try
                {
                    objInstagramAccount.bio = access.user.bio;
                }
                catch {
                    objInstagramAccount.bio = "";
                }
                try
                {
                    objInstagramAccount.ProfileUrl = access.user.profile_picture;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Instagram.asmx.cs >> AddInstagramAccount >> " + ex.StackTrace);
                }
                try
                {
                    objInstagramAccount.InsUserName = access.user.username;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Instagram.asmx.cs >> AddInstagramAccount >> " + ex.StackTrace);
                }
                try
                {
                    objInstagramAccount.TotalImages = objuser.data.counts.media;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Instagram.asmx.cs >> AddInstagramAccount >> " + ex.StackTrace);
                }
                try
                {
                    objInstagramAccount.FollowedBy = objuser.data.counts.followed_by;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Instagram.asmx.cs >> AddInstagramAccount >> " + ex.StackTrace);
                }
                try
                {
                    objInstagramAccount.Followers = objuser.data.counts.follows;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Instagram.asmx.cs >> AddInstagramAccount >> " + ex.StackTrace);
                }
                objInstagramAccount.UserId = userId;
                objInstagramAccount.IsActive = true;
                objInstagramAccount.lastUpdate = DateTime.UtcNow;
                if (objInstagramAccount.InstagramId != null)
                {
                    Instagramaccounts = Api.Socioboard.Repositories.InstagramRepository.getInstagramAccount(objInstagramAccount.InstagramId, _redisCache, dbr);
                    if (Instagramaccounts != null && Instagramaccounts.IsActive == true)
                    {
                        if(Instagramaccounts.UserId == userId)
                        {
                            return "instagram account already added by you.";
                        }
                        return "This Account is added by somebody else.";
                    }
                }
                else
                {
                    return "Issue while fetching instagram userId";
                }

                if (Instagramaccounts == null)
                {
                    int isSaved = dbr.Add<Domain.Socioboard.Models.Instagramaccounts>(objInstagramAccount);
                    if (isSaved == 1)
                    {
                        List<Domain.Socioboard.Models.Instagramaccounts> lstinsAcc = dbr.Find<Domain.Socioboard.Models.Instagramaccounts>(t => t.InstagramId.Equals(objInstagramAccount.InstagramId)).ToList();
                        if (lstinsAcc != null && lstinsAcc.Count() > 0)
                        {
                            isSaved = GroupProfilesRepository.AddGroupProfile(groupId, lstinsAcc.First().InstagramId, lstinsAcc.First().InsUserName, userId, lstinsAcc.First().ProfileUrl, Domain.Socioboard.Enum.SocialProfileType.Instagram, dbr);
                            if (isSaved == 1)
                            {
                                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheUserProfileCount + userId);
                                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGroupProfiles + groupId);

                                new Thread(delegate ()
                                {
                                    GetInstagramSelfFeeds(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, _appSettings);
                                }).Start();
                                new Thread(delegate ()
                                {
                                    GetInstagramUserDetails(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, _redisCache, dbr);
                                }).Start();
                                new Thread(delegate ()
                                {
                                    GetInstagramFollowing(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, 1, _appSettings);
                                }).Start();
                            
                                new Thread(delegate ()
                                {
                                    GetInstagramFollower(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, 1, _appSettings);
                                }).Start();
                                new Thread(delegate ()
                                {
                                    GetInstagramPostLikes(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, 1, _appSettings);
                                    //GetInstagramPostComments(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, _appSettings);
                                }).Start();

                                return "Added_Successfully";
                            }

                        }
                    }
                }
                else
                {
                    objInstagramAccount.id = Instagramaccounts.id;
                    int isSaved = dbr.Update<Domain.Socioboard.Models.Instagramaccounts>(objInstagramAccount);
                    if (isSaved == 1)
                    {
                        List<Domain.Socioboard.Models.Instagramaccounts> lstinsAcc = dbr.Find<Domain.Socioboard.Models.Instagramaccounts>(t => t.InstagramId.Equals(objInstagramAccount.InstagramId)).ToList();
                        if (lstinsAcc != null && lstinsAcc.Count() > 0)
                        {
                            isSaved = GroupProfilesRepository.AddGroupProfile(groupId, lstinsAcc.First().InstagramId, lstinsAcc.First().InsUserName, userId, lstinsAcc.First().ProfileUrl, Domain.Socioboard.Enum.SocialProfileType.Instagram, dbr);
                            if (isSaved == 1)
                            {
                                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheUserProfileCount + userId);
                                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGroupProfiles + groupId);


                                //todo : codes to update feeds 
                                new Thread(delegate ()
                                {
                                    GetInstagramSelfFeeds(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, _appSettings);
                                }).Start();
                                new Thread(delegate ()
                                {
                                    GetInstagramUserDetails(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, _redisCache, dbr);
                                }).Start();
                                new Thread(delegate ()
                                {
                                    GetInstagramFollowing(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, 1, _appSettings);
                                }).Start();
                                new Thread(delegate ()
                                {
                                    GetInstagramFollower(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, 1, _appSettings);
                                }).Start();
                                new Thread(delegate ()
                                {
                                    GetInstagramPostLikes(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, 1, _appSettings);
                                }).Start();
                                    // GetInstagramPostComments(objInstagramAccount.InstagramId, objInstagramAccount.AccessToken, _appSettings);
                                return "Added_Successfully";
                            }
                        }
                    }
                }
            }
            return "issue in access token fetching";
            #endregion
        }

        public static void GetInstagramSelfFeeds(string instagramId, string accessToken, Helper.AppSettings _appSettings)
        {
            //MongoRepository instagarmCommentRepo = new MongoRepository("InstagramComment", _appSettings);
            MongoRepository instagramFeedRepo = new MongoRepository("InstagramFeed", _appSettings);
            try
            {
                Users userInstagram = new Users();
                Media _Media = new Media();
                InstagramResponse<Comment[]> usercmts = new InstagramResponse<Comment[]>();
                CommentController objComment = new CommentController();
                LikesController objLikes = new LikesController();
                string feeds = _Media.UserResentFeeds(instagramId, accessToken);
                if (feeds != null)
                {
                    JObject feed_data = JObject.Parse(feeds);

                    foreach (var item in feed_data["data"])
                    {
                        try
                        {
                            Domain.Socioboard.Models.Mongo.InstagramFeed objInstagramFeed = new Domain.Socioboard.Models.Mongo.InstagramFeed();
                            try
                            {
                                objInstagramFeed.FeedDate = Convert.ToDouble(item["created_time"].ToString());
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.FeedId = item["id"].ToString();
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.Type = item["type"].ToString();
                                if (objInstagramFeed.Type == "video")
                                {
                                    objInstagramFeed.VideoUrl = item["videos"]["standard_resolution"]["url"].ToString();
                                }
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.FeedImageUrl = item["images"]["standard_resolution"]["url"].ToString();
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.InstagramId = instagramId;
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.LikeCount = Int32.Parse(item["likes"]["count"].ToString());
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.CommentCount = Int32.Parse(item["comments"]["count"].ToString());
                            }
                            catch { }
                            try
                            {
                                string str = item["user_has_liked"].ToString();
                                if (str.ToLower() == "false")
                                {
                                    objInstagramFeed.IsLike = 0;
                                }
                                else { objInstagramFeed.IsLike = 1; }
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.AdminUser = item["user"]["username"].ToString();
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.Feed = item["caption"]["text"].ToString();
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.ImageUrl = item["user"]["profile_picture"].ToString();
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.FromId = item["user"]["id"].ToString();
                            }
                            catch { }
                            try
                            {
                                objInstagramFeed.FeedUrl = item["link"].ToString();
                            }
                            catch { }
                            List<Domain.Socioboard.Models.Mongo.InstagramComment> lstInstagramComment = new List<Domain.Socioboard.Models.Mongo.InstagramComment>();
                            usercmts = objComment.GetComment(objInstagramFeed.FeedId, accessToken);
                            for (int cmt = 0; cmt < usercmts.data.Count(); cmt++)
                            {
                                try
                                {
                                    Domain.Socioboard.Models.Mongo.InstagramComment objInstagramComment = new Domain.Socioboard.Models.Mongo.InstagramComment();
                                    try
                                    {
                                        objInstagramComment.Comment = usercmts.data[cmt].text;
                                    }
                                    catch { }
                                    try
                                    {
                                        objInstagramComment.CommentDate = Convert.ToDouble(usercmts.data[cmt].created_time.ToString());
                                    }
                                    catch { }
                                    try
                                    {
                                        objInstagramComment.CommentId = usercmts.data[cmt].id;
                                    }
                                    catch { }

                                    try
                                    {
                                        objInstagramComment.FeedId = objInstagramFeed.FeedId;
                                    }
                                    catch { }
                                    try
                                    {
                                        objInstagramComment.InstagramId = instagramId;
                                    }
                                    catch { }
                                    try
                                    {
                                        objInstagramComment.FromName = usercmts.data[cmt].from.username;
                                    }
                                    catch { }
                                    try
                                    {
                                        objInstagramComment.FromProfilePic = usercmts.data[cmt].from.profile_picture;
                                    }
                                    catch { }

                                    lstInstagramComment.Add(objInstagramComment);
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                            objInstagramFeed._InstagramComment = lstInstagramComment;
                            var ret = instagramFeedRepo.Find<Domain.Socioboard.Models.Mongo.InstagramFeed>(t => t.FeedId.Equals(objInstagramFeed.FeedId) && t.InstagramId.Equals(objInstagramFeed.InstagramId));
                            var task = Task.Run(async () =>
                            {
                                return await ret;
                            });
                            int count = task.Result.Count;

                            if (count < 1)
                            {
                                instagramFeedRepo.Add(objInstagramFeed);
                            }
                            else
                            {
                                FilterDefinition<BsonDocument> filter = new BsonDocument("FeedId", objInstagramFeed.FeedId);
                                var update = Builders<BsonDocument>.Update.Set("IsLike", objInstagramFeed.IsLike).Set("CommentCount", objInstagramFeed.CommentCount).Set("LikeCount", objInstagramFeed.LikeCount).Set("Type", objInstagramFeed.Type).Set("VideoUrl", objInstagramFeed.VideoUrl).Set("_InstagramComment", objInstagramFeed._InstagramComment);
                                instagramFeedRepo.Update<Domain.Socioboard.Models.Mongo.InstagramFeed>(update, filter);
                            }
                           
                           // instagarmCommentRepo.AddList(lstInstagramComment);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void GetInstagramFollowing(string profile_id, string access_token, int status,Helper.AppSettings _appSettings)
        {
           
            Domain.Socioboard.Models.Mongo.MongoMessageModel objMessageModel = new Domain.Socioboard.Models.Mongo.MongoMessageModel();
            JObject post_data = new JObject();
            string url = "https://api.instagram.com/v1/users/self/follows?access_token=" + access_token + "&count=100";
            bool hasData = true;
            while (hasData)
            {
                try
                {
                    post_data = JObject.Parse(ApiInstagramHttp(url));

                }
                catch (Exception ex)
                {
                    hasData = false;
                }
                try
                {
                    dynamic items = post_data["data"];
                    if(items == null)
                    {
                        hasData = false;
                    }
                    foreach (var item in items)
                    {
                        try
                        {
                            Guid Id = Guid.NewGuid();

                            string user_name = item["username"].ToString();
                            string id = item["id"].ToString();
                            string full_name = item["full_name"].ToString();
                            DateTime CreatedTime = DateTime.Now;

                            objMessageModel.id = ObjectId.GenerateNewId();
                            objMessageModel.messageId = "";
                            objMessageModel.profileId = profile_id;
                            objMessageModel.fromId = id;
                            objMessageModel.fromName = "";
                            objMessageModel.RecipientId = profile_id;
                            objMessageModel.messageId = id;
                            objMessageModel.RecipientName = full_name;
                            objMessageModel.Message = "";
                            objMessageModel.fromProfileUrl = "";
                            objMessageModel.RecipientName = "";
                            objMessageModel.type = Domain.Socioboard.Enum.MessageType.InstagramFollowing;
                            objMessageModel.messageDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                            objMessageModel.FollowerCount = 0;
                            objMessageModel.FollowingCount = 0;
                            objMessageModel.messageTimeStamp = Domain.Socioboard.Helpers.SBHelper.ConvertToUnixTimestamp(DateTime.UtcNow);
                            MongoRepository mongorepo = new MongoRepository("MongoMessageModel", _appSettings);
                            var ret = mongorepo.Find<Domain.Socioboard.Models.Mongo.MongoMessageModel>(t => t.RecipientId == objMessageModel.RecipientId && t.fromId == objMessageModel.fromId && t.type == Domain.Socioboard.Enum.MessageType.InstagramFollowing);
                            var task = Task.Run(async () => {
                                return await ret;
                            });
                            if (task.Result != null)
                            {
                                int count = task.Result.Count;
                                if (count < 1)
                                {
                                    mongorepo.Add(objMessageModel);
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    url = post_data["pagination"]["next_url"].ToString();
                }
                catch (Exception ex)
                {
                    hasData = false;
                }
            }
           
       
           
        }

        public static void GetInstagramFollower(string profile_id, string access_token, int status, Helper.AppSettings _appSettings)
        {

            Domain.Socioboard.Models.Mongo.MongoMessageModel objMessageModel = new Domain.Socioboard.Models.Mongo.MongoMessageModel();
            JObject post_data = new JObject();
            string url = "https://api.instagram.com/v1/users/self/followed-by?access_token=" + access_token + "&cout=100";
            bool hasData = true;
            while (hasData)
            {
                try
                {
                    post_data = JObject.Parse(ApiInstagramHttp(url));
                }
                catch (Exception ex)
                {
                    hasData = false;
                }
                try
                {
                    dynamic items = post_data["data"];
                    if (items == null)
                    {
                        hasData = false;
                    }
                    foreach (var item in items)
                    {
                        try
                        {
                            Guid Id = Guid.NewGuid();
                            string user_name = item["username"].ToString();
                            string id = item["id"].ToString();
                            string full_name = item["full_name"].ToString();
                            string image_url = item["profile_picture"].ToString();
                            DateTime CreatedTime = DateTime.UtcNow;
                            objMessageModel.id = ObjectId.GenerateNewId();
                            objMessageModel.profileId = profile_id;
                            objMessageModel.messageId = id;
                            objMessageModel.fromId = id;
                            objMessageModel.fromName = user_name;
                            objMessageModel.RecipientId = profile_id;
                            objMessageModel.RecipientName = "";
                            objMessageModel.fromProfileUrl = image_url;
                            objMessageModel.type = Domain.Socioboard.Enum.MessageType.InstagramFollower;
                            objMessageModel.FollowerCount = 0;
                            objMessageModel.FollowingCount = 0;
                            objMessageModel.readStatus = status;
                            objMessageModel.messageTimeStamp = Helper.DateExtension.ConvertToUnixTimestamp(DateTime.UtcNow);
                            MongoRepository mongorepo = new MongoRepository("MongoMessageModel", _appSettings);
                            var ret = mongorepo.Find<Domain.Socioboard.Models.Mongo.MongoMessageModel>(t => t.RecipientId == objMessageModel.RecipientId && t.fromId == objMessageModel.fromId && t.type == Domain.Socioboard.Enum.MessageType.InstagramFollower);
                            var task = Task.Run(async () => {
                                return await ret;
                            });
                            if (task.Result != null)
                            {
                                int count = task.Result.Count;
                                if (count < 1)
                                {
                                    mongorepo.Add(objMessageModel);
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    url = post_data["pagination"]["next_url"].ToString();
                }
                catch (Exception ex)
                {
                    hasData = false;
                }
            }
           

          
        }
        public static string GetInstagramUserDetails(string profile_id, string access_token, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            string code_status = "false";
            Domain.Socioboard.Models.Mongo.InstagramUserDetails insert = new Domain.Socioboard.Models.Mongo.InstagramUserDetails();
            JObject post_data = new JObject();
            string url = "https://api.instagram.com/v1/users/" + profile_id + "?access_token=" + access_token;

            try
            {
                post_data = JObject.Parse(ApiInstagramHttp(url));
            }
            catch (Exception)
            {
            }

            try
            {
                dynamic item = post_data["data"];

                try
                {
                    string insta_name = item["username"].ToString();
                    string full_name = item["full_name"].ToString();
                    string imageUrl = item["profile_picture"].ToString();
                    string media_count = item["counts"]["media"].ToString();
                    DateTime Created_Time = DateTime.Now;
                    string follower = item["counts"]["followed_by"].ToString();
                    string following = item["counts"]["follows"].ToString();
                    Domain.Socioboard.Models.Instagramaccounts Instagramaccounts = Api.Socioboard.Repositories.InstagramRepository.getInstagramAccount(profile_id, _redisCache, dbr);

                    if (Instagramaccounts != null && Instagramaccounts.IsActive == true)
                    {
                        Instagramaccounts.TotalImages = Convert.ToInt32(media_count);
                        Instagramaccounts.Followers = Convert.ToInt32(follower);
                        Instagramaccounts.FollowedBy = Convert.ToInt32(following);
                        Instagramaccounts.InsUserName = insta_name;
                        Instagramaccounts.ProfileUrl = imageUrl;
                        Instagramaccounts.InstagramId = profile_id;
                        dbr.Update<Domain.Socioboard.Models.Instagramaccounts>(Instagramaccounts);
                    }

                    DateTime t1 = DateTime.Now.Date;
                    DateTime t2 = DateTime.Now.Date.AddHours(12);
                    DateTime t3 = DateTime.Now.AddDays(1).Date.AddSeconds(-1);
                    if (DateTime.Now.TimeOfDay >= t1.TimeOfDay && DateTime.Now.TimeOfDay < t2.TimeOfDay)
                    {
                        if (Instagramaccounts != null && Instagramaccounts.IsActive == true)
                        {
                            Instagramaccounts.TotalImages = Convert.ToInt32(media_count);
                            Instagramaccounts.Followers = Convert.ToInt32(follower);
                            Instagramaccounts.FollowedBy = Convert.ToInt32(following);
                            Instagramaccounts.InsUserName = insta_name;
                            Instagramaccounts.ProfileUrl = imageUrl;
                            Instagramaccounts.InstagramId = profile_id;
                            dbr.Update<Domain.Socioboard.Models.Instagramaccounts>(Instagramaccounts);
                        }
                    }
                    if (DateTime.Now.TimeOfDay >= t2.TimeOfDay && DateTime.Now.TimeOfDay < t3.TimeOfDay)
                    {
                        if (Instagramaccounts != null && Instagramaccounts.IsActive == true)
                        {
                            Instagramaccounts.TotalImages = Convert.ToInt32(media_count);
                            Instagramaccounts.Followers = Convert.ToInt32(follower);
                            Instagramaccounts.FollowedBy = Convert.ToInt32(following);
                            Instagramaccounts.InsUserName = insta_name;
                            Instagramaccounts.ProfileUrl = imageUrl;
                            Instagramaccounts.InstagramId = profile_id;
                            dbr.Update<Domain.Socioboard.Models.Instagramaccounts>(Instagramaccounts);
                        }
                    }

                    code_status = "true";



                }
                catch (Exception ex)
                {
                }


            }
            catch (Exception ex)
            {

            }
            return code_status;
        }

        public static string GetInstagramPostLikes(string profile_id, string access_token, int status, Helper.AppSettings _appSettings)
        {

            MongoRepository InstagramPostLikesRepo = new MongoRepository("InstagramPostLikes", _appSettings);
            MongoRepository InstagramSelfFeedRepo = new MongoRepository("InstagramSelfFeed", _appSettings);
            string code_status = "false";
            Domain.Socioboard.Models.Mongo.InstagramPostLikes insert = new Domain.Socioboard.Models.Mongo.InstagramPostLikes();
            JObject post_data = new JObject();
            string url = "https://api.instagram.com/v1/users/" + profile_id + "/media/recent?access_token=" + access_token + "&count=30";
            try
            {
                post_data = JObject.Parse(ApiInstagramHttp(url));
            }
            catch (Exception ex)
            {
            }
            try
            {
                dynamic items = post_data["data"];
                foreach (var item in items)
                {
                    string post_url = string.Empty;
                    string feed_url = string.Empty;
                    string user_name = string.Empty;
                    Guid Id = Guid.NewGuid();
                    string feed_id = item["id"].ToString();
                    string feed_type = item["type"].ToString();
                    string created_time_feed = item["created_time"].ToString();
                    DateTime create_time_feed = DateExtension.ToDateTime(DateTime.Now, long.Parse(created_time_feed));

                    Domain.Socioboard.Models.Mongo.InstagramSelfFeed send_data = new Domain.Socioboard.Models.Mongo.InstagramSelfFeed();

                    try
                    {
                        if (feed_type.Equals("video"))
                        {
                            try
                            {
                                post_url = item["videos"]["standard_resolution"]["url"].ToString();
                                feed_url = item["link"].ToString();
                                user_name = item["user"]["username"].ToString();
                            }
                            catch (Exception)
                            {
                            }

                        }
                        else if (feed_type.Equals("image"))
                        {
                            try
                            {
                                post_url = item["images"]["standard_resolution"]["url"].ToString();
                                feed_url = item["link"].ToString();
                                user_name = item["user"]["username"].ToString();
                            }
                            catch (Exception)
                            {
                            }
                        }

                        send_data.User_name = user_name;
                        send_data.ProfileId = profile_id;
                        send_data.FeedId = feed_id;
                        send_data.Accesstoken = access_token;
                        send_data.Post_url = post_url;
                        send_data.Link = feed_url;
                        send_data.Type = feed_type;
                        send_data.Created_Time = created_time_feed;
                        var ret = InstagramSelfFeedRepo.Find<Domain.Socioboard.Models.Mongo.InstagramSelfFeed>(t => t.FeedId.Equals(send_data.FeedId));
                        var task = Task.Run(async () =>
                        {
                            return await ret;
                        });
                        int count = task.Result.Count;

                        if (count < 1)
                        {
                            InstagramSelfFeedRepo.Add(send_data);
                        }
                    }
                    catch (Exception ex)
                    {
                    }



                    try
                    {
                        dynamic likes = null;
                        string url1 = "https://api.instagram.com/v1/media/" + feed_id + "/likes?access_token=" + access_token ;
                        try
                        {
                            likes = JObject.Parse(ApiInstagramHttp(url1));
                            likes = likes["data"];
                        }
                        catch (Exception ex)
                        {
                        }

                       
                        foreach (var like in likes)
                        {
                            try
                            {
                                string liked_by_id = like["id"].ToString();
                                string liked_by_name = like["username"].ToString();
                                insert.Profile_Id = profile_id;
                                insert.Feed_Id = feed_id;
                                insert.Liked_By_Id = liked_by_id;
                                insert.Liked_By_Name = liked_by_name;
                                insert.Feed_Type = feed_type;
                                insert.Created_Date = Helper.DateExtension.ConvertToUnixTimestamp(DateTime.UtcNow);
                                insert.Status = status;
                                var ret = InstagramPostLikesRepo.Find<Domain.Socioboard.Models.Mongo.InstagramPostLikes>(t => t.Feed_Id.Equals(insert.Feed_Id));
                                var task = Task.Run(async () =>
                                {
                                    return await ret;
                                });
                                int count = task.Result.Count;

                                if (count < 1)
                                {
                                    InstagramPostLikesRepo.Add(insert);
                                }
                                code_status = "true";
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

            catch (Exception ex)
            {
            }


            return code_status;

        }

        public static string GetInstagramPostComments(string profile_id, string access_token, Helper.AppSettings _appSettings)
        {

            MongoRepository InstagramPostCommentsRepo = new MongoRepository("InstagramPostComments", _appSettings);
            string code_status = "false";
            Domain.Socioboard.Models.Mongo.InstagramPostComments insert = new Domain.Socioboard.Models.Mongo.InstagramPostComments();
            JObject post_data = new JObject();
            string url = "https://api.instagram.com/v1/users/" + profile_id + "/media/recent?access_token=" + access_token + "&count=100";
            try
            {
                post_data = JObject.Parse(ApiInstagramHttp(url));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                dynamic items = post_data["data"];
                foreach (var item in items)
                {

                    Guid Id = Guid.NewGuid();
                    string feed_id = item["id"].ToString();
                    string feed_type = item["type"].ToString();
                    string created_time_feed = item["created_time"].ToString();
                    DateTime create_time_feed = DateExtension.ToDateTime(DateTime.Now, long.Parse(created_time_feed));
                    if (create_time_feed.Date >= DateTime.Now.AddDays(-90).Date)
                    {
                        dynamic comments = item["comments"]["data"];

                        foreach (var comment in comments)
                        {

                            string created_time = comment["created_time"].ToString();
                            DateTime create_time = DateExtension.ToDateTime(DateTime.Now, long.Parse(created_time));
                            string text = comment["text"].ToString();
                            string commented_by_id = comment["from"]["id"].ToString();
                            string commented_by_name = comment["from"]["username"].ToString();
                            string comment_id = comment["id"].ToString();

                            insert.Profile_Id = profile_id;
                            insert.Feed_Id = feed_id;
                            insert.Commented_By_Id = commented_by_id;
                            insert.Commented_By_Name = commented_by_name;
                            insert.Created_Time = Helper.DateExtension.ConvertToUnixTimestamp(Convert.ToDateTime(created_time));
                            insert.Comment_Id = comment_id;
                            insert.Comment = text;
                            insert.Feed_Type = feed_type;
                            var ret = InstagramPostCommentsRepo.Find<Domain.Socioboard.Models.Mongo.InstagramPostComments>(t => t.Feed_Id.Equals(insert.Feed_Id));
                            var task = Task.Run(async () =>
                            {
                                return await ret;
                            });
                            int count = task.Result.Count;

                            if (count < 1)
                            {
                                InstagramPostCommentsRepo.Add(insert);
                            }
                            code_status = "true";
                        }

                    }
                }
            }
            catch (Exception ex)
            {
            }



            return code_status;

        }

        public static List<Domain.Socioboard.Models.Mongo.InstagramFeed> GetInstagramFeeds(string instagramId, Helper.AppSettings _appSettings, Helper.Cache _redisCache,int skip,int count)
        {
           
            List<Domain.Socioboard.Models.Mongo.intafeed> lstintafeed = new List<Domain.Socioboard.Models.Mongo.intafeed>();
            List<Domain.Socioboard.Models.Mongo.InstagramFeed> iMmemInstagramFeed = _redisCache.Get<List<Domain.Socioboard.Models.Mongo.InstagramFeed>>(Domain.Socioboard.Consatants.SocioboardConsts.CacheInstagramFeed + instagramId);

            MongoRepository InstagramFeedRepo = new MongoRepository("InstagramFeed", _appSettings);
          //  var ret = InstagramFeedRepo.Find<Domain.Socioboard.Models.Mongo.InstagramFeed>(t => t.InstagramId.Equals(instagramId));
            var builder = Builders<Domain.Socioboard.Models.Mongo.InstagramFeed>.Sort;
            var sort = builder.Descending(t => t.FeedDate);
            var ret = InstagramFeedRepo.FindWithRange<Domain.Socioboard.Models.Mongo.InstagramFeed>(t => t.InstagramId.Equals(instagramId), sort, skip, count);
            var task = Task.Run(async () =>
            {
                return await ret;
            });
            IList<Domain.Socioboard.Models.Mongo.InstagramFeed> _lstInstagramFeed = task.Result;
            //var sortt= _lstInstagramFeed.OrderByDescending(t => t.FeedDate);
            //_redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheInstagramFeed + instagramId, sortt.ToList());
            //foreach (var item in sortt.ToList())
            //{
            //    Domain.Socioboard.Models.Mongo.intafeed _intafeed = new Domain.Socioboard.Models.Mongo.intafeed();
            //    MongoRepository InstagramCommentRepo = new MongoRepository("InstagramComment", _appSettings);
            //    var builderComment = Builders<Domain.Socioboard.Models.Mongo.InstagramComment>.Sort;
            //    var sortcomment = builderComment.Descending(t => t.CommentDate);
            //    var ret1 = InstagramCommentRepo.FindWithRange<Domain.Socioboard.Models.Mongo.InstagramComment>(t => t.FeedId.Equals(item.FeedId), sortcomment, skip, 5);
            //    var taskq = Task.Run(async () =>
            //    {
            //        return await ret1;
            //    });
            //    IList<Domain.Socioboard.Models.Mongo.InstagramComment> _lstInstagramComment = taskq.Result;
            //    _intafeed._InstagramFeed = item;
            //    _intafeed._InstagramComment = _lstInstagramComment.ToList();
            //    lstintafeed.Add(_intafeed);
            //}
            return _lstInstagramFeed.ToList();

        }

        public static List<Domain.Socioboard.Models.Mongo.InstagramFeed> GetTopInstagramFilterFeed(string instagramId, Helper.AppSettings _appSettings, Helper.Cache _redisCache, int skip, int count, string typeFilter)
        {
            List<Domain.Socioboard.Models.Mongo.intafeed> lstintafeed = new List<Domain.Socioboard.Models.Mongo.intafeed>();
            List<Domain.Socioboard.Models.Mongo.InstagramFeed> lstInstaFeedsLs = new List<InstagramFeed>();
            MongoRepository mongorepo = new MongoRepository("InstagramFeed", _appSettings);
            var builder = Builders<InstagramFeed>.Sort;
            var sort = builder.Descending(t => t.FeedDate);

                var result = mongorepo.FindWithRange<Domain.Socioboard.Models.Mongo.InstagramFeed>(t => t.InstagramId.Equals(instagramId) && t.Type.Equals(typeFilter), sort, skip, count);
                var task = Task.Run(async () =>
                {
                    return await result;
                });
                IList<Domain.Socioboard.Models.Mongo.InstagramFeed> lstInstaFeeds = task.Result;
                lstInstaFeedsLs = lstInstaFeeds.ToList();


            //List<string> postIds = new List<string>();
            //foreach (var x in lstInstaFeedsLs)
            //{
            //    postIds.Add(x.FeedId);
            //}
            //MongoRepository mongorepocomment = new MongoRepository("InstagramComment", _appSettings);
            //var resultcomment = mongorepocomment.Find<Domain.Socioboard.Models.Mongo.InstagramComment>(t => postIds.Contains(t.FeedId));
            //var taskcomment = Task.Run(async () =>
            //{
            //    return await resultcomment;
            //});
            //IList<Domain.Socioboard.Models.Mongo.InstagramComment> lstFbPostComment = taskcomment.Result;
            //List<Domain.Socioboard.Models.Mongo.InstagramComment> tempData = lstFbPostComment.ToList();

            //foreach (var item in lstInstaFeedsLs)
            //{
            //    Domain.Socioboard.Models.Mongo.intafeed _intafeed = new Domain.Socioboard.Models.Mongo.intafeed();
            //    List<Domain.Socioboard.Models.Mongo.InstagramComment> lstFbPostCommentTemp = tempData.Where(t => t.FeedId == item.FeedId).ToList();
            //    _intafeed._InstagramFeed = item;
            //    _intafeed._InstagramComment = lstFbPostCommentTemp.ToList();
            //    lstintafeed.Add(_intafeed);
            //}
            return lstInstaFeedsLs.ToList();
        }

        public static List<Domain.Socioboard.Models.Mongo.InstagramFeed> GetTopInstagramSortFeed(string instagramId, Helper.AppSettings _appSettings, Helper.Cache _redisCache, int skip, int count, string typeSort)
        {
            List<Domain.Socioboard.Models.Mongo.intafeed> lstintafeed = new List<Domain.Socioboard.Models.Mongo.intafeed>();
            List<Domain.Socioboard.Models.Mongo.InstagramFeed> lstInstaFeedsLs = new List<InstagramFeed>();
            MongoRepository mongorepo = new MongoRepository("InstagramFeed", _appSettings);
            var builder = Builders<InstagramFeed>.Sort;
            var sort = builder.Descending(t => t.FeedDate);

            var result = mongorepo.FindWithRange<Domain.Socioboard.Models.Mongo.InstagramFeed>(t => t.InstagramId.Equals(instagramId), sort, skip, count);
            var task = Task.Run(async () =>
            {
                return await result;
            });
            List<Domain.Socioboard.Models.Mongo.InstagramFeed> tempFeeds=null;
            if (typeSort == "mLikes")
            {
                IList<Domain.Socioboard.Models.Mongo.InstagramFeed> lstInstaFeeds = task.Result.OrderByDescending(t => t.LikeCount).ToList();
                tempFeeds = lstInstaFeeds.ToList();
            }
            else if (typeSort == "mComments")
            {
                IList<Domain.Socioboard.Models.Mongo.InstagramFeed> lstInstaFeeds = task.Result.OrderByDescending(t => t.CommentCount).ToList();
                tempFeeds = lstInstaFeeds.ToList();
            }
            else if (typeSort == "lLikes")
            {
                IList<Domain.Socioboard.Models.Mongo.InstagramFeed> lstInstaFeeds = task.Result.OrderBy(t => t.LikeCount).ToList();
                tempFeeds = lstInstaFeeds.ToList();
            }
            else if (typeSort == "lComments")
            {
                IList<Domain.Socioboard.Models.Mongo.InstagramFeed> lstInstaFeeds = task.Result.OrderBy(t => t.CommentCount).ToList();
                tempFeeds = lstInstaFeeds.ToList();
            }

            lstInstaFeedsLs = tempFeeds.ToList();


            //List<string> postIds = new List<string>();
            //foreach (var x in lstInstaFeedsLs)
            //{
            //    postIds.Add(x.FeedId);
            //}
            //MongoRepository mongorepocomment = new MongoRepository("InstagramComment", _appSettings);
            //var resultcomment = mongorepocomment.Find<Domain.Socioboard.Models.Mongo.InstagramComment>(t => postIds.Contains(t.FeedId));
            //var taskcomment = Task.Run(async () =>
            //{
            //    return await resultcomment;
            //});
            //IList<Domain.Socioboard.Models.Mongo.InstagramComment> lstFbPostComment = taskcomment.Result;
            //List<Domain.Socioboard.Models.Mongo.InstagramComment> tempData = lstFbPostComment.ToList();

            //foreach (var item in lstInstaFeedsLs)
            //{
            //    Domain.Socioboard.Models.Mongo.intafeed _intafeed = new Domain.Socioboard.Models.Mongo.intafeed();
            //    List<Domain.Socioboard.Models.Mongo.InstagramComment> lstFbPostCommentTemp = tempData.Where(t => t.FeedId == item.FeedId).ToList();
            //    _intafeed._InstagramFeed = item;
            //    _intafeed._InstagramComment = lstFbPostCommentTemp.ToList();
            //    lstintafeed.Add(_intafeed);
            //}
            return lstInstaFeedsLs.ToList();
        }

        public static void InstagramLikeUnLike(int LikeCount, int IsLike, string FeedId, string InstagramId,long groupId, Helper.AppSettings _appSettings, Helper.Cache _redisCache,Model.DatabaseRepository dbr)
        {
            MongoRepository instagramFeedRepo = new MongoRepository("InstagramFeed", _appSettings);
            Domain.Socioboard.Models.Instagramaccounts _Instagramaccounts = Repositories.InstagramRepository.getInstagramAccount(InstagramId, _redisCache, dbr);
            LikesController objlikes = new LikesController();
            if (IsLike == 1)
            {
                LikeCount = LikeCount - 1;
                IsLike = 0;
                bool ret = objlikes.DeleteLike(FeedId, _Instagramaccounts.AccessToken);
            }
            else
            {
                LikeCount = LikeCount + 1;
                IsLike = 1;
                bool ret = objlikes.PostUserLike(FeedId, _Instagramaccounts.AccessToken);
            }
                
            FilterDefinition<BsonDocument> filter = new BsonDocument("FeedId", FeedId);
            var update = Builders<BsonDocument>.Update.Set("IsLike",IsLike).Set("LikeCount",LikeCount);
            instagramFeedRepo.Update<Domain.Socioboard.Models.Mongo.InstagramFeed>(update, filter);
        }

        public static string AddInstagramComment(string FeedId, string Text, string InstagramId, long groupId, Helper.AppSettings _appSettings, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            MongoRepository instagarmCommentRepo = new MongoRepository("InstagramComment", _appSettings);
            MongoRepository instagramFeedRepo = new MongoRepository("InstagramFeed", _appSettings);
            Domain.Socioboard.Models.Mongo.InstagramComment _InstagramComment = new Domain.Socioboard.Models.Mongo.InstagramComment();
            Domain.Socioboard.Models.Instagramaccounts _Instagramaccounts = Repositories.InstagramRepository.getInstagramAccount(InstagramId, _redisCache, dbr);
            CommentController objComment = new CommentController();
            string ret = objComment.PostCommentAdd(FeedId, Text, _Instagramaccounts.AccessToken);
            if (!string.IsNullOrEmpty(ret))
            {
                try
                {
                    JObject JData = JObject.Parse(ret);
                    string commentid = JData["data"]["id"].ToString();
                    string time = JData["data"]["created_time"].ToString();
                    string profilepic = JData["data"]["from"]["profile_picture"].ToString();
                    string username = JData["data"]["from"]["username"].ToString();
                    _InstagramComment.Id = ObjectId.GenerateNewId();
                    _InstagramComment.strId = ObjectId.GenerateNewId().ToString();
                    _InstagramComment.FeedId = FeedId;
                    _InstagramComment.InstagramId = InstagramId;
                    _InstagramComment.FromProfilePic = profilepic;
                    _InstagramComment.FromName = username;
                    _InstagramComment.CommentDate = Convert.ToDouble(time);
                    _InstagramComment.Comment = Text;
                    _InstagramComment.CommentId = commentid;
                    instagarmCommentRepo.Add<Domain.Socioboard.Models.Mongo.InstagramComment>(_InstagramComment);

                    var retcomment = instagramFeedRepo.Find<Domain.Socioboard.Models.Mongo.InstagramFeed>(t => t.FeedId == FeedId);
                    var task=Task.Run(async()=>{
                        return await retcomment;
                    });
                    List<Domain.Socioboard.Models.Mongo.InstagramFeed> lstfeed = task.Result.ToList();
                    Domain.Socioboard.Models.Mongo.InstagramFeed feed = lstfeed.First();
                    feed.CommentCount = feed.CommentCount + 1;
                    FilterDefinition<BsonDocument> filter = new BsonDocument("FeedId", FeedId);
                    var update = Builders<BsonDocument>.Update.Set("CommentCount", feed.CommentCount);
                    instagramFeedRepo.Update<Domain.Socioboard.Models.Mongo.InstagramFeed>(update, filter);
                    return "comment";
                }
                catch (Exception ex)
                {
                    return "";
                }

            }
            else
            {
                return "";
            }
        }

        public static string DeleteProfile(Model.DatabaseRepository dbr, string profileId, long userId, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.Instagramaccounts fbAcc = dbr.Find<Domain.Socioboard.Models.Instagramaccounts>(t => t.InstagramId.Equals(profileId) && t.UserId == userId && t.IsActive).FirstOrDefault();
            if (fbAcc != null)
            {
                //fbAcc.IsActive = false;
                //dbr.Update<Domain.Socioboard.Models.Instagramaccounts>(fbAcc);
                dbr.Delete<Domain.Socioboard.Models.Instagramaccounts>(fbAcc);
                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheInstagramAccount + profileId);
                return "Deleted";
            }
            else
            {
                return "Account Not Exist";
            }
        }
        public static string ApiInstagramHttp(string url)
        {
            try
            {

                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
                httpRequest.Method = "GET";
                httpRequest.ContentType = "application/x-www-form-urlencoded";
                HttpWebResponse httResponse = (HttpWebResponse)httpRequest.GetResponse();
                Stream responseStream = httResponse.GetResponseStream();
                StreamReader responseStreamReader = new StreamReader(responseStream, System.Text.Encoding.Default);
                string pageContent = responseStreamReader.ReadToEnd();
                responseStreamReader.Close();
                responseStream.Close();
                httResponse.Close();
                return pageContent;
            }
            catch (Exception ex)
            {
                return "";
            }

        }

        //public static string SearchastagData(string tag, string InstagramId, long groupId, Helper.AppSettings _appSettings, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        //{
        //    Domain.Socioboard.Models.Instagramaccounts _Instagramaccounts = Repositories.InstagramRepository.getInstagramAccount(InstagramId, _redisCache, dbr);
        //    TagController objSearchtag = new TagController();
        //    string reta = objSearchtag.GetTagSearch(tag, _Instagramaccounts.AccessToken);
        //    return "";
        //}

        public static string Searchbyname(string quary, string InstagramId,string count, Helper.AppSettings _appSettings, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            Domain.Socioboard.Models.Instagramaccounts _Instagramaccounts = Repositories.InstagramRepository.getInstagramAccount(InstagramId, _redisCache, dbr);
            UserController objSearch = new UserController();
            Relationship objfollw = new Relationship();
            string reta = objSearch.GetUsersSearch(quary, count, _Instagramaccounts.AccessToken, InstagramId);
            return reta;
        }


        public static string Followpeople(string InstagramId, string followingid, Helper.AppSettings _appSettings, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            Domain.Socioboard.Models.Instagramaccounts _Instagramaccounts = Repositories.InstagramRepository.getInstagramAccount(InstagramId, _redisCache, dbr);
            Relationship objfollw = new Relationship();
            string respon = objfollw.FollowPost(followingid, _Instagramaccounts.AccessToken);
            return respon ;
        }
    }
}
