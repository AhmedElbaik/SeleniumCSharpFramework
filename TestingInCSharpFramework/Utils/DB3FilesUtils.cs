using SQLitePCL;
using System.Data.SQLite;
using TestingInCSharpFramework.DriverFactory;

namespace TestingInCSharpFramework.Utils
{
    public interface IDB3FilesUtils
    {
        bool VerifyPriceListTableWbsCodeColumnAsync(string fileName, string columnName, string tableName);
        bool VerifyOnlyEnabledModelsArePresentInPriceListTable(string[] requiredModels, string[] excludedModels);
        bool VerifyInStdCostTableVCPCostTypeColumn();
        bool VerifyChangesPersistOnSellingRegionColumn(string fileName, string sellingRegionToCheck);
        bool VerifyChangesPersistOnAbbreviationColumn(string fileName, string abbreviationToCheck);
        bool VerifyAColumnPresenceInATableInADB3File(string columnName, string tableName, string fileName);
        bool ValidateColumnValues(string tableName, string columnName, string[] expectedValues, string fileName);
    }

    public class DB3FilesUtils : IDB3FilesUtils
    {
        private readonly IDriverFixture _driverFixture;
        private readonly string _downloadDirectory;

        public DB3FilesUtils(IDriverFixture driverFixture)
        {
            _driverFixture = driverFixture;
            _downloadDirectory = _driverFixture.DownloadDirectory;
        }

        public bool VerifyPriceListTableWbsCodeColumnAsync(string fileName, string columnName, string tableName)
        {
            var dbFilePath = Path.Combine(_downloadDirectory, fileName + ".db3");

            if (!File.Exists(dbFilePath))
            {
                throw new FileNotFoundException($"The file {fileName} does not exist in the download directory.");
            }

            SQLitePCL.Batteries.Init();
            sqlite3 connection;
            int result = raw.sqlite3_open(dbFilePath, out connection);

            if (result != raw.SQLITE_OK)
            {
                throw new Exception($"Could not open database file: {dbFilePath}");
            }

            try
            {
                var commandText = $"PRAGMA table_info({tableName});";
                sqlite3_stmt stmt;
                result = raw.sqlite3_prepare_v2(connection, commandText, out stmt);

                if (result != raw.SQLITE_OK)
                {
                    throw new Exception($"Could not prepare SQL statement: {commandText}");
                }

                try
                {
                    bool columnExists = false;
                    while (raw.sqlite3_step(stmt) == raw.SQLITE_ROW)
                    {
                        var currentColumnName = raw.sqlite3_column_text(stmt, 1).utf8_to_string();
                        if (StringComparer.OrdinalIgnoreCase.Equals(currentColumnName, columnName))
                        {
                            columnExists = true;
                            break;
                        }
                    }
                    return columnExists;
                }
                finally
                {
                    raw.sqlite3_finalize(stmt);
                }
            }
            finally
            {
                raw.sqlite3_close(connection);
            }
        }

