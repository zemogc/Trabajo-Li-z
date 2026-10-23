namespace Supermarket.Core.Presentation;

/// <summary>
/// Feedback capabilities every screen offers while presenters run operations against the database.
/// </summary>
public interface IOperationView
{
    void SetBusy(bool isBusy);

    void ShowMessage(string message);

    void ShowErrors(IReadOnlyList<string> errors);

    bool Confirm(string question);
}
