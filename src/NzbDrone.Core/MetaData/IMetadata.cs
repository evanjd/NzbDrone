﻿using System.Collections.Generic;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Metadata.Files;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Metadata
{
    public interface IMetadata : IProvider
    {
        void OnSeriesUpdated(Series series, List<MetadataFile> existingMetadataFiles);
        void OnEpisodeImport(Series series, EpisodeFile episodeFile, bool newDownload);
        void AfterRename(Series series);
        MetadataFile FindMetadataFile(Series series, string path);
    }
}
