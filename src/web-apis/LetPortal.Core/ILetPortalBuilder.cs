﻿using LetPortal.Core.Persistences;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetPortal.Core
{
    public interface ILetPortalBuilder
    {
        IServiceCollection Services { get; }

        IConfiguration Configuration { get; }

        IHealthChecksBuilder HealthChecksBuilder { get; }

        ConnectionType ConnectionType { get; }

        LetPortalOptions LetPortalOptions { get; }

        void Build();
    }
}
