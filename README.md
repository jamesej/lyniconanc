# Lynicon CMS/DMS
*For ASP.Net Core (.Net Standard 1.6, .Net 4.6)*

![Lynicon CMS](http://www.lynicon.com/images/lynicon/twitter-logo.png)

Lynicon CMS for ASP.Net Core (.Net Standard 1.6, .Net 4.6).
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

Once you have cloned the repository, you will need to get the test site working on your machine.
Set the connection string in appsettings.json
![Appsettings](http://www.lynicon.com/install/ANC17_ConnectionString.jpg)

Now build the test site.

You can now set up the database by running the test site from the command line (a handy feature of
an ASP.Net Core site!). Open a command window as Administrator and go to the \src\LyniconANC.Release
directory. Now run `dotnet run lynicon initialize-database`.


