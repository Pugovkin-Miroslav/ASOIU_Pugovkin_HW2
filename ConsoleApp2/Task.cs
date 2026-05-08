using System;

/// <summary>
/// Задача (основная таблица, сторона «много»)
/// </summary>
class Task
{
    /// <summary>Идентификатор задачи (первичный ключ)</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор проекта (внешний ключ)</summary>
    public int ProjectId { get; set; }

    /// <summary>Название задачи</summary>
    public string Name { get; set; }

    private int _hours;

    /// <summary>
    /// Трудоёмкость в человеко-часах (не может быть отрицательной)
    /// </summary>
    public int Hours
    {
        get => _hours;
        set
        {
            if (value < 0)
                throw new ArgumentException("Трудоёмкость не может быть отрицательной");
            _hours = value;
        }
    }

    /// <summary>
    /// Конструктор с параметрами
    /// </summary>
    /// <param name="id">Идентификатор задачи</param>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <param name="name">Название задачи</param>
    /// <param name="hours">Трудоёмкость в часах</param>
    public Task(int id, int projectId, string name, int hours)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
        Hours = hours; // валидация сработает здесь
    }

    /// <summary>
    /// Конструктор по умолчанию (вызывает полный через this)
    /// </summary>
    public Task() : this(0, 0, "", 0) { }

    /// <summary>
    /// Возвращает строковое представление задачи
    /// </summary>
    public override string ToString() =>
        $"[{Id}] {Name}, Проект#{ProjectId}, часов: {Hours}";
}