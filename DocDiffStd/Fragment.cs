﻿using System;
using System.Text;

namespace DocDiffStd {
	
	/// <summary>
	/// A class to wrap up document changes.
	/// </summary>
	public class Fragment : IEquatable<Fragment>, IComparable<Fragment> {
		
		/// <summary>
		/// Type of the fragment. Will be <see cref="Differences.FragmentType.Deleted"/>, <see cref="Differences.FragmentType.Inserted"/>, or <see cref="Differences.FragmentType.Unchanged"/>.
		/// Changed text will be stored as a delete and an insert, in two fragments.
		/// </summary>
		public Differences.FragmentType Type { get; set; }
		
		/// <summary>
		/// Single character code for the type of fragment
		/// </summary>
		public string TypeString {
			get
			{
				return Type switch
				{
					Differences.FragmentType.Deleted => "d",
					Differences.FragmentType.Inserted => "i",
					Differences.FragmentType.Unchanged => "u",
					_ => ""
				};
			}
		}
		
		/// <summary>
		/// The text covered by this fragment, including unchanged sections
		/// </summary>
		public string Content => _sb.ToString();

		/// <summary>
		/// Length of the <see cref="Content"/> string
		/// </summary>
		public int Length => _sb.Length;

		/// <summary>
		/// Equivalent position of the change in new/right text
		/// </summary>
		public int Position { get; set; }

		private readonly StringBuilder _sb;

		/// <summary>
		/// Create a non-empty fragment with content and location
		/// </summary>
		public Fragment (Differences.FragmentType thisType, string part, int location) {
			_sb = new StringBuilder();
			Type = thisType;
			Position = location;
			_sb.Append(part);
		}

		/// <summary>
		/// Create an empty fragment with a given type and location
		/// </summary>
		public Fragment (Differences.FragmentType thisType, int location) {
			_sb = new StringBuilder();
			Type = thisType;
			Position = location;
		}

		/// <summary>
		/// Join more text to this fragment
		/// </summary>
		public void Join (string part) {
			_sb.Append(part);
		}

		/// <inheritdoc />
		public bool Equals(Fragment other)
		{
			return Position == other.Position && Length == other.Length && Content == other.Content;
		}

		/// <inheritdoc />
		public int CompareTo(Fragment other)
		{
			return Position.CompareTo(other.Position);
		}

		/// <summary>
		/// Readable representation of the fragment
		/// </summary>
		public override string ToString()
		{
			return $"{TypeString}@{Position}: {Content}";
		}
	}
}