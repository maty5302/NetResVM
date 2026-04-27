using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace NetResVM.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("SuperSecretPass123!") 
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        await CreateDatabaseSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    private async Task CreateDatabaseSchemaAsync()
    {
        string scriptPath = Path.Combine(AppContext.BaseDirectory, "SQLCreateTablesBc.sql");

        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Kritická chyba: SQL skript nenalezen na cestě {scriptPath}. Zkontroluj .csproj!");

        string rawScript = await File.ReadAllTextAsync(scriptPath);

        // 1. Rozsekáme skript na menší dávky všude tam, kde je slovo "GO"
        string[] batches = Regex.Split(rawScript, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        // 2. Projdeme každou dávku a spustíme ji zvlášť
        foreach (string batch in batches)
        {
            string currentBatch = batch.Trim();
        
            if (string.IsNullOrWhiteSpace(currentBatch)) 
                continue;

            // 3. Odstraníme řádky začínající na "USE [NazevDatabaze]", 
            // protože my už jsme ke správné testovací databázi připojeni!
            currentBatch = Regex.Replace(currentBatch, @"^\s*USE\s+\[?[a-zA-Z0-9_]+\]?;?", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
            if (string.IsNullOrWhiteSpace(currentBatch)) 
                continue;

            // 4. Vykonáme očištěný SQL příkaz
            using var command = new SqlCommand(currentBatch, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}