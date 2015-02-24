using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    class CWebTemplateProcessor : IScriptProcessor
    {
        public ScriptResult ProcessScript(Stream stream, IDictionary<string, string> requestParameters)
        {
            StringBuilder sb = new StringBuilder();
            CscriptProcessor scriptProcessor = new CscriptProcessor();
            List<String> inputLines = new List<String>();

            StreamReader reader = new StreamReader(stream);
            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
                inputLines.Add(line);
            }

            String parsedScript = ProcessInput(inputLines, sb);

            stream = createStream(parsedScript);

            ScriptResult result = scriptProcessor.ProcessScript(stream, requestParameters);
            return result;
        }

        private string ProcessInput(List<string> inputLines, StringBuilder sb)
        {
            int length = inputLines.Count;
            bool brace, atBrace, request;
            int index = 0, openingIndex = 0;

            foreach (String line in inputLines)
            {
                if (index > openingIndex)
                {
                    /* we've already accounted for these when we found the opening and closing brace
                     * so, we need to skip these interations */
                    index--;
                    continue;
                }
                brace = line.Contains("{");
                atBrace = line.Contains("@{");
                request = line.Contains("request[");

                if (request)
                {
                    sb.Append("try {");
                }

                if (atBrace)
                {
                    //this is a parameter or variable, so we need to separate the call
                    var separatedLine = line.Split(new char[] { '{', '}' });

                    //and then append the surrounding html along with the code to output the var
                    sb.Append(formatMarkup(separatedLine[0].Substring(0, separatedLine[0].Length - 1)));
                    sb.Append(formatVar(separatedLine[1]));
                    sb.Append(formatMarkup(separatedLine[2]));

                    if (request)
                    {
                        sb.Append("} catch (Exception e) {Console.Write(e); wout.WriteLine(\"<h4>Not Provided!</h4>\");}");
                    }
                }

                else if (brace)
                {
                    //this is a code block, so we need to find the end of it
                    bool found = false;
                    int match = 0;
                    index = inputLines.IndexOf(line);
                    openingIndex = index;
                    
                    //keep looping as long as the closing brace that matches the opening brace hasn't been located
                    while ((match != 0) || (!found))
                    {
                        index++;
                        if (inputLines[index].Contains("}"))
                        {
                            if (match != 0)
                                match--;
                            else
                                found = true;
                        }

                        else if (inputLines[index].Contains("{"))
                        {
                            match++;
                        }
                    }

                    /* This won't work if we append the braces on either side, so the lines
                     * containing those are ommitted */
                    for (int a = inputLines.IndexOf(line) + 1; a < index; a++)
                    {
                        sb.Append(inputLines[a]);
                    }

                    if (request)
                    {
                        sb.Append("} catch (Exception e) {Console.Write(e); wout.WriteLine(\"<h4>Unable to set variable!</h4>\");}");
                    }
                }

                else
                {
                    //Regular HTML statement
                    sb.Append(formatMarkup(line));
                }

                //reset for next iteration
                brace = false;
                atBrace = false;
                request = false;
            }

            return sb.ToString();
        }

        private Stream createStream(String template)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter w = new StreamWriter(ms);
            w.Write(template);

            //Have to flush the stream to make sure all data is properly written
            w.Flush();

            //It took an hour to figure out that stream position was a thing :/
            ms.Position = 0;
            return ms;
        }

        private String formatMarkup(String markup)
        {
            //need to make sure that quotes in HTML will be treated as such - element data and the like
            return String.Format("wout.WriteLine(\"{0}\");", markup.Replace("\"", "\\\""));
            //return String.Format("wout.WriteLine(\"{0}\");", markup);

        }

        private String formatVar(String var)
        {
            //This will write the code blocks directly to the stream, making them execuatble.
            return String.Format("wout.WriteLine({0});", var);
        }
    }
}
