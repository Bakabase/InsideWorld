﻿using System.Linq.Expressions;
using Bakabase.Abstractions.Models.Domain;
using Bakabase.InsideWorld.Models.Constants.AdditionalItems;
using Bakabase.InsideWorld.Models.RequestModels;
using Bootstrap.Models.ResponseModels;

namespace Bakabase.Modules.CustomProperty.Abstractions.Services;

public interface ICustomPropertyValueService
{
    Task<List<CustomPropertyValue>> GetAll(Expression<Func<Bakabase.Abstractions.Models.Db.CustomPropertyValue, bool>>? exp, CustomPropertyValueAdditionalItem additionalItems, bool returnCopy);

    Task<List<Bakabase.Abstractions.Models.Db.CustomPropertyValue>> GetAllDbModels(Expression<Func<Bakabase.Abstractions.Models.Db.CustomPropertyValue, bool>> selector = null, bool returnCopy = true);

    Task<BaseResponse> SetResourceValue(int resourceId, int propertyId,
        ResourceCustomPropertyValuePutRequestModel model);
    //
    Task<BaseResponse> AddRange(IEnumerable<CustomPropertyValue> values);
    // Task<ListResponse<CustomPropertyValue>> AddRange(List<CustomPropertyValue> resources);
    Task<BaseResponse> UpdateRange(IEnumerable<CustomPropertyValue> values);
    // Task<BaseResponse> UpdateRange(IReadOnlyCollection<CustomPropertyValue> resources);
    // Task<CustomPropertyValue> GetByKey(Int32 key, bool returnCopy = true);
    // Task<CustomPropertyValue[]> GetByKeys(IEnumerable<Int32> keys, bool returnCopy = true);
    //
    // Task<CustomPropertyValue> GetFirst(Expression<Func<CustomPropertyValue, bool>> selector,
    //     Expression<Func<CustomPropertyValue, object>> orderBy = null, bool asc = false, bool returnCopy = true);
    //
    // Task<int> Count(Func<CustomPropertyValue, bool> selector = null);
    //
    // /// <summary>
    // /// 
    // /// </summary>
    // /// <param name="selector"></param>
    // /// <param name="pageIndex"></param>
    // /// <param name="pageSize"></param>
    // /// <param name="orders">Key Selector - Asc</param>
    // /// <param name="returnCopy"></param>
    // /// <returns></returns>
    // Task<SearchResponse<CustomPropertyValue>> Search(Func<CustomPropertyValue, bool> selector,
    //     int pageIndex, int pageSize, (Func<CustomPropertyValue, object> SelectKey, bool Asc, IComparer<object>? comparer)[] orders,
    //     bool returnCopy = true);
    //
    // Task<SearchResponse<CustomPropertyValue>> Search(Func<CustomPropertyValue, bool> selector,
    //     int pageIndex, int pageSize, Func<CustomPropertyValue, object> orderBy = null, bool asc = false, IComparer<object>? comparer = null, bool returnCopy = true);
    //
    Task<BaseResponse> Remove(Bakabase.Abstractions.Models.Db.CustomPropertyValue resource);
    Task<BaseResponse> RemoveRange(IEnumerable<Bakabase.Abstractions.Models.Db.CustomPropertyValue> resources);
    Task<BaseResponse> RemoveAll(Expression<Func<Bakabase.Abstractions.Models.Db.CustomPropertyValue, bool>> selector);
    // Task<BaseResponse> RemoveByKey(Int32 key);
    Task<BaseResponse> RemoveByKeys(IEnumerable<Int32> keys);
    // Task<SingletonResponse<CustomPropertyValue>> Add(CustomPropertyValue resource);
    // Task<SingletonResponse<CustomPropertyValue>> UpdateByKey(Int32 key, Action<CustomPropertyValue> modify);
    // Task<BaseResponse> Update(CustomPropertyValue resource);
    //
    // Task<ListResponse<CustomPropertyValue>> UpdateByKeys(IReadOnlyCollection<Int32> keys,
    //     Action<CustomPropertyValue> modify);
    //
    // Task<SingletonResponse<CustomPropertyValue>> UpdateFirst(Expression<Func<CustomPropertyValue, bool>> selector,
    //     Action<CustomPropertyValue> modify);
    //
    // Task<ListResponse<CustomPropertyValue>> UpdateAll(Expression<Func<CustomPropertyValue, bool>> selector,
    //     Action<CustomPropertyValue> modify);
}