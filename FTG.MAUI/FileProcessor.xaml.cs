using System.ComponentModel;
using System.Diagnostics;
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
    private Guid _jobGuid = Guid.Empty;
    private string? _jobOutputFolder = null;
    
    public FileProcessor()
    {
        InitializeComponent();
        BindingContext = this;
        
        GlobalLogger.OnMessageLogged += OnLogMessageReceived;
        
        LoadSettings();
    }

    private void OnLogMessageReceived(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OutputInfo += $"{message}\n";
            OnPropertyChanged(nameof(OutputInfo));
        });
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
            
            if (result.IsSuccessful)
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
            switch (Path.GetExtension(InputFilePath))
            {
                case ".pdf":
                {
                    GlobalLogger.LogInfo($"Processing PDF file: {InputFilePath}");
                    GlobalLogger.LogInfo($"Processing started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    break;
                }
                case ".csv":
                {
                    GlobalLogger.LogInfo($"Processing CSV file: {InputFilePath}");
                    GlobalLogger.LogInfo($"Processing started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    break;
                }
                case ".xlsx":
                {
                    GlobalLogger.LogInfo($"Processing Excel file: {InputFilePath}");
                    GlobalLogger.LogInfo($"Processing started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    break;
                }
                default:
                {
                    GlobalLogger.LogError($"Unsupported file type: {Path.GetExtension(InputFilePath)}");
                    IsProcessing = false;
                    throw new NotSupportedException($"Unsupported file type: {Path.GetExtension(InputFilePath)}, Please select a PDF, CSV, or Excel file.");
                }
                    
            }
            
            await ProcessFileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Processing Error", $"An error occurred during processing: {ex.Message}", "OK");
            GlobalLogger.LogError($"Processing failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss} with error: {ex.Message}");
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
        _jobGuid = Guid.NewGuid();
        var jobOutputFolder = Path.Combine(OutputFolderPath, _jobGuid.ToString());
        _jobOutputFolder = jobOutputFolder;
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
        
        GlobalLogger.LogInfo($"Input file: {InputFilePath}");
        GlobalLogger.LogInfo($"Output root folder: {OutputFolderPath}");
        GlobalLogger.LogInfo($"Output file: {outputFileName}");
        GlobalLogger.LogInfo($"Output Job Id: {_jobGuid}");
        GlobalLogger.LogInfo($"Output Job Folder: {jobOutputFolder}");
        GlobalLogger.LogInfo($"Output Scale Interface Folder: {scaleInterfaceFolderOutput}");
        
        try
        {
            // Get file info
            var fileInfo = new FileInfo(InputFilePath);
            GlobalLogger.LogInfo($"Input file size: {FormatFileSize(fileInfo.Length)}");
            GlobalLogger.LogInfo($"Input file name: {fileInfo.Name}");
            GlobalLogger.LogInfo($"Last modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            GlobalLogger.LogInfo("Reading input file information...");

            FreshToGoManifest? manifest = null;
            
            if (fileInfo.Extension == ".pdf")
            {
                if (PdfProcessor.SimplifyPdf(InputFilePath, $"{outputSimplifiedFilePath}.pdf"))
                {
                    GlobalLogger.LogInfo($"PDF simplified successfully: {outputSimplifiedFileName}.pdf");
                }
                else
                {
                    GlobalLogger.LogError("❌ Failed to simplify PDF.");
                    throw new Exception("Failed to simplify PDF");;
                }
            
                manifest = PdfProcessor.ConvertPdfToExcel($"{outputSimplifiedFilePath}.pdf", $"{outputSimplifiedFilePath}.xlsx");
                
                if (manifest == null)
                {
                    GlobalLogger.LogError("❌ Failed to convert manifest to Excel.");
                    throw new Exception("Failed to convert manifest into Excel");
                }

                GlobalLogger.LogInfo($"Manifest read successfully");
                GlobalLogger.LogInfo($"   Manifest Date: {manifest.GetManifestDateString()}");
                GlobalLogger.LogInfo($"   Total Orders: {manifest.GetTotalOrders()}");
                GlobalLogger.LogInfo($"   Total Crates: {manifest.GetTotalCrates()}");
                GlobalLogger.LogInfo($"Manifest data exported to Excel in output folder");

                if(ManifestToScale.ConvertManifestToCsv(manifest, $"{outputSimplifiedFilePath}.csv"))
                {
                    GlobalLogger.LogInfo($"Manifest CSV generated successfully: {outputSimplifiedFilePath}.csv");
                    GlobalLogger.LogInfo($"Manifest data exported as CSV in output folder");
                }
                else
                {
                    GlobalLogger.LogError("❌ Failed to convert manifest to CSV.");
                    throw new Exception("Failed to convert manifest to CSV");
                }
                
                
                var receiptXmlPath = Path.Combine(scaleInterfaceFolderOutput, $"{manifest.Company.Company}_Receipt-{manifest.GetManifestDateString()}.rcxml");
                if(ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptXmlPath))
                {
                    GlobalLogger.LogInfo($"Receipt XML generated successfully: {receiptXmlPath}");
                    GlobalLogger.LogInfo($"Scale rcxml file saved to scale interface output folder");
                }
                else
                {
                    GlobalLogger.LogError("❌ Failed to generate receipt XML.");
                    throw new Exception("Failed to generate receipt XML");;
                }
                
                var shipmentXmlPath = Path.Combine(scaleInterfaceFolderOutput, $"{manifest.Company.Company}Shipments-{manifest.GetManifestDateString()}.shxml");
                if(ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentXmlPath))
                {
                    GlobalLogger.LogInfo($"Shipment XML generated successfully: {shipmentXmlPath}");
                    GlobalLogger.LogInfo($"Scale shxml file saved to scale interface output folder");
                }
                else
                {
                    GlobalLogger.LogError("❌ Failed to generate shipment XML.");
                    throw new Exception("Failed to generate shipment XML");
                }
            }
            else if (fileInfo.Extension == ".csv" || fileInfo.Extension == ".xlsx")
            {
                manifest = await AzuraFreshCsv.ConvertToManifest(InputFilePath);
                
                if (manifest == null)
                {
                    GlobalLogger.LogError("❌ Failed to read manifest.");
                    throw new Exception("Failed to read manifest");
                }

                GlobalLogger.LogInfo($"Manifest read successfully");
                GlobalLogger.LogInfo($"   Manifest Date: {manifest.GetManifestDateString()}");
                GlobalLogger.LogInfo($"   Total Orders: {manifest.GetTotalOrders()}");
                GlobalLogger.LogInfo($"   Total Crates: {manifest.GetTotalCrates()}");
                
                var receiptXmlPath = Path.Combine(scaleInterfaceFolderOutput, $"{manifest.Company.Company}_Receipt-{manifest.GetManifestDateString()}.rcxml");
                if(ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptXmlPath))
                {
                    GlobalLogger.LogInfo($"Scale rcxml file saved to scale interface output folder");
                    
                }
                else
                {
                    GlobalLogger.LogError("❌ Failed to generate receipt XML.");
                    
                    throw new Exception("Failed to generate receipt XML");
                }
                
                var shipmentXmlPath = Path.Combine(scaleInterfaceFolderOutput, $"{manifest.Company.Company}_Shipments-{manifest.GetManifestDateString()}.shxml");
                if(ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentXmlPath))
                {
                    GlobalLogger.LogInfo($"Scale shxml file saved to scale interface output folder");
                    
                }
                else
                {
                    GlobalLogger.LogError("❌ Failed to generate shipment XML.");
                    
                    throw new Exception("Failed to generate shipment XML");
                }
            }
            
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            
            GlobalLogger.LogInfo($"Processing completed: {endTime:yyyy-MM-dd HH:mm:ss}");
            GlobalLogger.LogInfo($"Total processing time: {duration.TotalSeconds:F1} seconds");
            
            GlobalLogger.LogInfo($"Processing completed successfully!");

            

            await DisplayAlert("Success", "File processed successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred during processing: {ex.Message}", "OK");
            //Clean up output folder if processing failed
            if (Directory.Exists(_jobOutputFolder))
            {
                try
                {
                    Directory.Delete(_jobOutputFolder, true);
                    GlobalLogger.LogInfo($"Output folder {_jobOutputFolder} deleted due to processing failure.");
                    _jobOutputFolder = null;
                }
                catch (Exception cleanupEx)
                {
                    GlobalLogger.LogError($"Failed to clean up output folder: {cleanupEx.Message}");
                }
            }
            
        }
    }

    private async void OnOpenOutputClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(OutputFolderPath) || !Directory.Exists(OutputFolderPath))
            {
                GlobalLogger.LogError("Output folder does not exist.");
                await DisplayAlert("Error", "Output folder does not exist.", "OK");
                throw new DirectoryNotFoundException("Output folder does not exist.");
            }

            // Open folder in file explorer
            Process.Start("explorer.exe", _jobOutputFolder ?? OutputFolderPath);
        }
        catch (Exception ex)
        {
            try
            {
                // Alternative method using Process.Start for desktop platforms
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    GlobalLogger.LogError($"Failed to open output folder: {ex.Message}");
                    System.Diagnostics.Process.Start("explorer.exe", OutputFolderPath);
                }
                else
                {
                    GlobalLogger.LogError($"Failed to open output folder: {ex.Message}");
                    await DisplayAlert("Error", $"Cannot open folder: {ex.Message}", "OK");
                }
            }
            catch (Exception ex2)
            {
                GlobalLogger.LogError($"Failed to open output folder with fallback: {ex2.Message}");
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

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}