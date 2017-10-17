# Lynicon CMS/DMS
*For ASP.Net Core (.Net Standard 2.0, .Net 4.6.1 and .Net Standard 1.6, .Net 4.6)*

![Lynicon CMS](http://www.lynicon.com/images/lynicon/twitter-logo.png)

Now we believe the most powerful CMS on .Net Core.

*It can be this easy to add content management to your site*

<pre style="width:49%; float:left; margin-right:2%;">
routes.MapRoute("articles", "article/index", new { controller = "Pages", action = "Index" });

public IActionResult Index()
{
  var data = new ModelType();
  return View(data);
}
</pre>

<pre style="width:49%;">
routes.<i>MapDataRoute&lt;ModelType&gt;</i>("articles", "article/<i>{_0}</i>", new { controller = "Pages", action = "Index" });

public IActionResult Index(<i>ModelType data</i>)
{
  return View(data);
}

</pre>

## Introduction

In tune with the .Net Core philosophy, Lynicon is a composable and
unopinionated CMS which is lightweight and low impact yet full featured. It supports
fully structured content defined as C# classes which can have properties which are subtypes
or lists. Generally it maps one content item to a page. It has no assumed tree structure for
content, content relationship is defined by foreign key fields as in a relational database,
with built-in facilities for traversing these relationships in both directions. Content
navigation is done via a powerful filtering/search system, or via the site itself.

Content storage is highly flexible, it can be run without a database, with a SQL or other database,
or customised to use almost any data source.

Delivered as a Nuget package or library, it will not get in the way of you using any other technology, or
force you to use its features, and can even be added into existing projects. It is
highly extensible with a powerful module system allowing you to remove unneeded features
to decrease complexity and increase efficiency.

The content editor is shown alongside the page being edited so the effects of content
changes are immediately visible. The rest of the backend is very straightforward as it
does not attempt to provide the generally unneeded facilities to change content structure
or front-end layout, this is done in code.

This CMS project provides the essential CMS functionality for the Lynicon in
ASP.Net Core, perfectly adequate for a smaller site or application. 
There is an MVC 5 version [here](https://github.com/jamesej/lynicon). 
The project site is [here](http://www.lynicon.com), and this project builds a
NuGet package whose page on Nuget is [here](https://www.nuget.org/packages/LyniconANC).
Documentation on Confluence is [here](https://lynicon.atlassian.net/wiki/display/DOC/ASP.Net+Core+Version).  We welcome feedback to info@lynicon.com, and you can sign up for news and the Slack support channel on [this page](http://www.lynicon.com/get-lynicon).

We have now released a module package supplying the major features needed
for a larger-scale CMS including caching, search, publishing,
url management etc.
This is available [here](http://www.lynicon.com/lynicon-base)
(closed source/paid for)

## Table of Contents

1. [Setup](#setup)
2. [How Tos](#how-tos)
    1. [Log in as admin](#log-in-as-admin)
    2. [Create a content managed route, controller & view](#create-a-content-managed-route-controller-view)
    3. [Add and edit content items](#add-and-edit-content-items)
    4. [Use HTML snippets, images, links etc in your content class](#use-html-snippets-images-links-etc-in-your-content-class)
    5. [Use lists and subtypes in your content class](#use-lists-and-subtypes-in-your-content-class)
    6. [Link to other content items in your content class](#link-to-other-content-items-in-your-content-class)
    7. [Filter, search and report on content](#filter-search-and-report-on-content)
    8. [Use the content API to create a list of items dynamically](#use-the-content-api-to-create-a-list-of-items-dynamically)
    9. [Use property source redirection to create site-wide fields with values constant across the site](#use-property-source-redirection-to-create-site-wide-fields-with-values-constant-across-the-site)
    10. [Administer site users](#administer-site-users)
    11. [Run without a database](#run-without-a-database)
    12. [Add a JSON API](#add-a-json-api)
3. [Running the tests](#running-the-tests)
4. [Contributing](#contributing)

## Setup

Because .net core 2.0 has breaking changes, the repository contains two branches, `Core1.1` for .net core 1.1, and `master` is now .net 2.0. Make
sure you check out the right branch.

Once you have cloned the repository, you will need to get the test site working on your machine.
Set the connection string in appsettings.json
![Appsettings](http://www.lynicon.com/install/ANC17_ConnectionString.jpg)

You can now set up the database by running the test site from the command line (a handy feature of
an ASP.Net Core site!). Open a command window as Administrator and go to the \src\LyniconANC.Release
directory. Now run `dotnet run -- --lynicon initialize-database`.

Then you can set up the CMS admin user. Run `dotnet run -- --lynicon initialize-admin --password p4ssw0rd`.
![Initialize](http://www.lynicon.com/install/ANC2_InitializeProject.jpg)

You can now run the site and login with the password you set up (the email is admin@lynicon-user.com)

If you would like to populate your database with sample content, there is a script at `\src\LyniconANC.Release\Areas\Lynicon\Admin\SQL\TilesSiteContentSetup.sql`
which will create example content for the test site.

## How Tos

### Log in as admin

Lynicon configures it's own customised ASP.Net Identity implementation. This means you can log
in via the (slightly modified) template code login page, using the admin email (admin@lynicon-user)
and the password you configured above. Alternatively you can use Lynicon's built in
login page at `/Lynicon/Login`

### Create a content-managed route, controller & view

In Lynicon, you define a route as supporting content management, which then
passes an instance of a content class into the controller, which
passes it on to the view to display. This is described further
in the [online manual](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/42598494/Content+Routing).

For examples, look in the [Startup.cs file](src/LyniconANC.Release/Startup.cs),
[TileContent.cs file](src/LyniconANC.Release/Models/TileContent.cs) and
[TileController.cs](src/LyniconANC.Release/Controllers/TileController.cs).

### Add and edit content items

Content items are listed and can be added at /lynicon/items.
Content items can be edited by visiting a url with which the content item
is associated while logged in with the appropriate rights. The content editor
panel is shown.
This is described in detail in the [online manual](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/42795022/User+Manual)

### Use HTML snippets, images, links etc in your content class

Since Lynicon uses C# classes to define the content schema, it provides standard classes
such as HtmlMin, Image, Link for storing data required in content management. This is described
in the [online manual](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/42926142/Predefined+content+subtypes).
The built-in asset handling system allows upload of images or other assets through a
Windows Explorer-style popup to a specified folder in the site.

### Use lists and subtypes in your content class

Content classes in Lynicon can have properties of an arbitrary subtype, or List<T> properties
of arbitrary type T. The content editor manages this automatically. You can create custom editors
for subtypes using the MVC templating system, by adding to the templates named after various
content subtypes which already exist in `Areas/Lynicon/Views/Shared/EditorTemplates`.
This is described in detail in the [online manual](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/42795058/Customising+Editors)

### Link to other content items in your content class

Another standard property class in Lynicon is Reference<T>. This stores the id of another content
item in your content class. It appears in the editor as a drop-down list of content items of
type T. In code you can retrieve the referenced content item (actually, it's summary, see below)
as a property on Reference<T> or you can get all content items with a reference to another item.
Examples can be seen in [Tile.cshtml](src/LyniconANC.Release/Views/Tile/Tile.cshtml)
and [TileMaterialContent.cs](src/LyniconANC.Release/Models/TileMaterialContent.cs).
See the [online manual](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/42598512/Relations) for more depth.

### Filter, search and report on content

Lynicon contains a page with various filters for creating, viewing, locating and reporting
on content. This is at `/lynicon/items/list` or reached by clicking the Filter button
on the bottom control bar on CMS pages. You can build custom filters which can be added to
the list available on this page. See the [online manual](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/43057157/The+Filter+Page)

### Use the content API to create a list of items dynamically

Lynicon has a clean and powerful content API you can use in code to retrieve content directly. You can
see an example in [HomeContent.cs](src/LyniconANC.Release/Models/HomeContent.cs). Generally when working with
content objects external to building the page that displays them, you use a [Summary Type](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/42664002/Summaries)
which contains the subset of the full content object's properties for efficiency. The content API uses
linq for filtering, can run queries across multiple data sources and lets you retrieve all content whose content
type implements an interface or inherits from a base type.

### Use property source redirection to create site-wide fields with values constant across the site

Lynicon has a powerful means of combining content from different sources in order to build a content item. One
use of this is to have a content item storing site-wide values, and have its fields be mapped into every
content item on the site, e.g. for the url of the logo on the top banner. This is done in the example site by having a shared base type for all content on the
site and using this property source redirection method to map fields on the base type to a single shared content
item.  You can see how the base type is set up at [TilesPageBase.cs](src/LyniconANC.Release/Models/TilesPageBase.cs).
The shared fields are held in [CommonContent.cs](src/LyniconANC.Release/Models/CommonContent.cs).

### Administer site users

Lynicon has an admin page at `/lynicon/users` which allows you to administer site users if you have admin
privileges.  See [the online manual](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/42827792/The+Users+Page)
for more on this.

### Run without a database

The (closed-source but free) Lynicon.Extra package on [Nuget](https://www.nuget.org/packages/LyniconANC.Extra/) provides
the Storeless module which converts Lynicon to run with CMS data in memory, with backup persistence to a JSON file.
See [the online manual](https://lynicon.atlassian.net/wiki/spaces/LAC/pages/73957380/Storeless) for how to set this
up - it's very simple and reduces hosting costs while making Lynicon run super fast for websites up to 500 or 1000 pages.

### Add a JSON API

If you want to get your content as JSON (or any other standard web format), the combination of ASP.Net Core and
Lynicon makes this very easy and flexible. To do this for content type T:

* Add a data route typed as `List<T>` [(see the Startup.cs file in the test project)](src/LyniconANC.Release/startup.cs)
* In the controller/action this points to, ensure there's an action parameter `List<T> data`. [(see ApiController)](src/LyniconANC.Release/Controllers/ApiController.cs)
* In the action method return `Ok(data)` (or the result of any code which processes the data)

In the example you can now call /api/tiles with any standard OData filtering, paging or sorting parameters to receive
a json array of tile content serialized to JSON.

## Running the tests

The tests should appear in the Test Explorer as normal in Visual Studio. If they are not
there this is likely an issue with the XUnit test framework. Sometimes such
issues can be resolved by cleaning and rebuilding the solution,
and closing and reopening Visual Studio.

Tests can also be run as normal in .net core on the command line
by going to the top-level installation directory
in a command window and running `dotnet test`.

## Contributing

We welcome all pull requests, comments, issues etc. You can also get in touch at [info@lynicon.com](mailto:info@lynicon.com)





