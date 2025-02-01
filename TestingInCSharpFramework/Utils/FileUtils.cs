using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TestingInCSharpFramework.Config;
using TestingInCSharpFramework.DriverFactory;


namespace TestingInCSharpFramework.Utils;

public interface IFileUtils
{
    Task<bool> IsFileDownloadedAsync(string expectedFileName, string fileExtension);
    bool ValidateExcelHeaders(string expectedFileName, string fileExtension);
    bool ValidateTheExportedCompareExcelFile(string fileName, string firstCostBookName, string secondCostBookName);
    void DeleteFileIfExists(string expectedFileName, string fileExtension);
    bool AllCellsContain8100(string fileName);
    void WritingInExcelSheet(string newModel, string newCondition, string newQty,
                                           string newRuleSetId, string newCostCenterItemId, string newValue);
    string GetRelativeFilePath(string filename);
    bool ValidateColumnValues(string fileName, string headerName, string[] expectedValues);
}

public class FileUtils : IFileUtils
{
    private readonly IDriverFixture _driverFixture;
    private readonly TestSettings _testSettings;
    private readonly string _downloadDirectory;

    public FileUtils(IDriverFixture driverFixture, TestSettings testSettings)
    {
        _driverFixture = driverFixture;
        _testSettings = testSettings;
        _downloadDirectory = _driverFixture.DownloadDirectory;
    }

    /// <summary>
    /// Checks if a file with the expected name and extension has been downloaded
    /// and schedules its deletion upon application exit.
    /// </summary>
    /// <param name="expectedFileName">The expected name of the downloaded file (without extension).</param>
    /// <param name="fileExtension">The file extension (e.g., "pdf", "txt").</param>
    /// <returns>True if the file was downloaded successfully, False otherwise.</returns>
    public async Task<bool> IsFileDownloadedAsync(string expectedFileName, string fileExtension)
    {
        // Use the previously set download path.
        string downloadPath = _downloadDirectory;

        // Construct the full file name with extension.
        string fileName = $"{expectedFileName}.{fileExtension}";
        string filePath = Path.Combine(downloadPath, fileName);
        Console.WriteLine(filePath);

        int maxWaitTimeMilliseconds = 60 * 5000; // 5 minutes in milliseconds
        DateTime startTime = DateTime.Now;
        bool isDownloaded = false;

        while ((DateTime.Now - startTime).TotalMilliseconds < maxWaitTimeMilliseconds)
        {
            // Check if the file exists and is readable.
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                isDownloaded = true;
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        return isDownloaded;
    }


    public string GetRelativeFilePath(string filename)
    {
        string baseDir = _testSettings.IsGrid()
            ? "/app/BDDTestingSeaRates"
            : AppDomain.CurrentDomain.BaseDirectory;

        string relativePath = Path.Combine(baseDir, "resources", "testfiles");
        string fullPath = Path.Combine(relativePath, filename);

        return fullPath;
    }


    public bool ValidateExcelHeaders(string expectedFileName, string fileExtension)
    {
        // Define the expected headers that should be present in the Excel file.
        string[] expectedHeaders = {
        "Model Number", "Prefix", "Description", "Is Enabled", "Ignore Is Enabled", "Is Distributor Enabled",
        "Notes", "Time Stamp", "User ID", "W 1", "W 2", "W 3", "W 4", "W 5", "W 6", "W 7", "W 8", "W 9", "W 10",
        "Currency Type ID", "SOM _ ID", "Status", "Model Type", "Model Group", "Currency", "Standard Of Measure",
        "Created User", "Created Date", "A _ Name", "A _ Dimension", "A _ Product Line", "A _ Is Visible",
        "A _ Is Read Only", "A _ Sort Order", "A _ Tooltip", "A _ Equimnent Notes", "A _ Class", "A _ Display _ Label",
        "A _ Attribute _ Name", "A _ Material Group", "A _ Segment", "A _ MCA _ ID", "A _ Attribute Value", "A _ Maestro",
        "A _ Currency Type ID", "A _ SOM _ ID", "A _ Wbs Code", "A _ WBS _ L 2", "A _ WBS _ L 3", "AV _ Value",
        "AV _ Vcp Cost Type", "AV _ Cost Type", "AV _ Base Cost", "AV _ W 1", "AV _ W 2", "AV _ W 3", "AV _ W 4",
        "AV _ W 5", "AV _ W 6", "AV _ W 7", "AV _ W 8", "AV _ W 9", "AV _ W 10", "AV _ Is Default", "AV _ Maestro Key"
    };

        // Use the previously set download path.
        string downloadPath = _downloadDirectory;
        // Construct the full file name with extension.
        string fileName = $"{expectedFileName}.{fileExtension}";
        string filePath = Path.Combine(downloadPath, fileName);

        // Check if the file exists.
        if (!File.Exists(filePath))
        {
            return false;
        }

        // Open the Excel file using OpenXml SDK.
        using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
        {
            // Get the first worksheet.
            Worksheet worksheet = spreadsheetDocument.WorkbookPart!.WorksheetParts.First().Worksheet;

            // Get the SheetData element (this is the correct way)
            SheetData sheetData = worksheet.Elements<SheetData>().First();

            // Get the third row (remember rows are 1-based, so 3 is the third row)
            Row thirdRow = sheetData.Elements<Row>().ElementAtOrDefault(2)!; // ElementAtOrDefault handles cases where the row doesn't exist

            if (thirdRow != null)
            {
                // Iterate through the expected headers.
                foreach (string expectedHeader in expectedHeaders)
                {
                    // Check if the expected header exists in the first row.
                    bool headerFound = false;
                    foreach (Cell cell in thirdRow.Elements<Cell>())
                    {
                        // Get the cell value.
                        string cellValue = cell.InnerText;

                        // Compare the cell value with the expected header (case-insensitive).
                        if (string.Equals(cellValue, expectedHeader, StringComparison.OrdinalIgnoreCase))
                        {
                            headerFound = true;
                            break;
                        }
                    }

                    // If the header is not found, return false.
                    if (!headerFound)
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Handle the case where the third row is not found (e.g., log an error)
                Console.WriteLine("Third row not found in the spreadsheet.");
                return false;
            }
        }

        // All expected headers are found, return true.
        return true;
    }


    public bool ValidateTheExportedCompareExcelFile(string fileName, string firstCostBookName, string secondCostBookName)
    {
        // Locate the folder specified by the path
        DirectoryInfo folder = new DirectoryInfo(_downloadDirectory);

        // Filter files in the folder based on filename containing 'fileName' and ending with '.xlsx' (case-insensitive)
        FileInfo[] files = folder.GetFiles().Where(file => file.Name.StartsWith(fileName) && file.Extension.ToLower() == ".xlsx").ToArray();

        // Check if any files were found
        if (files == null || files.Length == 0)
        {
            Console.WriteLine("No files found in the directory.");
            return false;
        }

        // Loop through each file found in the folder
        foreach (FileInfo file in files)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                // Open the Excel workbook using the file stream
                XSSFWorkbook workbook = new XSSFWorkbook(fs);

                // Get the first sheet (assuming data is in the first sheet)
                ISheet sheet = workbook.GetSheetAt(0);

                // Validate cell values in the first row (assuming headers)
                IRow row = sheet.GetRow(0);
                ICell cellA1 = row.GetCell(0); // Get cell A1
                ICell cellB1 = row.GetCell(1); // Get cell B1
                if (cellA1 == null || cellB1 == null || // Check if cells exist
                    cellA1.StringCellValue != firstCostBookName || // Compare cell A1 value
                    cellB1.StringCellValue != secondCostBookName) // Compare cell B1 value
                {
                    workbook.Close();
                    return false; // Fail if any cell value doesn't match
                }

                // If all validations pass, close the workbook
                workbook.Close();
            }
        }

        // If all files pass validation, return true
        return true;
    }

