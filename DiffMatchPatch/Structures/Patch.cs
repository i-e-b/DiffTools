using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace DiffMatchPatch {
	public class Patch {
		public List<Diff> diffs = new List<Diff>();
		public int start1;
		public int start2;
		public int length1;
		public int length2;

		/**
		 * Emmulate GNU diff's format.
		 * Header: @@ -382,8 +481,9 @@
		 * Indicies are printed as 1-based, not 0-based.
		 * @return The GNU diff string.
		 */
		public override string ToString () {
			string coords1, coords2;
			switch (length1) {
				case 0:
					coords1 = start1 + ",0";
					break;
				case 1:
					coords1 = Convert.ToString(start1 + 1);
					break;
				default:
					coords1 = (start1 + 1) + "," + length1;
					break;
			}
			switch (length2) {
				case 0:
					coords2 = start2 + ",0";
					break;
				case 1:
					coords2 = Convert.ToString(start2 + 1);
					break;
				default:
					coords2 = (start2 + 1) + "," + length2;
					break;
			}
			var text = new StringBuilder();
			text.Append("@@ -").Append(coords1).Append(" +").Append(coords2)
				.Append(" @@\n");
			// Escape the body of the patch with %xx notation.
			foreach (Diff aDiff in diffs) {
				switch (aDiff.operation) {
					case Operation.Insert:
						text.Append('+');
						break;
					case Operation.Delete:
						text.Append('-');
						break;
					case Operation.Equal:
						text.Append(' ');
						break;
				}

				text.Append(HttpUtility.UrlEncode(aDiff.text,
				                                  new UTF8Encoding()).Replace('+', ' ')).Append("\n");
			}

			return diff_match_patch.unescapeForEncodeUriCompatability(
			                                                          text.ToString());
		}
	}
}