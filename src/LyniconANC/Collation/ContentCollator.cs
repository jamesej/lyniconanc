
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Lynicon.Repositories;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Routing;
using Lynicon.Attributes;
using Newtonsoft.Json.Linq;
using Lynicon.Map;
using Lynicon.Linq;
using Linq2Rest;
using Lynicon.Relations;
using Microsoft.AspNetCore.Routing;
using Lynicon.Exceptions;
using LyniconANC.Extensibility;
using Lynicon.Services;

namespace Lynicon.Collation
{
    /// <summary>
    /// Collator for the Content persistence model (which JSON encodes content data into summary and content parts in a SQL table,
    /// storing metadata in the other SQL table fields)
    /// </summary>
    public class ContentCollator : BaseCollator, ICollator
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ContentCollator));

        public ContentCollator(LyniconSystem sys) : base(sys)
        { }

        /// <inheritdoc/>
        public override Type AssociatedContainerType { get { return typeof(ContentItem); } }

        public override void BuildForTypes(IEnumerable<Type> types)
        {
            System.Extender.RegisterForExtension(typeof(ContentItem));
            foreach (Type t in types)
            {
                System.Extender.RegisterForExtension(t);
                System.Extender.AddExtensionRule(t, typeof(ICoreMetadata));
            }
                
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T>(IEnumerable<Address> addresses)
        {
            if (typeof(Summary).IsAssignableFrom(typeof(T)))
            {
                foreach (var ag in addresses.GroupBy(a => a.Type))
                {
                    var conts = Repository.Get<ContentItem>(typeof(T), ag);
                    foreach (var cont in conts)
                    {
                        var summ = cont.GetSummary(System);
                        if (summ is T)
                            yield return summ as T;
                    }
                    //foreach (var res in Repository.GetByPath(typeof(T), ag.Select(a => a.GetAsContentPath()).ToList()))
                    //{
                    //    var summ = res.GetSummary();
                    //    if (summ is T)
                    //        yield return res.GetSummary() as T;
                    //}
                }

            }
            else
            {
                //foreach (var res in addresses.Select(a => GetWithRelated(a.Type, a.GetAsContentPath(), null)))
                //    yield return (T)res;
                foreach (var res in Collate<T>(null, addresses))
                    yield return res;
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T>(IEnumerable<ItemId> ids)
        {
            if (typeof(Summary).IsAssignableFrom(typeof(T)))
                return Repository.Get<ContentItem>(typeof(T), ids)
                        .Select(ci => ci.GetSummary(System) as T)
                        .Where(s => s != null);
            else
                //return Repository.Get<ContentItem>(typeof(T), ids)
                //        .Select(ci => ci.GetContent<T>());
                return Collate<T>(Repository.Get<ContentItem>(typeof(T), ids), null);
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Get<T, TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
        {
            bool isSummary = typeof(Summary).IsAssignableFrom(typeof(T));
            bool querySummary = typeof(Summary).IsAssignableFrom(typeof(TQuery));

            var dummy = new TQuery[0].AsQueryable();
            IQueryable<TQuery> results;
            bool isContainerQuery = typeof(TQuery).UnextendedType() == typeof(ContentItem)
                || (typeof(TQuery).IsAssignableFrom(typeof(ContentItem))
                    && queryBody(dummy).ExtractFields().All(fn => typeof(ContentItem).GetProperty(fn) != null));
            if (isContainerQuery)
            {
                Func<IQueryable<ContentItem>, IQueryable<ContentItem>> containerQueryBody = iq => queryBody(iq.AsFacade<TQuery>()).AsFacade<ContentItem>();
                results = Repository
                    .Get<ContentItem>(typeof(T), types, containerQueryBody)
                    .Cast<TQuery>()
                    .AsQueryable();
            }
            else if (isSummary && querySummary)
            {
                // Get all summaries from repository and filter them in memory

                results = Repository
                            .Get<ContentItem>(typeof(T), types, iq => iq)
                            .AsEnumerable()
                            .Select(ci => ci.GetSummary(System))
                            .OfType<TQuery>()
                            .AsQueryable();
                results = queryBody(results);
            }
            else // most inefficient choice, gets all items from repository then filters them in memory
                // needs work, bringing back an empty record if T is a summary because doesn't load content field
            {
                var preCollate = Repository
                .Get<ContentItem>(typeof(T), types, iq => iq)
                .AsEnumerable();

                var preResults = Collate<TQuery>(preCollate, null)
                .AsQueryable();

                // Apply query after all items of listed types have been pulled from database, potentially very inefficient
                results = queryBody(preResults);
            }
            
            if (isSummary && !querySummary)
            {
                foreach (var summ in results.Select(r => GetSummary<T>(r)))
                    yield return summ as T;
            }
            else if (isContainerQuery)
            {
                foreach (var item in Collate<T>(results.AsEnumerable(), null))
                    yield return item;
            }
            else
            {
                foreach (var item in results)
                    yield return item as T;
            }

        }

        /// <inheritdoc/>
        public override T GetNew<T>(Address a)
        {
            if (a == null)
                throw new ArgumentException("Trying to create a new item via ContentCollator but no way of generating an address, supply a GetDefaultAddress() method for " + typeof(T).FullName);
            string path = a.GetAsContentPath();
            ContentItem newRecord = GetNewRecord<T>(path);
            //return (T)GetWithRelated(typeof(T), path, newRecord);
            return Collate<T>(new object[] { newRecord }, new Address[] { a }).Single();
        }

        /// <inheritdoc/>
        public override TTarget GetSummary<TTarget>(object item)
        {
            return ContentItem.GetSummary(System, item) as TTarget;
        }

        /// <inheritdoc/>
        public override bool Set(Address a, object data, Dictionary<string, object> setOptions)
        {
            if (a == null)
                a = GetAddress(data);

            var ci = (ContentItem)GetContainer(a, data);

            var updatedData = SetRelated(ci.Path, data, (bool)(setOptions.ContainsKey("bypassChecks") ? setOptions["bypassChecks"] : false));

            if (updatedData != null)
                ci = (ContentItem)GetContainer(a, updatedData);

            if (data is ICoreMetadata && !((ICoreMetadata)data).HasMetadata())
                TypeExtender.CopyExtensionData(ci, data);

            var created = Repository.Set(new List<object> { ci }, setOptions);

            return created[0];
        }

        /// <inheritdoc/>
        public override void Delete(Address a, object data, bool bypassChecks)
        {
            if (a == null)
                a = GetAddress(data);

            var ci = (ContentItem)GetContainer(a, data);

            ci.SetContent(System, data);

            Repository.Delete(ci, bypassChecks);
        }

        /// <inheritdoc/>
        public override void MoveAddress(ItemId id, Address moveTo)
        {
            //var existingSumm = Repository.Get<ContentItem>(typeof(Summary), moveTo).FirstOrDefault();
            //if (existingSumm != null)
            //    throw new ApplicationException("There is an item already at that address");

            if (ContentMap.Instance.AddressOccupied(moveTo))
                throw new Lynicon.Exceptions.ProhibitedActionException("There is an item already at that address");

            var contentItem = Repository.Get<ContentItem>(id.Type, id.Id);

            // If the address is dependent on data fields, set those fields correspondingly
            object data = contentItem.GetContent(System.Extender);
            Address address = new Address(data);
            bool hasAddressFields = address.Count > 0;
            address = moveTo;
            if (hasAddressFields)
            {
                address.SetAddressFields(data);
                contentItem.SetContent(System, data);
            }

            contentItem.Path = address.GetAsContentPath();

            System.Events.ProcessEvent("Content.Move", this, Tuple.Create(moveTo, contentItem));

            Repository.Set(contentItem);
        }

        private ContentItem GetContentItem(Address a, object data)
        {
            string dataPath = null;
            string routePath = null;
            ContentItem contentItem = null;

            // try and get path from data via attributes
            Address address = new Address(data);
            if (address.Count > 0)
                dataPath = address.GetAsContentPath();

            // Get requested path
            if (a != null)
            {
                routePath = a.GetAsContentPath();
            }

            // We have path in data and address parameter and they disagree
            if (dataPath != null && routePath != null && dataPath != routePath)
            {
                // Raise event here for when address is changed via changing addressed mapped fields on data
                System.Events.ProcessEvent("Content.Move", this, Tuple.Create(a, data));
                // regenerate path in case event processor changed data
                address = new Address(data);
                dataPath = address.GetAsContentPath();
                routePath = a.GetAsContentPath();
            }

            string path = dataPath ?? routePath;

            // if we have an extended content object, we can use the OriginalRecord if must as we have no path, or if the path is the same
            if (data is ICoreMetadata)
            {
                contentItem = (ContentItem)Activator.CreateInstance(System.Extender[typeof(ContentItem)] ?? typeof(ContentItem));
                TypeExtender.CopyExtensionData(data, contentItem);
                contentItem.DataType = data.GetType().UnextendedType().FullName;
                
                if (path == null || path == contentItem.Path)
                {
                    contentItem.SetContent(System, data);
                    return contentItem;
                }
            }

            // If we get to here, we can't find the path
            if (path == null)
                throw new ArgumentException("Cannot find path of " + data.GetType().FullName);

            // Now we have to get the content item from the db so we get the right ids etc
            var findPath = routePath ?? dataPath;
            var contentItems = System.Repository.Get<ContentItem>(data.GetType(), iq => iq.Where(ci => ci.Path == findPath)).ToList();
            if (contentItems.Count > 1)
                throw new Exception("Duplicate content items at " + findPath + " of type " + data.GetType().FullName);

            contentItem = contentItems.SingleOrDefault();
            // If we can't we build a new one
            if (contentItem == null)
            {
                contentItem = Repository.New<ContentItem>();
                contentItem.DataType = data.GetType().FullName;
            }

            contentItem.Path = path;

            contentItem.SetContent(System, data);

            if (data is ICoreMetadata)
                TypeExtender.CopyExtensionData(contentItem, data);

            return contentItem;
        }

        protected ContentItem GetNewRecord<T>(string path)
        {
            return GetNewRecord(typeof(T), path);
        }
        protected virtual ContentItem GetNewRecord(Type type, string path)
        {
            var newCI = System.Repository.New<ContentItem>();
            newCI.Path = path;
            newCI.DataType = type.FullName;
            Type extType = System.Extender[type] ?? type;
            var newContent = Activator.CreateInstance(extType);
            var address = new Address(type, path);
            address.SetAddressFields(newContent);
            if (newContent is ICoreMetadata)
                TypeExtender.CopyExtensionData(newCI, newContent);
            newContent = System.Events.ProcessEvent("ContentItem.New", this, newContent).Data;

            newCI.SetContent(System, newContent);
            // ensure it is created in the current version
            System.Versions.SetVersion(System.Versions.CurrentVersion, newCI);
            return newCI;
        }

        /// <inheritdoc/>
        public override Address GetAddress(Type type, RouteData rd)
        {
            var dict = new Dictionary<string, object>();
            int ord;
            rd.Values
                .Where(v => (v.Key.StartsWith("_")
                            && int.TryParse(v.Key.After("_").UpTo("-"), out ord)
                            && (v.Value ?? "").ToString() != ""))
                .Do(kvp => dict.Add(kvp.Key.UpTo("-").Replace("*", ""), kvp.Value ?? ""));
            var address = new Address(type, dict);
            return address.FixCase();
        }
        /// <inheritdoc/>
        public override Address GetAddress(object o)
        {
            // try to get address from fields on item marked with AddressComponentAttribute
            Address address = null;
            if (!(o is ContentItem))
                address = new Address(o);

            // if no such field, try path from metadata
            if ((address == null || address.Count == 0) && (o is ICoreMetadata))
            {
                string path = ((ICoreMetadata)o).Path;
                if (path == null)
                    return null;
                Type t = o is IContentContainer ? ((IContentContainer)o).ContentType : o.GetType();
                address = new Address(t, path);
            }

            return address.FixCase();
        }

        /// <inheritdoc/>
        public override object GetContainer(Address a, object o)
        {
            var ci = GetContentItem(a, o);
            var data = new Dictionary<string, object> { { "Item", o }, { "Container", ci } };
            ci = System.Events.ProcessEvent("Collator.GetContainer", this, data).GetDataItem<ContentItem>("Container");
            return ci;
        }

        protected override Type UnextendedContainerType(Type type)
        {
            return typeof(ContentItem);
        }

        /// <inheritdoc/>
        public override PropertyInfo GetIdProperty(Type t)
        {
            return t.GetProperty("Identity");
        }
    }
}
