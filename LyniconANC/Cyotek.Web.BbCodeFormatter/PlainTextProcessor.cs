using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cyotek.Web.BbCodeFormatter
{
  public static class PlainTextProcessor
  {
    static List<IHtmlFormatter> _formatters;

    static PlainTextProcessor()
    {
      _formatters = new List<IHtmlFormatter>();

      _formatters.Add(new SearchReplaceFormatter("\r", ""));
      _formatters.Add(new SearchReplaceFormatter("\n\n", "</p><p>"));
      _formatters.Add(new SearchReplaceFormatter("\n", "<br />"));
    }

    public static string Format(string data)
    {
      foreach (IHtmlFormatter formatter in _formatters)
        data = formatter.Format(data);

      return "<p>" + data + "</p>";
    }
  }
}
