using System;
using System.Collections.Generic;

namespace Cyotek.Web.BbCodeFormatter
{
  internal class LineBreaksFormatter : IHtmlFormatter
  {
    #region  Private Member Declarations

    private string[] _exclusionCodes;
    private List<IHtmlFormatter> _formatters;

    #endregion  Private Member Declarations

    #region  Public Constructors

    public LineBreaksFormatter(string[] exclusionCodes)
    {
      _exclusionCodes = exclusionCodes;

      _formatters = new List<IHtmlFormatter>();
      _formatters.Add(new SearchReplaceFormatter("\r", ""));
      _formatters.Add(new SearchReplaceFormatter("\n\n", "</p><p>"));
      _formatters.Add(new SearchReplaceFormatter("\n", "<br />"));
    }

    #endregion  Public Constructors

    #region  Public Methods

    public string Format(string data)
    {
      int blockStart;
      int blockEnd;
      string tagName;
      string nonBlockText;

      blockEnd = 0;
      blockStart = 0;
      do
      {

        blockStart = GetNextBlockStart(blockEnd, data, out tagName);
        if (blockStart != -1)
          nonBlockText = data.Substring(blockEnd, blockStart - blockEnd);
        else if (blockEnd != -1 && blockEnd < data.Length)
          nonBlockText = data.Substring(blockEnd);
        else
          nonBlockText = null;

        if (nonBlockText != null)
        {
          int originalLength;

          originalLength = nonBlockText.Length;

          foreach (IHtmlFormatter formatter in _formatters)
            nonBlockText = formatter.Format(nonBlockText);

          if (blockStart != -1)
          {
            data = data.Substring(0, blockEnd) + nonBlockText + data.Substring(blockStart);

            blockStart += (nonBlockText.Length - originalLength);
            blockEnd = GetBlockEnd(blockStart, data, tagName);
          }
          else
            data = data.Substring(0, blockEnd) + nonBlockText;
        }

      } while (blockStart != -1);

      return data;
    }

    #endregion  Public Methods

    #region  Private Methods

    private int GetBlockEnd(int startingPosition, string data, string tag)
    {
      int matchPosition;
      string fullTag;

      fullTag = string.Format("[/{0}]", tag);
      matchPosition = data.IndexOf(fullTag, startingPosition, StringComparison.InvariantCultureIgnoreCase);

      if (matchPosition == -1)
        matchPosition = data.Length;

      return matchPosition;
    }

    private int GetNextBlockStart(int startingPosition, string data, out string matchedTag)
    {
      int lowestPosition;
      int matchPosition;

      lowestPosition = -1;
      matchedTag = null;

      foreach (string exclusion in _exclusionCodes)
      {
        string tag;

        tag = string.Format("[{0}]", exclusion);
        matchPosition = data.IndexOf(tag, startingPosition, StringComparison.InvariantCultureIgnoreCase);

        if (matchPosition > -1 && (matchPosition < lowestPosition || lowestPosition == -1))
        {
          matchedTag = exclusion;
          lowestPosition = matchPosition;
        }
      }

      return lowestPosition;
    }

    #endregion  Private Methods
  }
}
