using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Tables;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenDocsLib
{
	public class GenDocs
	{
		private string _Output = string.Empty;
		private string _Path = string.Empty;

		public GenDocs()
		{
			if (File.Exists(Directory.GetCurrentDirectory() + "/debugGenDocs.txt"))
				File.Delete(Directory.GetCurrentDirectory() + "/debugGenDocs.txt");

			Trace.Listeners.Add(new TextWriterTraceListener("debugGenDocs.txt"));
			Trace.AutoFlush = true;
			Trace.Listeners.Add(new ConsoleTraceListener());
		}

		public async Task GenerateDocs(string path, string output)
		{
			_Output = output + "\\Docs";
			_Path = path;

			if (!Directory.Exists(_Output))
			{
				Directory.CreateDirectory(_Output);
			}

			if (File.Exists(Path.Combine(_Output, "DocsFail.generated.txt")))
			{
				File.Delete(Path.Combine(_Output, "DocsFail.generated.txt"));
			}

			await ProcessDirectory(path);

			Log("--- Done!");
		}

		private async Task ProcessDirectory(string targetDirectory)
		{
			string name = string.Empty;

			if (targetDirectory == _Path)
			{
				name = "index";
			}
			else 
			{
				string _str = targetDirectory.Remove(0, _Path.Length);
				string[] _names = _str.Split("\\");
				_str = string.Empty;
				foreach (string item in _names)
				{
					_str += item.FirstCharToUpperCase();
				}
				name = _str;

				if(name.Contains('_'))
				{
					string[] strings = name.Split("_");
					name = string.Empty;
					foreach (string item in strings) 
					{
						name += name.FirstCharToUpperCase();
					}
				}

				if (name.Length >= 60) 
				{
					name = name.Substring(0, 60);
				}
			}

			if (!Directory.Exists(_Output + $"\\{name}"))
			{
				Directory.CreateDirectory(_Output + $"\\{name}");
			}

			string[] fileEntries = Directory.GetFiles(targetDirectory);

			foreach (string fileName in fileEntries) 
			{
				if (fileName.EndsWith(".md"))
				{
					if (!File.Exists(Path.Combine(_Output + $"\\{name}", $"{name}.generated.xml")))
					{
						FileStream fS = File.Create(Path.Combine(_Output + $"\\{name}", $"{name}.generated.xml"));
						fS.Dispose();
					}

					string _fileStr = await File.ReadAllTextAsync(Path.Combine(_Output + $"\\{name}", $"{name}.generated.xml"));

					StringBuilder _sb = new();
					_sb.Append("<docs>");
					_sb.AppendLine();

					ProcessFile(ref _sb, fileName);

					_sb.Append("</docs>");
					string _sbStr = _sb.ToString();

					if(_fileStr != _sbStr)
						await File.WriteAllTextAsync(Path.Combine(_Output + $"\\{name}", $"{name}.generated.xml"), _sbStr);
				}
			}

			// Recurse into subdirectories of this directory.
			string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
			foreach (string subdirectory in subdirectoryEntries)
				await ProcessDirectory(subdirectory);
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
				ProcessMDBlocks(ref tuple, blocks[i]);
			}

			if (tuple.Item2[0] == null)
			{
				Log($"xmlStr[0] == null, path:{path.Replace("\\", "/").Substring(Path.GetPathRoot(path.Replace("\\", "/")).Length)}\n");
				return;
			}

			if (tuple.Item2[0] == "<>")
			{
				Log($"xmlStr[0] == \"<>\", path:{path.Replace("\\", "/").Substring(Path.GetPathRoot(path.Replace("\\", "/")).Length)}\n");
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
				List<Block> lB = quoteBlock.ToList();
				tuple.Item2[tuple.Item1] += "<blockquote";

				if (lB[0] is ParagraphBlock _p)
				{
					if (_p.Inline.ToList().Count > 1)
					{
						EmphasisInline _eI = _p.Inline.ToList()[0] as EmphasisInline;
						if (_eI != null)
						{
							string _str = (_eI.ToList()[0] as LiteralInline).ToString();
							if (_str.Contains("Note")) 
							{
								tuple.Item2[tuple.Item1] += " class=\"NOTE\"><h5>NOTE</h5";
							}
							if (_str.Contains("Warning"))
							{
								tuple.Item2[tuple.Item1] += " class=\"WARNING\"><h5>WARNING</h5";
							}
						}
					}
				}

				tuple.Item2[tuple.Item1] += ">";

				foreach (Block b in lB)
				{
					ProcessMDBlocks(ref tuple, b);
				}

				if (tuple.Item2[1] != null &&
					tuple.Item2[1].Contains("<blockquote") == true &&
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

				if (tuple.Item1 != 0 && inline.Parent.ParentBlock is HeadingBlock)
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

					tuple.Item2[9] = $"<para><seealso href=\"https://developer.mozilla.org/en-US/docs/{text.Trim()}\"> <em>See also on MDN</em> </seealso></para>";

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

				Regex reff = new(@"domx?ref", RegexOptions.IgnoreCase);

				Regex mustache = new(@"{{1,}[(\S\s)]+?}{1,}", RegexOptions.IgnoreCase);

				if (mustache.IsMatch(text) &&
					tuple.Item1 == 1 &&
					tuple.Item2[1] == null &&
					!reff.IsMatch(text))
				{
					tuple.Item1 = 3;
					if (text.Contains("deprecated_header", StringComparison.OrdinalIgnoreCase))
					{
						tuple.Item2[1] += "<div class=\"IMPORTANT\"><h5>IMPORTANT</h5> <strong>Deprecated</strong></div> ";
					}
					return;
				}

				//
				//Mustache
				if (mustache.IsMatch(text))
				{
					tuple.Item2[tuple.Item1] += ProcessMustache(text);
					return;
				}
				
				//
				//md string
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
			if (inline is LineBreakInline)
			{
				tuple.Item2[tuple.Item1] += $"<br/>";
				return;
			}

			Log(inline.GetType().Name);
		}

		private string ProcessMDString(string str)
		{
			Regex regex = new(@"\[([^\[]+)\]\((\/{1,}.*)\)");

			MatchCollection matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
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

		private string ProcessMustache(string str) 
		{
			Regex regex = new(@"{{ ?domxref\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			MatchCollection matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					if (value.Contains('"'))
						value = value.Replace("\"", "");

					value = value.Replace("()", "");

					string[] arr = value.Split(".");

					value = "";
					foreach (string s in arr)
					{
						value += s.FirstCharToUpperCase() + ".";
					}

					if(value.EndsWith('_'))
						value = value.Remove(value.Length - 1);
					
					if (value.Contains('"'))
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
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					if (value.Contains('"'))
						value = value.Replace("\"", "");

					value = value.Replace("()", "");

					string[] arr = value.Split(".");

					value = "";
					foreach (string s in arr)
					{
						value += s.FirstCharToUpperCase();
					}

					if (value.Contains('"'))
						str = regex.Replace(str, "<see cref=" + value + "/>", 1);
					else
						str = regex.Replace(str, "<see cref=\"" + value + "\"/>", 1);
				}

				return str;
			}

			regex = new(@"{{ ?glossary\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("\"", "");

					//https://developer.mozilla.org/en-US/docs/Glossary/
					str = regex.Replace(str, $"<see href=\"https://developer.mozilla.org/en-US/docs/Glossary/{value}\">{value}</see>", 1);
				}

				return str;
			}

			
			regex = new(@"{{ ?htmlelement\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("\"", "");

					//https://developer.mozilla.org/en-US/docs/Web/HTML/Element/
					str = regex.Replace(str, $"<see href=\"https://developer.mozilla.org/en-US/docs/Web/HTML/Element/{value}\">{value}</see>", 1);
				}

				return str;
			}

			
			regex = new(@"{{ ?cssxref\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("\"", "");

					//https://developer.mozilla.org/en-US/docs/Web/CSS/
					str = regex.Replace(str, $"<see href=\"https://developer.mozilla.org/en-US/docs/Web/CSS/{value}\">{value}</see>", 1);
				}

				return str;
			}

			regex = new(@"{{ ?livesamplelink\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("\"", "");

					str = regex.Replace(str, value, 1);
				}

				return str;
			}

			regex = new(@"{{ ?httpheader\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("\"", "");

					//https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/
					str = regex.Replace(str, $"<see href=\"https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/{value}\">{value}</see>", 1);
				}
				
				return str;
			}

			regex = new(@"{{ ?WebExtAPIRef\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("\"", "");

					str = regex.Replace(str, value, 1);
				}

				return str;
			}

			regex = new(@"{{ ?SVGElement\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("\"", "");

					//https://developer.mozilla.org/en-US/docs/Web/API/SVGElement
					str = regex.Replace(str, $"<see href=\"https://developer.mozilla.org/en-US/docs/Web/API/SVGElement{value}\">{value}</see>", 1);
				}

				return str;
			}

			regex = new(@"{{ ?SVGAttr\(([\s\S]+?)\)(\s+)? ?}}", RegexOptions.IgnoreCase);
			matchCollection = regex.Matches(str);
			if (matchCollection.Count > 0)
			{
				for (int i = 0; i < matchCollection.Count; i++)
				{
					Match _match = matchCollection[i];
					Group? group = _match.Groups[1];
					string value = group.Value;

					if (value.Contains(','))
					{
						//TODO!
						value = value.Split(",").First();
					}

					value = value.Replace("\"", "");

					
					str = regex.Replace(str, value, 1);
				}

				return str;
			}

			if (str.StartsWith("{{") && str.EndsWith("}}")) 
			{
				regex = new(@"{{([\s\S]+?)}}", RegexOptions.IgnoreCase);

				matchCollection = regex.Matches(str);
				if (matchCollection.Count > 0)
				{
					for (int i = 0; i < matchCollection.Count; i++)
					{
						Match _match = matchCollection[i];
						Group? group = _match.Groups[1];
						string value = group.Value;

						
						if (value.Contains("Deprecated_Header", StringComparison.OrdinalIgnoreCase))
						{
							str = regex.Replace(str, "<div class=\"IMPORTANT\"><strong>Deprecated</strong></div>", 1);
							continue;
						}
						
						if (value.Contains("InheritanceDiagram", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("EmbedLiveSample", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("SecureContext_Header", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("DefaultAPISidebar", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("PreviousMenu", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("PreviousNext", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("Next", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("Previous", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("EmbedGHLiveSample", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("HTTPSidebar", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("DOMAttributeMethods", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("ListGroups", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("APIListAlpha", StringComparison.OrdinalIgnoreCase))
						{
							str = regex.Replace(str, "", 1);
							continue;
						}
						
						if (value.Contains("compat", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("specifications", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("apiref", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("AvailableInWorkers", StringComparison.OrdinalIgnoreCase) ||
							value.Contains("SVGAttr", StringComparison.OrdinalIgnoreCase)) 
						{
							str = regex.Replace(str, value, 1);
							continue;
						}

						str = regex.Replace(str, value, 1);
						Log($"Missing Mustache Default: {str}");
					}

					return str;
				}
			}

			Log($"Missing Mustache: {str}");
			return str;
		}

		private static void Log(string message, [CallerFilePath] string? file = null, [CallerMemberName] string? member = null, [CallerLineNumber] int line = 0)
		{
			Trace.WriteLine($"({line}):{Path.GetFileName(file.Replace("\\", "/"))} {member}: {message}");
		}
	}
}