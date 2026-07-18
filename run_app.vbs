Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "powershell.exe -NoProfile -ExecutionPolicy Bypass -File ""c:\Users\Pichau\Desktop\FinanceApp\run_app.ps1""", 0, False
