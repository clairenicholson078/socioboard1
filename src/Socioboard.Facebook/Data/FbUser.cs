﻿using Facebook;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Socioboard.Facebook.Utils;

namespace Socioboard.Facebook.Data
{
    public static class FbUser
    {
        public static object getFbUser(string accessToken)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/me?fields=id,about,birthday,cover,education,email,gender,hometown,name,first_name,last_name,work,picture");//v2.6
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }

        public static Int64 getFbFriends(string accessToken)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            dynamic friends = fb.Get($"{FbConstants.FacebookApiVersion}/me/friends");//v2.1
            try
            {
                return Convert.ToInt64(friends["summary"]["total_count"].ToString());
            }
            catch (Exception ex)
            {
                return 0;
            }            
        }

        public static dynamic getFeeds(string accessToken)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                var details = fb.Get($"{FbConstants.FacebookApiVersion}/me/feed?limit=99&fields=picture,created_time,message,description,story,from,likes.summary(true),comments.summary(true),type,application");//v2.1
                return details;
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }


        public static dynamic getFeeds(string accessToken, string facebookid)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/" + facebookid + "/posts?limit=99");//v2.1
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }

        public static dynamic getFeedDetail(string accessToken, string PostId)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/" + PostId + "?fields=likes.summary(true),comments.summary(true),shares");//v2.1
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }
        public static dynamic postdetails(string accessToken, string PostId)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/" + PostId + "/insights");//v2.1
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }

        public static dynamic conversations(string accessToken)
        {

            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/me/conversations");//v2.1
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }

        public static dynamic notifications(string accessToken)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/me/notifications");//v2.1
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }

        public static dynamic GetPostComments(string accessToken, string postid)
        {
            var fb = new FacebookClient {AccessToken = accessToken};
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/" + postid + "/comments?limit=99");//v2.1
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "Invalid Access Token";
            }
        }


        public static string PostComments(string accessToken, string postid, string message)
        {
            var args = new Dictionary<string, object> {["message"] = message};
            var fb = new FacebookClient {AccessToken = accessToken};
           
            try
            {
                return fb.Post($"{FbConstants.FacebookApiVersion}/" + postid + "/comments", args).ToString();//v2.1
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "Invalid Access Token";
            }
        }

        public static dynamic fbGet(string accessToken, string Url)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get(Url);
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }

        public static dynamic getPageTaggedPostDetails(string accessToken)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/me/tagged?fields=picture,created_time,message,description,from&limit=99");//v2.6
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }

        public static dynamic getPromotablePostsDetails(string accessToken)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            try
            {
                return fb.Get($"{FbConstants.FacebookApiVersion}/me/promotable_posts?fields=picture,created_time,message,description,from&limit=99");//v2.6
            }
            catch (Exception ex)
            {
                return "Invalid Access Token";
            }
        }

        public static string SetPrivacy(string privacy, FacebookClient fb, string fbUserId)
        {
            try
            {
                JObject Jdata = null;
                string JValue = string.Empty;

                if (!string.IsNullOrEmpty(privacy))
                {
                    if (privacy == "Close Friends")
                    {
                        Jdata = JObject.Parse(fb.Get("/" + fbUserId + "/friendlists/close_friends").ToString());
                        string closefrndid = Jdata["data"][0]["id"].ToString();
                        JValue = "{ \"description\": \"Close Friends\",\"value\": \"CUSTOM\",\"friends\": \"SOME_FRIENDS\",\"networks\": \"\",\"allow\":\"" + closefrndid + "\",\"deny\": \"\"}";
                    }
                    else if (privacy == "Only Me")
                    {
                        JValue = "{\"description\": \"Only Me\",\"value\": \"SELF\",\"friends\": \"\",\"networks\": \"\",\"allow\": \"\",\"deny\": \"\"}";
                    }
                    else if (privacy == "Friends")
                    {
                        JValue = "{\"description\": \"Your friends\",\"value\": \"ALL_FRIENDS\",\"friends\": \"\",\"networks\": \"\",\"allow\": \"\",\"deny\": \"\"}";
                    }
                    else if (privacy == "Friends of Friends")
                    {
                        JValue = "{\"description\": \"Your friends of friends\",\"value\": \"FRIENDS_OF_FRIENDS\",\"friends\": \"\",\"networks\": \"\",\"allow\": \"\",\"deny\": \"\"}";
                    }
                    else if (privacy == "Family")
                    {
                        Jdata = JObject.Parse(fb.Get("/" + fbUserId + "/friendlists/family").ToString());
                        string familyid = Jdata["data"][0]["id"].ToString();
                        JValue = "{\"description\": \"Family\",\"value\": \"CUSTOM\",\"friends\": \"SOME_FRIENDS\",\"networks\": \"\",\"allow\": \"" + familyid + "\",\"deny\": \"\"}";
                    }
                    else if (privacy == "Public")
                    {
                        JValue = "{\"description\": \"Public\",\"value\": \"EVERYONE\",\"friends\": \"\",\"networks\": \"\",\"allow\": \"\",\"deny\": \"\"}";
                    }
                    else if (privacy == "Acquaintances")
                    {
                        Jdata = JObject.Parse(fb.Get("/" + fbUserId + "/friendlists/acquaintances").ToString());
                        string AcquaintancesId = Jdata["data"][0]["id"].ToString();
                        JValue = "{\"description\": \"Acquaintances\",\"value\": \"CUSTOM\",\"friends\": \"SOME_FRIENDS\",\"networks\": \"\",\"allow\": \"" + AcquaintancesId + "\",\"deny\": \"\"}";
                    }
                    return JValue;
                }
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }


    }
}
