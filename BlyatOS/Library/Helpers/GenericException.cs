using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlyatOS;

internal class GenericException : Exception
{
    public string EMessage;
    public string Label;
    public string ComesFrom;

    public GenericException(string eMessage, string label = "", string comesFrom = "")
    {
        EMessage = eMessage;
        Label = label;
        ComesFrom = comesFrom;
    }
}
