﻿namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceOptionsAsync<TService> : IServiceOptions
        where TService : class
    {
        Task<Func<IServiceProvider, TService>> BuildAsync();
    }
}
