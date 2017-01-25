using System.Text.RegularExpressions;

namespace Cyotek.Web.BbCodeFormatter
{
  internal class RegexFormatter : IHtmlFormatter
  {
		#region  Private Member Declarations  

    private Regex _regex;
    private string _replace;

		#endregion  Private Member Declarations  

		#region  Public Constructors  

    public RegexFormatter(string pattern, string replace)
      : this(pattern, replace, true)
    { }

    public RegexFormatter(string pattern, string replace, bool ignoreCase)
    {
      RegexOptions options;

      options = RegexOptions.Compiled;

      if (ignoreCase)
        options |= RegexOptions.IgnoreCase;

      _replace = replace;
      _regex = new Regex(pattern, options);
    }

		#endregion  Public Constructors  

		#region  Public Virtual Methods  

    public virtual string Format(string data)
    {
      return _regex.Replace(data, _replace);
    }

		#endregion  Public Virtual Methods  

		#region  Protected Properties  

    protected Regex Regex
    { get { return _regex; } }

    protected string Replace
    { get { return _replace; } }

		#endregion  Protected Properties  
  }
}
