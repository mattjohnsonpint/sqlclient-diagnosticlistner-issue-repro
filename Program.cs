using System.Diagnostics;
using Microsoft.Data.SqlClient;
using SqlClientBugDemo;

// Subscribe for diagnostic messages
DiagnosticListener.AllListeners.Subscribe(new MyDiagnosticSubscriber());

using var connection = new SqlConnection(@"Server=(localdb)\TestDB;Integrated Security=true;");
connection.Open();

try
{
    CreateDatabaseSchema(connection);

    // Insert the first row.  This will always pass, so it shouldn't emit any error.
    InsertRow(connection, outputInsertedId: true);
    
    // Insert another row with the same value, which will error due to the unique index.
    // It emits a WriteCommandError diagnostic message, but only because we didn't use an OUTPUT clause.
    InsertRow(connection, outputInsertedId: false);
    
    // Insert another row with the same value, which will error due to the unique index.
    // This time, use an OUTPUT clause in the query to get the inserted ID.
    // It *should* emit a WriteCommandError diagnostic message - but it doesn't.
    InsertRow(connection, outputInsertedId: true);
}
finally
{
    connection.Close();
}

static void CreateDatabaseSchema(SqlConnection connection)
{
    const string sql = @"
DROP TABLE IF EXISTS [TestEntities];
CREATE TABLE [TestEntities] (
    [Id] int NOT NULL IDENTITY,
    [Property] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_TestEntities] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [IX_TestEntities_Property] ON [TestEntities] ([Property]);
";
    using var command = new SqlCommand(sql, connection);
    command.ExecuteNonQuery();
}

static void InsertRow(SqlConnection connection, bool outputInsertedId)
{
    var sql = outputInsertedId
        ? "INSERT INTO [TestEntities] ([Property]) OUTPUT INSERTED.[Id] VALUES ('TestValue')"
        : "INSERT INTO [TestEntities] ([Property]) VALUES ('TestValue')";
    
    Console.WriteLine("Inserting row...");
    
    try
    {
        using var command = new SqlCommand(sql, connection);

        // ExecuteReader doesn't emit the WriteCommandError diagnostic message when an OUTPUT clause is present.
        // Entity Framework Core uses this approach (which is why this is a concern at all).
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var id = reader.GetInt32(0);
            Console.WriteLine($"Row inserted with ID = {id}.");
        }
        else
        {
            Console.WriteLine("Row inserted.");
        }

        // ExecuteScalar *DOES* emit the WriteCommandError diagnostic message, in all cases.
        // var id = command.ExecuteScalar();
        // if (id != null)
        // {
        //     Console.WriteLine($"Row inserted with ID = {id}.");
        // }
    }
    catch (SqlException)
    {
        // For this demo, we want to see the diagnostic log message, not the exception.
    }
}
