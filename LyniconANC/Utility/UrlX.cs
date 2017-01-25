using Lynicon.Extensibility;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Utility
{
    public static class UrlX
    {
        /// <summary>
        /// Convert all the urls in markup to absolute urls based on the current request host
        /// </summary>
        /// <param name="s">the markup</param>
        /// <returns>Markup with urls converted</returns>
        static public string ConvertUrlsToAbsolute(string s)
        {
            return ConvertUrlsToAbsolute(s, RequestBaseUrl());
        }
        /// <summary>
        /// Convert all the urls in markup to absolute urls based on the given base url
        /// </summary>
        /// <param name="s">the markup</param>
        /// <param name="siteBaseUrl">the base url for making urls absolute</param>
        /// <returns>Markup with urls converted</returns>
        static public string ConvertUrlsToAbsolute(string s, string siteBaseUrl)
        {
            StringBuilder res = new StringBuilder();

            int pos = 0;
            string section;
            string[] seps;
            while (true)
            {
                section = s.GetHead(ref pos, new string[] { "href=\"", "href='", "src=\"", "src='", "background=\"", "background='", "location.replace(\"", "location.replace('", "url: '", "url: \"", ":url('", ":url(\"" }, true);
                res.Append(section);
                if (pos == -1) break;
                if (pos < s.Length && s[pos] == '\"')
                    continue;
                seps = new string[] { StringX.Right(section, 1) }; // Ensure we are scanning for the single or double quote appropriately
                res.Append(UrlToAbsolute(s.GetHead(ref pos, seps, false), siteBaseUrl));
                res.Append(seps[0]);
            }

            return res.ToString();
        }

        /// <summary>
        /// Get the base url for the current request
        /// </summary>
        /// <returns>The base url for the current request</returns>
        static public string RequestBaseUrl()
        {
            StringBuilder res = new StringBuilder();
            HttpRequest req = RequestContextManager.Instance.CurrentContext.Request;
            res.Append(req.Scheme);
            res.Append("://");
            res.Append(req.Host);

            return res.ToString();
        }

        /// <summary>
        /// Convert a single url to absolute based on the current request host
        /// </summary>
        /// <param name="url">The url</param>
        /// <returns>The absolute url</returns>
        static public string UrlToAbsolute(string url)
        {
            return UrlToAbsolute(url, RequestBaseUrl());
        }
        /// <summary>
        /// Convert a single url to absolute based on the given base url
        /// </summary>
        /// <param name="url">The url</param>
        /// <param name="siteBaseUrl">The base url</param>
        /// <returns>The absolute url</returns>
        static public string UrlToAbsolute(string url, string siteBaseUrl)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("javascript:"))
                return url;

            if (url.StartsWith("//"))
                return siteBaseUrl.UpTo("//") + url;

            if (url.StartsWith("/"))
                return siteBaseUrl + url;

            var req = RequestContextManager.Instance.CurrentContext.Request;
            StringBuilder res = new StringBuilder(siteBaseUrl);
            res.Append(req.Path.Add(url));

            return res.ToString();
        }

        /// <summary>
        /// Given a url, ensure its query string contains a given key with a given value (avoiding duplicate keys)
        /// </summary>
        /// <param name="url">The url</param>
        /// <param name="key">The query string key</param>
        /// <param name="val">The query string value</param>
        /// <returns>The url with the query string containing the key with the value given</returns>
        static public string EnsureQueryKeyValue(string url, string key, string val)
        {
            string currQuery = url.After("?");
            string query;
            string newKeyVal = key + "=" + val;
            if (currQuery.Contains(key))
                query = currQuery
                    .Split('&')
                    .Select(w => w.StartsWith(key + "=") ? newKeyVal : w)
                    .Join("&");
            else
                query = string.IsNullOrEmpty(currQuery) ? newKeyVal : currQuery + "&" + newKeyVal;

            return url.UpToLast("?") + "?" + query;
        }

        /// <summary>
        /// Use an anonymous object to set query string key from property name and value from property value
        /// </summary>
        /// <param name="url">The original url</param>
        /// <param name="values">Object specifying query string keys/values</param>
        /// <returns>url with query string overridden with specified keys/values</returns>
        static public string OverrideQueryValues(string url, object values)
        {
            foreach (var prop in values.GetType().GetProperties())
            {
                object val = prop.GetValue(values);
                if (val != null)
                    url = EnsureQueryKeyValue(url, prop.Name, prop.GetValue(values).ToString());
            }
                
            return url;
        }

        /// <summary>
        /// Take a list of strings, for each string in the list, replace the given string with each of the list of
        /// given replacements, adding the result each time to a new list.
        /// </summary>
        /// <param name="strings">Original list of strings</param>
        /// <param name="replace">substring of each string to replace</param>
        /// <param name="replaceWiths">successive replacements for each substring in each string</param>
        static public void PermuteReplace(List<string> strings, string replace, List<string> replaceWiths)
        {
            if (replaceWiths.Count == 0 || replace == null)
                return;

            int nOrig = strings.Count;
            for (int i = 0; i < nOrig; i++)
                foreach (string replaceWith in replaceWiths.Skip(1))
                    strings.Add(strings[i].Replace(replace, replaceWith));
            for (int i = 0; i < nOrig; i++)
                strings[i] = strings[i].Replace(replace, replaceWiths[0]);
        }

        /// <summary>
        /// Turn a title into a url slug
        /// </summary>
        /// <param name="s">Title</param>
        /// <returns>url slug</returns>
        static public string Urlise(string s)
        {
            return s.ToLower()
                .Replace(" ", "-")
                .Replace("?", "")
                .Replace("/", "")
                .Replace("&", "-and-")
                .Replace("=", "");
        }
    }
}
