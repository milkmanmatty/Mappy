﻿namespace TAUtil.Tdf
{
    using System;
    using System.IO;

    public class TdfParser
    {
        private readonly TextReader reader;
        private readonly ITdfNodeAdapter adapter;

        private int lineCount;

        public TdfParser(TextReader reader, ITdfNodeAdapter adapter)
        {
            this.reader = reader;
            this.adapter = adapter;
        }

        public TdfNode Load()
        {
            TdfNode root = new TdfNode();

            string line;
            while ((line = this.ReadNextInterestingLine()) != null)
            {
                this.ReadBlock(line);
            }

            return root;
        }

        private static string ExtractContent(string line)
        {
            int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex != -1)
            {
                line = line.Remove(commentIndex);
            }

            line = line.Trim();
            return line;
        }

        private void ReadBlock(string firstLine)
        {
            this.adapter.BeginBlock(this.ParseBlockName(firstLine));
            this.ReadBlockBody();
            this.adapter.EndBlock();
        }

        private string ReadNextInterestingLine()
        {
            string line;
            do
            {
                line = this.ReadNextLine();

                if (line == null)
                {
                    return null;
                }

                line = ExtractContent(line);
            }
            while (string.Equals(line, string.Empty, StringComparison.Ordinal));

            return line;
        }

        private string ReadNextLine()
        {
            string line = this.reader.ReadLine();
            this.lineCount++;
            return line;
        }

        private void ReadBlockBody()
        {
            string openBracket = this.ReadNextInterestingLine();
            if (!string.Equals(openBracket, "{", StringComparison.Ordinal))
            {
                this.RaiseError("{", openBracket);
            }

            string line;
            while (!string.Equals(line = this.ReadNextInterestingLine(), "}", StringComparison.Ordinal))
            {
                if (line.StartsWith("[", StringComparison.Ordinal))
                {
                    this.ReadBlock(line);
                }
                else
                {
                    this.ReadBlockLine(line);
                }
            }
        }

        private string ParseBlockName(string nameLine)
        {
            if (!nameLine.StartsWith("[", StringComparison.Ordinal)
                || !nameLine.EndsWith("]", StringComparison.Ordinal))
            {
                this.RaiseError("[<name>]", nameLine);
            }

            return nameLine.Substring(1, nameLine.Length - 2);
        }

        private void ReadBlockLine(string line)
        {
            // Chomp ending semicolon.
            // Some files are missing semicolons at the end of a statement,
            // so we assume that statements are terminated by newlines.
            int i = line.IndexOf(';');
            if (i != -1)
            {
                line = line.Remove(i);
            }

            string[] parts = line.Split(new[] { '=' }, 2);

            if (parts.Length < 2)
            {
                this.RaiseError("<key>=<value>", line);
            }

            this.adapter.AddProperty(parts[0].Trim(), parts[1].Trim());
        }

        private void RaiseError(string message)
        {
            throw new ParseException(string.Format("line {0}: {1}", this.lineCount, message));
        }

        private void RaiseError(string expected, string actual)
        {
            this.RaiseError(string.Format("Expected {0}, got {1}", expected, actual));
        }
    }
}
