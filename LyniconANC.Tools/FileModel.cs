using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyniconANC.Tools
{
    /// <summary>
    /// Line-based model for a code file
    /// </summary>
    public class FileModel
    {
        List<string> lines = null;
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
            lines = fms.StrippedLines;
            LineNum = 0;
        }
        public FileModel(List<string> lines)
        {
            this.lines = lines;
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
            LineNum = lines.Skip(LineNum).IndexOfPredicate(l => l.Contains(contains)) + LineNum;
            if (LineNum < currLineNum)
                LineNum = lines.Count;
            return LineNum < lines.Count;
        }
        public bool FindPrevLineContains(string contains)
        {
            int currLineNum = LineNum;
            LineNum = LineNum - lines.Reverse<string>().Skip(lines.Count - LineNum - 1).IndexOfPredicate(l => l.Contains(contains));
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
            LineNum = lines.Skip(LineNum).IndexOfPredicate(l => l.Trim() == lineIs) + LineNum;
            if (LineNum < currLineNum)
                LineNum = lines.Count;
            return LineNum < lines.Count;
        }
        public bool FindPrevLineIs(string lineIs)
        {
            int currLineNum = LineNum;
            LineNum = LineNum - lines.Reverse<string>().Skip(lines.Count - LineNum - 1).IndexOfPredicate(l => l.Trim() == lineIs);
            if (LineNum > currLineNum)
                LineNum = -1;
            return LineNum > -1;
        }

        public string FindLineContainsAny(IEnumerable<string> containsAny)
        {
            int currLineNum = LineNum;
            LineNum = lines.Skip(LineNum).IndexOfPredicate(l => containsAny.Any(c => l.Contains(c))) + LineNum;
            if (LineNum < currLineNum)
                LineNum = lines.Count;
            return LineNum == lines.Count ? null : containsAny.First(c => lines[LineNum].Contains(c));
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
        public bool InsertUniqueLineWithIndent(string line, bool useIndentAfter = false)
        {
            if (lines.Any(l => l.Contains(line)))
                return false;
            InsertLineWithIndent(line, useIndentAfter: useIndentAfter);
            return true;
        }

        /// <summary>
        /// Insert a line into the file at the pointer
        /// </summary>
        /// <param name="line">the line to insert</param>
        /// <param name="useIndentAfter">Match indentation to following line (otherwise uses previous line)</param>
        public void InsertLineWithIndent(string line, bool useIndentAfter = false)
        {
            string indent = "";
            if (!useIndentAfter && LineNum > 0)
                indent = new string(lines[LineNum - 1].TakeWhile(c => char.IsWhiteSpace(c)).ToArray());
            else if (useIndentAfter && LineNum < lines.Count - 1)
                indent = new string(lines[LineNum + 1].TakeWhile(c => char.IsWhiteSpace(c)).ToArray());
            LineNum++;
            lines.Insert(LineNum, indent + line);
        }
    }
}
