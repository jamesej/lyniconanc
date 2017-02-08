using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    public class FileModelStripper
    {
        public List<string> StrippedLines { get; set; }

        private bool stripping;
        private char endMatch;
        private Func<char?, bool> endMatchPrev;
        private char? prev;
        private List<string> lines;
        private Queue<bool> skip = new Queue<bool>();

        public FileModelStripper(List<string> lines)
        {
            stripping = false;
            this.lines = lines;
            StripLines();
        }

        private void StripLines()
        {
            StrippedLines = new List<string>();
            foreach (string line in lines)
            {
                StrippedLines.Add(new string(StripProcessLine(line).ToArray()));
            }
        }

        private IEnumerable<char> StripProcessLine(string line)
        {
            prev = null;
            foreach (char c in line.ToCharArray())
            {
                CheckStripping(c);
                if (prev.HasValue && (skip.Count == 0 ? (!stripping) : (!skip.Dequeue())))
                    yield return prev.Value;
                prev = c;
            }
            if (prev.HasValue && (skip.Count == 0 ? (!stripping) : (!skip.Dequeue())))
                yield return prev.Value;
            if (endMatch == '\n')
                stripping = false;
        }

        private void CheckStripping(char c)
        {
            if (stripping)
            {
                if (c == endMatch && (endMatchPrev == null || endMatchPrev(prev)))
                {
                    stripping = false;
                    skip.Enqueue(true); 
                    skip.Enqueue(true);
                }
            }
            else
            {
                switch (c)
                {
                    case '/':
                        if (prev == '/')
                        {
                            stripping = true;
                            endMatch = '\n';
                            endMatchPrev = null;
                        }
                        break;
                    case '*':
                        if (prev == '/')
                        {
                            stripping = true;
                            endMatch = '/';
                            endMatchPrev = ch => ch.HasValue && ch.Value == '*';
                        }
                        break;
                    case '"':
                        stripping = true;
                        endMatch = '"';
                        if (prev != '@')
                            endMatchPrev = ch => ch == null || ch.Value != '\\';
                        skip.Enqueue(prev == '"');
                        break;
                }
            }
        }
    }
}
