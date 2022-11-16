using System.Diagnostics;

namespace SqlClientBugDemo;

public class MyDiagnosticSubscriber: IObserver<DiagnosticListener>
{
    private readonly MyDiagnosticListener _myListener = new();

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "SqlClientDiagnosticListener")
        {
            listener.Subscribe(_myListener);
        }
    }
}
