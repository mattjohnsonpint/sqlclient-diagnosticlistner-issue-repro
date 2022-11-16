using System.Reflection;
using Microsoft.Data.SqlClient;

namespace SqlClientBugDemo;

public class MyDiagnosticListener: IObserver<KeyValuePair<string, object?>>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        // We're only interested in this one message type for this demo
        if (value.Key != "Microsoft.Data.SqlClient.WriteCommandError")
            return;

        // Unfortunately, we have to use reflection to look at the data
        var data = value.Value!;
        var dataType = data.GetType();

        var command = (SqlCommand) dataType
            .GetProperty("Command", BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(data)!;

        var exception = (Exception) dataType
            .GetProperty("Exception", BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(data)!;
        
        Console.WriteLine($"Error: {exception.Message}");
        Console.WriteLine($"Command: {command.CommandText}");
    }
}