<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="demo_rpc_server_mvc" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title><%=Convert.ToString(ViewData["Title"]) %></title>
    <style type="text/css">
    html, body { height: 100%; overflow: auto; }
    body { padding: 0; margin: 0; }
    #silverlightControlHost { height: 100%; }
    </style>    
    <%=Html.Script("~/Scripts/Silverlight.js") %>
    <%=Html.Script("~/Scripts/SilverlightError.js") %>
</head>

<body>
    <!-- Runtime errors from Silverlight will be displayed here.
	This will contain debugging information and should be removed or hidden when debugging is completed -->
	<div id='errorLocation' style="font-size: small;color: Gray;"></div>

    <div id="silverlightControlHost">
		<object data="data:application/x-silverlight-2," type="application/x-silverlight-2" width="100%" height="100%">
			<param name="source" value="<%=Html.AbsolutePath(Convert.ToString(ViewData["Path"])) + "?" + new Random().Next(5000)%>" />
			<param name="onerror" value="onSilverlightError" />
			<param name="background" value="white" />
			<param name="minRuntimeVersion" value="2.0.31005.0" />
			<param name="autoUpgrade" value="true" />
			<a href="http://go.microsoft.com/fwlink/?LinkID=124807" style="text-decoration: none;">
     			<img src="http://go.microsoft.com/fwlink/?LinkId=108181" alt="Get Microsoft Silverlight" style="border-style: none"/>
			</a>
		</object>
		<iframe style='visibility:hidden;height:0;width:0;border:0px'></iframe>
    </div>
</body>
</html>