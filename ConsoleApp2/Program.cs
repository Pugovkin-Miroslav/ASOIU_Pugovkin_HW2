using System;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Пути к файлам
string dbPath = "tasks.db";
string projectsCsv = Path.Combine(AppContext.BaseDirectory, "projects.csv");
string tasksCsv = Path.Combine(AppContext.BaseDirectory, "tasks.csv");
//Console.WriteLine($"[DEBUG] Базовая директория: {AppContext.BaseDirectory}");
//Console.WriteLine($"[DEBUG] projects.csv существует: {File.Exists(projectsCsv)}");
//Console.WriteLine($"[DEBUG] tasks.csv существует: {File.Exists(tasksCsv)}");

// Создаём менеджер БД и инициализируем данные
var db = new DatabaseManager(dbPath);
db.InitializeDatabase(projectsCsv, tasksCsv);
Console.WriteLine();

// Главный цикл меню
string choice;
do
{
    Console.WriteLine("\n=== Управление данными ===");
    Console.WriteLine("1 — Показать все проекты");
    Console.WriteLine("2 — Показать все задачи");
    Console.WriteLine("3 — Добавить задачу");
    Console.WriteLine("4 — Редактировать задачу");
    Console.WriteLine("5 — Удалить задачу");
    Console.WriteLine("6 — Отчёты");
    Console.WriteLine("7 — Фильтр по проекту");
    Console.WriteLine("8 — Экспорт в CSV");
    Console.WriteLine("0 — Выход");
    Console.Write("Ваш выбор: ");

    choice = Console.ReadLine()?.Trim() ?? "";
    Console.WriteLine();

    switch (choice)
    {
        case "1": ShowProjects(db); break;
        case "2": ShowTasks(db); break;
        case "3": AddTask(db); break;
        case "4": EditTask(db); break;
        case "5": DeleteTask(db); break;
        case "6": ReportsMenu(db); break;
        case "7": FilterByProject(db); break;      // [ГРУППА Г]
        case "8": ExportCsv(db); break;            // [ГРУППА Б]
        case "0": break;
        default: Console.WriteLine("Неверный пункт меню."); break;
    }
} while (choice != "0");


// ============================================================================
// ФУНКЦИИ ПУНКТОВ МЕНЮ
// ============================================================================

/// <summary>
/// Показать все проекты
/// </summary>
static void ShowProjects(DatabaseManager db)
{
    Console.WriteLine("--- Все проекты ---");
    var projects = db.GetAllProjects();
    foreach (var proj in projects)
        Console.WriteLine("  " + proj);
    Console.WriteLine($"\nИтого: {projects.Count}");
}

/// <summary>
/// Показать все задачи
/// </summary>
static void ShowTasks(DatabaseManager db)
{
    Console.WriteLine("--- Все задачи ---");
    var tasks = db.GetAllTasks();
    foreach (var task in tasks)
        Console.WriteLine("  " + task);
    Console.WriteLine($"\nИтого: {tasks.Count}");
}

