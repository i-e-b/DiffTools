namespace DocDiff {
	/// <summary>details of one difference.</summary>
	public struct DiffChange {
		/// <summary>Start position in Data A.</summary>
		public int StartA;
		/// <summary>Start positionin Data B.</summary>
		public int StartB;

		/// <summary>Number of changes in Data A.</summary>
		public int deletedA;
		/// <summary>Number of changes in Data B.</summary>
		public int insertedB;
	}
}