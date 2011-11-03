using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace DocDiff {
	public partial class _Default : System.Web.UI.Page {

		protected void Page_Load (object sender, EventArgs e) {
			string left = File.ReadAllText(Server.MapPath("~/Examples/DeOfficiis.txt"));
			//string left = File.ReadAllText(Server.MapPath("~/Examples/DeOfficiis_Recombined.txt"));

			string right = File.ReadAllText(Server.MapPath("~/Examples/DeOfficiis_Altered.txt"));
			//string right = File.ReadAllText(Server.MapPath("~/Examples/DeOfficiis_Minor_Altered.txt"));
			//string right = File.ReadAllText(Server.MapPath("~/Examples/TotallyDifferent.txt"));


			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			var differences = new Diff(left, right, Diff.PerWord);

			sw.Stop();

			Repeater1.DataSource = from d in differences
			                       select new Diff.Fragment(d.Type, 
									   HttpContext.Current.Server.HtmlEncode(d.SplitPart).Replace("\n", "<br/>"), d.Position);
			Repeater1.DataBind();

			Response.Write("Diff took " + sw.Elapsed.TotalSeconds + " seconds<br/>");


			// This gives a code to convert LEFT into RIGHT
			// Is a revision repository, the older file would be RIGHT, and the newer be LEFT.
			sw.Reset();
			sw.Start();
			byte[] final_out = DiffCode.StorageDiffCode(differences);
			sw.Stop();
			Response.Write("Diff-code took " + sw.Elapsed.TotalSeconds + " seconds<br/>");
			
			double sizePc = ((double)final_out.Length / right.Length) * 100.0;
			Response.Write("<br/>Diff code size: " + (final_out.Length / 1024) + "KiB which is " + sizePc.ToString("0.0") + "% of the resulting file");
			Response.Write("<br/>diff code contains " + differences.Count() + " alterations.");

			sw.Reset();
			sw.Start();
			File.WriteAllText(Server.MapPath("~/Examples/DeOfficiis_Recombined.txt"),
				DiffCode.BuildRevision(left, DiffCode.BuildDiffCode(differences)));
			sw.Stop();
			Response.Write("<br/>Rebuild and write took " + sw.Elapsed.TotalSeconds + " seconds<br/>");
		}

		

	}
}
