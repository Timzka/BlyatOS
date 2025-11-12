using System;

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
