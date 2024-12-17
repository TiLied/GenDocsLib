using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Tables;

namespace GenDocsLib
{
	public class GenDocs
	{
		private string Output = string.Empty;

		public GenDocs()
		{
			if (File.Exists(Directory.GetCurrentDirectory() + "/debugGenDocs.txt"))
				File.Delete(Directory.GetCurrentDirectory() + "/debugGenDocs.txt");

			Trace.Listeners.Add(new TextWriterTraceListener("debugGenDocs.txt"));
			Trace.AutoFlush = true;
			Trace.Listeners.Add(new ConsoleTraceListener());
		}

		public async void GenerateDocs(string path, string output)
		{
			Output = output;

			if (!Directory.Exists(output))
			{
				Directory.CreateDirectory(output);
			}

			if (File.Exists(Path.Combine(Output, "DocsFail.generated.txt")))
			{
				File.Delete(Path.Combine(Output, "DocsFail.generated.txt"));
			}

			StringBuilder sb = new();
			sb.Append("<docs>\n");

			ProcessDirectory(ref sb, path);

			sb.Append("</docs>");

			await File.WriteAllTextAsync(Path.Combine(output, "Docs.generated.xml"), sb.ToString());
		}

		private void ProcessDirectory(ref StringBuilder sb, string targetDirectory)
		{
			string[] fileEntries = Directory.GetFiles(targetDirectory);
			foreach (string fileName in fileEntries)
				ProcessFile(ref sb, fileName);

			// Recurse into subdirectories of this directory.
			string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
			foreach (string subdirectory in subdirectoryEntries)
				ProcessDirectory(ref sb, subdirectory);
		}

		private void ProcessFile(ref StringBuilder sb, string path)
		{
			string ext = Path.GetExtension(path);
			if (ext != ".md")
				return;

			string file = File.ReadAllText(path);

			// Configure the pipeline with all advanced extensions active
			var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
			MarkdownDocument? result = Markdown.Parse(file, pipeline);

			ValueTuple<int, string[]> tuple = new(0, new string[10]);

			List<Block> blocks = result.ToList();
			for (int i = 0; i < blocks.Count; i++)
			{
				Block md = blocks[i];

				ProcessMDBlocks(ref tuple, blocks[i]);
			}

			if (tuple.Item2[0] == null)
			{
				Log($"xmlStr[0] == null, path:{path.Substring(path.IndexOf("DocsToGen"))}\n");
				return;
			}

			if (tuple.Item2[0] == "<>")
			{
				Log($"xmlStr[0] == \"<>\", path:{path.Substring(path.IndexOf("DocsToGen"))}\n");
				return;
			}

			sb.Append(tuple.Item2[0].Replace("@","_"));

			sb.AppendLine();

			sb.AppendLine("<summary>");
			sb.AppendLine(tuple.Item2[1]);
			sb.AppendLine("</summary>");

			sb.AppendLine("<remarks>");
			if (tuple.Item2[2] != null)
			{
				sb.Append(tuple.Item2[2]);
				sb.AppendLine();
			}
			//see also
			if (tuple.Item2[7] != null)
			{
				sb.Append($"<para>{tuple.Item2[7]}</para>");
				sb.AppendLine();
			}
			//see also on mdn
			sb.AppendLine(tuple.Item2[9]);

			sb.AppendLine("</remarks>");

			if (tuple.Item2[5] != null)
			{
				sb.Append("<value>");
				sb.Append(tuple.Item2[5]);
				sb.AppendLine("</value>");
			}

			if (tuple.Item2[6] != null)
			{
				sb.Append("<returns>");
				sb.Append(tuple.Item2[6]);
				sb.AppendLine("</returns>");
			}

			sb.AppendLine(tuple.Item2[0].Replace("<", "</").Replace("@", "_"));
		}

