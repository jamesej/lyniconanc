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
        string path = null;
        int lineNum = 0;

        public FileModel(string path)
        {
            lines = new List<string>();
            this.path = path;
            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                    lines.Add(reader.ReadLine());
            }
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
            lineNum = 0;
        }

        /// <summary>
        /// Move pointer forward to the first line that contains the supplied string
        /// </summary>
        /// <param name="contains">String to find</param>
        /// <returns>Whether line containing string was found</returns>
        public bool FindLineContains(string contains)
        {
            lineNum = lines.Skip(lineNum).IndexOfPredicate(l => l.Contains(contains)) + lineNum;
            if (lineNum < 0)
                lineNum = lines.Count;
            return lineNum < lines.Count;
        }

        /// <summary>
        /// Move pointer forward to the first line which is exactly the supplied string
        /// </summary>
        /// <param name="lineIs">string to match</param>
        /// <returns>Whether the line was found</returns>
        public bool FindLineIs(string lineIs)
        {
            lineNum = lines.Skip(lineNum).IndexOfPredicate(l => l.Trim() == lineIs) + lineNum;
            if (lineNum < 0)
                lineNum = lines.Count;
            return lineNum < lines.Count;
        }

        /// <summary>
        /// Insert a line into the file at the pointer unless it already exists
        /// </summary>
        /// <param name="line">the line to insert</param>
        /// <param name="useIndentAfter">Match indentation to following line (otherwise uses previous line)</param>
        public void InsertUniqueLineWithIndent(string line, bool useIndentAfter = false)
        {
            if (lines.Any(l => l.Contains(line)))
                return;
            InsertLineWithIndent(line, useIndentAfter: useIndentAfter);
        }

        /// <summary>
        /// Insert a line into the file at the pointer
        /// </summary>
        /// <param name="line">the line to insert</param>
        /// <param name="useIndentAfter">Match indentation to following line (otherwise uses previous line)</param>
        public void InsertLineWithIndent(string line, bool useIndentAfter = false)
        {
            string indent = "";
            if (!useIndentAfter && lineNum > 0)
                indent = new string(lines[lineNum - 1].TakeWhile(c => char.IsWhiteSpace(c)).ToArray());
            else if (useIndentAfter && lineNum < lines.Count - 1)
                indent = new string(lines[lineNum + 1].TakeWhile(c => char.IsWhiteSpace(c)).ToArray());
            lineNum++;
            lines.Insert(lineNum, indent + line);
        }
    }
}
