using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Lynicon.Attributes;
using LyniconANC.Test.Models;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Membership;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Routing;
using Lynicon.Utility;
using Newtonsoft.Json;
using NUnit.Framework;
using Microsoft.AspNetCore.Routing;
using System.Reflection;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    internal class TestContainer : IContentContainer, IPublishable, IInternational
    {
        public bool? IsPubVersion { get; set; }

        #region IContentContainer Members

        public object GetContent()
        {
            throw new NotImplementedException();
        }

        public Summary GetSummary()
        {
            throw new NotImplementedException();
        }

        public void SetContent(object o)
        {
            throw new NotImplementedException();
        }

        public Type ContentType { get; set; }

        #endregion

        #region IInternational Members

        public string Locale { get; set; }

        #endregion
    }

    internal class TestContent : BaseContent
    {
        public string X { get; set; }
    }

        /// <summary>
    /// Interface for a container which supports publishing via the Publishing module
    /// </summary>
    public interface IPublishable
    {
        /// <summary>
        /// Flag which says whether this is the published version
        /// </summary>
        bool? IsPubVersion { get; set; }
    }

    public interface IInternational
    {
        string Locale { get; set; }
    }

    /// <summary>
    /// Versioner for the Publishing version system
    /// </summary>
    public class PublishingVersioner : Versioner
    {
        public ItemVersion PublishedVersion { get; set; }

        public override object PublicVersionValue
        {
            get
            {
                return PublishedVersion[VersionKey];
            }
        }

        public override string VersionKey
        {
            get { return "Published"; }
        }

        public override bool IsAddressable
        {
            get { return false; }
        }

        public override object[] AllVersionValues
        {
            get { return new object[] { true, false }; }
        }

        public PublishingVersioner() : base()
        {
            PublishedVersion = new ItemVersion { { VersionKey, true } };
        }
        public PublishingVersioner(Func<Type, bool> isVersionable) : base(isVersionable)
        {
            PublishedVersion = new ItemVersion { { VersionKey, true } };
        }

        public override bool Versionable(Type type)
        {
            Type containerType = Collator.Instance.ContainerType(type);
            if (containerType != type && this.IsVersionable != null && !this.IsVersionable(type))
                return false;
            return typeof(IPublishable).IsAssignableFrom(containerType);
        }

        public override void SetCurrentVersion(VersioningMode mode, ItemVersion version)
        {
            bool isEditor = false;
            try
            {
                isEditor = SecurityManager.Current.CurrentUserInRole("E");
            }
            catch { }

            if (!isEditor || mode == VersioningMode.Public)
                version[VersionKey] = true;
            else
                version[VersionKey] = false;
        }

        public override void GetItemVersion(object container, ItemVersion version)
        {
            if (((IPublishable)container).IsPubVersion.HasValue)
                version[VersionKey] = ((IPublishable)container).IsPubVersion;
        }

        public override void SetItemVersion(ItemVersion version, object container)
        {
            if (container is IPublishable)
                ((IPublishable)container).IsPubVersion = (bool)version[VersionKey];
        }

        public override VersionDisplay DisplayItemVersion(ItemVersion version)
        {
            return new VersionDisplay
            {
                Text = (bool)version[VersionKey] ? "P" : "U",
                CssClass = "publishing-version-display",
                Title = (bool)version[VersionKey] ? "Published" : "Unpublished",
                ListItem = (bool)version[VersionKey] ?  "Published" : "Unpublished"
            };
        }

        public override bool TestVersioningMode(object container, VersioningMode mode)
        {
            var iPub = container as IPublishable;
            switch (mode)
            {
                case VersioningMode.Public:
                    return (iPub.IsPubVersion ?? true);
                case VersioningMode.Current:
                    if (VersionManager.Instance.CurrentVersion.ContainsKey(VersionKey))
                        return (iPub.IsPubVersion == null || iPub.IsPubVersion == (bool)VersionManager.Instance.CurrentVersion[VersionKey]);
                    break;
                case VersioningMode.Specific:
                    if (VersionManager.Instance.SpecificVersion.ContainsKey(VersionKey))
                        return (iPub.IsPubVersion == null || iPub.IsPubVersion == (bool)VersionManager.Instance.SpecificVersion[VersionKey]);
                    break;
            }

            return true;
        }

        public override List<object> GetAllowedVersions(IUser u)
        {
            if (u != null && u.Roles.Contains(User.EditorRole))
                return new List<object> { true, false };
            else
                return new List<object> { true };
        }
    }

    public class I18nVersioner : Versioner
    {
        string[] localeSet;
        string localeRouteKey;
        string defaultLocale;
        Func<string, string> routeLocaleFromLocale;

        public override string VersionKey
        {
            get { return "Locale"; }
        }

        public override bool IsAddressable
        {
            get { return true; }
        }

        public override object[] AllVersionValues
        {
            get { return localeSet.Cast<object>().ToArray(); }
        }

        public I18nVersioner(string[] localeSet, string localeRouteKey, string defaultLocale, Func<string, string> routeLocaleFromLocale)
            : base()
        {
            this.localeSet = localeSet;
            this.localeRouteKey = localeRouteKey;
            this.defaultLocale = defaultLocale;
            this.routeLocaleFromLocale = routeLocaleFromLocale;
        }
        public I18nVersioner(string[] localeSet, string localeRouteKey, string defaultLocale, Func<string, string> routeLocaleFromLocale, Func<Type, bool> isVersionable)
            : base(isVersionable)
        {
            this.localeSet = localeSet;
            this.localeRouteKey = localeRouteKey;
            this.defaultLocale = defaultLocale;
            this.routeLocaleFromLocale = routeLocaleFromLocale;
        }

        public override bool Versionable(Type type)
        {
            Type containerType = Collator.Instance.ContainerType(type);
            if (containerType != type && this.IsVersionable != null && !this.IsVersionable(type))
                return false;
            return typeof(IInternational).IsAssignableFrom(containerType);
        }

        public override void SetCurrentVersion(VersioningMode mode, ItemVersion version)
        {
            var rd = RouteX.CurrentRouteData();
            string locale;
            if (rd == null || rd.Values[localeRouteKey] == null)
                locale = defaultLocale;
            else
            {
                var routeLocale = (string)rd.Values[localeRouteKey];
                locale = routeLocaleFromLocale(routeLocale);
            }

            version[VersionKey] = locale;
        }

        public override void GetItemVersion(object container, ItemVersion version)
        {
            version[VersionKey] = (container as IInternational).Locale;
        }

        public override void SetItemVersion(ItemVersion version, object container)
        {
            ((IInternational)container).Locale = (string)version[VersionKey];
        }

        public override VersionDisplay DisplayItemVersion(ItemVersion version)
        {
            return new VersionDisplay
            {
                Text = (string)version[VersionKey],
                CssClass = "i18n-version-display",
                Title = "Locale of item",
                ListItem = (string)version[VersionKey]
            };
        }

        public override bool TestVersioningMode(object container, VersioningMode mode)
        {
            if (!(container is IInternational))
                return true;

            var iI = container as IInternational;
            switch (mode)
            {
                case VersioningMode.Public:
                    return true;
                case VersioningMode.Current:
                    return true;
                case VersioningMode.Specific:
                    if (VersionManager.Instance.SpecificVersion.ContainsKey(VersionKey))
                        return (iI.Locale == (string)VersionManager.Instance.SpecificVersion[VersionKey]);
                    break;
            }

            return true;
        }

        public override List<object> GetAllowedVersions(IUser u)
        {
            return localeSet.Cast<object>().ToList();
        }

        public override string GetVersionUrl(string currUrl, ItemVersion version)
        {
            if (!version.ContainsKey(VersionKey))
                return currUrl;

            string locale = (string)version[VersionKey];
            RouteData rd = RouteX.GetRouteDataByUrl(currUrl);
            string routeLocale = routeLocaleFromLocale(locale);
            if (!rd.Values.ContainsKey(localeRouteKey) || (string)rd.Values[localeRouteKey] == routeLocale)
                return currUrl;

            rd.Values[localeRouteKey] = routeLocale;
            new string[] { "data", "originalAction", "originalController", "originalArea" }
                .Where(k => rd.Values.ContainsKey(k))
                .Do(k => rd.Values.Remove(k));
            string url = RouteX.CreateUrlFromRouteValues(rd.Values);

            return url;
        }

        public override object PublicVersionValue
        {
            get { return null; }
        }
    }

    public class TestCollator : ICollator
    {
        #region ICollator Members

        public Repository Repository { get; set; }

        public Type AssociatedContainerType
        {
            get { return typeof(TestContainer); }
        }

        public IEnumerable<T> Get<T>(IEnumerable<Address> addresses) where T : class
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Get<T>(IEnumerable<ItemId> ids) where T : class
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Get<T, TQuery>(IEnumerable<Type> types, Func<IQueryable<TQuery>, IQueryable<TQuery>> queryBody)
            where T : class
            where TQuery : class
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetList<T, TQuery>(IEnumerable<Type> types, RouteData rd)
            where T : class
            where TQuery : class
        {
            throw new NotImplementedException();
        }

        public T GetNew<T>(Address a) where T : class
        {
            throw new NotImplementedException();
        }

        public bool Set(Address a, object data, Dictionary<string, object> setOptions)
        {
            throw new NotImplementedException();
        }

        public void Delete(Address a, object data, bool bypassChecks)
        {
            throw new NotImplementedException();
        }

        public void MoveAddress(ItemId id, Address moveTo)
        {
            throw new NotImplementedException();
        }

        public Address GetAddress(Type type, RouteData rd)
        {
            throw new NotImplementedException();
        }

        public Address GetAddress(object data)
        {
            throw new NotImplementedException();
        }

        public T GetSummary<T>(object item) where T : class
        {
            throw new NotImplementedException();
        }

        public object GetContainer(Address a, object o)
        {
            var tc = new TestContainer();
            new ItemVersion(o).SetOnItem(tc);
            return tc;
        }

        public Type ContainerType(Type type)
        {
            return typeof(TestContainer);
        }

        public System.Reflection.PropertyInfo GetIdProperty(Type t)
        {
            throw new NotImplementedException();
        }

        #endregion
    }


    [TestFixture]
    public class VersionTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            VersionManager.Instance.RegisterVersion(new PublishingVersioner(t => t == typeof(HeaderContent)));
            VersionManager.Instance.RegisterVersion(new I18nVersioner(new string[] { "en-GB", "es-ES" }, "locale", "en-GB", s => s));

            Collator.Instance.SetupType<TestContent>(new TestCollator(), null);
        }

        [Test]
        public void ItemVersionEquality()
        {
            Dictionary<string, object> vers = new Dictionary<string, object> { { "Existence", "Exists" }, { "Published", false } };
            var ii0 = new ItemVersion(vers);
            var ii1 = new ItemVersion(vers);
            vers["Published"] = true;
            var ii2 = new ItemVersion(vers);
            vers.Remove("Published");
            var ii3 = new ItemVersion(vers);
            vers["Partition"] = null;
            var ii4 = new ItemVersion(vers);
            var ii5 = new ItemVersion(vers);

            Assert.IsTrue(ii0.Equals(ii1), ".Equals true");
            Assert.IsTrue(ii0 == ii1, "== true");
            Assert.IsFalse(ii0.Equals(ii2), ".Equals false by different val");
            Assert.IsFalse(ii0 == ii2, "== false by different val");
            Assert.IsFalse(ii1.Equals(ii3), ".Equals false by missing key");
            Assert.IsFalse(ii1 == ii3, "== false by missing key");

            Assert.IsFalse(ii0.GetHashCode() == ii2.GetHashCode(), "hash code by val");
            Assert.IsFalse(ii1.GetHashCode() == ii3.GetHashCode(), "hash code by missing key");

            Assert.IsTrue(ii3 == ii4, "== ignore null value");
            Assert.IsTrue(ii3.GetHashCode() == ii4.GetHashCode(), "hash code ignore null value");

            Assert.IsTrue(ii5 == ii4, "== compare null value");
            Assert.IsTrue(ii5.GetHashCode() == ii4.GetHashCode(), "hash code compare null value");
        }

        [Test]
        public void ItemVersionConstructors()
        {
            var iv0 = new ItemVersion();

            var cont = new TestContainer { ContentType = typeof(HeaderContent), IsPubVersion = true, Locale = "en-GB" };
            var iv1 = new ItemVersion(cont);
            Assert.AreEqual(true, iv1["Published"]);
            Assert.AreEqual("en-GB", iv1["Locale"]);

            var iv2 = new ItemVersion(iv1.ToString());
            Assert.AreEqual(iv2["Published"], iv1["Published"]);
            Assert.AreEqual(iv2["Locale"], iv1["Locale"]);
            Assert.AreEqual(iv2, iv1);

            var iv3 = new ItemVersion(iv2);
            Assert.AreEqual(iv3, iv2);
            iv3["Published"] = false;
            Assert.AreNotEqual(iv3, iv2);

            var iv4 = new ItemVersion(new Dictionary<string, object> { { "sval", "abc" }, { "ival", 15 } });
            var json = JsonConvert.SerializeObject(iv4);
            var iv5 = JsonConvert.DeserializeObject<ItemVersion>(json);
            Assert.AreEqual((int)15, iv5["ival"]);

            // can construct using dictionary initializer
            var iv6 = new ItemVersion { { "A", 1 }, { "B", false } };
        }

        [Test]
        public void ItemVersionOperations()
        {
            // means published English vsn
            var iv1 = new ItemVersion { { "Published", true }, { "Locale", "en-GB" } };
            // means unpublished vsn used for all locales
            var iv2 = new ItemVersion { { "Published", false }, { "Locale", null } };
            // means Spanish vsn of type which is not versionable for publishing
            var iv3 = new ItemVersion { { "Locale", "es-ES" } };

            var iv4 = iv1.GetAddressablePart();
            Assert.AreEqual(new ItemVersion { { "Locale", "en-GB" } }, iv4);

            var iv5 = iv1.GetUnaddressablePart();
            Assert.AreEqual(new ItemVersion { { "Published", true } }, iv5);

            var iv6 = iv1.GetApplicablePart(typeof(TestContent));
            Assert.AreEqual(new ItemVersion { { "Locale", "en-GB" } }, iv6);

            var iv7 = iv1.Superimpose(new ItemVersion { { "Published", false }, { "A", "x" } });
            Assert.AreEqual(true, iv1["Published"]); // does not mutate iv1
            Assert.AreEqual(new ItemVersion { { "Published", false }, { "Locale", "en-GB" }, { "A", "x" } }, iv7);

            var iv9 = new ItemVersion(iv1);
            iv9.Add("X", null);

            var iv8 = iv9.Overlay(new ItemVersion { { "Published", false }, { "Locale", null } });
            Assert.AreEqual(new ItemVersion { { "Published", false }, { "Locale", null }, { "X", null } }, iv8);
            Assert.AreEqual("en-GB", iv9["Locale"]); // does not mutate iv1

            var iv10 = iv9.Mask(new ItemVersion { { "Published", null }, { "Locale", "es-ES" } });
            Assert.AreEqual(new ItemVersion { { "Published", true }, { "Locale", "es-ES" } }, iv10);
            Assert.AreEqual("es-ES", iv10["Locale"]);
        }
    }
}
