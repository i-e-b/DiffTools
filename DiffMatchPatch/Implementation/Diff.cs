using System;
using System.Text;

namespace DiffMatchPatch {
	public class Diff {
		///<summary>One of: INSERT, DELETE or EQUAL.</summary>
		public Operation operation;
		///<summary>The text associated with this diff operation.</summary>
		public string text;

		public Diff (Operation operation, string text) {
			// Construct a diff with the specified operation and text.
			this.operation = operation;
			this.text = text;
		}

		public override string ToString () {
			string prettyText = text.Replace('\n', '\u00b6');
			return sb("Diff(", operation.ToString(), ",\"", prettyText, "\")");
		}

		private static string sb (params string[] strings) {
			var s = new StringBuilder();
			foreach (var s1 in strings) {
				s.Append(s1);
			}
			return s.ToString();
		}

		public override bool Equals (Object obj) {
			// If parameter is null return false.
			if (obj == null) {
				return false;
			}

			// If parameter cannot be cast to Diff return false.
			if (!(obj is Diff)) return false;
			var p = (Diff) obj;

			// Return true if the fields match.
			return p.operation == operation && p.text == text;
		}

		public bool Equals (Diff obj) {
			return obj.operation == operation && obj.text == text;
		}

		public override int GetHashCode () {
			return text.GetHashCode() ^ operation.GetHashCode();
		}
	}
}