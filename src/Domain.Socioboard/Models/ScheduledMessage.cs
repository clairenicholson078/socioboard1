﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Socioboard.Models
{
    public class ScheduledMessage
    {
        public ScheduledMessage()
        {
            createTime = DateTime.UtcNow;
        }
        public virtual long id { get; set; }
        public virtual string shareMessage { get; set; }
        public virtual DateTime clientTime { get; set; }
        public virtual DateTime scheduleTime { get; set; }
        public virtual string localscheduletime { get; set; }
        public virtual Enum.ScheduleStatus status { get; set; }
        public virtual long userId { get; set; }
        public virtual Enum.SocialProfileType profileType { get; set; }
        public virtual string profileId { get; set; }
        public virtual string picUrl { get; set; }
        public virtual DateTime createTime { get; set; }
        public virtual string url { get; set; }
        public virtual string link { get; set; }
        public virtual string socialprofileName { get; set; }
        public virtual DateTime calendertime { get; set; }
        public virtual Enum.MediaType mediaType { get; set; }
    }
    public class ScheduledMessageAdmin
    {
        public int messageCount { get; set; }
        public string userName { get; set; }
        public int messagecompletedCount { get; set; }
        public int messageremainingCount { get; set; }
    }
}
