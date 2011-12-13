using System.Text;

namespace DocDiff {
	/// <summary>
	/// A class to wrap up document changes.
	/// </summary>
	public class Fragment {
		public Differences.FragmentType Type { get; set; }
		public string TypeString {
			get {
				switch (Type) {
					case Differences.FragmentType.Deleted: return "d";
					case Differences.FragmentType.Inserted: return "i";
					case Differences.FragmentType.Unchanged: return "u";
					default: return null;
				}
			}
		}
		public string SplitPart {
			get { return sb.ToString(); }
		}
		public int Length { get { return sb.Length; } }
		public int Position { get; set; }

		private readonly StringBuilder sb;

		public Fragment (Differences.FragmentType ThisType, string Part, int Location) {
			sb = new StringBuilder();
			Type = ThisType;
			Position = Location;
			sb.Append(Part);
		}

		public Fragment (Differences.FragmentType ThisType, int Location) {
			sb = new StringBuilder();
			Type = ThisType;
			Position = Location;
		}

		public void Join (string Part) {
			sb.Append(Part);
		}
	}
}