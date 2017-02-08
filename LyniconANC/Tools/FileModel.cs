using Lynicon.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    /// <summary>
    /// Line-based model for a code file
    /// </summary>
    public class FileModel
    {
        List<string> lines = null;
        List<string> searchLines = null;
        string path = null;
        public int LineNum { get; set; }

        public FileModel(string path)
        {
            lines = new List<string>();
            this.path = path;
            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                    lines.Add(reader.ReadLine());
            }
            var fms = new FileModelStripper(lines);
            searchLines = fms.StrippedLines;
            LineNum = 0;
        }
        public FileModel(List<string> lines)
        {
            this.lines = lines;
            var fms = new FileModelStripper(lines);
            searchLines = fms.StrippedLines;
            LineNum = 0;
        }

        /// <summary>
        /// Write the model back to its original location
        /// </summary>
        public void Write()
        {
            using (var writer = new StreamWriter(path, false))
            {
                foreach (string line in lines)
                    writer.WriteLine(line);
            }
        }

        /// <summary>
        /// Move line pointer to top of file
        /// </summary>
        public void ToTop()
        {
            LineNum = 0;
        }

        public bool Jump(int offset)
        {
            LineNum += offset;
            if (LineNum >= lines.Count)
            {
                LineNum = lines.Count - 1;
                return false;
            }
            if (LineNum < 0)
            {
                LineNum = 0;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Move pointer forward to the first line that contains the supplied string
        /// </summary>
        /// <param name="contains">String to find</param>
        /// <returns>Whether line containing string was found</returns>
        public bool FindLineContains(string contains)
        {
            int currLineNum = LineNum;
            LineNum = searchLines.Skip(LineNum).IndexOfPredicate(l => l.Contains(contains)) + LineNum;
            if (LineNum < currLineNum)
                LineNum = lines.Count;
            return LineNum < lines.Count;
        }
        public bool FindPrevLineContains(string contains)
        {
            int currLineNum = LineNum;
            LineNum = LineNum - searchLines.Reverse<string>().Skip(lines.Count - LineNum - 1).IndexOfPredicate(l => l.Contains(contains));
            if (LineNum > currLineNum)
                LineNum = -1;
            return LineNum > -1;
        }

        /// <summary>
        /// Move pointer forward to the first line which is exactly the supplied string
        /// </summary>
        /// <param name="lineIs">string to match</param>
        /// <returns>Whether the line was found</returns>
        public bool FindLineIs(string lineIs)
        {
            int currLineNum = LineNum;
            LineNum = searchLines.Skip(LineNum).IndexOfPredicate(l => l.Trim() == lineIs) + LineNum;
            if (LineNum < currLineNum)
                LineNum = lines.Count;
            return LineNum < lines.Count;
        }
        public bool FindPrevLineIs(string lineIs)
        {
            int currLineNum = LineNum;
            LineNum = LineNum - searchLines.Reverse<string>().Skip(searchLines.Count - LineNum - 1).IndexOfPredicate(l => l.Trim() == lineIs);
            if (LineNum > currLineNum)
                LineNum = -1;
            return LineNum > -1;
        }

        public string CurrentLine
        {
            get { return searchLines[LineNum]; }
        }

        public bool LineContains(string contains)
        {
            return searchLines[LineNum].Contains(contains);
        }

        public string FindLineContainsAny(IEnumerable<string> containsAny)
        {
            int currLineNum = LineNum;
            LineNum = searchLines.Skip(LineNum).IndexOfPredicate(l => containsAny.Any(c => l.Contains(c))) + LineNum;
            if (LineNum < currLineNum)
                LineNum = lines.Count;
            return LineNum == lines.Count ? null : containsAny.First(c => searchLines[LineNum].Contains(c));
        }

        public bool FindEndOfMethod()
        {
            Jump(1);
            string foundKeyword = FindLineContainsAny(new string[] { "public", "internal", "protected", "private" });
            bool foundPrev;
            if (foundKeyword != null)
            {
                foundPrev = FindPrevLineIs("}");
                if (foundPrev)
                {
                    Jump(-1);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                Jump(9999);
                foundPrev = FindPrevLineIs("}"); // namespace
                if (!foundPrev)
                    return false;
                Jump(-1);
                foundPrev = FindPrevLineIs("}"); // class
                if (!foundPrev)
                    return false;
                Jump(-1);
                foundPrev = FindPrevLineIs("}"); // method
                if (!foundPrev)
                    return false;
                Jump(-1);
                return true;
            }
        }

        /// <summary>
        /// Insert a line into the file at the pointer unless it already exists
        /// </summary>
        /// <param name="line">the line to insert</param>
        /// <param name="useIndentAfter">Match indentation to following line (otherwise uses previous line)</param>
        /// <param name="backIndent">Number of characters to delete from indent</param>
        public bool InsertUniqueLineWithIndent(string line, bool useIndentAfter = false, int backIndent = 0)
        {
            if (lines.Any(l => l.Contains(line)))
                return false;
            InsertLineWithIndent(line, useIndentAfter: useIndentAfter, backIndent: backIndent);
            return true;
        }

        /// <summary>
        /// Insert a line into the file at the pointer
        /// </summary>
        /// <param name="line">the line to insert</param>
        /// <param name="useIndentAfter">Match indentation to following line (otherwise uses previous line)</param>
        /// <param name="backIndent">Number of characters to delete from indentation</param>
        public void InsertLineWithIndent(string line, bool useIndentAfter = false, int backIndent = 0)
        {
            string indent = "";
            if (!useIndentAfter && LineNum > 0)
                indent = new string(lines[LineNum - 1].TakeWhile(c => char.IsWhiteSpace(c)).ToArray());
            else if (useIndentAfter && LineNum < lines.Count - 1)
                indent = new string(lines[LineNum + 1].TakeWhile(c => char.IsWhiteSpace(c)).ToArray());
            if (backIndent != 0)
                indent = indent.Substring(0, Math.Max(indent.Length - backIndent, 0));
            LineNum++;
            lines.Insert(LineNum, indent + line);
            searchLines.Insert(LineNum, indent + line);
        }

        public bool InsertTextAfterMatchingBracket(string match, string text)
        {
            if (!match.EndsWith("("))
                throw new ArgumentException("parameter match should end with the open bracket to be matched");
            int idx = searchLines[LineNum].IndexOf(match) + match.Length;
            int lvl = 1;
            while (lvl > 0)
            {
                switch (searchLines[LineNum][idx])
                {
                    case '(':
                        lvl++;
                        break;
                    case ')':
                        lvl--;
                        break;
                }
                idx++;
                if (idx >= searchLines[LineNum].Length)
                {
                    idx = 0;
                    LineNum++;
                    if (LineNum >= searchLines.Count)
                        return false;
                }
            }
            if (lines[LineNum].Substring(0, idx) != searchLines[LineNum].Substring(0, idx))
                return false;
            lines[LineNum] = lines[LineNum].Substring(0, idx)
                + text
                + (idx >= lines[LineNum].Length ? "" : lines[LineNum].Substring(idx));
            searchLines[LineNum] = searchLines[LineNum].Substring(0, idx)
                + text
                + (idx >= searchLines[LineNum].Length ? "" : searchLines[LineNum].Substring(idx));
            return true;
        }

        public bool ReplaceText(string text, string replacement)
        {
            if (LineNum < 0 || LineNum >= lines.Count)
                return false;

            if (searchLines[LineNum].Contains(text))
            {
                lines[LineNum] = lines[LineNum].Replace(text, replacement);
                searchLines[LineNum] = searchLines[LineNum].Replace(text, replacement);
                return true;
            }
            else
                return false;
        }

        public List<string> GetLines()
        {
            return lines.ToList();
        }
    }
}
