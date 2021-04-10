﻿using Domain.Socioboard.Models.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Socioboard.Models
{
    public class YoutubeChannel
    {
        public virtual Int64 Id { get; set; }
        public virtual Int64 UserId { get; set; }
        public virtual string YtubeChannelId { get; set; }
        public virtual string YtubeChannelName { get; set; }
        public virtual DateTime LastUpdate { get; set; }
        public virtual string ChannelpicUrl { get; set; }
        public virtual string WebsiteUrl { get; set; }
        public virtual DateTime EntryDate { get; set; }
        public virtual string YtubeChannelDescription { get; set; }
        public virtual bool IsActive { get; set; }
        public virtual string AccessToken { get; set; }
        public virtual string RefreshToken { get; set; }
        public virtual DateTime PublishingDate { get; set; }
        public virtual double VideosCount { get; set; }
        public virtual double CommentsCount { get; set; }
        public virtual double SubscribersCount { get; set; }
        public virtual double ViewsCount { get; set; }
        public virtual string Channel_EmailId { get; set; }
        public virtual bool Days90Update { get; set; }
        public virtual DateTime LastReport_Update { get; set; }
        public virtual DateTime LastVideoListDetails_Update { get; set; }

    }


    public class YoutubeReports_all
    {
        public YoutubeChannel _YoutubeChannelss { get; set; }
        public List<YoutubeVideoDetailsList> _YoutubeVideoss { get; set; }
    }

    public class TotalYoutubesubscriber
    {
        public int SubscribersCounts { get; set; }
        public DateTime startdate { get; set; }
        public DateTime endtdate { get; set; }
        public string ChannelName { get; set; }
        public string ChannelId { get; set; }
        public string colors { get; set; }
        public string Channelpic { get; set; }
    }
}
