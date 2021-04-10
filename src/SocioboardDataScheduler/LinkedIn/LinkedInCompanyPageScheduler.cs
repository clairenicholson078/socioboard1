﻿using Domain.Socioboard.Models;
using Socioboard.LinkedIn.Authentication;
using Socioboard.LinkedIn.LinkedIn.Core.CompanyMethods;
using SocioboardDataScheduler.Model;
using System;
using System.Net.Mail;
using System.Net.Mime;
using SocioboardDataScheduler.Helper;

namespace SocioboardDataScheduler.LinkedIn
{
    public class LinkedInCompanyPageScheduler
    {
        public static int apiHitsCount = 0;
        public static int MaxapiHitsCount = 25;
        public static void PostLinkedInCompanyPageMessage(Domain.Socioboard.Models.ScheduledMessage schmessage, Domain.Socioboard.Models.LinkedinCompanyPage _LinkedinCompanyPage, Domain.Socioboard.Models.User _user)
        {
            try
            {
                DatabaseRepository dbr = new DatabaseRepository();
               
                //if (_LinkedinCompanyPage.SchedulerUpdate.AddHours(1) <= DateTime.UtcNow)
                //{
                    if (_LinkedinCompanyPage != null)
                    {
                        if (_LinkedinCompanyPage.IsActive)
                        {
                            //if (apiHitsCount < MaxapiHitsCount)
                            //{
                                if (schmessage.scheduleTime <= DateTime.UtcNow)
                                {
                                    string linkedindata = ComposeLinkedInCompanyPagePost(schmessage.url, schmessage.userId, schmessage.shareMessage, _LinkedinCompanyPage.LinkedinPageId, dbr, _LinkedinCompanyPage, schmessage, _user);
                                    if (!string.IsNullOrEmpty(linkedindata))
                                    {
                                        apiHitsCount++;
                                    }
                                }
                            //}
                            //else
                            //{
                            //    apiHitsCount = 0;
                            //}
                           
                        }
                        else
                        {
                            apiHitsCount = 0;
                        }
                    }
                //}
                //else
                //{
                //    apiHitsCount = 0;
                //}
            }
            catch (Exception exs)
            {
                apiHitsCount = MaxapiHitsCount;
            }
        }

