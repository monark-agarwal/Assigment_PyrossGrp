using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System;
using System.Security.Principal;

public static class TransactionFunction
{
    [FunctionName("TransactionFunction")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Retrieve the transaction object properties
            int id = data?.Id;
            int amount = data?.Amount;
            string direction = data?.Direction;
            int account = data?.Account;

            // Connect to the SQL database
            string connectionString = "YOUR_SQL_CONNECTION_STRING";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Retrieve the account details from the Wallet table
                string query = $"SELECT * FROM Wallet WHERE AccountID={account}";
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int currentBalance = reader.GetInt32(1);

                            // Check if there are sufficient funds in the account
                            if (direction == "Debit" && currentBalance < amount)
                            {
                                return new BadRequestObjectResult("Insufficient funds.");
                            }
                            else
                            {
                                // Add the transaction details to the Transacting table
                                query = $"INSERT INTO Transacting (TransactionID, Amount, Direction, AccountID, Timestamp) VALUES ({id}, {amount}, '{direction}', {account}, GETUTCDATE())";
                                using (SqlCommand insertCommand = new SqlCommand(query, connection))
                                {
                                    await insertCommand.ExecuteNonQueryAsync();
                                }

                                // Update the account balance in the Wallet table
                                int newBalance = direction == "Debit" ? currentBalance - amount : currentBalance + amount;
                                query = $"UPDATE Wallet SET AccountBalance={newBalance} WHERE AccountID={account}";
                                using (SqlCommand updateCommand = new SqlCommand(query, connection))
                                {
                                    await updateCommand.ExecuteNonQueryAsync();
                                }

                                return new OkObjectResult("Transaction processed successfully.");
                            }
                        }
                    }
                    else
                    {
                        return new BadRequestObjectResult($"Account with ID {account} not found.");
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }

        return new BadRequestObjectResult($"Unable to process request!");
    }
}
