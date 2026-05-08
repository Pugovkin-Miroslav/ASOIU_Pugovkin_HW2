using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;


class DatabaseManager
{
    private string _connectionString;

    /// <summary>
    /// Конструктор. Принимает путь к файлу БД.
    /// </summary>
    /// <param name="dbPath">Путь к файлу SQLite</param>
    public DatabaseManager(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>
    /// Создаёт таблицы (если не существуют) и загружает CSV при первом запуске
    /// </summary>
    /// <param name="projectsCsvPath">Путь к CSV с проектами</param>
    /// <param name="tasksCsvPath">Путь к CSV с задачами</param>
    public void InitializeDatabase(string projectsCsvPath, string tasksCsvPath)
    {
        CreateTables();

        // Импорт только если таблицы пусты
        if (GetAllProjects().Count == 0 && File.Exists(projectsCsvPath))
        {
            ImportProjectsFromCsv(projectsCsvPath);
            Console.WriteLine($"[OK] Загружены проекты из {projectsCsvPath}");
        }
        if (GetAllTasks().Count == 0 && File.Exists(tasksCsvPath))
        {
            ImportTasksFromCsv(tasksCsvPath);
            Console.WriteLine($"[OK] Загружены задачи из {tasksCsvPath}");
        }
    }

    /// <summary>
    /// Создание таблиц в БД
    /// </summary>
    private void CreateTables()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS project(
                project_id INTEGER PRIMARY KEY AUTOINCREMENT,
                project_name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS task(
                task_id INTEGER PRIMARY KEY AUTOINCREMENT,
                project_id INTEGER NOT NULL,
                task_name TEXT NOT NULL,
                hours INTEGER NOT NULL CHECK(hours >= 0),
                FOREIGN KEY(project_id) REFERENCES project(project_id)
            );";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Импорт проектов из CSV-файла
    /// </summary>
    /// <param name="path">Путь к CSV-файлу</param>
    private void ImportProjectsFromCsv(string path)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string[] lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++) // пропуск заголовка
        {
            string[] parts = lines[i].Split(';');
            if (parts.Length < 2) continue;
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO project(project_id, project_name) VALUES(@id, @name)";
            cmd.Parameters.AddWithValue("@id", int.Parse(parts[0]));
            cmd.Parameters.AddWithValue("@name", parts[1]);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Импорт задач из CSV-файла
    /// </summary>
    /// <param name="path">Путь к CSV-файлу</param>
    private void ImportTasksFromCsv(string path)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string[] lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++) // пропуск заголовка
        {
            string[] parts = lines[i].Split(';');
            if (parts.Length < 4) continue;
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO task(task_id, project_id, task_name, hours) 
                VALUES(@id, @projectId, @name, @hours)";
            cmd.Parameters.AddWithValue("@id", int.Parse(parts[0]));
            cmd.Parameters.AddWithValue("@projectId", int.Parse(parts[1]));
            cmd.Parameters.AddWithValue("@name", parts[2]);
            cmd.Parameters.AddWithValue("@hours", int.Parse(parts[3]));
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Получить все проекты
    /// </summary>
    /// <returns>Список проектов</returns>
    public List<Project> GetAllProjects()
    {
        var result = new List<Project>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT project_id, project_name FROM project ORDER BY project_id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Project(
                reader.GetInt32(0),
                reader.GetString(1)));
        }
        return result;
    }

    /// <summary>
    /// Получить все задачи
    /// </summary>
    /// <returns>Список задач</returns>
    public List<Task> GetAllTasks()
    {
        var result = new List<Task>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT task_id, project_id, task_name, hours FROM task ORDER BY task_id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Task(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }
        return result;
    }

    /// <summary>
    /// Получить задачу по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор задачи</param>
    /// <returns>Задача или null, если не найдена</returns>
    public Task? GetTaskById(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT task_id, project_id, task_name, hours FROM task WHERE task_id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Task(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetInt32(3));
        }
        return null;
    }

    /// <summary>
    /// Добавить новую задачу (ID генерируется автоматически)
    /// </summary>
    /// <param name="task">Задача для добавления</param>
    public void AddTask(Task task)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO task(project_id, task_name, hours) 
            VALUES(@projectId, @name, @hours)";
        cmd.Parameters.AddWithValue("@projectId", task.ProjectId);
        cmd.Parameters.AddWithValue("@name", task.Name);
        cmd.Parameters.AddWithValue("@hours", task.Hours);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Обновить данные существующей задачи
    /// </summary>
    /// <param name="task">Задача с обновлёнными данными</param>
    public void UpdateTask(Task task)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE task 
            SET project_id=@projectId, task_name=@name, hours=@hours 
            WHERE task_id=@id";
        cmd.Parameters.AddWithValue("@id", task.Id);
        cmd.Parameters.AddWithValue("@projectId", task.ProjectId);
        cmd.Parameters.AddWithValue("@name", task.Name);
        cmd.Parameters.AddWithValue("@hours", task.Hours);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Удалить задачу по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор задачи</param>
    public void DeleteTask(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM task WHERE task_id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Выполняет произвольный SQL-запрос и возвращает результат.
    /// Используется классом ReportBuilder для формирования отчётов.
    /// </summary>
    /// <param name="sql">SQL-запрос</param>
    /// <returns>Кортеж: имена столбцов и список строк результата</returns>
    public (string[] columns, List<string[]> rows) ExecuteQuery(string sql)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();

        // Имена столбцов
        string[] columns = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            columns[i] = reader.GetName(i);

        // Строки данных
        var rows = new List<string[]>();
        while (reader.Read())
        {
            string[] row = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                row[i] = reader.GetValue(i)?.ToString() ?? "";
            rows.Add(row);
        }

        return (columns, rows);
    }

    /// <summary>
    /// [ГРУППА Б] Экспорт обеих таблиц в CSV-файлы
    /// </summary>
    /// <param name="projectsPath">Путь для экспорта проектов</param>
    /// <param name="tasksPath">Путь для экспорта задач</param>
    public void ExportToCsv(string projectsPath, string tasksPath)
    {
        // Экспорт проектов
        var projectLines = new List<string>();
        projectLines.Add("project_id;project_name");
        foreach (var proj in GetAllProjects())
            projectLines.Add($"{proj.Id};{proj.Name}");
        File.WriteAllLines(projectsPath, projectLines.ToArray());

        // Экспорт задач
        var taskLines = new List<string>();
        taskLines.Add("task_id;project_id;task_name;hours");
        foreach (var task in GetAllTasks())
            taskLines.Add($"{task.Id};{task.ProjectId};{task.Name};{task.Hours}");
        File.WriteAllLines(tasksPath, taskLines.ToArray());
    }

    /// <summary>
    /// [ГРУППА Г] Получить задачи конкретного проекта
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <returns>Список задач проекта</returns>
    public List<Task> GetTasksByProject(int projectId)
    {
        var result = new List<Task>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT task_id, project_id, task_name, hours 
            FROM task 
            WHERE project_id=@projectId 
            ORDER BY task_name";
        cmd.Parameters.AddWithValue("@projectId", projectId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Task(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }
        return result;
    }
}