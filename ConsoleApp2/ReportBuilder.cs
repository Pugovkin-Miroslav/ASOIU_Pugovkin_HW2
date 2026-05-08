using System;
using System.Text;
using System.IO;

/// <summary>
/// Построитель отчётов с использованием паттерна Fluent Interface.
/// Промежуточные методы возвращают this (цепочка вызовов).
/// Терминальные методы выполняют запрос и форматируют результат.
/// 
/// Зависит от DatabaseManager (жёсткая связь через конструктор).
/// </summary>
class ReportBuilder
{
    // Жёсткая зависимость от DatabaseManager
    private DatabaseManager _db;

    // Настройки отчёта (заполняются промежуточными методами)
    private string _sql = "";
    private string _title = "";
    private string[] _headers = Array.Empty<string>();
    private int[] _widths = Array.Empty<int>();
    private bool _numbered = false;      // [ГРУППА А]
    private string _footer = "";         // [ГРУППА В]

    /// <summary>
    /// Конструктор. Принимает DatabaseManager для выполнения запросов.
    /// </summary>
    /// <param name="db">Экземпляр DatabaseManager</param>
    public ReportBuilder(DatabaseManager db)
    {
        _db = db;
    }

    // === Промежуточные методы (возвращают this) ===

    /// <summary>
    /// Устанавливает SQL-запрос для отчёта
    /// </summary>
    /// <param name="sql">Текст SQL-запроса</param>
    public ReportBuilder Query(string sql)
    {
        _sql = sql;
        return this;
    }

    /// <summary>
    /// Устанавливает заголовок отчёта
    /// </summary>
    /// <param name="title">Текст заголовка</param>
    public ReportBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Устанавливает названия колонок для отображения
    /// </summary>
    /// <param name="columns">Массив названий колонок</param>
    public ReportBuilder Header(params string[] columns)
    {
        _headers = columns;
        return this;
    }

    /// <summary>
    /// Устанавливает ширину каждой колонки в символах
    /// </summary>
    /// <param name="widths">Массив ширины колонок</param>
    public ReportBuilder ColumnWidths(params int[] widths)
    {
        _widths = widths;
        return this;
    }

    /// <summary>
    /// [ГРУППА А] Включить нумерацию строк в отчёте
    /// </summary>
    public ReportBuilder Numbered()
    {
        _numbered = true;
        return this;
    }

    /// <summary>
    /// [ГРУППА В] Добавить итоговую строку с меткой
    /// </summary>
    /// <param name="label">Текст метки итоговой строки</param>
    public ReportBuilder Footer(string label)
    {
        _footer = label;
        return this;
    }

    // === Терминальные методы ===

    /// <summary>
    /// Выполняет запрос, форматирует результат через StringBuilder,
    /// возвращает готовую строку отчёта
    /// </summary>
    /// <returns>Отформатированная строка отчёта</returns>
    public string Build()
    {
        // Выполняем запрос через DatabaseManager
        var (columns, rows) = _db.ExecuteQuery(_sql);
        var sb = new StringBuilder();

        // Заголовок отчёта
        if (_title.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"==={_title}===");
        }

        // Определяем названия колонок
        string[] displayHeaders = _headers.Length > 0 ? _headers : columns;

        // Определяем ширину колонок
        int colCount = displayHeaders.Length;
        int[] widths;
        if (_widths.Length >= colCount)
        {
            widths = _widths;
        }
        else
        {
            widths = new int[colCount];
            for (int i = 0; i < colCount; i++)
                widths[i] = 20;
        }

        // Ширина столбца нумерации [ГРУППА А]
        int numWidth = _numbered ? 5 : 0;

        // Шапка таблицы
        if (_numbered)
            sb.Append("№".PadRight(numWidth));
        for (int i = 0; i < colCount; i++)
            sb.Append(displayHeaders[i].PadRight(widths[i]));
        sb.AppendLine();

        // Разделитель
        int totalWidth = numWidth;
        for (int i = 0; i < colCount; i++)
            totalWidth += widths[i];
        sb.AppendLine(new string('-', totalWidth));

        // Строки данных
        for (int r = 0; r < rows.Count; r++)
        {
            if (_numbered)
                sb.Append((r + 1).ToString().PadRight(numWidth));
            for (int c = 0; c < rows[r].Length && c < colCount; c++)
                sb.Append(rows[r][c].PadRight(widths[c]));
            sb.AppendLine();
        }

        // Итоговая строка [ГРУППА В]
        if (_footer.Length > 0)
        {
            sb.AppendLine(new string('-', totalWidth));
            sb.AppendLine($"{_footer}: {rows.Count}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Выполняет Build() и выводит результат в консоль
    /// </summary>
    public void Print()
    {
        Console.Write(Build());
    }

    /// <summary>
    /// [ГРУППА Б] Сохраняет отчёт в текстовый файл
    /// </summary>
    /// <param name="path">Путь к файлу для сохранения</param>
    public void SaveToFile(string path)
    {
        File.WriteAllText(path, Build());
        Console.WriteLine($"Отчёт сохранён в файл: {path}");
    }
}