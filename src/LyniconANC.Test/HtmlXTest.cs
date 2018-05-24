using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Linq;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Xunit;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    public class HtmlXTests
    {
        [Fact]
        public void TestPutInParas()
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(@"<p>test</p>");
            HtmlX.PutTextInParas(doc, doc.DocumentNode);
            Assert.Equal("<p>test</p>", doc.DocumentNode.OuterHtml);

            doc.LoadHtml(@"some stuff<br/><br/>more stuff");
            HtmlX.PutTextInParas(doc, null);
            Assert.Equal("<p>some stuff</p><p>more stuff</p>", doc.DocumentNode.OuterHtml);

            doc.LoadHtml(@"just text");
            HtmlX.PutTextInParas(doc, null);
            Assert.Equal("<p>just text</p>", doc.DocumentNode.OuterHtml);

            doc.LoadHtml(@"<p>embedded<br/><br/>another embedded<br>ok break</p>");
            HtmlX.PutTextInParas(doc, null);
            Assert.Equal("<p>embedded</p><p>another embedded<br>ok break</p>", doc.DocumentNode.OuterHtml);

            doc.LoadHtml(@"<p style='text-align: center'>centred</p>");
            HtmlX.PutTextInParas(doc, null);
            Assert.Equal("<p style='text-align: center'>centred</p>", doc.DocumentNode.OuterHtml);
        }

    }
}
