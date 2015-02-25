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

        private StringBuilder sb;

        public ScriptResult ProcessScript(Stream stream, IDictionary<string, string> requestParameters)
        {
            sb = new StringBuilder();
            CscriptProcessor scriptProcessor = new CscriptProcessor();
            List<String> inputLines = new List<String>();

            StreamReader reader = new StreamReader(stream);
            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
                inputLines.Add(line);
            }

            String parsedScript = ProcessInput(inputLines);

            stream = createStream(parsedScript);

            ScriptResult result = scriptProcessor.ProcessScript(stream, requestParameters);
            return result;
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

        private int handleCodeBlock(List<String> inputLines, int l)
        {
            //this is a code block, so we need to find the end of it
            bool found = false, sameLine = false;
            //int match = 0;
            int index = l;

            //keep looping as long as the closing brace that matches the opening brace hasn't been located
            while ((!found))
            {

                if (inputLines[index].Contains("}"))
                {

                    if (inputLines[index + 1].Contains("<"))
                    {
                        found = true;
                        if (index == l)
                            sameLine = true;
                        continue;
                    }

                }

                index++;
            }

            /* This won't work if we append the braces on either side, so the lines
                * containing those are ommitted */
            if (sameLine)
            {
                int openingBrace = inputLines[l].IndexOf('{');
                int closingBrace = inputLines[l].IndexOf('}');
                String input = _substring(inputLines[l], openingBrace + 1, closingBrace);
                sb.Append(input);
            }
            else
            {
                for (int a = l + 1; a < index; a++)
                {
                    sb.Append(inputLines[a]);
                }
            }

            return index;
        }

        private string ProcessInput(List<string> inputLines)
        {
            int length = inputLines.Count;
            bool brace, atBrace, request, end;
            int index = 0, openingIndex = 0;

            for (int l = 0; l < length; l++ )
            {
                if (index > openingIndex)
                {
                    /* we've already accounted for these when we found the opening and closing brace
                        * so, we need to skip these interations */
                    index--;
                    continue;
                }
                brace = inputLines[l].Contains("{");
                atBrace = inputLines[l].Contains("@{");
                request = inputLines[l].Contains("request[");
                end = inputLines[l].Contains("}");

                if (request)
                {
                    sb.Append("try {");
                }

                if (atBrace)
                {
                    //this is a parameter or variable, so we need to separate the call
                    var separatedLine = inputLines[l].Split(new char[] { '{', '}' });

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
                    openingIndex = l;
                    index = handleCodeBlock(inputLines, l);

                    if (request )
                    {
                        sb.Append("} catch (Exception e) {Console.Write(e); wout.WriteLine(\"<h4>Unable to set variable!</h4>\");}");
                    }
                }

                else
                {
                    //Regular HTML statement
                    sb.Append(formatMarkup(inputLines[l]));
                }

                //reset for next iteration
                brace = false;
                atBrace = false;
                request = false;
                end = false;
            }

            return sb.ToString();
        }

        private String _substring(string text, int start, int length)
        {
            if (start >= text.Length)
                return "";
            if (start + length > text.Length)
                length = text.Length - start;
            return text.Substring(start, length-1);
        }

        
    }
}
