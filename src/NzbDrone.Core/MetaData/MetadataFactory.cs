﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Composition;
using NzbDrone.Core.Metadata.Consumers.Fake;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Metadata
{
    public interface IMetadataFactory : IProviderFactory<IMetadata, MetadataDefinition>
    {
        List<IMetadata> Enabled();
    }

    public class MetadataFactory : ProviderFactory<IMetadata, MetadataDefinition>, IMetadataFactory
    {
        private readonly IMetadataRepository _providerRepository;

        public MetadataFactory(IMetadataRepository providerRepository, IEnumerable<IMetadata> providers, IContainer container, Logger logger)
            : base(providerRepository, providers, container, logger)
        {
            _providerRepository = providerRepository;
        }

        protected override void InitializeProviders()
        {
            var definitions = new List<MetadataDefinition>();

            foreach (var provider in _providers)
            {
                if (provider.GetType() == typeof(FakeMetadata)) continue;;

                definitions.Add(new MetadataDefinition
                {
                    Enable = false,
                    Name = provider.GetType().Name,
                    Implementation = provider.GetType().Name,
                    Settings = (IProviderConfig)Activator.CreateInstance(provider.ConfigContract)
                });
            }

            var currentProviders = All();

            var newProviders = definitions.Where(def => currentProviders.All(c => c.Implementation != def.Implementation)).ToList();

            if (newProviders.Any())
            {
                _providerRepository.InsertMany(newProviders.Cast<MetadataDefinition>().ToList());
            }
        }

        public List<IMetadata> Enabled()
        {
            return GetAvailableProviders().Where(n => ((MetadataDefinition)n.Definition).Enable).ToList();
        }
    }
}