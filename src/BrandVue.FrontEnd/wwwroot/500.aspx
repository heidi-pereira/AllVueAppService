<%@ Page Language="C#" %>
<% 
    Response.StatusCode = 500; 
    Server.Transfer("~/500.html");
%>