﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bakabase.InsideWorld.Business.Components.BulkModification.Abstractions.Models;
using Bakabase.InsideWorld.Business.Components.BulkModification.Abstractions.Models.Constants;
using Bakabase.InsideWorld.Business.Components.BulkModification.Abstractions.Models.Dtos;
using Bakabase.InsideWorld.Business.Components.BulkModification.Processors;
using Bakabase.InsideWorld.Business.Components.BulkModification.Processors.BmVolumeProcessor;
using Bakabase.InsideWorld.Business.Services;
using Bakabase.InsideWorld.Models.Constants;
using Bakabase.InsideWorld.Models.Constants.AdditionalItems;
using Bakabase.InsideWorld.Models.Extensions;
using Bakabase.InsideWorld.Models.Models.Aos;
using Bakabase.InsideWorld.Models.Models.Dtos;
using Bootstrap.Components.Miscellaneous.ResponseBuilders;
using Bootstrap.Components.Orm;
using Bootstrap.Components.Orm.Infrastructures;
using Bootstrap.Extensions;
using Bootstrap.Models.ResponseModels;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bakabase.InsideWorld.Business.Components.BulkModification.Abstractions.Services
{
    public class BulkModificationService : ResourceService<InsideWorldDbContext, Models.BulkModification, int>
    {
        protected ResourceService ResourceService => GetRequiredService<ResourceService>();

        protected BulkModificationDiffService BulkModificationDiffService =>
            GetRequiredService<BulkModificationDiffService>();

        protected BulkModificationTempDataService BulkModificationTempDataService =>
            GetRequiredService<BulkModificationTempDataService>();

        public BulkModificationService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public static BulkModificationConfiguration GetConfiguration()
        {
            var bmc = new BulkModificationConfiguration();
            foreach (BulkModificationProperty property in Enum.GetValues(typeof(BulkModificationProperty)))
            {
                var attr = SpecificTypeUtils<BulkModificationProperty>.Type.GetField(property.ToString())!
                    .GetCustomAttributes<BulkModificationPropertyFilterAttribute>(false).FirstOrDefault()!;
                var options = new BulkModificationConfiguration.PropertyOptions
                {
                    Property = property,
                    AvailableOperations = attr.AvailableOperations
                };
                bmc.Properties.Add(options);
            }

            return bmc;
        }

        public async Task<List<BulkModificationDto>> GetAllDto()
        {
            var data = await GetAll();
            var tempData =
                (await BulkModificationTempDataService.GetAll()).ToDictionary(x => x.BulkModificationId, x => x);
            return data.Select(d => d.ToDto(tempData.GetValueOrDefault(d.Id))).ToList();
        }

        public async Task<BulkModificationDto> GetDto(int id)
        {
            var d = await GetByKey(id);
            var tempData = await BulkModificationTempDataService.GetByKey(id);
            return d.ToDto(tempData);
        }

        private ConcurrentDictionary<BulkModificationProperty, IBulkModificationProcessor> PrepareProcessors()
        {
            return new(
                new Dictionary<BulkModificationProperty, IBulkModificationProcessor>
                {
                    {BulkModificationProperty.Category, GetRequiredService<BmCategoryProcessor>()},
                    {BulkModificationProperty.MediaLibrary, GetRequiredService<BmMediaLibraryProcessor>()},
                    {BulkModificationProperty.ReleaseDt, GetRequiredService<BmReleaseDtProcessor>()},
                    {BulkModificationProperty.Publisher, GetRequiredService<BmPublishersProcessor>()},
                    {BulkModificationProperty.Name, GetRequiredService<BmNameProcessor>()},
                    {BulkModificationProperty.Language, GetRequiredService<BmLanguageProcessor>()},
                    {BulkModificationProperty.Volume, GetRequiredService<BmVolumeProcessor>()},
                    {BulkModificationProperty.Original, GetRequiredService<BmOriginalProcessor>()},
                    {BulkModificationProperty.Series, GetRequiredService<BmSeriesProcessor>()},
                    {BulkModificationProperty.Tag, GetRequiredService<BmTagProcessor>()},
                    {BulkModificationProperty.Introduction, GetRequiredService<BmIntroductionProcessor>()},
                    {BulkModificationProperty.Rate, GetRequiredService<BmRateProcessor>()},
                    {BulkModificationProperty.CustomProperty, GetRequiredService<BmCustomPropertiesProcessor>()},
                });
        }

        public async Task<List<int>> PerformFiltering(int id)
        {
            List<int>? ids = null;

            var bulkModification = await GetDto(id);
            var rootFilter = bulkModification.Filter;
            if (rootFilter != null)
            {
                var allResources = await ResourceService.GetAll(ResourceAdditionalItem.All);
                var exp = rootFilter.BuildExpression();
                var filteredResources = allResources.Where(exp.Compile()).ToList();
                ids = filteredResources.Select(r => r.Id).ToList();
                await BulkModificationTempDataService.UpdateResourceIds(id, ids);
            }

            await UpdateByKey(id, a => a.FilteredAt = DateTime.Now);
            return ids ?? new List<int>();
        }

        public async Task<List<(ResourceDto Current, ResourceDto New, List<BulkModificationDiff> Diffs)>>
            Preview(int id, CancellationToken ct)
        {
            var bm = await GetDto(id);
            var tempData = await BulkModificationTempDataService.GetByKey(id);
            var resourceIds = tempData.GetResourceIds();
            var resources = await ResourceService.GetByKeys(resourceIds.ToArray());

            var processors = PrepareProcessors();
            var data = new List<(ResourceDto Current, ResourceDto New, List<BulkModificationDiff> Diffs)>();

            if (bm.Processes?.Any() == true)
            {
                foreach (var resource in resources)
                {
                    ct.ThrowIfCancellationRequested();
                    var variables = new Dictionary<string, string?>();
                    if (bm.Variables != null)
                    {
                        foreach (var variable in bm.Variables)
                        {
                            var sourceValue =
                                variable.Source switch
                                {
                                    BulkModificationVariableSource.Name => resource.Name,
                                    BulkModificationVariableSource.None => null,
                                    BulkModificationVariableSource.FileName => resource.RawName,
                                    BulkModificationVariableSource.FileNameWithoutExtension => Path
                                        .GetFileNameWithoutExtension(
                                            resource.RawName),
                                    BulkModificationVariableSource.FullPath => resource.RawFullname,
                                    BulkModificationVariableSource.DirectoryName => resource.Directory,
                                    _ => throw new ArgumentOutOfRangeException()
                                };

                            string? value = null;
                            if (!string.IsNullOrEmpty(variable.Find) && !string.IsNullOrEmpty(sourceValue))
                            {
                                var match = Regex.Match(sourceValue, variable.Find);
                                if (match.Success)
                                {
                                    value = string.IsNullOrEmpty(variable.Value)
                                        ? match.Value
                                        : Regex.Replace(match.Value, variable.Find, variable.Value);
                                }
                            }
                            else
                            {
                                value = variable.Value;
                            }

                            if (!string.IsNullOrEmpty(value))
                            {
                                variables[variable.Key] = value;
                            }
                        }
                    }

                    var copy = resource.Clone();
                    foreach (var process in bm.Processes)
                    {
                        var processor = processors[process.Property];
                        try
                        {
                            await processor.Process(process, copy, variables);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(
                                $"An error occurred during applying processor [{processor.GetType()}] on process {process}: {e.Message}",
                                e);
                        }
                    }

                    var diffs = resource.Compare(copy)
                        .Select(d => d.ToBulkModificationDiff(id, resource.Id, resource.RawFullname)).ToList();
                    data.Add((resource, copy, diffs));
                }
            }

            var allDiffs = data.SelectMany(d => d.Diffs).ToList();

            await using var tran = await DbContext.Database.BeginTransactionAsync(ct);

            await BulkModificationDiffService.UpdateAll(id, allDiffs);
            await UpdateByKey(id, d => { d.CalculatedAt = DateTime.Now; });

            await tran.CommitAsync(ct);

            return data;
        }

        public async Task<BaseResponse> Apply(int id)
        {
            return await ApplyOrRevert(id, true);
        }

        public async Task<BaseResponse> Revert(int id)
        {
            return await ApplyOrRevert(id, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="apply">False for reverting</param>
        /// <returns></returns>
        public async Task<BaseResponse> ApplyOrRevert(int id, bool apply)
        {
            var bm = (await GetByKey(id))!;
            if (!apply && !bm.AppliedAt.HasValue)
            {
                return BaseResponseBuilder.BuildBadRequest("Can't revert a bulk modification that hasn't applied yet.");
            }

            var diffs = (await BulkModificationDiffService.GetByBmId(id)).GroupBy(a => a.ResourceId)
                .ToDictionary(a => a.Key, a => a.ToList());
            var resources = await ResourceService.GetByKeys(diffs.Keys.ToArray());
            foreach (var resource in resources)
            {
                var resourceDiffs = diffs.GetValueOrDefault(resource.Id);
                if (resourceDiffs?.Any() == true)
                {
                    foreach (var diff in resourceDiffs)
                    {
                        if (apply)
                        {
                            diff.Apply(resource);
                        }
                        else
                        {
                            diff.Revert(resource);
                        }
                    }
                }
            }

            await ResourceService.AddOrUpdateRange(resources);
            var now = DateTime.Now;
            if (apply)
            {
                bm.AppliedAt = now;
            }
            else
            {
                bm.RevertedAt = now;
            }

            await Update(bm);

            return BaseResponseBuilder.Ok;
        }
    }
}