using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;

namespace DocDiff {
	/// <summary>
	/// Tools for manipulating the output of System.Text.Diff package.
	/// </summary>
	public static class DiffCode {

		#region Decode

		/// <summary>
		/// Build and return a revision by applying a set of DiffCodes to a base version.
		/// DiffCodes should be from CompressedDiffCode() or BuildDiffCode().
		/// </summary>
		public static string BuildRevision (string BaseRevision, string DiffCodes) {
			// Decide if it's compressed or not, and send to appropriate decoder.
			return DiffCodes.StartsWith("[DCMP]") 
				? BuildRevision(BaseRevision, Convert.FromBase64String(DiffCodes.Substring(6))) 
				: DecodeRevision(BaseRevision, DiffCodes);
		}

		/// <summary>
		/// Build and return a revision by applying a set of DiffCodes to a base version.
		/// DiffCodes should be from StorageDiffCode().
		/// </summary>
		public static string BuildRevision (string BaseRevision, byte[] DiffCodes) {
			// Decompress and send to real decoder.
			var @in = new MemoryStream(DiffCodes); // this will receive the compressed code
			var filter = new DeflateStream(@in, CompressionMode.Decompress);

			var @out = new List<byte>(DiffCodes.Length);
			int b = filter.ReadByte();
			while (b >= 0) {
				@out.Add((byte)b);
				b = filter.ReadByte();
			}
			return DecodeRevision(BaseRevision, Encoding.UTF8.GetString(@out.ToArray()));
		}


		/// <summary>
		/// Build and return a revision by applying a set of DiffCodes to a base version.
		/// DiffCodes should be from CompressedDiffCode() or BuildDiffCode().
		/// </summary>
		public static List<Differences.Fragment> BuildChanges (string BaseRevision, string DiffCodes) {
			// Decide if it's compressed or not, and send to appropriate decoder.
			return DiffCodes.StartsWith("[DCMP]") 
				? BuildChanges(BaseRevision, Convert.FromBase64String(DiffCodes.Substring(6))) 
				: DecodeRevisionFragments(BaseRevision, DiffCodes);
		}

		/// <summary>
		/// Build and return a revision by applying a set of DiffCodes to a base version.
		/// DiffCodes should be from StorageDiffCode().
		/// </summary>
		public static List<Differences.Fragment> BuildChanges (string BaseRevision, byte[] DiffCodes) {
			// Decompress and send to real decoder.
			var @in = new MemoryStream(DiffCodes); // this will receive the compressed code
			var filter = new DeflateStream(@in, CompressionMode.Decompress);

			var @out = new List<byte>(DiffCodes.Length);
			int b = filter.ReadByte();
			while (b >= 0) {
				@out.Add((byte)b);
				b = filter.ReadByte();
			}
			return DecodeRevisionFragments(BaseRevision, Encoding.UTF8.GetString(@out.ToArray()));
		}

		#region Inner Workings

		private static List<Differences.Fragment> DecodeRevisionFragments (string BaseRevision, string DiffCodes) {
			var @out = new List<Differences.Fragment>();

			if (String.IsNullOrEmpty(DiffCodes)) {
				// no changes.
				@out.Add(new Differences.Fragment(Differences.FragmentType.Unchanged, BaseRevision, 0));
				return @out;
			}
			string[] codes = DiffCodes.Split(new[] { (char)31 }, StringSplitOptions.RemoveEmptyEntries);
			if (codes.Length < 1) {
				// no changes.
				@out.Add(new Differences.Fragment(Differences.FragmentType.Unchanged, BaseRevision, 0));
				return @out;
			}


			int pos, del;
			string ins;
			DecodeDiffCode(codes[0], out pos, out del, out ins);

			if (pos > 0) { // write leading text
				@out.Add(new Differences.Fragment(Differences.FragmentType.Unchanged, BaseRevision.Substring(0, pos), 0));
			}

			foreach (string code in codes) {
				int old_pos = pos;
				// get code
				DecodeDiffCode(code, out pos, out del, out ins);

				// write unchanged text
				if (pos > old_pos) {
					int l = (pos - old_pos);
					if (l + old_pos >= BaseRevision.Length) {
						l = (BaseRevision.Length - old_pos) - 1;
					}
					if (l >= 0) {
						string skip = BaseRevision.Substring(old_pos, l);
						@out.Add(new Differences.Fragment(Differences.FragmentType.Unchanged, skip, old_pos));
					} else {
						// error!
						throw new Exception("Diff code does not match text - Can't skip!");
					}
				}

				// Skip deleted text
				string delstr = BaseRevision.Substring(pos, del);
				@out.Add(new Differences.Fragment(Differences.FragmentType.Deleted, delstr, pos));
				pos += del;

				// Write any inserted text
				if (ins.Length > 0) {
					@out.Add(new Differences.Fragment(Differences.FragmentType.Inserted, ins, pos));
				}
			}

			// Write any unchanged text at the end.
			if (pos < BaseRevision.Length) {
				@out.Add(new Differences.Fragment(Differences.FragmentType.Unchanged, BaseRevision.Substring(pos), pos));
			}

			return @out;
		}

