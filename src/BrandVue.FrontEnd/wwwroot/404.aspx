<%@ Page Language="C#" %>
<% 
    Response.StatusCode = 404; 
    Server.Transfer("~/404.html");
%>