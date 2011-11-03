<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DocDiff._Default" EnableViewState="false" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Diff Test Page</title>
    <style type="text/css">
		.i {color:black; background-color:#80FF80; padding:0px; margin:0px;}
		.d {color:#FFa0a0; background-color:inherit; padding:0px; margin:0px;}
		.u {color:#707070; background-color:inherit; padding:0px; margin:0px;}
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <asp:Repeater ID="Repeater1" runat="server">
    <ItemTemplate>
    <span class='<%# Eval("TypeString") %>'><%# Eval("SplitPart")%></span></ItemTemplate>
	</asp:Repeater>
    </div>
    <hr />
    <div>
    <asp:Literal ID="redecoded" runat="server"></asp:Literal>
    </div>
    </form>
</body>
</html>