		/// <summary>
		/// Build and return a revision by applying a set of DiffCodes to a base version
		/// </summary>
		private static string DecodeRevision (string BaseRevision, string DiffCodes) {
			List<Differences.Fragment> frags = DecodeRevisionFragments(BaseRevision, DiffCodes);

			var sb = new StringBuilder();
			foreach (var frag in frags) {
				if (frag.Type == Differences.FragmentType.Deleted) continue;
				sb.Append(frag.SplitPart);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Work out the meaning of a diff code
		/// </summary>
		private static void DecodeDiffCode (string Code, out int Position, out int Deletes, out string Inserts) {
			if (String.IsNullOrEmpty(Code)) {
				throw new Exception("Empty codes not allowed");
			}
			int s1 = Code.IndexOf('-',1); // pos/del
			int s2 = Code.IndexOf(';'); // [pos/del] / ins

			if (s1 < 0 || s2 < 0) throw new ArgumentException("Code was invalid");

			string pStr = Code.Substring(0, s1).Trim();
			string dStr = Code.Substring(s1+1, (s2-s1)-1).Trim();
			string iStr = (s2+1 >= Code.Length) ? (null) : (Code.Substring(s2 + 1)); // Never trim this!!

			if (String.IsNullOrEmpty(pStr)) {
				Position = 0;
			} else {
				Position = int.Parse(pStr);
			}

			if (String.IsNullOrEmpty(dStr)) {
				Deletes = 0;
			} else {
				Deletes = int.Parse(dStr);
			}

			if (String.IsNullOrEmpty(iStr)) {
				Inserts = "";
			} else {
				Inserts = iStr;
			}

			return;
		}
		#endregion

		#endregion


		#region Encode
		/// <summary>
		/// Returns a highly compressed version of BuildDiffCode()
		/// for textual storage and transmission.
		/// </summary>
		public static string CompressedDiffCode (Differences differences) {
			var @out = new MemoryStream(); // this will receive the compressed code
			var filter = new DeflateStream(@out, CompressionMode.Compress);

			byte[] buffer = Encoding.UTF8.GetBytes(BuildDiffCode (differences));
			filter.Write(buffer, 0, buffer.Length);

			return "[DCMP]" + Convert.ToBase64String(@out.ToArray()); // this is 7-bit safe and robust for storage
		}

		/// <summary>
		/// Returns a highly compressed version of BuildDiffCode()
		/// for binary storage
		/// </summary>
		public static byte[] StorageDiffCode (Differences differences) {
			var @out = new MemoryStream(); // this will receive the compressed code
			var filter = new DeflateStream(@out, CompressionMode.Compress);

			byte[] buffer = Encoding.UTF8.GetBytes(BuildDiffCode(differences));
			filter.Write(buffer, 0, buffer.Length);

			return @out.ToArray();
		}

		/// <summary>
		/// Build a code that can be used to transform one version of a file into another.
		/// </summary>
		public static string BuildDiffCode (Differences differences) {
			var sb = new StringBuilder();
			int pos = 0;
			int del = 0;
			string ins = "";

			foreach (Differences.Fragment frag in differences) {

				if (frag.Type == Differences.FragmentType.Unchanged) {
					pos += del;
					pos -= ins.Length;
					if (del > 0 || ins.Length > 0) {
						WriteDiffCode(sb, pos-del, del, ins);
					}
					pos += frag.Length;
					del = 0;
					ins = "";
				} else if (frag.Type == Differences.FragmentType.Deleted) {
					del += frag.Length;
				} else if (frag.Type == Differences.FragmentType.Inserted) {
					pos += frag.Length;
					ins += frag.SplitPart;
				} else {
					throw new Exception("broken frag type");
				}
			}

			// Don't forget the last one!
			if (del > 0 || ins.Length > 0) {
				WriteDiffCode(sb, pos - del, del, ins);
			}

			return sb.ToString();
		}

		#region Inner Workings
		/// <summary>Used by BuildDiffCode()</summary>
		private static void WriteDiffCode (StringBuilder sb, int pos, int del, string ins) {
			if (del == 0 && String.IsNullOrEmpty(ins)) return;
			sb.Append((char)31);
			int p = pos;
			sb.Append(p);
			sb.Append("-");
			if (del > 0) {
				sb.Append(del);
			}
			sb.Append(";");
			if (!String.IsNullOrEmpty(ins)) {
				sb.Append(ins.Replace((char)31, ' '));
			}
		}

		#endregion
		#endregion

	}
}
