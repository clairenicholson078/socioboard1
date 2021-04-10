﻿using Domain.Socioboard.Models.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Socioboard.GoogleLib.Authentication;
using Socioboard.GoogleLib.Youtube.Core;
using SocioboardDataServices.Helper;
using SocioboardDataServices.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SocioboardDataServices.Youtube
{
    public class YtFeeds
    {
        static List<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> _lstGlobalComVideos = new List<MongoYoutubeComments>();
        public static void GetYtFeeds(string ChannelId, string AcessToken)
        {
            oAuthTokenYoutube ObjoAuthTokenGPlus = new oAuthTokenYoutube(AppSettings.googleClientId, AppSettings.googleClientSecret, AppSettings.googleRedirectionUrl);
            Activities _ObjYtActivities = new Activities(AppSettings.googleClientId, AppSettings.googleClientSecret, AppSettings.googleRedirectionUrl);
            string apiKey = AppSettings.googleApiKey_TestApp;

            try
            {

                MongoYoutubeFeeds _ObjMongoYtFeeds;
                string _ChannelVideos = _ObjYtActivities.GetYtVideos(ChannelId, apiKey);
                JObject J_ChannelVideos = JObject.Parse(_ChannelVideos);
                foreach (var item in J_ChannelVideos["items"])
                {
                    _lstGlobalComVideos.Clear();
                    _ObjMongoYtFeeds = new MongoYoutubeFeeds();
                    _ObjMongoYtFeeds.Id = ObjectId.GenerateNewId();
                    _ObjMongoYtFeeds.YtChannelId = ChannelId;
                    try
                    {
                        _ObjMongoYtFeeds.YtVideoId = item["contentDetails"]["upload"]["videoId"].ToString();
                    }
                    catch
                    {

                    }
                    try
                    {
                        _ObjMongoYtFeeds.VdoTitle = item["snippet"]["title"].ToString();
                    }
                    catch
                    {

                    }
                    try
                    {
                        _ObjMongoYtFeeds.VdoDescription = item["snippet"]["description"].ToString();
                        if (_ObjMongoYtFeeds.VdoDescription == "")
                        {
                            _ObjMongoYtFeeds.VdoDescription = "No Description";
                        }
                    }
                    catch
                    {

                    }

                    try
                    {
                        _ObjMongoYtFeeds.VdoPublishDate = item["snippet"]["publishedAt"].ToString();
                    }
                    catch
                    {

                    }
                    try
                    {
                        _ObjMongoYtFeeds.VdoImage = item["snippet"]["thumbnails"]["high"]["url"].ToString();
                    }
                    catch
                    {
                        _ObjMongoYtFeeds.VdoImage = item["snippet"]["thumbnails"]["medium"]["url"].ToString();
                    }
                    try
                    {
                        _ObjMongoYtFeeds.VdoUrl = "https://www.youtube.com/watch?v=" + _ObjMongoYtFeeds.YtVideoId;
                    }
                    catch
                    {

                    }
                    try
                    {
                        _ObjMongoYtFeeds.VdoEmbed = "https://www.youtube.com/embed/" + _ObjMongoYtFeeds.YtVideoId;
                    }
                    catch
                    {

                    }
                    try
                    {
                        MongoRepository YtFeedsRepo = new MongoRepository("YoutubeVideos");
                        var ret = YtFeedsRepo.Find<MongoYoutubeFeeds>(t => t.YtVideoId.Equals(_ObjMongoYtFeeds.YtVideoId));
                        var task = Task.Run(async () =>
                        {
                            return await ret;
                        });
                        int count = task.Result.Count;
                        if (count < 1)
                        {
                            try
                            {
                                YtFeedsRepo.Add(_ObjMongoYtFeeds);
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                FilterDefinition<BsonDocument> filter = new BsonDocument("YtVideoId", _ObjMongoYtFeeds.YtVideoId);
                                var update = Builders<BsonDocument>.Update.Set("VdoTitle", _ObjMongoYtFeeds.VdoTitle).Set("VdoDescription", _ObjMongoYtFeeds.VdoDescription).Set("VdoImage", _ObjMongoYtFeeds.VdoImage);
                                YtFeedsRepo.Update<MongoYoutubeFeeds>(update, filter);
                            }
                            catch { }
                        }

                        //new Thread(delegate () {
                        GetYtComments(_ObjMongoYtFeeds.YtVideoId, apiKey, ChannelId);
                        UpdateYtComments(_ObjMongoYtFeeds.YtVideoId, apiKey, ChannelId);
                        //}).Start();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
            }
        }


        public static void GetYtComments(string VideoId, string apiKey, string ChannelId)
        {
            Video _Videos = new Video(AppSettings.googleClientId, AppSettings.googleClientSecret, AppSettings.googleRedirectionUrl);

            try
            {
                //Domain.Socioboard.Domain.GooglePlusActivities _GooglePlusActivities = null;


                MongoYoutubeComments _ObjMongoYtComments;
                string _CommentsData = _Videos.Get_CommentsBy_VideoId(VideoId, "", "", apiKey);
                JObject J_CommentsData = JObject.Parse(_CommentsData);
                foreach (var item in J_CommentsData["items"])
                {
                    _ObjMongoYtComments = new MongoYoutubeComments();
                    _ObjMongoYtComments.Id = ObjectId.GenerateNewId();

                    try
                    {
                        _ObjMongoYtComments.ChannelId = ChannelId;
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.videoId = item["snippet"]["videoId"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.commentId = item["id"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.authorDisplayName = item["snippet"]["topLevelComment"]["snippet"]["authorDisplayName"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.authorProfileImageUrl = item["snippet"]["topLevelComment"]["snippet"]["authorProfileImageUrl"].ToString().Replace(".jpg", "");
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.authorChannelUrl = item["snippet"]["topLevelComment"]["snippet"]["authorChannelUrl"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.authorChannelId = item["snippet"]["topLevelComment"]["snippet"]["authorChannelId"]["value"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.commentDisplay = item["snippet"]["topLevelComment"]["snippet"]["textDisplay"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.commentOriginal = item["snippet"]["topLevelComment"]["snippet"]["textOriginal"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.viewerRating = item["snippet"]["topLevelComment"]["snippet"]["viewerRating"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.likesCount = item["snippet"]["topLevelComment"]["snippet"]["likeCount"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.publishTime = item["snippet"]["topLevelComment"]["snippet"]["publishedAt"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.publishTimeUnix = UnixTimeFromDatetime(Convert.ToDateTime(_ObjMongoYtComments.publishTime));
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.updatedTime = item["snippet"]["topLevelComment"]["snippet"]["updatedAt"].ToString();
                    }
                    catch { }
                    try
                    {
                        _ObjMongoYtComments.totalReplyCount = item["snippet"]["totalReplyCount"].ToString();
                    }
                    catch { }
                    _ObjMongoYtComments.active = true;
                    _ObjMongoYtComments.review = false;
                    _ObjMongoYtComments.sbGrpTaskAssign = false;

                    try
                    {
                        _lstGlobalComVideos.Add(_ObjMongoYtComments);//Global var for update the comments
                        MongoRepository youtubecommentsrepo = new MongoRepository("YoutubeVideosComments");
                        var ret = youtubecommentsrepo.Find<MongoYoutubeComments>(t => t.commentId.Equals(_ObjMongoYtComments.commentId));
                        var task = Task.Run(async () =>
                        {
                            return await ret;
                        });
                        int count = task.Result.Count;
                        if (count < 1)
                        {
                            try
                            {
                                youtubecommentsrepo.Add(_ObjMongoYtComments);
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                FilterDefinition<BsonDocument> filter = new BsonDocument("commentId", _ObjMongoYtComments.commentId);
                                var update = Builders<BsonDocument>.Update.Set("authorDisplayName", _ObjMongoYtComments.authorDisplayName).Set("authorProfileImageUrl", _ObjMongoYtComments.authorProfileImageUrl).Set("commentDisplay", _ObjMongoYtComments.commentDisplay).Set("commentOriginal", _ObjMongoYtComments.commentOriginal).Set("viewerRating", _ObjMongoYtComments.viewerRating).Set("likesCount", _ObjMongoYtComments.likesCount).Set("totalReplyCount", _ObjMongoYtComments.totalReplyCount).Set("updatedTime", _ObjMongoYtComments.updatedTime).Set("publishTimeUnix", _ObjMongoYtComments.publishTimeUnix).Set("active", _ObjMongoYtComments.active);
                                youtubecommentsrepo.Update<MongoYoutubeComments>(update, filter);
                            }
                            catch { }
                        }

                    }
                    catch { }

                }
            }
            catch (Exception ex)
            {
            }
        }

        public static long UnixTimeFromDatetime(DateTime input)
        {
            var timeSpan = (input - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        public static void UpdateYtComments(string VideoId, string apiKey, string ChannelId)
        {
            int c = 0;
            MongoRepository mongoreposs = new MongoRepository("YoutubeVideosComments");
            var result = mongoreposs.Find<Domain.Socioboard.Models.Mongo.MongoYoutubeComments>(t => t.videoId == VideoId);
            var task = Task.Run(async () =>
            {
                return await result;
            });
            IList<Domain.Socioboard.Models.Mongo.MongoYoutubeComments> lstAllComment = task.Result;

            List<string> idMongo = lstAllComment.Select(t => t.commentId).ToList();
            List<string> idLatest = _lstGlobalComVideos.Select(t => t.commentId).ToList();
            Console.WriteLine("Filter the deleted Elements");
            foreach (string item in idMongo)
            {
                if (!(idLatest.Contains(item)))
                {
                    Console.WriteLine("===============");
                    MongoRepository _mongoRepo = new MongoRepository("YoutubeVideosComments");
                    var temp = lstAllComment.Single(t => t.commentId == item);
                    temp.active = false;
                    FilterDefinition<BsonDocument> filter = new BsonDocument("commentId", temp.commentId);
                    var update = Builders<BsonDocument>.Update.Set("active", temp.active);
                    _mongoRepo.Update<MongoYoutubeComments>(update, filter);
                    Console.WriteLine(c++);
                }
                Console.Write(".");
            }
        }
    }
}