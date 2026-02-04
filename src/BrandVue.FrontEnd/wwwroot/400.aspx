<%@ Page Language="C#" %>
<% 
    Response.StatusCode = 400; 
    Server.Transfer("~/400.html");
%>