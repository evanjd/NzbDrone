﻿using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.NotifyMyAndroid
{
    public class NotifyMyAndroid : NotificationBase<NotifyMyAndroidSettings>
    {
        private readonly INotifyMyAndroidProxy _notifyMyAndroidProxy;

        public NotifyMyAndroid(INotifyMyAndroidProxy notifyMyAndroidProxy)
        {
            _notifyMyAndroidProxy = notifyMyAndroidProxy;
        }

        public override string Link
        {
            get { return "http://www.notifymyandroid.com/"; }
        }

        public override void OnGrab(string message)
        {
            const string title = "Episode Grabbed";

            _notifyMyAndroidProxy.SendNotification(title, message, Settings.ApiKey, (NotifyMyAndroidPriority)Settings.Priority);
        }

        public override void OnDownload(DownloadMessage message)
        {
            const string title = "Episode Downloaded";

            _notifyMyAndroidProxy.SendNotification(title, message.Message, Settings.ApiKey, (NotifyMyAndroidPriority)Settings.Priority);
        }

        public override void AfterRename(Series series)
        {
        }
    }
}