		private void ProcessMDBlocks(ref ValueTuple<int, string[]> tuple, Block block)
		{
			if (block is HeadingBlock headingBlock)
			{
				List<Inline> inlines = headingBlock.Inline.ToList();
				foreach (Inline inline in inlines)
				{
					ProcessMDInlines(ref tuple, inline);
				}

				return;
			}

			if (block is ParagraphBlock paragraphBlock)
			{
				if (tuple.Item1 == 2)
				{
					tuple.Item2[tuple.Item1] += "<para>";
				}

				List<Inline> inlines = paragraphBlock.Inline.ToList();
				foreach (Inline inline in inlines)
				{
					ProcessMDInlines(ref tuple, inline);
					if (tuple.Item1 == 3)
					{
						tuple.Item1 = 1;
						return;
					}
				}

				if (tuple.Item1 == 1)
				{
					tuple.Item1++;
					return;
				}
				if (tuple.Item1 == 2)
				{
					tuple.Item2[tuple.Item1] += "</para>";
					return;
				}

				return;
			}

			if (block is ListBlock listBlock && tuple.Item1 == 7)
			{
				List<Block> lBlock = listBlock.ToList();
				foreach (Block block1 in lBlock)
				{
					ProcessMDBlocks(ref tuple, block1);
				}

				return;
			}

			if (block is ListItemBlock listItemBlock)
			{
				tuple.Item2[tuple.Item1] += "-";

				List<Block> lBlock = listItemBlock.ToList();
				foreach (Block block1 in lBlock)
				{
					ProcessMDBlocks(ref tuple, block1);
				}

				tuple.Item2[tuple.Item1] += "<br/>";

				return;
			}

			if (block is QuoteBlock quoteBlock)
			{
				tuple.Item2[tuple.Item1] += "<blockquote>";

				List<Block> lB = quoteBlock.ToList();

				foreach (Block b in lB)
				{
					ProcessMDBlocks(ref tuple, b);
				}

				if (tuple.Item2[1] != null &&
					tuple.Item2[1].StartsWith("<blockquote>") == true &&
					tuple.Item2[1].EndsWith("</blockquote>") == false)
				{
					tuple.Item2[1] += "</blockquote>";
					return;
				}

				tuple.Item2[tuple.Item1] += "</blockquote>";

				return;
			}

			if (block is Table table)
			{
				tuple.Item2[tuple.Item1] += "<table>";

				List<Block> lB = table.ToList();

				foreach (Block b in lB)
				{
					ProcessMDBlocks(ref tuple, b);
				}

				tuple.Item2[tuple.Item1] += "</table>";

				return;
			}

			if (block is TableCell tableCell)
			{
				tuple.Item2[tuple.Item1] += "<td>";

				List<Block> lB = tableCell.ToList();

				foreach (Block b in lB)
				{
					ProcessMDBlocks(ref tuple, b);
				}

				tuple.Item2[tuple.Item1] += "</td>";

				return;
			}

			if (block is TableRow tableRow)
			{
				tuple.Item2[tuple.Item1] += "<tr>";

				List<Block> lB = tableRow.ToList();

				foreach (Block b in lB)
				{
					ProcessMDBlocks(ref tuple, b);
				}

				tuple.Item2[tuple.Item1] += "</tr>";

				return;
			}

			//if (block is FencedCodeBlock fencedCodeBlock) 
			//{
			//	tuple.Item2[tuple.Item1] += "<pre>";
			//
			//	foreach (StringLine s in fencedCodeBlock.Lines.Lines)
			//	{
			//		tuple.Item2[tuple.Item1] += s;
			//	}
			//
			//	tuple.Item2[tuple.Item1] += "</pre>";
			//
			//	return;
			//}


			if (block is FencedCodeBlock ||
				block is ThematicBreakBlock ||
				block is ListBlock ||
				block is HtmlBlock ||
				block is LinkReferenceDefinitionGroup)
			{
				//TODO!
				return;
			}

			Log(block.GetType().Name);
		}
		private void ProcessMDInlines(ref ValueTuple<int, string[]> tuple, Inline inline)
		{
			if (inline is LiteralInline literalInline)
			{
				string text = literalInline.ToString();

				if (tuple.Item1 != 0 && inline.Parent.ParentBlock is HeadingBlock headingBlock)
				{
					if (text.StartsWith("Value"))
					{
						tuple.Item1 = 5;
						return;
					}
					if (text.StartsWith("Return"))
					{
						tuple.Item1 = 6;
						return;
					}
					if (text.StartsWith("See also"))
					{
						tuple.Item1 = 7;
						return;
					}

					tuple.Item1 = 4;
				}

				if (text.Contains("slug:") && tuple.Item1 == 0)
				{
					Regex regex = new(@"(slug:[\s\S]+?)\n");

					string temp = regex.Match(text).ToString();
					if (temp != "")
						text = regex.Match(text).ToString();

					text = text.Replace("slug:", "").Trim();
					text = text.Replace("\\n", "").Trim();

					tuple.Item2[9] = $"<para><seealso href=\"https://developer.mozilla.org/en-US/docs/{text.Trim()}\"><em>See also on MDN</em></seealso></para>";

					string[] names = text.Split('/');

					tuple.Item2[tuple.Item1] = "<";

					foreach (string item in names)
					{
						if (item == "Web")
							continue;
						if (item == "API")
							continue;

						tuple.Item2[tuple.Item1] += item.FirstCharToUpperCase().Trim();
					}

					tuple.Item2[tuple.Item1] = tuple.Item2[tuple.Item1].Replace("()", "");
					tuple.Item2[tuple.Item1] = tuple.Item2[tuple.Item1].Trim();
					tuple.Item2[tuple.Item1] += ">";

					tuple.Item1 = 3;

					return;
				}


				//
				//
				//
				//
				//
				//
				//
				//
				//TODO!!!!!!!!!!!!!!!!!!
				//Do somthing with {{apiref}}, ignore?, and generally do something with {{...}}

				Regex reff = new(@"domx?ref", RegexOptions.IgnoreCase);

				Regex t = new(@"{{1,}[(\S\s)]+?}{1,}", RegexOptions.IgnoreCase);

				if (t.IsMatch(text) &&
					tuple.Item1 == 1 &&
					tuple.Item2[1] == null &&
					!reff.IsMatch(text))
				{
					tuple.Item1 = 3;
					return;
				}

				Regex api = new(@"{{apiref", RegexOptions.IgnoreCase);
				if (api.IsMatch(text) &&
					!reff.IsMatch(text))
				{
					return;
				}

				//if (t.IsMatch(text) &&
				//	!reff.IsMatch(text))
				//{
				//	return;
				//}
				//
				//
				//
				//
				//
				//
				//
				//

				if (tuple.Item1 != 0)
				{
					tuple.Item2[tuple.Item1] += ProcessMDString(text);
					return;
				}

				return;
			}

			if (inline is EmphasisInline emphasisInline)
			{
				List<Inline> eiL = emphasisInline.ToList();

				foreach (Inline item in eiL)
				{
					tuple.Item2[tuple.Item1] += $"<strong>";
					ProcessMDInlines(ref tuple, item);
					tuple.Item2[tuple.Item1] += $"</strong>";
				}

				return;
			}

			//CodeInline
			if (inline is CodeInline codeInline)
			{
				tuple.Item2[tuple.Item1] += $"<c>{ProcessMDString(codeInline.Content)}</c>";
				return;
			}

			//link
			if (inline is LinkInline linkInline)
			{
				if (linkInline.Url.StartsWith("https://"))
					tuple.Item2[tuple.Item1] += $"<see href=\"{ProcessMDString(linkInline.Url)}\">";
				else
					tuple.Item2[tuple.Item1] += $"<see href=\"https://developer.mozilla.org{linkInline.Url}\">";

				List<Inline> liL = linkInline.ToList();
				foreach (Inline item in liL)
				{
					ProcessMDInlines(ref tuple, item);
				}
				tuple.Item2[tuple.Item1] += "</see>";
				return;
			}
			//link
			if (inline is AutolinkInline autolinkInline)
			{
				tuple.Item2[tuple.Item1] += $"<see href=\"{autolinkInline.Url}\"/>";
				return;
			}

			//HtmlInline
			if (inline is HtmlInline htmlInline)
			{
				tuple.Item2[tuple.Item1] += $"{ProcessMDString(htmlInline.Tag)}";
				return;
			}

			//HtmlEntityInline
			if (inline is HtmlEntityInline htmlEntityInline)
			{
				tuple.Item2[tuple.Item1] += $"{ProcessMDString(htmlEntityInline.Original.ToString())}";
				return;
			}

			//LinkDelimiterInline
			//?
			if (inline is LinkDelimiterInline linkDelimiterInline)
			{
				List<Inline> lI = linkDelimiterInline.ToList();

				foreach (Inline i in lI)
				{
					ProcessMDInlines(ref tuple, i);
				}
				return;
			}

			//PipeTableDelimiterInline
			//?
			if (inline is PipeTableDelimiterInline tableDelimiterInline)
			{
				List<Inline> tI = tableDelimiterInline.ToList();

				foreach (Inline i in tI)
				{
					ProcessMDInlines(ref tuple, i);
				}
				return;
			}
			//
			if (inline is LineBreakInline breakInline)
			{
				tuple.Item2[tuple.Item1] += $"<br/>";
				return;
			}

			Log(inline.GetType().Name);
		}
		private string ProcessMDString(string str)
		{
			Regex regex = new(@"{{domxref\(([\s\S]+?)\)(\s+)?}}", RegexOptions.IgnoreCase);

			MatchCollection matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				foreach (Match _match in matchCollection)
				{
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(","))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("()", "");

					string[] arr = value.Split(".");

					value = "";
					foreach (string s in arr)
					{
						value += s.FirstCharToUpperCase();
					}

					if (value.Contains("\""))
						str = regex.Replace(str, "<see cref=" + value + "/>", 1);
					else
						str = regex.Replace(str, "<see cref=\"" + value + "\"/>", 1);
				}