    public void DeleteFileIfExists(string expectedFileName, string fileExtension)
    {
        // Download Folder Path
        string downloadsFolderPath = _downloadDirectory;
        FileInfo[] listOfFiles;

        // Get all the files in the downloaded folder
        DirectoryInfo downloadsFolder = new DirectoryInfo(downloadsFolderPath);
        listOfFiles = downloadsFolder.GetFiles();

        // Filter files based on name and extension
        FileInfo[] filteredFiles = listOfFiles.Where(file =>
        {
            string fileName = file.Name.ToLower();
            return fileName.Contains(expectedFileName.ToLower()) &&
                fileName.Contains(fileExtension.ToLower()) &&
                !fileName.Contains("download");
        }).ToArray();

        // Sort files by last modified time in ascending order
        Array.Sort(filteredFiles, (file1, file2) => file1.LastWriteTime.CompareTo(file2.LastWriteTime));

        // Delete the oldest file if found
        if (filteredFiles.Length > 0)
        {
            filteredFiles[0].Delete();
        }
    }

    public bool AllCellsContain8100(string fileName)
    {
        string downloadDirectory;

        // Check if the current OS is Windows
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            downloadDirectory = _downloadDirectory; // Use the path as is for Windows
        }
        else
        {
            // Assume Linux/Docker environment
            // Adjust the path for Linux/Docker, assuming _downloadDirectory is already set correctly
            downloadDirectory = Path.Combine("/app/BDDTestingSeaRates/resources/tmp", _downloadDirectory).Replace('\\', '/');
        }

        //if (!Directory.Exists(downloadDirectory))
        //{
        //    Console.WriteLine($"Directory does not exist: {downloadDirectory}");
        //    return false;
        //}

        DirectoryInfo folder = new DirectoryInfo(downloadDirectory);
        FileInfo[] files = folder.GetFiles().Where(file => file.Name.Contains(fileName) && file.Extension.ToLower() == ".xlsx").ToArray();

        if (files == null || files.Length == 0)
        {
            Console.WriteLine("No files found in the directory.");
            return false;
        }

