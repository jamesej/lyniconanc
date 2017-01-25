
namespace Cyotek.Web.BbCodeFormatter
{
  internal class SearchReplaceFormatter : IHtmlFormatter
  {
		#region  Private Member Declarations  

    private string _pattern;
    private string _replace;

		#endregion  Private Member Declarations  

		#region  Public Constructors  

    public SearchReplaceFormatter(string pattern, string replace)
    {
      _pattern = pattern;
      _replace = replace;
    }

		#endregion  Public Constructors  

		#region  Public Methods  

    public string Format(string data)
    {
      return data.Replace(_pattern, _replace);
    }

		#endregion  Public Methods  
  }
}
