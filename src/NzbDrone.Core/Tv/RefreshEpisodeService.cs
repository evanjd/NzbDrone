﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using NLog;
using NzbDrone.Common;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Tv
{
    public interface IRefreshEpisodeService
    {
        void RefreshEpisodeInfo(Series series, IEnumerable<Episode> remoteEpisodes);
    }

    public class RefreshEpisodeService : IRefreshEpisodeService
    {
        private readonly IEpisodeService _episodeService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public RefreshEpisodeService(IEpisodeService episodeService, IEventAggregator eventAggregator, Logger logger)
        {
            _episodeService = episodeService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void RefreshEpisodeInfo(Series series, IEnumerable<Episode> remoteEpisodes)
        {
            _logger.Info("Starting episode info refresh for: {0}", series);
            var successCount = 0;
            var failCount = 0;

            var existingEpisodes = _episodeService.GetEpisodeBySeries(series.Id);
            var seasons = series.Seasons;

            var updateList = new List<Episode>();
            var newList = new List<Episode>();
            var dupeFreeRemoteEpisodes = remoteEpisodes.DistinctBy(m => new { m.SeasonNumber, m.EpisodeNumber }).ToList();

            foreach (var episode in dupeFreeRemoteEpisodes.OrderBy(e => e.SeasonNumber).ThenBy(e => e.EpisodeNumber))
            {
                try
                {
                    var episodeToUpdate = existingEpisodes.FirstOrDefault(e => e.SeasonNumber == episode.SeasonNumber && e.EpisodeNumber == episode.EpisodeNumber);

                    if (episodeToUpdate != null)
                    {
                        existingEpisodes.Remove(episodeToUpdate);
                        updateList.Add(episodeToUpdate);
                    }
                    else
                    {
                        episodeToUpdate = new Episode();
                        episodeToUpdate.Monitored = GetMonitoredStatus(episode, seasons);
                        newList.Add(episodeToUpdate);
                    }

                    episodeToUpdate.SeriesId = series.Id;
                    episodeToUpdate.EpisodeNumber = episode.EpisodeNumber;
                    episodeToUpdate.SeasonNumber = episode.SeasonNumber;
                    episodeToUpdate.Title = episode.Title;
                    episodeToUpdate.Overview = episode.Overview;
                    episodeToUpdate.AirDate = episode.AirDate;
                    episodeToUpdate.AirDateUtc = episode.AirDateUtc;

                    successCount++;
                }
                catch (Exception e)
                {
                    _logger.FatalException(String.Format("An error has occurred while updating episode info for series {0}. {1}", series, episode), e);
                    failCount++;
                }
            }

            var allEpisodes = new List<Episode>();
            allEpisodes.AddRange(newList);
            allEpisodes.AddRange(updateList);

            AdjustMultiEpisodeAirTime(series, allEpisodes);
            SetAbsoluteEpisodeNumber(allEpisodes);

            _episodeService.DeleteMany(existingEpisodes);
            _episodeService.UpdateMany(updateList);
            _episodeService.InsertMany(newList);

            if (newList.Any())
            {
                _eventAggregator.PublishEvent(new EpisodeInfoAddedEvent(newList, series));
            }

            if (updateList.Any())
            {
                _eventAggregator.PublishEvent(new EpisodeInfoUpdatedEvent(updateList));
            }

            if (existingEpisodes.Any())
            {
                _eventAggregator.PublishEvent(new EpisodeInfoDeletedEvent(updateList));
            }

            if (failCount != 0)
            {
                _logger.Info("Finished episode refresh for series: {0}. Successful: {1} - Failed: {2} ",
                    series.Title, successCount, failCount);
            }
            else
            {
                _logger.Info("Finished episode refresh for series: {0}.", series);
            }
        }

        private static bool GetMonitoredStatus(Episode episode, IEnumerable<Season> seasons)
        {
            if (episode.EpisodeNumber == 0 && episode.SeasonNumber != 1)
            {
                return false;
            }

            var season = seasons.SingleOrDefault(c => c.SeasonNumber == episode.SeasonNumber);
            return season == null || season.Monitored;
        }

        private static void AdjustMultiEpisodeAirTime(Series series, IEnumerable<Episode> allEpisodes)
        {
            var groups =
                allEpisodes.Where(c => c.AirDateUtc.HasValue)
                    .GroupBy(e => new { e.SeasonNumber, e.AirDate })
                    .Where(g => g.Count() > 1)
                    .ToList();

            foreach (var group in groups)
            {
                var episodeCount = 0;
                foreach (var episode in group.OrderBy(e => e.SeasonNumber).ThenBy(e => e.EpisodeNumber))
                {
                    episode.AirDateUtc = episode.AirDateUtc.Value.AddMinutes(series.Runtime * episodeCount);
                    episodeCount++;
                }
            }
        }

        private static void SetAbsoluteEpisodeNumber(IEnumerable<Episode> allEpisodes)
        {
            var episodes = allEpisodes.Where(e => e.SeasonNumber > 0 && e.EpisodeNumber > 0)
                                      .OrderBy(e => e.SeasonNumber).ThenBy(e => e.EpisodeNumber)
                                      .ToList();

            for (int i = 0; i < episodes.Count(); i++)
            {
                episodes[i].AbsoluteEpisodeNumber = i + 1;
            }
        }
    }
}