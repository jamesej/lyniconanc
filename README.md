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
unopinionated CMS which is lightweight and low impact yet full featured.

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

For examples, look in the [Startup.cs file](blob/master/src/LyniconANC.Release/Startup.cs),
[TileContent.cs file](blob/master/src/LyniconANC.Release/Models/TileContent.cs) and
[TileController.cs](blob/master/src/LyniconANC.Release/Controllers/TileController.cs).

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

### Running the tests

The tests should appear in the Test Explorer as normal in Visual Studio. If they are not
there this is likely an issue with the XUnit test framework. Sometimes such
issues can be resolved by cleaning and rebuilding the solution,
and closing and reopening Visual Studio.

Tests can also be run as normal in .net core on the command line
by going to the top-level installation directory
in a command window and running `dotnet test`.





