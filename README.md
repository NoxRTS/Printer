# PDF Folder Printer

A simple Windows desktop application that prints **all PDF documents in a selected folder** with a single click. Built with .NET 10 and Windows Forms, it renders each PDF using the built-in `Windows.Data.Pdf` API and sends every page directly to your default Windows printer — no browser or third-party PDF reader required.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)

## Features

- 📁 **Browse** for any folder containing PDF files
- 📄 Automatically lists every `*.pdf` in the folder
- 🖨️ **Prints all PDFs** to your default printer with one click
- 📊 Live progress bar and per-file status updates
- 🪟 Clean, resizable Windows Forms GUI
- 📦 Ships as a **self-contained single-file `.exe`** — no .NET install needed

## Requirements

- Windows 10 (build 19041 / version 2004) or later
- A configured **default printer** (Windows Settings → Bluetooth & devices → Printers & scanners)

> When running from source you also need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

## Usage

1. Launch **`Printer.exe`**.
2. Click **Browse…** and select the folder containing your PDF files.
3. Review the list of PDFs found.
4. Click **Print All PDFs**.

Each PDF is rendered and spooled to your default printer in turn, with the progress bar tracking completion.

## Building from source

```powershell
# Clone the repository
git clone https://github.com/NoxRTS/Printer.git
cd Printer

# Run directly
dotnet run

# Or produce a self-contained single-file executable
dotnet publish -c Release -r win-x64 --self-contained true `
	-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
	-o publish
```

The resulting executable is written to `publish\Printer.exe`.

## How it works

The app uses the WinRT **`Windows.Data.Pdf.PdfDocument`** API to render each page of a PDF to a bitmap, then draws those bitmaps onto a **`System.Drawing.Printing.PrintDocument`**, scaling each page to fit the printable area. This sends the output straight to the Windows print spooler, so it works reliably regardless of which PDF viewer (if any) is installed.

## Project structure

| File | Purpose |
|------|---------|
| `Program.cs` | Application entry point |
| `MainForm.cs` | The Windows Forms UI and printing logic |
| `Printer.csproj` | Project configuration and build settings |
| `CreateIcon.ps1` | Script that generates the printer app icon |
| `app.ico` | Application icon |

## License

Released under the [MIT License](LICENSE).
