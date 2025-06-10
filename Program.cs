using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddControllers();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapControllers();

/*
app.MapGet("/ftgManifestUpload", () =>
{
    string basePath =  "C:\\dev\\test";
    string inputFile = "input\\ftgManifest.pdf";
    string outputFile = "output\\ftgManifest01.pdf";
    string outputExcelFile = "output\\ftgManifest01.xlsx";
    
    PdfProcessor.SimplifyPdf(Path.Join(basePath, inputFile), Path.Join(basePath, outputFile));

    var manifest = PdfProcessor.ConvertPdfToExcel(Path.Join(basePath, outputFile), Path.Join(basePath, outputExcelFile));

    if (manifest != null)
    {
        ManifestToScale.ConvertManifestToCsv(manifest, Path.Join(basePath, Path.ChangeExtension(outputFile, ".csv")));
        
        ManifestToScale.GenerateReceiptFromTemplate(manifest, Path.Join(basePath, Path.ChangeExtension(outputFile, ".rcxml")));
        
        ManifestToScale.GenerateShipmentFromTemplate(manifest, Path.Join(basePath, Path.ChangeExtension(outputFile, ".shxml")));
    }
    
    return Results.Ok("FTG Manifest Upload Endpoint");
}).WithName("FTGManifestUpload");

app.MapPost("/ftgManifestUpload", (IFormFile file) =>
{
    string basePath = "C:\\dev\\test";
    Guid guid = Guid.NewGuid();
    string fileName = guid + "_ftgManifest";
    string inputFolder = "upload";
    string outputFile = "output";

    if (file.Length > 0)
    {
        using (var stream = new FileStream(Path.Join(basePath, inputFolder, fileName + ".pdf"), FileMode.Create))
        {
            file.CopyTo(stream);
        }
        
        PdfProcessor.SimplifyPdf(Path.Join(basePath, inputFolder,fileName + ".pdf"), Path.Join(basePath, outputFile, fileName + "_simplified.pdf"));

        var manifest = PdfProcessor.ConvertPdfToExcel(Path.Join(basePath, outputFile, fileName + "_simplified.pdf"), Path.Join(basePath, outputFile, fileName + ".xlsx"));

        if (manifest == null) return Results.Ok("FTG Manifest Uploaded Successfully");
        
        ManifestToScale.ConvertManifestToCsv(manifest, Path.Join(basePath, outputFile, fileName + ".csv"));
            
        ManifestToScale.GenerateReceiptFromTemplate(manifest, Path.Join(basePath, outputFile, fileName + ".rcxml"));
            
        ManifestToScale.GenerateShipmentFromTemplate(manifest, Path.Join(basePath, outputFile, fileName + ".shxml"));

        return Results.Ok("FTG Manifest Uploaded Successfully");
    }

    return Results.BadRequest("No file uploaded");
}).WithName("FTGManifestUploadPost").DisableAntiforgery();
*/

app.Run();
