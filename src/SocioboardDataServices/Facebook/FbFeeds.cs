﻿using Domain.Socioboard.Models.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Socioboard.Facebook.Data;
using SocioboardDataServices.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocioboardDataServices.Helper;

namespace SocioboardDataServices.Facebook
{
    public class FbFeeds
    {
        public static int apiHitsCount = 0;
        public static int MaxapiHitsCount = 25;

        public int updateFacebookFeeds(Domain.Socioboard.Models.Facebookaccounts fbAcc)
        {
            apiHitsCount = 0;
            var databaseRepository = new DatabaseRepository();

            if (fbAcc.LastUpdate.AddHours(1) <= DateTime.UtcNow)
            {
                if (fbAcc.IsAccessTokenActive)
                {
                    var feeds = FacebookApiHelper.GetOwnFeedDetails(fbAcc.AccessToken);

                    dynamic profile = FacebookApiHelper.GetUserDetails(fbAcc.AccessToken);

                    apiHitsCount++;

                    if (Convert.ToString(profile) != "Invalid Access Token")
                    {
                        fbAcc.Friends = FacebookApiHelper.GetFriendCounts(fbAcc.AccessToken);

                        apiHitsCount++;

                        try
                        {
                            fbAcc.EmailId = Convert.ToString(profile["email"]);
                        }
                        catch
                        {
                            fbAcc.EmailId = fbAcc.EmailId;
                        }

                        try
                        {
                            fbAcc.ProfileUrl = (Convert.ToString(profile["link"]));
                        }
                        catch
                        {
                            fbAcc.ProfileUrl = (Convert.ToString(profile["picture"]["data"]["url"]));

                        }
                        try
                        {
                            fbAcc.Gender = (Convert.ToString(profile["gender"]));
                        }
                        catch
                        {
                            fbAcc.Gender = fbAcc.Gender;
                        }
                        try
                        {
                            fbAcc.Bio = (Convert.ToString(profile["bio"]));
                        }
                        catch
                        {
                            fbAcc.Bio = fbAcc.Bio;
                        }
                        try
                        {
                            fbAcc.About = (Convert.ToString(profile["about"]));
                        }
                        catch
                        {
                            fbAcc.About = fbAcc.About;
                        }
                        try
                        {
                            fbAcc.CoverPic = (Convert.ToString(profile["cover"]["source"]));
                        }
                        catch
                        {
                            fbAcc.CoverPic = fbAcc.CoverPic;
                        }
                        try
                        {
                            fbAcc.Birthday = (Convert.ToString(profile["birthday"]));
                        }
                        catch
                        {
                            fbAcc.Birthday = fbAcc.Birthday;
                        }
                        try
                        {
                            JArray arry = JArray.Parse(profile["education"]);
                            if (arry.Count > 0)
                            {
                                fbAcc.College = Convert.ToString(arry[arry.Count - 1]["school"]["name"]);
                                fbAcc.Education = Convert.ToString(arry[arry.Count - 1]["concentration"]["name"]);
                            }
                        }
                        catch
                        {
                            fbAcc.College = fbAcc.College;
                            fbAcc.Education = fbAcc.Education;
                        }
                        try
                        {
                            JArray arry = JArray.Parse(profile["work"]);
                            if (arry.Count > 0)
                            {
                                fbAcc.WorkPosition = Convert.ToString(arry[0]["position"]["name"]);
                                fbAcc.WorkCompany = Convert.ToString(arry[0]["employer"]["name"]);
                            }
                        }
                        catch
                        {
                            fbAcc.WorkPosition = fbAcc.WorkPosition;
                            fbAcc.WorkCompany = fbAcc.WorkCompany;
                        }

                        try
                        {
                            databaseRepository.Update(fbAcc);
                        }
                        catch { }

                        if (apiHitsCount < MaxapiHitsCount && feeds != null && feeds["data"] != null)
                        {

                            apiHitsCount++;
                            if (feeds["data"] != null)
                            {
                                Console.WriteLine(feeds["data"]);
                                foreach (var result in feeds["data"])
                                {
                                    MongoFacebookFeed objFacebookFeed = new MongoFacebookFeed();
                                    objFacebookFeed.Type = "fb_feed";
                                    objFacebookFeed.ProfileId = fbAcc.FbUserId;
                                    objFacebookFeed.Id = MongoDB.Bson.ObjectId.GenerateNewId();
                                    try
                                    {
                                        objFacebookFeed.FromProfileUrl = "http://graph.facebook.com/" + result["from"]["id"] + "/picture?type=small";
                                    }
                                    catch (Exception)
                                    {
                                        objFacebookFeed.FromProfileUrl = "http://graph.facebook.com/" + fbAcc.FbUserId + "/picture?type=small";
                                    }
                                    try
                                    {
                                        objFacebookFeed.FromName = result["from"]["name"].ToString();
                                    }
                                    catch (Exception)
                                    {

                                        objFacebookFeed.FromName = fbAcc.FbUserName;
                                    }
                                    try
                                    {
                                        objFacebookFeed.FromId = result["from"]["id"].ToString();
                                    }
                                    catch (Exception)
                                    {

                                        objFacebookFeed.FromId = fbAcc.FbUserId;
                                    }
                                    objFacebookFeed.FeedId = result["id"].ToString();
                                    try
                                    {
                                        objFacebookFeed.FeedDateToshow = DateTime.Parse(result["created_time"].ToString()).ToString("yyyy/MM/dd HH:mm:ss");                                       
                                        objFacebookFeed.FeedDate = Domain.Socioboard.Helpers.SBHelper.ConvertToUnixTimestamp(Convert.ToDateTime(objFacebookFeed.FeedDateToshow)).ToString();
                                        objFacebookFeed.FeedDateToshow = Domain.Socioboard.Helpers.SBHelper.ConvertFromUnixTimestamp(Convert.ToDouble(objFacebookFeed.FeedDate)).ToString("yyyy/MM/dd HH:mm:ss");

                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    try
                                    {
                                        objFacebookFeed.FbComment = "http://graph.facebook.com/" + result["id"] + "/comments";
                                        objFacebookFeed.FbLike = "http://graph.facebook.com/" + result["id"] + "/likes";
                                    }
                                    catch (Exception)
                                    {

                                    }
                                    try
                                    {
                                        objFacebookFeed.Likecount = result["likes"]["summary"]["total_count"].ToString();
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    try
                                    {
                                        objFacebookFeed.Commentcount = result["comments"]["summary"]["total_count"].ToString();
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    try
                                    {
                                        objFacebookFeed.postType = result["type"].ToString();
                                    }                                 
                                    catch
                                    {
                                        objFacebookFeed.postType = "status";
                                    }
                                    try
                                    {
                                        objFacebookFeed.postingFrom = result["application"]["name"].ToString();
                                    }
                                    catch
                                    {
                                        objFacebookFeed.postingFrom = "Facebook";
                                    }
                                    try
                                    {
                                        objFacebookFeed.Picture = result["picture"].ToString();
                                    }
                                    catch (Exception ex)
                                    {
                                        objFacebookFeed.Picture = "";
                                    }

                                    string message = string.Empty;

                                    try
                                    {
                                        if (result["message"] != null)
                                        {
                                            message = result["message"];
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        try
                                        {
                                            if (result["description"] != null)
                                            {
                                                message = result["description"];
                                            }
                                        }
                                        catch (Exception exx)
                                        {
                                            try
                                            {
                                                if (result["story"] != null)
                                                {
                                                    message = result["story"];
                                                }
                                            }
                                            catch (Exception exxx)
                                            {
                                                message = string.Empty;
                                            }
                                        }

                                    }

                                    if (message == null)
                                    {
                                        message = "";
                                    }
                                    dynamic comment;
                                    try
                                    {
                                        comment = result["comments"];
                                    }
                                    catch (Exception)
                                    {
                                        comment = null;
                                    }
                                    objFacebookFeed.FeedDescription = message;
                                    objFacebookFeed.EntryDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                                    //objFacebookFeed._facebookComment = FbPostComments(objFacebookFeed.FeedId, fbAcc.AccessToken);
                                    objFacebookFeed._facebookComment = FbPostComments(objFacebookFeed.FeedId, comment);


                                    try
                                    {
                                        MongoRepository mongorepo = new MongoRepository("MongoFacebookFeed");
                                        var ret = mongorepo.Find<MongoFacebookFeed>(t => t.FeedId == objFacebookFeed.FeedId && t.ProfileId == objFacebookFeed.ProfileId);
                                        var task = Task.Run(async () =>
                                          {
                                              return await ret;

                                          });
                                        int count = task.Result.Count;
                                        if (count < 1)
                                        {
                                            mongorepo.Add<MongoFacebookFeed>(objFacebookFeed);
                                        }
                                        else
                                        {
                                            try
                                            {
                                                FilterDefinition<BsonDocument> filter = new BsonDocument("FeedId", objFacebookFeed.FeedId);
                                                var update = Builders<BsonDocument>.Update.Set("postType", objFacebookFeed.postType).Set("postingFrom", objFacebookFeed.postingFrom).Set("Likecount", objFacebookFeed.Likecount).Set("Commentcount", objFacebookFeed.Commentcount).Set("_facebookComment", objFacebookFeed._facebookComment);
                                                mongorepo.Update<MongoFacebookFeed>(update, filter);
                                            }
                                            catch { }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        //_logger.LogInformation(ex.Message);
                                        //_logger.LogError(ex.StackTrace);
                                    }
                                    if (apiHitsCount < MaxapiHitsCount)
                                    {
                                        //AddFbPostComments(objFacebookFeed.FeedId, fbAcc.AccessToken);
                                    }

                                }

                            }
                            else
                            {
                                apiHitsCount = MaxapiHitsCount;
                            }
                            try
                            {
                                feeds = Socioboard.Facebook.Data.FbUser.fbGet(fbAcc.AccessToken, feeds["paging"]["next"]);
                            }
                            catch
                            {
                                apiHitsCount = MaxapiHitsCount;
                            }

                        }

                        fbAcc.LastUpdate = DateTime.UtcNow;

                        databaseRepository.Update(fbAcc);
                    }
                }
            }
            else
            {
                apiHitsCount = 0;
            }
            return 0;
        }

        //todo : need to write codes to get all comments 
        public static string AddFbPostComments(string postid, string AccessToken)
        {
            MongoFbPostComment fbPostComment = new MongoFbPostComment();
            string ret = string.Empty;
            try
            {

                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
                apiHitsCount++;
                //while (apiHitsCount < MaxapiHitsCount )
                //{

                //}
                dynamic post = Socioboard.Facebook.Data.FbUser.GetPostComments(AccessToken, postid);

                foreach (var item in post["data"])
                {
                    fbPostComment.Id = MongoDB.Bson.ObjectId.GenerateNewId();
                    fbPostComment.EntryDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                    fbPostComment.PostId = postid;

                    try
                    {
                        fbPostComment.CommentId = item["id"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.Comment = item["message"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.Likes = Convert.ToInt32(item["like_count"]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.UserLikes = Convert.ToInt32(item["user_likes"]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.Commentdate = DateTime.Parse(item["created_time"].ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    catch (Exception ex)
                    {
                        fbPostComment.Commentdate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    try
                    {
                        fbPostComment.FromName = item["from"]["name"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.FromId = item["from"]["id"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.PictureUrl = item["id"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }

                    try
                    {

                        MongoRepository fbPostRepo = new MongoRepository("MongoFbPostComment");
                        var retur = fbPostRepo.Find<MongoFbPostComment>(t => t.CommentId == fbPostComment.CommentId && t.PostId == fbPostComment.PostId);
                        var task = Task.Run(async () =>
                        {
                            return await retur;

                        });
                        int count = task.Result.Count;
                        if (count < 1)
                        {
                            fbPostRepo.Add<MongoFbPostComment>(fbPostComment);
                        }
                    }
                    catch (Exception ex)
                    {
                        //_logger.LogInformation(ex.Message);
                        //_logger.LogError(ex.StackTrace);
                    }
                    try
                    {
                        //   AddFbPagePostCommentsLikes(objFbPageComment.CommentId, accesstoken, userid);
                    }
                    catch (Exception ex)
                    {
                        //_logger.LogInformation(ex.Message);
                        //_logger.LogError(ex.StackTrace);
                    }

                }

            }
            catch (Exception ex)
            {
                //_logger.LogInformation(ex.Message);
                //_logger.LogError(ex.StackTrace);
            }
            return ret;
        }

        //public static List<MongoFbPostComment> FbPostComments(string postid, string AccessToken)
        //{
        //    apiHitsCount++;
        //    List<MongoFbPostComment> lstFbPOstComment = new List<MongoFbPostComment>();
        //    string ret = string.Empty;
        //    try
        //    {

        //        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
        //        dynamic post = FbUser.getPostComments(AccessToken, postid);

        //        foreach (var item in post["data"])
        //        {
        //            MongoFbPostComment fbPostComment = new MongoFbPostComment();
        //            fbPostComment.Id = MongoDB.Bson.ObjectId.GenerateNewId();
        //            fbPostComment.EntryDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
        //            fbPostComment.PostId = postid;

        //            try
        //            {
        //                fbPostComment.CommentId = item["id"];
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.StackTrace);
        //            }
        //            try
        //            {
        //                fbPostComment.Comment = item["message"];
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.StackTrace);
        //            }
        //            try
        //            {
        //                fbPostComment.Likes = Convert.ToInt32(item["like_count"]);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.StackTrace);
        //            }
        //            try
        //            {
        //                fbPostComment.UserLikes = Convert.ToInt32(item["user_likes"]);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.StackTrace);
        //            }
        //            try
        //            {
        //                fbPostComment.Commentdate = DateTime.Parse(item["created_time"].ToString()).ToString("yyyy/MM/dd HH:mm:ss");
        //            }
        //            catch (Exception ex)
        //            {
        //                fbPostComment.Commentdate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
        //            }
        //            try
        //            {
        //                fbPostComment.FromName = item["from"]["name"];
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.StackTrace);
        //            }
        //            try
        //            {
        //                fbPostComment.FromId = item["from"]["id"];
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.StackTrace);
        //            }
        //            try
        //            {
        //                fbPostComment.PictureUrl = item["id"];
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.StackTrace);
        //            }

        //            try
        //            {
        //                lstFbPOstComment.Add(fbPostComment);

        //                //MongoRepository fbPostRepo = new MongoRepository("MongoFbPostComment", settings);
        //                //fbPostRepo.Add<MongoFbPostComment>(fbPostComment);
        //            }
        //            catch (Exception ex)
        //            {


        //                new List<MongoFbPostComment>();

        //            }
        //            try
        //            {
        //                //   AddFbPagePostCommentsLikes(objFbPageComment.CommentId, accesstoken, userid);
        //            }
        //            catch (Exception ex)
        //            {


        //            }

        //        }

        //    }
        //    catch (Exception ex)
        //    {


        //    }
        //    return lstFbPOstComment;
        //}
        public static List<MongoFbPostComment> FbPostComments(string postid,dynamic post)
        {
            apiHitsCount++;
            List<MongoFbPostComment> lstFbPOstComment = new List<MongoFbPostComment>();
            string ret = string.Empty;
            try
            {

                //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
                //dynamic post = FbUser.getPostComments(AccessToken, postid);

                foreach (var item in post["data"])
                {
                    MongoFbPostComment fbPostComment = new MongoFbPostComment();
                    fbPostComment.Id = MongoDB.Bson.ObjectId.GenerateNewId();
                    fbPostComment.EntryDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                    fbPostComment.PostId = postid;

                    try
                    {
                        fbPostComment.CommentId = item["id"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.Comment = item["message"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.Likes = Convert.ToInt32(item["like_count"]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.UserLikes = Convert.ToInt32(item["user_likes"]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.Commentdate = DateTime.Parse(item["created_time"].ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    catch (Exception ex)
                    {
                        fbPostComment.Commentdate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    try
                    {
                        fbPostComment.FromName = item["from"]["name"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.FromId = item["from"]["id"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                    try
                    {
                        fbPostComment.PictureUrl = item["id"];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }

                    try
                    {
                        lstFbPOstComment.Add(fbPostComment);
                    }
                    catch (Exception ex)
                    {
                        new List<MongoFbPostComment>();

                    }
                    try
                    {
                    }
                    catch (Exception ex)
                    {


                    }

                }

            }
            catch (Exception ex)
            {


            }
            return lstFbPOstComment;
        }
    }
}
