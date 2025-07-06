using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Storage;
using FTG.Core.Logging;
using FTG.Core.Manifest;
using FTG.Core.PDF;
using FTG.Core.CSV;

namespace FTG.MAUI;

public partial class FileProcessor :  ContentPage, INotifyPropertyChanged
{
    private string _inputFilePath = string.Empty;
    private string _outputFolderPath = string.Empty;
    private string _outputInfo = string.Empty;
    private bool _isProcessing = false;
    
    public FileProcessor()
    {
        InitializeComponent();
        BindingContext = this;
        
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        OutputFolderPath = SettingsManager.GetOutputFolderPath();
    }
    
    public string InputFilePath
    {
        get => _inputFilePath;
        set
        {
            _inputFilePath = value;
            OnPropertyChanged();
            if (!string.IsNullOrEmpty(value))
            {
                SettingsManager.SetLastInputFolderPath(value);
                SettingsManager.AddRecentFile(value);
            }
        }
    }

    public string OutputFolderPath
    {
        get => _outputFolderPath;
        set
        {
            _outputFolderPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasOutputFolder));
            SettingsManager.SetOutputFolderPath(value);
        }
    }

    public string OutputInfo
    {
        get => _outputInfo;
        set
        {
            _outputInfo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasOutputInfo));
        }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotProcessing));
        }
    }

    public bool IsNotProcessing => !IsProcessing;
    public bool HasOutputFolder => !string.IsNullOrWhiteSpace(OutputFolderPath);
    public bool HasOutputInfo => !string.IsNullOrWhiteSpace(OutputInfo);

    private async void OnSelectFileClicked(object sender, EventArgs e)
    {
        try
        {
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, [".pdf", ".csv", ".xlsx", "*"] },
            });

            var lastInputFolder = SettingsManager.GetLastInputFolderPath();
            
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a file to process",
                FileTypes = fileTypes
            });

            if (result != null)
            {
                InputFilePath = result.FullPath;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to select file: {ex.Message}", "OK");
        }
    }

    private async void OnSelectFolderClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync();
            
            if (result != null && result.IsSuccessful)
            {
                OutputFolderPath = result.Folder.Path;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to select folder: {ex.Message}", "OK");
        }
    }

    private async void OnProcessClicked(object sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(InputFilePath))
        {
            await DisplayAlert("Validation Error", "Please select an input file.", "OK");
            return;
        }

        if (!File.Exists(InputFilePath))
        {
            await DisplayAlert("Validation Error", "The selected input file does not exist.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputFolderPath))
        {
            await DisplayAlert("Validation Error", "Please select an output folder.", "OK");
            return;
        }

        if (!Directory.Exists(OutputFolderPath))
        {
            try
            {
                Directory.CreateDirectory(OutputFolderPath);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create output directory: {ex.Message}", "OK");
                return;
            }
        }

        // Start processing
        IsProcessing = true;
        OutputInfo = string.Empty;

        try
        {
            await ProcessFileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Processing Error", $"An error occurred during processing: {ex.Message}", "OK");
            OutputInfo = $"Error: {ex.Message}\n\nProcessing failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ProcessFileAsync()
    {
        var startTime = DateTime.Now;
        var inputFileName = Path.GetFileName(InputFilePath);
        var jobGuid = Guid.NewGuid();
        var jobOutputFolder = Path.Combine(OutputFolderPath, jobGuid.ToString());
        if (!Directory.Exists(jobOutputFolder))
        {
            GlobalLogger.LogInfo($"Creating output folder: {jobOutputFolder}");
            Directory.CreateDirectory(jobOutputFolder);
        }
        var scaleInterfaceFolderOutput = Path.Combine(jobOutputFolder, "scale_interface_files");
        if (!Directory.Exists(scaleInterfaceFolderOutput))
        {
            GlobalLogger.LogInfo($"Creating Scale Interface output folder: {scaleInterfaceFolderOutput}");
            Directory.CreateDirectory(scaleInterfaceFolderOutput);
        }
        var outputFileName = $"{Path.GetFileNameWithoutExtension(InputFilePath)}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(InputFilePath)}";
        var outputFilePath = Path.Combine(jobOutputFolder, outputFileName);
        var outputSimplifiedFileName = $"simplified_{Path.GetFileNameWithoutExtension(InputFilePath)}_{DateTime.Now:yyyyMMdd_HHmmss}";
        var outputSimplifiedFilePath = Path.Combine(jobOutputFolder, outputSimplifiedFileName);

        var info = new List<string>
        {
            $"Processing started: {startTime:yyyy-MM-dd HH:mm:ss}",
            $"Input file: {InputFilePath}",
            $"Output root folder: {OutputFolderPath}",
            $"Output file: {outputFileName}",
            "",
            $"Output Job Id: {jobGuid}",
            $"Output Job Folder: {jobOutputFolder}",
            $"Output Scale Interface Folder: {scaleInterfaceFolderOutput}",
            ""
        };

        OutputInfo = string.Join("\n", info);

        // Simulate processing with some actual file operations
        await Task.Delay(1000); // Simulate initial setup

        try
        {
            // Get file info
            var fileInfo = new FileInfo(InputFilePath);
            info.Add($"Input file size: {FormatFileSize(fileInfo.Length)}");
            info.Add($"Last modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            info.Add("");

            OutputInfo = string.Join("\n", info);
            
            info.Add("Reading manifest file content...");
            OutputInfo = string.Join("\n", info);

            FreshToGoManifest? manifest = null;
            
            if (fileInfo.Extension == ".pdf")
            {
                if (PdfProcessor.SimplifyPdf(InputFilePath, $"{outputSimplifiedFilePath}.pdf"))
                {
                    GlobalLogger.LogInfo($"PDF simplified successfully: {outputSimplifiedFileName}.pdf");
                    info.Add("");
                    info.Add($"Manifest PDF simplified successfully");
                    info.Add("");
                    OutputInfo = string.Join("\n", info);
                }
                else
                {
                    GlobalLogger.LogError("Failed to simplify PDF");
                    info.Add("❌ Failed to simplify PDF.");
                    OutputInfo = string.Join("\n", info);
                    return;
                }
            
                manifest = PdfProcessor.ConvertPdfToExcel($"{outputSimplifiedFilePath}.pdf", $"{outputSimplifiedFilePath}.xlsx");
                
                if (manifest == null)
                {
                    info.Add("❌ Failed to convert manifest to Excel.");
                    OutputInfo = string.Join("\n", info);
                    GlobalLogger.LogError("Failed to convert manifest into Excel");
                    return;
                }
                else
                {
                    info.Add($"Manifest read successfully");
                    info.Add($"   Manifest Date: {manifest.GetManifestDateString()}");
                    info.Add($"   Total Orders: {manifest.GetTotalOrders()}");
                    info.Add($"   Total Crates: {manifest.GetTotalCrates()}");
                    info.Add("");
                    info.Add($"Manifest data exported to Excel in output folder");
                    OutputInfo = string.Join("\n", info);
                }
            
                if(ManifestToScale.ConvertManifestToCsv(manifest, $"{outputSimplifiedFilePath}.csv"))
                {
                    GlobalLogger.LogInfo($"Manifest CSV generated successfully: {outputSimplifiedFilePath}.csv");
                    info.Add($"Manifest data exported as CSV in output folder");
                    OutputInfo = string.Join("\n", info);
                }
                else
                {
                    GlobalLogger.LogError("Failed to convert manifest to CSV");
                    info.Add("❌ Failed to convert manifest to CSV.");
                    OutputInfo = string.Join("\n", info);
                    return;
                }
                
                
                var receiptXmlPath = Path.Combine(scaleInterfaceFolderOutput, $"{manifest.Company.Company}_Receipt-{manifest.GetManifestDateString()}.rcxml");
                if(ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptXmlPath))
                {
                    GlobalLogger.LogInfo($"Receipt XML generated successfully: {receiptXmlPath}");
                    info.Add("");
                    info.Add($"Manifest receipt for Scale generated successfully");
                    info.Add($"Scale rcxml file saved to scale interface output folder");
                    OutputInfo = string.Join("\n", info);
                }
                else
                {
                    GlobalLogger.LogError("Failed to generate receipt XML");
                    info.Add("❌ Failed to generate receipt XML.");
                    OutputInfo = string.Join("\n", info);
                    return;
                }
                
                var shipmentXmlPath = Path.Combine(scaleInterfaceFolderOutput, $"{manifest.Company.Company}Shipments-{manifest.GetManifestDateString()}.shxml");
                if(ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentXmlPath))
                {
                    GlobalLogger.LogInfo($"Shipment XML generated successfully: {shipmentXmlPath}");
                    info.Add("");
                    info.Add($"Manifest shipments for Scale generated successfully");
                    info.Add($"Scale shxml file saved to scale interface output folder");
                    OutputInfo = string.Join("\n", info);
                }
                else
                {
                    GlobalLogger.LogError("Failed to generate shipment XML");
                    info.Add("❌ Failed to generate shipment XML.");
                    OutputInfo = string.Join("\n", info);
                    return;
                }
            }
            else if (fileInfo.Extension == ".csv" || fileInfo.Extension == ".xlsx")
            {
                manifest = AzuraFreshCsv.ConvertCsvToManifest(InputFilePath);
                
                if (manifest == null)
                {
                    info.Add("❌ Failed to read manifest.");
                    OutputInfo = string.Join("\n", info);
                    GlobalLogger.LogError("Failed to read manifest.");
                    return;
                }
                else
                {
                    info.Add($"Manifest read successfully");
                    info.Add($"   Manifest Date: {manifest.GetManifestDate()}");
                    info.Add($"   Total Orders: {manifest.GetTotalOrders()}");
                    info.Add($"   Total Crates: {manifest.GetTotalCrates()}");
                    info.Add("");
                    info.Add($"Manifest data exported to output folder");
                    OutputInfo = string.Join("\n", info);
                }
                
                var receiptXmlPath = Path.Combine(scaleInterfaceFolderOutput, $"{manifest.Company.Company}_Receipt-{manifest.GetManifestDateString()}.rcxml");
                if(ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptXmlPath))
                {
                    GlobalLogger.LogInfo($"Receipt XML generated successfully: {receiptXmlPath}");
                    info.Add("");
                    info.Add($"Manifest receipt for Scale generated successfully");
                    info.Add($"Scale rcxml file saved to scale interface output folder");
                    OutputInfo = string.Join("\n", info);
                }
                else
                {
                    GlobalLogger.LogError("Failed to generate receipt XML");
                    info.Add("❌ Failed to generate receipt XML.");
                    OutputInfo = string.Join("\n", info);
                    return;
                }
                
                var shipmentXmlPath = Path.Combine(scaleInterfaceFolderOutput, $"{manifest.Company.Company}_Shipments-{manifest.GetManifestDateString()}.shxml");
                if(ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentXmlPath))
                {
                    GlobalLogger.LogInfo($"Shipment XML generated successfully: {shipmentXmlPath}");
                    info.Add("");
                    info.Add($"Manifest shipments for Scale generated successfully");
                    info.Add($"Scale shxml file saved to scale interface output folder");
                    OutputInfo = string.Join("\n", info);
                }
                else
                {
                    GlobalLogger.LogError("Failed to generate shipment XML");
                    info.Add("❌ Failed to generate shipment XML.");
                    OutputInfo = string.Join("\n", info);
                    return;
                }
            }
            
            

            
            
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            info.Add("");
            info.Add($"Processing completed: {endTime:yyyy-MM-dd HH:mm:ss}");
            info.Add($"Total processing time: {duration.TotalSeconds:F1} seconds");
            info.Add("");
            info.Add("✓ Processing completed successfully!");

            OutputInfo = string.Join("\n", info);

            await DisplayAlert("Success", "File processed successfully!", "OK");
        }
        catch (Exception ex)
        {
            info.Add($"❌ Error during processing: {ex.Message}");
            OutputInfo = string.Join("\n", info);
            throw;
        }
    }

    private async void OnOpenOutputClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(OutputFolderPath) || !Directory.Exists(OutputFolderPath))
            {
                await DisplayAlert("Error", "Output folder does not exist.", "OK");
                return;
            }

            // Open folder in file explorer
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(OutputFolderPath)
            });
        }
        catch (Exception ex)
        {
            try
            {
                // Alternative method using Process.Start for desktop platforms
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    System.Diagnostics.Process.Start("explorer.exe", OutputFolderPath);
                }
                else if (DeviceInfo.Platform == DevicePlatform.macOS)
                {
                    System.Diagnostics.Process.Start("open", OutputFolderPath);
                }
                else
                {
                    await DisplayAlert("Error", $"Cannot open folder: {ex.Message}", "OK");
                }
            }
            catch (Exception ex2)
            {
                await DisplayAlert("Error", $"Failed to open output folder: {ex2.Message}", "OK");
            }
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:N1} {suffixes[counter]}";
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}