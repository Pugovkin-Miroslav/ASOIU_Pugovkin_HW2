/// <summary>
/// Проект (справочная таблица, сторона «один»).
/// Представляет справочник проектов для связи один-ко-многим с задачами.
/// </summary>
class Project
{
    /// <summary>Идентификатор проекта (первичный ключ)</summary>
    public int Id { get; set; }

    /// <summary>Название проекта</summary>
    public string Name { get; set; }

    /// <summary>
    /// Конструктор с параметрами.
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    /// <param name="name">Название проекта</param>
    public Project(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Конструктор по умолчанию (вызывает полный через this).
    /// </summary>
    public Project() : this(0, "") { }

    /// <summary>
    /// Возвращает строковое представление проекта.
    /// </summary>
    /// <returns>Строка формата "[Id] Name"</returns>
    public override string ToString() => $"[{Id}] {Name}";
}