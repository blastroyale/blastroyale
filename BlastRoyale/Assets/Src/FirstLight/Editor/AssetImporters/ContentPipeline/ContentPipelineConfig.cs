namespace FirstLight.Editor.AssetImporters
{
	public class ContentPipelineConfig
	{
		/// <summary>
		/// Path where addressabke assets will live after imported
		/// </summary>
		public string AssetDir;
		
		/// <summary>
		/// Path refering to where assets are initially imported
		/// </summary>
		public string ImportPath;
		
		/// <summary>
		/// Prefix for this collection
		/// </summary>
		public string Prefix;

		/// <summary>
		/// Does this collection requires a preset to be the base ?
		/// </summary>
		public string Preset;

		/// <summary>
		/// Material to be replaced
		/// </summary>
		public string Material;

		/// <summary>
		/// Should it be set to a specific layer ?
		/// </summary>
		public string Layer;
	}
}