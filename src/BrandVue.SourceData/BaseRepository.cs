using System.Diagnostics;
using System.Linq.Expressions;

namespace BrandVue.SourceData
{
    [DebuggerStepThrough]
    public abstract class BaseRepository<TStored, TIdentity> : IAddableRepository<TStored, TIdentity> where TStored : class
    {
        protected readonly object _lock = new object();
        protected IDictionary<TIdentity, TStored> _objectsById;
        private static readonly Func<TStored> CompiledParameterlessConstructorInvocation;

        static BaseRepository()
        {
            //  This madness allows us to execute the constructor with much better performance
            //  than alternatives, such as ConstructorInfo.Invoke or Activator.CreateInstance.
            //  This holds true even if we cache the ConstructorInfo. Note that the expectation
            //  is that, for types that don't export a public default constructor, GetOrCreate
            //  will be overridden.
            //
            //  (If I can I may retire all of this in due course, but I have bigger problems to
            //  worry about for now.)
            var parameterlessConstructor = typeof(TStored).GetConstructor(Type.EmptyTypes);
            if (parameterlessConstructor != null)
            {
                var expression = Expression.New(parameterlessConstructor);
                var lambda = Expression.Lambda(typeof(Func<TStored>), expression);
                CompiledParameterlessConstructorInvocation = (Func<TStored>) lambda.Compile();
            }
        }

        protected BaseRepository(IEqualityComparer<TIdentity> identityComparer = null)
        {
            _objectsById = new Dictionary<TIdentity, TStored>(identityComparer ?? EqualityComparer<TIdentity>.Default);
        }

        /// <remarks>
        /// Not used during the normal load process, allows manual adding if the id is not yet in use
        /// Returns true if added
        /// </remarks>
        public bool TryAdd(TIdentity objectId, TStored obj)
        {
            lock (_lock)
            {
                if (_objectsById.ContainsKey(objectId))
                {
                    return false;
                }
                _objectsById.Add(objectId, obj);
                return true;
            }
        }

        public virtual TStored GetOrCreate(TIdentity objectId)
        {
            lock (_lock)
            {
                if (!_objectsById.TryGetValue(objectId, out var thingOfDesire))
                {
                    if (CompiledParameterlessConstructorInvocation == null)
                    {
                        throw new InvalidOperationException(
                            $@"Cannot instantiate object of type {
                                    typeof(TStored)
                                } because it does not have a parameterless constructor.");
                    }

                    thingOfDesire = CompiledParameterlessConstructorInvocation.Invoke();
                    SetIdentity(thingOfDesire, objectId);
                    _objectsById.Add(objectId, thingOfDesire);
                }
                return thingOfDesire;
            }
        }

        public virtual TStored Remove(TIdentity objectId)
        {
            lock (_lock)
            {
                if (_objectsById.TryGetValue(objectId, out var thingOfDisgust))
                {
                    _objectsById.Remove(objectId);
                    return thingOfDisgust;
                }
                else
                {
                    return default(TStored);
                }
            }
        }

        protected abstract void SetIdentity(TStored target, TIdentity identity);

        public TStored Get(TIdentity identity)
        {
            lock (_lock)
            {
                return _objectsById[identity];
            }
        }

        public IEnumerable<TStored> GetMany(TIdentity[] identities)
        {
            lock (_lock)
            {
                return identities.Select(identity => _objectsById[identity]);
            }
        }

        public bool TryGet(TIdentity identity, out TStored stored)
        {
            lock (_lock)
            {
                return _objectsById.TryGetValue(identity, out stored);
            }
        }
    }
}