				return str;
			}
			
			regex = new(@"{{jsxref\(([\s\S]+?)\)(\s+)?}}", RegexOptions.IgnoreCase);

			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				foreach (Match _match in matchCollection)
				{
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(","))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("()", "");

					string[] arr = value.Split(".");

					value = "";
					foreach (string s in arr)
					{
						value += s.FirstCharToUpperCase();
					}

					if (value.Contains("\""))
						str = regex.Replace(str, "<see cref=" + value + "/>", 1);
					else
						str = regex.Replace(str, "<see cref=\"" + value + "\"/>", 1);
				}

				return str;
			}


			regex = new(@"\[([^\[]+)\]\((\/{1,}.*)\)");

			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				foreach (Match _match in matchCollection)
				{
					Group? group = _match.Groups[1];
					string name = group.Value;

					group = _match.Groups[2];
					string uri = "https://developer.mozilla.org/en-US/docs/" + group.Value;

					str = str.Replace(_match.Groups[0].Value, $"<see href=\"{uri}\">{name}</see>");
				}
				return str;
			}

			//"	&quot;
			//'	&apos;
			//<	&lt;
			//>	&gt;
			//&	&amp;
			str = str.Replace("\"", "&quot;");
			str = str.Replace("'", "&apos;");
			str = str.Replace("<", "&lt;");
			str = str.Replace(">", "&gt;");
			str = str.Replace("&", "&amp;");

			str = str.Replace("@", "_");

			return str;
		}

		private static void Log(string message, [CallerFilePath] string? file = null, [CallerMemberName] string? member = null, [CallerLineNumber] int line = 0)
		{
			Trace.WriteLine($"({line}):{Path.GetFileName(file)} {member}: {message}");
		}
	}
}