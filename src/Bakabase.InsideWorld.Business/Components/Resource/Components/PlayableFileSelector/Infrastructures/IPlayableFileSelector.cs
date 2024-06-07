﻿using System.Threading;
using System.Threading.Tasks;
using Bakabase.Abstractions.Components.Component;

namespace Bakabase.InsideWorld.Business.Components.Resource.Components.PlayableFileSelector.Infrastructures
{
    public interface IPlayableFileSelector : IComponent
    {
        Task<string[]> GetStartFiles(string fileOrDirectory, CancellationToken ct);
    }
}