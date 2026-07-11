using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Services;

public sealed class InfoBarService
{
    private InfoBar? _infoBar;

    public void Register(InfoBar infoBar)
    {
        _infoBar = infoBar;
    }

    public void Show(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        if (_infoBar == null) return;
        _infoBar.Title = title;
        _infoBar.Message = message;
        _infoBar.Severity = severity;
        _infoBar.IsOpen = true;
    }

    public void Success(string message) => Show("Sucesso", message, InfoBarSeverity.Success);
    public void Error(string message) => Show("Erro", message, InfoBarSeverity.Error);
    public void Warning(string message) => Show("Aviso", message, InfoBarSeverity.Warning);
    public void Info(string message) => Show("Info", message, InfoBarSeverity.Informational);
}
