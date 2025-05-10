using Telepathic.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Telepathic.Data;

/// <summary>
/// Repository class for managing tasks in the database.
/// </summary>
public class TaskRepository
{
	private bool _hasBeenInitialized = false;
	private readonly ILogger _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="TaskRepository"/> class.
	/// </summary>
	/// <param name="logger">The logger instance.</param>
	public TaskRepository(ILogger<TaskRepository> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Initializes the database connection and creates the Task table if it does not exist.
	/// </summary>
	private async Task Init()
	{
		if (_hasBeenInitialized)
			return;

		await using var connection = new SqliteConnection(Constants.DatabasePath);
		await connection.OpenAsync();

		try
		{
			var createTableCmd = connection.CreateCommand();
			createTableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Task (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                IsCompleted INTEGER NOT NULL,
                ProjectID INTEGER NOT NULL,
                AssistType INTEGER NOT NULL DEFAULT 0,
                AssistData TEXT
            );";
			await createTableCmd.ExecuteNonQueryAsync();
			
			// Check if the AssistType column exists - for database upgrades
			var checkAssistTypeCmd = connection.CreateCommand();
			checkAssistTypeCmd.CommandText = "PRAGMA table_info(Task)";
			var hasAssistType = false;
			var hasAssistData = false;
			
			await using var reader = await checkAssistTypeCmd.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				var columnName = reader.GetString(1);
				if (columnName == "AssistType")
					hasAssistType = true;
				else if (columnName == "AssistData")
					hasAssistData = true;
			}
			
			// Add columns if they don't exist (for upgrade scenario)
			if (!hasAssistType)
			{
				var addAssistTypeCmd = connection.CreateCommand();
				addAssistTypeCmd.CommandText = "ALTER TABLE Task ADD COLUMN AssistType INTEGER NOT NULL DEFAULT 0";
				await addAssistTypeCmd.ExecuteNonQueryAsync();
				_logger.LogInformation("Added AssistType column to Task table");
			}
			
			if (!hasAssistData)
			{
				var addAssistDataCmd = connection.CreateCommand();
				addAssistDataCmd.CommandText = "ALTER TABLE Task ADD COLUMN AssistData TEXT";
				await addAssistDataCmd.ExecuteNonQueryAsync();
				_logger.LogInformation("Added AssistData column to Task table");
			}
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Error creating or upgrading Task table");
			throw;
		}

		_hasBeenInitialized = true;
	}

	/// <summary>
	/// Retrieves a list of all tasks from the database.
	/// </summary>
	/// <returns>A list of <see cref="ProjectTask"/> objects.</returns>
	public async Task<List<ProjectTask>> ListAsync()
	{
		await Init();
		await using var connection = new SqliteConnection(Constants.DatabasePath);
		await connection.OpenAsync();

		var selectCmd = connection.CreateCommand();
		selectCmd.CommandText = "SELECT * FROM Task";
		var tasks = new List<ProjectTask>();

		await using var reader = await selectCmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			var task = new ProjectTask
			{
				ID = reader.GetInt32(0),
				Title = reader.GetString(1),
				IsCompleted = reader.GetBoolean(2),
				ProjectID = reader.GetInt32(3)
			};
			
			// Handle AssistType and AssistData if columns exist
			try
			{
				task.AssistType = (AssistType)reader.GetInt32(4);
				task.AssistData = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty;
			}
			catch (IndexOutOfRangeException)
			{
				// Older database version without these columns
				task.AssistType = AssistType.None;
				task.AssistData = string.Empty;
			}
			
			tasks.Add(task);
		}

