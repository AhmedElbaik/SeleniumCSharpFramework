using System.Data.SqlClient;
using TestingInCSharpFramework.Config;

namespace TestingInCSharpFramework.Utils;

public interface ISqlDBUtils
{
    bool CheckSendToQmaInCtsEdr(string costBookName);
}

public class SqlDBUtils : ISqlDBUtils
{
    private readonly TestSettings _testSettings;

    public SqlDBUtils(TestSettings testSettings)
    {
        _testSettings = testSettings;
    }

    public bool CheckSendToQmaInCtsEdr(string costBookName)
    {
        try
        {
            Thread.Sleep(5000); // Simulate delay
        }
        catch (ThreadInterruptedException e)
        {
            throw new InvalidOperationException("Thread was interrupted", e);
        }

        // Connection string
        string connectionString = _testSettings.CtsEdr!;

        // SQL query to check
        string query = @"SELECT P.Name, PM.ModelNumber, A.Name AS [Attribute Name], M.WbsCode 
                              FROM Pricebook.Pricebook P 
                              JOIN Pricebook.PricebookModel PM ON PM.PricebookID = P.PricebookID 
                              JOIN Pricebook.ModelProductLineAttribute M ON M.PricebookModelID = PM.PricebookModelID 
                              JOIN Catalog.Attribute A ON A.AttributeID = M.AttributeID 
                              WHERE P.Name = @CostBookName";

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CostBookName", costBookName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Check if the query returns any rows
                        return reader.HasRows;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error executing query: {e.Message}");
            return false;
        }
    }
}
