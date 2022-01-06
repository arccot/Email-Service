<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SubmitEmail.aspx.cs" Inherits="Email_API.SubmitEmail" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" method="post" action="api/email/" 
        enctype="application/x-www-form-urlencoded">
        <div>
            <label for="destination">Recipient Email Address</label>
        </div>
        <div>
            <input name="destination" type="text" id="rec"/>
        </div>
        <div>
            <label for="subject">Subject</label>
        </div>
        <div>
            <input name="subject" type="text" id="sub"/>
        </div>
        <div>
            <label for="body">Body</label>
        </div>
        <div>
            <input name="body" type="text" id="body"/>
        </div>
        <div>
            <input type="submit" value="Submit" />
        </div>
    </form>
</body>
</html>