        public static string ComposeLinkedInCompanyPagePost(string ImageUrl, long userid, string comment, string LinkedinPageId, Model.DatabaseRepository dbr,Domain.Socioboard.Models.LinkedinCompanyPage objLinkedinCompanyPage, Domain.Socioboard.Models.ScheduledMessage schmessage, Domain.Socioboard.Models.User _user)
        {
            string json = "";
            Domain.Socioboard.Models.LinkedinCompanyPage objlicompanypage = objLinkedinCompanyPage;
            oAuthLinkedIn Linkedin_oauth = new oAuthLinkedIn();
            Linkedin_oauth.ConsumerKey = AppSettings.LinkedinConsumerKey;
            Linkedin_oauth.ConsumerSecret = AppSettings.LinkedinConsumerSecret;
            Linkedin_oauth.Verifier = objlicompanypage.OAuthVerifier;
            Linkedin_oauth.TokenSecret = objlicompanypage.OAuthSecret;
            Linkedin_oauth.Token = objlicompanypage.OAuthToken;
            Linkedin_oauth.Id = objlicompanypage.LinkedinPageId;
            Linkedin_oauth.FirstName = objlicompanypage.LinkedinPageName;
            Company company = new Company();
           
            if (string.IsNullOrEmpty(ImageUrl))
            {
                json = company.SetPostOnPage(Linkedin_oauth, objlicompanypage.LinkedinPageId, comment);
            }
            else
            {
                //var client = new ImgurClient("5f1ad42ec5988b7", "f3294c8632ef8de6bfcbc46b37a23d18479159c5");
                //var endpoint = new ImageEndpoint(client);
                //IImage image;
                //using (var fs = new FileStream(ImageUrl, FileMode.Open))
                //{
                //    image = endpoint.UploadImageStreamAsync(fs).GetAwaiter().GetResult();
                //}

                //var imgs = image.Link;
                json = company.SetPostOnPageWithImage(Linkedin_oauth, objlicompanypage.LinkedinPageId, ImageUrl, comment);
            }
            if (!string.IsNullOrEmpty(json))
            {
                apiHitsCount++;
                schmessage.status = Domain.Socioboard.Enum.ScheduleStatus.Compleated;
                //schmessage.url = json;
                dbr.Update<ScheduledMessage>(schmessage);
                Domain.Socioboard.Models.Notifications notify = new Notifications();
                Notifications lstnotifications = dbr.Single<Notifications>(t => t.MsgId == schmessage.id);
                if(lstnotifications==null)
                {
                    notify.MsgId = schmessage.id;
                    notify.MsgStatus = "Scheduled";
                    notify.notificationtime = schmessage.localscheduletime;
                    notify.NotificationType = "Schedule Successfully";
                    notify.ReadOrUnread = "Unread";
                    notify.UserId = userid;
                    dbr.Add<Notifications>(notify);
                    if (_user.scheduleSuccessUpdates)
                    {
                        string sucResponse = SendMailbySendGrid(AppSettings.from_mail, "", _user.EmailId, "", "", "", "", _user.FirstName, schmessage.localscheduletime, true, AppSettings.sendGridUserName, AppSettings.sendGridPassword);
                    }
                    return "posted";
                }
                else
                {
                    //if (_user.scheduleSuccessUpdates)
                    //{
                    //    string sucResponse = SendMailbySendGrid(AppSettings.from_mail, "", _user.EmailId, "", "", "", "", _user.FirstName, schmessage.localscheduletime, true, AppSettings.sendGridUserName, AppSettings.sendGridPassword);
                    //}
                    return "posted";
                }
                
            }
            else
            {
                apiHitsCount = MaxapiHitsCount;
                json = "Message not posted";
                Domain.Socioboard.Models.Notifications notify = new Notifications();
                Notifications lstnotifications = dbr.Single<Notifications>(t => t.MsgId == schmessage.id);
                if (lstnotifications == null)
                {
                    notify.MsgId = schmessage.id;
                    notify.MsgStatus = "Failed";
                    notify.notificationtime = schmessage.localscheduletime;
                    notify.NotificationType = "Schedule Failed";
                    notify.ReadOrUnread = "Unread";
                    notify.UserId = userid;
                    dbr.Add<Notifications>(notify);
                    if (_user.scheduleFailureUpdates)
                    {
                        string falResponse = SendMailbySendGrid(AppSettings.from_mail, "", _user.EmailId, "", "", "", "", _user.FirstName, schmessage.localscheduletime, false, AppSettings.sendGridUserName, AppSettings.sendGridPassword);
                    }
                    return json;
                }
                else
                {
                    //if (_user.scheduleFailureUpdates)
                    //{
                    //    string falResponse = SendMailbySendGrid(AppSettings.from_mail, "", _user.EmailId, "", "", "", "", _user.FirstName, schmessage.localscheduletime, false, AppSettings.sendGridUserName, AppSettings.sendGridPassword);
                    //}
                    return json;
                }
              
            }
        }

