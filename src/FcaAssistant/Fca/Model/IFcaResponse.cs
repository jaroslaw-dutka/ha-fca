namespace FcaAssistant.Fca.Model;

public interface IFcaResponse
{
    bool CheckForError();

    void ThrowOnError(string message);
}