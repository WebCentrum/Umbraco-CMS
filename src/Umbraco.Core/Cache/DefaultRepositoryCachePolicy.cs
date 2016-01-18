using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.EntityBase;

namespace Umbraco.Core.Cache
{
    /// <summary>
    /// The default cache policy for retrieving a single entity
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    internal class DefaultRepositoryCachePolicy<TEntity, TId> : DisposableObject, IRepositoryCachePolicy<TEntity, TId>
        where TEntity : class, IAggregateRoot
    {
        private readonly RepositoryCachePolicyOptions _options;
        protected IRuntimeCacheProvider Cache { get; private set; }
        private Action _action;
       
        public DefaultRepositoryCachePolicy(IRuntimeCacheProvider cache, RepositoryCachePolicyOptions options)
        {
            if (cache == null) throw new ArgumentNullException("cache");
            if (options == null) throw new ArgumentNullException("options");

            _options = options;
            Cache = cache;
        }

        public string GetCacheIdKey(object id)
        {
            if (id == null) throw new ArgumentNullException("id");

            return string.Format("{0}{1}", GetCacheTypeKey(), id);
        }

        public string GetCacheTypeKey()
        {
            return string.Format("uRepo_{0}_", typeof(TEntity).Name);
        }

        public void CreateOrUpdate(TEntity entity, Action<TEntity> persistMethod)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            if (persistMethod == null) throw new ArgumentNullException("persistMethod");

            try
            {
                persistMethod(entity);

                //set the disposal action                
                SetCacheAction(() =>
                {
                    //just to be safe, we cannot cache an item without an identity
                    if (entity.HasIdentity)
                    {
                        Cache.InsertCacheItem(GetCacheIdKey(entity.Id), () => entity);
                    }
                    
                    //If there's a GetAllCacheAllowZeroCount cache, ensure it is cleared
                    Cache.ClearCacheItem(GetCacheTypeKey());
                });
                
            }
            catch
            {
                //set the disposal action                
                SetCacheAction(() =>
                {
                    //if an exception is thrown we need to remove the entry from cache, this is ONLY a work around because of the way
                    // that we cache entities: http://issues.umbraco.org/issue/U4-4259
                    Cache.ClearCacheItem(GetCacheIdKey(entity.Id));

                    //If there's a GetAllCacheAllowZeroCount cache, ensure it is cleared
                    Cache.ClearCacheItem(GetCacheTypeKey());
                });
                
                throw;
            }
        }

        public void Remove(TEntity entity, Action<TEntity> persistMethod)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            if (persistMethod == null) throw new ArgumentNullException("persistMethod");

            persistMethod(entity);

