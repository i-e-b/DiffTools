using System.Collections.Generic;

namespace DiffMatchPatch {
	internal static class CompatibilityExtensions {
		public static void Splice<T>(this List<T> input, int start, int count,  params T[] objects) {
			input.RemoveRange(start, count);
			input.InsertRange(start, objects);
		}

		// Java substring function
		public static string JavaSubstring(this string s, int begin, int end) {
			return s.Substring(begin, end - begin);
		}
	}
}