        foreach (FileInfo file in files)
        {
            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(file.FullName, false))
            {
                Worksheet worksheet = spreadsheetDocument.WorkbookPart!.WorksheetParts.First().Worksheet;
                SheetData sheetData = worksheet.Elements<SheetData>().First();

                // Start from the 4th row (index 3)
                for (int row = 3; row < sheetData.Elements<Row>().Count(); row++)
                {
                    Row currentRow = sheetData.Elements<Row>().ElementAtOrDefault(row)!;
                    if (currentRow != null)
                    {
                        Cell? cell = currentRow.Elements<Cell>().FirstOrDefault(c => c.CellReference?.Value?.StartsWith("A") ?? false);
                        if (cell != null && cell.DataType! == CellValues.Number)
                        {
                            // Read the cell value
                            string cellValue = cell.InnerText;

                            // Check if the cell value is equal to "8100"
                            if (!cellValue.Contains("8100"))
                            {
                                return false; // Found a cell that doesn't contain 8100
                            }
                        }
                    }
                }
            }
        }

        return true; // All cells in column A starting from the 4th row contain 8100
    }


    public void WritingInExcelSheet(string newModel, string newCondition, string newQty,
                                           string newRuleSetId, string newCostCenterItemId, string newValue)
    {
        string downloadFolderPath = _downloadDirectory;

        // Get all files from download folder
        var files = Directory.GetFiles(downloadFolderPath, "Services*.xlsx");

        // Find the file starting with "Services.xlsx"
        var excelFile = files.FirstOrDefault();

        // Check if file found
        if (excelFile == null)
        {
            throw new FileNotFoundException("Excel file 'Services.xlsx' not found in download folder");
        }

        using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(excelFile, true))
        {
            WorkbookPart? workbookPart = spreadsheetDocument.WorkbookPart;
            WorksheetPart? worksheetPart = workbookPart!.WorksheetParts.First();
            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

            // Get or create the second row (index 1)
            Row? row = sheetData.Elements<Row>().FirstOrDefault(r => (r.RowIndex ?? 0) == 2);
            if (row == null)
            {
                row = new Row() { RowIndex = 2 };
                sheetData.Append(row);
            }

            // Helper method to set cell value
            void SetCellValue(uint columnIndex, string value)
            {
                Cell? cell = row.Elements<Cell>().FirstOrDefault(c =>
                    c.CellReference != null && GetColumnName(c.CellReference!) == GetColumnName(columnIndex));
                if (cell == null)
                {
                    cell = new Cell() { CellReference = GetColumnName(columnIndex) + "2" };
                    row.Append(cell);
                }
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(value);
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            // Set values for each column
            SetCellValue(2, newModel);
            SetCellValue(3, newCondition);
            SetCellValue(4, newQty);
            SetCellValue(5, newRuleSetId);
            SetCellValue(6, newCostCenterItemId);
            SetCellValue(7, newValue);

            // Save the changes
            worksheetPart.Worksheet.Save();
        }
    }

    private string GetColumnName(uint columnIndex)
    {
        int dividend = (int)columnIndex;
        string columnName = String.Empty;
        int modulo;

        while (dividend > 0)
        {
            modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private string GetColumnName(string cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
        {
            return string.Empty;
        }
        return new string(cellReference.TakeWhile(c => !char.IsDigit(c)).ToArray());
    }

    /// <summary>
    /// Validates that all values in a specific column match one of the expected values.
    /// The method looks for the header in the third row and validates all subsequent rows.
    /// </summary>
    /// <param name="fileName">The name of the Excel file to validate</param>
    /// <param name="headerName">The name of the column header to find</param>
    /// <param name="expectedValues">Array of valid values that the column cells can contain</param>
    /// <returns>True if all values in the column match one of the expected values, false otherwise</returns>
    public bool ValidateColumnValues(string fileName, string headerName, string[] expectedValues)
    {
        // Add .xlsx extension if not present
        string fullFileName = fileName.EndsWith(".xlsx") ? fileName : $"{fileName}.xlsx";
        string filePath = Path.Combine(_downloadDirectory, fullFileName);

        using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
        {
            WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart!;
            WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

            // Get the third row (index 2) as header row
            Row headerRow = sheetData.Elements<Row>().ElementAt(2);
            int columnIndex = -1;
            int cellIndex = 0;

            foreach (Cell cell in headerRow.Elements<Cell>())
            {
                if (cell.InnerText.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                {
                    columnIndex = cellIndex;
                    break;
                }
                cellIndex++;
            }

            if (columnIndex == -1)
                return false;

            // Check values in the found column, starting from fourth row
            foreach (Row row in sheetData.Elements<Row>().Skip(3))
            {
                Cell cell = row.Elements<Cell>().ElementAtOrDefault(columnIndex)!;
                if (cell != null)
                {
                    string cellValue = cell.InnerText;
                    if (!expectedValues.Contains(cellValue, StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}