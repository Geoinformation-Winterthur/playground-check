using Microsoft.AspNetCore.Components;

public class SnackbarService
{
    private Action<string, int>? _showSnackbar;

    /// <summary>
    /// Wird von der Snackbar-Komponente gesetzt, um den Anzeigen-Aufruf zu verknüpfen.
    /// </summary>
    public void Register(Action<string, int> showSnackbarAction)
    {
        _showSnackbar = showSnackbarAction;
    }

    /// <summary>
    /// Zeigt eine Snackbar-Nachricht an.
    /// </summary>
    public Task ShowAsync(string message, int durationMs = 3000)
    {
        _showSnackbar?.Invoke(message, durationMs);
        return Task.CompletedTask;
    }
}