/// <summary>
/// Добавить новую задачу
/// </summary>
static void AddTask(DatabaseManager db)
{
    Console.WriteLine("--- Добавление задачи ---");

    // Показываем проекты для выбора
    Console.WriteLine("Доступные проекты:");
    var projects = db.GetAllProjects();
    foreach (var proj in projects)
        Console.WriteLine("  " + proj);

    // Ввод ProjectId
    Console.Write("\nID проекта: ");
    if (!int.TryParse(Console.ReadLine(), out int projectId))
    {
        Console.WriteLine("Ошибка: введите целое число.");
        return;
    }

    // Ввод названия задачи
    Console.Write("Название задачи: ");
    string name = Console.ReadLine()?.Trim() ?? "";
    if (name.Length == 0)
    {
        Console.WriteLine("Ошибка: название не может быть пустым.");
        return;
    }

    // Ввод часов
    Console.Write("Трудоёмкость (часы): ");
    if (!int.TryParse(Console.ReadLine(), out int hours))
    {
        Console.WriteLine("Ошибка: введите целое число.");
        return;
    }

    try
    {
        var task = new Task(0, projectId, name, hours);
        db.AddTask(task);
        Console.WriteLine("✓ Задача добавлена.");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

/// <summary>
/// Редактировать существующую задачу
/// </summary>
static void EditTask(DatabaseManager db)
{
    Console.WriteLine("--- Редактирование задачи ---");

    Console.Write("Введите ID задачи: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("Ошибка: введите целое число.");
        return;
    }

    var task = db.GetTaskById(id);
    if (task == null)
    {
        Console.WriteLine($"Задача с ID={id} не найдена.");
        return;
    }

    Console.WriteLine($"\nТекущие данные: {task}");
    Console.WriteLine("(нажмите Enter, чтобы оставить значение без изменений)\n");

    // Название задачи
    Console.Write($"Название [{task.Name}]: ");
    string input = Console.ReadLine()?.Trim() ?? "";
    if (input.Length > 0) task.Name = input;

    // Проект
    Console.Write($"ID проекта [{task.ProjectId}]: ");
    input = Console.ReadLine()?.Trim() ?? "";
    if (input.Length > 0 && int.TryParse(input, out int newProjId))
        task.ProjectId = newProjId;

    // Часы
    Console.Write($"Часы [{task.Hours}]: ");
    input = Console.ReadLine()?.Trim() ?? "";
    if (input.Length > 0 && int.TryParse(input, out int newHours))
    {
        try
        {
            task.Hours = newHours; // валидация в set-аксессоре
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return;
        }
    }

    db.UpdateTask(task);
    Console.WriteLine("✓ Данные обновлены.");
}

/// <summary>
/// Удалить задачу с подтверждением
/// </summary>
static void DeleteTask(DatabaseManager db)
{
    Console.WriteLine("--- Удаление задачи ---");

    Console.Write("Введите ID задачи: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("Ошибка: введите целое число.");
        return;
    }

    var task = db.GetTaskById(id);
    if (task == null)
    {
        Console.WriteLine($"Задача с ID={id} не найдена.");
        return;
    }

    Console.Write($"Удалить «{task.Name}»? (да/нет): ");
    string confirm = Console.ReadLine()?.Trim().ToLower() ?? "";
    if (confirm == "да")
    {
        db.DeleteTask(id);
        Console.WriteLine("✓ Задача удалена.");
    }
    else
    {
        Console.WriteLine("Удаление отменено.");
    }
}


// ============================================================================
// ПОДМЕНЮ ОТЧЁТОВ
// ============================================================================

/// <summary>
/// Подменю для выбора отчётов
/// </summary>
static void ReportsMenu(DatabaseManager db)
{
    string choice;
    do
    {
        Console.WriteLine("\n--- Отчёты ---");
        Console.WriteLine("1 — Задачи с названиями проектов (полный список)");
        Console.WriteLine("2 — Количество задач по проектам");
        Console.WriteLine("3 — Среднее значение часов по проектам");
        Console.WriteLine("0 — Назад");
        Console.Write("Ваш выбор: ");

        choice = Console.ReadLine()?.Trim() ?? "";

        switch (choice)
        {
            case "1": Report1_TasksWithProjects(db); break;
            case "2": Report2_CountByProject(db); break;
            case "3": Report3_AvgHoursByProject(db); break;
            case "0": break;
            default: Console.WriteLine("Неверный пункт."); break;
        }
        Console.WriteLine();
    } while (choice != "0");
}

/// <summary>
/// Отчёт 1: Задачи с названиями проектов (JOIN, сортировка по названию задачи)
/// </summary>
static void Report1_TasksWithProjects(DatabaseManager db)
{
    new ReportBuilder(db)
        .Query(@"
            SELECT t.task_name, p.project_name, t.hours 
            FROM task t 
            JOIN project p ON t.project_id = p.project_id 
            ORDER BY t.task_name")
        .Title("Задачи по проектам")
        .Header("Задача", "Проект", "Часы")
        .ColumnWidths(25, 20, 8)
        .Numbered()           // [ГРУППА А]
        .Footer("Всего записей") // [ГРУППА В]
        .Print();
}

/// <summary>
/// Отчёт 2: Количество задач по проектам (GROUP BY + COUNT)
/// </summary>
static void Report2_CountByProject(DatabaseManager db)
{
    new ReportBuilder(db)
        .Query(@"
            SELECT p.project_name, COUNT(*) AS cnt 
            FROM task t 
            JOIN project p ON t.project_id = p.project_id 
            GROUP BY p.project_name 
            ORDER BY p.project_name")
        .Title("Количество задач по проектам")
        .Header("Проект", "Кол-во")
        .ColumnWidths(30, 10)
        .Numbered()
        .Footer("Всего проектов")
        .Print();
}

/// <summary>
/// Отчёт 3: Среднее значение часов по проектам (GROUP BY + AVG)
/// </summary>
static void Report3_AvgHoursByProject(DatabaseManager db)
{
    // [ГРУППА Б] Пример сохранения отчёта в файл:
    // Замените .Print() на .SaveToFile("report3_avg.txt")

    new ReportBuilder(db)
        .Query(@"
            SELECT p.project_name, ROUND(AVG(t.hours), 1) AS avg_hours 
            FROM task t 
            JOIN project p ON t.project_id = p.project_id 
            GROUP BY p.project_name 
            ORDER BY avg_hours DESC")
        .Title("Средняя трудоёмкость по проектам")
        .Header("Проект", "Среднее часов")
        .ColumnWidths(30, 15)
        .Numbered()
        .Footer("Всего проектов")
        .Print();
}


// ============================================================================
// ФИЛЬТР ПО ПРОЕКТУ
// ============================================================================

/// <summary>
/// Фильтр задач по выбранному проекту
/// </summary>
static void FilterByProject(DatabaseManager db)
{
    Console.WriteLine("--- Фильтр по проекту ---");

    Console.WriteLine("Доступные проекты:");
    var projects = db.GetAllProjects();
    foreach (var proj in projects)
        Console.WriteLine("  " + proj);

    Console.Write("\nВведите ID проекта: ");
    if (!int.TryParse(Console.ReadLine(), out int projectId))
    {
        Console.WriteLine("Ошибка: введите целое число.");
        return;
    }

    var tasks = db.GetTasksByProject(projectId);
    if (tasks.Count == 0)
    {
        Console.WriteLine("В этом проекте нет задач.");
        return;
    }

    Console.WriteLine($"\nЗадачи проекта #{projectId}:");
    foreach (var task in tasks)
        Console.WriteLine("  " + task);
    Console.WriteLine($"\nИтого: {tasks.Count}");
}


// ============================================================================
// ЭКСПОРТ В CSV
// ============================================================================

/// <summary>
/// Экспорт данных в CSV-файлы
/// </summary>
static void ExportCsv(DatabaseManager db)
{
    string projPath = Path.Combine(AppContext.BaseDirectory, "projects_export.csv");
    string taskPath = Path.Combine(AppContext.BaseDirectory, "tasks_export.csv");

    db.ExportToCsv(projPath, taskPath);

    Console.WriteLine($"✓ Проекты экспортированы в: {projPath}");
    Console.WriteLine($"✓ Задачи экспортированы в: {taskPath}");
}