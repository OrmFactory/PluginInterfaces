using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace PluginInterfaces;

public class CodeBuilder
{
	private List<string> lines = new();

	public bool Empty => lines.Count == 0;

	public string NewLineChars = "\r\n";
	public string IndentChars = "\t";

	public void AppendLine()
	{
		AppendLine("");
	}

	public void AppendLine(string line)
	{
		lines.Add(line);
	}

	public void Append(CodeBuilder cb)
	{
		lines.AddRange(cb.lines);
	}

	public override string ToString()
	{
		var sb = new StringBuilder();
		var tabIndex = 0;
		foreach (var line in lines)
		{
			if (line == "")
			{
				sb.AppendLine();
				continue;
			}

			if (line.StartsWith("}"))
			{
				tabIndex += BracesBalance(line);
				sb.Append(GetIndent(tabIndex));
				sb.Append(line);
				sb.Append(NewLineChars);
			}
			else
			{
				sb.Append(GetIndent(tabIndex));
				sb.Append(line);
				sb.Append(NewLineChars);
				tabIndex += BracesBalance(line);
			}
		}
		return sb.ToString();
	}

	private string GetIndent(int count)
	{
		return String.Concat(Enumerable.Repeat(IndentChars, count));
	}

	private int BracesBalance(string line)
	{
		return line.Count(c => c == '{') - line.Count(c => c == '}');
	}
}