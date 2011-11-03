namespace DiffMatchPatch {
	public class DMPSettings {
		// Defaults.
		// Set these on your diff_match_patch instance to override the defaults.

		/// <summary>Number of seconds to map a diff before giving up (0 for infinity).</summary>
		public float Diff_Timeout = 1.0f;

		/// <summary>Cost of an empty edit operation in terms of edit characters.</summary>
		public short Diff_EditCost = 4;

		/// <summary>At what point is no match declared (0.0 = perfection, 1.0 = very loose).</summary>
		public float Match_Threshold = 0.5f;

		/// <summary>
		/// How far to search for a match (0 = exact location, 1000+ = broad match).
		/// A match this many characters away from the expected location will add
		/// 1.0 to the score (0.0 is a perfect match).
		/// </summary>
		public int Match_Distance = 1000;

		/// <summary>
		/// When deleting a large block of text (over ~64 characters), how close
		/// does the contents have to match the expected contents. (0.0 =
		/// perfection, 1.0 = very loose). Match_Threshold controls
		/// how closely the end points of a delete need to match.
		/// </summary>
		public float Patch_DeleteThreshold = 0.5f;

		/// <summary>Chunk size for context length.</summary>
		public short Patch_Margin = 4;
	}
}