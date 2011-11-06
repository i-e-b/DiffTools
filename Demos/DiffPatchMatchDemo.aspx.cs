using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using DiffMatchPatch;

namespace Demos {
	public partial class DiffPatchMatchDemo : Page {
		protected void Page_Load (object sender, EventArgs e) {
			string left = File.ReadAllText(Server.MapPath("~/Examples/DeOfficiis.txt"));
			
			//string right = File.ReadAllText(Server.MapPath("~/Examples/DeOfficiis_Altered.txt"));
			//string right = File.ReadAllText(Server.MapPath("~/Examples/DeOfficiis_Minor_Altered.txt"));
			string right = File.ReadAllText(Server.MapPath("~/Examples/TotallyDifferent.txt"));

			GC.Collect(3, GCCollectionMode.Forced); // give the algorithm a fair shot...
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			var differences = new diff_match_patch().diff_main(left, right);

			sw.Stop();

			Repeater1.DataSource = from d in differences
								   select new {
									   TypeString = d.operation.ToString(),
									   SplitPart = HttpContext.Current.Server.HtmlEncode(d.text).Replace("\n", "<br/>")
								   };

			Repeater1.DataBind();

			Response.Write("DocDiff took " + sw.Elapsed.TotalSeconds + " seconds<br/>");

		}
	}
}