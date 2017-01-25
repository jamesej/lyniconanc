<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<dynamic>" %>
<%@ Import Namespace="Lynicon.Extensibility" %>

<% bool isAlt = false; foreach (var viewKvp in LyniconUi.Instance.RevealPanelViews)
   {%>
    <div class="reveal-panel-view <%= isAlt ? "alt" : "" %>">
       <%= Html.Partial(viewKvp.Value) %>
       <div style="clear:both"></div>
    </div>

   <%
   isAlt = !isAlt;}
%>


