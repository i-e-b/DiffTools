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
			string right = File.ReadAllText(Server.MapPath("~/Examples/DeOfficiis_Altered.txt"));

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


			// This gives a code to convert LEFT into RIGHT
			// Is a revision repository, the older file would be RIGHT, and the newer be LEFT.
			/*sw.Reset();
			sw.Start();
			byte[] final_out = DiffCode.StorageDiffCode(differences);
			sw.Stop();
			Response.Write("DocDiff-code took " + sw.Elapsed.TotalSeconds + " seconds<br/>");

			double sizePc = ((double)final_out.Length / right.Length) * 100.0;
			Response.Write("<br/>DocDiff code size: " + (final_out.Length / 1024) + "KiB which is " + sizePc.ToString("0.0") + "% of the resulting file");
			Response.Write("<br/>diff code contains " + differences.Count() + " alterations.");

			sw.Reset();
			sw.Start();
			File.WriteAllText(Server.MapPath("~/Examples/DeOfficiis_Recombined.txt"),
				DiffCode.BuildRevision(left, DiffCode.BuildDiffCode(differences)));
			sw.Stop();
			Response.Write("<br/>Rebuild and write took " + sw.Elapsed.TotalSeconds + " seconds<br/>");*/

		}
	}
}