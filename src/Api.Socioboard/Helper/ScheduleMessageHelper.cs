﻿using Api.Socioboard.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Domain.Socioboard.Models;
using System.Globalization;
using Domain.Socioboard.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.Socioboard.Helper
{
    public class ScheduleMessageHelper
    {
        //DaywiseSchedule
        public static string ScheduleMessage(string profileId, string socialprofileName, string shareMessage, Domain.Socioboard.Enum.SocialProfileType profiletype, long userId, string link, string url, string picUrl, string scheduleTime, string localscheduletime, Domain.Socioboard.Enum.MediaType mediaType, AppSettings _AppSettings, Cache _redisCache, DatabaseRepository dbr, ILogger _logger)
        {


            ScheduledMessage scheduledMessage = new ScheduledMessage();
            //scheduledMessage.calendertime = Convert.ToDateTime(localscheduletime); error coming so change
            scheduledMessage.calendertime = DateTime.Today;
            scheduledMessage.shareMessage = shareMessage;
            string userlocalscheduletime = localscheduletime;
            try
            {
                _logger.LogError("ScheduleMessageHelperscheduleTime>>>>" + scheduleTime);
                var dt = DateTime.Parse(scheduleTime);
                scheduledMessage.scheduleTime = Convert.ToDateTime(TimeZoneInfo.ConvertTimeToUtc(dt));
                //scheduledMessage.scheduleTime = Convert.ToDateTime(scheduleTime) ;
                // scheduledMessage.scheduleTime = Convert.ToDateTime(CompareDateWithclient(DateTime.UtcNow.ToString(),scheduleTime));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
            DateTime fromTime = scheduledMessage.scheduleTime.AddMinutes(-scheduledMessage.scheduleTime.Minute);
            DateTime toTime = scheduledMessage.scheduleTime.AddMinutes(-scheduledMessage.scheduleTime.Minute).AddHours(1);
            try
            {
                int count = dbr.Find<ScheduledMessage>(t => t.scheduleTime > fromTime && t.scheduleTime <= toTime && t.profileId == profileId).Count();
                if (count > _AppSettings.FacebookScheduleMessageMaxLimit)
                {
                    _logger.LogError("Facebook Max limit Reached.");
                    return "Max limit Reached.";
                }
            }
            catch (Exception)
            {
            }
            scheduledMessage.status = Domain.Socioboard.Enum.ScheduleStatus.Pending;
            scheduledMessage.userId = userId;
            scheduledMessage.profileType = profiletype;
            scheduledMessage.profileId = profileId;
            scheduledMessage.url = url;
            scheduledMessage.link = link;
            scheduledMessage.picUrl = picUrl;
            scheduledMessage.createTime = DateTime.UtcNow;
            scheduledMessage.clientTime = DateTime.Now;
            scheduledMessage.localscheduletime = userlocalscheduletime;
            scheduledMessage.socialprofileName = socialprofileName;
            scheduledMessage.mediaType = mediaType;
            int ret = dbr.Add<ScheduledMessage>(scheduledMessage);
            if (ret == 1)
            {
                return "Scheduled.";
            }
            else
            {
                return "Not Scheduled.";
            }
        }

        public static string DaywiseScheduleMessage(string profileId, string socialprofileName, string weekdays, string shareMessage, Domain.Socioboard.Enum.SocialProfileType profiletype, long userId, string link, string url, string picUrl, string localscheduletime, AppSettings _AppSettings, Cache _redisCache, DatabaseRepository dbr, ILogger _logger)
        {
            DaywiseSchedule scheduledMessage = new DaywiseSchedule();
            scheduledMessage.shareMessage = shareMessage;
            //scheduledMessage.calendertime = Convert.ToDateTime(localscheduletime);
            string userlocalscheduletime = localscheduletime;
            try
            {
                // _logger.LogError("ScheduleMessageHelperscheduleTime>>>>" + scheduleTime);
                // var dt = DateTime.Parse(scheduleTime);
                // scheduledMessage.scheduleTime = Convert.ToDateTime(TimeZoneInfo.ConvertTimeToUtc(dt));
                //scheduledMessage.scheduleTime = Convert.ToDateTime(scheduleTime) ;
                // scheduledMessage.scheduleTime = Convert.ToDateTime(CompareDateWithclient(DateTime.UtcNow.ToString(),scheduleTime));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
            DateTime fromTime = scheduledMessage.scheduleTime.AddMinutes(-scheduledMessage.scheduleTime.Minute);
            DateTime toTime = scheduledMessage.scheduleTime.AddMinutes(-scheduledMessage.scheduleTime.Minute).AddHours(1);
            try
            {
                int count = dbr.Find<ScheduledMessage>(t => t.scheduleTime > fromTime && t.scheduleTime <= toTime && t.profileId == profileId).Count();
                if (count > _AppSettings.FacebookScheduleMessageMaxLimit)
                {
                    _logger.LogError("Facebook Max limit Reached.");
                    return "Max limit Reached.";
                }
            }
            catch (Exception)
            {
            }
            scheduledMessage.status = Domain.Socioboard.Enum.ScheduleStatus.Pending;
            scheduledMessage.userId = userId;
            scheduledMessage.profileType = profiletype;
            scheduledMessage.profileId = profileId;
            scheduledMessage.weekdays = weekdays;


            scheduledMessage.url = url;
            scheduledMessage.link = link;
            scheduledMessage.picUrl = picUrl;
            scheduledMessage.createTime = DateTime.UtcNow;
            scheduledMessage.clientTime = DateTime.Now;        
            scheduledMessage.localscheduletime = Convert.ToDateTime(userlocalscheduletime);

         
            var selectDayObject = JsonConvert.DeserializeObject<List<string>>(scheduledMessage.weekdays);
            scheduledMessage.scheduleTime = DateTimeHelper.GetNextScheduleDate(selectDayObject, scheduledMessage.localscheduletime);

            // scheduledMessage.localscheduletime = userlocalscheduletime;
            scheduledMessage.socialprofileName = socialprofileName;
            int ret = dbr.Add<DaywiseSchedule>(scheduledMessage);
            if (ret == 1)
            {
                return "Scheduled.";
            }
            else
            {
                return "Not Scheduled.";
            }

        }


        public static void DraftScheduleMessage(string shareMessage, long userId, long groupId, string picUrl, string scheduleTime, Domain.Socioboard.Enum.MediaType mediaType, AppSettings _AppSettings, Cache _redisCache, DatabaseRepository dbr, ILogger _logger)
        {
            Draft _Draft = new Draft();
            _Draft.shareMessage = shareMessage;

            try
            {
                _Draft.scheduleTime = Convert.ToDateTime(scheduleTime);
            }
            catch (Exception ex)
            {

            }
            //_Draft.scheduleTime = DateTime.Parse(scheduleTime);
            _Draft.userId = userId;
            _Draft.GroupId = groupId;
            _Draft.picUrl = picUrl;
            _Draft.createTime = DateTime.UtcNow;
            _Draft.mediaType = mediaType;
            dbr.Add<Draft>(_Draft);
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
                return schedule.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return "";
            }
        }
    }
}