		return tasks;
	}

	/// <summary>
	/// Retrieves a list of tasks associated with a specific project.
	/// </summary>
	/// <param name="projectId">The ID of the project.</param>
	/// <returns>A list of <see cref="ProjectTask"/> objects.</returns>
	public async Task<List<ProjectTask>> ListAsync(int projectId)
	{
		await Init();
		await using var connection = new SqliteConnection(Constants.DatabasePath);
		await connection.OpenAsync();

		var selectCmd = connection.CreateCommand();
		selectCmd.CommandText = "SELECT * FROM Task WHERE ProjectID = @projectId";
		selectCmd.Parameters.AddWithValue("@projectId", projectId);
		var tasks = new List<ProjectTask>();

		await using var reader = await selectCmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			var task = new ProjectTask
			{
				ID = reader.GetInt32(0),
				Title = reader.GetString(1),
				IsCompleted = reader.GetBoolean(2),
				ProjectID = reader.GetInt32(3)
			};
			
			// Handle AssistType and AssistData if columns exist
			try
			{
				task.AssistType = (AssistType)reader.GetInt32(4);
				task.AssistData = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty;
			}
			catch (IndexOutOfRangeException)
			{
				// Older database version without these columns
				task.AssistType = AssistType.None;
				task.AssistData = string.Empty;
			}
			
			tasks.Add(task);
		}

		return tasks;
	}

	/// <summary>
	/// Retrieves a specific task by its ID.
	/// </summary>
	/// <param name="id">The ID of the task.</param>
	/// <returns>A <see cref="ProjectTask"/> object if found; otherwise, null.</returns>
	public async Task<ProjectTask?> GetAsync(int id)
	{
		await Init();
		await using var connection = new SqliteConnection(Constants.DatabasePath);
		await connection.OpenAsync();

		var selectCmd = connection.CreateCommand();
		selectCmd.CommandText = "SELECT * FROM Task WHERE ID = @id";
		selectCmd.Parameters.AddWithValue("@id", id);

		await using var reader = await selectCmd.ExecuteReaderAsync();
		if (await reader.ReadAsync())
		{
			var task = new ProjectTask
			{
				ID = reader.GetInt32(0),
				Title = reader.GetString(1),
				IsCompleted = reader.GetBoolean(2),
				ProjectID = reader.GetInt32(3)
			};
			
			// Handle AssistType and AssistData if columns exist
			try
			{
				task.AssistType = (AssistType)reader.GetInt32(4);
				task.AssistData = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty;
			}
			catch (IndexOutOfRangeException)
			{
				// Older database version without these columns
				task.AssistType = AssistType.None;
				task.AssistData = string.Empty;
			}
			
			return task;
		}

		return null;
	}

	/// <summary>
	/// Saves a task to the database. If the task ID is 0, a new task is created; otherwise, the existing task is updated.
	/// </summary>
	/// <param name="item">The task to save.</param>
	/// <returns>The ID of the saved task.</returns>
	public async Task<int> SaveItemAsync(ProjectTask item)
	{
		await Init();
		await using var connection = new SqliteConnection(Constants.DatabasePath);
		await connection.OpenAsync();

		var saveCmd = connection.CreateCommand();
		if (item.ID == 0)
		{
			saveCmd.CommandText = @"
            INSERT INTO Task (Title, IsCompleted, ProjectID, AssistType, AssistData) 
            VALUES (@title, @isCompleted, @projectId, @assistType, @assistData);
            SELECT last_insert_rowid();";
		}
		else
		{
			saveCmd.CommandText = @"
            UPDATE Task SET Title = @title, IsCompleted = @isCompleted, ProjectID = @projectId, 
            AssistType = @assistType, AssistData = @assistData WHERE ID = @id";
			saveCmd.Parameters.AddWithValue("@id", item.ID);
		}

		saveCmd.Parameters.AddWithValue("@title", item.Title);
		saveCmd.Parameters.AddWithValue("@isCompleted", item.IsCompleted);
		saveCmd.Parameters.AddWithValue("@projectId", item.ProjectID);
		saveCmd.Parameters.AddWithValue("@assistType", (int)item.AssistType);
		saveCmd.Parameters.AddWithValue("@assistData", item.AssistData ?? (object)DBNull.Value);

		var result = await saveCmd.ExecuteScalarAsync();
		if (item.ID == 0)
		{
			item.ID = Convert.ToInt32(result);
		}

		return item.ID;
	}

	/// <summary>
	/// Deletes a task from the database.
	/// </summary>
	/// <param name="item">The task to delete.</param>
	/// <returns>The number of rows affected.</returns>
	public async Task<int> DeleteItemAsync(ProjectTask item)
	{
		await Init();
		await using var connection = new SqliteConnection(Constants.DatabasePath);
		await connection.OpenAsync();

		var deleteCmd = connection.CreateCommand();
		deleteCmd.CommandText = "DELETE FROM Task WHERE ID = @id";
		deleteCmd.Parameters.AddWithValue("@id", item.ID);

		return await deleteCmd.ExecuteNonQueryAsync();
	}

	/// <summary>
	/// Drops the Task table from the database.
	/// </summary>
	public async Task DropTableAsync()
	{
		await Init();
		await using var connection = new SqliteConnection(Constants.DatabasePath);
		await connection.OpenAsync();

		var dropTableCmd = connection.CreateCommand();
		dropTableCmd.CommandText = "DROP TABLE IF EXISTS Task";
		await dropTableCmd.ExecuteNonQueryAsync();
		_hasBeenInitialized = false;
	}
}