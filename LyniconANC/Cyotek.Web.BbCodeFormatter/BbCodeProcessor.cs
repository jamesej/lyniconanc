using System.Collections.Generic;

// expanded from original code found here: http://forums.asp.net/p/1087581/1635776.aspx
// advanced formatters can be found here: http://manoli.net/csharpformat/

namespace Cyotek.Web.BbCodeFormatter
{
  /// <summary>
  /// BBCode Helper allows formatting of text
  /// without the need to use html
  /// </summary>
  public class BbCodeProcessor
  {
    #region  Private Class Member Declarations

    static List<IHtmlFormatter> _formatters;

    #endregion  Private Class Member Declarations

    #region  Static Constructors

    static BbCodeProcessor()
    {
      string orderedListFormat;

      _formatters = new List<IHtmlFormatter>();

      _formatters.Add(new RegexFormatter(@"<(.|\n)*?>", string.Empty));

      // using the below three formatters make a complete mess of preformatted text. 
      // I've disabled them and replaced them with a slower version that will replace line breaks only when they are not within one of the specified blocks.

      //_formatters.Add(new SearchReplaceFormatter("\r", ""));
      //_formatters.Add(new SearchReplaceFormatter("\n\n", "</p><p>"));
      //_formatters.Add(new SearchReplaceFormatter("\n", "<br />"));
      _formatters.Add(new LineBreaksFormatter(new string[] { "html", "csharp", "code", "jscript", "sql", "vb", "php" }));

      _formatters.Add(new RegexFormatter(@"\[b(?:\s*)\]((.|\n)*?)\[/b(?:\s*)\]", "<strong>$1</strong>"));
      _formatters.Add(new RegexFormatter(@"\[i(?:\s*)\]((.|\n)*?)\[/i(?:\s*)\]", "<em>$1</em>"));
      _formatters.Add(new RegexFormatter(@"\[s(?:\s*)\]((.|\n)*?)\[/s(?:\s*)\]", "<strike>$1</strike>"));
      _formatters.Add(new RegexFormatter(@"\[u(?:\s*)\]((.|\n)*?)\[/u(?:\s*)\]", "<u>$1</u>"));

      _formatters.Add(new RegexFormatter(@"\[left(?:\s*)\]((.|\n)*?)\[/left(?:\s*)]", "<div style=\"text-align:left\">$1</div>"));
      _formatters.Add(new RegexFormatter(@"\[center(?:\s*)\]((.|\n)*?)\[/center(?:\s*)]", "<div style=\"text-align:center\">$1</div>"));
      _formatters.Add(new RegexFormatter(@"\[right(?:\s*)\]((.|\n)*?)\[/right(?:\s*)]", "<div style=\"text-align:right\">$1</div>"));

      _formatters.Add(new RegexFormatter(@"\[code(?:\s*)\]((.|\n)*?)\[/code(?:\s*)]", "<div class=\"bbc-codetitle\">Code:</div><div class=\"bbc-codecontent\"><pre>$1</pre></div>"));
      _formatters.Add(new RegexFormatter(@"\[php(?:\s*)\]((.|\n)*?)\[/php(?:\s*)]", "<div class=\"bbc-codetitle\">PHP Code:</div><div class=\"bbc-codecontent\"><pre>$1</pre></div>"));

      _formatters.Add(new RegexFormatter(@"\[quote=((.|\n)*?)(?:\s*)\]", "<div class=\"bbc-quotetitle\">$1 said:</div><div class=\"bbc-quotecontent\"><p>"));
      _formatters.Add(new RegexFormatter(@"\[quote(?:\s*)\]", "<div class=\"bbc-quotecontent\"><p>"));
      _formatters.Add(new RegexFormatter(@"\[/quote(?:\s*)\]", "</p></div>"));

      _formatters.Add(new RegexFormatter(@"\[url(?:\s*)\]www\.(.*?)\[/url(?:\s*)\]", "<a href=\"http://www.$1\" target=\"_blank\" title=\"$1\">$1</a>"));
      _formatters.Add(new RegexFormatter(@"\[url(?:\s*)\]((.|\n)*?)\[/url(?:\s*)\]", "<a href=\"$1\" target=\"_blank\" title=\"$1\">$1</a>"));
      _formatters.Add(new RegexFormatter(@"\[url=""((.|\n)*?)(?:\s*)""\]((.|\n)*?)\[/url(?:\s*)\]", "<a href=\"$1\" target=\"_blank\" title=\"$1\">$3</a>"));
      _formatters.Add(new RegexFormatter(@"\[url=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/url(?:\s*)\]", "<a href=\"$1\" target=\"_blank\" title=\"$1\">$3</a>"));
      _formatters.Add(new RegexFormatter(@"\[link(?:\s*)\]((.|\n)*?)\[/link(?:\s*)\]", "<a href=\"$1\" target=\"_blank\" title=\"$1\">$1</a>"));
      _formatters.Add(new RegexFormatter(@"\[link=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/link(?:\s*)\]", "<a href=\"$1\" target=\"_blank\" title=\"$1\">$3</a>"));

      _formatters.Add(new RegexFormatter(@"\[img(?:\s*)\]((.|\n)*?)\[/img(?:\s*)\]", "<img src=\"$1\" border=\"0\" alt=\"\" />"));
      _formatters.Add(new RegexFormatter(@"\[img align=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/img(?:\s*)\]", "<img src=\"$3\" border=\"0\" align=\"$1\" alt=\"\" />"));
      _formatters.Add(new RegexFormatter(@"\[img=((.|\n)*?)x((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/img(?:\s*)\]", "<img width=\"$1\" height=\"$3\" src=\"$5\" border=\"0\" alt=\"\" />"));

      _formatters.Add(new RegexFormatter(@"\[color=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/color(?:\s*)\]", "<span style=\"color:$1;\">$3</span>"));

      _formatters.Add(new RegexFormatter(@"\[highlight(?:\s*)\]((.|\n)*?)\[/highlight(?:\s*)]", "<span class=\"bbc-highlight\">$1</span>"));
      _formatters.Add(new RegexFormatter(@"\[spoiler(?:\s*)\]((.|\n)*?)\[/spoiler(?:\s*)]", "<span class=\"bbc-spoiler\">$1</span>"));
      _formatters.Add(new RegexFormatter(@"\[indent(?:\s*)\]((.|\n)*?)\[/indent(?:\s*)]", "<div class=\"bbc-indent\">$1</div>"));

      _formatters.Add(new RegexFormatter(@"\[hr(?:\s*)\]", "<hr />"));
      _formatters.Add(new RegexFormatter(@"\[br(?:\s*)\]", "<br />"));
      _formatters.Add(new RegexFormatter(@"\[nbsp(?:\s*)\]", "&nbsp;"));
      _formatters.Add(new RegexFormatter(@"\[rule=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/rule(?:\s*)\]", "<div style=\"height: 0pt; border-top: 1px solid $3; margin: auto; width: $1;\"></div>"));

      _formatters.Add(new RegexFormatter(@"\[email(?:\s*)\]((.|\n)*?)\[/email(?:\s*)\]", "<a href=\"mailto:$1\">$1</a>"));
      _formatters.Add(new RegexFormatter(@"\[email=""((.|\n)*?)(?:\s*)""\]((.|\n)*?)\[/email(?:\s*)\]", "<a href=\"mailto:$1\" title=\"$3\">$3</a>"));
      _formatters.Add(new RegexFormatter(@"\[email=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/email(?:\s*)\]", "<a href=\"mailto:$1\" title=\"$3\">$3</a>"));

      _formatters.Add(new RegexFormatter(@"\[small(?:\s*)\]((.|\n)*?)\[/small(?:\s*)]", "<small>$1</small>"));
      _formatters.Add(new RegexFormatter(@"\[size=+((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/size(?:\s*)\]", "<span style=\"font-size:$1em\">$3</span>"));
      _formatters.Add(new RegexFormatter(@"\[size=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/size(?:\s*)\]", "<span style=\"font-size:$1\">$3</span>"));
      _formatters.Add(new RegexFormatter(@"\[font=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/font(?:\s*)\]", "<span style=\"font-family:$1;\">$3</span>"));
      _formatters.Add(new RegexFormatter(@"\[align=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/align(?:\s*)\]", "<span style=\"text-align:$1;\">$3</span>"));
      _formatters.Add(new RegexFormatter(@"\[float=((.|\n)*?)(?:\s*)\]((.|\n)*?)\[/float(?:\s*)\]", "<span style=\"float:$1;\">$3</div>"));

      orderedListFormat = "<ol class=\"bbc-list\" style=\"list-style:{0};\">$1</ol>";
      _formatters.Add(new RegexFormatter(@"\[\*(?:\s*)]\s*([^\[]*)", "<li>$1</li>"));
      _formatters.Add(new RegexFormatter(@"\[list(?:\s*)\]((.|\n)*?)\[/list(?:\s*)\]", "<ul class=\"bbc-list\">$1</ul>"));
      _formatters.Add(new RegexFormatter(@"\[list=1(?:\s*)\]((.|\n)*?)\[/list(?:\s*)\]", string.Format(orderedListFormat, "decimal"), false));
      _formatters.Add(new RegexFormatter(@"\[list=i(?:\s*)\]((.|\n)*?)\[/list(?:\s*)\]", string.Format(orderedListFormat, "lower-roman"), false));
      _formatters.Add(new RegexFormatter(@"\[list=I(?:\s*)\]((.|\n)*?)\[/list(?:\s*)\]", string.Format(orderedListFormat, "upper-roman"), false));
      _formatters.Add(new RegexFormatter(@"\[list=a(?:\s*)\]((.|\n)*?)\[/list(?:\s*)\]", string.Format(orderedListFormat, "lower-alpha"), false));
      _formatters.Add(new RegexFormatter(@"\[list=A(?:\s*)\]((.|\n)*?)\[/list(?:\s*)\]", string.Format(orderedListFormat, "upper-alpha"), false));
    }

    #endregion  Static Constructors

    #region  Public Class Methods

    /// <summary>
    /// Convert bbCode data into HTML
    /// </summary>
    /// <param name="data">bbCode data</param>
    /// <returns>HTML</returns>
    public static string Format(string data)
    {
      foreach (IHtmlFormatter formatter in _formatters)
        data = formatter.Format(data);

      return data;
    }

    #endregion  Public Class Methods
  }
}
