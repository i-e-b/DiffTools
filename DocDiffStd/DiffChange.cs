namespace DocDiffStd;

/// <summary>Details of one difference between a pair of documents</summary>
public struct DiffChange {
	
	/// <summary>Start position in document A</summary>
	public int StartA;
	
	/// <summary>Start position in document B</summary>
	public int StartB;

	/// <summary>Number of changes in document A</summary>
	public int DeletedA;
	
	/// <summary>Number of changes in document B</summary>
	public int InsertedB;
}