        public bool VerifyOnlyEnabledModelsArePresentInPriceListTable(string[] requiredModels, string[] excludedModels)
        {
            // Find the newest file with "distributor" in the name
            var downloadsFolder = new DirectoryInfo(_downloadDirectory);
            var files = downloadsFolder.GetFiles()
                .Where(file => file.Name.Contains("distributor"))
                .OrderByDescending(file => file.LastWriteTime)
                .ToArray();

            if (files.Length == 0)
            {
                throw new FileNotFoundException("No files found with 'distributor' in the name.");
            }

            var filePath = files[0].FullName;

            // Connect to the database
            var connectionString = $"Data Source={filePath};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT Enabled, Model FROM PriceList", connection))
                using (var reader = command.ExecuteReader())
                {
                    var allEnabledOrNull = true;
                    var foundModels = new HashSet<string>();

                    while (reader.Read())
                    {
                        // Check the Enabled column
                        var enabledValue = reader["Enabled"];
                        if (enabledValue != DBNull.Value && enabledValue.ToString() != "True")
                        {
                            allEnabledOrNull = false;
                            break;
                        }

                        // Collect the Model values
                        var modelValue = reader["Model"].ToString();
                        if (modelValue != null)
                        {
                            foundModels.Add(modelValue);
                        }
                    }

                    if (!allEnabledOrNull)
                    {
                        return false;
                    }

                    // Check if all required models are present
                    foreach (var requiredModel in requiredModels)
                    {
                        if (!foundModels.Contains(requiredModel))
                        {
                            return false;
                        }
                    }

                    // Check if none of the excluded models are present
                    foreach (var excludedModel in excludedModels)
                    {
                        if (foundModels.Contains(excludedModel))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }


        public bool VerifyInStdCostTableVCPCostTypeColumn()
        {
            SQLiteConnection? connection = null;
            SQLiteCommand? command = null;
            SQLiteDataReader? reader = null;

            try
            {
                // Find the newest file with "Test" in the name
                var downloadsFolder = new DirectoryInfo(_downloadDirectory);
                var files = downloadsFolder.GetFiles().Where(file => file.Name.Contains("Test")).ToArray();
                if (files.Length == 0)
                {
                    throw new FileNotFoundException("No files found with 'Test' in the name.");
                }
                var filePath = files[0].FullName;

                // Establish the connection
                var connectionString = $"Data Source={filePath};Version=3;";
                connection = new SQLiteConnection(connectionString);
                connection.Open();

                // Check if the StdCost table contains a column named VcpCostType
                var commandText = "PRAGMA table_info(StdCost)";
                command = new SQLiteCommand(commandText, connection);
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var columnName = reader.GetString(1);
                    if (string.Equals(columnName, "VcpCostType", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine($"SQLite error: {e.Message}");
            }
            catch (IOException e)
            {
                Console.WriteLine($"IO error: {e.Message}");
            }
            finally
            {
                reader?.Close();
                command?.Dispose();
                connection?.Close();
            }

            return false;
        }

        public bool VerifyChangesPersistOnSellingRegionColumn(string fileName, string sellingRegionToCheck)
        {
            string connectionString = null!;

            try
            {
                // Get all files from the download folder
                string[] files = Directory.GetFiles(_downloadDirectory);
                if (files.Length == 0)
                {
                    // Handle case where folder is empty
                    return false;
                }

                // Find the file containing given name in the name
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).Contains(fileName) && Path.GetExtension(file) == ".db3")
                    {
                        connectionString = $"Data Source={file};Version=3;";
                        break;
                    }
                }

                // Check if a connection string was created
                if (string.IsNullOrEmpty(connectionString))
                {
                    // No file containing "AE.db3" found
                    return false;
                }

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(connection))
                    {
                        command.CommandText = "SELECT * FROM RegionalSectors WHERE SellingRegion = @SellingRegion";
                        command.Parameters.AddWithValue("@SellingRegion", sellingRegionToCheck);

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            // Verify the existence of the specific value in the specific column
                            return reader.HasRows;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }

        public bool VerifyChangesPersistOnAbbreviationColumn(string fileName, string abbreviationToCheck)
        {
            string connectionString = null!;

            try
            {
                // Get all files from the download folder
                string[] files = Directory.GetFiles(_downloadDirectory);
                if (files.Length == 0)
                {
                    // Handle case where folder is empty
                    return false;
                }

                // Find the file containing given name in the name
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).Contains(fileName) && Path.GetExtension(file) == ".db3")
                    {
                        connectionString = $"Data Source={file};Version=3;";
                        break;
                    }
                }

                // Check if a connection string was created
                if (string.IsNullOrEmpty(connectionString))
                {
                    // No file containing "AE.db3" found
                    return false;
                }

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(connection))
                    {
                        command.CommandText = "SELECT * FROM RegionalSectors WHERE Abbreviation = @Abbreviation";
                        command.Parameters.AddWithValue("@Abbreviation", abbreviationToCheck);

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            // Verify the existence of the specific value in the specific column
                            return reader.HasRows;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }

        public bool VerifyAColumnPresenceInATableInADB3File(string columnName, string tableName, string fileName)
        {
            string connectionString = null!;

            try
            {
                // Get all files from the download folder
                string[] files = Directory.GetFiles(_downloadDirectory);
                if (files.Length == 0)
                {
                    // Handle case where folder is empty
                    return false;
                }

                // Find the file containing given name in the name
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).Contains(fileName) && Path.GetExtension(file) == ".db3")
                    {
                        connectionString = $"Data Source={file};Version=3;";
                        break;
                    }
                }

                // Check if a connection string was created
                if (string.IsNullOrEmpty(connectionString))
                {
                    // No file containing "AE.db3" found
                    return false;
                }

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand($"PRAGMA table_info({tableName})", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader.GetString(1);
                            if (name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Validates that all values in a specific column of a DB3 table match one of the expected values.
        /// </summary>
        /// <param name="tableName">The name of the table to check</param>
        /// <param name="columnName">The name of the column to validate</param>
        /// <param name="expectedValues">Array of valid values that the column can contain</param>
        /// <param name="fileName">The name of the DB3 file (without extension)</param>
        /// <returns>True if all values in the column match one of the expected values, false otherwise</returns>
        public bool ValidateColumnValues(string tableName, string columnName, string[] expectedValues, string fileName)
        {
            string connectionString = null!;
            try
            {
                // Get all files from the download folder
                string[] files = Directory.GetFiles(_downloadDirectory);
                if (files.Length == 0)
                {
                    return false;
                }

                // Find the file containing given name
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).Contains(fileName) && Path.GetExtension(file) == ".db3")
                    {
                        connectionString = $"Data Source={file};Version=3;";
                        break;
                    }
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    return false;
                }

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(connection))
                    {
                        command.CommandText = $"SELECT {columnName} FROM {tableName}";
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var value = reader[columnName].ToString();
                                if (!expectedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}