        public static string SendMailbySendGrid(string from, string passsword, string to, string bcc, string cc, string subject, string body, string Name, string time, bool schStatus, string UserName, string Password)
        {
            subject = "Socioboard updates";
            string datetime = time;
            if (schStatus)
            {
                body = "<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, minimum-scale=1.0, maximum-scale=1.0\"><title>Socioboard | Christmas Offer</title><link rel=\"icon\" type=\"image/png\" sizes=\"192x192\"href=\"../../images/favicon/favicon.ico\"><style type=\"text/css\">html {width: 100%}::-moz-selection {background: #eb5600;color: #fff}::selection {background: #eb5600;color: #fff}body {background-color: #f8f8f8;margin: 0;padding: 0}.ReadMsgBody {width: 100%;background-color: #f8f8f8}.ExternalClass {width: 100%;background-color: #f8f8f8}a {color: #eb5600;text-decoration: none;font-weight: 400;font-style: normal}a:hover {color: #aaa;text-decoration: underline;font-weight: 400;font-style: normal}a.heading-link {text-decoration: none;font-weight: 400;font-style: normal}a.heading-link:hover {text-decoration: none;font-weight: 400;font-style: normal}p,div {margin: 0!important}table {border-collapse: collapse}@media only screen and (max-width: 640px) {table table {width: 100%!important}td[class=full_width] {width: 100%!important}div[class=div_scale] {width: 440px!important;margin: 0 auto!important}table[class=table_scale] {width: 440px!important;margin: 0 auto!important}td[class=td_scale] {width: 440px!important;margin: 0 auto!important}img[class=img_scale] {width: 100%!important;height: auto!important}img[class=divider] {width: 440px!important;height: 2px!important}table[class=spacer] {display: none!important}td[class=spacer] {display: none!important}td[class=center] {text-align: center!important}table[class=full] {width: 400px!important;margin-left: 20px!important;margin-right: 20px!important}img[class=divider] {width: 100%!important;height: 1px!important}}@media only screen and (max-width: 479px) {table table {width: 100%!important}td[class=full_width] {width: 100%!important}div[class=div_scale] {width: 280px!important;margin: 0 auto!important}table[class=table_scale] {width: 280px!important;margin: 0 auto!important}td[class=td_scale] {width: 280px!important;margin: 0 auto!important}img[class=img_scale] {width: 100%!important;height: auto!important}img[class=divider] {width: 280px!important;height: 2px!important}table[class=spacer] {display: none!important}td[class=spacer] {display: none!important}td[class=center] {text-align: center!important}table[class=full] {width: 240px!important;margin-left: 20px!important;margin-right: 20px!important}img[class=divider] {width: 100%!important;height: 1px!important}}</style><style type=\"text/css\"></style></head><body bgcolor=\"#f8f8f8\"> <!-- START OF HEADER BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin-top: 20px;\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><!-- START OF VERTICAL SPACER--><table bgcolor=\"#ffffff\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--><table class=\"table_scale\" width=\"600\" bgcolor=\"#ffffff\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td class=\"spacer\" width=\"30\"></td><td width=\"540\"><!-- START OF LOGO IMAGE TABLE--><table class=\"full\" align=\"left\" width=\"255\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"left\" style=\"padding: 0px; text-transform: uppercase; font-family: Lucida Sans Unicode; color:#666666; font-size:24px; line-height:34px;\"> <span> <a href=\"#\" style=\"color:#eb5600;\"> <img src=\"http://i.imgur.com/sC2o03M.png\" alt=\"logo\" width=\"250\" height=\"55\" border=\"0\" style=\"display: inline;\"> </a> </span> </td></tr></tbody></table><!-- END OF LOGO IMAGE TABLE--><!-- START OF SPACER--><table width=\"25\" align=\"left\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td width=\"100%\" height=\"30\"></td></tr></tbody></table><!-- END OF SPACER--><!-- START OF CONTACT TABLE--><table class=\"full\" align=\"right\" width=\"255\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"right\" style=\"margin: 0; padding-top: 10px; text-transform: uppercase; font-size: 10px; color:#666666; font-family: Lucida Sans Unicode; line-height: 30px;mso-line-height-rule: exactly;\"> <span> call us: +91 74 0631 7771, +91 80 4166 0003</span> </td></tr></tbody></table><!-- END OF CONTACT TABLE--></td><td class=\"spacer\" width=\"30\"></td></tr></tbody></table></td></tr></tbody></table><!-- START OF VERTICAL SPACER--><table bgcolor=\"#ffffff\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--></td></tr></tbody></table></td></tr></tbody></table><!-- END OF HEADER BLOCK--><!-- START OF FEATURED AREA BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table class=\"table_scale\" width=\"600\" bgcolor=\"#eb5600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table width=\"600\" bgcolor=\"#eb5600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"600\" valign=\"middle\" style=\"padding: 0px;\"><table class=\"full\" align=\"center\" width=\"540\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr><!-- START OF HEADING--><tr><td class=\"center\" align=\"center\" style=\"margin: 0; padding-bottom:15px; margin:0; text-transform: uppercase; font-family: Lucida Sans Unicode; font-size: 32px; color: #ffffff; line-height: 42px;mso-line-height-rule: exactly;\"> <span> HI " + Name + ",</span></td></tr><!-- END OF HEADING--><!-- START OF Cong--><tr><td class=\"center\" align=\"center\" style=\"margin: 0; padding-bottom:15px; margin:0; text-transform: uppercase; font-family: Lucida Sans Unicode; font-size: 18px; color: #ffffff; line-height: 30px;mso-line-height-rule: exactly;\"> <span>Schedule Time - " + datetime + ", post was successfully posted !!</span></td></tr><!-- END oF Cong --><!-- START OF TEXT--><tr><td class=\"center\" align=\"center\" style=\"margin: 0; padding:10px; margin:0; font-size:13px ; color:#ffffff; font-family: Helvetica, Arial, sans-serif; line-height: 25px;mso-line-height-rule: exactly;\"> <span><br>Sharing is Caring! <br>Happy Socioboarding! <br>Thanks </span> </td></tr><!-- END OF TEXT--><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table></td></tr><!--[if gte mso 9]> </v:textbox> </v:rect> <![endif]--></tbody></table></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table><!-- END OF FEATURED AREA BLOCK--><!-- START OF SOCIAL BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><!-- START OF VERTICAL SPACER--><table bgcolor=\"#ededed\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--><table class=\"table_scale\" width=\"600\" bgcolor=\"#ededed\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td class=\"spacer\" width=\"30\"></td><td width=\"540\"><!-- START OF LEFT COLUMN FOR HEADING--><table class=\"full\" align=\"left\" width=\"255\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"left\" style=\"margin: 0;text-transform: uppercase; font-size: 20px; color:#666666; font-family: Lucida Sans Unicode; line-height: 30px;mso-line-height-rule: exactly;\"> <span> FOLLOW US ONLINE:</span> </td></tr></tbody></table><!-- END OF LEFT COLUMN FOR HEADING--><!-- START OF SPACER--><table width=\"25\" align=\"left\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td width=\"100%\" height=\"30\"></td></tr></tbody></table><!-- END OF SPACER--><!-- START OF RIGHT COLUMN FOR SOCIAL ICONS--><table class=\"full\" align=\"right\" width=\"300\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"right\" style=\"margin: 0; font-size:14px ; color:#aaaaaa; font-family: Helvetica, Arial, sans-serif; line-height: 100%;\"> <span> <a href=\"https://www.facebook.com/SocioBoard\" target=\"_blank\"><img src=\"http://i.imgur.com/mrfZQKf.png?1\" style=\"height: 70px;\"></a> &nbsp;<a href=\"https://twitter.com/Socioboard\" target=\"_blank\"><img src=\"http://i.imgur.com/gSA8HbM.png?1\" style=\"height: 70px;\"></a>&nbsp;<a href=\"https://plus.google.com/b/105412765098773776122/105412765098773776122/posts\" target=\"_blank\"><img src=\"http://i.imgur.com/EseCwni.png?1\" style=\"height: 70px;\"></a></span> </td></tr></tbody></table><!-- END OF RIGHT COLUMN FOR SOCIAL ICONS--></td><td class=\"spacer\" width=\"30\"></td></tr></tbody></table></td></tr></tbody></table><!-- START OF VERTICAL SPACER--><table bgcolor=\"#ededed\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--></td></tr></tbody></table></td></tr></tbody></table><!-- END OF SOCIAL BLOCK--><!-- START OF FOOTER BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><!-- START OF VERTICAL SPACER--><table bgcolor=\"#666666\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--><table class=\"table_scale\" width=\"600\" bgcolor=\"#666666\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"600\"><table class=\"full\" align=\"center\" width=\"540\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"center\" style=\"margin: 0;font-size:12px ; color:#ededed; font-family: Helvetica, Arial, sans-serif; line-height: 18px;\"> <span> We doubled our revenues from the leads acquired with the help of Socioboard. <a target=\"_blank\" href=\"#\" style=\"color:#FDAA7A;\"> - Mahmoudou Sidibe, US</a></span> </td></tr><tr><td class=\"center\" align=\"center\" style=\"margin: 0;font-size:12px ; color:#ededed; font-family: Helvetica, Arial, sans-serif; line-height: 18px;\"> <span> Makes life easy and less cumbersome with smart social inbox. Thanks Socioboard. <a target=\"_blank\" href=\"#\" style=\"color:#FDAA7A;\"> - Maya Ross, UK</a></span> </td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table><!-- START OF VERTICAL SPACER--><table bgcolor=\"#666666\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"20\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--></td></tr></tbody></table></td></tr></tbody></table><!-- END OF FOOTER BLOCK--><!-- START OF SUB-FOOTER BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><!-- START OF VERTICAL SPACER--><table bgcolor=\"#666666\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-top: 1px solid #767373;\"><tbody><tr><td width=\"100%\" height=\"20\">&nbsp;</td></tr></tbody></table><table bgcolor=\"#666666\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"20\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--><!-- START OF VERTICAL SPACER--><table bgcolor=\"#f8f8f8\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--></td></tr></tbody></table></td></tr></tbody></table><!-- END OF SUB-FOOTER BLOCK--><script>(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)})(window,document,'script','//www.google-analytics.com/analytics.js','ga');ga('create', 'UA-58515856-3', 'auto');ga('send', 'pageview');</script></body></html>";
            }
            else
            {
                body = "<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, minimum-scale=1.0, maximum-scale=1.0\"><title>Socioboard | Christmas Offer</title><link rel=\"icon\" type=\"image/png\" sizes=\"192x192\"href=\"../../images/favicon/favicon.ico\"><style type=\"text/css\">html {width: 100%}::-moz-selection {background: #eb5600;color: #fff}::selection {background: #eb5600;color: #fff}body {background-color: #f8f8f8;margin: 0;padding: 0}.ReadMsgBody {width: 100%;background-color: #f8f8f8}.ExternalClass {width: 100%;background-color: #f8f8f8}a {color: #eb5600;text-decoration: none;font-weight: 400;font-style: normal}a:hover {color: #aaa;text-decoration: underline;font-weight: 400;font-style: normal}a.heading-link {text-decoration: none;font-weight: 400;font-style: normal}a.heading-link:hover {text-decoration: none;font-weight: 400;font-style: normal}p,div {margin: 0!important}table {border-collapse: collapse}@media only screen and (max-width: 640px) {table table {width: 100%!important}td[class=full_width] {width: 100%!important}div[class=div_scale] {width: 440px!important;margin: 0 auto!important}table[class=table_scale] {width: 440px!important;margin: 0 auto!important}td[class=td_scale] {width: 440px!important;margin: 0 auto!important}img[class=img_scale] {width: 100%!important;height: auto!important}img[class=divider] {width: 440px!important;height: 2px!important}table[class=spacer] {display: none!important}td[class=spacer] {display: none!important}td[class=center] {text-align: center!important}table[class=full] {width: 400px!important;margin-left: 20px!important;margin-right: 20px!important}img[class=divider] {width: 100%!important;height: 1px!important}}@media only screen and (max-width: 479px) {table table {width: 100%!important}td[class=full_width] {width: 100%!important}div[class=div_scale] {width: 280px!important;margin: 0 auto!important}table[class=table_scale] {width: 280px!important;margin: 0 auto!important}td[class=td_scale] {width: 280px!important;margin: 0 auto!important}img[class=img_scale] {width: 100%!important;height: auto!important}img[class=divider] {width: 280px!important;height: 2px!important}table[class=spacer] {display: none!important}td[class=spacer] {display: none!important}td[class=center] {text-align: center!important}table[class=full] {width: 240px!important;margin-left: 20px!important;margin-right: 20px!important}img[class=divider] {width: 100%!important;height: 1px!important}}</style><style type=\"text/css\"></style></head><body bgcolor=\"#f8f8f8\"> <!-- START OF HEADER BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin-top: 20px;\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><!-- START OF VERTICAL SPACER--><table bgcolor=\"#ffffff\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--><table class=\"table_scale\" width=\"600\" bgcolor=\"#ffffff\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td class=\"spacer\" width=\"30\"></td><td width=\"540\"><!-- START OF LOGO IMAGE TABLE--><table class=\"full\" align=\"left\" width=\"255\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"left\" style=\"padding: 0px; text-transform: uppercase; font-family: Lucida Sans Unicode; color:#666666; font-size:24px; line-height:34px;\"> <span> <a href=\"#\" style=\"color:#eb5600;\"> <img src=\"http://i.imgur.com/sC2o03M.png\" alt=\"logo\" width=\"250\" height=\"55\" border=\"0\" style=\"display: inline;\"> </a> </span> </td></tr></tbody></table><!-- END OF LOGO IMAGE TABLE--><!-- START OF SPACER--><table width=\"25\" align=\"left\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td width=\"100%\" height=\"30\"></td></tr></tbody></table><!-- END OF SPACER--><!-- START OF CONTACT TABLE--><table class=\"full\" align=\"right\" width=\"255\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"right\" style=\"margin: 0; padding-top: 10px; text-transform: uppercase; font-size: 10px; color:#666666; font-family: Lucida Sans Unicode; line-height: 30px;mso-line-height-rule: exactly;\"> <span> call us: +91 74 0631 7771, +91 80 4166 0003</span> </td></tr></tbody></table><!-- END OF CONTACT TABLE--></td><td class=\"spacer\" width=\"30\"></td></tr></tbody></table></td></tr></tbody></table><!-- START OF VERTICAL SPACER--><table bgcolor=\"#ffffff\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--></td></tr></tbody></table></td></tr></tbody></table><!-- END OF HEADER BLOCK--><!-- START OF FEATURED AREA BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table class=\"table_scale\" width=\"600\" bgcolor=\"#eb5600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table width=\"600\" bgcolor=\"#eb5600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"600\" valign=\"middle\" style=\"padding: 0px;\"><table class=\"full\" align=\"center\" width=\"540\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr><!-- START OF HEADING--><tr><td class=\"center\" align=\"center\" style=\"margin: 0; padding-bottom:15px; margin:0; text-transform: uppercase; font-family: Lucida Sans Unicode; font-size: 32px; color: #ffffff; line-height: 42px;mso-line-height-rule: exactly;\"> <span> HI " + Name + ",</span></td></tr><!-- END OF HEADING--><!-- START OF Cong--><tr><td class=\"center\" align=\"center\" style=\"margin: 0; padding-bottom:15px; margin:0; text-transform: uppercase; font-family: Lucida Sans Unicode; font-size: 18px; color: #ffffff; line-height: 30px;mso-line-height-rule: exactly;\"> <span>Schedule Time - " + datetime + ", unable to post due to security issue !!</span></td></tr><!-- END oF Cong --><!-- START OF TEXT--><tr><td class=\"center\" align=\"center\" style=\"margin: 0; padding:10px; margin:0; font-size:13px ; color:#ffffff; font-family: Helvetica, Arial, sans-serif; line-height: 25px;mso-line-height-rule: exactly;\"> <span><br>Sharing is Caring! <br>Happy Socioboarding! <br>Thanks </span> </td></tr><!-- END OF TEXT--><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table></td></tr><!--[if gte mso 9]> </v:textbox> </v:rect> <![endif]--></tbody></table></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table><!-- END OF FEATURED AREA BLOCK--><!-- START OF SOCIAL BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><!-- START OF VERTICAL SPACER--><table bgcolor=\"#ededed\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--><table class=\"table_scale\" width=\"600\" bgcolor=\"#ededed\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td class=\"spacer\" width=\"30\"></td><td width=\"540\"><!-- START OF LEFT COLUMN FOR HEADING--><table class=\"full\" align=\"left\" width=\"255\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"left\" style=\"margin: 0;text-transform: uppercase; font-size: 20px; color:#666666; font-family: Lucida Sans Unicode; line-height: 30px;mso-line-height-rule: exactly;\"> <span> FOLLOW US ONLINE:</span> </td></tr></tbody></table><!-- END OF LEFT COLUMN FOR HEADING--><!-- START OF SPACER--><table width=\"25\" align=\"left\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td width=\"100%\" height=\"30\"></td></tr></tbody></table><!-- END OF SPACER--><!-- START OF RIGHT COLUMN FOR SOCIAL ICONS--><table class=\"full\" align=\"right\" width=\"300\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"right\" style=\"margin: 0; font-size:14px ; color:#aaaaaa; font-family: Helvetica, Arial, sans-serif; line-height: 100%;\"> <span> <a href=\"https://www.facebook.com/SocioBoard\" target=\"_blank\"><img src=\"http://i.imgur.com/mrfZQKf.png?1\" style=\"height: 70px;\"></a> &nbsp;<a href=\"https://twitter.com/Socioboard\" target=\"_blank\"><img src=\"http://i.imgur.com/gSA8HbM.png?1\" style=\"height: 70px;\"></a>&nbsp;<a href=\"https://plus.google.com/b/105412765098773776122/105412765098773776122/posts\" target=\"_blank\"><img src=\"http://i.imgur.com/EseCwni.png?1\" style=\"height: 70px;\"></a></span> </td></tr></tbody></table><!-- END OF RIGHT COLUMN FOR SOCIAL ICONS--></td><td class=\"spacer\" width=\"30\"></td></tr></tbody></table></td></tr></tbody></table><!-- START OF VERTICAL SPACER--><table bgcolor=\"#ededed\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--></td></tr></tbody></table></td></tr></tbody></table><!-- END OF SOCIAL BLOCK--><!-- START OF FOOTER BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><!-- START OF VERTICAL SPACER--><table bgcolor=\"#666666\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--><table class=\"table_scale\" width=\"600\" bgcolor=\"#666666\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"600\"><table class=\"full\" align=\"center\" width=\"540\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;\"><tbody><tr><td class=\"center\" align=\"center\" style=\"margin: 0;font-size:12px ; color:#ededed; font-family: Helvetica, Arial, sans-serif; line-height: 18px;\"> <span> We doubled our revenues from the leads acquired with the help of Socioboard. <a target=\"_blank\" href=\"#\" style=\"color:#FDAA7A;\"> - Mahmoudou Sidibe, US</a></span> </td></tr><tr><td class=\"center\" align=\"center\" style=\"margin: 0;font-size:12px ; color:#ededed; font-family: Helvetica, Arial, sans-serif; line-height: 18px;\"> <span> Makes life easy and less cumbersome with smart social inbox. Thanks Socioboard. <a target=\"_blank\" href=\"#\" style=\"color:#FDAA7A;\"> - Maya Ross, UK</a></span> </td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table><!-- START OF VERTICAL SPACER--><table bgcolor=\"#666666\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"20\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--></td></tr></tbody></table></td></tr></tbody></table><!-- END OF FOOTER BLOCK--><!-- START OF SUB-FOOTER BLOCK--><table align=\"center\" bgcolor=\"#f8f8f8\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td valign=\"top\" width=\"100%\"><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\"><!-- START OF VERTICAL SPACER--><table bgcolor=\"#666666\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-top: 1px solid #767373;\"><tbody><tr><td width=\"100%\" height=\"20\">&nbsp;</td></tr></tbody></table><table bgcolor=\"#666666\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"20\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--><!-- START OF VERTICAL SPACER--><table bgcolor=\"#f8f8f8\" class=\"table_scale\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tbody><tr><td width=\"100%\" height=\"30\">&nbsp;</td></tr></tbody></table><!-- END OF VERTICAL SPACER--></td></tr></tbody></table></td></tr></tbody></table><!-- END OF SUB-FOOTER BLOCK--><script>(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)})(window,document,'script','//www.google-analytics.com/analytics.js','ga');ga('create', 'UA-58515856-3', 'auto');ga('send', 'pageview');</script></body></html>";
            }

            try
            {

                try
                {
                    MailMessage mailMsg = new MailMessage();

                    // To
                    mailMsg.To.Add(new MailAddress(to));
                    if (!string.IsNullOrEmpty(cc))
                    {
                        mailMsg.CC.Add(new MailAddress(cc));
                    }
                    // From
                    mailMsg.From = new MailAddress(from);

                    // Subject and multipart/alternative Body
                    mailMsg.Subject = subject;
                    string html = @body;
                    mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));

                    // Init SmtpClient and send
                    SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                    System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(UserName, Password);
                    smtpClient.Credentials = credentials;

                    smtpClient.Send(mailMsg);
                    return "success";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return "Mail Not Send";
                }

                //string posturl = "https://api.sendgrid.com/api/mail.send.json";
                //string postdata = "api_user=" + UserName + "&api_key=" + Password + "&to=" + to + "&toname=" + to + "&subject=" + subject + "&text=" + body + "&from=" + from;
                //string ret = ApiSendGridHttp(posturl, postdata);
                //return ret;
                return "success";
            }
            catch (Exception ex)
            {
                return "Mail Not Send";
            }
        }
    }
}
