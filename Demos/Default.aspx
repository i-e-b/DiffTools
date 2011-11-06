<%@ Page Language="C#" Inherits="System.Web.UI.Page" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>Diff Test Page</title>
</head>
<body>
    <table style="width:100%;height:500px">
	<tr>
	<td><iframe src="DocDiff.aspx" style="width:100%;height:500px"></iframe></td>
	<td><iframe src="DiffPatchMatchDemo.aspx" style="width:100%;height:500px"></iframe></td>
	</tr>
	</table>
</body>
</html>