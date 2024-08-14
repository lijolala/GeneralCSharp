using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        string connectionString = @"Server=(local)\SQL2008;initial catalog=tempdb;Integrated Security=True"; ;
        int numberOfParallelTasks = 10;

        Parallel.For(0, numberOfParallelTasks, i =>
        {
            ExecuteComplexQuery(connectionString);
        });
    }

    static void ExecuteComplexQuery(string connectionString)
    {
        // Example of a complex query that might exhaust SQL Server resources
        string complexQuery = @"
        WITH Numbers AS (
            SELECT 1 AS Number
            UNION ALL
            SELECT Number + 1
            FROM Numbers
            WHERE Number < 10000
        )
        SELECT A.Number, B.Number, C.Number, D.Number, E.Number, F.Number, G.Number, H.Number, I.Number, J.Number
        FROM Numbers A
        CROSS JOIN Numbers B
        CROSS JOIN Numbers C
        CROSS JOIN Numbers D
        CROSS JOIN Numbers E
        CROSS JOIN Numbers F
        CROSS JOIN Numbers G
        CROSS JOIN Numbers H
        CROSS JOIN Numbers I
        CROSS JOIN Numbers J
        OPTION (MAXRECURSION 0)";

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(complexQuery, connection))
                {
                    command.CommandTimeout = 0; // Disable timeout for the command
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Error: {ex.Message}");
            if (ex.Message.Contains("The query processor ran out of internal resources"))
            {
                Console.WriteLine("The expected error has been triggered.");
            }
        }
    }
}