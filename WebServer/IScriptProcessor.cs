﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    /* Generic interface for a script processor that will take a file
     * and a set of request paramters and generat a script result. 
     * 
     * A script result is a flag indicating an error and an HTML
     * document that was produced by running the script.
     */
    interface IScriptProcessor
    {
        ScriptResult ProcessScript(Stream stream, IDictionary<string, string> requestParameters);
    }
}