            //set the disposal action
            var cacheKey = GetCacheIdKey(entity.Id);
            SetCacheAction(() =>
            {
                Cache.ClearCacheItem(cacheKey);
                //If there's a GetAllCacheAllowZeroCount cache, ensure it is cleared
                Cache.ClearCacheItem(GetCacheTypeKey());
            });            
        }

        public TEntity Get(TId id, Func<TId, TEntity> getFromRepo)
        {
            if (getFromRepo == null) throw new ArgumentNullException("getFromRepo");

            var cacheKey = GetCacheIdKey(id);
            var fromCache = Cache.GetCacheItem<TEntity>(cacheKey);
            if (fromCache != null)
                return fromCache;
            
            var entity = getFromRepo(id);

            //set the disposal action
            SetCacheAction(cacheKey, entity);

            return entity;
        }

        public TEntity Get(TId id)
        {
            var cacheKey = GetCacheIdKey(id);
            return Cache.GetCacheItem<TEntity>(cacheKey);
        }

        public bool Exists(TId id, Func<TId, bool> getFromRepo)
        {
            if (getFromRepo == null) throw new ArgumentNullException("getFromRepo");

            var cacheKey = GetCacheIdKey(id);
            var fromCache = Cache.GetCacheItem<TEntity>(cacheKey);
            return fromCache != null || getFromRepo(id);
        }

        public virtual TEntity[] GetAll(TId[] ids, Func<TId[], IEnumerable<TEntity>> getFromRepo)            
        {
            if (getFromRepo == null) throw new ArgumentNullException("getFromRepo");

            if (ids.Any())
            {
                var entities = ids.Select(Get).ToArray();
                if (ids.Length.Equals(entities.Length) && entities.Any(x => x == null) == false)
                    return entities;
            }
            else
            {
                var allEntities = GetAllFromCache();
                if (allEntities.Any())
                {
                    if (_options.GetAllCacheValidateCount)
                    {
                        //Get count of all entities of current type (TEntity) to ensure cached result is correct
                        var totalCount = _options.PerformCount();
                        if (allEntities.Length == totalCount)
                            return allEntities;
                    }
                    else
                    {
                        return allEntities;
                    }
                }
                else if (_options.GetAllCacheAllowZeroCount)
                {
                    //if the repository allows caching a zero count, then check the zero count cache
                    var zeroCount = Cache.GetCacheItem<TEntity[]>(GetCacheTypeKey());
                    if (zeroCount != null && zeroCount.Any() == false)
                    {
                        //there is a zero count cache so return an empty list
                        return new TEntity[] {};
                    }
                }
            }

            //we need to do the lookup from the repo
            var entityCollection = getFromRepo(ids)
                //ensure we don't include any null refs in the returned collection!
                .WhereNotNull()
                .ToArray();

            //set the disposal action
            SetCacheAction(ids, entityCollection);

            return entityCollection;
        }

        /// <summary>
        /// Performs the lookup for all entities of this type from the cache
        /// </summary>
        /// <returns></returns>
        protected virtual TEntity[] GetAllFromCache()
        {
            var allEntities = Cache.GetCacheItemsByKeySearch<TEntity>(GetCacheTypeKey())
                    .WhereNotNull()
                    .ToArray();
            return allEntities.Any() ? allEntities : new TEntity[] {};
        }

        /// <summary>
        /// The disposal performs the caching
        /// </summary>
        protected override void DisposeResources()
        {
            if (_action != null)
            {
                _action();
            }
        }

        /// <summary>
        /// Sets the action to execute on disposal for a single entity
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="entity"></param>
        protected virtual void SetCacheAction(string cacheKey, TEntity entity)
        {
            if (entity == null) return;

            SetCacheAction(() =>
            {
                //just to be safe, we cannot cache an item without an identity
                if (entity.HasIdentity)
                {
                    Cache.InsertCacheItem(cacheKey, () => entity);
                }
            });
        }

        /// <summary>
        /// Sets the action to execute on disposal for an entity collection
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="entityCollection"></param>
        protected virtual void SetCacheAction(TId[] ids, TEntity[] entityCollection)
        {
            SetCacheAction(() =>
            {
                //This option cannot execute if we are looking up specific Ids
                if (ids.Any() == false && entityCollection.Length == 0 && _options.GetAllCacheAllowZeroCount)
                {
                    //there was nothing returned but we want to cache a zero count result so add an TEntity[] to the cache
                    // to signify that there is a zero count cache
                    Cache.InsertCacheItem(GetCacheTypeKey(), () => new TEntity[] {});
                }
                else
                {
                    //This is the default behavior, we'll individually cache each item so that if/when these items are resolved 
                    // by id, they are returned from the already existing cache.
                    foreach (var entity in entityCollection.WhereNotNull())
                    {
                        var localCopy = entity;
                        //just to be safe, we cannot cache an item without an identity
                        if (localCopy.HasIdentity)
                        {
                            Cache.InsertCacheItem(GetCacheIdKey(entity.Id), () => localCopy);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Sets the action to execute on disposal
        /// </summary>
        /// <param name="action"></param>
        protected void SetCacheAction(Action action)
        {
            _action = action;
        }
    }
}